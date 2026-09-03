using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZeroUI.WinForms.Controls
{
    public enum ZeroStepStatus
    {
        Waiting,
        InProgress,
        Completed,
        Warning,
        Error
    }

    public enum ZeroStepGlyph
    {
        Gear,
        Checkmark,
        Warehouse,
        Truck,
        Alert,
        Custom
    }

    public class ZeroStepItem
    {
        public string Key { get; set; } = "";
        public string Title { get; set; } = "Step Title";
        public int Quantity { get; set; } = 0;
        public int? TargetQuantity { get; set; }
        public string? Timestamp { get; set; } = "--";
        public ZeroStepStatus Status { get; set; } = ZeroStepStatus.Waiting;
        public ZeroStepGlyph Glyph { get; set; } = ZeroStepGlyph.Gear;
        public string? CustomGlyphText { get; set; }
        public object? Tag { get; set; }
    }

    public class ZeroStepClickedEventArgs : EventArgs
    {
        public int StepIndex { get; }
        public ZeroStepItem Step { get; }

        public ZeroStepClickedEventArgs(int index, ZeroStepItem step)
        {
            StepIndex = index;
            Step = step;
        }
    }

    /// <summary>
    /// Modern Data-Driven Workflow Process Steps control for ZeroUI with vector nodes and transition arrows.
    /// </summary>
    public class ZeroSteps : Control
    {
        private readonly List<ZeroStepItem> _steps = new List<ZeroStepItem>();
        private readonly List<Rectangle> _stepRects = new List<Rectangle>();
        private int _hoveredIndex = -1;

        public event EventHandler<ZeroStepClickedEventArgs>? StepClicked;

        public ZeroSteps()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);

            Size = new Size(600, 85);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9f);
        }

        [Browsable(false)]
        public IReadOnlyList<ZeroStepItem> Steps => _steps;

        public void SetSteps(IEnumerable<ZeroStepItem> steps)
        {
            _steps.Clear();
            if (steps != null)
            {
                _steps.AddRange(steps);
            }
            Invalidate();
        }

        public void UpdateStep(string key, int quantity, string? timestamp = null, ZeroStepStatus? status = null)
        {
            for (int i = 0; i < _steps.Count; i++)
            {
                if (string.Equals(_steps[i].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    _steps[i].Quantity = quantity;
                    if (timestamp != null) _steps[i].Timestamp = timestamp;
                    if (status.HasValue) _steps[i].Status = status.Value;
                    Invalidate();
                    return;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            _stepRects.Clear();
            int count = _steps.Count;
            if (count == 0) return;

            int arrowWidth = 32;
            int totalArrowsWidth = (count - 1) * arrowWidth;
            int availableWidthForCards = Math.Max(count * 80, Width - totalArrowsWidth - 24);
            int cardWidth = availableWidthForCards / count;
            int cardHeight = Height - 16;
            int cardY = 8;

            int currentX = 12;

            for (int i = 0; i < count; i++)
            {
                var step = _steps[i];
                Rectangle cardRect = new Rectangle(currentX, cardY, cardWidth, cardHeight);
                _stepRects.Add(cardRect);

                bool isHovered = (i == _hoveredIndex);

                // 1. Draw Step Card Background
                var (bgCol, borderCol, iconCol, glyphChar) = GetStepColorsAndGlyph(step);

                using (var cardPath = CreateRoundedRectangle(cardRect, 8))
                {
                    using var bgBrush = new SolidBrush(isHovered ? Color.FromArgb(249, 250, 251) : Color.FromArgb(254, 254, 254));
                    g.FillPath(bgBrush, cardPath);

                    using var borderPen = new Pen(isHovered ? Color.FromArgb(79, 70, 229) : Color.FromArgb(229, 231, 235), isHovered ? 1.5f : 1f);
                    g.DrawPath(borderPen, cardPath);
                }

                // 2. Draw Circular Glyph Icon
                int iconDiameter = 34;
                int iconX = cardRect.Left + 10;
                int iconY = cardRect.Top + (cardHeight - iconDiameter) / 2;
                Rectangle iconRect = new Rectangle(iconX, iconY, iconDiameter, iconDiameter);

                using (var iconPath = new GraphicsPath())
                {
                    iconPath.AddEllipse(iconRect);
                    using var iconBgBrush = new SolidBrush(bgCol);
                    g.FillPath(iconBgBrush, iconPath);

                    using var iconBorderPen = new Pen(borderCol, 1.2f);
                    g.DrawPath(iconBorderPen, iconPath);
                }

                TextRenderer.DrawText(
                    g,
                    glyphChar,
                    new Font("Segoe UI", 12f, FontStyle.Bold),
                    iconRect,
                    iconCol,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // 3. Draw Step Text Info (Title, Quantity, Timestamp)
                int textX = iconRect.Right + 10;
                int textWidth = cardRect.Right - textX - 6;

                if (textWidth > 20)
                {
                    // Title
                    using var titleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    Rectangle titleRect = new Rectangle(textX, cardRect.Top + 8, textWidth, 20);
                    TextRenderer.DrawText(g, step.Title, titleFont, titleRect, Color.FromArgb(17, 24, 39), TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);

                    // Quantity
                    using var qtyFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
                    string qtyText = step.TargetQuantity.HasValue
                        ? $"Quantity: {step.Quantity:N0} / {step.TargetQuantity.Value:N0}"
                        : $"Quantity: {step.Quantity:N0}";
                    Rectangle qtyRect = new Rectangle(textX, titleRect.Bottom + 1, textWidth, 18);
                    TextRenderer.DrawText(g, qtyText, qtyFont, qtyRect, Color.FromArgb(55, 65, 81), TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);

                    // Timestamp
                    using var timeFont = new Font("Segoe UI", 8f, FontStyle.Regular);
                    string timeText = $"Last finish: {step.Timestamp ?? "--"}";
                    Rectangle timeRect = new Rectangle(textX, qtyRect.Bottom + 1, textWidth, 16);
                    TextRenderer.DrawText(g, timeText, timeFont, timeRect, Color.FromArgb(156, 163, 175), TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);
                }

                currentX += cardWidth;

                // 4. Draw Connecting Transition Arrow (between steps)
                if (i < count - 1)
                {
                    Rectangle arrowRect = new Rectangle(currentX, cardY, arrowWidth, cardHeight);
                    TextRenderer.DrawText(
                        g,
                        "→",
                        new Font("Segoe UI", 14f, FontStyle.Bold),
                        arrowRect,
                        Color.FromArgb(59, 130, 246),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                    currentX += arrowWidth;
                }
            }
        }

        private static (Color bg, Color border, Color icon, string glyph) GetStepColorsAndGlyph(ZeroStepItem step)
        {
            string glyph = step.Glyph switch
            {
                ZeroStepGlyph.Gear => "⚙",
                ZeroStepGlyph.Checkmark => "✔",
                ZeroStepGlyph.Warehouse => "🏠",
                ZeroStepGlyph.Truck => "🚚",
                ZeroStepGlyph.Alert => "⚠",
                ZeroStepGlyph.Custom => step.CustomGlyphText ?? "•",
                _ => "•"
            };

            return step.Status switch
            {
                ZeroStepStatus.Completed => (Color.FromArgb(246, 255, 237), Color.FromArgb(183, 235, 143), Color.FromArgb(82, 196, 26), step.Glyph == ZeroStepGlyph.Checkmark ? "✔" : glyph),
                ZeroStepStatus.InProgress => (Color.FromArgb(230, 244, 255), Color.FromArgb(145, 202, 255), Color.FromArgb(22, 119, 255), glyph),
                ZeroStepStatus.Warning => (Color.FromArgb(255, 251, 230), Color.FromArgb(255, 229, 143), Color.FromArgb(250, 173, 20), glyph),
                ZeroStepStatus.Error => (Color.FromArgb(255, 242, 240), Color.FromArgb(255, 204, 199), Color.FromArgb(255, 77, 79), "✖"),
                _ => (Color.FromArgb(243, 244, 246), Color.FromArgb(229, 231, 235), Color.FromArgb(156, 163, 175), glyph)
            };
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int found = -1;
            for (int i = 0; i < _stepRects.Count; i++)
            {
                if (_stepRects[i].Contains(e.Location))
                {
                    found = i;
                    break;
                }
            }

            if (_hoveredIndex != found)
            {
                _hoveredIndex = found;
                Cursor = found >= 0 ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoveredIndex != -1)
            {
                _hoveredIndex = -1;
                Cursor = Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button == MouseButtons.Left && _hoveredIndex >= 0 && _hoveredIndex < _steps.Count)
            {
                StepClicked?.Invoke(this, new ZeroStepClickedEventArgs(_hoveredIndex, _steps[_hoveredIndex]));
            }
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0 || rect.Width <= 0 || rect.Height <= 0)
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
