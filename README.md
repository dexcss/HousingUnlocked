# Housing Unlocked

A Dalamud plugin for FFXIV housing. Full placement control (inherited from
[BDTH](https://github.com/LeonBlade/BDTHPlugin) by LeonBlade) plus **snap prediction**: it tells you
what the game is about to snap to **before** you let go of the item.

Open with `/hu`.

> **Under active testing.** Not a fully working product yet — features can be incomplete or behave
> unexpectedly, especially rotated wall items. Bug reports with a session log are very welcome.

---

## What it predicts

The FFXIV housing engine snaps items in four ways. Housing Unlocked decodes each one from the
game's own data and shows a single status card: teal = nothing will snap, magenta = something will.

| Warning | What drives it |
|---|---|
| `WILL SNAP — floor` / `floor above` | The item's authored SphereCastRange when it has one, otherwise `min(footprint X, Z) / 2` from the item's model box. The surface **above** is checked first, matching the engine. |
| `WILL SNAP — item: X` | Both items' SphereCastRange (Either / Sum / Placed-only). |
| `WILL SNAP — table: X` | `TableTop` collision surfaces on nearby furniture. |
| `WILL SNAP — wall: X` | Wall-mounted items only (category 15): the receiver's own snap walls. |
| `→ X will snap here` | Reverse warning: an already-placed wall item will re-attach to the item you are holding. |

Snaps are resolved by the engine when the housing menu is exited, not on deselect — the card
predicts accordingly.

## How the wall prediction works

Wall snapping is the part that needed real measurements, so it is worth stating plainly:

- Receivers are **hollow**: a multi-face partition has a thin snap wall on **every** face; a
  single-face item (Stage Panel) has one. They are not solid boxes.
- The measured face is the one the item is against (**axis from the winning collision wall**) on
  the side the item faces (**side from the item's front**).
- Capture is **1.0 of travel along the item's own axis** — `perpendicular gap / |front · normal|`
  — compared strictly (`0.99` snaps, `1.00` does not). A rotated item therefore reaches less far
  along the wall.
- Priority when several receivers are in range: house wall < single-face < multi-face, with a
  single-face panel blocking a multi-face receiver once the item is more than `0.37` behind it.
- Backfaces never attach.

## Install

Add this URL as a third-party plugin repository in Dalamud:

```
https://raw.githubusercontent.com/dexcss/DalamudPlugins/main/repo.json
```

Then install **Housing Unlocked** from the plugin installer. Requires Dalamud API level 15.

## Commands

All commands are `/hu`. Item editing needs Rotate mode.

| Command | Effect |
|---|---|
| `/hu` | Toggle the main window |
| `/hu autoopen` | Auto-open the window when entering housing mode |
| `/hu threshold <v>` | Safety margin for formula-derived floor warnings (default `0.07`) |
| `/hu itemsnap` | Toggle item-to-item warnings |
| `/hu itemsnapmode <either\|sum\|placed>` | Which item's range drives the item snap |
| `/hu tablesnap` | Toggle tabletop warnings |
| `/hu wallsnap` | Toggle wall + reverse-wall warnings |
| `/hu wallsrefresh` | Rebuild the territory wall cache |
| `/hu log` | Start/stop a session log (see below) |
| `/hu mark <note>` | Place a marker in the session log |
| `/hu logpath` | Print the log folder |
| `/hu list` | Placed furnishing list |
| `/hu debug` | Debug window |
| `/hu reset` | Reset the window position |
| `/hu x y z [ry]` | Set the active item's position/rotation |

## Position control

- **X / Y / Z** rows: type a value, or hover and use the mouse wheel (step = the Drag value).
- Lock any axis so the wheel and gizmo cannot move it.
- **RY** for rotation, copy/paste to move exact transforms between items.
- **Place Anywhere** ignores the engine's placement limits; **Gizmo** adds an on-screen move
  widget (local/world), **Snap** makes it move on the drag grid.

## Session logging (opt-in)

Logging is **off by default** — no files are written for users who never ask for them. Enable it
with `/hu log` or **Settings → Start logging** when you want a trace for a bug report. It writes a
JSONL session to the plugin config folder; `/hu logpath` prints the location.

Wall truth lines (`POSTSNAP_FINAL`, `layout_change`, `housing_exit_snap`) only run while logging is
on and only for wall-mounted items.

## Notes and known issues

- Floor, item and table snapping are stable.
- Wall snapping is correct at the measured boundaries but has no alignment gate: an item rotated
  nearly perpendicular to a wall can still read a snap.
- The reverse `→ X will snap here` line is a hint, not a guarantee.
- Outdoors behaves differently from indoors; both are supported but tested separately.

## Differences from upstream BDTH

- Snap prediction card (floor / item / table / wall) with engine-derived distances
- `/hu learn` + `/hu unlearn` for manual per-item floor-distance overrides
- Condensed UI: icon+text Copy/Paste, no duplicate drag rows
- Auto-open via `/hu autoopen` instead of an in-window checkbox
- Renamed throughout (`HousingUnlocked`, `/hu`) so it can sit alongside BDTH or BDTV without conflict

## Credits

- **[LeonBlade](https://github.com/LeonBlade)** — BDTH, the foundation of this plugin. Support him on [Ko-Fi](https://ko-fi.com/LeonBlade) or [Patreon](https://patreon.com/LeonBlade).
- **[Abyeon / Housingway](https://github.com/Abyeon/Housingway)** — the SphereCastRange runtime decode and wall TargetMarker rule.
- **The Re:MakePlace team** — material-flag tables and the SGB research that seeded the item-to-item work.
- **Do not bother LeonBlade, his Discord, or the Housingway author with issues from this fork** — they go [here](https://github.com/dexcss/HousingUnlocked/issues).

## License

Same terms as the upstream BDTH plugin.
