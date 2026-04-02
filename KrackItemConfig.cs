using System.Text.Json;
using System.Text.Json.Serialization;

namespace KrackKards;

/// <summary>
/// Matches the JSON config schema used by the original TypeScript mod.
/// </summary>
public class KrackItemConfig
{
    [JsonPropertyName("item_type")]             public string?                       ItemType               { get; set; }
    [JsonPropertyName("clone_item")]            public string                        CloneItem              { get; set; } = "";
    [JsonPropertyName("id")]                    public string                        Id                     { get; set; } = "";
    [JsonPropertyName("item_name")]             public string                        ItemName               { get; set; } = "";
    [JsonPropertyName("item_short_name")]       public string                        ItemShortName          { get; set; } = "";
    [JsonPropertyName("item_description")]      public string                        ItemDescription        { get; set; } = "";
    [JsonPropertyName("item_parent")]           public string                        ItemParent             { get; set; } = "";
    [JsonPropertyName("category_id")]           public string                        CategoryId             { get; set; } = "";
    [JsonPropertyName("item_prefab_path")]      public string                        ItemPrefabPath         { get; set; } = "";
    [JsonPropertyName("color")]                 public string?                       Color                  { get; set; }
    [JsonPropertyName("sold")]                  public bool                          Sold                   { get; set; }
    [JsonPropertyName("lootable")]              public bool                          Lootable               { get; set; }
    [JsonPropertyName("stack_max_size")]        public int                           StackMaxSize           { get; set; } = 1;
    [JsonPropertyName("trader")]                public string?                       Trader                 { get; set; }
    [JsonPropertyName("trader_loyalty_level")]  public int                           TraderLoyaltyLevel     { get; set; } = 1;
    [JsonPropertyName("currency")]              public string?                       Currency               { get; set; }
    [JsonPropertyName("price")]                 public int                           Price                  { get; set; }
    [JsonPropertyName("weight")]                public double                        Weight                 { get; set; }
    [JsonPropertyName("rarity")]                public string?                       Rarity                 { get; set; }
    [JsonPropertyName("loot_locations")]        public Dictionary<string, List<string>>? LootLocations      { get; set; }
    [JsonPropertyName("unlimited_stock")]       public bool                          UnlimitedStock         { get; set; } = true;
    [JsonPropertyName("stock_amount")]          public int                           StockAmount            { get; set; } = 999999;
    [JsonPropertyName("is_container")]          public bool                          IsContainer            { get; set; }
    [JsonPropertyName("is_loot_box")]           public bool                          IsLootBox              { get; set; }
    [JsonPropertyName("is_trophy")]             public bool                          IsTrophy               { get; set; }
    [JsonPropertyName("special")]               public bool                          Special                { get; set; }
    [JsonPropertyName("lootContent")]           public KrackLootContent?             LootContent            { get; set; }
    [JsonPropertyName("slotStructure")]         public List<JsonElement>?            SlotStructure          { get; set; }
    [JsonPropertyName("gridStructure")]         public List<JsonElement>?            GridStructure          { get; set; }
    [JsonPropertyName("ExternalSize")]          public KrackExternalSize             ExternalSize           { get; set; } = new();
    [JsonPropertyName("can_sell_on_ragfair")]   public bool                          CanSellOnRagfair       { get; set; }
    [JsonPropertyName("CanSellOnRagfair")]      public bool                          CanSellOnRagfair2      { get; set; }
    [JsonPropertyName("insurancedisabled")]     public bool                          InsuranceDisabled      { get; set; }
    [JsonPropertyName("availableforinsurance")] public bool                          AvailableForInsurance  { get; set; }
    [JsonPropertyName("examinedbydefault")]     public bool                          ExaminedByDefault      { get; set; }
    [JsonPropertyName("isunremovable")]         public bool                          IsUnremovable          { get; set; }
    [JsonPropertyName("discardingblock")]       public bool                          DiscardingBlock        { get; set; }
    [JsonPropertyName("isundiscardable")]       public bool                          IsUndiscardable        { get; set; }
    [JsonPropertyName("isungivable")]           public bool                          IsUngivable            { get; set; }
    [JsonPropertyName("discardlimit")]          public int                           DiscardLimit           { get; set; } = -1;
    [JsonPropertyName("item_sound")]            public string?                       ItemSound              { get; set; }
    [JsonPropertyName("quest_item")]            public bool                          QuestItem              { get; set; }
    [JsonPropertyName("allow_in_slots")]        public List<string>?                 AllowInSlots           { get; set; }

    public bool EffectiveCanSellOnRagfair => CanSellOnRagfair || CanSellOnRagfair2;
}

public class KrackLootContent
{
    [JsonPropertyName("rewardCount")]    public int                         RewardCount    { get; set; }
    [JsonPropertyName("foundInRaid")]    public bool                        FoundInRaid    { get; set; }
    [JsonPropertyName("rewardTplPool")]  public Dictionary<string, double>  RewardTplPool  { get; set; } = new();
}

public class KrackExternalSize
{
    [JsonPropertyName("width")]  public int Width  { get; set; } = 1;
    [JsonPropertyName("height")] public int Height { get; set; } = 1;
}
