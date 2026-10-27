using System.Numerics;

using Dalamud.Bindings.ImGui;

namespace HousingUnlocked.Interface
{
  /// <summary>
  /// The plugin's visual language: deep indigo surfaces with a periwinkle accent, teal for
  /// "good", magenta for "will snap". Deliberately not the default ImGui grey, and not the
  /// stock plugin red — the warning still reads as alarming, but the palette is its own.
  /// </summary>
  public static class Theme
  {
    // Base surfaces
    public static readonly Vector4 WindowBg   = new(0.071f, 0.063f, 0.118f, 0.97f);
    public static readonly Vector4 Surface    = new(0.110f, 0.098f, 0.173f, 1.00f);
    public static readonly Vector4 SurfaceAlt = new(0.153f, 0.137f, 0.235f, 1.00f);
    public static readonly Vector4 Border     = new(0.310f, 0.255f, 0.545f, 0.55f);

    // Accent + text
    public static readonly Vector4 Accent     = new(0.639f, 0.549f, 0.980f, 1.00f); // periwinkle
    public static readonly Vector4 AccentSoft = new(0.639f, 0.549f, 0.980f, 0.30f);
    public static readonly Vector4 Text       = new(0.906f, 0.890f, 0.973f, 1.00f);
    public static readonly Vector4 Muted      = new(0.596f, 0.565f, 0.706f, 1.00f);

    // States
    public static readonly Vector4 Good       = new(0.412f, 0.886f, 0.867f, 1.00f); // teal
    public static readonly Vector4 GoodSoft   = new(0.412f, 0.886f, 0.867f, 0.14f);
    public static readonly Vector4 Warn       = new(0.937f, 0.408f, 0.812f, 1.00f); // magenta
    public static readonly Vector4 WarnSoft   = new(0.937f, 0.408f, 0.812f, 0.14f);

    // Coordinate axes (kept distinguishable, retoned to the palette)
    public static readonly Vector4 AxisX      = new(0.937f, 0.478f, 0.741f, 1.00f);
    public static readonly Vector4 AxisY      = new(0.412f, 0.886f, 0.867f, 1.00f);
    public static readonly Vector4 AxisZ      = new(0.639f, 0.549f, 0.980f, 1.00f);

    public static uint U32(Vector4 c) => ImGui.GetColorU32(c);

    private static readonly (ImGuiCol Col, Vector4 Value)[] Styles =
    {
      (ImGuiCol.WindowBg,                WindowBg),
      (ImGuiCol.ChildBg,                 new Vector4(0.086f, 0.075f, 0.137f, 0.55f)),
      (ImGuiCol.PopupBg,                 Surface),
      (ImGuiCol.Border,                  Border),
      (ImGuiCol.BorderShadow,            new Vector4(0, 0, 0, 0)),
      (ImGuiCol.FrameBg,                 SurfaceAlt),
      (ImGuiCol.FrameBgHovered,          new Vector4(0.204f, 0.180f, 0.310f, 1f)),
      (ImGuiCol.FrameBgActive,           new Vector4(0.235f, 0.208f, 0.353f, 1f)),
      (ImGuiCol.TitleBg,                 Surface),
      (ImGuiCol.TitleBgActive,           SurfaceAlt),
      (ImGuiCol.TitleBgCollapsed,        Surface),
      (ImGuiCol.CheckMark,               Accent),
      (ImGuiCol.SliderGrab,              Accent),
      (ImGuiCol.SliderGrabActive,        Accent),
      (ImGuiCol.Button,                  SurfaceAlt),
      (ImGuiCol.ButtonHovered,           new Vector4(0.290f, 0.243f, 0.451f, 1f)),
      (ImGuiCol.ButtonActive,            Accent),
      (ImGuiCol.Header,                  AccentSoft),
      (ImGuiCol.HeaderHovered,           new Vector4(0.639f, 0.549f, 0.980f, 0.45f)),
      (ImGuiCol.HeaderActive,            AccentSoft),
      (ImGuiCol.Separator,               Border),
      (ImGuiCol.SeparatorHovered,        AccentSoft),
      (ImGuiCol.SeparatorActive,         Accent),
      (ImGuiCol.ScrollbarBg,             Surface),
      (ImGuiCol.ScrollbarGrab,           SurfaceAlt),
      (ImGuiCol.ScrollbarGrabHovered,    new Vector4(0.290f, 0.243f, 0.451f, 1f)),
      (ImGuiCol.ScrollbarGrabActive,     Accent),
      (ImGuiCol.Tab,                     Surface),
      (ImGuiCol.TabHovered,              AccentSoft),
      (ImGuiCol.Text,                    Text),
      (ImGuiCol.TextDisabled,            Muted),
      (ImGuiCol.TextSelectedBg,          AccentSoft),
      (ImGuiCol.PlotLines,               Accent),
      (ImGuiCol.PlotHistogram,           Accent),
    };

    /// <summary>Push the whole palette for one window frame. Always pair with <see cref="Pop"/>.</summary>
    public static void Push()
    {
      foreach (var (col, value) in Styles)
        ImGui.PushStyleColor(col, value);
    }

    public static void Pop() => ImGui.PopStyleColor(Styles.Length);
  }
}
