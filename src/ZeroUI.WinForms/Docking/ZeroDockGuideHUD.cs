using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Docking
{
    /// <summary>
    /// Visual Docking Diamond HUD overlay control for ZeroDockManager.
    /// Renders 5-zone docking targets (Center/Document, Left, Top, Right, Bottom)
    /// and dynamic translucent preview zones during panel drag operations.
    /// </summary>
    internal class ZeroDockGuideHUD : Control
    {
        private readonly ZeroDockManager _manager;
        private ZeroDockPosition? _hoveredPosition;

        private Rectangle _centerRect;
        private Rectangle _leftRect;
        private Rectangle _rightRect;
        private Rectangle _topRect;
        private Rectangle _bottomRect;

        public ZeroDockPosition? HoveredPosition => _hoveredPosition;

        public ZeroDockGuideHUD(ZeroDockManager manager)
        {
            _manager = manager;
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor, true);
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Visible = false;
        }

        public void UpdateMouse(Point screenPoint)
        {
            Point clientPt = PointToClient(screenPoint);
            ZeroDockPosition? newPos = null;

            if (_centerRect.Contains(clientPt)) newPos = ZeroDockPosition.Document;
            else if (_leftRect.Contains(clientPt)) newPos = ZeroDockPosition.Left;
            else if (_rightRect.Contains(clientPt)) newPos = ZeroDockPosition.Right;
            else if (_topRect.Contains(clientPt)) newPos = ZeroDockPosition.Top;
            else if (_bottomRect.Contains(clientPt)) newPos = ZeroDockPosition.Bottom;

            if (_hoveredPosition != newPos)
            {
                _hoveredPosition = newPos;
                Invalidate();
            }
        }

        public void Reset()
        {
            _hoveredPosition = null;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ComputeGuideRectangles();
        }

        private void ComputeGuideRectangles()
        {
            int cx = Width / 2;
            int cy = Height / 2;
            int size = 36;
            int spacing = 44;

            _centerRect = new Rectangle(cx - size / 2, cy - size / 2, size, size);
            _leftRect = new Rectangle(cx - size / 2 - spacing, cy - size / 2, size, size);
            _rightRect = new Rectangle(cx - size / 2 + spacing, cy - size / 2, size, size);
            _topRect = new Rectangle(cx - size / 2, cy - size / 2 - spacing, size, size);
            _bottomRect = new Rectangle(cx - size / 2, cy - size / 2 + spacing, size, size);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var palette = ZeroTheme.Colors;

            // 1. Draw Translucent Dock Preview Zone
            if (_hoveredPosition.HasValue)
            {
                Rectangle previewRect = _hoveredPosition.Value switch
                {
                    ZeroDockPosition.Left => new Rectangle(0, 0, Math.Min(280, Width / 2), Height),
                    ZeroDockPosition.Right => new Rectangle(Width - Math.Min(280, Width / 2), 0, Math.Min(280, Width / 2), Height),
                    ZeroDockPosition.Top => new Rectangle(0, 0, Width, Math.Min(200, Height / 2)),
                    ZeroDockPosition.Bottom => new Rectangle(0, Height - Math.Min(200, Height / 2), Width, Math.Min(200, Height / 2)),
                    _ => new Rectangle(40, 40, Math.Max(100, Width - 80), Math.Max(100, Height - 80))
                };

                using var fillBrush = new SolidBrush(Color.FromArgb(55, palette.Primary));
                g.FillRectangle(fillBrush, previewRect);
                using var borderPen = new Pen(palette.Primary, 2f);
                g.DrawRectangle(borderPen, previewRect);
            }

            // 2. Draw Diamond HUD Buttons
            ComputeGuideRectangles();
            DrawGuideButton(g, _centerRect, ZeroDockPosition.Document, palette);
            DrawGuideButton(g, _leftRect, ZeroDockPosition.Left, palette);
            DrawGuideButton(g, _rightRect, ZeroDockPosition.Right, palette);
            DrawGuideButton(g, _topRect, ZeroDockPosition.Top, palette);
            DrawGuideButton(g, _bottomRect, ZeroDockPosition.Bottom, palette);
        }

        private void DrawGuideButton(Graphics g, Rectangle r, ZeroDockPosition pos, ZeroThemePalette palette)
        {
            bool isHovered = _hoveredPosition == pos;
            Color bgColor = isHovered ? palette.Primary : Color.FromArgb(240, palette.Surface);
            Color borderColor = isHovered ? Color.White : palette.Border;
            Color iconColor = isHovered ? Color.White : palette.TextPrimary;

            // Button Card Shadow
            using (var shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
            {
                g.FillRectangle(shadowBrush, r.X + 2, r.Y + 2, r.Width, r.Height);
            }

            using (var path = ZeroUIConfig.CreateRoundedRectangle(r, 6))
            {
                using var brush = new SolidBrush(bgColor);
                g.FillPath(brush, path);
                using var pen = new Pen(borderColor, 1.5f);
                g.DrawPath(pen, path);
            }

            // Vector Glyphs
            using var iconPen = new Pen(iconColor, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            int cx = r.X + r.Width / 2;
            int cy = r.Y + r.Height / 2;

            switch (pos)
            {
                case ZeroDockPosition.Document:
                    g.DrawRectangle(iconPen, cx - 7, cy - 7, 14, 14);
                    g.DrawLine(iconPen, cx - 7, cy - 3, cx + 7, cy - 3);
                    break;

                case ZeroDockPosition.Left:
                    g.DrawRectangle(iconPen, cx - 7, cy - 7, 14, 14);
                    using (var b = new SolidBrush(iconColor))
                        g.FillRectangle(b, cx - 7, cy - 7, 5, 14);
                    break;

                case ZeroDockPosition.Right:
                    g.DrawRectangle(iconPen, cx - 7, cy - 7, 14, 14);
                    using (var b = new SolidBrush(iconColor))
                        g.FillRectangle(b, cx + 2, cy - 7, 5, 14);
                    break;

                case ZeroDockPosition.Top:
                    g.DrawRectangle(iconPen, cx - 7, cy - 7, 14, 14);
                    using (var b = new SolidBrush(iconColor))
                        g.FillRectangle(b, cx - 7, cy - 7, 14, 5);
                    break;

                case ZeroDockPosition.Bottom:
                    g.DrawRectangle(iconPen, cx - 7, cy - 7, 14, 14);
                    using (var b = new SolidBrush(iconColor))
                        g.FillRectangle(b, cx - 7, cy + 2, 14, 5);
                    break;
            }
        }
    }
}
