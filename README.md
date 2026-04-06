# SPT-KrackasourusCards

A consolidation and SPT 4.0.13 port of three mods by **krackasourus**:

- `krackasourus-animeCards`
- `krackasourus-pokemonCards`
- `krackasourus-cardCase`

All item content, bundles, and configs are preserved from the originals. This mod merges them into a single server-side C# mod compatible with the SPT 4.0.13 server framework.

---

## Features

- **Anime trading cards** — individual collectible cards with custom art and descriptions
- **Pokémon trading cards** — individual collectible cards with custom art and descriptions
- **Card packs** — purchasable packs that contain randomized cards when opened
- **Binders** — slotted containers for organizing your anime card collection
- **Card case** — a universal storage container that accepts every card added by this mod
- All items are purchased from **Ragman** at various loyalty levels
- All items are **blacklisted from the flea market**
- Card packs use SPT's `RandomLootContainers` system for their contents

---

## Installation

1. Download the latest release from the [Releases](../../releases) page
2. Extract the folder into your SPT `user/mods/` directory
3. Your mod folder should look like: `user/mods/KrackKards/`
4. Launch the SPT server — you'll see `[KrackKards] Gotta find 'em all!` in the server log on success

---

## Compatibility

| SPT Version | Supported |
|-------------|-----------|
| 4.0.13      | ✅ Yes    |
| < 4.0.13    | ❌ No     |

---

## Notes

- Cards are **not** available as loose loot — Ragman is your only source
- All cards are automatically recognized by the card case, including any added in future updates
- This mod does not conflict with other custom item mods unless they share item IDs

---

## Credits

All original item configs, bundles, and artwork belong to **krackasourus**. This mod is a port and consolidation only — no original content was created by Vonbraunz.

---

## License

This project inherits the licensing terms of the original krackasourus mods. Redistribution should respect the wishes of the original author.
