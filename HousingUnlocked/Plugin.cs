using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

using HousingUnlocked.Interface;
using Dalamud.Interface.Textures;

namespace HousingUnlocked
{
  public class Plugin : IDalamudPlugin
  {
    private const string commandName = "/hu";

    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static IDataManager Data { get; private set; } = null!;
    [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] public static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static IChatGui Chat { get; private set; } = null!;
    [PluginService] public static IGameGui GameGui { get; private set; } = null!;
    [PluginService] public static ISigScanner TargetModuleScanner { get; private set; } = null!;
    [PluginService] public static ICondition Condition { get; private set; } = null!;
    [PluginService] public static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] public static IPluginLog Log { get; private set; } = null!;

    private static Configuration _configuration = null!;
    private static PluginUI _ui = null!;
    private static PluginMemory _memory = null!;

    // Sheets used to get housing item info.
    private static Dictionary<uint, HousingFurniture> _furnitureDict = [];
    private static Dictionary<uint, HousingYardObject> _yardObjectDict = [];

    public Plugin()
    {
      _configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
      _ui = new();
      _memory = new();

      _furnitureDict = Data.GetExcelSheet<HousingFurniture>()!.ToDictionary(row => row.RowId, row => row);
      _yardObjectDict = Data.GetExcelSheet<HousingYardObject>()!.ToDictionary(row => row.RowId, row => row);

      CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
      {
        HelpMessage = "Opens the controls for Housing Unlocked.\n/hu autoopen → toggles auto-open when entering housing mode.\n/hu threshold <value> → safety margin for formula-derived snap distances (default 0.07).\n/hu itemsnap → toggles the item-to-item snap warning.\n/hu itemsnapmode <either|sum|placed> → which side's snap range drives item-to-item distance.\n/hu tablesnap → toggles the table snap warning.\n/hu wallsnap → toggles the wall snap warning.\n/hu wallsrefresh → rebuilds the house-wall surface cache.\n/hu log → toggles the snap session logger.\n/hu mark <note> → labels this moment in the log.\n/hu logpath → shows where session logs are saved."
      });

      // Set the ImGui context 
      ImGuizmo.SetImGuiContext(ImGui.GetCurrentContext());

      PluginInterface.UiBuilder.Draw += _ui.Draw;
      PluginInterface.UiBuilder.OpenMainUi += OpenMainUI;
      Condition.ConditionChange += Condition_ConditionChange;
      Framework.Update += Framework_Update;
    }

    public static Configuration GetConfiguration()
    {
      return _configuration;
    }

    public static PluginMemory GetMemory()
    {
      return _memory;
    }

    private void Framework_Update(IFramework framework)
    {
      _memory.Update();
    }

    private void Condition_ConditionChange(ConditionFlag flag, bool value)
    {
      if (_configuration.AutoVisible && flag == ConditionFlag.UsingHousingFunctions)
        _ui.Main.IsOpen = value;
    }

    public void Dispose()
    {
      PluginInterface.UiBuilder.Draw -= _ui.Draw;

      PluginInterface.UiBuilder.OpenMainUi -= OpenMainUI;
      Condition.ConditionChange -= Condition_ConditionChange;
      Framework.Update -= Framework_Update;

      // Dispose for stuff in Plugin Memory class.
      _memory.Dispose();

      CommandManager.RemoveHandler(commandName);

      GC.SuppressFinalize(this);
    }

    private void OpenMainUI()
    {
      _ui.Main.IsOpen = true;
    }

    /// <summary>
    /// Draws icon from game data.
    /// </summary>
    /// <param name="icon"></param>
    /// <param name="size"></param>
    public static void DrawIcon(ushort icon, Vector2 size)
    {
      if (icon < 65000)
      {
        var iconTexture = TextureProvider.GetFromGameIcon(new GameIconLookup(icon));
        ImGui.Image(iconTexture.GetWrapOrEmpty().Handle, size);
      }
    }

    public static unsafe bool IsOutdoors() => _memory.HousingModule->OutdoorTerritory != null;

    public static bool TryGetFurnishing(uint id, out HousingFurniture furniture) => _furnitureDict.TryGetValue(id, out furniture);
    public static bool TryGetYardObject(uint id, out HousingYardObject furniture) => _yardObjectDict.TryGetValue(id, out furniture);

    private unsafe void OnCommand(string command, string args)
    {
      args = args.Trim().ToLower();

      // Arguments are being passed in.
      if (!string.IsNullOrEmpty(args))
      {
        // Split the arguments into an array.
        var argArray = args.Split(' ');

        // Check valid state for modifying memory.
        var disabled = !(_memory.CanEditItem() && _memory.HousingStructure->ActiveItem != null);

        // Show/Hide the furnishing list.
        if (argArray.Length == 1)
        {
          var opt = argArray[0].ToLower();
          if (opt.Equals("list"))
          {
            // Only allow furnishing list when the housing window is open.
            if (!_memory.IsHousingOpen())
            {
              Chat.PrintError("Cannot open furnishing list unless housing menu is open.");
              _ui.Furniture.IsOpen = false;
              return;
            }

            // Disallow the ability to open furnishing list outdoors.
            if (IsOutdoors())
            {
              Chat.PrintError("Cannot open furnishing outdoors currently.");
              _ui.Furniture.IsOpen = false;
              return;
            }

            _ui.Furniture.Toggle();
          }

          if (opt.Equals("debug"))
            _ui.Debug.Toggle();

          if (opt.Equals("reset"))
            _ui.Main.Reset = true;

          if (opt.Equals("autoopen"))
          {
            _configuration.AutoVisible = !_configuration.AutoVisible;
            _configuration.Save();
            Chat.Print($"[HU] Auto-open is now {(_configuration.AutoVisible ? "ON" : "OFF")}.");
          }

          if (opt.Equals("itemsnap"))
          {
            _configuration.ShowItemSnapWarning = !_configuration.ShowItemSnapWarning;
            _configuration.Save();
            Chat.Print($"[HU] Item-to-item snap warning is now {(_configuration.ShowItemSnapWarning ? "ON" : "OFF")}.");
          }

          if (opt.Equals("tablesnap"))
          {
            _configuration.ShowTableSnapWarning = !_configuration.ShowTableSnapWarning;
            _configuration.Save();
            Chat.Print($"[HU] Table snap warning is now {(_configuration.ShowTableSnapWarning ? "ON" : "OFF")}.");
          }

          if (opt.Equals("wallsnap"))
          {
            _configuration.ShowWallSnapWarning = !_configuration.ShowWallSnapWarning;
            _configuration.Save();
            Chat.Print($"[HU] Wall snap warning is now {(_configuration.ShowWallSnapWarning ? "ON" : "OFF")}.");
          }

          if (opt.Equals("wallsrefresh"))
          {
            _memory.InvalidateTerritoryWallCache();
            Chat.Print("[HU] Territory wall cache invalidated — it will rebuild on the next wall item selection.");
          }

          if (opt.Equals("log"))
          {
            SnapLogger.ManualOverride = true;
            if (SnapLogger.Enabled) SnapLogger.Stop("/hu log toggle");
            else SnapLogger.Start("/hu log toggle");
          }

          if (opt.Equals("logpath"))
          {
            Chat.Print($"[HU] Log directory: {SnapLogger.LogDirectory}");
            if (SnapLogger.Enabled) Chat.Print($"[HU] Current session file: {SnapLogger.CurrentFile}");
          }

          if (argArray.Length >= 2 && opt.Equals("mark"))
          {
            var note = string.Join(' ', argArray[1..]);
            var p = _memory.Position;
            SnapLogger.Mark(note, p.X, p.Y, p.Z);
            Chat.Print($"[HU] Marked: {note}");
          }
          else if (opt.Equals("mark"))
          {
            var p = _memory.Position;
            SnapLogger.Mark("snap", p.X, p.Y, p.Z);
            Chat.Print("[HU] Marked: snap");
          }
        }

        // Two-argument subcommands: e.g. "threshold 0.05"
        if (argArray.Length == 2)
        {
          var opt = argArray[0].ToLower();
          if (opt.Equals("threshold"))
          {
            if (float.TryParse(argArray[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var t) && t >= 0 && t <= 5)
            {
              _configuration.SnapThreshold = t;
              _configuration.Save();
              Chat.Print($"[HU] Snap warning safety margin set to {t:0.###}.");
            }
            else
            {
              Chat.PrintError("[HU] Threshold must be a number between 0 and 5 (e.g. 0.05).");
            }
          }

          if (opt.Equals("itemsnapmode"))
          {
            switch (argArray[1].ToLower())
            {
              case "either":
                _configuration.ItemSnapRangeMode = ItemSnapRangeMode.Either;
                break;
              case "sum":
                _configuration.ItemSnapRangeMode = ItemSnapRangeMode.Sum;
                break;
              case "placed":
                _configuration.ItemSnapRangeMode = ItemSnapRangeMode.PlacedOnly;
                break;
              default:
                Chat.PrintError("[HU] itemsnapmode must be either, sum, or placed.");
                return;
            }
            _configuration.Save();
            Chat.Print($"[HU] Item snap range mode set to {_configuration.ItemSnapRangeMode}.");
          }
        }

        // Position or rotation values are being passed in, and we're not disabled.
        if (argArray.Length >= 3 && !disabled)
        {
          try
          {
            // Parse the coordinates into floats.
            var x = float.Parse(argArray[0], NumberStyles.Any, CultureInfo.InvariantCulture);
            var y = float.Parse(argArray[1], NumberStyles.Any, CultureInfo.InvariantCulture);
            var z = float.Parse(argArray[2], NumberStyles.Any, CultureInfo.InvariantCulture);

            // Set the position in the memory object.
            _memory.Position.X = x;
            _memory.Position.Y = y;
            _memory.Position.Z = z;

            // Write the position.
            _memory.WritePosition(_memory.Position);

            // Specifying the rotation as well.
            if (argArray.Length == 4)
            {
              // Parse and write the rotation.
              _memory.Rotation.Y = (float)(float.Parse(argArray[3], NumberStyles.Any, CultureInfo.InvariantCulture) * 180 / Math.PI);
              _memory.WriteRotation(_memory.Rotation);
            }
          }
          catch (Exception ex)
          {
            Log.Error(ex, "Error when positioning with command");
          }
        }
      }
      else
      {
        // Hide or show the UI.
        _ui.Main.Toggle();
      }
    }
  }
}
