using Dalamud.Game.NativeWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using HousingUnlocked.Anywheres;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Node;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision.Math;
using CameraManager = FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager;
using SphereBounds = FFXIVClientStructs.FFXIV.Common.Math.SphereBounds;
using AxisAlignedBounds = FFXIVClientStructs.FFXIV.Common.Math.AxisAlignedBounds;
using OrientedBounds = FFXIVClientStructs.FFXIV.Common.Math.OrientedBounds;
using ColliderType = FFXIVClientStructs.FFXIV.Common.Component.BGCollision.ColliderType;

namespace HousingUnlocked
{
  public class PluginMemory
  {
    // Floor-snap distance is the item's raw shortest horizontal footprint / 2 —
    // no extra margin. Measured exactly on the White Rectangular Partition:
    // engine snaps at 0.1120, floats at 0.1130; its collision mesh shortest side
    // is 0.2248 -> /2 = 0.1124. The formula value is treated as exact.
    private const float FloorSnapMargin = 0f;

    private bool _isHousingOpen;

    // Layout and housing module pointers.
    private readonly IntPtr _layoutWorldPtr;
    private readonly IntPtr _housingModulePtr;

    public unsafe LayoutWorld* Layout => (LayoutWorld*)_layoutWorldPtr;
    public unsafe HousingStructure* HousingStructure => Layout->HousingStruct;
    public unsafe HousingModule* HousingModule => _housingModulePtr != IntPtr.Zero ? (HousingModule*)Marshal.ReadIntPtr(_housingModulePtr) : null;
    public static unsafe Camera* Camera => &CameraManager.Instance()->GetActiveCamera()->CameraBase.SceneCamera;

    private static AtkUnitBasePtr HousingLayout => Plugin.GameGui.GetAddonByName("HousingLayout");
    public static bool GamepadMode => !(HousingLayout != null && HousingLayout.IsVisible);

    // Local references to position and rotation to use to free them when an item isn't selected but to keep the UI bound to a reference.
    public Vector3 Position;
    public Vector3 Rotation;

    // Function for selecting an item, usually used when clicking on one in game.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SelectItemDelegate(IntPtr housingStruct, IntPtr item);
    public SelectItemDelegate SelectItem = null!;

    // [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    // public delegate void PlaceHousingItemDelegate(IntPtr item, Vector3 position);
    // private readonly IntPtr placeHousingItemAddress;
    // public PlaceHousingItemDelegate PlaceHousingItem = null!;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void HousingLayoutModelUpdateDelegate(IntPtr item);
    public HousingLayoutModelUpdateDelegate HousingLayoutModelUpdate = null!;

    public PluginMemory()
    {
      try
      {
        PlaceAnywhere.Initialize();
        ShowcaseAnywhere.Initialize();

        // Pointers for housing structures.
        _layoutWorldPtr = Plugin.TargetModuleScanner.GetStaticAddressFromSig("48 8B D1 48 8B 0D ?? ?? ?? ?? 48 85 C9 74 0A", 3);
        _housingModulePtr = Plugin.TargetModuleScanner.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 8B 52");

        // Read the pointers.
        _layoutWorldPtr = Marshal.ReadIntPtr(_layoutWorldPtr);

        // Select housing item.
        var selectItemAddress = Plugin.TargetModuleScanner.ScanText("48 85 D2 0F 84 ?? ?? ?? ?? 53 41 ?? 48 83 ?? ?? 48 89");
        SelectItem = Marshal.GetDelegateForFunctionPointer<SelectItemDelegate>(selectItemAddress);
        
        // Address for the place item function.
        // placeHousingItemAddress = Plugin.TargetModuleScanner.ScanText("40 53 48 83 EC 20 8B 02 48 8B D9 89 41 50 8B 42 04 89 41 54 8B 42 08 89 41 58 48 83 E9 80");
        // PlaceHousingItem = Marshal.GetDelegateForFunctionPointer<PlaceHousingItemDelegate>(placeHousingItemAddress);
        // Housing item model update.
        
        var housingLayoutModelUpdateAddress = Plugin.TargetModuleScanner.ScanText("48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 50 48 8B E9 48 8B 49");
        HousingLayoutModelUpdate = Marshal.GetDelegateForFunctionPointer<HousingLayoutModelUpdateDelegate>(housingLayoutModelUpdateAddress);

        var config = Plugin.GetConfiguration();

        if (config.PlaceAnywhere)
                    SetPlaceAnywhere(Plugin.GetConfiguration().PlaceAnywhere);
      }
      catch (Exception ex)
      {
        Plugin.Log.Error(ex, "Error while calling PluginMemory.ctor()");
      }
    }

    /// <summary>
    /// Dispose for the memory functions.
    /// </summary>
    public void Dispose()
    {
      try
      {
                // Disable the place anywhere in case it's on.
                SetPlaceAnywhere(false);
        AtkManager.ShowFurnishingList(true);
      }
      catch (Exception ex)
      {
        Plugin.Log.Error(ex, "Error while calling PluginMemory.Dispose()");
      }
    }

    /// <summary>
    /// Is the housing menu open.
    /// </summary>
    /// <returns>Boolean state.</returns>
    public unsafe bool IsHousingOpen()
    {
      try
      {
        if (HousingStructure == null)
          return false;

        // Anything other than none means the housing menu is open.
        return HousingStructure->Mode != HousingLayoutMode.None;
      }
      catch
      {
        return false;
      }
    }

    /// <summary>
    /// Checks if you can edit a housing item, specifically checks that rotate mode is active.
    /// </summary>
    /// <returns>Boolean state if housing menu is on or off.</returns>
    public unsafe bool CanEditItem()
    {
      try
      {
        if (HousingStructure == null)
          return false;

        // Rotate mode only.
        return HousingStructure->Mode == HousingLayoutMode.Rotate;
      }
      catch
      {
        return false;
      }
    }

    /// <summary>
    /// Read the position of the active item.
    /// </summary>
    /// <returns>Vector3 of the position.</returns>
    public unsafe Vector3 ReadPosition()
    {
      // Ensure that we're hooked and have the housing structure address.
      if (HousingStructure == null)
        throw new PluginException("Housing structure is invalid!");

      // Ensure active item pointer isn't null.
      var item = HousingStructure->ActiveItem;
      if (item == null)
        throw new PluginException("No valid item selected!");

      // Return the position vector.
      return item->Transform.Translation;
    }

    /// <summary>
    /// Reads the rotation of the item.
    /// </summary>
    /// <returns></returns>
    public unsafe Vector3 ReadRotation()
    {
      // Ensure that we're hooked and have the housing structure address.
      if (HousingStructure == null)
        throw new PluginException("Housing structure is invalid!");

      // Ensure active item pointer isn't null.
      var item = HousingStructure->ActiveItem;
      if (item == null)
        throw new PluginException("No valid item selected!");

      // Return the rotation radian.
      return Util.FromQ(item->Transform.Rotation);
    }

    // Cache for the active item's FFXIV Item ID. We re-resolve only when the
    // underlying ActiveItem pointer changes — saves iterating 600 GameObjects
    // every frame when the same item stays selected.
    private unsafe void* _cachedActiveItemPtr = null;
    private uint _cachedActiveItemId = 0;

    // Per-item snap threshold derived from the item's collision-mesh footprint.
    // Captured at selection-time alongside the item ID; expressed in game units.
    // 0 = not yet computed / unavailable.
    public float ActiveItemSnapThreshold { get; private set; } = 0f;

    /// <summary>
    /// True when ActiveItemSnapThreshold comes from authored engine data
    /// (SphereCastRange override or wall TargetMarker distance). Authored values
    /// are EXACT — the safety margin must not be added. False = formula-derived
    /// (approximate, margin applies).
    /// </summary>
    public bool ActiveItemThresholdIsOverride { get; private set; } = false;

    // Last table-snap detection results (tabletop item snapping onto a TableTop surface).
    public bool LastTableSnapHit { get; private set; } = false;
    public string LastTableSnapTargetName { get; private set; } = "";
    public float LastTableSnapDeltaY { get; private set; } = float.PositiveInfinity;
    public float LastTableSnapSurfaceY { get; private set; } = 0f;
    public string TableSnapDiagnostic { get; private set; } = "(not computed)";

    // Last wall-snap detection results (wall item snapping onto a wall surface,
    // including "false walls" like stage panels).
    public bool LastWallSnapHit { get; private set; } = false;
    public string LastWallSnapTargetName { get; private set; } = "";
    public float LastWallSnapDistance { get; private set; } = float.PositiveInfinity;
    public float LastWallSnapThreshold { get; private set; } = 0f;
    public string WallSnapDiagnostic { get; private set; } = "(not computed)";

    // True when the wall target is the house's own territory walls (snap applies
    // on deselect); false when it is a furniture false wall (attachment is
    // deferred to housing-interface exit / re-instance).
    public bool LastWallSnapTargetIsTerritory { get; private set; } = false;

    // Last raycast result for the active item (for diagnostics).
    public float LastFloorRaycastDistance { get; private set; } = float.PositiveInfinity;
    public bool LastFloorRaycastHit { get; private set; } = false;

    // Same for upward raycast — used to detect upward snaps to ceiling/floor-above.
    public float LastCeilingRaycastDistance { get; private set; } = float.PositiveInfinity;
    public bool LastCeilingRaycastHit { get; private set; } = false;

    // Diagnostic info for snap-threshold computation (visible in /Housing Unlocked Debug).
    public string SnapThresholdDiagnostic { get; private set; } = "(not computed)";

    // ==== Item-to-item snap detection (SphereCastRange overrides) ====

    // Where the active item's SphereCastRange came from (runtime instance read
    // vs embedded .sgb table vs none). Visible in /Housing Unlocked Debug.
    public string ActiveItemSphereCastSource { get; private set; } = "(none)";

    // Last item-to-item snap detection results (warning banner + debug panel).
    public bool LastItemSnapHit { get; private set; } = false;
    public float LastItemSnapDistance { get; private set; } = float.PositiveInfinity;
    public float LastItemSnapEffectiveRange { get; private set; } = 0f;
    public uint LastItemSnapTargetItemId { get; private set; } = 0;
    public string LastItemSnapTargetName { get; private set; } = "";
    public string ItemSnapDiagnostic { get; private set; } = "(not computed)";

    // Cache: HousingFurniture/HousingYardObject row ID -> (item ID, item name).
    // Filled on first sight of each placed furniture row; avoids per-frame
    // Excel RowRef dereferences during the item-snap scan.
    private readonly Dictionary<uint, (uint ItemId, string Name)> _furnitureRowCache = new();

    // Cache: Item RowId -> ItemUICategory row ID (78 = Tabletop, 79 = Wall-mounted,
    // per Re:MakePlace's shipped item handling). Used for table-snap detection.
    private readonly Dictionary<uint, uint> _itemUICategoryCache = new();

    // ==== Territory wall cache (the house's own walls — static geometry) ====
    // The interior/exterior layout is static once loaded: walk it once per zone,
    // cache every wall-flagged BgPart, then treat them like furniture receivers
    // with the same one-sided triangle check.
    private readonly List<(nint Instance, AABB WorldBB)> _territoryWallCache = new();
    private DateTime _territoryWallCacheBuilt = DateTime.MinValue;
    private uint _territoryWallCacheTerritory = 0;
    private bool _territoryWallCacheDeep;
    public string TerritoryWallCacheDiagnostic { get; private set; } = "(not built)";

    // Vertical movement direction of the active item: -1 pushing down, +1 pulling
    // up, 0 stationary. The engine snaps toward the direction of travel — pushing
    // a sunken item down warns about terrain below (rocks), pulling up warns about
    // the floor above — so warnings follow the direction instead of raw min().
    public int VerticalMoveDirection { get; private set; } = 0;
    private float _lastActiveY = float.NaN;

    // ==== Deselect / post-snap capture ====
    // The engine applies snaps when the item is DESELECTED, not while dragging.
    // On the ActiveItem -> null transition we log the last held position, then
    // watch the placed item's layout instance in the housing manager and log
    // where the engine moves it — the before/after delta is the actual snap vector.
    private bool _hadActiveItem;
    private nint _lastActiveGroup;
    private uint _lastActiveItemIdPost;
    private Vector3 _lastActiveGroupPos;
    private nint _pendingPostSnapGroup;
    private uint _pendingPostSnapItemId;
    private Vector3 _postSnapLast;
    private int _postSnapFrames;
    private int _postSnapStable;

    // Movement-since-selection: a freshly selected item that has not been moved is
    // already placed — its floor snap warning is noise (e.g. a stage panel sitting
    // on the floor). Any movement re-enables the warnings until reselection.
    private Vector3 _activeItemSelectPos;
    private nint _activeItemSelectGroup;

    /// <summary>True once the active item has been moved since it was selected.</summary>
    public bool ActiveItemMovedSinceSelect { get; private set; }

    // ==== Layout drift watch ====
    // Catches the "snaps when you re-instance" behavior: every 2 seconds the
    // positions of all placed items are compared against the previous snapshot;
    // any item that moved without being dragged is logged (before -> after).
    private readonly Dictionary<nint, Vector3> _lastLayoutSnapshot = new();
    private nint _lastTerritoryPtr;
    private DateTime _lastLayoutSnapTime = DateTime.MinValue;
    private bool _layoutSeeding;

    // HousingItemCategory of the cached active item (15 = wall-mounted, per the
    // HousingFurniture/HousingYardObject sheets).
    private uint _cachedActiveItemCategory = 0;
    private uint _cachedActiveItemModelKey = 0;
    private bool _cachedActiveItemIsOutdoor = false;

    /// <summary>HousingItemCategory of the active item (78=tabletop item, 15=wall-mounted item).</summary>
    public uint ActiveItemCategory => _cachedActiveItemCategory;

    /// <summary>
    /// Computes the floor-snap threshold for the currently selected item.
    /// </summary>
    /// <returns>The snap threshold in game units, or 0 if no item is selected
    /// or no usable collider data is available.</returns>
    /// <remarks>
    /// The FFXIV housing engine uses <c>min(footprintX, footprintZ) / 2</c> as the
    /// snap distance. The footprint source depends on collider type:
    ///
    /// <list type="bullet">
    /// <item><b>Mesh colliders</b> (most furniture): the footprint is the static
    /// mesh's tight bounding box, accessed via
    /// <c>ColliderMesh.Mesh-&gt;MeshPCB.RootNode.LocalBounds</c>. This is in mesh-
    /// local space; the divide-by-2 gives the snap distance directly.</item>
    /// <item><b>Box / Cylinder / Sphere / Plane colliders</b> (analytic shapes
    /// like the Company Chest): local bounds are unit (-1..1), so the world-space
    /// half-extent IS the <c>Scale</c> field at offset 0xD8. <c>min(scale.X,
    /// scale.Z)</c> directly gives the snap distance (no divide because the half-
    /// extent already represents "from center to edge").</item>
    /// </list>
    ///
    /// A housing item is a <see cref="SharedGroupLayoutInstance"/> (a group of
    /// child layout instances). We compute a per-child threshold from each child's
    /// collider, then take the minimum across children — the engine snaps when
    /// *any* child's footprint is close to a surface.
    ///
    /// Validated against measured snap distances:
    /// <list type="table">
    /// <item><term>Grassy Rug (Mesh)</term><description>computed 2.0, measured 2.06</description></item>
    /// <item><term>White Rect Partition (Mesh)</term><description>computed 0.113, measured 0.11</description></item>
    /// <item><term>Serving Barrel (Mesh)</term><description>computed 0.373, measured 0.36</description></item>
    /// <item><term>Company Chest (Box)</term><description>computed 0.491, measured 0.49</description></item>
    /// </list>
    /// </remarks>
    private unsafe float ComputeSnapThresholdForActiveItem()
    {
      if (HousingStructure == null) { SnapThresholdDiagnostic = "no housing structure"; return 0f; }
      var active = HousingStructure->ActiveItem;
      if (active == null) { SnapThresholdDiagnostic = "no active item"; return 0f; }

      // Floor snap distance:
      //  1. authored SphereCastRange (handled by the caller � exact)
      //  2. collider footprint / 2 (validated: Grassy Rug 2.0 vs 2.06)
      //  3. collider-less items (glass, bag, Log Pillar): the game uses a small
      //     fixed distance � measured on the Log Pillar: snapped at 0.179,
      //     floated at 0.244 (the visual-box formula does NOT apply to these).
      var instances = active->Instances.Instances;
      var size = (int)(instances.Last - instances.First);
      var totalChildren = size;

      float minThreshold = float.PositiveInfinity;
      int childrenWithThreshold = 0;
      string firstSource = "(none)";

      for (int i = 0; i < size; i++)
      {
        var childPtr = instances.First[i];
        if (childPtr.Value == null) continue;
        var childInstance = childPtr.Value->Instance;
        if (childInstance == null) continue;
        if (childInstance->Id.Type != InstanceType.BgPart) continue;

        var bgPart = (BgPartsLayoutInstance*)childInstance;
        var collider = bgPart->Collider;
        if (collider == null) continue;

        float childThreshold = 0f;
        string source = "";

        var colliderType = collider->GetColliderType();
        if (colliderType == ColliderType.Mesh)
        {
          var meshCol = (ColliderMesh*)collider;
          var mesh = meshCol->Mesh;
          if (mesh != null)
          {
            var meshPCB = (MeshPCB*)mesh;
            var rootNode = meshPCB->RootNode;
            if (rootNode != null)
            {
              var lb = rootNode->LocalBounds;
              // The collision mesh bounds are mesh-local — apply the child's scale.
              // The Log Pillar's mesh is scaled ~1.67 (0.222 read vs the engine's
              // measured 0.371); the White Rect is 1.0 (0.112 measured).
              var childScale = bgPart->GetTransformImpl()->Scale;
              var lbW = (lb.Max.X - lb.Min.X) * MathF.Abs(childScale.X);
              var lbD = (lb.Max.Z - lb.Min.Z) * MathF.Abs(childScale.Z);
              if (lbW > 0f && lbD > 0f)
              {
                childThreshold = System.Math.Min(lbW, lbD) * 0.5f + FloorSnapMargin;
                source = $"mesh.LocalBounds x{childScale.X:0.###}/{childScale.Z:0.###} (Y {lb.Min.Y:0.###}..{lb.Max.Y:0.###})";
              }
            }
          }
        }
        else if (colliderType == ColliderType.Box
              || colliderType == ColliderType.Cylinder
              || colliderType == ColliderType.Sphere
              || colliderType == ColliderType.Plane)
        {
          // Analytic shape: local bounds are unit (-1..1), Scale at offset 0xD8 is
          // the half-extent. min(scaleX, scaleZ) directly = snap distance.
          var scale = *(Vector3*)((byte*)collider + 0xD8);
          if (scale.X > 0f && scale.Z > 0f)
          {
            childThreshold = System.Math.Min(scale.X, scale.Z);
            source = $"{colliderType}.Scale";
          }
        }
        // Streamed colliders (terrain etc.) aren't applicable to placeable furniture.

        if (childThreshold > 0f)
        {
          if (childrenWithThreshold == 0) firstSource = source;
          if (childThreshold < minThreshold) minThreshold = childThreshold;
          childrenWithThreshold++;
        }
      }

      if (childrenWithThreshold > 0)
      {
        // The footprint formula is treated as exact (measured against the engine's
        // 0.112 boundary on the White Rect), so no global safety margin is added.
        ActiveItemThresholdIsOverride = true;
        SnapThresholdDiagnostic = $"{firstSource} -> {minThreshold:0.######} ({childrenWithThreshold}/{totalChildren} children)";
        return minThreshold;
      }

      // Collider-less items: fixed floor snap distance (measured on the Log Pillar).
      ActiveItemThresholdIsOverride = true;
      SnapThresholdDiagnostic = $"no usable colliders (children: {totalChildren}) -> default 0.2";
      return 0.2f;
    }

    /// <summary>
    /// Raw minimum horizontal footprint half-extent from the active item's colliders
    /// (mesh LocalBounds or analytic Scale), without any safety margin.
    /// </summary>
    /// <returns>The footprint half-extent, or 0 when the item has no usable colliders.</returns>
    private unsafe float ComputeActiveItemRawFootprintHalf()
    {
      if (HousingStructure == null || HousingStructure->ActiveItem == null) return 0f;
      var active = HousingStructure->ActiveItem;
      var instances = active->Instances.Instances;
      var size = (int)(instances.Last - instances.First);
      var best = float.PositiveInfinity;
      for (int i = 0; i < size; i++)
      {
        var childPtr = instances.First[i];
        if (childPtr.Value == null) continue;
        var childInstance = childPtr.Value->Instance;
        if (childInstance == null || childInstance->Id.Type != InstanceType.BgPart) continue;
        var bgPart = (BgPartsLayoutInstance*)childInstance;
        var collider = bgPart->Collider;
        if (collider == null) continue;
        var colliderType = collider->GetColliderType();
        if (colliderType == ColliderType.Mesh)
        {
          var mesh = ((ColliderMesh*)collider)->Mesh;
          if (mesh == null) continue;
          var rootNode = ((MeshPCB*)mesh)->RootNode;
          if (rootNode == null) continue;
          var lb = rootNode->LocalBounds;
          var childScale = bgPart->GetTransformImpl()->Scale;
          var lbW = (lb.Max.X - lb.Min.X) * MathF.Abs(childScale.X);
          var lbD = (lb.Max.Z - lb.Min.Z) * MathF.Abs(childScale.Z);
          if (lbW > 0f && lbD > 0f)
            best = MathF.Min(best, MathF.Min(lbW, lbD) * 0.5f);
        }
        else if (colliderType == ColliderType.Box
              || colliderType == ColliderType.Cylinder
              || colliderType == ColliderType.Sphere
              || colliderType == ColliderType.Plane)
        {
          var scale = *(Vector3*)((byte*)collider + 0xD8);
          if (scale.X > 0f && scale.Z > 0f)
            best = MathF.Min(best, MathF.Min(scale.X, scale.Z));
        }
      }
      return float.IsInfinity(best) ? 0f : best;
    }

    /// <summary>
    /// Casts a ray straight down from the active item's position and returns
    /// the distance to the nearest collision surface. Used by snap detection
    /// to know whether the item is close enough to the ground to be snapped.
    /// </summary>
    /// <returns>Distance in game units to the nearest surface below, or
    /// <see cref="float.PositiveInfinity"/> if no collision was found / no item is active.</returns>
    /// <remarks>
    /// The ray origin is lifted slightly above the item's pivot to avoid starting
    /// inside collision geometry (housing items often pivot at their base). The
    /// returned distance accounts for that lift, so it represents the gap from
    /// the item's actual base to the surface beneath it.
    /// </remarks>
    /// <summary>
    /// Floor-top material raycast across ALL collision layers. The Dalamud helper
    /// filters to layer 1 and misses placed floor props (gravel plots, decks) that
    /// live on another layer — outdoors the ray then reports the terrain far below
    /// instead of the walkway under the item.
    /// </summary>
    private static unsafe bool RaycastFloorTopAll(Vector3 origin, Vector3 direction, float maxDistance, out FFXIVClientStructs.FFXIV.Common.Component.BGCollision.RaycastHit hit)
    {
      hit = default;
      var framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
      var module = framework != null ? framework->BGCollisionModule : null;
      if (module == null) return false;
      var flags = stackalloc int[] { 0x4000, 0, 0x4000, 0 };
      var o = origin;
      var d = direction;
      var h = default(FFXIVClientStructs.FFXIV.Common.Component.BGCollision.RaycastHit);
      var ok = module->RaycastMaterialFilter(&h, &o, &d, maxDistance, -1, flags);
      hit = h;
      return ok;
    }

    public unsafe float GetDistanceToFloorBelow()
    {
      if (HousingStructure == null || HousingStructure->ActiveItem == null)
      {
        LastFloorRaycastHit = false;
        LastFloorRaycastDistance = float.PositiveInfinity;
        LastCeilingRaycastHit = false;
        LastCeilingRaycastDistance = float.PositiveInfinity;
        return float.PositiveInfinity;
      }

      var active = HousingStructure->ActiveItem;
      var pos = active->Transform.Translation;

      // Find lowest and highest Y across the item's child colliders (world space).
      // We need lowest for downward raycast (cast from just below the item) and
      // highest for upward-snap detection.
      float lowestY = float.PositiveInfinity;
      float highestY = float.NegativeInfinity;
      var instances = active->Instances.Instances;
      var size = (int)(instances.Last - instances.First);
      for (int i = 0; i < size; i++)
      {
        var childPtr = instances.First[i];
        if (childPtr.Value == null) continue;
        var ci = childPtr.Value->Instance;
        if (ci == null) continue;
        if (ci->Id.Type != InstanceType.BgPart) continue;
        var bgPart = (BgPartsLayoutInstance*)ci;
        var collider = bgPart->Collider;
        if (collider == null) continue;
        AABB worldBB = default;
        collider->GetWorldBB(&worldBB);
        if (worldBB.Min.Y < lowestY) lowestY = worldBB.Min.Y;
        if (worldBB.Max.Y > highestY) highestY = worldBB.Max.Y;
      }

      // Fall back to pivot if no world BB could be read.
      if (float.IsPositiveInfinity(lowestY)) lowestY = pos.Y;
      if (float.IsNegativeInfinity(highestY)) highestY = pos.Y;

      const float Epsilon = 0.001f;

      // === Downward ray: distance to floor below the item ===
      // Start 1 mm below the item's lowest point so the item's own collider cannot
      // block the ray, then also probe from just above it. A resting base can sit
      // *inside* the top surface, in which case the lower probe slips through and
      // hits a floor far below (the ward-connector floor that runs under the plot,
      // ~19 yalms down). Use the NEAREST surface across all probes, never the first.
      var downDir = new Vector3(0f, -1f, 0f);
      float floorGap = float.PositiveInfinity;
      var gotFloor = false;
      Span<float> downOffsets = stackalloc float[] { -Epsilon, 0.01f, 0.1f };
      for (var oi = 0; oi < downOffsets.Length; oi++)
      {
        var offset = downOffsets[oi];
        var downOrigin = new Vector3(pos.X, lowestY + offset, pos.Z);
        if (!RaycastFloorTopAll(downOrigin, downDir, 100f, out var downHit)) continue;
        var gap = downHit.Distance - offset + (pos.Y - lowestY);
        if (gap < 0f) gap = 0f;
        if (gap < floorGap) floorGap = gap;
        gotFloor = true;
      }
      if (gotFloor)
      {
        LastFloorRaycastHit = true;
        LastFloorRaycastDistance = floorGap;
      }
      else
      {
        LastFloorRaycastHit = false;
        LastFloorRaycastDistance = float.PositiveInfinity;
      }

      // === Upward direction: lookup-based ===
      // Upward floor surfaces have one-sided collision (only collide from above),
      // so an upward raycast passes through them. Instead, we use known floor-Y
      // values for housing interiors: basement -7, ground 0, upper +7.
      // For each known floor above the item's top, compute the distance.
      // Take the minimum across all candidate floors.
      //
      // This also used to treat a "tall item spans floor" case as distance 0, but
      // that misfired on ordinary floor items (Log Pillar moving over the ground
      // floor reported an upward snap from 1.4 yalms up). Only real floors above
      // the item's top count now.
      float ceilingGap = float.PositiveInfinity;
      bool foundUpwardSnap = false;

      // Only do the lookup when we're inside a housing interior (basement/ground/upper).
      // Outdoor plots and FC rooms don't have multiple floor levels in the same way.
      // We approximate this by checking if there are any known floors that could be
      // relevant given the item's position — i.e., any floor Y >= item bottom.
      if (HousingModule != null && HousingModule->IndoorTerritory != null)
      {
        // The three known indoor housing floor Y values, hardcoded based on
        // measurements across all housing sizes (FC rooms, small/medium/large).
        // Values are constant across housing types — only which subset is present
        // varies (FC rooms have only Y=0; small/medium have Y=-7 and 0; large
        // has all three). We test all three; floors that don't exist in the
        // current house simply won't be near the item.
        var knownFloors = stackalloc float[] { -7f, 0f, 7f };
        for (int f = 0; f < 3; f++)
        {
          var floorY = knownFloors[f];

          // Floor-above case: the engine snaps the item's base (pivot) onto the
          // upper floor, so the distance is measured from the PIVOT, not the top.
          // (Measured: the Log Pillar snapped onto the 2nd floor at y=7 with the
          // pivot within the per-item distance; a top-based gap missed it.)
          if (floorY > pos.Y)
          {
            var gap = floorY - pos.Y;
            if (gap < ceilingGap)
            {
              ceilingGap = gap;
              foundUpwardSnap = true;
            }
          }
          // Floor-below case is already handled by the downward raycast above.
        }
      }
      else if (HousingModule != null)
      {
        // Outdoor: there are no fixed floor levels, so find the floor-top surface
        // above the item by casting down from well above it. If that surface sits
        // above the pivot, the item is sunk into the terrain/deck and the engine
        // pulls it up onto the surface — same rule as the indoor floor-above case,
        // measured from the pivot. Hits inside the item's own vertical span (the
        // held preview collider, if it is in the scene at all) are ignored.
        var upOrigin = new Vector3(pos.X, pos.Y + 60f, pos.Z);
        if (RaycastFloorTopAll(upOrigin, new Vector3(0f, -1f, 0f), 200f, out var upHit))
        {
          var surfaceY = upOrigin.Y - upHit.Distance;
          var insideItem = surfaceY >= lowestY - 0.1f && surfaceY <= highestY + 0.1f;
          if (!insideItem && surfaceY > pos.Y + 0.02f)
          {
            ceilingGap = surfaceY - pos.Y;
            foundUpwardSnap = true;
          }
        }
      }

      LastCeilingRaycastHit = foundUpwardSnap;
      LastCeilingRaycastDistance = ceilingGap;

      // Return whichever surface is closer — engine snaps to whichever is nearest.
      return System.Math.Min(floorGap, ceilingGap);
    }

    /// <summary>
    /// Walks a housing item's child layout instances looking for a SphereCastRange
    /// (LayerEntryType 0x4B) child and returns Cast.X — the engine's authored snap
    /// distance override in yalms. Validated against Housingway's implementation
    /// and 106 extracted .sgb blocks. Returns null when the item has no such child
    /// or the instance children aren't loaded yet.
    /// </summary>
    public static unsafe float? GetSphereCastRangeOverride(SharedGroupLayoutInstance* group)
    {
      if (group == null) return null;
      var instances = group->Instances.Instances;
      var size = (int)(instances.Last - instances.First);
      for (int i = 0; i < size; i++)
      {
        var childPtr = instances.First[i];
        if (childPtr.Value == null) continue;
        var ci = childPtr.Value->Instance;
        if (ci == null) continue;
        if (ci->Id.Type != InstanceType.SphereCastRange) continue;

        var scr = (SphereCastRangeData*)ci;
        var x = scr->Cast.X;
        // Reject absurd values (zeroed/unloaded instances); real overrides are 0.001..2.7.
        if (float.IsFinite(x) && x > 0f && x < 100f) return x;
        return null;
      }
      return null;
    }

    /// <summary>
    /// Scans placed housing objects for the nearest item that can receive an
    /// item-to-item snap. Both sides' distances come from SphereCastRange
    /// overrides (runtime read, embedded-table fallback); items without one
    /// don't participate. Result is exposed for the warning banner + debug panel.
    /// </summary>
    public unsafe void UpdateItemSnapDetection()
    {
      LastItemSnapHit = false;
      LastItemSnapDistance = float.PositiveInfinity;
      LastItemSnapEffectiveRange = 0f;
      LastItemSnapTargetItemId = 0;
      LastItemSnapTargetName = "";

      if (HousingStructure == null || HousingStructure->ActiveItem == null ||
          HousingModule == null || HousingModule->GetCurrentManager() == null ||
          HousingModule->GetCurrentManager()->Objects == null)
      {
        ItemSnapDiagnostic = "no housing/active item";
        return;
      }

      var active = HousingStructure->ActiveItem;
      var activePos = active->Transform.Translation;
      var activeItemId = GetActiveItemFFXIVId();

      // Active item's own range (runtime first, table fallback).
      float activeRange = 0f;
      var activeRuntime = GetSphereCastRangeOverride(active);
      if (activeRuntime.HasValue)
      {
        activeRange = activeRuntime.Value;
        ActiveItemSphereCastSource = "runtime";
      }
      else if (activeItemId != 0 && SnapRangeData.TryGetSnapRange(activeItemId, out var atr))
      {
        activeRange = atr;
        ActiveItemSphereCastSource = "sgb table";
      }
      else
      {
        ActiveItemSphereCastSource = "(none)";
      }

      var config = Plugin.GetConfiguration();
      var safety = config.SnapThreshold;
      var mode = config.ItemSnapRangeMode;

      float bestDist = float.PositiveInfinity;
      float bestEffective = 0f;
      uint bestId = 0;
      string bestName = "";
      int scanned = 0;
      int withRange = 0;

      for (var i = 0; i < 600; i++)
      {
        var addr = HousingModule->GetCurrentManager()->Objects[i];
        if (addr == 0) continue;
        var go = (GameObject*)addr;
        if (go->SharedGroupLayoutInstance == active) continue; // skip the dragged item itself

        var dist = Vector3.Distance(activePos, go->Position);
        if (dist > 8f) continue; // max known range 2.7 (+sum mode) — cheap early-out

        scanned++;

        // Resolve this placed item's identity (cached per furniture row).
        var rowId = go->BaseId;
        if (!_furnitureRowCache.TryGetValue(rowId, out var cached))
        {
          uint itemId = 0;
          string name = "";
          if (Plugin.TryGetFurnishing(rowId, out var f))
          {
            itemId = f.Item.Value.RowId;
            name = f.Item.Value.Name.ExtractText();
          }
          else if (Plugin.TryGetYardObject(rowId, out var y))
          {
            itemId = y.Item.Value.RowId;
            name = y.Item.Value.Name.ExtractText();
          }
          cached = (itemId, name);
          _furnitureRowCache[rowId] = cached;
        }

        if (cached.ItemId == 0) continue;

        // Placed item's range: runtime SphereCastRange first, table fallback.
        float placedRange = 0f;
        var placedRuntime = GetSphereCastRangeOverride(go->SharedGroupLayoutInstance);
        if (placedRuntime.HasValue) placedRange = placedRuntime.Value;
        else if (SnapRangeData.TryGetSnapRange(cached.ItemId, out var ptr2)) placedRange = ptr2;

        if (placedRange <= 0f && activeRange <= 0f) continue;
        withRange++;

        var effective = mode switch
        {
          ItemSnapRangeMode.Sum => activeRange + placedRange,
          ItemSnapRangeMode.PlacedOnly => placedRange,
          _ => System.Math.Max(activeRange, placedRange),
        };
        if (effective <= 0f) continue;

        if (dist <= effective + safety && dist < bestDist)
        {
          bestDist = dist;
          bestEffective = effective;
          bestId = cached.ItemId;
          bestName = cached.Name;
        }
      }

      ItemSnapDiagnostic = $"scanned {scanned} nearby ({withRange} snap-capable), active range {activeRange:0.####} [{ActiveItemSphereCastSource}], mode {mode}";

      if (bestId != 0)
      {
        LastItemSnapHit = true;
        LastItemSnapDistance = bestDist;
        LastItemSnapEffectiveRange = bestEffective;
        LastItemSnapTargetItemId = bestId;
        LastItemSnapTargetName = bestName;
      }
    }

    /// <summary>
    /// Wall-mounted items snap to walls using their TargetMarker children: the
    /// engine's wall snap distance is (max XZ distance from the item pivot to a
    /// TargetMarker) x 0.5, matching Housingway's shipped implementation for
    /// HousingItemCategory == 15 items. Returns null when no TargetMarker exists.
    /// </summary>
    public static unsafe float? GetWallTargetMarkerDistance(SharedGroupLayoutInstance* group, Vector3 pos)
    {
      if (group == null) return null;
      var instances = group->Instances.Instances;
      var size = (int)(instances.Last - instances.First);

      float max = float.MinValue;
      var found = false;
      for (int i = 0; i < size; i++)
      {
        var childPtr = instances.First[i];
        if (childPtr.Value == null) continue;
        var ci = childPtr.Value->Instance;
        if (ci == null) continue;
        if (ci->Id.Type != InstanceType.TargetMarker) continue;

        found = true;
        var transform = *ci->GetTransformImpl();
        var dx = pos.X - transform.Translation.X;
        var dz = pos.Z - transform.Translation.Z;
        var dist = MathF.Sqrt(dx * dx + dz * dz);
        if (dist > max) max = dist;
      }

      if (!found) return null;
      return MathF.Abs(MathF.Round(max, 2)) * 0.5f;
    }

    /// <summary>
    /// Walks a MeshPCB node tree looking for any per-primitive material carrying
    /// the given nibble flag. Mirrors Housingway's GetAllMaterialValue primitive
    /// OR walk. Nibbles per the Re:MakePlace dev's material table:
    /// shift 16 (d4): 1=FloorTop, 8=TableTop, 9=RugTop; shift 20 (d3): 1=WallCollisionMesh.
    /// </summary>
    private static unsafe bool MeshHasMaterialPrim(MeshPCB.FileNode* node, int shift, int value, int depth)
    {
      while (node != null && depth < 64)
      {
        var prims = node->PrimitivesPtr;
        for (int i = 0; i < node->NumPrims; i++)
        {
          if (((prims[i].Material >> shift) & 0xF) == (ulong)value) return true;
        }
        if (node->Child1 != null && MeshHasMaterialPrim(node->Child1, shift, value, depth + 1)) return true;
        node = node->Child2;
        depth++;
      }
      return false;
    }

    /// <summary>True if the item's ItemUICategory is 78 (Tabletop).</summary>
    private bool IsTabletopItem(uint itemId)
    {
      if (_itemUICategoryCache.TryGetValue(itemId, out var cat)) return cat == 78;
      uint resolved = 0;
      try
      {
        var row = Plugin.Data.GetExcelSheet<Lumina.Excel.Sheets.Item>().GetRowOrDefault(itemId);
        if (row.HasValue) resolved = row.Value.ItemUICategory.Value.RowId;
      }
      catch
      {
        // Sheet unavailable — treat as not tabletop.
      }
      _itemUICategoryCache[itemId] = resolved;
      return resolved == 78;
    }

    /// <summary>
    /// Table snap: the engine attaches tabletop items (ItemUICategory 78) onto
    /// TableTop-flagged collision meshes of nearby placed furniture. Fires when
    /// the active item is a tabletop item and a nearby receiver's surface sits
    /// within the snap margin of the item's pivot.
    /// </summary>
    public unsafe void UpdateTableSnapDetection()
    {
      LastTableSnapHit = false;
      LastTableSnapTargetName = "";
      LastTableSnapDeltaY = float.PositiveInfinity;
      LastTableSnapSurfaceY = 0f;

      if (HousingStructure == null || HousingStructure->ActiveItem == null ||
          HousingModule == null || HousingModule->GetCurrentManager() == null ||
          HousingModule->GetCurrentManager()->Objects == null)
      {
        TableSnapDiagnostic = "no housing/active item";
        return;
      }

      var active = HousingStructure->ActiveItem;
      var pos = active->Transform.Translation;
      var activeItemId = GetActiveItemFFXIVId();

      if (activeItemId == 0 || !IsTabletopItem(activeItemId))
      {
        TableSnapDiagnostic = $"active item not tabletop (category {_cachedActiveItemCategory})";
        return;
      }

      // The engine's visual AABB is the best proxy for the item's body: available
      // without colliders, and tighter than the bounding sphere (which can be
      // inflated by non-visual children). Fall back to the sphere, then colliders.
      Vector3 bbMin, bbMax;
      var sourceNote = "";
      if (GetActiveItemWorldBounds() is { } bounds)
      {
        bbMin = bounds.Min;
        bbMax = bounds.Max;
        sourceNote = "visual AABB";
      }
      else if (GetActiveItemSphereBounds(out var sphereCenter, out var sphereRadius))
      {
        bbMin = sphereCenter - new Vector3(sphereRadius);
        bbMax = sphereCenter + new Vector3(sphereRadius);
        sourceNote = $"sphere r {sphereRadius:0.###}";
      }
      else
      {
        TableSnapDiagnostic = "active item bounds unavailable";
        return;
      }

      // The engine allows a small gap above the tabletop, scaled by the item's
      // footprint, with a minimum reach for small items. Boundary tests (drag step
      // 0.001): the engine's snap edge sits ~3 clicks above the previous green edge.
      //   glass   min XZ 0.097 -> snapped at 0.041/0.050 -> floor ~0.055
      //   bag     min XZ 0.196 -> snaps at 0.079, floats at 0.085
      //   basket  min XZ 0.933 -> snaps at 0.350, floats at 0.439
      // Tabletop reach calibrated on the Used Banquet Table (glass/bag/basket):
      // Tabletop reach calibrated on the Used Banquet Table (glass/bag/basket
      // boundary tests): the engine allows a gap above the surface of
      //   0.74 × the item's floor snap distance (min horizontal half-extent of
      //   the visual box) + 8 mm. Fits: glass snap 0.041/float 0.05,
      //   bag snap 0.080/float 0.085, basket snap 0.350/float 0.36.
      // Tabletop reach calibrated on the Used Banquet Table (glass/bag/basket
      // boundary tests): the engine allows a gap above the surface of
      //   0.74 x the item's min horizontal half-extent (oriented bounds) + 8 mm.
      // Fits: glass snap 0.041/float 0.05, bag snap 0.080/float 0.085,
      // basket snap 0.350/float 0.36.
      var ob = default(OrientedBounds);
      active->GetOrientedBoundsImpl(&ob);
      var floorReach = System.Math.Min(ob.HalfExtents.X, ob.HalfExtents.Z);

      // Tabletop reach follows the same box formula as the floor (Fox's rule):
      // the game's model bounding box, min(X, Z) / 2 — verified against the engine
      // (grocery bag 0.0815 vs measured 0.080/0.085; wine glass 0.0471 vs 0.041/0.050).
      float reach;
      string reachSrc;
      if (ActiveItemSphereCastSource is "runtime" or "sgb table")
      {
        reach = ActiveItemSnapThreshold;
        reachSrc = "scr";
      }
      else
      {
        var modelFloor = ModelFloorData.Get(_cachedActiveItemModelKey, _cachedActiveItemIsOutdoor);
        if (modelFloor > 0f)
        {
          reach = modelFloor;
          reachSrc = "model box";
        }
        else
        {
          var rawFootprint = ComputeActiveItemRawFootprintHalf();
          if (rawFootprint > 0f)
          {
            reach = rawFootprint + 0.02f;
            reachSrc = "mesh footprint";
          }
          else
          {
            reach = floorReach * 0.74f + 0.008f + 0.02f;
            reachSrc = "visual 0.74";
          }
        }
      }

      var candidates = GetNearbyTableSurfaces();
      float best = float.PositiveInfinity;
      string bestName = "";
      float bestSurfaceY = 0f;
      var nearestGap = float.PositiveInfinity;
      foreach (var t in candidates)
      {
        var surfaceY = t.BB.Max.Y;

        // The item must be horizontally over the table…
        var overX = bbMin.X <= t.BB.Max.X && bbMax.X >= t.BB.Min.X;
        var overZ = bbMin.Z <= t.BB.Max.Z && bbMax.Z >= t.BB.Min.Z;
        if (!overX || !overZ) continue;

        // Base height above the tabletop (positive = floating above it).
        var above = bbMin.Y - surfaceY;
        if (above < nearestGap)
          nearestGap = above;

        // Entirely below the tabletop → not a tabletop snap.
        if (bbMax.Y < surfaceY) continue;

        // Too high above the surface for this item's reach → no snap.
        if (above > reach) continue;

        var dy = System.Math.Abs(surfaceY - pos.Y);
        if (dy < best)
        {
          best = dy;
          bestName = t.Name;
          bestSurfaceY = surfaceY;
        }
      }

      TableSnapDiagnostic = $"{candidates.Count} TableTop receiver(s), item Y [{bbMin.Y:0.###}..{bbMax.Y:0.###}] " +
        $"XZ {bbMax.X - bbMin.X:0.###}x{bbMax.Z - bbMin.Z:0.###}, gap {(float.IsInfinity(nearestGap) ? float.NaN : nearestGap):0.###}, reach {reach:0.###} ({reachSrc}){(sourceNote.Length > 0 ? ", " + sourceNote : "")}";

      if (best != float.PositiveInfinity)
      {
        LastTableSnapHit = true;
        LastTableSnapTargetName = bestName;
        LastTableSnapDeltaY = best;
        LastTableSnapSurfaceY = bestSurfaceY;
      }
    }

    /// <summary>
    /// Scans placed items near the active item for colliders whose material value
    /// carries the given nibble flag — object-level first, then per-primitive for
    /// mesh colliders. When nothing is flagged at runtime but <paramref name="embeddedFallback"/>
    /// says the item is a known receiver (from the offline collision census), the
    /// union of the item's collider AABBs is used instead. Returns each receiver's
    /// name, world AABB and pivot distance.
    /// </summary>
    public unsafe List<(string Name, AABB BB, float Distance)> GetNearbySurfaces(int matShift, int matValue, float maxDistance, Func<uint, bool>? embeddedFallback = null)
    {
      var results = new List<(string, AABB, float)>();

      if (HousingStructure == null || HousingStructure->ActiveItem == null ||
          HousingModule == null || HousingModule->GetCurrentManager() == null ||
          HousingModule->GetCurrentManager()->Objects == null)
        return results;

      var active = HousingStructure->ActiveItem;
      var activePos = active->Transform.Translation;

      for (var i = 0; i < 600; i++)
      {
        var addr = HousingModule->GetCurrentManager()->Objects[i];
        if (addr == 0) continue;
        var go = (GameObject*)addr;
        if (go->SharedGroupLayoutInstance == active) continue;

        var dist = Vector3.Distance(activePos, go->Position);
        if (dist > maxDistance) continue;

        var rowId = go->BaseId;
        if (!_furnitureRowCache.TryGetValue(rowId, out var cached))
        {
          uint itemId = 0;
          string name = "";
          if (Plugin.TryGetFurnishing(rowId, out var f))
          {
            itemId = f.Item.Value.RowId;
            name = f.Item.Value.Name.ExtractText();
          }
          else if (Plugin.TryGetYardObject(rowId, out var y))
          {
            itemId = y.Item.Value.RowId;
            name = y.Item.Value.Name.ExtractText();
          }
          cached = (itemId, name);
          _furnitureRowCache[rowId] = cached;
        }
        if (cached.ItemId == 0) continue;

        // Walk the placed item's children for a flagged collider.
        var group = go->SharedGroupLayoutInstance;
        if (group == null) continue;
        var instances = group->Instances.Instances;
        var size = (int)(instances.Last - instances.First);

        AABB bestBB = default;
        var found = false;
        for (int c = 0; c < size; c++)
        {
          var childPtr = instances.First[c];
          if (childPtr.Value == null) continue;
          var ci = childPtr.Value->Instance;
          if (ci == null) continue;
          if (ci->Id.Type != InstanceType.BgPart) continue;

          var bgPart = (BgPartsLayoutInstance*)ci;
          var collider = bgPart->Collider;
          if (collider == null) continue;
          if (collider->GetColliderType() != ColliderType.Mesh) continue;

          var nibble = (int)((bgPart->CollisionMaterialIdLow >> matShift) & 0xF);
          var flagged = nibble == matValue;
          if (!flagged)
          {
            var meshCol = (ColliderMesh*)collider;
            if (!meshCol->MeshIsSimple && meshCol->Mesh != null)
            {
              var pcb = (MeshPCB*)meshCol->Mesh;
              if (pcb->RootNode != null)
                flagged = MeshHasMaterialPrim(pcb->RootNode, matShift, matValue, 0);
            }
          }
          if (!flagged) continue;

          AABB worldBB = default;
          collider->GetWorldBB(&worldBB);
          if (!found || worldBB.Max.Y > bestBB.Max.Y) bestBB = worldBB;
          found = true;
        }

        // Embedded-census fallback: item is a known receiver but no flagged
        // collider was read at runtime — use the union of its collider AABBs.
        if (!found && embeddedFallback != null && embeddedFallback(cached.ItemId))
        {
          AABB union = default;
          var any = false;
          for (int c = 0; c < size; c++)
          {
            var childPtr = instances.First[c];
            if (childPtr.Value == null) continue;
            var ci = childPtr.Value->Instance;
            if (ci == null) continue;
            if (ci->Id.Type != InstanceType.BgPart) continue;
            var bgPart = (BgPartsLayoutInstance*)ci;
            if (bgPart->Collider == null) continue;
            AABB bb = default;
            bgPart->Collider->GetWorldBB(&bb);
            if (!any)
            {
              union = bb;
              any = true;
            }
            else
            {
              if (bb.Min.X < union.Min.X) union.Min.X = bb.Min.X;
              if (bb.Min.Y < union.Min.Y) union.Min.Y = bb.Min.Y;
              if (bb.Min.Z < union.Min.Z) union.Min.Z = bb.Min.Z;
              if (bb.Max.X > union.Max.X) union.Max.X = bb.Max.X;
              if (bb.Max.Y > union.Max.Y) union.Max.Y = bb.Max.Y;
              if (bb.Max.Z > union.Max.Z) union.Max.Z = bb.Max.Z;
            }
          }
          if (any)
          {
            bestBB = union;
            found = true;
          }
        }

        if (found)
          results.Add((cached.Name, bestBB, dist));
      }

      return results;
    }

    /// <summary>TableTop receivers (nibble d4 == 8) — surfaces tabletop items snap onto.</summary>
    public unsafe List<(string Name, AABB BB, float Distance)> GetNearbyTableSurfaces(float maxDistance = 4f)
      => GetNearbySurfaces(16, 8, maxDistance, id => SnapRangeData.IsTableReceiver(id));

    /// <summary>
    /// WallCollisionMesh receivers (nibble d3 == 1) — wall surfaces wall-mounted
    /// items snap onto, including "false walls" (stage panels, partitions).
    /// </summary>
    public unsafe List<(string Name, AABB BB, float Distance)> GetNearbyWallSurfaces(float maxDistance = 6f)
      => GetNearbySurfaces(20, 1, maxDistance, id => SnapRangeData.IsWallReceiver(id));

    /// <summary>Invalidates the territory wall cache so the next wall check rebuilds it.</summary>
    public void InvalidateTerritoryWallCache()
    {
      _territoryWallCacheBuilt = DateTime.MinValue;
      _territoryWallCacheDeep = false;
      _territoryWallCache.Clear();
      TerritoryWallCacheDiagnostic = "(invalidated)";
    }

    /// <summary>
    /// Builds (or refreshes) the territory wall cache: walks the active layout's
    /// layers and caches every BgPart whose collider carries WallCollisionMesh.
    /// Shallow pass uses object-level flags only; a deep pass adds per-primitive
    /// walks and runs once if the shallow pass found nothing. House geometry is
    /// static, so the cache is only rebuilt on territory change or age.
    /// </summary>
    private unsafe void EnsureTerritoryWallCache(bool allowDeep)
    {
      var lm = Layout != null ? *(LayoutManager**)((byte*)Layout + 0x20) : null; // LayoutWorld.ActiveLayout (0x020)
      var tid = lm != null ? lm->TerritoryTypeId : 0;
      var age = (DateTime.Now - _territoryWallCacheBuilt).TotalMinutes;

      if (_territoryWallCacheBuilt != DateTime.MinValue &&
          tid == _territoryWallCacheTerritory && age < 10 &&
          (!allowDeep || _territoryWallCacheDeep))
        return;

      _territoryWallCache.Clear();
      _territoryWallCacheTerritory = tid;
      _territoryWallCacheBuilt = DateTime.Now;

      int scanned = 0, walls = 0;
      var wallBBs = new System.Text.StringBuilder();
      if (lm == null || lm->InitState != 7)
      {
        // Do not cache the failure for 10 minutes: retry on the next check.
        _territoryWallCacheBuilt = DateTime.MinValue;
        _territoryWallCacheDeep = false;
        TerritoryWallCacheDiagnostic = "layout not ready";
        return;
      }

      foreach (var layerPtr in lm->Layers.Values)
      {
        var layer = layerPtr.Value;
        if (layer == null) continue;
        foreach (var instPtr in layer->Instances.Values)
        {
          var inst = instPtr.Value;
          if (inst == null) continue;
          if (inst->Id.Type != InstanceType.BgPart) continue;
          scanned++;
          var bgPart = (BgPartsLayoutInstance*)inst;
          var collider = bgPart->Collider;
          if (collider == null) continue;
          if (collider->GetColliderType() != ColliderType.Mesh) continue;

          // object-level flag fast path; deep pass adds per-primitive walks
          var isWall = ((bgPart->CollisionMaterialIdLow >> 20) & 0xF) == 1;
          if (!isWall && allowDeep)
          {
            var meshCol = (ColliderMesh*)collider;
            if (!meshCol->MeshIsSimple && meshCol->Mesh != null)
            {
              var pcb = (MeshPCB*)meshCol->Mesh;
              if (pcb->RootNode != null) isWall = MeshHasMaterialPrim(pcb->RootNode, 20, 1, 0);
            }
          }
          if (!isWall) continue;

          AABB bb = default;
          collider->GetWorldBB(&bb);
          _territoryWallCache.Add(((nint)bgPart, bb));
          wallBBs.Append($"[{bb.Min.X:0.##},{bb.Min.Y:0.##},{bb.Min.Z:0.##}]-[{bb.Max.X:0.##},{bb.Max.Y:0.##},{bb.Max.Z:0.##}] ");
          walls++;
        }
      }
      _territoryWallCacheDeep = allowDeep;
      TerritoryWallCacheDiagnostic = $"{scanned} bgparts scanned, {walls} wall surfaces cached{(allowDeep ? " (deep)" : "")} at {DateTime.Now:HH:mm:ss} {wallBBs}";
      SnapLogger.Event("wallcache", TerritoryWallCacheDiagnostic);
    }

    /// <summary>
    /// Wall snap: wall-mounted items (HousingItemCategory 15) snap onto wall
    /// collision surfaces — including "false walls" (Stage Panels, partitions,
    /// wardrobes) whose meshes carry the WallCollisionMesh material flag
    /// (nibble d3 == 1, bits 20..23). House walls have ONE attachable face, but the
    /// snap is distance-based: an item slightly BEHIND the face still snaps forward
    /// onto it (measured: lantern at Z=-12.04 snapped forward to the Z=-12 wall, and
    /// kept attaching up to 1.0 behind, Z=-13). Furniture false walls (stage panels)
    /// accept on their back side only — turning the panel around breaks the attach.
    /// Fires when the item's pivot or any of its TargetMarker points comes within
    /// the flat 1.0 capture of a wall triangle.
    /// </summary>
    public unsafe void UpdateWallSnapDetection()
    {
      LastWallSnapHit = false;
      LastWallSnapTargetName = "";
      LastWallSnapDistance = float.PositiveInfinity;
      LastWallSnapThreshold = 0f;
      LastWallSnapTargetIsTerritory = false;

      if (HousingStructure == null || HousingStructure->ActiveItem == null ||
          HousingModule == null || HousingModule->GetCurrentManager() == null ||
          HousingModule->GetCurrentManager()->Objects == null)
      {
        WallSnapDiagnostic = "no housing/active item";
        return;
      }

      var active = HousingStructure->ActiveItem;
      var activeItemId = GetActiveItemFFXIVId();

      if (_cachedActiveItemCategory != 15)
      {
        WallSnapDiagnostic = $"active item not wall-mounted (category {_cachedActiveItemCategory})";
        return;
      }

      // Wall snap capture: 1.0 yalm flat for ALL wall-mounted items and ALL wall
      // surfaces — MEASURED IN-GAME on the X=-20 house wall: the skylight, indoor
      // awning, mounted box shelf, crescent wall rack and wall lantern ALL snap at
      // 0.99 and not at 1.00. Fox's item list carries no per-item wall-snap data
      // (365/369 wall-mounted items have no override), confirming a flat rule.
      var config = Plugin.GetConfiguration();
      var capture = config.WallCaptureDistance;
      var limit = capture;
      var territoryLimit = capture;
      LastWallSnapThreshold = capture;
      // The item's world front (+Z local). Wall items only attach when their front
      // points the same way as the wall surface � flipping the item 180� (front
      // toward the wall) kills the snap for house walls and false walls alike.
      var itemFront = Vector3.Transform(Vector3.UnitZ, active->Transform.Rotation);

      // Probe points: the item pivot (MakePlace traces from the actor position).
      var pos = active->Transform.Translation;
      var probes = new List<Vector3> { pos };

      // One-sided wall surface scan: walk placed receivers' wall-flagged collision
      // triangles in world space and measure each probe against the FRONT side only.
      // 32 of the game's 38 wall receivers are strictly one-sided (verified offline
      // from their collision meshes) — the back of a stage panel accepts nothing.
      float bestFurn = float.PositiveInfinity;
      string bestFurnName = "";
      float bestTerr = float.PositiveInfinity;
      string bestTerrName = "";
      int wallTris = 0;
      var usedAabbFallback = false;
      AABB? bestFurnBB = null;
      var bestFurnBBName = "";
      // Per-class bests: multi-face receivers outrank single-face false walls
      // (measured: lantern between a Stage Panel and a White Rect snaps to the
      // White Rect), which outrank house walls (handled via terrHit below).
      float bestMulti = float.PositiveInfinity;
      string bestMultiName = "";
      AABB? bestMultiBB = null;
      float bestSingle = float.PositiveInfinity;
      string bestSingleName = "";
      AABB? bestSingleBB = null;
      var furnClass = "";

      for (var i = 0; i < 600; i++)
      {
        var addr = HousingModule->GetCurrentManager()->Objects[i];
        if (addr == 0) continue;
        var go = (GameObject*)addr;
        if (go->SharedGroupLayoutInstance == active) continue;
        if (Vector3.Distance(pos, go->Position) > 8f) continue;

        var rowId = go->BaseId;
        if (!_furnitureRowCache.TryGetValue(rowId, out var cached))
        {
          uint itemId = 0;
          string name = "";
          if (Plugin.TryGetFurnishing(rowId, out var f))
          {
            itemId = f.Item.Value.RowId;
            name = f.Item.Value.Name.ExtractText();
          }
          else if (Plugin.TryGetYardObject(rowId, out var y))
          {
            itemId = y.Item.Value.RowId;
            name = y.Item.Value.Name.ExtractText();
          }
          cached = (itemId, name);
          _furnitureRowCache[rowId] = cached;
        }
        if (cached.ItemId == 0) continue;

        var group = go->SharedGroupLayoutInstance;
        if (group == null) continue;

        var isMultiSided = SnapRangeData.IsMultiSidedWallReceiver(cached.ItemId);

        var itemInstances = group->Instances.Instances;
        var itemSize = (int)(itemInstances.Last - itemInstances.First);
        var hadWallTris = false;
        var beforeItemBest = float.PositiveInfinity;
        var itemBest = float.PositiveInfinity;
        var itemName = "";
        AABB? itemBB = null;

        for (int c = 0; c < itemSize; c++)
        {
          var childPtr = itemInstances.First[c];
          if (childPtr.Value == null) continue;
          var ci = childPtr.Value->Instance;
          if (ci == null) continue;
          if (ci->Id.Type != InstanceType.BgPart) continue;
          // Child instance transforms are WORLD-space (layout engine flattens the
          // group hierarchy — same reason TargetMarker translations compare
          // directly against world positions). Do NOT compose with the group
          // transform: double-applying it displaces the triangles by the item's
          // own position/rotation (the v1.7.0.0 stage-panel height bug).
          var childM = BuildMatrix(*ci->GetTransformImpl());
          WalkWallTriangles(
            (BgPartsLayoutInstance*)ci,
            childM,
            probes, limit, cached.Name,
            ref itemBest, ref itemName, ref wallTris, ref hadWallTris,
            itemFront, acceptBackSide: false, acceptBothSides: true);
        }

        if (itemBest < beforeItemBest)
        {
          var hitUnion = GetItemColliderUnion(group);
          if (hitUnion.HasValue)
          {
            itemBB = hitUnion.Value;
            itemName = cached.Name;
          }
        }
        // Embedded-census fallback: known wall receiver, but no wall triangles were
        // readable at runtime — fall back to a two-sided union-AABB check.
        // Embedded-census fallback: known wall receiver, but no wall triangles were
        // readable at runtime — fall back to the union-AABB distance.
        if (!hadWallTris && SnapRangeData.IsWallReceiver(cached.ItemId))
        {
          usedAabbFallback = true;
          var union = GetItemColliderUnion(group);
          if (union.HasValue)
          {
            foreach (var p in probes)
            {
              var d = DistancePointToAABB(p, union.Value);
              if (d < itemBest)
              {
                itemBest = d;
                itemName = cached.Name;
              }
            }
          }
        }

        // Merge this item's best into its receiver class.
        if (itemBest < float.PositiveInfinity)
        {
          if (isMultiSided)
          {
            if (itemBest < bestMulti) { bestMulti = itemBest; bestMultiName = itemName; bestMultiBB = itemBB; }
          }
          else if (itemBest < bestSingle)
          {
            bestSingle = itemBest;
            bestSingleName = itemName;
            bestSingleBB = itemBB;
          }
        }
      }

      // Priority (measured): multi-face receivers > single-face false walls >
      // house walls. Distance does not decide between receiver classes.
      if (bestMulti <= limit)
      {
        bestFurn = bestMulti;
        bestFurnName = bestMultiName;
        bestFurnBB = bestMultiBB;
        furnClass = "multi-face";
      }
      else if (bestSingle <= limit)
      {
        bestFurn = bestSingle;
        bestFurnName = bestSingleName;
        bestFurnBB = bestSingleBB;
        furnClass = "single-face";
      }
      else
      {
        bestFurn = float.PositiveInfinity;
        bestFurnName = "";
      }

      // Static territory walls (the house's own walls): cached once per zone and
      // measured with the same one-sided triangle test. Same per-item capture.
      EnsureTerritoryWallCache(allowDeep: false);
      if (_territoryWallCache.Count == 0)
        EnsureTerritoryWallCache(allowDeep: true);

      var terrDiag = new System.Text.StringBuilder();
      foreach (var (inst, bb) in _territoryWallCache)
      {
        var near = false;
        var bbDist = float.PositiveInfinity;
        foreach (var p in probes)
        {
          bbDist = MathF.Min(bbDist, DistancePointToAABB(p, bb));
          if (bbDist <= territoryLimit) { near = true; break; }
        }
        if (!near)
        {
          terrDiag.Append($"[bb={bbDist:0.##} skip]");
          continue;
        }

        var m = BuildMatrix(*((BgPartsLayoutInstance*)inst)->GetTransformImpl());
        var dummy = false;
        // Per-wall score so each surface's result is visible in the diagnostic;
        // the winning wall is still the closest triangle hit (pre-1.15.12 rule —
        // containment / deep-inside hits were reverted, they over-warned).
        var wallBest = float.PositiveInfinity;
        var wallName = "";
        // House walls: distance to the attachable face only — no half-space test,
        // so an item slightly behind the plane still snaps forward onto it.
        WalkWallTriangles((BgPartsLayoutInstance*)inst, m, probes, territoryLimit, "house wall", ref wallBest, ref wallName, ref wallTris, ref dummy, itemFront, acceptBothSides: true);
        terrDiag.Append($"[bb={bbDist:0.##} tri={(float.IsInfinity(wallBest) ? "-" : wallBest.ToString("0.###"))}]");
        if (wallBest < bestTerr)
        {
          bestTerr = wallBest;
          bestTerrName = wallName;
        }
      }

      var furnHit = bestFurn <= limit;
      var terrHit = bestTerr <= territoryLimit;
      WallSnapDiagnostic = $"back-ray check ({wallTris} wall tris{(usedAabbFallback ? ", aabb fallback" : "")}), capture {capture:0.###} ({probes.Count} probes); furniture {(float.IsInfinity(bestFurn) ? "-" : bestFurn.ToString("0.###"))} (hit {(furnHit ? 1 : 0)}){(furnClass.Length > 0 ? ", " + furnClass : "")}{(bestFurnBB.HasValue ? $" bb=[{bestFurnBB.Value.Min.X:0.##},{bestFurnBB.Value.Min.Y:0.##},{bestFurnBB.Value.Min.Z:0.##}]-[{bestFurnBB.Value.Max.X:0.##},{bestFurnBB.Value.Max.Y:0.##},{bestFurnBB.Value.Max.Z:0.##}]" : "")}, territory {(float.IsInfinity(bestTerr) ? "-" : bestTerr.ToString("0.###"))} (hit {(terrHit ? 1 : 0)}){terrDiag}";

      if (furnHit || terrHit)
      {
        // PRIORITY RULE (measured in-game): furniture false walls (stage panels,
        // partitions) take priority over house/territory walls. The wall lantern
        // at Z=-12 snapped to the stage panel even though the house wall was
        // ~0.01 away; at Z=-12.01 (outside the panel's capture) it fell back to
        // the house wall. So if furniture is in capture, it wins outright.
        var useFurniture = furnHit;
        LastWallSnapHit = true;
        LastWallSnapDistance = useFurniture ? bestFurn : bestTerr;
        LastWallSnapTargetName = useFurniture ? bestFurnName : bestTerrName;
        LastWallSnapTargetIsTerritory = !useFurniture;
      }
    }

    private static Matrix4x4 BuildMatrix(FFXIVClientStructs.FFXIV.Client.LayoutEngine.Transform t) => t.Compose();

    /// <summary>
    /// Walks a BgPart's collision mesh and, for every wall-flagged triangle
    /// (bits 20..23 == 1), measures the distance from each probe point.
    /// House walls are distance-only (either side of the plane). Furniture false
    /// walls accept the back (anti-normal) side only.
    /// </summary>
    private static unsafe void WalkWallTriangles(
      BgPartsLayoutInstance* bgPart, Matrix4x4 world, List<Vector3> probes, float limit,
      string name, ref float best, ref string bestName, ref int wallTris, ref bool anyWall,
      Vector3 itemFront, bool acceptBackSide = false, bool acceptBothSides = false)
    {
      var collider = bgPart->Collider;
      if (collider == null || collider->GetColliderType() != ColliderType.Mesh) return;
      var meshCol = (ColliderMesh*)collider;
      if (meshCol->MeshIsSimple || meshCol->Mesh == null) return;
      var pcb = (MeshPCB*)meshCol->Mesh;
      if (pcb->RootNode == null) return;

      WalkNode(pcb->RootNode, world, probes, limit, name, ref best, ref bestName, ref wallTris, ref anyWall, new HashSet<nint>(), 0, acceptBackSide, acceptBothSides, itemFront);
    }

    private static unsafe void WalkNode(
      MeshPCB.FileNode* node, Matrix4x4 world, List<Vector3> probes, float limit,
      string name, ref float best, ref string bestName, ref int wallTris, ref bool anyWall,
      HashSet<nint> visited, int depth, bool acceptBackSide, bool acceptBothSides, Vector3 itemFront)
    {
      if (node == null || depth > 64) return;
      if (!visited.Add((nint)node)) return;

      // Prune: conservative world-space box of this node's bounds; if every probe
      // is beyond the warning limit from the box, no triangle inside can matter.
      var lb = node->LocalBounds;
      Vector3 bmin = new(float.MaxValue), bmax = new(float.MinValue);
      for (int cx = 0; cx < 2; cx++)
      for (int cy = 0; cy < 2; cy++)
      for (int cz = 0; cz < 2; cz++)
      {
        var corner = Vector3.Transform(
          new Vector3(
            (cx & 1) != 0 ? lb.Max.X : lb.Min.X,
            (cy & 1) != 0 ? lb.Max.Y : lb.Min.Y,
            (cz & 1) != 0 ? lb.Max.Z : lb.Min.Z),
          world);
        bmin = Vector3.Min(bmin, corner);
        bmax = Vector3.Max(bmax, corner);
      }
      var box = new AABB { Min = bmin, Max = bmax };
      var pruned = true;
      foreach (var p in probes)
      {
        if (DistancePointToAABB(p, box) <= limit) { pruned = false; break; }
      }
      if (pruned) return;

      for (int p = 0; p < node->NumPrims; p++)
      {
        var prim = &node->PrimitivesPtr[p];
        if (((prim->Material >> 20) & 0xF) != 1) continue;
        wallTris++;
        anyWall = true;

        var a = Vector3.Transform(NodeVertex(node, prim->V1), world);
        var b = Vector3.Transform(NodeVertex(node, prim->V2), world);
        var c = Vector3.Transform(NodeVertex(node, prim->V3), world);
        var n = Vector3.Cross(b - a, c - a);
        var len = n.Length();
        if (len < 1e-9f) continue;
        n /= len;

        foreach (var probe in probes)
        {
          float t;
          if (acceptBothSides)
          {
            // Multi-sided receiver � MakePlace rule (Cutest, ported):
            //   frontVector = rotation * (0, 100, 0)
            //   LineTraceSingleByChannel(position + frontVector, position - frontVector, WALL_CHANNEL)
            // A bidirectional line trace along the item's facing axis, 1.0 yalm each
            // way; the first wall surface hit is the snap target. The facing side
            // selects which side the item is pulled out of.
            var backHit = RayTriangleHit(probe, -itemFront, a, b, c, out var tb) &&
                          tb >= -0.1f && tb <= limit;
            var frontHit = RayTriangleHit(probe, itemFront, a, b, c, out var tf) &&
                           tf >= -0.1f && tf <= limit;
            if (!backHit && !frontHit) continue;
            t = backHit ? (frontHit ? MathF.Min(tb, tf) : tb) : tf;
          }          else
          {
            // Fallback for callers that don't pass the facing axis: plain distance
            // to the face. All wall receivers (one-sided included) use the
            // bidirectional facing ray — measured: the engine attaches from close
            // range at any angle but only from ~1.0/cos(angle) along the item's
            // facing axis, which is exactly the ray rule (same as multi-face).
            var d = DistancePointTriangle(probe, a, b, c);
            if (d > limit) continue;
            t = d;
          }

          if (t < best)
          {
            best = t;
            bestName = name;
          }
        }
      }

      WalkNode(node->Child1, world, probes, limit, name, ref best, ref bestName, ref wallTris, ref anyWall, visited, depth + 1, acceptBackSide, acceptBothSides, itemFront);
      WalkNode(node->Child2, world, probes, limit, name, ref best, ref bestName, ref wallTris, ref anyWall, visited, depth + 1, acceptBackSide, acceptBothSides, itemFront);
    }

    /// <summary>Möller–Trumbore ray/triangle intersection (no back-face culling).</summary>
    /// <summary>True when the point lies inside the AABB (with a small tolerance).</summary>
    private static bool ContainsPoint(AABB bb, Vector3 p, float eps = 0.001f)
      => p.X >= bb.Min.X - eps && p.X <= bb.Max.X + eps &&
         p.Y >= bb.Min.Y - eps && p.Y <= bb.Max.Y + eps &&
         p.Z >= bb.Min.Z - eps && p.Z <= bb.Max.Z + eps;
    private static bool RayTriangleHit(Vector3 origin, Vector3 dir, Vector3 a, Vector3 b, Vector3 c, out float t)
    {
      t = 0f;
      var e1 = b - a;
      var e2 = c - a;
      var pvec = Vector3.Cross(dir, e2);
      var det = Vector3.Dot(e1, pvec);
      if (MathF.Abs(det) < 1e-7f) return false;
      var invDet = 1f / det;
      var tvec = origin - a;
      var u = Vector3.Dot(tvec, pvec) * invDet;
      if (u < 0f || u > 1f) return false;
      var qvec = Vector3.Cross(tvec, e1);
      var v = Vector3.Dot(dir, qvec) * invDet;
      if (v < 0f || u + v > 1f) return false;
      t = Vector3.Dot(e2, qvec) * invDet;
      return true;
    }

    /// <summary>Decodes a mesh vertex (raw floats, or compressed ushorts scaled into the node bounds).</summary>
    private static unsafe Vector3 NodeVertex(MeshPCB.FileNode* node, int index)
    {
      var numRaw = node->NumVertsRaw;
      if (index < numRaw)
      {
        var p = node->RawVerticesPtr + 3 * index;
        return new Vector3(p[0], p[1], p[2]);
      }
      var ci = index - numRaw;
      var cp = node->CompressedVerticesPtr + 3 * ci;
      return node->LocalBounds.Min + node->CompressedVertexScale * new Vector3(cp[0], cp[1], cp[2]);
    }

    /// <summary>Exact distance from a point to a triangle (Ericson, Real-Time Collision Detection).</summary>
    private static float DistancePointTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
      var ab = b - a;
      var ac = c - a;
      var ap = p - a;
      var d1 = Vector3.Dot(ab, ap);
      var d2 = Vector3.Dot(ac, ap);
      if (d1 <= 0f && d2 <= 0f) return Vector3.Distance(p, a);

      var bp = p - b;
      var d3 = Vector3.Dot(ab, bp);
      var d4 = Vector3.Dot(ac, bp);
      if (d3 >= 0f && d4 <= d3) return Vector3.Distance(p, b);

      var vc = d1 * d4 - d3 * d2;
      if (vc <= 0f && d1 >= 0f && d3 <= 0f)
      {
        var v = d1 / (d1 - d3);
        return Vector3.Distance(p, a + v * ab);
      }

      var cp = p - c;
      var d5 = Vector3.Dot(ab, cp);
      var d6 = Vector3.Dot(ac, cp);
      if (d6 >= 0f && d5 <= d6) return Vector3.Distance(p, c);

      var vb = d5 * d2 - d1 * d6;
      if (vb <= 0f && d2 >= 0f && d6 <= 0f)
      {
        var w = d2 / (d2 - d6);
        return Vector3.Distance(p, a + w * ac);
      }

      var va = d3 * d6 - d5 * d4;
      if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f)
      {
        var w = (d4 - d3) / (d4 - d3 + (d5 - d6));
        return Vector3.Distance(p, b + w * (c - b));
      }

      var denom = 1f / (va + vb + vc);
      var v2 = vb * denom;
      var w2 = vc * denom;
      return Vector3.Distance(p, a + ab * v2 + ac * w2);
    }

    /// <summary>World-space union bounds of the active item's child colliders, or null.</summary>
    public unsafe (Vector3 Min, Vector3 Max)? GetActiveItemWorldBounds()
    {
      if (HousingStructure == null || HousingStructure->ActiveItem == null)
        return null;

      var active = HousingStructure->ActiveItem;

      // Union of the child BgPart VISUAL bounds (render meshes) — the group-level
      // bounds include marker/effect children that inflate the box (the Log Pillar
      // read 1.04 wide vs its ~0.45 mesh, which broke the floor snap threshold).
      var instances = active->Instances.Instances;
      var size = (int)(instances.Last - instances.First);
      Vector3 min = new(float.MaxValue), max = new(float.MinValue);
      var any = false;
      for (int i = 0; i < size; i++)
      {
        var childPtr = instances.First[i];
        if (childPtr.Value == null) continue;
        var ci = childPtr.Value->Instance;
        if (ci == null || ci->Id.Type != InstanceType.BgPart) continue;
        var bgPart = (BgPartsLayoutInstance*)ci;

        var childAabb = default(AxisAlignedBounds);
        bgPart->GetAxisAlignedBoundsImpl(&childAabb);
        if (childAabb.Max.X > childAabb.Min.X && childAabb.Max.Y > childAabb.Min.Y && childAabb.Max.Z > childAabb.Min.Z)
        {
          min = Vector3.Min(min, childAabb.Min);
          max = Vector3.Max(max, childAabb.Max);
          any = true;
          continue;
        }

        if (bgPart->Collider == null) continue;
        AABB bb = default;
        bgPart->Collider->GetWorldBB(&bb);
        min = Vector3.Min(min, new Vector3(bb.Min.X, bb.Min.Y, bb.Min.Z));
        max = Vector3.Max(max, new Vector3(bb.Max.X, bb.Max.Y, bb.Max.Z));
        any = true;
      }
      return any ? (min, max) : null;
    }

    /// <summary>
    /// The engine's own world-space bounds for the active item. Works for items
    /// without colliders (small tabletop furnishings), where collider AABBs are
    /// unavailable. Returns false when the bounds are degenerate.
    /// </summary>
    public unsafe bool GetActiveItemSphereBounds(out Vector3 center, out float radius)
    {
      center = default;
      radius = 0f;
      if (HousingStructure == null || HousingStructure->ActiveItem == null)
        return false;

      var sb = default(SphereBounds);
      HousingStructure->ActiveItem->GetSphereBounds(&sb);
      center = sb.CenterPoint;
      radius = sb.Radius;
      return radius > 0.0001f;
    }

    /// <summary>Union AABB of all BgPart collider world bounds for a placed item (embedded fallback).</summary>
    private static unsafe AABB? GetItemColliderUnion(SharedGroupLayoutInstance* group)
    {
      AABB? result = null;
      var instances = group->Instances.Instances;
      var size = (int)(instances.Last - instances.First);
      for (int c = 0; c < size; c++)
      {
        var childPtr = instances.First[c];
        if (childPtr.Value == null) continue;
        var ci = childPtr.Value->Instance;
        if (ci == null) continue;
        if (ci->Id.Type != InstanceType.BgPart) continue;
        var bgPart = (BgPartsLayoutInstance*)ci;
        if (bgPart->Collider == null) continue;
        AABB bb = default;
        bgPart->Collider->GetWorldBB(&bb);
        if (result == null)
        {
          result = bb;
        }
        else
        {
          var u = result.Value;
          if (bb.Min.X < u.Min.X) u.Min.X = bb.Min.X;
          if (bb.Min.Y < u.Min.Y) u.Min.Y = bb.Min.Y;
          if (bb.Min.Z < u.Min.Z) u.Min.Z = bb.Min.Z;
          if (bb.Max.X > u.Max.X) u.Max.X = bb.Max.X;
          if (bb.Max.Y > u.Max.Y) u.Max.Y = bb.Max.Y;
          if (bb.Max.Z > u.Max.Z) u.Max.Z = bb.Max.Z;
          result = u;
        }
      }
      return result;
    }

    /// <summary>Distance from a point to the closest point on an AABB (0 when inside).</summary>
    private static float DistancePointToAABB(Vector3 p, AABB bb)
    {
      var cx = System.Math.Clamp(p.X, bb.Min.X, bb.Max.X);
      var cy = System.Math.Clamp(p.Y, bb.Min.Y, bb.Max.Y);
      var cz = System.Math.Clamp(p.Z, bb.Min.Z, bb.Max.Z);
      var dx = p.X - cx;
      var dy = p.Y - cy;
      var dz = p.Z - cz;
      return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// Returns the FFXIV Item RowId of the currently selected housing item,
    /// or 0 if no item is selected or the lookup fails. This is the ID format
    /// used by Lumina's Item sheet — suitable for SnapData lookups.
    /// </summary>
    public unsafe uint GetActiveItemFFXIVId()
    {
      if (HousingStructure == null)
        return 0;
      var active = HousingStructure->ActiveItem;
      if (active == null)
      {
        _cachedActiveItemPtr = null;
        _cachedActiveItemId = 0;
        _cachedActiveItemCategory = 0;
        ActiveItemSnapThreshold = 0f;
        return 0;
      }

      // Cache hit — same selected item as last query.
      if (_cachedActiveItemPtr == (void*)active)
        return _cachedActiveItemId;

      _cachedActiveItemPtr = (void*)active;
      _cachedActiveItemId = 0;

      // Walk the housing object array, find the GameObject whose
      // SharedGroupLayoutInstance pointer matches the active item.
      if (HousingModule == null || HousingModule->GetCurrentManager() == null || HousingModule->GetCurrentManager()->Objects == null)
      {
        ActiveItemSnapThreshold = ComputeSnapThresholdForActiveItem();
        return 0;
      }

      for (var i = 0; i < 600; i++)
      {
        var addr = HousingModule->GetCurrentManager()->Objects[i];
        if (addr == 0) continue;

        var go = (GameObject*)addr;
        if (go->SharedGroupLayoutInstance != active) continue;

        // Found the matching GameObject. Its BaseId is the HousingFurniture
        // row ID; resolve through the loaded furniture sheet to get the
        // actual Item.RowId (which is what Re:MakePlace's data is keyed on).
        var furnitureRowId = go->BaseId;
        if (Plugin.TryGetFurnishing(furnitureRowId, out var furniture))
        {
          _cachedActiveItemId = furniture.Item.Value.RowId;
          _cachedActiveItemCategory = (uint)furniture.HousingItemCategory;
          _cachedActiveItemModelKey = furniture.ModelKey;
          _cachedActiveItemIsOutdoor = false;
        }
        else if (Plugin.TryGetYardObject(furnitureRowId, out var yard))
        {
          _cachedActiveItemId = yard.Item.Value.RowId;
          _cachedActiveItemCategory = (uint)yard.HousingItemCategory;
          _cachedActiveItemModelKey = yard.ModelKey;
          _cachedActiveItemIsOutdoor = true;
        }
        _lastActiveItemIdPost = _cachedActiveItemId; // kept through deselect for post-snap logging
        break;
      }

      // Capture the snap threshold. Priority:
      // 1. SphereCastRange override read at runtime (sgb type 0x4B -> Cast.X).
      // 2. Embedded .sgb table fallback (instance children may not be streamed in).
      // 3. Wall items: authored TargetMarker distance.
      // 4. Regular snap: shortest-edge / 2 from the collider footprint.
      //
      // Authored values (1-3) are EXACT � in-game confirmed: the Stage Panel's
      // authored 0.1 snaps at 0.100 and is free at 0.101, so the safety margin
      // must NOT be added to them. Only the approximate formula (4) gets the
      // safety margin (it can underestimate, e.g. Grassy Rug).
      {        var runtimeRange = GetSphereCastRangeOverride(active);
        var modelFloor = ModelFloorData.Get(_cachedActiveItemModelKey, _cachedActiveItemIsOutdoor);
        if (runtimeRange.HasValue)
        {
          ActiveItemSnapThreshold = runtimeRange.Value;
          ActiveItemThresholdIsOverride = true;
          ActiveItemSphereCastSource = "runtime";
          SnapThresholdDiagnostic = $"SphereCastRange override (runtime) -> {runtimeRange.Value:0.####}";
        }
        else if (_cachedActiveItemId != 0 && SnapRangeData.TryGetSnapRange(_cachedActiveItemId, out var tableRange))
        {
          ActiveItemSnapThreshold = tableRange;
          ActiveItemThresholdIsOverride = true;
          ActiveItemSphereCastSource = "sgb table";
          SnapThresholdDiagnostic = $"SphereCastRange override (sgb table) -> {tableRange:0.####}";
        }
        else if ((modelFloor = ModelFloorData.Get(_cachedActiveItemModelKey, _cachedActiveItemIsOutdoor)) > 0f)
        {
          // Fox's box formula, taken from the game's own model bounding box:
          // min(box X, box Z) / 2. Verified against the engine's exact boundaries
          // (Log Pillar 0.3716 vs snap 0.371 / float 0.372; White Rect 0.11292 vs
          // 0.112 / 0.113). The formula is exact — no safety margin.
          ActiveItemSnapThreshold = modelFloor;
          ActiveItemThresholdIsOverride = true;
          ActiveItemSphereCastSource = "(none)";
          SnapThresholdDiagnostic = $"model box -> {modelFloor:0.#####} (m{_cachedActiveItemModelKey})";
        }
        else if (_cachedActiveItemCategory == 15)
        {
          // Wall-mounted item: the engine's wall snap distance comes from the
          // item's TargetMarker children (max XZ distance from pivot x 0.5) —
          // Housingway-validated. Fallbacks: embedded sgb table, then the
          // footprint formula.
          ActiveItemSphereCastSource = "(none)";
          var wallDist = GetWallTargetMarkerDistance(active, active->Transform.Translation);
          if (wallDist.HasValue && wallDist.Value > 0f)
          {
            ActiveItemSnapThreshold = wallDist.Value;
            ActiveItemThresholdIsOverride = true;
            SnapThresholdDiagnostic = $"wall TargetMarker -> {wallDist.Value:0.####}";
          }
          else if (_cachedActiveItemId != 0 && SnapRangeData.TryGetWallThreshold(_cachedActiveItemId, out var wt))
          {
            ActiveItemSnapThreshold = wt;
            ActiveItemThresholdIsOverride = true;
            SnapThresholdDiagnostic = $"wall threshold (sgb table) -> {wt:0.####}";
          }
          else
          {
            ActiveItemThresholdIsOverride = false;
            ActiveItemSnapThreshold = ComputeSnapThresholdForActiveItem();
          }
        }
        else
        {
          ActiveItemSphereCastSource = "(none)";
          ActiveItemThresholdIsOverride = false;
          ActiveItemSnapThreshold = ComputeSnapThresholdForActiveItem();
        }
      }

      return _cachedActiveItemId;
    }

    /// <summary>
    /// Writes the position vector to memory.
    /// </summary>
    /// <param name="newPosition">Position vector to write.</param>
    public unsafe void WritePosition(Vector3 newPosition)
    {
      // Don't write if housing mode isn't on.
      if (!CanEditItem())
        return;

      try
      {
        var item = HousingStructure->ActiveItem;
        if (item == null)
          return;

        // Set the position.
        item->Transform.Translation = newPosition;
      }
      catch (Exception ex)
      {
        Plugin.Log.Error(ex, "Error occured while writing position!");
      }
    }

    public unsafe void WriteRotation(Vector3 newRotation)
    {
      // Don't write if housing mode isn't on.
      if (!CanEditItem())
        return;

      try
      {
        var item = HousingStructure->ActiveItem;
        if (item == null)
          return;

        // Convert into a quaternion.
        item->Transform.Rotation = Util.ToQ(newRotation);
      }
      catch (Exception ex)
      {
        Plugin.Log.Error(ex, "Error occured while writing rotation!");
      }
    }

    /// <summary>
    /// Thread loop for reading memory.
    /// </summary>
    public unsafe void Update()
    {
      try
      {
        var lastIsHousingOpen = _isHousingOpen;
        _isHousingOpen = IsHousingOpen();

        // Housing interface closed: the engine applies DEFERRED snaps here (items
        // pushed into the floor get pulled onto the floor only when placement mode
        // ends). Diff the layout snapshot immediately and log every movement.
        if (lastIsHousingOpen && !_isHousingOpen)
        {
          var mgrExit = HousingModule != null ? HousingModule->GetCurrentManager() : null;
          if (mgrExit != null && mgrExit->Objects != null)
          {
            for (var i = 0; i < 600; i++)
            {
              var addr = mgrExit->Objects[i];
              if (addr == 0) continue;
              var go = (GameObject*)addr;
              var key = (nint)go->SharedGroupLayoutInstance;
              var p = go->Position;
              if (_lastLayoutSnapshot.TryGetValue(key, out var prev))
              {
                if (Vector3.Distance(p, prev) > 0.0005f)
                  SnapLogger.Event("housing_exit_snap",
                    $"baseId={go->BaseId} before=[{prev.X:0.####},{prev.Y:0.####},{prev.Z:0.####}] after=[{p.X:0.####},{p.Y:0.####},{p.Z:0.####}]");
                _lastLayoutSnapshot[key] = p;
              }
            }
          }
        }

        // Deselect detection + post-snap tracking. The engine applies snaps when
        // the item is deselected, so this transition (and where the item lands)
        // is the ground truth for every snap warning.
        var hsTrack = HousingStructure;
        if (hsTrack != null)
        {
          var ai = hsTrack->ActiveItem;
          if (ai != null)
          {
            var activePos = ai->Transform.Translation;
            if (!_hadActiveItem || _activeItemSelectGroup != (nint)ai)
            {
              // New selection: remember where the item started.
              _activeItemSelectGroup = (nint)ai;
              _activeItemSelectPos = activePos;
              ActiveItemMovedSinceSelect = false;
            }
            else if (Vector3.Distance(activePos, _activeItemSelectPos) > 0.0005f)
            {
              ActiveItemMovedSinceSelect = true;
            }

            _hadActiveItem = true;
            _lastActiveGroup = (nint)ai;
            _lastActiveGroupPos = activePos;
            _postSnapFrames = 0;
          }
          else if (_hadActiveItem)
          {
            _hadActiveItem = false;
            _pendingPostSnapGroup = _lastActiveGroup;
            _pendingPostSnapItemId = _lastActiveItemIdPost;
            _postSnapLast = _lastActiveGroupPos;
            _postSnapFrames = 45;
            _postSnapStable = 0;
            SnapLogger.Event("DESELECT",
              $"item={_pendingPostSnapItemId} group=0x{_lastActiveGroup:X} " +
              $"heldPos=[{_lastActiveGroupPos.X:0.####},{_lastActiveGroupPos.Y:0.####},{_lastActiveGroupPos.Z:0.####}]");
          }

          if (_pendingPostSnapGroup != 0 && _postSnapFrames > 0)
          {
            _postSnapFrames--;
            var mgr = HousingModule != null ? HousingModule->GetCurrentManager() : null;
            if (mgr != null && mgr->Objects != null)
            {
              for (var i = 0; i < 600; i++)
              {
                var addr = mgr->Objects[i];
                if (addr == 0) continue;
                var go = (GameObject*)addr;
                if ((nint)go->SharedGroupLayoutInstance != _pendingPostSnapGroup) continue;

                var p = go->Position;
                var moved = Vector3.Distance(p, _postSnapLast);
                if (moved > 0.0005f)
                {
                  _postSnapLast = p;
                  _postSnapStable = 0;
                  SnapLogger.Event("postsnap_move",
                    $"pos=[{p.X:0.####},{p.Y:0.####},{p.Z:0.####}] moved={moved:0.####} framesLeft={_postSnapFrames}");
                }
                else if (++_postSnapStable >= 3)
                {
                  var dx = p.X - _lastActiveGroupPos.X;
                  var dy = p.Y - _lastActiveGroupPos.Y;
                  var dz = p.Z - _lastActiveGroupPos.Z;
                  SnapLogger.Event("POSTSNAP_FINAL",
                    $"item={_pendingPostSnapItemId} before=[{_lastActiveGroupPos.X:0.####},{_lastActiveGroupPos.Y:0.####},{_lastActiveGroupPos.Z:0.####}] " +
                    $"after=[{p.X:0.####},{p.Y:0.####},{p.Z:0.####}] delta=[{dx:0.####},{dy:0.####},{dz:0.####}] mag={MathF.Sqrt(dx * dx + dy * dy + dz * dz):0.####}");
                  _pendingPostSnapGroup = 0;
                }
                break;
              }
            }
            if (_postSnapFrames == 0 && _pendingPostSnapGroup != 0)
            {
              SnapLogger.Event("postsnap_lost", $"item={_pendingPostSnapItemId} group=0x{_pendingPostSnapGroup:X}");
              _pendingPostSnapGroup = 0;
            }
          }
        }

        // Layout drift watch: re-instance snaps (the server re-validating placed
        // items on load) and any un-dragged movement of placed items land here.
        if (_isHousingOpen && HousingModule != null)
        {
          var cur = (nint)HousingModule->CurrentTerritory;
          if (cur != _lastTerritoryPtr)
          {
            _lastTerritoryPtr = cur;
            _lastLayoutSnapshot.Clear();
            _layoutSeeding = true;
            SnapLogger.Event("territory", $"currentTerritory=0x{cur:X}");
          }

          if ((DateTime.Now - _lastLayoutSnapTime).TotalMilliseconds >= 2000)
          {
            _lastLayoutSnapTime = DateTime.Now;
            var mgr = HousingModule != null ? HousingModule->GetCurrentManager() : null;
            if (mgr != null && mgr->Objects != null)
            {
              var activeNow = hsTrack != null ? hsTrack->ActiveItem : null;
              for (var i = 0; i < 600; i++)
              {
                var addr = mgr->Objects[i];
                if (addr == 0) continue;
                var go = (GameObject*)addr;
                if (activeNow != null && go->SharedGroupLayoutInstance == activeNow) continue; // dragged item covered by deselect capture
                var key = (nint)go->SharedGroupLayoutInstance;
                var p = go->Position;
                if (_lastLayoutSnapshot.TryGetValue(key, out var prev))
                {
                  if (Vector3.Distance(p, prev) > 0.0005f)
                    SnapLogger.Event("layout_change",
                      $"baseId={go->BaseId} before=[{prev.X:0.####},{prev.Y:0.####},{prev.Z:0.####}] after=[{p.X:0.####},{p.Y:0.####},{p.Z:0.####}]");
                }
                _lastLayoutSnapshot[key] = p;
              }
              if (_layoutSeeding)
              {
                _layoutSeeding = false;
                SnapLogger.Event("layout_seed", $"count={_lastLayoutSnapshot.Count}");
              }
            }
          }
        }

        // Just perform once when housing is opened
        if (lastIsHousingOpen != _isHousingOpen && _isHousingOpen)
        {
          var config = Plugin.GetConfiguration();
          if (!config.DisplayFurnishingList)
            AtkManager.ShowFurnishingList(false);
          if (!config.DisplayInventory)
            AtkManager.ShowInventory(false);
        }

        if (!CanEditItem())
        {
          _lastActiveY = float.NaN;
          return;
        }

        // Don't really need to load position if we're reading it in the UI thread anyway, but leaving it for now for redudency...
        Position = ReadPosition();
        Rotation = ReadRotation();

        // Track vertical movement direction (see VerticalMoveDirection): the
        // engine's snap follows the direction of travel, so warnings should too.
        if (!float.IsNaN(_lastActiveY))
        {
          var dy = Position.Y - _lastActiveY;
          if (dy < -0.0005f) VerticalMoveDirection = -1;
          else if (dy > 0.0005f) VerticalMoveDirection = 1;
          else VerticalMoveDirection = 0;
        }
        _lastActiveY = Position.Y;

        // Update the model of active item, the game doesn't do this for wall mounted and outside in rotate mode
        var item = HousingStructure->ActiveItem;
        if (item != null)
          HousingLayoutModelUpdate((IntPtr)item + 0x80);
      }
      catch (PluginException)
      {
        Position = Vector3.Zero;
        Rotation = Vector3.Zero;
      }
      catch (Exception ex)
      {
        Plugin.Log.Error(ex, "Unknown exception");
        Position = Vector3.Zero;
        Rotation = Vector3.Zero;
      }
    }

    /// <summary>
    /// Get furnishings as they appear in the array in memory.
    /// </summary>
    /// <param name="objects"></param>
    /// <param name="point"></param>
    /// <param name="sortByDistance"></param>
    /// <returns></returns>
    public unsafe bool GetFurnishings(out List<GameObject> objects, Vector3 point, bool sortByDistance = false)
    {
      // Initialize the output to empty for the sake of noticing an error
      objects = [];

      // Exit early due to missing addresses
      if (HousingModule == null || HousingModule->GetCurrentManager() == null || HousingModule->GetCurrentManager()->Objects == null)
        return false;

      // Create a list of game objects and their distance from the player
      var furnishings = new List<(GameObject gameObject, float distance)>();
      // Iterate over array size, max furnishing size (600 as of Dawntrail)
      for (var i = 0; i < 600; i++)
      {
        // Get the object from the array
        var objectAddress = HousingModule->GetCurrentManager()->Objects[i];
        if (objectAddress == 0)
          continue;
        // Cast the address to a GameObject
        var gameObject = *(GameObject*)objectAddress;
        // Add to the objects including distance from player
        furnishings.Add((gameObject, Util.DistanceFromPlayer(gameObject, point)));
      }

      // Pre-sort the objects
      if (sortByDistance)
        furnishings.Sort((obj1, obj2) => obj1.distance.CompareTo(obj2.distance));

      // Set the output variable to just the game objects
      objects = furnishings.Select(obj => obj.gameObject).ToList();

      return true;
    }

    public static byte[] ReadBytes(IntPtr addr, int length)
    {
      var bytes = new byte[length];
      Marshal.Copy(addr, bytes, 0, length);
      return bytes;
    }

    public static void WriteProtectedBytes(IntPtr addr, byte[] b)
    {
      if (addr == IntPtr.Zero)
        return;
      VirtualProtect(addr, 1, Protection.PAGE_EXECUTE_READWRITE, out var oldProtection);
      Marshal.Copy(b, 0, addr, b.Length);
      VirtualProtect(addr, 1, oldProtection, out _);
    }

    public static void WriteProtectedBytes(IntPtr addr, byte b)
    {
      if (addr == IntPtr.Zero)
        return;
      WriteProtectedBytes(addr, [b]);
    }

    /// <summary>
    /// Sets the flag for place anywhere in memory.
    /// </summary>
    /// <param name="state">Boolean state for if you can place anywhere.</param>
    public static void SetPlaceAnywhere(bool state)
    {
      PlaceAnywhere.SetState(state);
      ShowcaseAnywhere.SetState(state);
    }

    #region Kernel32
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, Protection flNewProtect, out Protection lpflOldProtect);

    private enum Protection
    {
      // PAGE_NOACCESS = 0x01,
      // PAGE_READONLY = 0x02,
      // PAGE_READWRITE = 0x04,
      // PAGE_WRITECOPY = 0x08,
      // PAGE_EXECUTE = 0x10,
      // PAGE_EXECUTE_READ = 0x20,
      PAGE_EXECUTE_READWRITE = 0x40,
      // PAGE_EXECUTE_WRITECOPY = 0x80,
      // PAGE_GUARD = 0x100,
      // PAGE_NOCACHE = 0x200,
      // PAGE_WRITECOMBINE = 0x400
    }
    #endregion
  }
}
