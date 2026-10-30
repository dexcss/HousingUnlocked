using System;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace HousingUnlocked.Interface
{
  /// <summary>
  /// Fancy UI widget kit (blocky edition). Everything is custom-drawn with the window draw list.
  /// Each widget consumes layout space via InvisibleButton/Dummy, so it composes with normal
  /// ImGui layout.
  /// </summary>
  public static class UiKit
  {
    private static readonly Dictionary<uint, float> Anim = new();

    // ---------------------------------------------------------------- cards

    public static void Card(ImDrawListPtr dl, Vector2 min, Vector2 max, Vector4? fill = null, float glow = 0f)
    {
      var r = Theme.RadiusCard;
      if (glow > 0f)
      {
        for (var i = 1; i <= 3; i++)
        {
          var c = new Vector4(Theme.Accent.X, Theme.Accent.Y, Theme.Accent.Z, glow / i);
          dl.AddRectFilled(min - new Vector2(i, i), max + new Vector2(i, i), Theme.U32(c), r + i * Theme.UiScale);
        }
      }

      dl.AddRectFilled(min, max, Theme.U32(fill ?? Theme.Surface), r);
      dl.AddRect(min, max, Theme.U32(Theme.Border), r, ImDrawFlags.None, 1f * Theme.UiScale);
    }

    /// <summary>Place a card at the cursor and return its rect; advances the cursor past it.</summary>
    public static (Vector2 Min, Vector2 Max) BeginCard(float height, float width = 0f, Vector4? fill = null, float glow = 0f)
    {
      var s = Theme.UiScale;
      if (width <= 0f)
        width = ImGui.GetContentRegionAvail().X;
      var p = ImGui.GetCursorScreenPos();
      var max = p + new Vector2(width, height * s);
      Card(ImGui.GetWindowDrawList(), p, max, fill, glow);
      ImGui.SetCursorScreenPos(new Vector2(p.X, max.Y) + new Vector2(0, Theme.Gap));
      return (p, max);
    }

    // ------------------------------------------------------------- headings

    public static void Section(FontAwesomeIcon icon, string text)
    {
      var s = Theme.UiScale;
      ImGui.Dummy(new Vector2(0, 2f * s));

      ImGui.PushFont(UiBuilder.IconFont);
      ImGui.TextColored(Theme.Accent, icon.ToIconString());
      ImGui.PopFont();

      ImGui.SameLine(0, 6f * s);
      ImGui.TextColored(Theme.Muted, text.ToUpperInvariant());
      ImGui.Dummy(new Vector2(0, 2f * s));
    }

    // -------------------------------------------------------------- toggle

    /// <summary>Rectangular slide switch.</summary>
    public static bool Toggle(string id, bool value)
    {
      var s = Theme.UiScale;
      var size = new Vector2(40f * s, 22f * s);
      var p = ImGui.GetCursorScreenPos();

      ImGui.InvisibleButton(id, size);
      var hovered = ImGui.IsItemHovered();
      if (ImGui.IsItemClicked())
        value = !value;

      var key = ImGui.GetID(id);
      var target = value ? 1f : 0f;
      var cur = Anim.TryGetValue(key, out var v) ? v : target;
      cur = Lerp(cur, target, ImGui.GetIO().DeltaTime * 16f);
      Anim[key] = cur;

      var dl = ImGui.GetWindowDrawList();
      var max = p + size;
      dl.AddRectFilled(p, max, Theme.U32(Vector4.Lerp(Theme.SurfaceAlt, Theme.Accent, cur)), Theme.RadiusControl);

      var knobW = size.Y - 4f * s;
      var knobX = Lerp(p.X + 2f * s, max.X - 2f * s - knobW, cur);
      var knob = hovered ? new Vector4(1f, 1f, 1f, 1f) : new Vector4(0.88f, 0.85f, 0.94f, 1f);
      dl.AddRectFilled(new Vector2(knobX, p.Y + 2f * s),
        new Vector2(knobX + knobW, max.Y - 2f * s), Theme.U32(knob), Theme.RadiusTag);

      return value;
    }

    // ----------------------------------------------------------- segmented

    public static int Segmented(string id, string[] labels, int value, float width = 0f)
    {
      var s = Theme.UiScale;
      var h = 30f * s;
      if (width <= 0f)
        width = ImGui.GetContentRegionAvail().X;

      var p = ImGui.GetCursorScreenPos();
      ImGui.InvisibleButton(id, new Vector2(width, h));
      var hovered = ImGui.IsItemHovered();
      var clicked = ImGui.IsItemClicked();

      var n = Math.Max(1, labels.Length);
      var segW = width / n;
      var dl = ImGui.GetWindowDrawList();
      var max = p + new Vector2(width, h);

      dl.AddRectFilled(p, max, Theme.U32(Theme.SurfaceAlt), Theme.RadiusControl);

      var sel = Math.Clamp(value, 0, n - 1);
      var hlMin = new Vector2(p.X + segW * sel + 2f * s, p.Y + 2f * s);
      var hlMax = new Vector2(p.X + segW * (sel + 1) - 2f * s, max.Y - 2f * s);
      dl.AddRectFilled(hlMin, hlMax, Theme.U32(Theme.Surface), Theme.RadiusTag);
      dl.AddRect(hlMin, hlMax, Theme.U32(Theme.Border), Theme.RadiusTag, ImDrawFlags.None, 1f * s);

      for (var i = 0; i < n; i++)
      {
        var label = labels[i];
        var ts = ImGui.CalcTextSize(label);
        var tp = new Vector2(p.X + segW * i + (segW - ts.X) * 0.5f, p.Y + (h - ts.Y) * 0.5f);
        dl.AddText(tp, Theme.U32(i == sel ? Theme.Text : Theme.Muted), label);
      }

      if (clicked && hovered)
        value = Math.Clamp((int)((ImGui.GetIO().MousePos.X - p.X) / segW), 0, n - 1);

      return value;
    }

    // ---------------------------------------------------------- icon button

    public static bool IconButton(string id, FontAwesomeIcon icon, string tooltip = "", bool active = false, float size = 36f)
    {
      var s = Theme.UiScale;
      var box = new Vector2(size * s, size * s);
      var p = ImGui.GetCursorScreenPos();

      ImGui.InvisibleButton(id, box);
      var hovered = ImGui.IsItemHovered();
      var clicked = ImGui.IsItemClicked();

      var dl = ImGui.GetWindowDrawList();
      Card(dl, p, p + box, active ? Theme.Accent : hovered ? Theme.SurfaceAlt : Theme.Surface);

      ImGui.PushFont(UiBuilder.IconFont);
      var glyph = icon.ToIconString();
      var ts = ImGui.CalcTextSize(glyph);
      var col = active ? Theme.WindowBg : hovered ? Theme.Text : Theme.Muted;
      dl.AddText(p + (box - ts) * 0.5f, Theme.U32(col), glyph);
      ImGui.PopFont();

      if (tooltip.Length > 0 && hovered)
        ImGui.SetTooltip(tooltip);

      return clicked;
    }

    /// <summary>Button with a leading FontAwesome glyph and a text label.</summary>
    public static bool TextIconButton(string id, FontAwesomeIcon icon, string label, float height = 30f)
    {
      var s = Theme.UiScale;
      ImGui.PushFont(UiBuilder.IconFont);
      var glyph = icon.ToIconString();
      var gsz = ImGui.CalcTextSize(glyph);
      ImGui.PopFont();
      var tsz = ImGui.CalcTextSize(label);

      var h = height * s;
      var w = 14f * s + gsz.X + 8f * s + tsz.X + 14f * s;
      var p = ImGui.GetCursorScreenPos();

      ImGui.InvisibleButton(id, new Vector2(w, h));
      var hovered = ImGui.IsItemHovered();
      var clicked = ImGui.IsItemClicked();

      var dl = ImGui.GetWindowDrawList();
      Card(dl, p, p + new Vector2(w, h), hovered ? Theme.SurfaceAlt : Theme.Surface);

      ImGui.PushFont(UiBuilder.IconFont);
      dl.AddText(new Vector2(p.X + 14f * s, p.Y + (h - gsz.Y) * 0.5f),
        Theme.U32(hovered ? Theme.Text : Theme.Accent), glyph);
      ImGui.PopFont();
      dl.AddText(new Vector2(p.X + 14f * s + gsz.X + 8f * s, p.Y + (h - tsz.Y) * 0.5f),
        Theme.U32(hovered ? Theme.Text : Theme.Muted), label);

      return clicked;
    }

    // ------------------------------------------------------ gradient button

    public static bool GradientButton(string id, string label, Vector4 left, Vector4 right, float height = 40f)
    {
      var s = Theme.UiScale;
      var h = height * s;
      var w = ImGui.GetContentRegionAvail().X;
      var p = ImGui.GetCursorScreenPos();
      var max = p + new Vector2(w, h);

      ImGui.InvisibleButton(id, new Vector2(w, h));
      var hovered = ImGui.IsItemHovered();
      var clicked = ImGui.IsItemClicked();

      var a = hovered ? Brighten(left) : left;
      var b = hovered ? Brighten(right) : right;

      var dl = ImGui.GetWindowDrawList();
      dl.AddRectFilledMultiColor(p, max, Theme.U32(a), Theme.U32(b), Theme.U32(b), Theme.U32(a));
      dl.AddRect(p, max, Theme.U32(Theme.Border), Theme.RadiusControl, ImDrawFlags.None, 1f * s);

      var ts = ImGui.CalcTextSize(label);
      dl.AddText(new Vector2(p.X + (w - ts.X) * 0.5f, p.Y + (h - ts.Y) * 0.5f),
        Theme.U32(new Vector4(1f, 1f, 1f, 1f)), label);

      return clicked;
    }

    // ---------------------------------------------------------------- chip

    public static void Chip(string text, Vector4 color)
    {
      var s = Theme.UiScale;
      var ts = ImGui.CalcTextSize(text);
      var padX = 8f * s;
      var h = ts.Y + 6f * s;
      var p = ImGui.GetCursorScreenPos();
      var size = new Vector2(ts.X + padX * 2f, h);

      var dl = ImGui.GetWindowDrawList();
      var bg = new Vector4(color.X, color.Y, color.Z, 0.18f);
      dl.AddRectFilled(p, p + size, Theme.U32(bg), Theme.RadiusTag);
      dl.AddRect(p, p + size, Theme.U32(color), Theme.RadiusTag, ImDrawFlags.None, 1f * s);
      dl.AddText(p + new Vector2(padX, 3f * s), Theme.U32(color), text);

      ImGui.Dummy(size);
    }

    // ------------------------------------------------------------- helpers

    /// <summary>Label on the left, switch on the right, one row high. Returns the new value.</summary>
    public static bool ToggleRow(string id, string label, bool value, string tooltip = "")
    {
      var s = Theme.UiScale;
      var rowH = 24f * s;
      var start = ImGui.GetCursorScreenPos();
      var avail = ImGui.GetContentRegionAvail().X;

      ImGui.SetCursorScreenPos(new Vector2(start.X, start.Y + (rowH - ImGui.GetTextLineHeight()) * 0.5f));
      ImGui.TextColored(Theme.Text, label);
      if (tooltip.Length > 0 && ImGui.IsItemHovered())
        ImGui.SetTooltip(tooltip);

      ImGui.SetCursorScreenPos(new Vector2(start.X + avail - 40f * s, start.Y + (rowH - 22f * s) * 0.5f));
      var result = Toggle(id, value);

      ImGui.SetCursorScreenPos(new Vector2(start.X, start.Y + rowH + 4f * s));
      return result;
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * MathF.Min(1f, t);

    private static Vector4 Brighten(Vector4 c) => new(
      MathF.Min(1f, c.X * 1.12f),
      MathF.Min(1f, c.Y * 1.12f),
      MathF.Min(1f, c.Z * 1.12f),
      c.W);
  }
}
