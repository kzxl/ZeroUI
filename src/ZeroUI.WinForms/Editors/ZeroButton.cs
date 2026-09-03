using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

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

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // Draw Button Body
            using (var path = CreateRoundedRectangle(rect, _borderRadius))
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
            if (!Enabled)
            {
                return (Color.FromArgb(229, 231, 235), Color.FromArgb(156, 163, 175), Color.Transparent);
            }

            return _style switch
            {
                ZeroButtonStyle.Primary => _isPressed
                    ? (Color.FromArgb(55, 48, 163), Color.White, Color.Transparent)
                    : _isHovered
                        ? (Color.FromArgb(67, 56, 202), Color.White, Color.Transparent)
                        : (Color.FromArgb(79, 70, 229), Color.White, Color.Transparent),

                ZeroButtonStyle.Secondary => _isPressed
                    ? (Color.FromArgb(209, 213, 219), Color.FromArgb(17, 24, 39), Color.FromArgb(156, 163, 175))
                    : _isHovered
                        ? (Color.FromArgb(243, 244, 246), Color.FromArgb(17, 24, 39), Color.FromArgb(209, 213, 219))
                        : (Color.FromArgb(255, 255, 255), Color.FromArgb(31, 41, 55), Color.FromArgb(209, 213, 219)),

                ZeroButtonStyle.Success => _isPressed
                    ? (Color.FromArgb(21, 128, 61), Color.White, Color.Transparent)
                    : _isHovered
                        ? (Color.FromArgb(22, 163, 74), Color.White, Color.Transparent)
                        : (Color.FromArgb(34, 197, 94), Color.White, Color.Transparent),

                ZeroButtonStyle.Danger => _isPressed
                    ? (Color.FromArgb(185, 28, 28), Color.White, Color.Transparent)
                    : _isHovered
                        ? (Color.FromArgb(220, 38, 38), Color.White, Color.Transparent)
                        : (Color.FromArgb(239, 68, 68), Color.White, Color.Transparent),

                ZeroButtonStyle.Ghost => _isPressed
                    ? (Color.FromArgb(229, 231, 235), Color.FromArgb(17, 24, 39), Color.Transparent)
                    : _isHovered
                        ? (Color.FromArgb(243, 244, 246), Color.FromArgb(17, 24, 39), Color.Transparent)
                        : (Color.Transparent, Color.FromArgb(55, 65, 81), Color.Transparent),

                _ => (Color.FromArgb(79, 70, 229), Color.White, Color.Transparent)
            };
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int diameter = radius * 2;
            Rectangle arc = new Rectangle(rect.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
