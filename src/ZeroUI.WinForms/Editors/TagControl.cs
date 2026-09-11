using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.Editors;
using ZeroUI.Core.Theme;
using ZeroUI.WinForms.Base;
using ZeroUI.WinForms.Icons;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// Lightweight status tag component for ZeroUI with soft backgrounds, clean borders, and clear status typography.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultProperty("Text")]
    [Description("Lightweight status tag badge with clean border and status typography")]
    [ToolboxBitmap(typeof(ZeroIcons), "ZeroTag.bmp")]
    public class TagControl : ZeroControlBase
    {
        private TagType _tagType = TagType.Default;
        private int _borderRadius = 4;

        public TagControl()
        {
            Size = new Size(80, 24);
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            Text = "Tag";

            ZeroUIConfig.CornerStyleChanged += (s, e) => Invalidate();
            ZeroUIConfig.FontChanged += (s, e) =>
            {
                Font = new Font(ZeroUIConfig.DefaultFont.FontFamily, 8.5f, FontStyle.Regular);
                Invalidate();
            };
        }

        protected override void OnThemeChanged(ZeroSkin skin)
        {
            base.OnThemeChanged(skin);
            Invalidate();
        }

        [Category("Appearance")]
        [DefaultValue(TagType.Default)]
        public TagType TagType
        {
            get => _tagType;
            set { _tagType = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(4)]
        public int BorderRadius
        {
            get => _borderRadius;
            set { _borderRadius = Math.Max(0, value); Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var (bg, border, fg) = GetTagColors(_tagType, EffectiveSkin.IsDark);

            // 1. Fill parent background to eliminate black corner clipping artifacts
            Color parentBg = ZeroUIConfig.GetParentBackground(this, CurrentPalette.Background);
            using (var brushParent = new SolidBrush(parentBg))
            {
                g.FillRectangle(brushParent, ClientRectangle);
            }

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int effRadius = ZeroUIConfig.GetEffectiveRadius(_borderRadius);

            using (var path = CreateRoundedRectangle(rect, effRadius))
            {
                using var brush = new SolidBrush(bg);
                g.FillPath(brush, path);

                using var pen = new Pen(border, 1f);
                g.DrawPath(pen, path);
            }

            TextRenderer.DrawText(
                g,
                Text,
                Font,
                rect,
                fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        public static (Color bg, Color border, Color fg) GetTagColors(TagType type) => GetTagColors(type, false);

        public static (Color bg, Color border, Color fg) GetTagColors(TagType type, bool isDark) => (type, isDark) switch
        {
            (TagType.Success, true) => (Color.FromArgb(19, 41, 26), Color.FromArgb(39, 80, 50), Color.FromArgb(73, 170, 95)),
            (TagType.Processing, true) => (Color.FromArgb(17, 33, 56), Color.FromArgb(28, 64, 110), Color.FromArgb(88, 166, 255)),
            (TagType.Warning, true) => (Color.FromArgb(46, 32, 12), Color.FromArgb(92, 65, 24), Color.FromArgb(232, 175, 59)),
            (TagType.Error, true) => (Color.FromArgb(48, 16, 20), Color.FromArgb(98, 33, 41), Color.FromArgb(248, 81, 73)),
            (_, true) => (Color.FromArgb(33, 38, 45), Color.FromArgb(48, 54, 61), Color.FromArgb(201, 209, 217)),

            (TagType.Success, false) => (Color.FromArgb(246, 255, 237), Color.FromArgb(183, 235, 143), Color.FromArgb(56, 158, 13)),   // Emerald
            (TagType.Processing, false) => (Color.FromArgb(230, 244, 255), Color.FromArgb(145, 202, 255), Color.FromArgb(9, 88, 217)), // Sapphire
            (TagType.Warning, false) => (Color.FromArgb(255, 251, 230), Color.FromArgb(255, 229, 143), Color.FromArgb(212, 107, 8)),  // Amber
            (TagType.Error, false) => (Color.FromArgb(255, 242, 240), Color.FromArgb(255, 204, 199), Color.FromArgb(207, 19, 34)),   // Ruby
            _ => (Color.FromArgb(250, 250, 250), Color.FromArgb(217, 217, 217), Color.FromArgb(38, 38, 38))                    // Slate
        };

        [Obsolete("ZeroTagType is deprecated. Use TagType overload instead.")]
        public static (Color bg, Color border, Color fg) GetTagColors(ZeroTagType type) => GetTagColors((TagType)type, false);

        [Obsolete("ZeroTagType is deprecated. Use TagType overload instead.")]
        public static (Color bg, Color border, Color fg) GetTagColors(ZeroTagType type, bool isDark) => GetTagColors((TagType)type, isDark);


        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius) =>
            ZeroUIConfig.CreateRoundedRectangle(rect, radius);
    }

    /// <summary>
    /// Backward-compatibility alias for <see cref="TagControl"/>.
    /// </summary>
    [Obsolete("ZeroTag is deprecated. Use TagControl instead.")]
    [ToolboxItem(false)]
    public class ZeroTag : TagControl
    {
    }
}
