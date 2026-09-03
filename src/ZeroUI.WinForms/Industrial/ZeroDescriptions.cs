using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ZeroUI.WinForms.Industrial
{
    public class ZeroDescriptionItem

    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
        public Color? ValueColor { get; set; }
        public bool IsHighlighted { get; set; }

        public ZeroDescriptionItem() { }

        public ZeroDescriptionItem(string label, string value, Color? valueColor = null, bool isHighlighted = false)
        {
            Label = label;
            Value = value;
            ValueColor = valueColor;
            IsHighlighted = isHighlighted;
        }
    }

    /// <summary>
    /// Modern Key-Value metadata description grid component for ZeroUI.
    /// </summary>
    public class ZeroDescriptions : Control
    {
        private readonly List<ZeroDescriptionItem> _items = new List<ZeroDescriptionItem>();
        private int _columns = 2;
        private int _rowHeight = 28;
        private Color _labelColor = Color.FromArgb(107, 114, 128); // Muted gray
        private Color _valueColor = Color.FromArgb(17, 24, 39);     // Dark gray

        public ZeroDescriptions()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9f);
            Size = new Size(400, 90);
        }

        [Browsable(false)]
        public List<ZeroDescriptionItem> Items => _items;

        [Category("Layout")]
        [DefaultValue(2)]
        public int Columns
        {
            get => _columns;
            set { _columns = Math.Max(1, value); Invalidate(); }
        }

        [Category("Layout")]
        [DefaultValue(28)]
        public int RowHeight
        {
            get => _rowHeight;
            set { _rowHeight = Math.Max(18, value); Invalidate(); }
        }

        [Category("Appearance")]
        public Color LabelColor
        {
            get => _labelColor;
            set { _labelColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        public Color ValueColor
        {
            get => _valueColor;
            set { _valueColor = value; Invalidate(); }
        }

        public void Add(string label, string value, Color? valueColor = null, bool isHighlighted = false)
        {
            _items.Add(new ZeroDescriptionItem(label, value, valueColor, isHighlighted));
            Invalidate();
        }

        public void SetValue(string label, string value, Color? valueColor = null)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (string.Equals(_items[i].Label, label, StringComparison.OrdinalIgnoreCase))
                {
                    _items[i].Value = value;
                    if (valueColor.HasValue) _items[i].ValueColor = valueColor.Value;
                    Invalidate();
                    return;
                }
            }
            Add(label, value, valueColor);
        }

        public void Clear()
        {
            _items.Clear();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int count = _items.Count;
            if (count == 0) return;

            int cols = Math.Max(1, _columns);
            int rowsPerCol = (count + cols - 1) / cols;
            int colWidth = Width / cols;

            using var labelFont = new Font(Font.FontFamily, Font.Size, FontStyle.Regular);
            using var valFont = new Font(Font.FontFamily, Font.Size, FontStyle.Bold);

            for (int i = 0; i < count; i++)
            {
                var item = _items[i];
                int c = i / rowsPerCol;
                int r = i % rowsPerCol;

                int colLeft = c * colWidth;
                int rowTop = r * _rowHeight + 4;

                // Divide label (approx 50%) and value (approx 50%)
                int labelWidth = (int)(colWidth * 0.52f);
                int valWidth = colWidth - labelWidth - 8;

                Rectangle labelRect = new Rectangle(colLeft + 4, rowTop, labelWidth, _rowHeight);
                Rectangle valRect = new Rectangle(colLeft + labelWidth + 4, rowTop, valWidth, _rowHeight);

                // Draw Label
                TextRenderer.DrawText(
                    g,
                    item.Label + ":",
                    labelFont,
                    labelRect,
                    _labelColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                // Draw Value
                Color valCol = item.ValueColor ?? _valueColor;
                TextRenderer.DrawText(
                    g,
                    item.Value,
                    valFont,
                    valRect,
                    valCol,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                // Draw Vertical Column Divider (if multiple columns)
                if (c > 0 && r == 0)
                {
                    using var divPen = new Pen(Color.FromArgb(243, 244, 246), 1f);
                    g.DrawLine(divPen, colLeft, 4, colLeft, Height - 8);
                }
            }
        }
    }
}
