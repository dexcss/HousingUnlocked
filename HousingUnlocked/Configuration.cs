using Dalamud.Configuration;
using System;

namespace HousingUnlocked
{
  [Serializable]
  public class Configuration : IPluginConfiguration
  {
    // Required by IPluginConfiguration (Dalamud's config versioning hook).
    public int Version { get; set; } = 0;
    public bool UseGizmo { get; set; } = false;
    public bool DoSnap { get; set; } = false;
    public float Drag { get; set; } = 0.05f;
    public bool SortByDistance { get; set; } = false;
    public bool AutoVisible { get; set; } = true;
    public bool PlaceAnywhere { get; set; } = false;
    public bool DisplayFurnishingList { get; set; } = true;
    public bool DisplayInventory { get; set; } = true;

    // Safety margin for formula-derived floor snap distances. Authored values
    // (SphereCastRange, wall markers, TargetMarker distances) are exact and never
    // get the margin — see PluginMemory.GetActiveItemFFXIVId.
    public float SnapThreshold { get; set; } = 0.07f;

    // Warning toggles (also available in the Advanced section of the main window).
    public bool ShowSnapWarning { get; set; } = true;
    public bool ShowItemSnapWarning { get; set; } = true;
    public bool ShowTableSnapWarning { get; set; } = true;
    public bool ShowWallSnapWarning { get; set; } = true;

    // Session logging is opt-in: nothing is written to disk for users who never asked for
    // it. Start a session on demand with /hu log or the Advanced button, or turn this on to
    // restore the old "auto start when the main window opens" behaviour.
    public bool AutoStartSessionLog { get; set; } = false;

    // Which side's SphereCastRange drives the item-to-item snap distance:
    // Either = max(active, placed); Sum = active + placed; PlacedOnly = placed only.
    public ItemSnapRangeMode ItemSnapRangeMode { get; set; } = ItemSnapRangeMode.Either;

    // Wall capture: flat 1.0 measured in-game for every wall item and every wall
    // surface. The item's back-ray must reach the surface within this distance.
    public float WallCaptureDistance { get; set; } = 1.0f;

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
