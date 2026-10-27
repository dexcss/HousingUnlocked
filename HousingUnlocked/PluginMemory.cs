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

    // Wall receiver STICKINESS. The engine keeps a wall item on the receiver it is
    // already on until another is clearly closer. Both pure rules were tried and each
    // fixed one case while breaking the other:
    //   multi-face-first  -> predicted White Rect, engine attached Stage Panel (nd=0)
    //   nearest-first     -> predicted Stage Panel, engine attached White Rect
    // so the previous winner is retained unless the alternative is >=10 cm closer.
    private string _lastWallFurnName = "";

    // ==== Reverse wall re-attachment: placed wall items that will snap to the
    // item currently being held (the active item acting as a wall receiver) ====
    public bool LastReverseWallHit { get; private set; } = false;
    public string LastReverseWallNames { get; private set; } = "";
    public string ReverseWallDiagnostic { get; private set; } = "(not computed)";

    /// <summary>
    /// Compact per-frame reverse-wall geometry, written to the JSONL frame line as
    /// "revDiag". It records the NEAREST placed category-15 item even when it is
    /// rejected, including that item's live position and facing, so a warning that
    /// flickers can be correlated with the placed item actually moving (re-instance)
    /// rather than with the held receiver's position. Format:
    ///   n=&lt;candidates&gt; near="&lt;name&gt;" p=(x,y,z) f=(x,y,z) a=along s=side g=gap why=ok|behind|side|far
    ///   ap=(active pivot) fh=faceHalf sideTol=... cap=...
    /// </summary>
    public string ReverseWallFrameDiag { get; private set; } = "(not computed)";

    // Already-attached detection for the reverse warning. A placed wall item that is
    // RIDING ALONG with the held item is already attached to it, so "-> X will snap
    // here" is redundant and reads as a bug. Detected by matching per-frame motion:
    // if the candidate moved by the same delta as the held item, it is following it.
    // (Measured 2026-10-27: the Wall Lantern tracked the dragged White Rect via
    // layout_change events while the line kept claiming it would snap.)
    private readonly Dictionary<nint, Vector3> _revCandLastPos = new();
    private Vector3 _revLastActivePos;
    private bool _revHaveActivePos;

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

    // Cache: furniture row ID -> HousingItemCategory (15 = wall-mounted actor).
    private readonly Dictionary<uint, uint> _furnitureCategoryCache = new();

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

    // The active-wall prediction captured at the moment of deselect, so the placed
    // item's FINAL position can be compared against what the card claimed. This makes
    // every future test self-validating: the log records both sides without needing the
    // tester to narrate what happened.
    private bool _predWallHit;
    private string _predWallTarget = "";
    private float _predWallDist = float.PositiveInfinity;
    private bool _predWallIsTerritory;
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
      var ownColliders = new List<nint>();
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
        ownColliders.Add((nint)collider);
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
      // Sample the whole footprint, not just the pivot: the game rests an item on
      // the highest floor-top point under its base, so on the sloped yard land the
      // pivot can read a very different height than the contact point. Each sample
      // also probes from just below and just above the base (a resting base can sit
      // inside the surface, where the lower probe slips through to the ward-connector
      // floor far below). Nearest surface across every sample wins.
      var obFloor = default(OrientedBounds);
      active->GetOrientedBoundsImpl(&obFloor);
      var footX = MathF.Max(0.05f, obFloor.HalfExtents.X * 0.7f);
      var footZ = MathF.Max(0.05f, obFloor.HalfExtents.Z * 0.7f);
      var downDir = new Vector3(0f, -1f, 0f);
      float floorGap = float.PositiveInfinity;
      var gotFloor = false;
      Span<Vector3> footOffsets = stackalloc Vector3[]
      {
        Vector3.Zero, new Vector3(footX, 0f, footZ), new Vector3(-footX, 0f, footZ),
        new Vector3(footX, 0f, -footZ), new Vector3(-footX, 0f, -footZ),
      };
      Span<float> downOffsets = stackalloc float[] { -Epsilon, 0.01f, 0.1f };
      for (var fi = 0; fi < footOffsets.Length; fi++)
      {
        var fo = footOffsets[fi];
        for (var oi = 0; oi < downOffsets.Length; oi++)
        {
          var offset = downOffsets[oi];
          var downOrigin = new Vector3(pos.X + fo.X, lowestY + offset, pos.Z + fo.Z);
          if (!RaycastFloorTopAll(downOrigin, downDir, 100f, out var downHit)) continue;
          // A probe starting inside the held item hits its own collider at ~0 — ignore
          // those so the item can never report itself as the floor.
          if (ownColliders.Contains((nint)downHit.Object)) continue;
          var gap = downHit.Distance - offset + (pos.Y - lowestY);
          if (gap < 0f) gap = 0f;
          if (gap < floorGap) floorGap = gap;
          gotFloor = true;
        }
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
        // Outdoor: no fixed floor levels. The engine uses the floor-top surface
        // directly above the item's PIVOT - a deck edge beside the item must not
        // count (Summer Bonfire: an adjacent 19.0 deck made the old
        // outside-footprint probes report "floor above" while the pivot only had
        // the 20.0 roof over it). The held item's own colliders are hidden from
        // raycasts while probing so the ray cannot stop on the item itself.
        // Every surface above the pivot is walked and the nearest one wins
        // (plot: deck at 19.0033 pulls up from 18.22, not 18.21; bonfire: the
        // 20.0 roof pulls up from 18.83, not 18.82).
        var savedCount = ownColliders.Count;
        Span<nint> savedPtrs = stackalloc nint[savedCount];
        Span<byte> savedFlags = stackalloc byte[savedCount];
        for (var oi = 0; oi < savedCount; oi++)
        {
          var col = (Collider*)ownColliders[oi];
          savedPtrs[oi] = ownColliders[oi];
          savedFlags[oi] = col->VisibilityFlags;
          col->VisibilityFlags &= 0xFE; // bit0 = active for raycasts
        }
        try
        {
          var probeY = pos.Y + 10f;
          for (var iter = 0; iter < 8; iter++)
          {
            var upOrigin = new Vector3(pos.X, probeY, pos.Z);
            if (!RaycastFloorTopAll(upOrigin, new Vector3(0f, -1f, 0f), 100f, out var upHit)) break;
            var surfaceY = probeY - upHit.Distance;
            if (surfaceY <= pos.Y + 0.02f) break; // reached the pivot; nothing above left
            var gapUp = surfaceY - pos.Y;
            if (gapUp < ceilingGap)
            {
              ceilingGap = gapUp;
              foundUpwardSnap = true;
            }
            probeY = surfaceY - 0.02f;
          }
        }
        finally
        {
          for (var oi = 0; oi < savedCount; oi++)
            ((Collider*)savedPtrs[oi])->VisibilityFlags = savedFlags[oi];
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
      var mode = config.ItemSnapRangeMode;

      float bestDist = float.PositiveInfinity;
      float bestEffective = 0f;
      uint bestId = 0;
      string bestName = "";
      int scanned = 0;
      int withRange = 0;
      // DIAGNOSTIC: near-miss candidates (within effective + 0.3), so a false card or a
      // missing card can be explained by the numbers rather than inferred.
      var cands = new List<string>(4);

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

        if (dist <= effective + 0.3f && cands.Count < 4)
          cands.Add($"{cached.Name}#{cached.ItemId}@{dist:0.###}/{effective:0.###}");

        // STRICT test for the "WILL SNAP" card. The safety margin (config.SnapThreshold,
        // default 0.07) is an early-warning allowance, NOT part of the engine's snap
        // range. Including it here produced false cards: measured 2026-10-26 holding the
        // White Rect, the Stage Panel has range 0.100 and the logged distance was 0.1655,
        // yet 0.1655 <= 0.100 + 0.07 = 0.17 made the card claim "WILL SNAP - item:
        // Stage Panel". It does not snap at that distance.
        if (dist <= effective && dist < bestDist)
        {
          bestDist = dist;
          bestEffective = effective;
          bestId = cached.ItemId;
          bestName = cached.Name;
        }
      }

      var obCtx = default(OrientedBounds);
      active->GetOrientedBoundsImpl(&obCtx);
      var ctxFront = Vector3.Transform(Vector3.UnitZ, active->Transform.Rotation);
      ItemSnapDiagnostic = $"scanned {scanned} nearby ({withRange} snap-capable), active range {activeRange:0.####} [{ActiveItemSphereCastSource}], mode {mode} || " +
        $"ctx=id:{activeItemId} cat:{_cachedActiveItemCategory} model:{_cachedActiveItemModelKey} out:{(_cachedActiveItemIsOutdoor ? 1 : 0)} " +
        $"p=({activePos.X:0.###},{activePos.Y:0.###},{activePos.Z:0.###}) front=({ctxFront.X:0.##},{ctxFront.Y:0.##},{ctxFront.Z:0.##}) " +
        $"half=({obCtx.HalfExtents.X:0.###},{obCtx.HalfExtents.Y:0.###},{obCtx.HalfExtents.Z:0.###}) cands=[{string.Join(";", cands)}]";

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

      // DIAGNOSTIC: every candidate receiver within range, with the side its winning
      // triangle was hit from. This is the data needed to explain "it says it will snap
      // when the item is behind the wall" without guessing at the winding convention.
      var recvLog = new List<string>(6);
      var winN = Vector3.Zero;
      var winP = Vector3.Zero;
      var winT = 0f;
      var winSide = 0;
      var winSigned = 0f;
      var winPerp = 0f;
      var winIsTerr = false;
      var winName = "";
      // Per-class hit plane + position, needed for the occlusion rule: a single-face
      // panel BETWEEN the item and a multi-face receiver BLOCKS it (author, 2026-10-27:
      // behind the Stage Panel -> snaps to the Stage Panel; in the gap -> snaps to the
      // White Rect). That is a side-of-plane test, not a distance test.
      var multiN = Vector3.Zero; var multiP = Vector3.Zero; var multiPos = Vector3.Zero;
      var singleN = Vector3.Zero; var singleP = Vector3.Zero; var singlePos = Vector3.Zero;
      // STRAIGHT-LINE distance per class. The ray parameter `bestMulti` grows large at
      // oblique angles, which wrongly put an in-range White Rect out of capture and let
      // the Stage Panel win by default (seen at lantern Z=-8.6891: Euclidean 0.562 but
      // ray > 1.0). Capture for the multi-face receiver must use the Euclidean distance.
      var multiPerp = float.PositiveInfinity;
      var singlePerp = float.PositiveInfinity;
      // DIAGNOSTIC: nearest object in the scan at all (regardless of whether it resolved
      // to a known furnishing). If a receiver is "invisible", this shows whether the object
      // is even present and how close it is, separating a cache/resolution drop from a
      // triangle-walk miss.
      uint nearId = 0;
      var nearDist = float.MaxValue;

      for (var i = 0; i < 600; i++)
      {
        var addr = HousingModule->GetCurrentManager()->Objects[i];
        if (addr == 0) continue;
        var go = (GameObject*)addr;
        if (go->SharedGroupLayoutInstance == active) continue;
        var dNear = Vector3.Distance(pos, go->Position);
        if (dNear > 8f) continue;
        if (dNear < nearDist) { nearDist = dNear; nearId = go->BaseId; }

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

        // DIAGNOSTIC: geometry of this receiver's winning triangle (see _wallGeom*).
        var rGeomN = _wallGeomNormal;
        var rGeomP = _wallGeomPoint;
        var rGeomT = _wallGeomT;
        var rGeomSide = _wallGeomSide;
        var rGeomSigned = _wallGeomSigned;
        var rGeomPerp = _wallGeomPerpT;

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
          if (recvLog.Count < 6)
            recvLog.Add($"{cached.Name}#{cached.ItemId}:{(isMultiSided ? "M" : "S")}" +
                        $":d={itemBest:0.###}:side={rGeomSide}:sd={rGeomSigned:0.###}:perp={rGeomPerp:0.###}:t={rGeomT:0.###}");
          if (isMultiSided)
          {
            if (itemBest < bestMulti)
            {
              bestMulti = itemBest; bestMultiName = itemName; bestMultiBB = itemBB;
              winN = rGeomN; winP = rGeomP; winT = rGeomT; winSide = rGeomSide;
              winSigned = rGeomSigned; winPerp = rGeomPerp; winIsTerr = false; winName = itemName;
              multiN = rGeomN; multiP = rGeomP; multiPerp = hadWallTris ? rGeomPerp : itemBest;
              multiPos = new Vector3(go->Position.X, go->Position.Y, go->Position.Z);
            }
          }
          else if (itemBest < bestSingle)
          {
            bestSingle = itemBest;
            bestSingleName = itemName;
            bestSingleBB = itemBB;
            winN = rGeomN; winP = rGeomP; winT = rGeomT; winSide = rGeomSide;
            winSigned = rGeomSigned; winPerp = rGeomPerp; winIsTerr = false; winName = itemName;
            singleN = rGeomN; singleP = rGeomP; singlePerp = hadWallTris ? rGeomPerp : itemBest;
            singlePos = new Vector3(go->Position.X, go->Position.Y, go->Position.Z);
          }
        }
      }

      // RECEIVER PRIORITY — stated authoritatively by the plugin author 2026-10-27:
      //   house wall (territory)   = LOWEST
      //   single-face receiver     = SECOND   (Stage Panel)
      //   multi-face receiver      = HIGHEST  (White Rectangular Partition)
      //
      // The class order is ABSOLUTE and distance only breaks ties WITHIN a class. This is
      // the original rule; it was briefly abandoned because one logged case had a Stage
      // Panel at nd=0.000 beating a White Rect at 0.816 - that is now understood as the
      // White Rect being outside ITS OWN (smaller, magnet-like) capture rather than the
      // class order being wrong. Do not replace this with nearest-wins again: three
      // distance-based rules were tried and each contradicted the author's report.
      // OCCLUSION (author, 2026-10-27): if a single-face panel lies BETWEEN the item and
      // the multi-face receiver, the multi-face one is blocked and the panel wins even
      // though multi-face has the higher class priority. Otherwise class priority holds:
      // territory < single-face < multi-face.
      var occludedBySingle = false;
      var occDepth = 0f;
      var occPanelHalf = 0f;
      if (bestMulti <= limit && bestSingle <= limit && singleN != Vector3.Zero)
      {
        var itemSide = Vector3.Dot(singleN, probes[0] - singleP);
        var multiSide = Vector3.Dot(singleN, multiPos - singleP);
        occDepth = itemSide;
        // "FULLY behind the face" (author, 2026-10-27): the item only switches to the
        // single-face panel once it is past the panel's whole BODY, not merely past its
        // plane. Anything less and the multi-face receiver still wins. So the required
        // depth is the panel's own half-thickness along the hit normal, not a constant.
        if (bestSingleBB.HasValue)
        {
          var e = bestSingleBB.Value.Max - bestSingleBB.Value.Min;
          occPanelHalf = 0.5f * (MathF.Abs(singleN.X) * e.X
                               + MathF.Abs(singleN.Y) * e.Y
                               + MathF.Abs(singleN.Z) * e.Z);
        }
        // SIGNED touch band, calibrated from MEASURED distances (not assumed geometry):
        //   hu_session_20261027_101438 at lantern Z=-8.9631 -> Stage Panel d=0.319, side=1
        //   => the panel's near face is at Z=-9.282 (0.1129 assumption was wrong)
        //   => at the author's flip (Z=-9.015) the panel distance is ~0.267
        //   => panel wins at Z=-9.0200 (d~0.259), loses at Z=-9.0100 (d~0.269)
        // Upper bound 0.265 sits between those. Lower bound 0 (>= 0f) keeps the
        // behind-the-face case at Z=-8.6891 (d=-0.017) losing to the White Rect.
        // BEHIND-THE-PANEL RULE (v1.15.85). Pure class priority handled every case except
        // the item being BEHIND the Stage Panel, where the engine keeps it on the panel
        // (author: "it wont show me the stage panel snap behind the stage panel").
        //
        // This uses the SIGNED side of the panel's face, which is the one quantity that
        // behaved consistently (logged as sd=): negative = behind/inside the face.
        //   lantern 0.017 BEHIND the face -> sd=-0.017 -> engine chose White Rect
        // so a small negative is NOT enough; only clearly behind counts. -0.05 is the
        // smallest threshold consistent with that observation, and it restores the
        // behind-panel warning without reintroducing the touch-band oscillation (there is
        // still no rule for the FRONT side - class priority governs there).
        const float BEHIND_DEPTH = 0.37f;
        var panelSide = 0f;
        if (bestSingle <= limit && singleN != Vector3.Zero)
          panelSide = Vector3.Dot(singleN, probes[0] - singleP);
        occludedBySingle = panelSide < -BEHIND_DEPTH;
      }

      // HOLLOW MULTI-FACE MODEL (author, 2026-10-27):
      //   "you can think of each multi directional as HOLLOW, each wall on the partition
      //    surrounding it has a snap wall ~0.001 thick. If you move 1 away from that it
      //    wont snap in either direction as long as it would attach to that snap wall and
      //    its direction is lined up with it."
      // So the multi-face distance is the PERPENDICULAR distance to the winning face's snap
      // PLANE, valid on BOTH sides of it - not the ray parameter along the item's facing
      // axis, which grows without bound when the item faces away or is inside the hollow.
      // FINITE PATCH distance (v1.15.97). The snap wall is a bounded quad, not an infinite
      // plane. Using the plane distance ignored lateral offset, so a lantern rotated 70 deg
      // read as "0.3 from the plane" from yalms away along the wall and claimed snaps from
      // across the room (author report, 2026-10-27). Use the true point-to-patch distance,
      // which is what the triangle walk already computes; it is only stale when the winning
      // distance came from the AABB fallback, which the merge now corrects.
      // SNAP-POINT DISTANCE (v1.15.106). Author measurements, rotation 0, partition pivot Z=0.1183,
      // lantern attaches at Z=0.3683:
      //     lantern Z= 1.3787  -> 1.0104 from the attach point -> NO snap
      //     lantern Z=-0.6417  -> 1.0100 from the attach point -> NO snap
      // Both sides sit ~1.01 from the SAME point, so the engine measures from the snap point on
      // the face the ITEM FACES - not from the nearest face. Nearest-face read 0.37 from behind
      // and wrongly claimed a snap.
      if (!float.IsInfinity(multiPerp) && multiPerp < bestMulti) bestMulti = multiPerp;
      if (!float.IsInfinity(singlePerp) && singlePerp < bestSingle) bestSingle = singlePerp;

      // Replace with distance to the snap point on the faced side, when we have the receiver AABB.
      if (bestMultiBB.HasValue && !float.IsInfinity(bestMulti))
      {
        var bb = bestMultiBB.Value;
        var bbC = (bb.Min + bb.Max) * 0.5f;
        var bbE = (bb.Max - bb.Min) * 0.5f;
        var ax = MathF.Abs(itemFront.X); var ay = MathF.Abs(itemFront.Y); var az = MathF.Abs(itemFront.Z);
        Vector3 n, faceC;
        if (ax >= ay && ax >= az) { n = new Vector3(MathF.Sign(itemFront.X == 0 ? 1 : itemFront.X), 0, 0); faceC = new Vector3(bbC.X + bbE.X * n.X, bbC.Y, bbC.Z); }
        else if (ay >= az) { n = new Vector3(0, MathF.Sign(itemFront.Y == 0 ? 1 : itemFront.Y), 0); faceC = new Vector3(bbC.X, bbC.Y + bbE.Y * n.Y, bbC.Z); }
        else { n = new Vector3(0, 0, MathF.Sign(itemFront.Z == 0 ? 1 : itemFront.Z)); faceC = new Vector3(bbC.X, bbC.Y, bbC.Z + bbE.Z * n.Z); }
        const float ITEM_DEPTH = 0.13f;
        var attachPt = faceC - n * ITEM_DEPTH;
        var snapD = Vector3.Distance(probes[0], attachPt);
        if (snapD < bestMulti) bestMulti = snapD;
      }

      // Same model applies to SINGLE-face receivers (author, 2026-10-27: "the stage panel
      // and one faced things are the same, just only has one wall"); its point-to-patch
      // distance is applied above via singlePerp.

      if (occludedBySingle)
      {
        bestFurn = bestSingle;
        bestFurnName = bestSingleName;
        bestFurnBB = bestSingleBB;
        furnClass = "single-face (occludes multi)";
      }
      // NOTE: do NOT use multiPerp here. It is read from rGeomPerp, a shared scratch field
      // written during the triangle walk, and it is STALE within a frame - observed
      // reporting 0 for a receiver whose real distance was 4.661. Using it
      // (v1.15.79) let an out-of-range White Rect win and suppressed the warning for a
      // partition the lantern was touching (hu_session_20261027_111843). The ray distance
      // bestMulti is correct; oblique over-estimation is preferable to a false zero.
      else if (bestMulti <= limit)
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
        terrDiag.Append($"[bb={bbDist:0.##} tri={(float.IsInfinity(wallBest) ? "-" : wallBest.ToString("0.###"))} side={_wallGeomSide} sd={_wallGeomSigned:0.###}]");
        if (wallBest < bestTerr)
        {
          bestTerr = wallBest;
          bestTerrName = wallName;
          winN = _wallGeomNormal; winP = _wallGeomPoint; winT = _wallGeomT;
          winSide = _wallGeomSide; winSigned = _wallGeomSigned; winPerp = _wallGeomPerpT;
          winIsTerr = true; winName = wallName;
        }
      }

      // SURFACE -> ATTACH-POINT CORRECTION (v1.15.90).
      // The engine's capture is 1.0 measured from the ATTACH POINT - where the item comes
      // to rest when snapped - while this code measures the distance to the receiver's
      // wall surface. The two differ by the item's own wall offset.
      // Calibrated 2026-10-27 on a Blank Riviera Partition:
      //   partition pivot Z=0.1183, lantern settles at Z=0.3683 (offset +0.25)
      //   flip: lantern -0.631 -> 0.9993 from attach (snaps), -0.632 -> 1.0003 (doesn't)
      //   plugin surface distance D at that flip = 0.499  =>  surface->attach = 0.500
      // Applied to single-face receivers only, to avoid moving the multi-face thresholds
      // that are already confirmed working.
      // REMOVED (v1.15.96): the wallOffset add-back from 1.15.92 is no longer valid.
      // It existed because the distance was measured to the receiver's wall SURFACE while
      // the engine measures from the ATTACH point. The hollow-model change (1.15.93/94)
      // now measures the perpendicular distance to the snap PLANE itself, which IS the
      // engine's reference - so adding the offset double-counts. Observed at lantern
      // Z=-9.0131: White Rect at 0.773 (inside capture) + 0.5 = 1.273 -> wrongly "no snap".
      var wallOffset = 0f;
      if (_cachedActiveItemId != 0)
        SnapRangeData.TryGetWallThreshold(_cachedActiveItemId, out wallOffset);

      // STRICT comparison: the engine snaps at 0.99 but NOT at exactly 1.0 (author,
      // 2026-10-27). Using <= accepted the boundary itself and produced a false positive
      // at exactly the capture distance.
      var furnHit = bestFurn < limit;
      var terrHit = bestTerr < territoryLimit;
      var geomDiag = $"front=({itemFront.X:0.###},{itemFront.Y:0.###},{itemFront.Z:0.###}) " +
                     $"recv=[{string.Join(";", recvLog)}] " +
                     $"win={(winName.Length > 0 ? winName : "-")}{(winIsTerr ? "(terr)" : "")} " +
                     $"winN=({winN.X:0.###},{winN.Y:0.###},{winN.Z:0.###}) " +
                     $"winP=({winP.X:0.###},{winP.Y:0.###},{winP.Z:0.###}) " +
                     $"winT={winT:0.###} winSide={winSide} winSD={winSigned:0.###} winPerp={winPerp:0.###} " +
                     $"occDepth={occDepth:0.###} occPanelHalf={occPanelHalf:0.###} occBlocked={(occludedBySingle ? 1 : 0)}";
      WallSnapDiagnostic = $"back-ray check ({wallTris} wall tris{(usedAabbFallback ? ", aabb fallback" : "")}), nearObject id={nearId} d={(float.IsInfinity(nearDist) ? "-" : nearDist.ToString("0.###"))}, capture {capture:0.###} ({probes.Count} probes); furniture {(float.IsInfinity(bestFurn) ? "-" : bestFurn.ToString("0.###"))} (hit {(furnHit ? 1 : 0)}){(furnClass.Length > 0 ? ", " + furnClass : "")}{(bestFurnBB.HasValue ? $" bb=[{bestFurnBB.Value.Min.X:0.##},{bestFurnBB.Value.Min.Y:0.##},{bestFurnBB.Value.Min.Z:0.##}]-[{bestFurnBB.Value.Max.X:0.##},{bestFurnBB.Value.Max.Y:0.##},{bestFurnBB.Value.Max.Z:0.##}]" : "")}, territory {(float.IsInfinity(bestTerr) ? "-" : bestTerr.ToString("0.###"))} (hit {(terrHit ? 1 : 0)}){terrDiag} || {geomDiag}";

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

    /// <summary>
    /// Reverse wall check. When the held item is a wall receiver (partition, stage
    /// panel, ...), placed wall-mounted items re-attach to it when the housing
    /// layout commits — even though their own item was never touched. The engine
    /// uses the same front-facing rule: the wall item's front (+Z local) must point
    /// at the receiver, and the receiver's wall face must be inside the flat wall
    /// capture. Measured 2026-10-26: the Wall Lantern at Z=-8.50 re-attached to a
    /// White Rect between Z=-7.6594 and -7.5594 (inside the 1.0 capture).
    ///
    /// RULE (v1.15.62) — all conditions required:
    ///   0. the item is NOT already attached to the held receiver — either its pivot is
    ///      essentially ON the held receiver's surface (embedded) or it is physically
    ///      RIDING ALONG with it (same per-frame motion delta). Measured 2026-10-27:
    ///      the Wall Lantern tracked the dragged White Rect and the line kept claiming
    ///      "will snap here" for something already there;
    ///   1. the placed item's pivot is within the flat wall capture (1.0) of one of the
    ///      HELD receiver's own wall-flagged triangles, measured to the FAR face
    ///      (triangle distance + 2*faceHalf) — the v1.15.41 validated convention; and
    ///   2. the HELD receiver is the CLOSEST wall surface to that item by at least 1 cm
    ///      (<see cref="FindNearestWallReceiver"/>), otherwise the item simply stays
    ///      attached where it already is.
    ///
    /// The competitor lookup uses PLAIN POINT-TO-TRIANGLE distance, not the forward
    /// path's bidirectional ray: the ray returns a signed parameter that ranges over
    /// -0.1..0, so comparing ray values across receivers made the "nearest" flip every
    /// frame (-0.059 vs -0.000) and the line flickered off while an item was sliding in.
    ///
    /// The rule deliberately does not gate on the placed item's facing: the v1.15.57
    /// instrumentation showed a yaw-70 lantern (front (0.94,0,0.34), almost
    /// perpendicular to the partition's Z faces) attaching anyway, while the old facing
    /// gate flipped the line on +/-0.01 of `along` - off exactly where the engine
    /// attaches.
    ///
    /// The method also fills <see cref="ReverseWallFrameDiag"/> every frame with the
    /// nearest placed wall item's live transform, the new triangle distance, and the
    /// old facing verdict side by side, so the two models can be compared per frame.
    /// </summary>
    public unsafe void UpdateReverseWallSnapDetection()
    {
      LastReverseWallHit = false;
      LastReverseWallNames = "";
      ReverseWallDiagnostic = "(not computed)";
      ReverseWallFrameDiag = "(not computed)";

      if (HousingStructure == null || HousingStructure->ActiveItem == null ||
          HousingModule == null || HousingModule->GetCurrentManager() == null ||
          HousingModule->GetCurrentManager()->Objects == null)
      {
        ReverseWallDiagnostic = "no housing/active item";
        ReverseWallFrameDiag = "n=0 why=no-housing-or-active-item";
        return;
      }

      var active = HousingStructure->ActiveItem;
      var activeItemId = GetActiveItemFFXIVId();
      if (activeItemId == 0 || !SnapRangeData.IsWallReceiver(activeItemId))
      {
        ReverseWallDiagnostic = activeItemId == 0 ? "no active item id" : "active item is not a wall receiver";
        ReverseWallFrameDiag = activeItemId == 0 ? "n=0 why=no-active-item-id" : $"n=0 why=active-not-wall-receiver item={activeItemId}";
        return;
      }

      var config = Plugin.GetConfiguration();
      if (!config.ShowWallSnapWarning)
      {
        ReverseWallDiagnostic = "wall warnings disabled";
        ReverseWallFrameDiag = "n=0 why=warnings-disabled";
        return;
      }

      var capture = config.WallCaptureDistance;
      var activePos = active->Transform.Translation;
      var ob = default(OrientedBounds);
      active->GetOrientedBoundsImpl(&ob);
      var sideTolerance = MathF.Max(ob.HalfExtents.X, ob.HalfExtents.Z) + 0.5f;
      var faceHalf = MathF.Min(ob.HalfExtents.X, ob.HalfExtents.Z);
      var scanRadius = capture + sideTolerance + 2f;

      // ROLE-SWAPPED RECEIVER WALK. The held item is the receiver and each placed
      // wall item's pivot is the probe - the same wall-triangle machinery the forward
      // (verified) snap uses, with the roles exchanged. It is distance-based, so it
      // does not depend on the placed item's own facing.
      //
      // Why the facing-axis test is no longer the gate (measured from the v1.15.57
      // logs, 2026-10-26): the placed Wall Lantern sits at yaw 70, so its front is
      // (0.94, 0, 0.34) - nearly PERPENDICULAR to the partition's Z-facing surfaces.
      // Its front projection `along` therefore hovers at +/-0.01 and flips sign on
      // centimetre movement: the line toggled between -8.265 and -8.260 with
      // why=behind/ok alternating, i.e. it inverted exactly where the engine
      // attaches. The old geometry is still logged (a=/s=/g=/why=) for comparison.
      var activeChildPtrs = new List<nint>();
      var activeChildMats = new List<Matrix4x4>();
      {
        var ai = active->Instances.Instances;
        var childCount = (int)(ai.Last - ai.First);
        for (var c = 0; c < childCount; c++)
        {
          var cp = ai.First[c];
          if (cp.Value == null) continue;
          var ci = cp.Value->Instance;
          if (ci == null) continue;
          if (ci->Id.Type != InstanceType.BgPart) continue;
          activeChildPtrs.Add((nint)ci);
          activeChildMats.Add(BuildMatrix(*ci->GetTransformImpl()));
        }
      }
      // Read a little past the capture so the log shows how far outside a rejected
      // candidate actually was.
      var triLimit = capture * 2f;
      var triProbes = new List<Vector3>(1) { default };

      // Diagnostic accumulators: the nearest placed wall item, recorded even when
      // rejected, plus BOTH verdicts per candidate. candLog lists every candidate so
      // a name that is a false positive but not the nearest one still reaches the log
      // (that gap is why the Stage Panel report was not diagnosable from v1.15.58).
      var candLog = new List<string>(4);
      var compLog = new List<string>(4);
      var candCount = 0;
      var nearRank = float.PositiveInfinity;
      var nearName = "";
      var nearPos = default(Vector3);
      var nearFront = default(Vector3);
      var nearAlong = 0f;
      var nearSide = 0f;
      var nearGap = 0f;
      var nearReason = "";
      var nearTri = float.PositiveInfinity;
      var nearTriTris = 0;

      var targets = new List<(string Name, float Gap)>();
      for (var i = 0; i < 600; i++)
      {
        var addr = HousingModule->GetCurrentManager()->Objects[i];
        if (addr == 0) continue;
        var go = (GameObject*)addr;
        if (go->SharedGroupLayoutInstance == active) continue;

        var dist = Vector3.Distance(activePos, go->Position);
        if (dist > scanRadius) continue;

        var rowId = go->BaseId;
        if (!_furnitureRowCache.TryGetValue(rowId, out var cached))
        {
          uint itemId = 0;
          string name = "";
          uint category = 0;
          if (Plugin.TryGetFurnishing(rowId, out var f))
          {
            itemId = f.Item.Value.RowId;
            name = f.Item.Value.Name.ExtractText();
            category = (uint)f.HousingItemCategory;
          }
          else if (Plugin.TryGetYardObject(rowId, out var y))
          {
            itemId = y.Item.Value.RowId;
            name = y.Item.Value.Name.ExtractText();
            category = (uint)y.HousingItemCategory;
          }
          cached = (itemId, name);
          _furnitureRowCache[rowId] = cached;
          _furnitureCategoryCache[rowId] = category;
        }
        if (cached.ItemId == 0) continue;
        // The row cache is shared with the item/table/wall scans, so a row can be
        // cached without its HousingItemCategory. Resolve the category lazily on
        // its own so already-cached wall items are not skipped.
        if (!_furnitureCategoryCache.TryGetValue(rowId, out var cat15))
        {
          uint category = 0;
          if (Plugin.TryGetFurnishing(rowId, out var f2)) category = (uint)f2.HousingItemCategory;
          else if (Plugin.TryGetYardObject(rowId, out var y2)) category = (uint)y2.HousingItemCategory;
          _furnitureCategoryCache[rowId] = category;
          cat15 = category;
        }
        if (cat15 != 15) continue;

        var group = go->SharedGroupLayoutInstance;
        if (group == null) continue;

        // The item's world front is +Z local (same convention as the active wall snap).
        var front = Vector3.Transform(Vector3.UnitZ, group->Transform.Rotation);
        var goPos = new System.Numerics.Vector3(go->Position.X, go->Position.Y, go->Position.Z);

        // Already-attached detector: is this placed item following the held item?
        // If it moved by (approximately) the same delta, it is riding along and is
        // therefore already attached - nothing left to warn about.
        var riding = false;
        if (_revHaveActivePos && _revCandLastPos.TryGetValue((nint)group, out var prevCand))
        {
          var heldD = activePos - _revLastActivePos;
          var candD = goPos - prevCand;
          if (heldD.LengthSquared() > 4e-6f &&          // held moved > 2 mm
              candD.LengthSquared() > 1e-6f &&         // candidate moved at all
              (candD - heldD).LengthSquared() < 4e-4f) // same delta within 2 cm
            riding = true;
        }
        if (_revCandLastPos.Count > 64) _revCandLastPos.Clear();
        _revCandLastPos[(nint)group] = goPos;

        var toActive = activePos - goPos;
        var along = Vector3.Dot(front, toActive);

        var sideSq = Vector3.Dot(toActive, toActive) - along * along;
        var side = sideSq > 0f ? MathF.Sqrt(sideSq) : 0f;

        // Old facing-axis gap (v1.15.41 calibration). Retained for the log only.
        var gap = along + faceHalf;
        if (gap < 0f) gap = 0f;

        string reason;
        if (along <= 0f) reason = "behind";
        else if (side > sideTolerance) reason = "side";
        else if (gap > capture) reason = "far";
        else reason = "ok";

        // New verdict: distance from this placed item's pivot to the held receiver's
        // wall-flagged collision triangles. This is the quantity the engine actually
        // cares about - "is this wall item's pivot within wall capture of a wall
        // surface" - and it is blind to the placed item's rotation.
        var triBest = float.PositiveInfinity;
        var triTris = 0;
        if (activeChildPtrs.Count > 0)
        {
          triProbes[0] = goPos;
          var triAny = false;
          var triName = "";
          for (var c = 0; c < activeChildPtrs.Count; c++)
          {
            WalkWallTriangles(
              (BgPartsLayoutInstance*)activeChildPtrs[c], activeChildMats[c],
              triProbes, triLimit, "rel", ref triBest, ref triName, ref triTris,
              ref triAny, front);
          }
        }

        candCount++;
        if (candLog.Count < 4)
        {
          var shortName = cached.Name.Length > 18 ? cached.Name.Substring(0, 18) : cached.Name;
          candLog.Add($"{shortName}@{(float.IsInfinity(triBest) ? "-" : triBest.ToString("0.##"))}");
        }
        // Rank so that a candidate with a real triangle distance always outranks one
        // whose receiver has no readable wall triangles.
        var rank = float.IsPositiveInfinity(triBest) ? 1000f + MathF.Abs(along) : triBest;
        if (rank < nearRank)
        {
          nearRank = rank;
          nearName = cached.Name;
          nearPos = goPos;
          nearFront = front;
          nearAlong = along;
          nearSide = side;
          nearGap = gap;
          nearReason = reason;
          nearTri = triBest;
          nearTriTris = triTris;
        }

        // The triangle walk measures to the NEAREST wall triangle, i.e. the receiver's
        // NEAR face. The only reverse boundary ever validated in-game (v1.15.41: the
        // lantern at Z=-8.50 re-attached with the partition at along=0.8406 and did
        // not at along=0.9406, half-thickness 0.115) is expressed to the FAR face:
        // along + faceHalf <= 1.0. The far face is exactly 2*faceHalf beyond the near
        // face, so apply that correction here. Without it the line fires ~0.23 too
        // far out - which is the "still showing it too far out on the angle" report.
        var farGap = triBest + 2f * faceHalf;
        if (farGap > capture) continue;

        // (0) ALREADY ATTACHED to the held receiver. If the item is embedded in it
        // (pivot essentially on its wall surface) or physically riding along with it,
        // then it is already there and "will snap here" is wrong.
        if (triBest <= 0.02f || riding)
        {
          var why0 = riding ? "riding" : "embedded";
          if (compLog.Count < 4) compLog.Add($"{cached.Name}->already-{why0}(tri={triBest:0.###})");
          continue;
        }

        // COMPETITOR TEST: the item only re-attaches to the held receiver if that
        // receiver is the CLOSEST wall surface to it. Otherwise it stays where it
        // already is. This is exactly the case the 2026-10-26 log showed: the Wall
        // Lantern sat at wallD=0.000 on a Stage Panel for 960 frames while a White Rect
        // was dragged past it, and never moved - so warning about the White Rect was
        // wrong no matter how close it got.
        string compText;
        if (FindNearestWallReceiver(goPos, front, capture, (nint)group, 0,
              out var compDist, out var compName, out var compGroup, out var compTerritory))
        {
          compText = $"\"{compName}\"@{(compTerritory ? "terr" : compDist.ToString("0.###"))}";
          if (compLog.Count < 4) compLog.Add($"{cached.Name}->{compText}");
          // NOTE: this is DATA ONLY, it does NOT suppress. An earlier version suppressed
          // when a different receiver was closer, and that was wrong: the only validated
          // reverse boundary (v1.15.41) had the lantern sitting ON a wall at distance ~0
          // and still re-attaching to a partition 0.72 away. "Already attached to
          // something closer" therefore does NOT predict "will not move". Condition 0
          // (already attached to THE HELD RECEIVER) is the real predictor.
        }
        else
        {
          compText = "none";
          if (compLog.Count < 4) compLog.Add($"{cached.Name}->none");
        }

        targets.Add((cached.Name, farGap));
      }

      // Commit this frame's held position for next frame's rider test.
      _revLastActivePos = activePos;
      _revHaveActivePos = true;

      var geomDiag =
        $"ap=({activePos.X:0.###},{activePos.Y:0.###},{activePos.Z:0.###}) " +
        $"fh={faceHalf:0.###} sideTol={sideTolerance:0.###} cap={capture:0.###} " +
        $"kids={activeChildPtrs.Count} cands={string.Join(",", candLog)} " +
        $"comps={string.Join(",", compLog)}";
      var nearTriText = float.IsInfinity(nearTri) ? "-" : nearTri.ToString("0.###");
      ReverseWallFrameDiag = candCount == 0
        ? $"n=0 {geomDiag}"
        : $"n={candCount} near=\"{nearName}\" " +
          $"p=({nearPos.X:0.###},{nearPos.Y:0.###},{nearPos.Z:0.###}) " +
          $"f=({nearFront.X:0.###},{nearFront.Y:0.###},{nearFront.Z:0.###}) " +
          $"a={nearAlong:0.###} s={nearSide:0.###} g={nearGap:0.###} why={nearReason} " +
          $"tri={nearTriText} tris={nearTriTris} " +
          geomDiag;

      if (targets.Count == 0)
      {
        ReverseWallFrameDiag += " hits=none";
        ReverseWallDiagnostic = $"no wall item in capture; {ReverseWallFrameDiag}";
        return;
      }

      targets.Sort((a, b) => a.Gap.CompareTo(b.Gap));
      // Accepted targets with their far-face gap, so every name shown on the red line
      // (or wrongly shown) is attributable to a number in the log.
      ReverseWallFrameDiag += " hits=" + string.Join(",", targets.Take(3).Select(t => $"\"{t.Name}\"@{t.Gap:0.###}"));
      var names = string.Join(", ", targets.Take(3).Select(t => t.Name));
      if (targets.Count > 3) names += " +" + (targets.Count - 3);
      LastReverseWallHit = true;
      LastReverseWallNames = names;
      ReverseWallDiagnostic = $"{targets.Count} wall item(s) would re-attach: {names}; {ReverseWallFrameDiag}";
    }

    /// <summary>
    /// Nearest wall receiver for an arbitrary probe point, using the SAME rules as the
    /// forward wall check: wall-flagged collision triangles of nearby furniture
    /// (bidirectional facing-axis ray), receiver-class priority multi-face &gt;
    /// single-face, then the cached territory walls, with furniture beating territory.
    /// Returns the winning receiver's group pointer (0 for a territory wall) so a caller
    /// can ask "is the nearest receiver the item I am holding?".
    ///
    /// This exists for the reverse check. A placed wall item only MOVES onto the held
    /// receiver if that receiver is the closest wall surface to it; otherwise it stays
    /// where it already is. Measured 2026-10-26: the Wall Lantern sat at wallD=0.000 on a
    /// Stage Panel for 960 frames while a White Rect was dragged past it, and never moved.
    /// </summary>
    private unsafe bool FindNearestWallReceiver(
      Vector3 probe, Vector3 itemFront, float limit, nint skipGroupA, nint skipGroupB,
      out float dist, out string name, out nint group, out bool isTerritory,
      List<string>? screen = null)
    {
      dist = float.PositiveInfinity;
      name = "";
      group = 0;
      isTerritory = false;

      if (HousingModule == null || HousingModule->GetCurrentManager() == null ||
          HousingModule->GetCurrentManager()->Objects == null)
        return false;

      var probes = new List<Vector3>(1) { probe };
      var trTris = 0;

      float bestMulti = float.PositiveInfinity, bestSingle = float.PositiveInfinity;
      string bestMultiName = "", bestSingleName = "";
      nint bestMultiGroup = 0, bestSingleGroup = 0;

      for (var i = 0; i < 600; i++)
      {
        var addr = HousingModule->GetCurrentManager()->Objects[i];
        if (addr == 0) continue;
        var go = (GameObject*)addr;
        var gp = go->SharedGroupLayoutInstance;
        if (gp == null) continue;
        if ((nint)gp == skipGroupA || (nint)gp == skipGroupB) continue;
        if (Vector3.Distance(probe, go->Position) > 8f) continue;

        var rowId = go->BaseId;
        if (!_furnitureRowCache.TryGetValue(rowId, out var cached))
        {
          uint itemId = 0;
          string nm = "";
          if (Plugin.TryGetFurnishing(rowId, out var f))
          {
            itemId = f.Item.Value.RowId;
            nm = f.Item.Value.Name.ExtractText();
          }
          else if (Plugin.TryGetYardObject(rowId, out var y))
          {
            itemId = y.Item.Value.RowId;
            nm = y.Item.Value.Name.ExtractText();
          }
          cached = (itemId, nm);
          _furnitureRowCache[rowId] = cached;
        }
        if (cached.ItemId == 0) continue;

        var isMulti = SnapRangeData.IsMultiSidedWallReceiver(cached.ItemId);
        var itemInstances = gp->Instances.Instances;
        var itemSize = (int)(itemInstances.Last - itemInstances.First);
        var hadWallTris = false;
        var itemBest = float.PositiveInfinity;
        var itemName = "";

        for (int c = 0; c < itemSize; c++)
        {
          var childPtr = itemInstances.First[c];
          if (childPtr.Value == null) continue;
          var ci = childPtr.Value->Instance;
          if (ci == null || ci->Id.Type != InstanceType.BgPart) continue;
          // Child transforms are WORLD-space; do not compose the group transform.
          var childM = BuildMatrix(*ci->GetTransformImpl());
          // PLAIN EUCLIDEAN DISTANCE, not the forward path's bidirectional ray. The ray
          // returns a SIGNED parameter along the item's front axis (it is allowed to be
          // slightly negative, -0.1..0), so comparing ray values across receivers made
          // "which surface is nearest" flip every frame on values like -0.059 vs -0.000.
          // Point-to-triangle is non-negative and directly comparable, which is what
          // "what is this item attached to?" needs.
          WalkWallTriangles(
            (BgPartsLayoutInstance*)ci, childM, probes, limit, cached.Name,
            ref itemBest, ref itemName, ref trTris, ref hadWallTris,
            itemFront, acceptBackSide: false, acceptBothSides: false);
        }

        // Same embedded-census fallback the forward path uses.
        if (!hadWallTris && SnapRangeData.IsWallReceiver(cached.ItemId))
        {
          var union = GetItemColliderUnion(gp);
          if (union.HasValue)
          {
            var d = DistancePointToAABB(probe, union.Value);
            if (d < itemBest) { itemBest = d; itemName = cached.Name; }
          }
        }

        if (itemBest <= limit && screen != null)
          screen.Add($"{cached.Name}@{(isMulti ? "M" : "S")}{itemBest:0.###}");

        if (itemBest < float.PositiveInfinity)
        {
          if (isMulti)
          {
            if (itemBest < bestMulti) { bestMulti = itemBest; bestMultiName = itemName; bestMultiGroup = (nint)gp; }
          }
          else if (itemBest < bestSingle)
          {
            bestSingle = itemBest; bestSingleName = itemName; bestSingleGroup = (nint)gp;
          }
        }
      }

      // NEAREST physical surface, deliberately WITHOUT the forward path's
      // multi-face-over-single-face class priority. The question here is "what is this
      // item actually attached to?", and a single-face receiver at distance 0 beats a
      // multi-face one at 0.14. Inheriting the priority would pick the further
      // multi-face receiver and defeat the whole test - which is exactly the
      // White Rect (multi, 0.14) vs Stage Panel (single, 0.000) case.
      if (bestMulti <= bestSingle)
      {
        dist = bestMulti; name = bestMultiName; group = bestMultiGroup;
      }
      else
      {
        dist = bestSingle; name = bestSingleName; group = bestSingleGroup;
      }

      // Territory (house) walls.
      EnsureTerritoryWallCache(allowDeep: false);
      if (_territoryWallCache.Count == 0) EnsureTerritoryWallCache(allowDeep: true);

      var terrBest = float.PositiveInfinity;
      var terrName = "";
      foreach (var (inst, bb) in _territoryWallCache)
      {
        if (DistancePointToAABB(probe, bb) > limit) continue;
        var m = BuildMatrix(*((BgPartsLayoutInstance*)inst)->GetTransformImpl());
        var dummy = false;
        var wallBest = float.PositiveInfinity;
        var wallName = "";
        WalkWallTriangles((BgPartsLayoutInstance*)inst, m, probes, limit, "house wall",
          ref wallBest, ref wallName, ref trTris, ref dummy, itemFront, acceptBothSides: false);
        if (wallBest < terrBest) { terrBest = wallBest; terrName = wallName; }
        if (wallBest <= limit && screen != null) screen.Add($"house wall@{wallBest:0.###}");
      }

      if (terrBest < dist)
      {
        dist = terrBest;
        name = terrName;
        group = 0;
        isTerritory = true;
      }

      return dist <= limit;
    }

    private static Matrix4x4 BuildMatrix(FFXIVClientStructs.FFXIV.Client.LayoutEngine.Transform t) => t.Compose();

    /// <summary>
    /// Walks a BgPart's collision mesh and, for every wall-flagged triangle
    /// (bits 20..23 == 1), measures the distance from each probe point.
    /// House walls are distance-only (either side of the plane). Furniture false
    /// walls accept the back (anti-normal) side only.
    /// </summary>
    // DIAGNOSTIC scratch: geometry of the best wall-triangle hit for the last
    // WalkWallTriangles call. Written only when a call improves its own `best`, so it
    // always describes that call's winning triangle. Purely observational - no rule
    // reads it. Used to learn the mesh winding convention (which side is attachable).
    private static Vector3 _wallGeomNormal;
    private static Vector3 _wallGeomPoint;
    private static float _wallGeomT;
    private static int _wallGeomSide;   // -1 = hit along -front, +1 = along +front, 0 = plain distance
    private static float _wallGeomSigned; // dot(triangleNormal, probe - trianglePoint)
    private static float _wallGeomPerpT;  // unsigned point-to-triangle distance

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
          int side;
          if (acceptBothSides)
          {
            // Multi-sided receiver — MakePlace rule (Cutest, ported):
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
            if (backHit && frontHit)
            {
              if (tb <= tf) { t = tb; side = -1; } else { t = tf; side = 1; }
            }
            else if (backHit) { t = tb; side = -1; }
            else { t = tf; side = 1; }
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
            side = 0;
          }

          if (t < best)
          {
            best = t;
            bestName = name;
            // DIAGNOSTIC: capture the winning triangle's geometry. This is the data
            // needed to learn the mesh winding convention (which face is the attachable
            // one) without guessing - see WallSnapDiagnostic.
            _wallGeomNormal = n;
            _wallGeomPoint = a;
            _wallGeomT = t;
            _wallGeomSide = side;
            _wallGeomSigned = Vector3.Dot(n, probe - a);
            _wallGeomPerpT = DistancePointTriangle(probe, a, b, c);
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

        // Housing interface closed: the engine applies DEFERRED snaps HERE, not on
        // deselect. Confirmed by the author 2026-10-27: "it doesn't snap until I press
        // escape and exit the housing" (and tabletop items can need exiting and
        // re-entering the house). So THIS is the only valid moment to measure whether a
        // wall item actually attached - measuring at deselect reads pre-snap state and
        // makes every prediction look like a false positive.
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
                {
                  // Post-exit truth: how far is it from the nearest wall surface now?
                  var exitTruth = "";
                  try
                  {
                    var eg = go->SharedGroupLayoutInstance;
                    if (eg != null)
                    {
                      var ep = new Vector3(p.X, p.Y, p.Z);
                      var ef = Vector3.Transform(Vector3.UnitZ, eg->Transform.Rotation);
                      FindNearestWallReceiver(ep, ef, 2f, (nint)eg, 0,
                        out var end, out var enn, out _, out var eterr);
                      exitTruth = $" nearest=\"{enn}\"{(eterr ? "(terr)" : "")} nd={end:0.###} attached={(end <= 0.05f ? 1 : 0)}";
                    }
                  }
                  catch { }
                  SnapLogger.Event("housing_exit_snap",
                    $"baseId={go->BaseId} before=[{prev.X:0.####},{prev.Y:0.####},{prev.Z:0.####}] after=[{p.X:0.####},{p.Y:0.####},{p.Z:0.####}]{exitTruth}");
                }
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
            // Freeze the card's last claim for the snap_truth comparison.
            _predWallHit = LastWallSnapHit;
            _predWallTarget = LastWallSnapTargetName;
            _predWallDist = LastWallSnapDistance;
            _predWallIsTerritory = LastWallSnapTargetIsTerritory;
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
                  // SNAP TRUTH: compare what the card predicted against where the item
                  // actually ended up. `attached=1` means the placed item settled on a
                  // wall surface (nearest receiver distance <= 5 cm), which is what a
                  // real wall snap looks like. This removes all ambiguity from testing.
                  var truthDiag = "";
                  try
                  {
                    var pg = go->SharedGroupLayoutInstance;
                    if (pg != null)
                    {
                      var pp = new Vector3(go->Position.X, go->Position.Y, go->Position.Z);
                      var pf = Vector3.Transform(Vector3.UnitZ, pg->Transform.Rotation);
                      FindNearestWallReceiver(pp, pf, 2f, (nint)pg, 0,
                        out var nd, out var nn, out _, out var isTerr);
                      var attached = nd <= 0.05f ? 1 : 0;
                      // PRE-SNAP ONLY: the engine applies wall snaps when the housing
                      // interface EXITS, not on deselect, so this reading is taken before
                      // the snap and is NOT the verdict. The authoritative one is on
                      // housing_exit_snap. Named explicitly so it cannot be misread.
                      truthDiag = $" predicted={(_predWallHit ? 1 : 0)} predTarget=\"{_predWallTarget}\" " +
                                  $"predD={_predWallDist:0.###} predTerr={(_predWallIsTerritory ? 1 : 0)} " +
                                  $"nearest=\"{nn}\"{(isTerr ? "(terr)" : "")} ndPreSnap={nd:0.###} preSnapAttached={attached} " +
                                  $"(NOT the verdict - snaps apply on housing exit)";
                    }
                  }
                  catch { }
                  SnapLogger.Event("POSTSNAP_FINAL",
                    $"item={_pendingPostSnapItemId} before=[{_lastActiveGroupPos.X:0.####},{_lastActiveGroupPos.Y:0.####},{_lastActiveGroupPos.Z:0.####}] " +
                    $"after=[{p.X:0.####},{p.Y:0.####},{p.Z:0.####}] delta=[{dx:0.####},{dy:0.####},{dz:0.####}] mag={MathF.Sqrt(dx * dx + dy * dy + dz * dz):0.####}{truthDiag}");
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
                  {
                    // THIS is where the deferred wall snap actually lands: re-entering the
                    // house re-instances placed items and the engine resolves the attach
                    // here (confirmed 2026-10-27 - the lantern sat 0.25-0.33 off the wall
                    // on deselect and moved onto the wall on the next entry). Log the
                    // resulting attachment state so it can be compared to the prediction.
                    var lcTruth = "";
                    try
                    {
                      var lg = go->SharedGroupLayoutInstance;
                      if (lg != null)
                      {
                        var lp = new Vector3(p.X, p.Y, p.Z);
                        var lf = Vector3.Transform(Vector3.UnitZ, lg->Transform.Rotation);
                        var lcCands = new List<string>(8);
                        FindNearestWallReceiver(lp, lf, 2f, (nint)lg, 0,
                          out var lnd, out var lnn, out _, out var lterr, lcCands);
                        lcTruth = $" nearest=\"{lnn}\"{(lterr ? "(terr)" : "")} nd={lnd:0.###} attached={(lnd <= 0.05f ? 1 : 0)}" +
                                  $" predHit={(_predWallHit ? 1 : 0)} predTarget=\"{_predWallTarget}\" predD={_predWallDist:0.###} " +
                                  $"MATCH={((_predWallHit && lnn == _predWallTarget) ? 1 : 0)}" +
                                  $" cands=[{string.Join(";", lcCands)}]";
                      }
                    }
                    catch { }
                    SnapLogger.Event("layout_change",
                      $"baseId={go->BaseId} before=[{prev.X:0.####},{prev.Y:0.####},{prev.Z:0.####}] after=[{p.X:0.####},{p.Y:0.####},{p.Z:0.####}]{lcTruth}");
                  }
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
