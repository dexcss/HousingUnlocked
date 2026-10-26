using System;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Components;

using Dalamud.Bindings.ImGui;

namespace HousingUnlocked.Interface.Components
{
  /// <summary>
  /// Item panel: live snap status, position/rotation controls, copy/paste.
  /// The status prediction is debounced — while dragging, boundary cases can flip
  /// between good/snap every frame; only a state that holds is shown.
  /// </summary>
  public class ItemControls
  {
    private static PluginMemory Memory => Plugin.GetMemory();
    private static Configuration Configuration => Plugin.GetConfiguration();

    private static float Drag => Configuration.Drag;

    private static readonly Vector4 Red = new(1f, .32f, .32f, 1f);
    private static readonly Vector4 Green = new(.35f, 1f, .45f, 1f);
    private static readonly Vector4 Blue = new(.38f, .58f, 1f, 1f);
    private static readonly Vector4 Gold = new(.95f, .78f, .35f, 1f);

    private float? lockX;
    private float? lockY;
    private float? lockZ;

    private Vector3? copyPosition;
    private float? copyRotation;

    // Status stabilization (flicker fix).
    private const double StatusHoldMs = 160;
    private string _shownSummary = "";
    private string _pendingSummary = "";
    private DateTime _pendingSince = DateTime.UtcNow;
    private nint _statusItem;

    public unsafe void Draw()
    {
      if (Memory.HousingStructure->ActiveItem != null)
      {
        Memory.Position = Memory.ReadPosition();
        if (lockX != null) Memory.Position.X = lockX.Value;
        if (lockY != null) Memory.Position.Y = lockY.Value;
        if (lockZ != null) Memory.Position.Z = lockZ.Value;
        Memory.WritePosition(Memory.Position);
      }

      var activeItemId = Memory.GetActiveItemFFXIVId();
      // Every item snaps down to the surface below it (Fox: the box formula applies
      // to all non-wall items). The old hardcoded list covered only 655 of 2115
      // items — e.g. the Morning Glory Plot got no floor warning at all. Wall-mounted
      // items keep the wall warning and are excluded here as before.
      var isFloorSnapItem = activeItemId != 0 && Memory.ActiveItemCategory != 15;
      var floorD = float.PositiveInfinity;
      var ceilD = float.PositiveInfinity;
      if (isFloorSnapItem)
      {
        Memory.GetDistanceToFloorBelow();
        floorD = Memory.LastFloorRaycastDistance;
        ceilD = Memory.LastCeilingRaycastDistance;
      }

      // The engine resolves floor snaps when the housing menu is exited, and it
      // checks the surface ABOVE the item first: if the gap up to a floor above
      // is within the item's snap distance it snaps upward; only when that is
      // out of range does it fall through to the floor below. Confirmed in-game:
      // Morning Glory Plot (0.7898) snaps up at a 0.78 gap and down at 0.79;
      // Summer Bonfire (1.1767) switches between 1.17 and 1.18. Drag direction
      // does not influence the choice.

      // Authored thresholds are exact; the safety margin only applies to
      // formula-derived values (see PluginMemory).
      var perItemThreshold = Memory.ActiveItemSnapThreshold;
      var effectiveThreshold = perItemThreshold > 0f
        ? perItemThreshold + (Memory.ActiveItemThresholdIsOverride ? 0f : Configuration.SnapThreshold)
        : Configuration.SnapThreshold;
      var floorInZone = isFloorSnapItem && floorD > 0.02f &&
        SnapData.IsInFloorSnapZone(activeItemId, floorD, effectiveThreshold);
      var ceilInZone = isFloorSnapItem && ceilD > 0.02f &&
        SnapData.IsInFloorSnapZone(activeItemId, ceilD, effectiveThreshold);
      var inSnapZone = floorInZone || ceilInZone;
      // The floor above wins whenever it is in range; the floor below is only
      // the engine's choice when the surface above is out of reach. Use the
      // authored distance without the safety margin for this choice, so the
      // direction flips exactly where the engine flips (0.79 for the plot,
      // 1.18 for the bonfire); the margin only widens the early red warning.
      var exactThreshold = perItemThreshold > 0f ? perItemThreshold : Configuration.SnapThreshold;
      var snapsUp = isFloorSnapItem && ceilD > 0.02f &&
        SnapData.IsInFloorSnapZone(activeItemId, ceilD, exactThreshold);

      // Gather every snap state once.
      if (Configuration.ShowItemSnapWarning && activeItemId != 0)
        Memory.UpdateItemSnapDetection();
      if (Configuration.ShowTableSnapWarning && activeItemId != 0)
        Memory.UpdateTableSnapDetection();
      if (Configuration.ShowWallSnapWarning && activeItemId != 0)
        Memory.UpdateWallSnapDetection();
      if (Configuration.ShowWallSnapWarning && activeItemId != 0)
        Memory.UpdateReverseWallSnapDetection();

      var itemName = string.IsNullOrEmpty(Memory.LastItemSnapTargetName)
        ? SnapRangeData.GetItemName(Memory.LastItemSnapTargetItemId)
        : Memory.LastItemSnapTargetName;
      var tableName = string.IsNullOrEmpty(Memory.LastTableSnapTargetName) ? "table" : Memory.LastTableSnapTargetName;
      var wallName = string.IsNullOrEmpty(Memory.LastWallSnapTargetName) ? "wall surface" : Memory.LastWallSnapTargetName;

      var summary = "";
      if (snapsUp)
        summary = "floor above";
      else if (floorInZone)
        summary = "floor";
      else if (ceilInZone)
        summary = "floor above";
      if (Configuration.ShowItemSnapWarning && Memory.LastItemSnapHit)
        summary += (summary.Length > 0 ? " · " : "") + "item: " + itemName;
      if (Configuration.ShowTableSnapWarning && Memory.LastTableSnapHit)
        summary += (summary.Length > 0 ? " · " : "") + "table: " + tableName;
      if (Configuration.ShowWallSnapWarning && Memory.LastWallSnapHit)
        summary += (summary.Length > 0 ? " · " : "") + "wall: " + wallName;

      var shownSummary = Stabilize(summary, Memory.HousingStructure->ActiveItem != null ? (nint)Memory.HousingStructure->ActiveItem : 0);

      ImGui.Spacing();
      DrawStatusCard(shownSummary);

      if (Configuration.ShowWallSnapWarning && Memory.LastReverseWallHit)
      {
        ImGui.TextColored(Red, $"-> {Memory.LastReverseWallNames} will snap here");
        if (ImGui.IsItemHovered())
          ImGui.SetTooltip("Placed wall item(s) re-attach to the item you are holding when the housing menu is closed.");
      }

      ImGui.Spacing();
      ImGui.TextColored(Gold, "POSITION");
      ImGui.Separator();

      DrawPositionRow("X", ref Memory.Position.X, ref lockX, Red);
      DrawPositionRow("Y", ref Memory.Position.Y, ref lockY, Green, inSnapZone);
      DrawPositionRow("Z", ref Memory.Position.Z, ref lockZ, Blue);
      DrawRotationRow();

      ImGui.Spacing();
      if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Copy, "Copy"))
      {
        copyPosition = Memory.Position;
        copyRotation = Memory.Rotation.Y;
      }
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Copy position & rotation");

      ImGui.SameLine();

      ImGui.BeginDisabled(copyPosition == null || copyRotation == null);
      if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Paste, "Paste") && copyPosition != null && copyRotation != null)
      {
        Memory.WritePosition(copyPosition.Value);
        Memory.WriteRotation(Memory.Rotation with { Y = copyRotation.Value });
      }
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Paste position & rotation");
      ImGui.EndDisabled();

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
          Memory.LastReverseWallHit, Memory.LastReverseWallNames,
          Memory.SnapThresholdDiagnostic, Memory.ItemSnapDiagnostic);
      }
    }

    /// <summary>Only switches the displayed state after the new one holds briefly.</summary>
    private string Stabilize(string raw, nint item)
    {
      var now = DateTime.UtcNow;
      if (item != _statusItem)
      {
        _statusItem = item;
        _shownSummary = raw;
        _pendingSummary = raw;
        _pendingSince = now;
        return raw;
      }

      if (raw == _shownSummary)
      {
        _pendingSummary = raw;
        return _shownSummary;
      }

      // Warnings appear immediately: holding a green status for 160 ms while a
      // snap is already starting reads as "it said it wouldn't snap". Only the
      // clearing (warning -> green) is held, so boundary jitter can't blank it.
      if (raw.Length > 0 && _shownSummary.Length == 0)
      {
        _shownSummary = raw;
        _pendingSummary = raw;
        _pendingSince = now;
        return _shownSummary;
      }

      if (raw != _pendingSummary)
      {
        _pendingSummary = raw;
        _pendingSince = now;
        return _shownSummary;
      }

      if ((now - _pendingSince).TotalMilliseconds >= StatusHoldMs)
        _shownSummary = raw;
      return _shownSummary;
    }

    private void DrawStatusCard(string summary)
    {
      var good = summary.Length == 0;
      var color = good ? Green : Red;
      var text = good ? "GOOD \u2014 no snap" : "WILL SNAP \u2014 " + summary;
      if (text.Length > 58)
        text = text[..55] + "\u2026";

      var scale = ImGui.GetIO().FontGlobalScale;
      var pad = 9f * scale;
      var width = ImGui.GetContentRegionAvail().X;
      var height = ImGui.CalcTextSize(text).Y + pad * 2f;
      var p = ImGui.GetCursorScreenPos();

      ImGui.InvisibleButton("##hu-status", new Vector2(width, height));
      var hovered = ImGui.IsItemHovered();

      var dl = ImGui.GetWindowDrawList();
      var bg = good ? new Vector4(.10f, .28f, .14f, .55f) : new Vector4(.36f, .10f, .10f, .60f);
      var max = p + new Vector2(width, height);
      dl.AddRectFilled(p, max, ImGui.GetColorU32(bg), 6f * scale);
      dl.AddRect(p, max, ImGui.GetColorU32(new Vector4(color.X, color.Y, color.Z, .8f)), 6f * scale, ImDrawFlags.None, 1.5f * scale);
      dl.AddCircleFilled(p + new Vector2(pad + 5f * scale, height / 2f), 4.5f * scale, ImGui.GetColorU32(color));
      dl.AddText(p + new Vector2(pad + 16f * scale, pad), ImGui.GetColorU32(color), text);

      if (hovered)
        DrawStatusTooltip(shownSummary: summary);

      ImGui.SetCursorScreenPos(new Vector2(p.X, p.Y + height + 6f * scale));
    }

    private void DrawStatusTooltip(string shownSummary)
    {
      ImGui.BeginTooltip();
      if (shownSummary.Length == 0)
      {
        ImGui.Text("No snap detected — the item stays where it is.");
      }
      else
      {
        if (Memory.LastFloorRaycastHit)
          ImGui.Text($"Surface below: {Memory.LastFloorRaycastDistance:0.###}");
        if (Memory.LastCeilingRaycastHit)
          ImGui.Text($"Floor above: {Memory.LastCeilingRaycastDistance:0.###}");
        if (Memory.ActiveItemSnapThreshold > 0f)
          ImGui.Text(Memory.ActiveItemThresholdIsOverride
            ? $"Per-item snap distance: {Memory.ActiveItemSnapThreshold:0.###} (authored exact)"
            : $"Per-item snap distance: {Memory.ActiveItemSnapThreshold:0.###} (+{Configuration.SnapThreshold:0.###} safety)");

        if (Configuration.ShowItemSnapWarning && Memory.LastItemSnapHit)
        {
          var itemName = string.IsNullOrEmpty(Memory.LastItemSnapTargetName)
            ? SnapRangeData.GetItemName(Memory.LastItemSnapTargetItemId)
            : Memory.LastItemSnapTargetName;
          ImGui.Text($"Item: {itemName} — {Memory.LastItemSnapDistance:0.###} away (range {Memory.LastItemSnapEffectiveRange:0.###})");
        }
        if (Configuration.ShowTableSnapWarning && Memory.LastTableSnapHit)
          ImGui.Text($"Table: {Memory.LastTableSnapTargetName} — surface Y {Memory.LastTableSnapSurfaceY:0.###}");
        if (Configuration.ShowWallSnapWarning && Memory.LastWallSnapHit)
        {
          ImGui.Text($"Wall: {Memory.LastWallSnapTargetName} — ray {Memory.LastWallSnapDistance:0.###} (capture {Memory.LastWallSnapThreshold:0.###})");
          ImGui.Text(Memory.LastWallSnapTargetIsTerritory
            ? "House wall: snaps on deselect."
            : "False wall: attaches when the housing interface closes.");
        }
        ImGui.TextDisabled("Snap applies when you exit the housing menu.");
      }
      ImGui.EndTooltip();
    }

    private void DrawPositionRow(string label, ref float value, ref float? locked, Vector4 color, bool warn = false)
    {
      var scale = ImGui.GetIO().FontGlobalScale;
      ImGui.TextColored(color, label);
      ImGui.SameLine(30f * scale);
      ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 34f * ImGui.GetIO().FontGlobalScale);

      if (warn)
        ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(.45f, .12f, .12f, 1f));
      var changed = ImGui.InputFloat($"##hu-{label}", ref value, Drag, 0f, "%.4f");
      if (warn)
        ImGui.PopStyleColor();

      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Mouse wheel adjusts by the drag amount.");
      var wheelMoved = false;
      if (ImGui.IsMouseHoveringRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax()))
      {
        var wheel = ImGui.GetIO().MouseWheel * Drag;
        if (wheel != 0f)
        {
          value += wheel;
          wheelMoved = true;
        }
      }
      if (changed || wheelMoved)
        Memory.WritePosition(Memory.Position);

      ImGui.SameLine();
      if (ImGuiComponents.IconButton((int)ImGui.GetID(label), locked == null ? FontAwesomeIcon.Unlock : FontAwesomeIcon.Lock))
        locked = locked == null ? value : null;
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip(locked == null ? "Lock this axis" : "Unlock this axis");
    }

    private void DrawRotationRow()
    {
      ImGui.TextColored(Gold, "RY");
      ImGui.SameLine(30f * ImGui.GetIO().FontGlobalScale);
      ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 34f * ImGui.GetIO().FontGlobalScale);
      if (ImGui.InputFloat("##hu-ry", ref Memory.Rotation.Y, Drag, 0f, "%.1f"))
        Memory.WriteRotation(Memory.Rotation);
      if (ImGui.IsItemHovered())
        ImGui.SetTooltip("Rotation in degrees.");
    }
  }
}
