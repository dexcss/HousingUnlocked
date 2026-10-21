using Dalamud.Interface.Windowing;

using Dalamud.Bindings.ImGui;

namespace HousingUnlocked.Interface.Windows
{
  public class DebugWindow : Window
  {
    private static PluginMemory Memory => Plugin.GetMemory();

    public DebugWindow() : base("Housing Unlocked Debug")
    {

    }

    public unsafe override void Draw()
    {
      ImGui.Text($"Gamepad Mode: {PluginMemory.GamepadMode}");
      ImGui.Text($"CanEditItem: {Memory.CanEditItem()}");
      ImGui.Text($"IsHousingOpen: {Memory.IsHousingOpen()}");
      ImGui.Separator();
      ImGui.Text($"LayoutWorld: {(ulong)Memory.Layout:X}");
      ImGui.Text($"Housing Structure: {(ulong)Memory.HousingStructure:X}");
      ImGui.Text($"Mode: {Memory.HousingStructure->Mode}");
      ImGui.Text($"State: {Memory.HousingStructure->State}");
      ImGui.Text($"State2: {Memory.HousingStructure->State2}");
      ImGui.Text($"Active: {(ulong)Memory.HousingStructure->ActiveItem:X}");
      ImGui.Text($"Hover: {(ulong)Memory.HousingStructure->HoverItem:X}");
      ImGui.Text($"Rotating: {Memory.HousingStructure->Rotating}");
      ImGui.Separator();
      ImGui.Text($"Housing Module: {(ulong)Memory.HousingModule:X}");
      ImGui.Text($"Current Territory: {(ulong)Memory.HousingModule->CurrentTerritory:X}");
      ImGui.Text($"Outdoor Territory: {(ulong)Memory.HousingModule->OutdoorTerritory:X}");
      ImGui.Text($"Indoor Territory: {(ulong)Memory.HousingModule->IndoorTerritory:X}");
      var active = Memory.HousingStructure->ActiveItem;
      if (active != null)
      {
        ImGui.Separator();
        var pos = Memory.HousingStructure->ActiveItem->Transform.Translation;
        ImGui.Text($"Position: {pos.X}, {pos.Y}, {pos.Z}");

        // Snap data introspection — useful when the warning misbehaves.
        var itemId = Memory.GetActiveItemFFXIVId();
        ImGui.Text($"FFXIV Item Id: {itemId}");
        if (itemId != 0)
        {
          var floorSnap = SnapData.HasFloorSnap(itemId);
          var wallSnap = SnapData.HasWallSnap(itemId);
          ImGui.Text($"FloorSnap tag: {floorSnap}");
          ImGui.Text($"WallSnap tag:  {wallSnap}");
          if (floorSnap)
          {
            var dist = Memory.GetDistanceToFloorBelow();
            var perItem = Memory.ActiveItemSnapThreshold;
            var safety = Plugin.GetConfiguration().SnapThreshold;
            var effective = perItem > 0f ? perItem + safety : safety;
            ImGui.Text($"Floor below — hit: {Memory.LastFloorRaycastHit}, distance: {(Memory.LastFloorRaycastHit ? Memory.LastFloorRaycastDistance.ToString("0.######") : "(no hit)")}");
            ImGui.Text($"Ceiling above — hit: {Memory.LastCeilingRaycastHit}, distance: {(Memory.LastCeilingRaycastHit ? Memory.LastCeilingRaycastDistance.ToString("0.######") : "(no hit)")}");
            ImGui.Text($"Closer surface (used for warning): {(Memory.LastFloorRaycastDistance <= Memory.LastCeilingRaycastDistance ? "floor below" : "ceiling above")}, distance {dist:0.######}");
            ImGui.Text($"Per-item snap threshold: {(perItem > 0f ? perItem.ToString("0.######") : "(unavailable)")}  +safety {safety:0.###}  = effective {effective:0.###}");
            ImGui.Text($"Threshold source: {Memory.SnapThresholdDiagnostic}");
            ImGui.Text($"In snap zone: {SnapData.IsInFloorSnapZone(itemId, dist, effective)}");

            // Raw field dump — looking for per-item snap data we don't yet understand.
            // Dumps undocumented fields and anything else that might correlate with the
            // engine's actual snap distance for this item. Drop this once we identify
            // which (if any) field we're after.
            ImGui.Separator();
            ImGui.Text("--- Raw fields (hunting for snap data) ---");
            Memory.DumpRawSnapHuntFields();
          }

          // Snap model: regular edge/2, SphereCastRange override, table snap.
          ImGui.Separator();
          ImGui.Text("--- Snap model (SphereCastRange / table surfaces) ---");
          var scrRuntime = PluginMemory.GetSphereCastRangeOverride(active);
          var hasTableRange = SnapRangeData.TryGetSnapRange(itemId, out var scrTable);
          ImGui.Text($"SphereCastRange (runtime): {(scrRuntime.HasValue ? scrRuntime.Value.ToString("0.####") : "(none)")}  [{Memory.ActiveItemSphereCastSource}]");
          ImGui.Text($"SphereCastRange (sgb table): {(hasTableRange ? scrTable.ToString("0.####") : "(none)")}");
          ImGui.Text($"Active snap threshold: {Memory.ActiveItemSnapThreshold:0.####}  [{Memory.SnapThresholdDiagnostic}]");
          ImGui.Text($"Item category: {Memory.ActiveItemCategory} (78=tabletop, 15=wall-mounted)");

          // Item-to-item snap detection.
          Memory.UpdateItemSnapDetection();
          ImGui.Text($"Item snap: {(Memory.LastItemSnapHit ? $"HIT {Memory.LastItemSnapTargetName} @ {Memory.LastItemSnapDistance:0.###} (range {Memory.LastItemSnapEffectiveRange:0.###})" : "no target in range")}");
          ImGui.Text($"  {Memory.ItemSnapDiagnostic}");

          // Nearby TableSurface receivers (table snap component).
          var tables = Memory.GetNearbyTableSurfaces();
          ImGui.Text($"Nearby TableTop receivers: {tables.Count}");
          foreach (var t in tables)
            ImGui.Text($"  {t.Name} — surface Y {t.BB.Max.Y:0.###} ({t.Distance:0.###} away)");

          // Table snap detection for the active item.
          Memory.UpdateTableSnapDetection();
          ImGui.Text($"Table snap: {(Memory.LastTableSnapHit ? $"HIT {Memory.LastTableSnapTargetName} surfaceY={Memory.LastTableSnapSurfaceY:0.###} \u0394Y={Memory.LastTableSnapDeltaY:0.###}" : "no surface in snap margin")}");
          ImGui.Text($"  {Memory.TableSnapDiagnostic}");

          // Wall snap detection (wall items vs false walls).
          Memory.UpdateWallSnapDetection();
          ImGui.Text($"Wall snap: {(Memory.LastWallSnapHit ? $"HIT {Memory.LastWallSnapTargetName} dist={Memory.LastWallSnapDistance:0.###} threshold={Memory.LastWallSnapThreshold:0.###}" : "no wall surface in snap range")}");
          ImGui.Text($"  {Memory.WallSnapDiagnostic}");
          var walls = Memory.GetNearbyWallSurfaces();
          ImGui.Text($"Nearby WallCollision receivers: {walls.Count}");
          foreach (var w in walls)
            ImGui.Text($"  {w.Name} — top Y {w.BB.Max.Y:0.###} ({w.Distance:0.###} away)");
        }
      }
    }
  }
}
