using System;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;
using Dalamud.Utility;

using HousingUnlocked.Interface.Components;

namespace HousingUnlocked.Interface.Windows
{
  /// <summary>
  /// Fancy UI main window: one scrollable page with the snap status, transform controls, and all
  /// settings. Borderless shell from <see cref="ShellWindow"/>; behaviour is unchanged.
  /// </summary>
  public class MainWindow : ShellWindow
  {
    private static PluginMemory Memory => Plugin.GetMemory();
    private static Configuration Configuration => Plugin.GetConfiguration();

    private readonly Gizmo _gizmo;
    private readonly ItemControls _itemControls = new();

    public bool Reset;

    public MainWindow(Gizmo gizmo)
      : base("Housing Unlocked##HousingUnlocked", "Housing Unlocked",
        new Vector2(480, 300), new Vector2(1000, 1600))
    {
      _gizmo = gizmo;
    }

    public override void PreDraw()
    {
      base.PreDraw();
      if (!Reset)
        return;
      Reset = false;
      ImGui.SetNextWindowPos(new Vector2(69, 69), ImGuiCond.Always);
    }

    protected override void DrawBody()
    {
      var s = Theme.UiScale;

      DrawQuickToggles();
      ImGui.Dummy(new Vector2(0, Theme.Gap));

      DrawItemSection();
      ImGui.Dummy(new Vector2(0, Theme.Gap));

      DrawControlsSection();
      DrawWarningsSection();
      DrawInventorySection();
      DrawMaintenanceSection();
      DrawAboutSection();

      _ = s;
    }

    // ------------------------------------------------------------ quick row

    private void DrawQuickToggles()
    {
      var avail = ImGui.GetContentRegionAvail().X;
      var w = (avail - 2f * Theme.Gap) / 3f;

      var placeAnywhere = Configuration.PlaceAnywhere;
      var newPlaceAnywhere = UiKit.ToggleChip("##hu-chip-pa", "Place Anywhere", placeAnywhere, w,
        "Place objects without the engine's placement limits.");
      if (newPlaceAnywhere != placeAnywhere)
      {
        PluginMemory.SetPlaceAnywhere(newPlaceAnywhere);
        Configuration.PlaceAnywhere = newPlaceAnywhere;
        Configuration.Save();
      }

      ImGui.SameLine(0, Theme.Gap);
      var useGizmo = Configuration.UseGizmo;
      var newUseGizmo = UiKit.ToggleChip("##hu-chip-gizmo", "Gizmo", useGizmo, w,
        "Show a movement gizmo on the selected item.");
      if (newUseGizmo != useGizmo)
      {
        Configuration.UseGizmo = newUseGizmo;
        Configuration.Save();
      }

      ImGui.SameLine(0, Theme.Gap);
      var doSnap = Configuration.DoSnap;
      var newDoSnap = UiKit.ToggleChip("##hu-chip-snap", "Snap grid", doSnap, w,
        "Snap gizmo movement to the drag grid.");
      if (newDoSnap != doSnap)
      {
        Configuration.DoSnap = newDoSnap;
        Configuration.Save();
      }
    }

    // ----------------------------------------------------------------- item

    private unsafe void DrawItemSection()
    {
      if (Memory.HousingStructure == null || Memory.HousingStructure->Mode == HousingLayoutMode.None)
        DrawHint("Enter housing mode to get started.");
      else if (PluginMemory.GamepadMode)
        DrawHint("Gamepad mode is not supported.");
      else if (Memory.HousingStructure->ActiveItem == null
            || Memory.HousingStructure->Mode != HousingLayoutMode.Rotate)
        DrawHint("Select a housing item in Rotate mode.");
      else
        _itemControls.Draw();
    }

    // ------------------------------------------------------------- controls

    private void DrawControlsSection()
    {
      var s = Theme.UiScale;
      UiKit.Section(FontAwesomeIcon.Wrench, "controls");

      ImGui.TextColored(Theme.Text, "Drag");
      ImGui.SameLine(0, Theme.Gap);
      ImGui.SetNextItemWidth(80f * s);
      var drag = Configuration.Drag;
      if (ImGui.InputFloat("##hu-drag", ref drag, 0.01f, 0f, "%.3f"))
      {
        Configuration.Drag = Math.Clamp(drag, 0.001f, 10f);
        Configuration.Save();
      }
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Step for the +/- buttons, mouse wheel, and gizmo snap.");

      ImGui.SameLine(0, Theme.Gap * 2f);
      var local = _gizmo.Mode == ImGuizmoMode.Local;
      if (UiKit.ChipButton("##hu-gizmo-space", local ? "Gizmo: Local" : "Gizmo: World"))
        _gizmo.Mode = local ? ImGuizmoMode.World : ImGuizmoMode.Local;
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Gizmo axes: local item space or world space.");
    }

    // ------------------------------------------------------------- warnings

    private void DrawWarningsSection()
    {
      var s = Theme.UiScale;
      UiKit.Section(FontAwesomeIcon.ExclamationTriangle, "warnings");
      var avail = ImGui.GetContentRegionAvail().X;
      var w = (avail - Theme.Gap) / 2f;

      var floor = Configuration.ShowSnapWarning;
      var newFloor = UiKit.ToggleChip("##hu-w-floor", "Floor", floor, w, "Floor snap warnings.");
      if (newFloor != floor)
      {
        Configuration.ShowSnapWarning = newFloor;
        Configuration.Save();
      }

      ImGui.SameLine(0, Theme.Gap);
      var items = Configuration.ShowItemSnapWarning;
      var newItems = UiKit.ToggleChip("##hu-w-item", "Items", items, w, "Item-to-item snap warnings.");
      if (newItems != items)
      {
        Configuration.ShowItemSnapWarning = newItems;
        Configuration.Save();
      }

      var tables = Configuration.ShowTableSnapWarning;
      var newTables = UiKit.ToggleChip("##hu-w-table", "Tables", tables, w, "Tabletop snap warnings.");
      if (newTables != tables)
      {
        Configuration.ShowTableSnapWarning = newTables;
        Configuration.Save();
      }

      ImGui.SameLine(0, Theme.Gap);
      var walls = Configuration.ShowWallSnapWarning;
      var newWalls = UiKit.ToggleChip("##hu-w-wall", "Walls", walls, w, "Wall snap + reverse warnings.");
      if (newWalls != walls)
      {
        Configuration.ShowWallSnapWarning = newWalls;
        Configuration.Save();
      }

      ImGui.Dummy(new Vector2(0, 3f * s));
      var mode = (int)Configuration.ItemSnapRangeMode;
      var newMode = UiKit.Segmented("##hu-range", new[] { "Either", "Sum", "Placed" }, mode);
      if (newMode != mode)
      {
        Configuration.ItemSnapRangeMode = (ItemSnapRangeMode)newMode;
        Configuration.Save();
      }
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Which item's SphereCastRange drives item-to-item distance.");
    }

    // ------------------------------------------------------------ inventory

    private void DrawInventorySection()
    {
      UiKit.Section(FontAwesomeIcon.List, "inventory");
      var avail = ImGui.GetContentRegionAvail().X;
      var w = (avail - 2f * Theme.Gap) / 3f;

      var housingGoods = AtkManager.HousingGoods != null && AtkManager.HousingGoods.IsVisible;
      var newGoods = UiKit.ToggleChip("##hu-inv-list", "Furnishing list", housingGoods, w);
      if (newGoods != housingGoods)
      {
        AtkManager.ShowFurnishingList(newGoods);
        Configuration.DisplayFurnishingList = newGoods;
        Configuration.Save();
      }

      ImGui.SameLine(0, Theme.Gap);
      var inventory = AtkManager.InventoryVisible;
      var newInventory = UiKit.ToggleChip("##hu-inv-bags", "Inventory", inventory, w);
      if (newInventory != inventory)
      {
        AtkManager.ShowInventory(newInventory);
        Configuration.DisplayInventory = newInventory;
        Configuration.Save();
      }

      ImGui.SameLine(0, Theme.Gap);
      if (UiKit.ChipButton("##hu-inv-open", "Open list", false, w))
        Plugin.CommandManager.ProcessCommand("/hu list");
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Sortable list of placed furnishings — click one to select it.");
    }

    // ---------------------------------------------------------- maintenance

    private void DrawMaintenanceSection()
    {
      var s = Theme.UiScale;
      UiKit.Section(FontAwesomeIcon.Cog, "maintenance");
      var avail = ImGui.GetContentRegionAvail().X;
      var w = (avail - 2f * Theme.Gap) / 3f;

      if (UiKit.ChipButton("##hu-m-walls", "Rebuild walls", false, w))
        Memory.InvalidateTerritoryWallCache();
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Re-scan the house walls on the next wall item selection.");

      ImGui.SameLine(0, Theme.Gap);
      if (UiKit.ChipButton("##hu-m-reset", "Reset window", false, w))
        Reset = true;
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Move the window back to its default position.");

      ImGui.SameLine(0, Theme.Gap);
      if (UiKit.ChipButton("##hu-m-log", SnapLogger.Enabled ? "Logging: on" : "Logging: off", SnapLogger.Enabled, w))
      {
        SnapLogger.ManualOverride = true;
        if (SnapLogger.Enabled)
          SnapLogger.Stop("/hu menu");
        else
          SnapLogger.Start("/hu menu");
      }
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Writes a JSONL trace to the plugin config folder (for bug reports).");

      ImGui.Dummy(new Vector2(0, 2f * s));
      ImGui.TextColored(Theme.Muted, "Debug window: /hu debug");
    }

    // ---------------------------------------------------------------- about

    private void DrawAboutSection()
    {
      var s = Theme.UiScale;
      var version = typeof(Plugin).Assembly.GetName().Version?.ToString() ?? "dev";

      UiKit.Section(FontAwesomeIcon.InfoCircle, "about");
      UiKit.Chip($"v{version}", Theme.Accent);
      ImGui.SameLine(0, Theme.Gap);
      if (UiKit.ChipButton("##hu-about-github", "GitHub / issues"))
        Dalamud.Utility.Util.OpenLink("https://github.com/dexcss/HousingUnlocked");
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Source, releases and issue tracker.");

      ImGui.Dummy(new Vector2(0, 2f * s));
      ImGui.TextColored(Theme.Muted, "Forked from BDTH by LeonBlade.");
      ImGui.TextColored(Theme.Muted, "Under active testing — report bugs with /hu log.");
      _ = s;
    }

    private static void DrawHint(string text)
    {
      ImGui.TextColored(Theme.Muted, text);
    }
  }
}
