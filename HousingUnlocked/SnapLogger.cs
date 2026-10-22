using System;
using System.IO;
using System.Text;

namespace HousingUnlocked
{
  /// <summary>
  /// JSONL session logger for in-game snap ground truth.
  /// Starts automatically when the main window opens (or /hu log), stops on close.
  /// /hu mark writes a labelled marker so snap moments can be matched to traces.
  /// Frame lines are throttled: written when the situation CHANGES (position, hits,
  /// distances) or at most every 250 ms otherwise.
  /// </summary>
  public static class SnapLogger
  {
    private static StreamWriter? _writer;
    private static string? _file;
    private static long _lines;
    private static string _lastSig = "";
    private static DateTime _lastFrameWrite = DateTime.MinValue;

    public static bool Enabled => _writer != null;
    public static string? CurrentFile => _file;
    public static bool ManualOverride { get; set; }

    public static string LogDirectory
    {
      get
      {
        try
        {
          var di = Plugin.PluginInterface.ConfigDirectory;
          if (di != null && !string.IsNullOrEmpty(di.FullName))
            return Path.Combine(di.FullName, "logs");
        }
        catch { }
        return Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
          "XIVLauncher", "pluginConfigs", "HousingUnlocked", "logs");
      }
    }

    public static void Start(string reason)
    {
      if (_writer != null) return;
      try
      {
        Directory.CreateDirectory(LogDirectory);
        _file = Path.Combine(LogDirectory, $"hu_session_{DateTime.Now:yyyyMMdd_HHmmss}.jsonl");
        _writer = new StreamWriter(_file, append: false, Encoding.UTF8) { AutoFlush = true };
        _lines = 0;
        _lastSig = "";
        WriteRaw("session", $"\"reason\":\"{Esc(reason)}\",\"log_path\":\"{Esc(_file)}\"");
        Plugin.Chat.Print($"[HU] Logging started: {_file}");
      }
      catch (Exception e)
      {
        Plugin.Log.Error(e, "SnapLogger.Start failed");
        _writer = null;
      }
    }

    public static void Stop(string reason)
    {
      if (_writer == null) return;
      WriteRaw("session_end", $"\"reason\":\"{Esc(reason)}\",\"lines\":{_lines}");
      _writer.Dispose();
      _writer = null;
      Plugin.Chat.Print("[HU] Logging stopped.");
    }

    /// <summary>User label for a snap moment — always written immediately.</summary>
    public static void Mark(string note, float x, float y, float z)
    {
      if (_writer == null) Start("mark before start");
      WriteRaw("MARK", $"\"note\":\"{Esc(note)}\",\"pos\":[{R(x)},{R(y)},{R(z)}]");
    }

    /// <summary>Generic event (cache builds, warnings toggles, ...). No-op when disabled.</summary>
    public static void Event(string name, string detail)
    {
      if (_writer == null) return;
      WriteRaw(name, $"\"detail\":\"{Esc(detail)}\"");
    }

    /// <summary>
    /// Per-frame capture. Writes only when the situation changed (signature) or
    /// at least 250 ms passed since the last line.
    /// </summary>
    public static void Frame(
      uint itemId, int category, float px, float py, float pz, float yaw, int dir,
      uint mode, uint state, uint state2, bool rotating, bool placeAnywhere,
      bool isFloorSnap, bool floorHit, float floorD, bool ceilHit, float ceilD,
      float perItemThreshold, float effThreshold, bool inSnapZone, bool snapsUp,
      bool itemHit, string itemName, float itemD, float itemRange,
      bool wallHit, string wallName, float wallD, float wallThr, bool wallTerritory, string wallDiag,
      bool tableHit, string tableName, float tableDy, string tableDiag,
      string scrDiag, string itemSnapDiag)
    {
      if (_writer == null) return;

      var sig = $"{R(px)}|{R(py)}|{R(pz)}|{R(yaw)}|{dir}|{inSnapZone}|{snapsUp}|{itemHit}|{R(itemD)}|{wallHit}|{R(wallD)}|{wallTerritory}|{tableHit}|{R(tableDy)}|{state}|{R(perItemThreshold)}";
      var now = DateTime.Now;
      var stale = (now - _lastFrameWrite).TotalMilliseconds > 250;
      if (sig == _lastSig && !stale) return;
      _lastSig = sig;
      _lastFrameWrite = now;

      WriteRaw("frame",
        $"\"item\":{itemId},\"cat\":{category}," +
        $"\"pos\":[{R(px)},{R(py)},{R(pz)}],\"yaw\":{R(yaw)},\"dir\":{dir}," +
        $"\"mode\":{mode},\"state\":{state},\"state2\":{state2},\"rot\":{(rotating ? 1 : 0)},\"pa\":{(placeAnywhere ? 1 : 0)}," +
        $"\"floorSnap\":{(isFloorSnap ? 1 : 0)},\"floorHit\":{(floorHit ? 1 : 0)},\"floorD\":{Jn(floorD)},\"ceilHit\":{(ceilHit ? 1 : 0)},\"ceilD\":{Jn(ceilD)}," +
        $"\"thr\":{R(perItemThreshold)},\"eff\":{R(effThreshold)},\"inZone\":{(inSnapZone ? 1 : 0)},\"snapUp\":{(snapsUp ? 1 : 0)}," +
        $"\"itemHit\":{(itemHit ? 1 : 0)},\"itemTarget\":\"{Esc(itemName)}\",\"itemD\":{Jn(itemD)},\"itemRange\":{Jn(itemRange)}," +
        $"\"wallHit\":{(wallHit ? 1 : 0)},\"wallTarget\":\"{Esc(wallName)}\",\"wallD\":{Jn(wallD)},\"wallThr\":{R(wallThr)},\"wallTerritory\":{(wallTerritory ? 1 : 0)},\"wallDiag\":\"{Esc(wallDiag)}\"," +
        $"\"tableHit\":{(tableHit ? 1 : 0)},\"tableTarget\":\"{Esc(tableName)}\",\"tableDy\":{Jn(tableDy)},\"tableDiag\":\"{Esc(tableDiag)}\"," +
        $"\"thrSrc\":\"{Esc(scrDiag)}\",\"itemDiag\":\"{Esc(itemSnapDiag)}\"");
    }

    private static void WriteRaw(string type, string fields)
    {
      _writer?.WriteLine($"{{\"t\":\"{type}\",\"ts\":\"{DateTime.Now:HH:mm:ss.fff}\",\"line\":{++_lines},{fields}}}");
    }

    private static string R(float f) => MathF.Round(f, 4).ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
    private static string Jn(float f) => float.IsInfinity(f) || float.IsNaN(f) ? "null" : R(f);
    private static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
  }
}
