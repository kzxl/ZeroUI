using System;

using ZeroUI.WinForms.Icons;using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Industrial
{
    public enum StepStatus

    {
        Waiting,
        InProgress,
        Completed,
        Warning,
        Error
    }

    public enum StepGlyph
    {
        Gear,
        Checkmark,
        Warehouse,
        Truck,
        Alert,
        Custom
    }

    public class StepItem
    {
        public string Key { get; set; } = "";
        public string Title { get; set; } = "Step Title";
        public int Quantity { get; set; } = 0;
        public int? TargetQuantity { get; set; }
        public string? Timestamp { get; set; } = "--";
        public StepStatus Status { get; set; } = StepStatus.Waiting;
        public StepGlyph Glyph { get; set; } = StepGlyph.Gear;
        public string? CustomGlyphText { get; set; }
        public string QuantityPrefix { get; set; } = "Qty: ";
        public string TimestampPrefix { get; set; } = "Updated: ";
        public object? Tag { get; set; }
    }


    public class StepClickedEventArgs : EventArgs
    {
        public int StepIndex { get; }
        public StepItem Step { get; }

        public StepClickedEventArgs(int index, StepItem step)
        {
            StepIndex = index;
            Step = step;
        }
    }

    /// <summary>
    /// Modern Data-Driven Workflow Process Steps control for ZeroUI with vector nodes and transition arrows.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Industrial & SCADA")]
    [DefaultEvent("StepClicked")]
    [Description("Data-Driven Manufacturing Workflow Steps control")]
    [ToolboxBitmap(typeof(ZeroIcons), "ZeroSteps.bmp")]
    public class StepsControl : Control
    {

        private readonly List<StepItem> _steps = new List<StepItem>();
        private readonly List<Rectangle> _stepRects = new List<Rectangle>();
        private int _hoveredIndex = -1;

        public event EventHandler<StepClickedEventArgs>? StepClicked;

        public StepsControl()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(600, 85);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9f);

            ZeroTheme.ThemeChanged += (s, e) => Invalidate();
        }

        [Browsable(false)]
        public IReadOnlyList<StepItem> Steps => _steps;

        public void SetSteps(IEnumerable<StepItem> steps)
        {
            _steps.Clear();
            if (steps != null)
            {
                _steps.AddRange(steps);
            }
            Invalidate();
        }

        public void UpdateStep(string key, int quantity, string? timestamp = null, StepStatus? status = null)
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

            int sideMargin = 16;
            int availableW = Math.Max(100, Width - (sideMargin * 2));

            // Well-proportioned card dimensions: optimal width between 210px and 260px
            int idealCardWidth = 235;
            int minGap = 24;

            int cardWidth;
            int gap;

            if (availableW < (count * idealCardWidth) + ((count - 1) * minGap))
            {
                cardWidth = Math.Max(150, (availableW - ((count - 1) * minGap)) / count);
                gap = (count > 1) ? Math.Max(8, (availableW - (count * cardWidth)) / (count - 1)) : 0;
            }
            else
            {
                cardWidth = idealCardWidth;
                gap = (count > 1) ? (availableW - (count * cardWidth)) / (count - 1) : 0;
            }

            int cardHeight = Math.Min(64, Math.Max(52, Height - 12));
            int cardY = (Height - cardHeight) / 2;

            int currentX = sideMargin;

            for (int i = 0; i < count; i++)
            {
                var step = _steps[i];
                Rectangle cardRect = new Rectangle(currentX, cardY, cardWidth, cardHeight);
                _stepRects.Add(cardRect);

                bool isHovered = (i == _hoveredIndex);
                var palette = ZeroTheme.Colors;

                // 1. Draw Step Card Background
                var (bgCol, borderCol, iconCol, glyphChar) = GetStepColorsAndGlyph(step);

                using (var cardPath = CreateRoundedRectangle(cardRect, 8))
                {
                    using var bgBrush = new SolidBrush(isHovered ? palette.Hover : palette.CardBackground);
                    g.FillPath(bgBrush, cardPath);

                    using var borderPen = new Pen(isHovered ? palette.Primary : palette.Border, isHovered ? 1.5f : 1f);
                    g.DrawPath(borderPen, cardPath);
                }

                // 2. Draw Circular Glyph Icon (vertically centered)
                int iconDiameter = 36;
                int iconX = cardRect.Left + 12;
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

                // 3. Draw Step Text Info (Vertically Centered)
                int textX = iconRect.Right + 10;
                int textWidth = cardRect.Right - textX - 8;

                if (textWidth > 20)
                {
                    int totalTextH = 49;
                    int textY = cardRect.Top + (cardHeight - totalTextH) / 2;

                    // Title
                    using var titleFont = new Font("Segoe UI", 9.25f, FontStyle.Bold);
                    Rectangle titleRect = new Rectangle(textX, textY, textWidth, 18);
                    TextRenderer.DrawText(g, step.Title, titleFont, titleRect, palette.TextPrimary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                    // Quantity
                    using var qtyFont = new Font("Segoe UI", 8.25f, FontStyle.Regular);
                    string qtyText = step.TargetQuantity.HasValue
                        ? $"{step.QuantityPrefix}{step.Quantity:N0} / {step.TargetQuantity.Value:N0}"
                        : $"{step.QuantityPrefix}{step.Quantity:N0}";
                    Rectangle qtyRect = new Rectangle(textX, textY + 17, textWidth, 15);
                    TextRenderer.DrawText(g, qtyText, qtyFont, qtyRect, palette.TextSecondary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                    // Timestamp
                    using var timeFont = new Font("Segoe UI", 7.75f, FontStyle.Regular);
                    string timeText = $"{step.TimestampPrefix}{step.Timestamp ?? "--"}";
                    Rectangle timeRect = new Rectangle(textX, textY + 32, textWidth, 15);
                    TextRenderer.DrawText(g, timeText, timeFont, timeRect, palette.TextSecondary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                }

                currentX += cardWidth;

                // 4. Draw Connecting Transition Arrow (between steps)
                if (i < count - 1)
                {
                    Rectangle arrowRect = new Rectangle(currentX, cardY, gap, cardHeight);
                    int centerY = cardY + (cardHeight / 2);

                    // Subtle horizontal dashed line when gap is wide
                    if (gap >= 48)
                    {
                        using var linePen = new Pen(Color.FromArgb(229, 231, 235), 1.5f);
                        linePen.DashStyle = DashStyle.Dot;
                        g.DrawLine(linePen, currentX + 8, centerY, currentX + gap - 8, centerY);
                    }

                    // Centered transition arrow (→)
                    TextRenderer.DrawText(
                        g,
                        "→",
                        new Font("Segoe UI", 13f, FontStyle.Bold),
                        arrowRect,
                        Color.FromArgb(59, 130, 246),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                    currentX += gap;
                }
            }
        }


        private static (Color bg, Color border, Color icon, string glyph) GetStepColorsAndGlyph(StepItem step)
        {
            string glyph = step.Glyph switch
            {
                StepGlyph.Gear => "⚙",
                StepGlyph.Checkmark => "✔",
                StepGlyph.Warehouse => "🏠",
                StepGlyph.Truck => "🚚",
                StepGlyph.Alert => "⚠",
                StepGlyph.Custom => step.CustomGlyphText ?? "•",
                _ => "•"
            };

            return step.Status switch
            {
                StepStatus.Completed => (Color.FromArgb(246, 255, 237), Color.FromArgb(183, 235, 143), Color.FromArgb(82, 196, 26), step.Glyph == StepGlyph.Checkmark ? "✔" : glyph),
                StepStatus.InProgress => (Color.FromArgb(230, 244, 255), Color.FromArgb(145, 202, 255), Color.FromArgb(22, 119, 255), glyph),
                StepStatus.Warning => (Color.FromArgb(255, 251, 230), Color.FromArgb(255, 229, 143), Color.FromArgb(250, 173, 20), glyph),
                StepStatus.Error => (Color.FromArgb(255, 242, 240), Color.FromArgb(255, 204, 199), Color.FromArgb(255, 77, 79), "✖"),
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
                StepClicked?.Invoke(this, new StepClickedEventArgs(_hoveredIndex, _steps[_hoveredIndex]));
            }
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius) =>
            ZeroUIConfig.CreateRoundedRectangle(rect, radius);
    }

    /// <summary>
    /// Legacy alias for <see cref="StepStatus"/>.
    /// Preserved for backward compatibility.
    /// </summary>
    [Obsolete("ZeroStepStatus is deprecated. Please use StepStatus instead.")]
    public enum ZeroStepStatus
    {
        Waiting = StepStatus.Waiting,
        InProgress = StepStatus.InProgress,
        Completed = StepStatus.Completed,
        Warning = StepStatus.Warning,
        Error = StepStatus.Error
    }

    /// <summary>
    /// Legacy alias for <see cref="StepGlyph"/>.
    /// Preserved for backward compatibility.
    /// </summary>
    [Obsolete("ZeroStepGlyph is deprecated. Please use StepGlyph instead.")]
    public enum ZeroStepGlyph
    {
        Gear = StepGlyph.Gear,
        Checkmark = StepGlyph.Checkmark,
        Warehouse = StepGlyph.Warehouse,
        Truck = StepGlyph.Truck,
        Alert = StepGlyph.Alert,
        Custom = StepGlyph.Custom
    }

    /// <summary>
    /// Legacy alias for <see cref="StepItem"/>.
    /// Preserved for backward compatibility.
    /// </summary>
    [Obsolete("ZeroStepItem is deprecated. Please use StepItem instead.")]
    public class ZeroStepItem : StepItem
    {
    }

    /// <summary>
    /// Legacy alias for <see cref="StepClickedEventArgs"/>.
    /// Preserved for backward compatibility.
    /// </summary>
    [Obsolete("ZeroStepClickedEventArgs is deprecated. Please use StepClickedEventArgs instead.")]
    public class ZeroStepClickedEventArgs : StepClickedEventArgs
    {
        public ZeroStepClickedEventArgs(int index, StepItem step) : base(index, step) { }
    }

    /// <summary>
    /// Legacy alias for <see cref="StepsControl"/>.
    /// Preserved for backward compatibility.
    /// </summary>
    [Obsolete("ZeroSteps is deprecated. Please use StepsControl instead.")]
    [ToolboxItem(false)]
    public class ZeroSteps : StepsControl
    {
    }

    /// <summary>
    /// Legacy alias for <see cref="StepsControl"/>.
    /// Preserved for backward compatibility.
    /// </summary>
    [Obsolete("ZeroStepsControl is deprecated. Please use StepsControl instead.")]
    [ToolboxItem(false)]
    public class ZeroStepsControl : StepsControl
    {
    }
}
