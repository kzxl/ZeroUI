using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{

    public enum ZeroTagType
    {
        Default,
        Success,
        Processing,
        Warning,
        Error
    }

    /// <summary>
    /// Lightweight status tag component for ZeroUI with soft backgrounds, clean borders, and clear status typography.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultProperty("Text")]
    [Description("Lightweight status tag badge with clean border and status typography")]
    public class ZeroTag : Control
    {

        private ZeroTagType _tagType = ZeroTagType.Default;
        private int _borderRadius = 4;

        public ZeroTag()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(80, 24);
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            Text = "Tag";

            ZeroUIConfig.ConfigChanged += (s, e) =>
            {
                Font = new Font(ZeroUIConfig.DefaultFont.FontFamily, 8.5f, FontStyle.Regular);
                Invalidate();
            };
        }

        [Category("Appearance")]
        [DefaultValue(ZeroTagType.Default)]
        public ZeroTagType TagType
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

            var (bg, border, fg) = GetTagColors(_tagType);

            // 1. Fill parent background to eliminate black corner clipping artifacts
            Color parentBg = ZeroUIConfig.GetParentBackground(this, ZeroTheme.Colors.Background);
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

        public static (Color bg, Color border, Color fg) GetTagColors(ZeroTagType type) => type switch
        {
            ZeroTagType.Success => (Color.FromArgb(246, 255, 237), Color.FromArgb(183, 235, 143), Color.FromArgb(56, 158, 13)),   // Emerald
            ZeroTagType.Processing => (Color.FromArgb(230, 244, 255), Color.FromArgb(145, 202, 255), Color.FromArgb(9, 88, 217)), // Sapphire
            ZeroTagType.Warning => (Color.FromArgb(255, 251, 230), Color.FromArgb(255, 229, 143), Color.FromArgb(212, 107, 8)),  // Amber
            ZeroTagType.Error => (Color.FromArgb(255, 242, 240), Color.FromArgb(255, 204, 199), Color.FromArgb(207, 19, 34)),   // Ruby
            _ => (Color.FromArgb(250, 250, 250), Color.FromArgb(217, 217, 217), Color.FromArgb(38, 38, 38))                    // Slate
        };


        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius) =>
            ZeroUIConfig.CreateRoundedRectangle(rect, radius);
    }
}
