using System.Numerics;

using Dalamud.Bindings.ImGui;

namespace HousingUnlocked.Interface
{
  /// <summary>
  /// Fancy UI tokens for Housing Unlocked: purple, blocky, near-black window. Every metric is
  /// multiplied by the game's UI scale. Widgets must read the radii from here, never hardcode.
  /// </summary>
  public static class Theme
  {
    // ---- palette: purple default ----------------------------------------
    public static readonly Vector4 WindowBg   = new(0.027f, 0.024f, 0.043f, 0.96f); // #07060B
    public static readonly Vector4 Surface    = new(0.110f, 0.090f, 0.188f, 1.00f); // #1C1730
    public static readonly Vector4 SurfaceAlt = new(0.153f, 0.125f, 0.251f, 1.00f); // #272040
    public static readonly Vector4 Border     = new(0.294f, 0.227f, 0.478f, 0.60f); // #4B3A7A

    public static readonly Vector4 Accent     = new(0.608f, 0.420f, 1.000f, 1.00f); // #9B6BFF violet
    public static readonly Vector4 AccentSoft = new(0.608f, 0.420f, 1.000f, 0.30f);
    public static readonly Vector4 Text       = new(0.914f, 0.894f, 0.980f, 1.00f); // #E9E4FA
    public static readonly Vector4 Muted      = new(0.604f, 0.561f, 0.745f, 1.00f); // #9A8FBE

    // ---- states ---------------------------------------------------------
    public static readonly Vector4 Good       = new(0.369f, 0.878f, 0.784f, 1.00f); // #5EE0C8 teal
    public static readonly Vector4 GoodSoft   = new(0.369f, 0.878f, 0.784f, 0.14f);
    public static readonly Vector4 Warn       = new(1.000f, 0.420f, 0.839f, 1.00f); // #FF6BD6 magenta
    public static readonly Vector4 WarnSoft   = new(1.000f, 0.420f, 0.839f, 0.14f);
    public static readonly Vector4 Danger     = new(1.000f, 0.361f, 0.424f, 1.00f);

    // ---- coordinate axes (kept distinguishable) -------------------------
    public static readonly Vector4 AxisX      = new(1.000f, 0.478f, 0.741f, 1.00f);
    public static readonly Vector4 AxisY      = new(0.369f, 0.878f, 0.784f, 1.00f);
    public static readonly Vector4 AxisZ      = new(0.608f, 0.420f, 1.000f, 1.00f);

    // ---- shape ----------------------------------------------------------
    /// <summary>1 = blocky default. Raise for softer/pill controls; widgets read the radii.</summary>
    public static float Roundness = 1f;

    public static float UiScale => ImGui.GetIO().FontGlobalScale;
    public static float RadiusCard => 4f * Roundness * UiScale;
    public static float RadiusControl => 3f * Roundness * UiScale;
    public static float RadiusTag => 2f * Roundness * UiScale;
    public static float Pad => 12f * UiScale;
    public static float Gap => 8f * UiScale;

    public static uint U32(Vector4 c) => ImGui.GetColorU32(c);

    private static readonly (ImGuiCol Col, Vector4 Value)[] Styles =
    {
      (ImGuiCol.WindowBg,              WindowBg),
      (ImGuiCol.PopupBg,               Surface),
      (ImGuiCol.Border,                Border),
      (ImGuiCol.FrameBg,               SurfaceAlt),
      (ImGuiCol.FrameBgHovered,        new Vector4(0.196f, 0.161f, 0.310f, 1f)),
      (ImGuiCol.FrameBgActive,         new Vector4(0.227f, 0.188f, 0.353f, 1f)),
      (ImGuiCol.TitleBg,               Surface),
      (ImGuiCol.TitleBgActive,         SurfaceAlt),
      (ImGuiCol.CheckMark,             Accent),
      (ImGuiCol.SliderGrab,            Accent),
      (ImGuiCol.SliderGrabActive,      Accent),
      (ImGuiCol.Button,                SurfaceAlt),
      (ImGuiCol.ButtonHovered,         new Vector4(0.278f, 0.231f, 0.435f, 1f)),
      (ImGuiCol.ButtonActive,          Accent),
      (ImGuiCol.Header,                AccentSoft),
      (ImGuiCol.HeaderHovered,         new Vector4(0.608f, 0.420f, 1.000f, 0.45f)),
      (ImGuiCol.HeaderActive,          AccentSoft),
      (ImGuiCol.Separator,             Border),
      (ImGuiCol.ScrollbarBg,           Surface),
      (ImGuiCol.ScrollbarGrab,         SurfaceAlt),
      (ImGuiCol.ScrollbarGrabHovered,  new Vector4(0.278f, 0.231f, 0.435f, 1f)),
      (ImGuiCol.ScrollbarGrabActive,   Accent),
      (ImGuiCol.Tab,                   Surface),
      (ImGuiCol.TabHovered,            AccentSoft),
      (ImGuiCol.Text,                  Text),
      (ImGuiCol.TextDisabled,          Muted),
      (ImGuiCol.TextSelectedBg,        AccentSoft),
      (ImGuiCol.PlotLines,             Accent),
      (ImGuiCol.PlotHistogram,         Accent),
    };

    public static void Push()
    {
      foreach (var (col, value) in Styles)
        ImGui.PushStyleColor(col, value);
    }

    public static void Pop() => ImGui.PopStyleColor(Styles.Length);
  }
}
