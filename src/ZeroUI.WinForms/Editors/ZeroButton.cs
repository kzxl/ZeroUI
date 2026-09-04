using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{
    public enum ZeroButtonStyle

    {
        Primary,
        Secondary,
        Success,
        Danger,
        Ghost
    }

    /// <summary>
    /// Modern flat button control with stateful hover effects, smooth rounded corners, and badge counter support.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultEvent("Click")]
    [DefaultProperty("Text")]
    [Description("Modern anti-aliased button with rounded corners and stateful styling")]
    public class ZeroButton : Control
    {

        private ZeroButtonStyle _style = ZeroButtonStyle.Primary;
        private int _borderRadius = 6;
        private string? _badgeText;
        private bool _isHovered = false;
        private bool _isPressed = false;

        public ZeroButton()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(130, 36);
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            Cursor = Cursors.Hand;

            ZeroTheme.ThemeChanged += (s, e) => Invalidate();
            ZeroUIConfig.CornerStyleChanged += (s, e) => Invalidate();
            ZeroUIConfig.FontChanged += (s, e) =>
            {
                Font = new Font(ZeroUIConfig.DefaultFont.FontFamily, 9.5f, FontStyle.Bold);
                Invalidate();
            };
        }

        [Category("Appearance")]
        [DefaultValue(ZeroButtonStyle.Primary)]
        public ZeroButtonStyle ButtonStyle
        {
            get => _style;
            set { _style = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(6)]
        public int BorderRadius
        {
            get => _borderRadius;
            set { _borderRadius = Math.Max(0, value); Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(null)]
        public string? BadgeText
        {
            get => _badgeText;
            set { _badgeText = value; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            _isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var (bg, fg, border) = GetColors();

            // 1. Fill parent background to eliminate black corner artifacts
            Color parentBg = ZeroUIConfig.GetParentBackground(this, ZeroTheme.Colors.Background);
            using (var brushParent = new SolidBrush(parentBg))
            {
                g.FillRectangle(brushParent, ClientRectangle);
            }

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int effRadius = ZeroUIConfig.GetEffectiveRadius(_borderRadius);

            // Draw Button Body
            using (var path = CreateRoundedRectangle(rect, effRadius))
            {
                using var brush = new SolidBrush(bg);
                g.FillPath(brush, path);

                if (border != Color.Transparent)
                {
                    using var pen = new Pen(border, 1f);
                    g.DrawPath(pen, path);
                }
            }

            // Draw Button Text
            TextRenderer.DrawText(
                g,
                Text,
                Font,
                rect,
                fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

            // Draw Optional Badge
            if (!string.IsNullOrEmpty(_badgeText))
            {
                Size badgeSize = TextRenderer.MeasureText(_badgeText, Font);
                int badgeW = Math.Max(18, badgeSize.Width + 6);
                int badgeH = 18;
                Rectangle badgeRect = new Rectangle(Width - badgeW - 6, (Height - badgeH) / 2, badgeW, badgeH);

                using var badgePath = CreateRoundedRectangle(badgeRect, 9);
                using var badgeBg = new SolidBrush(Color.FromArgb(220, 38, 38));
                g.FillPath(badgeBg, badgePath);

                TextRenderer.DrawText(
                    g,
                    _badgeText,
                    new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    badgeRect,
                    Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            }
        }

        private (Color bg, Color fg, Color border) GetColors()
        {
            var palette = ZeroTheme.Colors;

            if (!Enabled)
            {
                Color disabledBg = ZeroTheme.IsDark ? Color.FromArgb(40, 44, 60) : Color.FromArgb(229, 231, 235);
                return (disabledBg, palette.TextSecondary, Color.Transparent);
            }

            return _style switch
            {
                ZeroButtonStyle.Primary => _isPressed
                    ? (palette.PrimaryHover, Color.White, Color.Transparent)
                    : _isHovered
                        ? (palette.PrimaryHover, Color.White, Color.Transparent)
                        : (palette.Primary, Color.White, Color.Transparent),

                ZeroButtonStyle.Secondary => _isPressed
                    ? (palette.Hover, palette.TextPrimary, palette.Primary)
                    : _isHovered
                        ? (palette.Hover, palette.TextPrimary, palette.Border)
                        : (palette.Surface, palette.TextPrimary, palette.Border),

                ZeroButtonStyle.Success => _isPressed
                    ? (palette.Success, Color.White, Color.Transparent)
                    : _isHovered
                        ? (palette.Success, Color.White, Color.Transparent)
                        : (palette.Success, Color.White, Color.Transparent),

                ZeroButtonStyle.Danger => _isPressed
                    ? (palette.Danger, Color.White, Color.Transparent)
                    : _isHovered
                        ? (palette.Danger, Color.White, Color.Transparent)
                        : (palette.Danger, Color.White, Color.Transparent),

                ZeroButtonStyle.Ghost => _isPressed
                    ? (palette.Hover, palette.TextPrimary, Color.Transparent)
                    : _isHovered
                        ? (palette.Hover, palette.TextPrimary, Color.Transparent)
                        : (Color.Transparent, palette.TextPrimary, Color.Transparent),

                _ => (palette.Primary, Color.White, Color.Transparent)
            };
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius) =>
            ZeroUIConfig.CreateRoundedRectangle(rect, radius);
    }
}
