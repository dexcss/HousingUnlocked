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
  /// Fancy UI main window: borderless shell with a left rail (Item / Settings / About) and
  /// card-based pages. All bindings to configuration and memory are unchanged.
  /// </summary>
  public class MainWindow : ShellWindow
  {
    private static PluginMemory Memory => Plugin.GetMemory();
    private static Configuration Configuration => Plugin.GetConfiguration();

    private readonly Gizmo _gizmo;
    private readonly ItemControls _itemControls = new();

    private const int PageItem = 0;
    private const int PageSettings = 1;
    private const int PageAbout = 2;

    private int _page = PageItem;

    public bool Reset;

    public MainWindow(Gizmo gizmo)
      : base("Housing Unlocked##HousingUnlocked", "Housing Unlocked",
        new Vector2(460, 360), new Vector2(900, 1400))
    {
      _gizmo = gizmo;
      Size = new Vector2(560, 560);
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
      var railW = 42f * s;

      ImGui.BeginChild("##hu-rail", new Vector2(railW, 0), false, ImGuiWindowFlags.NoScrollbar);
      if (UiKit.IconButton("##hu-tab-item", FontAwesomeIcon.Cube, "Item", _page == PageItem, 32f))
        _page = PageItem;
      ImGui.Dummy(new Vector2(0, 4f * s));
      if (UiKit.IconButton("##hu-tab-settings", FontAwesomeIcon.Cog, "Settings", _page == PageSettings, 32f))
        _page = PageSettings;
      ImGui.Dummy(new Vector2(0, 4f * s));
      if (UiKit.IconButton("##hu-tab-about", FontAwesomeIcon.InfoCircle, "About", _page == PageAbout, 32f))
        _page = PageAbout;
      ImGui.EndChild();

      ImGui.SameLine(0, Theme.Gap);
      ImGui.BeginChild("##hu-page", new Vector2(0, 0), false, ImGuiWindowFlags.None);
      switch (_page)
      {
        case PageSettings:
          DrawSettingsPage();
          break;
        case PageAbout:
          DrawAboutPage();
          break;
        default:
          DrawItemPage();
          break;
      }
      ImGui.EndChild();
    }

    // ------------------------------------------------------------------ item

    private unsafe void DrawItemPage()
    {
      var s = Theme.UiScale;
      DrawQuickToggles();
      ImGui.Dummy(new Vector2(0, 2f * s));
      DrawControlsRow();

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

    private void DrawQuickToggles()
    {
      var s = Theme.UiScale;
      var rowH = 26f * s;
      var (min, max) = UiKit.BeginCard(3f * rowH + Theme.Pad * 2f);
      var x = min.X + Theme.Pad;
      var right = max.X - Theme.Pad;
      var y = min.Y + Theme.Pad;

      var placeAnywhere = Configuration.PlaceAnywhere;
      var newPlaceAnywhere = SwitchRow("Place Anywhere", placeAnywhere,
        "Place objects without the engine's placement limits.", new Vector2(x, y), right, rowH);
      if (newPlaceAnywhere != placeAnywhere)
      {
        PluginMemory.SetPlaceAnywhere(newPlaceAnywhere);
        Configuration.PlaceAnywhere = newPlaceAnywhere;
        Configuration.Save();
      }

      y += rowH;
      var useGizmo = Configuration.UseGizmo;
      var newUseGizmo = SwitchRow("Gizmo", useGizmo,
        "Show a movement gizmo on the selected item.", new Vector2(x, y), right, rowH);
      if (newUseGizmo != useGizmo)
      {
        Configuration.UseGizmo = newUseGizmo;
        Configuration.Save();
      }

      y += rowH;
      var doSnap = Configuration.DoSnap;
      var newDoSnap = SwitchRow("Snap to grid", doSnap,
        "Snap gizmo movement to the drag grid.", new Vector2(x, y), right, rowH);
      if (newDoSnap != doSnap)
      {
        Configuration.DoSnap = newDoSnap;
        Configuration.Save();
      }
    }

    /// <summary>Label on the left, switch on the right; returns the (possibly new) value.</summary>
    private static bool SwitchRow(string label, bool value, string tooltip, Vector2 at, float rightEdge, float rowH)
    {
      var s = Theme.UiScale;
      ImGui.SetCursorScreenPos(new Vector2(at.X, at.Y + (rowH - ImGui.GetTextLineHeight()) * 0.5f));
      ImGui.TextColored(Theme.Text, label);
      if (tooltip.Length > 0 && ImGui.IsItemHovered())
        ImGui.SetTooltip(tooltip);

      ImGui.SetCursorScreenPos(new Vector2(rightEdge - 40f * s, at.Y + (rowH - 22f * s) * 0.5f));
      return UiKit.Toggle("##hu-sw-" + label, value);
    }

    // -------------------------------------------------------------- settings

    /// <summary>Drag step and gizmo space — on the item page, where they are used.</summary>
    private void DrawControlsRow()
    {
      var s = Theme.UiScale;

      ImGui.TextColored(Theme.Text, "Drag");
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Step for the +/- buttons, mouse wheel, and gizmo snap.");
      ImGui.SameLine(0, Theme.Gap);
      ImGui.SetNextItemWidth(80f * s);
      var drag = Configuration.Drag;
      if (ImGui.InputFloat("##hu-drag", ref drag, 0.01f, 0f, "%.3f"))
      {
        Configuration.Drag = Math.Clamp(drag, 0.001f, 10f);
        Configuration.Save();
      }

      ImGui.SameLine(0, Theme.Gap * 2f);
      var local = _gizmo.Mode == ImGuizmoMode.Local;
      if (UiKit.TextIconButton("##hu-gizmo-space", local ? FontAwesomeIcon.ArrowsAlt : FontAwesomeIcon.Globe,
            local ? "Local" : "World"))
        _gizmo.Mode = local ? ImGuizmoMode.World : ImGuizmoMode.Local;
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Gizmo axes: local item space or world space.");
    }

    private void DrawSettingsPage()
    {
      var s = Theme.UiScale;

      UiKit.Section(FontAwesomeIcon.ExclamationTriangle, "snap warnings");

      var floorWarnings = Configuration.ShowSnapWarning;
      DrawSwitchRowInline("Floor snap warnings", ref floorWarnings);
      if (floorWarnings != Configuration.ShowSnapWarning)
      {
        Configuration.ShowSnapWarning = floorWarnings;
        Configuration.Save();
      }

      var itemWarnings = Configuration.ShowItemSnapWarning;
      DrawSwitchRowInline("Item-to-item warnings", ref itemWarnings);
      if (itemWarnings != Configuration.ShowItemSnapWarning)
      {
        Configuration.ShowItemSnapWarning = itemWarnings;
        Configuration.Save();
      }

      var tableWarnings = Configuration.ShowTableSnapWarning;
      DrawSwitchRowInline("Table snap warnings", ref tableWarnings);
      if (tableWarnings != Configuration.ShowTableSnapWarning)
      {
        Configuration.ShowTableSnapWarning = tableWarnings;
        Configuration.Save();
      }

      var wallWarnings = Configuration.ShowWallSnapWarning;
      DrawSwitchRowInline("Wall snap warnings", ref wallWarnings);
      if (wallWarnings != Configuration.ShowWallSnapWarning)
      {
        Configuration.ShowWallSnapWarning = wallWarnings;
        Configuration.Save();
      }

      ImGui.Dummy(new Vector2(0, 4f * s));
      ImGui.TextColored(Theme.Muted, "Item snap range");
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Which item's SphereCastRange drives item-to-item distance.");
      ImGui.Dummy(new Vector2(0, 2f * s));
      var mode = (int)Configuration.ItemSnapRangeMode;
      var newMode = UiKit.Segmented("##hu-snapmode", new[] { "Either", "Sum", "Placed" }, mode);
      if (newMode != mode)
      {
        Configuration.ItemSnapRangeMode = (ItemSnapRangeMode)newMode;
        Configuration.Save();
      }

      ImGui.Dummy(new Vector2(0, Theme.Gap));
      UiKit.Section(FontAwesomeIcon.List, "inventory");

      var housingGoods = AtkManager.HousingGoods != null && AtkManager.HousingGoods.IsVisible;
      DrawSwitchRowInline("Furnishing list", ref housingGoods);
      if (housingGoods != (AtkManager.HousingGoods != null && AtkManager.HousingGoods.IsVisible))
      {
        AtkManager.ShowFurnishingList(housingGoods);
        Configuration.DisplayFurnishingList = housingGoods;
        Configuration.Save();
      }

      var inventory = AtkManager.InventoryVisible;
      DrawSwitchRowInline("Inventory", ref inventory);
      if (inventory != AtkManager.InventoryVisible)
      {
        AtkManager.ShowInventory(inventory);
        Configuration.DisplayInventory = inventory;
        Configuration.Save();
      }

      ImGui.Dummy(new Vector2(0, 4f * s));
      if (UiKit.TextIconButton("##hu-open-list", FontAwesomeIcon.List, "Open furnishing list"))
        Plugin.CommandManager.ProcessCommand("/hu list");
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Sortable list of placed furnishings — click one to select it.");

      ImGui.Dummy(new Vector2(0, Theme.Gap));
      UiKit.Section(FontAwesomeIcon.Cog, "maintenance");

      if (UiKit.TextIconButton("##hu-wallcache", FontAwesomeIcon.Sync, "Rebuild wall cache"))
        Memory.InvalidateTerritoryWallCache();
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Re-scan the house walls on the next wall item selection.");

      ImGui.SameLine(0, Theme.Gap);
      if (UiKit.TextIconButton("##hu-reset", FontAwesomeIcon.ArrowsAlt, "Reset window"))
        Reset = true;

      ImGui.Spacing();
      if (UiKit.GradientButton("##hu-log", SnapLogger.Enabled ? "Stop logging" : "Start logging",
            Theme.Accent, Theme.Warn, 34f))
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

    /// <summary>Switch row laid out in normal flow; writes the new value back through ref.</summary>
    private static void DrawSwitchRowInline(string label, ref bool value)
    {
      var s = Theme.UiScale;
      var start = ImGui.GetCursorScreenPos();
      var avail = ImGui.GetContentRegionAvail().X;

      ImGui.SetCursorScreenPos(new Vector2(start.X, start.Y + 2f * s));
      ImGui.TextColored(Theme.Text, label);

      ImGui.SetCursorScreenPos(new Vector2(start.X + avail - 40f * s, start.Y + 3f * s));
      value = UiKit.Toggle("##hu-sw-" + label, value);

      ImGui.SetCursorScreenPos(new Vector2(start.X, start.Y + 26f * s));
    }

    // ----------------------------------------------------------------- about

    private void DrawAboutPage()
    {
      var s = Theme.UiScale;
      var version = typeof(Plugin).Assembly.GetName().Version?.ToString() ?? "dev";

      var (min, max) = UiKit.BeginCard(96f);
      var dl = ImGui.GetWindowDrawList();
      dl.AddRectFilled(new Vector2(min.X + Theme.Pad, min.Y + Theme.Pad - 2f * s),
        new Vector2(min.X + Theme.Pad + 6f * s, min.Y + Theme.Pad + 14f * s), Theme.U32(Theme.Accent), Theme.RadiusTag);

      ImGui.SetCursorScreenPos(new Vector2(min.X + Theme.Pad + 14f * s, min.Y + Theme.Pad - 4f * s));
      ImGui.TextColored(Theme.Text, "Housing Unlocked");
      ImGui.SameLine(0, Theme.Gap);
      UiKit.Chip($"v{version}", Theme.Accent);

      ImGui.SetCursorScreenPos(new Vector2(min.X + Theme.Pad + 14f * s, min.Y + Theme.Pad + 22f * s));
      ImGui.TextColored(Theme.Muted, "Snap prediction and placement control for FFXIV housing.");

      ImGui.SetCursorScreenPos(new Vector2(min.X + Theme.Pad, min.Y + Theme.Pad + 50f * s));
      ImGui.TextColored(Theme.Muted, "Forked from BDTH by LeonBlade.");

      ImGui.Dummy(new Vector2(0, Theme.Gap));
      UiKit.Section(FontAwesomeIcon.Heart, "community");

      if (UiKit.TextIconButton("##hu-github", FontAwesomeIcon.Link, "GitHub / issues"))
        Dalamud.Utility.Util.OpenLink("https://github.com/dexcss/HousingUnlocked");
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Source, releases and issue tracker.");

      ImGui.Dummy(new Vector2(0, Theme.Gap));
      ImGui.TextColored(Theme.Muted, "Under active testing — features may be incomplete.");
      ImGui.TextColored(Theme.Muted, "Please report bugs with a session log (/hu log).");
    }

    private static void DrawHint(string text)
    {
      var s = Theme.UiScale;
      var (min, max) = UiKit.BeginCard(46f);
      ImGui.SetCursorScreenPos(new Vector2(min.X + Theme.Pad, min.Y + Theme.Pad));
      ImGui.TextColored(Theme.Muted, text);
      _ = max;
    }
  }
}
