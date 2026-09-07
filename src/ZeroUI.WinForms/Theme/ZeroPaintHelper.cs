using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZeroUI.WinForms.Theme
{
    /// <summary>
    /// High-performance GDI+ graphics routines and styling helpers for ZeroUI WinForms controls.
    /// Standardizes anti-aliased card backgrounds, status badges, focus rings, and borders.
    /// </summary>
    public static class ZeroPaintHelper
    {
        /// <summary>
        /// Draws a smooth anti-aliased card surface with rounded corners and border.
        /// </summary>
        public static void DrawCard(Graphics g, Rectangle bounds, Color backColor, Color borderColor, int cornerRadius = 4)
        {
            if (g == null || bounds.Width <= 0 || bounds.Height <= 0) return;

            var oldSmoothing = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (cornerRadius > 0)
            {
                using var path = CreateRoundedRectangle(bounds, cornerRadius);
                using var brush = new SolidBrush(backColor);
                g.FillPath(brush, path);

                if (borderColor != Color.Transparent)
                {
                    using var pen = new Pen(borderColor, 1f);
                    g.DrawPath(pen, path);
                }
            }
            else
            {
                using var brush = new SolidBrush(backColor);
                g.FillRectangle(brush, bounds);

                if (borderColor != Color.Transparent)
                {
                    using var pen = new Pen(borderColor, 1f);
                    g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
                }
            }

            g.SmoothingMode = oldSmoothing;
        }

        /// <summary>
        /// Draws a standardized industrial status pill badge (e.g. NORMAL, FAULT, ACTIVE).
        /// </summary>
        public static void DrawStatusBadge(Graphics g, Rectangle bounds, string text, Font font, Color statusColor, Color textColor, int cornerRadius = 3)
        {
            if (g == null || bounds.Width <= 0 || bounds.Height <= 0) return;

            var oldSmoothing = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Semi-transparent badge background (20% opacity)
            Color pillFill = Color.FromArgb(40, statusColor);
            using (var path = CreateRoundedRectangle(bounds, cornerRadius))
            {
                using (var fillBrush = new SolidBrush(pillFill))
                {
                    g.FillPath(fillBrush, path);
                }
                using (var borderPen = new Pen(statusColor, 1f))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            TextRenderer.DrawText(
                g,
                text,
                font,
                bounds,
                textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

            g.SmoothingMode = oldSmoothing;
        }

        /// <summary>
        /// Draws an accessibility-compliant high-visibility focus ring.
        /// </summary>
        public static void DrawFocusRing(Graphics g, Rectangle bounds, Color focusColor, int cornerRadius = 4)
        {
            if (g == null || bounds.Width <= 0 || bounds.Height <= 0) return;

            var oldSmoothing = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle focusRect = new Rectangle(bounds.X + 1, bounds.Y + 1, bounds.Width - 2, bounds.Height - 2);
            using var path = CreateRoundedRectangle(focusRect, Math.Max(1, cornerRadius - 1));
            using var pen = new Pen(focusColor, 1.5f) { DashStyle = DashStyle.Dash };
            g.DrawPath(pen, path);

            g.SmoothingMode = oldSmoothing;
        }

        /// <summary>
        /// Creates a smooth GDI+ GraphicsPath for a rounded rectangle.
        /// </summary>
        public static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(rect.Location, size);

            // Top-left
            path.AddArc(arc, 180, 90);

            // Top-right
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Bottom-right
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Bottom-left
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}
