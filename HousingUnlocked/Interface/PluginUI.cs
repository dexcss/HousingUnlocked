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
      // Snap logger auto start/stop with the main window (manual /hu log overrides).
      if (Main.IsOpen)
      {
        if (!SnapLogger.Enabled && !SnapLogger.ManualOverride)
          SnapLogger.Start("main window opened");
      }
      else if (SnapLogger.Enabled && !SnapLogger.ManualOverride)
        SnapLogger.Stop("main window closed");

      Gizmo.Draw();
      Windows.Draw();
    }
  }
}
