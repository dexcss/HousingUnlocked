using HousingUnlocked.Interface.Windows;

using Dalamud.Interface.Windowing;

namespace HousingUnlocked.Interface
{
  public class PluginUI
  {
    private readonly WindowSystem Windows = new("HousingUnlocked");
    
    private readonly Gizmo Gizmo = new();

    public readonly MainWindow Main;
    public readonly DebugWindow Debug = new();
    public readonly FurnitureList Furniture = new();

    public PluginUI()
    {
      Main = new MainWindow(Gizmo);
      Windows.AddWindow(Main);
      Windows.AddWindow(Debug);
      Windows.AddWindow(Furniture);
    }

    public void Draw()
    {
      // Session logging is opt-in (Configuration.AutoStartSessionLog). By default no log
      // file is created; /hu log or the Advanced button still starts one on demand.
      if (Plugin.GetConfiguration().AutoStartSessionLog)
      {
        if (Main.IsOpen)
        {
          if (!SnapLogger.Enabled && !SnapLogger.ManualOverride)
            SnapLogger.Start("main window opened");
        }
        else if (SnapLogger.Enabled && !SnapLogger.ManualOverride)
          SnapLogger.Stop("main window closed");
      }

      // Only build the long wallDiag / itemDiag strings while something reads them.
      PluginMemory.VerboseDiagnostics = Debug.IsOpen || SnapLogger.Enabled;

      Gizmo.Draw();
      Windows.Draw();
    }
  }
}
