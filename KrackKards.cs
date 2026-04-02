using System.Reflection;
using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;
using Path = System.IO.Path;

namespace KrackKards;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class KrackKardsMod(
    ISptLogger<KrackKardsMod> logger,
    ModHelper modHelper,
    DatabaseService databaseService,
    ConfigServer configServer,
    CustomItemService customItemService
) : IOnLoad
{
    private const string RagmanId  = "5ac3b934156ae10c4430e83c";
    private const string RoublesId = "5449016a4bdc2d6f028b456f";

    private readonly RagfairConfig   _ragfairConfig   = configServer.GetConfig<RagfairConfig>();
    private readonly InventoryConfig _inventoryConfig = configServer.GetConfig<InventoryConfig>();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling         = JsonCommentHandling.Skip,
        AllowTrailingCommas         = true
    };

    public Task OnLoad()
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        var db        = databaseService.GetTables();
        var ragman    = db.Traders[new MongoId(RagmanId)];

        // Collect all card IDs so the card case can accept them all
        var allCardIds = new List<string>();

        // ── Anime cards ──────────────────────────────────────────────────────
        foreach (var file in EnumJson(pathToMod, "config", "anime", "collectables", "cards"))
            ProcessCard(file, db, ragman, allCardIds);

        foreach (var file in EnumJson(pathToMod, "config", "anime", "collectables", "packs"))
            ProcessPack(file, db, ragman);

        foreach (var file in EnumJson(pathToMod, "config", "anime", "cases"))
            ProcessBinder(file, db, ragman);

        // ── Pokemon cards ────────────────────────────────────────────────────
        foreach (var file in EnumJson(pathToMod, "config", "pokemon", "cards"))
            ProcessCard(file, db, ragman, allCardIds);

        foreach (var file in EnumJson(pathToMod, "config", "pokemon", "packs"))
            ProcessPack(file, db, ragman);

        foreach (var file in EnumJson(pathToMod, "config", "pokemon", "binders"))
            ProcessBinder(file, db, ragman);

        // ── Card case ────────────────────────────────────────────────────────
        var casePath = Path.Combine(pathToMod, "config", "cases", "card_case.json");
        if (File.Exists(casePath))
            ProcessCardCase(casePath, db, ragman, allCardIds);

        logger.Success("[KrackKards] Gotta find 'em all!");
        return Task.CompletedTask;
    }

    // ── Per-type processing ──────────────────────────────────────────────────

    private void ProcessCard(string file, DatabaseTables db, Trader ragman, List<string> allCardIds)
    {
        var cfg = Load(file);
        if (cfg == null) return;
        RegisterItem(cfg, db, ragman);
        allCardIds.Add(cfg.Id);
    }

    private void ProcessPack(string file, DatabaseTables db, Trader ragman)
    {
        var cfg = Load(file);
        if (cfg == null) return;
        RegisterItem(cfg, db, ragman);
        RegisterLootBox(cfg);
    }

    private void ProcessBinder(string file, DatabaseTables db, Trader ragman)
    {
        var cfg = Load(file);
        if (cfg == null) return;
        RegisterItem(cfg, db, ragman);
        ApplySlotStructure(cfg, db);
    }

    private void ProcessCardCase(string file, DatabaseTables db, Trader ragman, List<string> allCardIds)
    {
        var cfg = Load(file);
        if (cfg == null) return;
        RegisterItem(cfg, db, ragman);
        BuildAllCardsSlot(cfg.Id, allCardIds, db);
    }

    // ── Core registration ────────────────────────────────────────────────────

    private void RegisterItem(KrackItemConfig cfg, DatabaseTables db, Trader ragman)
    {
        customItemService.CreateItemFromClone(new NewItemFromCloneDetails
        {
            ItemTplToClone       = cfg.CloneItem,
            NewId                = cfg.Id,
            ParentId             = cfg.ItemParent,
            HandbookParentId     = cfg.CategoryId,
            HandbookPriceRoubles = cfg.Price,
            FleaPriceRoubles     = cfg.Price,
            Locales = new Dictionary<string, LocaleDetails>
            {
                {
                    "en", new LocaleDetails
                    {
                        Name        = cfg.ItemName,
                        ShortName   = cfg.ItemShortName,
                        Description = cfg.ItemDescription
                    }
                }
            },
            OverrideProperties = new TemplateItemProperties
            {
                BackgroundColor   = cfg.Color,
                Weight            = (float)cfg.Weight,
                Width             = cfg.ExternalSize.Width,
                Height            = cfg.ExternalSize.Height,
                StackMaxSize      = cfg.StackMaxSize,
                ExaminedByDefault = cfg.ExaminedByDefault
            }
        });

        // Apply properties not covered by OverrideProperties
        if (db.Templates.Items.TryGetValue(new MongoId(cfg.Id), out var item) && item.Properties != null)
        {
            if (!string.IsNullOrEmpty(cfg.ItemPrefabPath))
            {
                item.Properties.Prefab ??= new Prefab();
                item.Properties.Prefab.Path = cfg.ItemPrefabPath;
            }
            if (!string.IsNullOrEmpty(cfg.ItemSound))
                item.Properties.ItemSound = cfg.ItemSound;
            if (cfg.DiscardLimit >= 0)
                item.Properties.DiscardLimit = cfg.DiscardLimit;
        }

        // Trader stock
        if (cfg.Sold)
            AddToRagman(cfg, ragman);

        // Flea blacklist (all KrackKards items are blacklisted as in the original)
        _ragfairConfig.Dynamic.Blacklist.Custom.Add(cfg.Id);
    }

    // ── Slot structures ──────────────────────────────────────────────────────

    /// <summary>Parse slotStructure JSON elements and assign to item in DB.</summary>
    private static void ApplySlotStructure(KrackItemConfig cfg, DatabaseTables db)
    {
        if (cfg.SlotStructure is not { Count: > 0 }) return;
        if (!db.Templates.Items.TryGetValue(new MongoId(cfg.Id), out var item) || item.Properties == null) return;

        var slots = new List<Slot>();
        foreach (var el in cfg.SlotStructure)
        {
            var name     = TryStr(el, "_name");
            var id       = TryStr(el, "_id");
            var parent   = TryStr(el, "_parent");
            var required = TryBool(el, "_required");
            var merge    = TryBool(el, "_mergeSlotWithChildren");
            var proto    = TryStr(el, "_proto") ?? "55d30c4c4bdc2db4468b457e";

            var filters = new List<SlotFilter>();
            if (el.TryGetProperty("_props", out var props) &&
                props.TryGetProperty("filters", out var filtersArr))
            {
                foreach (var f in filtersArr.EnumerateArray())
                {
                    var ids = new List<MongoId>();
                    if (f.TryGetProperty("Filter", out var filterList))
                        foreach (var fid in filterList.EnumerateArray())
                            ids.Add(new MongoId(fid.GetString() ?? ""));
                    filters.Add(new SlotFilter { Filter = new HashSet<MongoId>(ids) });
                }
            }

            slots.Add(new Slot
            {
                Name                  = name   ?? "",
                Id                    = id     ?? "",
                Parent                = parent ?? "",
                Properties            = new SlotProperties { Filters = filters },
                Required              = required,
                MergeSlotWithChildren = merge,
                Prototype             = proto
            });
        }

        item.Properties.Slots = slots;
    }

    /// <summary>Build a single catch-all slot that accepts every card ID.</summary>
    private static void BuildAllCardsSlot(string itemId, List<string> cardIds, DatabaseTables db)
    {
        if (cardIds.Count == 0) return;
        if (!db.Templates.Items.TryGetValue(new MongoId(itemId), out var item) || item.Properties == null) return;

        item.Properties.Slots =
        [
            new Slot
            {
                Name   = "mod_mount_1",
                Id     = $"{itemId}_card_slot",
                Parent = itemId,
                Properties = new SlotProperties
                {
                    Filters =
                    [
                        new SlotFilter { Filter = new HashSet<MongoId>(cardIds.Select(id => new MongoId(id))) }
                    ]
                },
                Required              = false,
                MergeSlotWithChildren = false,
                Prototype             = "55d30c4c4bdc2db4468b457e"
            }
        ];
    }

    // ── Loot box ─────────────────────────────────────────────────────────────

    private void RegisterLootBox(KrackItemConfig cfg)
    {
        if (!cfg.IsLootBox || cfg.LootContent == null) return;

        _inventoryConfig.RandomLootContainers[cfg.Id] = new RewardDetails
        {
            RewardCount   = cfg.LootContent.RewardCount,
            FoundInRaid   = cfg.LootContent.FoundInRaid,
            RewardTplPool = cfg.LootContent.RewardTplPool
                .ToDictionary(kvp => new MongoId(kvp.Key), kvp => kvp.Value)
        };
    }

    // ── Trader ───────────────────────────────────────────────────────────────

    private static void AddToRagman(KrackItemConfig cfg, Trader ragman)
    {
        var id = new MongoId(cfg.Id);
        ragman.Assort.Items.Add(new Item
        {
            Id       = id,
            Template = id,
            ParentId = "hideout",
            SlotId   = "hideout",
            Upd      = new Upd
            {
                UnlimitedCount    = cfg.UnlimitedStock,
                StackObjectsCount = (double)cfg.StockAmount
            }
        });
        ragman.Assort.BarterScheme[id]    = [[new BarterScheme { Count = cfg.Price, Template = new MongoId(RoublesId) }]];
        ragman.Assort.LoyalLevelItems[id] = cfg.TraderLoyaltyLevel;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private KrackItemConfig? Load(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<KrackItemConfig>(File.ReadAllText(path), JsonOpts);
        }
        catch (Exception ex)
        {
            logger.Warning($"[KrackKards] Failed to load {Path.GetFileName(path)}: {ex.Message}");
            return null;
        }
    }

    private static IEnumerable<string> EnumJson(params string[] parts)
    {
        var dir = Path.Combine(parts);
        return Directory.Exists(dir) ? Directory.EnumerateFiles(dir, "*.json") : [];
    }

    private static string? TryStr(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) ? v.GetString() : null;

    private static bool TryBool(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.GetBoolean();
}
