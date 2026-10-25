# Housing Unlocked

A Dalamud plugin for FFXIV that gives you full control over placing housing items — **and tells you when the game is about to snap them out of your hands.**

Forked from [BDTH (Burning Down the House)](https://github.com/LeonBlade/BDTHPlugin) by LeonBlade.

## Snap warnings (the reason this fork exists)

The FFXIV housing engine snaps items in three different ways. Housing Unlocked decodes all three from the game's own data and warns you **before** the snap happens:

| Warning | Driven by | Where the data comes from |
|---|---|---|
| `WILL SNAP TO FLOOR` / `WILL SNAP UPWARD` | Each item's snap threshold | Regular snap: `min(width, depth) / 2` from the item's collision footprint; **SphereCastRange override** read live from the item's layout instances, with an embedded table (116 items) extracted from every housing `.sgb` in the game files |
| `WILL SNAP TO ITEM` | Each item's SphereCastRange block (sgb type `0x4B`) | Extracted from all 1907 housing `.sgb` files — e.g. Stage Panel 0.1, Hanging Bench 0.4, arcade machines 1.0–1.5, beds 1.8, chocobo stables 2.2, Raised Wooden Deck 2.7 |
| `WILL SNAP TO TABLE` | `TableTop` collision-material surfaces on nearby placed furniture | Read live from each placed item's colliders, incl. per-primitive material flags |

All warnings support a configurable safety margin (default `0.07`) so the warning fires slightly **before** the engine's snap point.

The floor check is **above-first**, matching the engine: if the gap up to a floor above is within the item's distance, the item will snap upward; the floor below only applies when no floor above is in range. Drag direction does not change the result. Snaps are resolved when the housing menu is exited.

## Commands

All commands are `/hu` (you must be in rotate mode for item editing):

- `/hu` — opens the main control window
- `/hu list` — opens the furnishing list (indoors only); sort by distance to find items in dense rooms
- `/hu debug` — debug window: snap model internals, SphereCastRange reads, TableTop receivers
- `/hu reset` — resets the main window position
- `/hu autoopen` — toggles auto-open when entering housing mode
- `/hu threshold <value>` — snap warning safety margin (default `0.07`)
- `/hu learn` — records the active item's current floor distance as its snap-distance override (manual overrides beat everything)
- `/hu unlearn` — removes the override
- `/hu itemsnap` — toggles the item-to-item snap warning
- `/hu itemsnapmode <either|sum|placed>` — which side's SphereCastRange drives the item-snap distance (default `either` = the larger of the two)
- `/hu tablesnap` — toggles the table snap warning

You can also pass coordinates directly: `/hu x y z [ry]`.

## How the snap model was decoded

- **Regular snap** — `min(|size.X|, |size.Z|) / 2` from the item's collision footprint (validated against in-game measurements: Grassy Rug 2.06, White Rect Partition 0.11, Company Chest 0.49, Serving Barrel 0.36).
- **Override snap** — items carrying a `SphereCastRange` (LayerEntryType `0x4B`) block in their `.sgb` use its `Cast.X` instead. Verified against [Housingway](https://github.com/Abyeon/Housingway)'s shipped implementation and an independent extraction of all 1907 housing `.sgb` files.
- **Wall snap** — wall-mounted items (HousingItemCategory 15) use their `TargetMarker` children: `max XZ distance × 0.5`.
- **Table snap** — tabletop items (ItemUICategory 78) attach onto `TableTop`-flagged collision surfaces (material nibble `d4 == 8`) of nearby furniture.

Floor-Y lookup for the upward warning uses the hardcoded indoor floor heights `-7 / 0 / +7` (confirmed across all housing types).

## Differences from upstream BDTH

- Snap warning banners (floor / item-to-item / table) with per-item engine-accurate distances
- `/hu learn` + `/hu unlearn` for manual per-item snap-distance overrides
- Condensed UI: icon+text Copy/Paste, no duplicate drag rows
- Auto-open via `/hu autoopen` instead of an in-window checkbox
- Renamed throughout (`HousingUnlocked`, `/hu`) so it can sit alongside BDTH or BDTV without conflict

## Credits

- **[LeonBlade](https://github.com/LeonBlade)** — BDTH, the foundation of this plugin. Support him on [Ko-Fi](https://ko-fi.com/LeonBlade) or [Patreon](https://patreon.com/LeonBlade).
- **[Abyeon / Housingway](https://github.com/Abyeon/Housingway)** — the SphereCastRange runtime decode and wall TargetMarker rule.
- **The Re:MakePlace team** — material-flag tables and the SGB research that seeded the item-to-item work.
- **Do not bother LeonBlade, his Discord, or the Housingway author with issues from this fork** — they go [here](https://github.com/dexcss/HousingUnlocked/issues).
