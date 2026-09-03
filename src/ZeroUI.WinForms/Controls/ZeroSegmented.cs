using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZeroUI.WinForms.Controls
{
    /// <summary>
    /// Modern Segmented Control (Pill switcher) for ZeroUI providing clean, compact view and filter switching.
    /// </summary>
    public class ZeroSegmented : Control

    {
        private string[] _items = new[] { "All", "Daily", "Weekly", "Monthly" };

        private int _selectedIndex = 0;
        private int _hoveredIndex = -1;

        public event EventHandler? SelectedIndexChanged;

        public ZeroSegmented()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(320, 34);
            Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            Cursor = Cursors.Hand;
        }

        [Category("Data")]
        public string[] Items
        {
            get => _items;
            set
            {
                _items = value ?? Array.Empty<string>();
                if (_selectedIndex >= _items.Length) _selectedIndex = Math.Max(0, _items.Length - 1);
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(0)]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                int clamped = Math.Max(0, Math.Min(_items.Length - 1, value));
                if (_selectedIndex != clamped)
                {
                    _selectedIndex = clamped;
                    Invalidate();
                    SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string? SelectedItem => (_selectedIndex >= 0 && _selectedIndex < _items.Length) ? _items[_selectedIndex] : null;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int idx = GetIndexAt(e.X);
            if (_hoveredIndex != idx)
            {
                _hoveredIndex = idx;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoveredIndex = -1;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int idx = GetIndexAt(e.X);
            if (idx >= 0 && idx < _items.Length)
            {
                SelectedIndex = idx;
            }
        }

        private int GetIndexAt(int x)
        {
            if (_items.Length == 0) return -1;
            int itemW = (Width - 4) / _items.Length;
            if (itemW <= 0) return -1;
            int idx = (x - 2) / itemW;
            return Math.Max(0, Math.Min(_items.Length - 1, idx));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle trackRect = new Rectangle(0, 0, Width - 1, Height - 1);

            // 1. Draw Track Background
            using (var trackPath = CreateRoundedRectangle(trackRect, 6))
            {
                using var trackBrush = new SolidBrush(Color.FromArgb(243, 244, 246)); // Gray 100
                g.FillPath(trackBrush, trackPath);
            }

            if (_items.Length == 0) return;

            float itemW = (float)(Width - 4) / _items.Length;
            float itemH = Height - 4;

            // 2. Draw Active Pill
            if (_selectedIndex >= 0 && _selectedIndex < _items.Length)
            {
                RectangleF pillRect = new RectangleF(2 + (_selectedIndex * itemW), 2, itemW, itemH);
                using (var pillPath = CreateRoundedRectangleF(pillRect, 5))
                {
                    using var pillBrush = new SolidBrush(Color.White);
                    g.FillPath(pillBrush, pillPath);

                    using var shadowPen = new Pen(Color.FromArgb(229, 231, 235), 1f);
                    g.DrawPath(shadowPen, pillPath);
                }
            }

            // 3. Draw Item Texts
            for (int i = 0; i < _items.Length; i++)
            {
                Rectangle itemRect = new Rectangle((int)(2 + (i * itemW)), 2, (int)itemW, (int)itemH);
                bool isSelected = (i == _selectedIndex);
                Color textColor = isSelected ? Color.FromArgb(17, 24, 39) : Color.FromArgb(107, 114, 128);
                Font itemFont = isSelected ? new Font(Font, FontStyle.Bold) : Font;

                TextRenderer.DrawText(
                    g,
                    _items[i],
                    itemFont,
                    itemRect,
                    textColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            }
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(rect.Location, new Size(diameter, diameter));
            path.AddArc(arc, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static GraphicsPath CreateRoundedRectangleF(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2;
            RectangleF arc = new RectangleF(rect.Location, new SizeF(diameter, diameter));
            path.AddArc(arc, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
