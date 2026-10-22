using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Components;

using Dalamud.Bindings.ImGui;

namespace HousingUnlocked.Interface.Components
{
  public class ItemControls
  {
    private static PluginMemory Memory => Plugin.GetMemory();
    private static Configuration Configuration => Plugin.GetConfiguration();

    private static float Drag => Configuration.Drag;

    private float? lockX;
    private float? lockY;
    private float? lockZ;

    private Vector3? copyPosition;
    private float? copyRotation;

    private Vector4 RED = new(1, .2f, .2f, 1);
    private Vector4 GREEN = new(.2f, 1, .2f, 1);
    private Vector4 BLUE = new(.2f, .2f, 1, 1);
    private Vector4 ORANGE = new(1, .6f, .1f, 1);

    public unsafe void Draw()
    {
      if (Memory.HousingStructure->ActiveItem != null)
      {
        Memory.Position = Memory.ReadPosition();
        // Handle lock logic.
        if (lockX != null)
          Memory.Position.X = (float)lockX;
        if (lockY != null)
          Memory.Position.Y = (float)lockY;
        if (lockZ != null)
          Memory.Position.Z = (float)lockZ;
        Memory.WritePosition(Memory.Position);
      }

      // Snap-status banner (only shown when there's an item selected and the
      // setting is enabled). Reflects whether the active item is FloorSnap-tagged
      // and currently within the per-item snap distance derived from its collider footprint.
      var activeItemId = Memory.GetActiveItemFFXIVId();
      var isFloorSnapItem = activeItemId != 0 && SnapData.HasFloorSnap(activeItemId);
      var floorD = float.PositiveInfinity;
      var ceilD = float.PositiveInfinity;
      var distanceToFloor = float.PositiveInfinity;
      if (isFloorSnapItem)
      {
        distanceToFloor = Memory.GetDistanceToFloorBelow();
        floorD = Memory.LastFloorRaycastDistance;
        ceilD = Memory.LastCeilingRaycastDistance;
      }

      // Direction-aware: the engine snaps toward the direction of travel. Pushing
      // an item down targets the nearest surface BELOW (terrain and rocks included
      // — the "sunken item, random rock" case); pulling up targets the floor above;
      // stationary falls back to whichever is closer.
      var dir = Memory.VerticalMoveDirection;
      var snapsUp = dir > 0 ? true : dir < 0 ? false : ceilD < floorD;
      var directionalDistance = snapsUp ? ceilD : floorD;

      // Authored thresholds (SphereCastRange / wall marker / manual learn) are
      // EXACT — in-game confirmed: Stage Panel snaps at 0.100, free at 0.101 —
      // so the safety margin is only added for approximate formula thresholds.
      var perItemThreshold = Memory.ActiveItemSnapThreshold;
      var effectiveThreshold = perItemThreshold > 0f
        ? perItemThreshold + (Memory.ActiveItemThresholdIsOverride ? 0f : Configuration.SnapThreshold)
        : Configuration.SnapThreshold;
      var inSnapZone = isFloorSnapItem && SnapData.IsInFloorSnapZone(
        activeItemId, directionalDistance, effectiveThreshold);

      if (Configuration.ShowSnapWarning && isFloorSnapItem)
      {
        if (inSnapZone)
        {
          // Determine which direction the snap will fire (movement direction, or
          // whichever surface is closer when stationary).
          ImGui.TextColored(RED, snapsUp ? "\u26A0 WILL SNAP UPWARD" : "\u26A0 WILL SNAP TO FLOOR");
        }
        else
          ImGui.TextColored(GREEN, "\u2713 Floating safely");
        if (ImGui.IsItemHovered())
        {
          ImGui.BeginTooltip();
          ImGui.Text("This item is tagged as floor-snapping. Snap applies when you DESELECT the item;");
          ImGui.Text("pushed-down items may snap only when you exit the housing interface.");
          ImGui.Text($"Vertical direction: {(dir < 0 ? "pushing down" : dir > 0 ? "pulling up" : "stationary")} — snap target follows movement.");
          if (Memory.LastFloorRaycastHit)
            ImGui.Text($"Distance to surface below (terrain included): {Memory.LastFloorRaycastDistance:0.###}");
          else
            ImGui.Text("No surface detected below the item (open air).");
          if (Memory.LastCeilingRaycastHit)
            ImGui.Text($"Distance to floor above: {Memory.LastCeilingRaycastDistance:0.###}");
          else
            ImGui.Text("No surface detected above the item (open air).");
          if (perItemThreshold > 0f)
            ImGui.Text(Memory.ActiveItemThresholdIsOverride ? $"Per-item snap distance: {perItemThreshold:0.###} (authored exact)" : $"Per-item snap distance: {perItemThreshold:0.###}  (+{Configuration.SnapThreshold:0.###} safety = {effectiveThreshold:0.###})");
          else
            ImGui.Text($"Per-item snap distance unavailable, using fallback {Configuration.SnapThreshold:0.###}.");
          ImGui.Text("Adjust safety margin with: /hu threshold <value>");
          ImGui.EndTooltip();
        }
      }

      // Item-to-item snap warning: fires when the active item is within the
      // SphereCastRange-derived snap distance of a placed snap-capable item
      // (each item's authored 0x4B override — see SnapRangeData).
      if (Configuration.ShowItemSnapWarning && activeItemId != 0)
      {
        Memory.UpdateItemSnapDetection();
        if (Memory.LastItemSnapHit)
        {
          var snapName = string.IsNullOrEmpty(Memory.LastItemSnapTargetName)
            ? SnapRangeData.GetItemName(Memory.LastItemSnapTargetItemId)
            : Memory.LastItemSnapTargetName;
          ImGui.TextColored(ORANGE, $"\u26A0 WILL SNAP TO ITEM: {snapName}");
          if (ImGui.IsItemHovered())
          {
            ImGui.BeginTooltip();
            ImGui.Text($"Target: {snapName} (item {Memory.LastItemSnapTargetItemId})");
            ImGui.Text($"Distance: {Memory.LastItemSnapDistance:0.###}");
            ImGui.Text($"Effective snap range: {Memory.LastItemSnapEffectiveRange:0.###} (+{Configuration.SnapThreshold:0.###} safety)");
            ImGui.Text($"Range mode: {Configuration.ItemSnapRangeMode} — change with /hu itemsnapmode <either|sum|placed>");
            ImGui.Text("Ranges come from each item's SphereCastRange (0x4B) sgb block.");
            ImGui.Text("Snap applies when you DESELECT the item.");
            ImGui.EndTooltip();
          }
        }
      }

      // Table snap warning: tabletop items (ItemUICategory 78) attach onto the
      // TableTop collision surfaces of nearby placed furniture.
      if (Configuration.ShowTableSnapWarning && activeItemId != 0)
      {
        Memory.UpdateTableSnapDetection();
        if (Memory.LastTableSnapHit)
        {
          var tableName = string.IsNullOrEmpty(Memory.LastTableSnapTargetName) ? "table" : Memory.LastTableSnapTargetName;
          ImGui.TextColored(ORANGE, $"\u26A0 WILL SNAP TO TABLE: {tableName}");
          if (ImGui.IsItemHovered())
          {
            ImGui.BeginTooltip();
            ImGui.Text($"Surface: {tableName}");
            ImGui.Text($"Surface Y: {Memory.LastTableSnapSurfaceY:0.###} (item pivot \u0394Y {Memory.LastTableSnapDeltaY:0.###})");
            ImGui.Text("Tabletop items attach to TableTop collision surfaces when close.");
            ImGui.Text("Snap applies when you DESELECT the item.");
            ImGui.EndTooltip();
          }
        }
      }

      // Wall snap warning: wall-mounted items attach onto wall collision surfaces —
      // including "false walls" (stage panels, partitions) via WallCollisionMesh.
      if (Configuration.ShowWallSnapWarning && activeItemId != 0)
      {
        Memory.UpdateWallSnapDetection();
        if (Memory.LastWallSnapHit)
        {
          var wallName = string.IsNullOrEmpty(Memory.LastWallSnapTargetName) ? "wall surface" : Memory.LastWallSnapTargetName;
          if (Memory.LastWallSnapTargetIsTerritory)
          {
            ImGui.TextColored(RED, $"\u26A0 WILL SNAP TO WALL: {wallName}");
          }
          else
          {
            ImGui.TextColored(ORANGE, $"\u26A0 WILL ATTACH TO: {wallName}");
          }
          if (ImGui.IsItemHovered())
          {
            ImGui.BeginTooltip();
            ImGui.Text($"Surface: {wallName}");
            ImGui.Text($"Distance: {Memory.LastWallSnapDistance:0.###}");
            ImGui.Text($"Wall snap distance: {Memory.LastWallSnapThreshold:0.###} (measured flat rule)");
            if (Memory.LastWallSnapTargetIsTerritory)
              ImGui.Text("House walls snap the item on DESELECT.");
            else
              ImGui.Text("False-wall furniture attaches when the housing interface closes or you re-enter the house.");
            ImGui.EndTooltip();
          }
        }
      }

      // Copy / Paste buttons for position & rotation (icon + text).
      if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Copy, "Copy"))
      {
        copyPosition = Memory.Position;
        copyRotation = Memory.Rotation.Y;
      }
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Copy Position & Rotation");

      ImGui.SameLine();

      ImGui.BeginDisabled(copyPosition == null || copyRotation == null);
      {
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Paste, "Paste") && copyPosition != null && copyRotation != null)
        {
          Memory.WritePosition(copyPosition.Value);
          Memory.WriteRotation(Memory.Rotation with { Y = copyRotation.Value });
        }
        if (ImGui.IsItemHovered())
          ImGui.SetTooltip("Paste Position & Rotation");
      }
      ImGui.EndDisabled();

      DrawInputCoord("x coord##hu-x", ref Memory.Position.X, ref lockX, RED);
      DrawInputCoord("y coord##hu-y", ref Memory.Position.Y, ref lockY, GREEN, inSnapZone);
      DrawInputCoord("z coord##hu-z", ref Memory.Position.Z, ref lockZ, BLUE);
      DrawInputRotate("ry degree##hu-ry", ref Memory.Rotation.Y);

      // In-game ground-truth capture (see SnapLogger): session runs while the
      // window is open; /hu mark labels snap moments inside the trace.
      if (SnapLogger.Enabled && Memory.HousingStructure != null)
      {
        var hs = Memory.HousingStructure;
        SnapLogger.Frame(
          activeItemId, (int)Memory.ActiveItemCategory,
          Memory.Position.X, Memory.Position.Y, Memory.Position.Z, Memory.Rotation.Y,
          Memory.VerticalMoveDirection,
          (uint)hs->Mode, (uint)hs->State, (uint)hs->State2, hs->Rotating,
          Configuration.PlaceAnywhere,
          isFloorSnapItem, Memory.LastFloorRaycastHit, floorD, Memory.LastCeilingRaycastHit, ceilD,
          perItemThreshold, effectiveThreshold, inSnapZone, snapsUp,
          Memory.LastItemSnapHit, Memory.LastItemSnapTargetName, Memory.LastItemSnapDistance, Memory.LastItemSnapEffectiveRange,
          Memory.LastWallSnapHit, Memory.LastWallSnapTargetName, Memory.LastWallSnapDistance, Memory.LastWallSnapThreshold, Memory.LastWallSnapTargetIsTerritory, Memory.WallSnapDiagnostic,
          Memory.LastTableSnapHit, Memory.LastTableSnapTargetName, Memory.LastTableSnapDeltaY, Memory.TableSnapDiagnostic,
          Memory.SnapThresholdDiagnostic, Memory.ItemSnapDiagnostic);
      }
    }

    private void HandleScrollInput(ref float f)
    {
      if (ImGui.IsMouseHoveringRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax()))
      {
        var delta = ImGui.GetIO().MouseWheel * Drag;
        if (delta != 0)
        {
          f += delta;
          Memory.WritePosition(Memory.Position);
        }
      }
    }

    private bool DrawInput(string name, ref float f)
    {
      var changed = ImGui.InputFloat(name, ref f, Drag);
      HandleScrollInput(ref f);
      ImGui.SameLine();
      return changed;
    }

    private void DrawInputCoord(string name, ref float f)
    {
      if (DrawInput(name, ref f))
        Memory.WritePosition(Memory.Position);
    }

    private void DrawInputRotate(string name, ref float f)
    {
      if (DrawInput(name, ref f))
        Memory.WriteRotation(Memory.Rotation);
    }

    static void DrawCircle(Vector4 color)
    {
      ImGui.SameLine();
      var pos = ImGui.GetCursorScreenPos();
      var center = pos.Y + ImGui.GetTextLineHeight() / 2f + 3f;
      ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(pos.X + 12, center), 8, ImGui.GetColorU32(color));
      ImGui.Dummy(new Vector2(24, 24));
    }

    private void DrawInputCoord(string name, ref float f, ref float? locked, Vector4 color, bool warnSnap = false)
    {
      // Tint the input frame red when this coord is in the floor-snap danger zone.
      if (warnSnap)
        ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.5f, 0.1f, 0.1f, 1f));
      DrawInputCoord(name, ref f);
      if (warnSnap)
        ImGui.PopStyleColor();

      if (ImGuiComponents.IconButton((int)ImGui.GetID(name), locked == null ? FontAwesomeIcon.Unlock : FontAwesomeIcon.Lock))
        locked = locked == null ? f : null;
      DrawCircle(color);
    }
  }
}
