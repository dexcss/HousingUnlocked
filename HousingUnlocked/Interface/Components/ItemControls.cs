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
      var inSnapZone = isFloorSnapItem && Memory.ActiveItemMovedSinceSelect && SnapData.IsInFloorSnapZone(
        activeItemId, directionalDistance, effectiveThreshold);

      // Gather every snap state once, then show one universal status line for ALL
      // items: RED = at least one snap will fire at this position, GREEN = none.
      if (Configuration.ShowItemSnapWarning && activeItemId != 0)
        Memory.UpdateItemSnapDetection();
      if (Configuration.ShowTableSnapWarning && activeItemId != 0)
        Memory.UpdateTableSnapDetection();
      if (Configuration.ShowWallSnapWarning && activeItemId != 0)
        Memory.UpdateWallSnapDetection();

      var snapSummary = "";
      if (inSnapZone)
        snapSummary = snapsUp ? "floor above" : "floor";
      if (Configuration.ShowItemSnapWarning && Memory.LastItemSnapHit)
        snapSummary += (snapSummary.Length > 0 ? ", " : "") + "item: " +
          (string.IsNullOrEmpty(Memory.LastItemSnapTargetName)
            ? SnapRangeData.GetItemName(Memory.LastItemSnapTargetItemId)
            : Memory.LastItemSnapTargetName);
      if (Configuration.ShowTableSnapWarning && Memory.LastTableSnapHit)
        snapSummary += (snapSummary.Length > 0 ? ", " : "") + "table: " +
          (string.IsNullOrEmpty(Memory.LastTableSnapTargetName) ? "table" : Memory.LastTableSnapTargetName);
      if (Configuration.ShowWallSnapWarning && Memory.LastWallSnapHit)
        snapSummary += (snapSummary.Length > 0 ? ", " : "") + "wall: " +
          (string.IsNullOrEmpty(Memory.LastWallSnapTargetName) ? "wall surface" : Memory.LastWallSnapTargetName);

      if (snapSummary.Length > 0)
        ImGui.TextColored(RED, $"\u26A0 WILL SNAP \u2014 {snapSummary}");
      else
        ImGui.TextColored(GREEN, "\u2713 GOOD \u2014 no snap");

      if (ImGui.IsItemHovered())
      {
        ImGui.BeginTooltip();
        if (snapSummary.Length == 0)
          ImGui.Text("No snap detected; the item stays where it is.");
        else
        {
          if (inSnapZone)
          {
            ImGui.Text(snapsUp ? "Floor above: WILL SNAP UPWARD" : "Floor: WILL SNAP TO FLOOR");
            ImGui.Text($"Vertical direction: {(dir < 0 ? "pushing down" : dir > 0 ? "pulling up" : "stationary")}");
            if (Memory.LastFloorRaycastHit)
              ImGui.Text($"Distance to surface below: {Memory.LastFloorRaycastDistance:0.###}");
            if (Memory.LastCeilingRaycastHit)
              ImGui.Text($"Distance to floor above: {Memory.LastCeilingRaycastDistance:0.###}");
            if (perItemThreshold > 0f)
              ImGui.Text(Memory.ActiveItemThresholdIsOverride
                ? $"Per-item snap distance: {perItemThreshold:0.###} (authored exact)"
                : $"Per-item snap distance: {perItemThreshold:0.###} (+{Configuration.SnapThreshold:0.###} safety = {effectiveThreshold:0.###})");
            ImGui.Text("Snap applies when you DESELECT the item.");
          }

          if (Configuration.ShowItemSnapWarning && Memory.LastItemSnapHit)
          {
            var snapName = string.IsNullOrEmpty(Memory.LastItemSnapTargetName)
              ? SnapRangeData.GetItemName(Memory.LastItemSnapTargetItemId)
              : Memory.LastItemSnapTargetName;
            ImGui.Text($"Item: {snapName} (item {Memory.LastItemSnapTargetItemId})");
            ImGui.Text($"  distance {Memory.LastItemSnapDistance:0.###}, range {Memory.LastItemSnapEffectiveRange:0.###} (+{Configuration.SnapThreshold:0.###} safety)");
            ImGui.Text("  Snap applies when you DESELECT the item.");
          }

          if (Configuration.ShowTableSnapWarning && Memory.LastTableSnapHit)
          {
            var tableName = string.IsNullOrEmpty(Memory.LastTableSnapTargetName) ? "table" : Memory.LastTableSnapTargetName;
            ImGui.Text($"Table: {tableName} (surface Y {Memory.LastTableSnapSurfaceY:0.###}, pivot \u0394Y {Memory.LastTableSnapDeltaY:0.###})");
            ImGui.Text("  Snap applies when you DESELECT the item.");
          }

          if (Configuration.ShowWallSnapWarning && Memory.LastWallSnapHit)
          {
            var wallName = string.IsNullOrEmpty(Memory.LastWallSnapTargetName) ? "wall surface" : Memory.LastWallSnapTargetName;
            ImGui.Text($"Wall: {wallName} at {Memory.LastWallSnapDistance:0.###} (capture {Memory.LastWallSnapThreshold:0.###})");
            ImGui.Text(Memory.LastWallSnapTargetIsTerritory
              ? "  House wall: snaps the item on DESELECT."
              : "  False wall: attaches when the housing interface closes.");
          }
        }
        ImGui.EndTooltip();
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
