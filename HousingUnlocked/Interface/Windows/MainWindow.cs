using System;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;

using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;

using HousingUnlocked.Interface.Components;

namespace HousingUnlocked.Interface.Windows
{
  public class MainWindow(Gizmo gizmo) : Window("Housing Unlocked##HousingUnlocked",
    ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize |
    ImGuiWindowFlags.AlwaysAutoResize)
  {
    private static PluginMemory Memory => Plugin.GetMemory();
    private static Configuration Configuration => Plugin.GetConfiguration();

    private static readonly Vector4 Gold = Theme.Accent;
    private static readonly Vector4 Red = Theme.Warn;
    private static readonly Vector4 Muted = Theme.Muted;

    private readonly ItemControls _itemControls = new();

    private bool _showSettings;

    public bool Reset;

    public override void PreDraw()
    {
      Theme.Push();
      // A stable minimum width keeps the window from resizing as the status text
      // changes — one source of the "flickering" feel while dragging.
      SizeConstraints = new WindowSizeConstraints
      {
        MinimumSize = new Vector2(330, 0),
        MaximumSize = new Vector2(560, 2000),
      };

      if (!Reset)
        return;
      Reset = false;
      ImGui.SetNextWindowPos(new Vector2(69, 69), ImGuiCond.Always);
    }

    public override void PostDraw() => Theme.Pop();

    public override unsafe void Draw()
    {
      DrawQuickToggles();
      DrawDragStep();
      ImGui.Separator();

      if (Memory.HousingStructure == null)
        DrawHint("Enter housing mode to get started.");
      else if (Memory.HousingStructure->Mode == HousingLayoutMode.None)
        DrawHint("Enter housing mode to get started.");
      else if (PluginMemory.GamepadMode)
        DrawHint("Gamepad mode is not supported.");
      else if (Memory.HousingStructure->ActiveItem == null || Memory.HousingStructure->Mode != HousingLayoutMode.Rotate)
        DrawHint("Select a housing item in Rotate mode.");
      else
        _itemControls.Draw();

      ImGui.Separator();
      DrawFooter();
    }

    private static void DrawDragStep()
    {
      ImGui.TextDisabled("Drag");
      ImGui.SameLine();
      ImGui.SetNextItemWidth(120f * ImGui.GetIO().FontGlobalScale);
      var drag = Configuration.Drag;
      if (ImGui.InputFloat("##hu-drag", ref drag, 0.01f, 0f, "%.3f"))
      {
        Configuration.Drag = Math.Clamp(drag, 0.001f, 10f);
        Configuration.Save();
      }
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Step for the +/- buttons, mouse wheel, and gizmo snap.");
    }

    private void DrawQuickToggles()
    {
      var placeAnywhere = Configuration.PlaceAnywhere;
      if (ImGui.Checkbox("Place Anywhere", ref placeAnywhere))
      {
        PluginMemory.SetPlaceAnywhere(placeAnywhere);
        Configuration.PlaceAnywhere = placeAnywhere;
        Configuration.Save();
      }
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Place objects without the engine's placement limits.");

      ImGui.SameLine();
      var useGizmo = Configuration.UseGizmo;
      if (ImGui.Checkbox("Gizmo", ref useGizmo))
      {
        Configuration.UseGizmo = useGizmo;
        Configuration.Save();
      }
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Show a movement gizmo on the selected item.");

      ImGui.SameLine();
      var doSnap = Configuration.DoSnap;
      if (ImGui.Checkbox("Snap", ref doSnap))
      {
        Configuration.DoSnap = doSnap;
        Configuration.Save();
      }
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Snap gizmo movement to the drag grid.");

      ImGui.SameLine();
      if (ImGuiComponents.IconButton(1, gizmo.Mode == ImGuizmoMode.Local ? FontAwesomeIcon.ArrowsAlt : FontAwesomeIcon.Globe))
        gizmo.Mode = gizmo.Mode == ImGuizmoMode.Local ? ImGuizmoMode.World : ImGuizmoMode.Local;
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip($"Gizmo space: {(gizmo.Mode == ImGuizmoMode.Local ? "Local" : "World")}");
    }

    private void DrawSettings()
    {
      var version = typeof(Plugin).Assembly.GetName().Version?.ToString() ?? "dev";
      ImGui.TextColored(Gold, "SETTINGS");
      ImGui.SameLine();
      ImGui.TextDisabled($"v{version}");

      var floorWarnings = Configuration.ShowSnapWarning;
      if (ImGui.Checkbox("Floor snap warnings", ref floorWarnings))
      {
        Configuration.ShowSnapWarning = floorWarnings;
        Configuration.Save();
      }

      var itemWarnings = Configuration.ShowItemSnapWarning;
      if (ImGui.Checkbox("Item-to-item warnings", ref itemWarnings))
      {
        Configuration.ShowItemSnapWarning = itemWarnings;
        Configuration.Save();
      }

      var tableWarnings = Configuration.ShowTableSnapWarning;
      if (ImGui.Checkbox("Table snap warnings", ref tableWarnings))
      {
        Configuration.ShowTableSnapWarning = tableWarnings;
        Configuration.Save();
      }

      var wallWarnings = Configuration.ShowWallSnapWarning;
      if (ImGui.Checkbox("Wall snap warnings", ref wallWarnings))
      {
        Configuration.ShowWallSnapWarning = wallWarnings;
        Configuration.Save();
      }

      ImGui.Spacing();
      var mode = (int)Configuration.ItemSnapRangeMode;
      if (ImGui.Combo("Item snap range", ref mode, "Either\0Sum\0Placed\0"))
      {
        Configuration.ItemSnapRangeMode = (ItemSnapRangeMode)mode;
        Configuration.Save();
      }
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Which item's SphereCastRange drives item-to-item distance.");

      ImGui.Spacing();
      if (ImGui.Button("Rebuild wall cache"))
        Memory.InvalidateTerritoryWallCache();
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Re-scan the house walls on the next wall item selection.");

      ImGui.SameLine();
      if (ImGui.Button("Reset window"))
        Reset = true;

      ImGui.SameLine();
      if (ImGui.Button(SnapLogger.Enabled ? "Stop logging" : "Start logging"))
      {
        SnapLogger.ManualOverride = true;
        if (SnapLogger.Enabled)
          SnapLogger.Stop("/hu menu");
        else
          SnapLogger.Start("/hu menu");
      }
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Writes a JSONL trace to the plugin config folder (for bug reports).");

      ImGui.TextDisabled("Debug window: /hu debug");
    }

    private void DrawFooter()
    {
      var housingGoods = AtkManager.HousingGoods != null && AtkManager.HousingGoods.IsVisible;
      var inventory = AtkManager.InventoryVisible;

      if (ImGui.Checkbox("Furnishing list", ref housingGoods))
      {
        AtkManager.ShowFurnishingList(housingGoods);
        Configuration.DisplayFurnishingList = housingGoods;
        Configuration.Save();
      }

      ImGui.SameLine();
      if (ImGui.Checkbox("Inventory", ref inventory))
      {
        AtkManager.ShowInventory(inventory);
        Configuration.DisplayInventory = inventory;
        Configuration.Save();
      }

      ImGui.Spacing();
      if (ImGui.Button("Open furnishing list"))
        Plugin.CommandManager.ProcessCommand("/hu list");
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Sortable list of placed furnishings — click one to select it.");

      ImGui.SameLine();
      if (ImGui.Button(_showSettings ? "Close settings" : "Settings"))
        _showSettings = !_showSettings;

      if (_showSettings)
      {
        ImGui.Separator();
        DrawSettings();
      }
    }

    private static void DrawHint(string text)
    {
      ImGui.Spacing();
      ImGui.TextColored(Muted, text);
      ImGui.Spacing();
    }
  }
}
