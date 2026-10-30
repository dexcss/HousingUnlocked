using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;

namespace HousingUnlocked.Interface
{
  /// <summary>
  /// Borderless Fancy UI window shell: custom draggable title bar with an accent dot, a close
  /// button, and a padded body region. Windows deriving from this must implement DrawBody().
  /// </summary>
  public abstract class ShellWindow : Window
  {
    private readonly string _title;
    private bool _dragging;

    protected ShellWindow(string id, string title, Vector2? minSize = null, Vector2? maxSize = null)
      : base(id, ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse
            | ImGuiWindowFlags.NoCollapse)
    {
      _title = title;
      SizeConstraints = new WindowSizeConstraints
      {
        MinimumSize = minSize ?? new Vector2(420, 320),
        MaximumSize = maxSize ?? new Vector2(1600, 1400),
      };
    }

    /// <summary>Draw the page content. The cursor is inside the padded body child.</summary>
    protected abstract void DrawBody();

    public override void PreDraw() => Theme.Push();

    public override void PostDraw() => Theme.Pop();

    public override void Draw()
    {
      var s = Theme.UiScale;
      var pos = ImGui.GetWindowPos();
      var size = ImGui.GetWindowSize();
      var dl = ImGui.GetWindowDrawList();
      var headerH = 42f * s;

      // Header plate.
      dl.AddRectFilled(pos, pos + new Vector2(size.X, headerH), Theme.U32(Theme.Surface),
        Theme.RadiusCard, ImDrawFlags.RoundCornersTop);
      dl.AddLine(new Vector2(pos.X, pos.Y + headerH), new Vector2(pos.X + size.X, pos.Y + headerH),
        Theme.U32(Theme.Border), 1f * s);

      // Drag from anywhere in the header.
      var mouse = ImGui.GetIO().MousePos;
      var inHeader = mouse.X >= pos.X && mouse.X <= pos.X + size.X
                  && mouse.Y >= pos.Y && mouse.Y <= pos.Y + headerH;
      if (inHeader && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        _dragging = true;
      if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
        _dragging = false;
      if (_dragging)
        ImGui.SetWindowPos(pos + ImGui.GetIO().MouseDelta);

      // Accent dot + title.
      var titleY = pos.Y + (headerH - ImGui.GetTextLineHeight()) * 0.5f;
      dl.AddRectFilled(new Vector2(pos.X + 14f * s, pos.Y + headerH * 0.5f - 3f * s),
        new Vector2(pos.X + 20f * s, pos.Y + headerH * 0.5f + 3f * s), Theme.U32(Theme.Accent), Theme.RadiusTag);
      dl.AddText(new Vector2(pos.X + 28f * s, titleY), Theme.U32(Theme.Text), _title);

      DrawHeaderExtras(pos, size, headerH);

      // Close button.
      var close = 26f;
      ImGui.SetCursorScreenPos(new Vector2(
        pos.X + size.X - (close * s) - 8f * s,
        pos.Y + (headerH - close * s) * 0.5f));
      if (UiKit.IconButton("##hu-close", FontAwesomeIcon.Times, "Close", false, close))
        IsOpen = false;

      // Body.
      ImGui.SetCursorScreenPos(new Vector2(pos.X + Theme.Pad, pos.Y + headerH + Theme.Pad));
      var bodySize = new Vector2(size.X - Theme.Pad * 2f, size.Y - headerH - Theme.Pad * 2f);
      if (ImGui.BeginChild("##hu-body", bodySize, false, ImGuiWindowFlags.None))
        DrawBody();
      ImGui.EndChild();
    }

    /// <summary>Hook for a title-bar status chip or similar; default draws nothing.</summary>
    protected virtual void DrawHeaderExtras(Vector2 pos, Vector2 size, float headerH)
    {
    }
  }
}
