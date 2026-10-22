using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace HousingUnlocked
{
  [Serializable]
  public class Configuration : IPluginConfiguration
  {
    public int Version { get; set; } = 0;
    public bool UseGizmo { get; set; } = false;
    public bool DoSnap { get; set; } = false;
    public float Drag { get; set; } = 0.05f;
    public bool SortByDistance { get; set; } = false;
    public bool AutoVisible { get; set; } = true;
    public bool PlaceAnywhere { get; set; } = false;
    public bool DisplayFurnishingList { get; set; } = true;
    public bool DisplayInventory { get; set; } = true;

    // Snap warning settings. Per-item snap distance is computed from each item's
    // collider footprint using the engine's actual formula. Validation across test
    // items showed the formula matches real snap to within ~0.06 game units, with
    // the worst case being Grassy Rug (formula 2.0, real 2.06). Safety margin of
    // 0.07 ensures the warning covers all observed cases — distance ≤ formula+0.07
    // catches the snap zone reliably without much false-alarm headroom.
    public float SnapThreshold { get; set; } = 0.07f;
    public bool ShowSnapWarning { get; set; } = true;

    // Per-item snap distance overrides, keyed by FFXIV Item RowId. Populated by
    // /hu learn when the formula's auto-computed value is wrong for a specific
    // item (typically when the collision mesh has padding that doesn't match the
    // visual footprint, e.g. Company Chest). When an override is present, it
    // takes precedence over the auto-computed value.
    public Dictionary<uint, float> SnapOverrides { get; set; } = new();

    // Item-to-item snap warning. Driven by each item's SphereCastRange override
    // (sgb type 0x4B, see SnapRangeData): a warning fires when the active item is
    // within the effective snap range of a placed snap-capable item
    // (e.g. Stage Panel 0.1, Hanging Bench 0.4, arcade machines 1.0-1.5).
    public bool ShowItemSnapWarning { get; set; } = true;

    // Which side's SphereCastRange drives the effective item-snap distance:
    // Either = max(active, placed); Sum = active + placed; PlacedOnly = placed only.
    public ItemSnapRangeMode ItemSnapRangeMode { get; set; } = ItemSnapRangeMode.Either;

    // Table snap warning: fires for tabletop items (ItemUICategory 78) when a
    // nearby placed item's TableTop collision surface is within snap margin of
    // the item's pivot — the engine will attach the item onto that surface.
    public bool ShowTableSnapWarning { get; set; } = true;

    // Wall snap warning: fires for wall-mounted items (HousingItemCategory 15)
    // when a nearby WallCollisionMesh surface (house-adjacent false walls like
    // stage panels and partitions) is within the item's wall snap threshold.
    public bool ShowWallSnapWarning { get; set; } = true;

    // The engine PROJECTS wall items back onto the wall plane on deselect within
    // a capture range larger than the TargetMarker threshold (measured in-game:
    // projected from 3.25 yalms, did not project from 5.76). The warning limit
    // for wall items is therefore marker threshold + this margin.
    public float WallCaptureMargin { get; set; } = 2.5f;

    public void Save()
    {
      Plugin.PluginInterface.SavePluginConfig(this);
    }
  }

  public enum ItemSnapRangeMode
  {
    Either = 0,
    Sum = 1,
    PlacedOnly = 2,
  }
}
