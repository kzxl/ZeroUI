using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Industrial
{
    public enum ZeroTimelineStatus

    {
        Completed,
        InProgress,
        Pending,
        Error
    }

    public class ZeroTimelineItem
    {
        public string Title { get; set; } = "Process Node";
        public string Timestamp { get; set; } = "";
        public string? Description { get; set; }
        public ZeroTimelineStatus Status { get; set; } = ZeroTimelineStatus.Completed;
        public object? Tag { get; set; }

        public ZeroTimelineItem() { }

        public ZeroTimelineItem(string title, string timestamp, string? description = null, ZeroTimelineStatus status = ZeroTimelineStatus.Completed)
        {
            Title = title;
            Timestamp = timestamp;
            Description = description;
            Status = status;
        }
    }

    /// <summary>
    /// Modern vertical timeline control for lot tracking, manufacturing journals, and audit trails.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Industrial & SCADA")]
    [Description("Vertical timeline control for lot tracking and manufacturing audit trails")]
    public class ZeroTimeline : Control
    {

        private readonly List<ZeroTimelineItem> _items = new List<ZeroTimelineItem>();
        private int _itemSpacing = 52;
        private int _nodeX = 24;

        public ZeroTimeline()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9f);
            Size = new Size(320, 240);

            ZeroTheme.ThemeChanged += (s, e) => Invalidate();
        }

        [Browsable(false)]
        public List<ZeroTimelineItem> Items => _items;

        [Category("Layout")]
        [DefaultValue(52)]
        public int ItemSpacing
        {
            get => _itemSpacing;
            set { _itemSpacing = Math.Max(36, value); Invalidate(); }
        }

        public void Add(string title, string timestamp, string? description = null, ZeroTimelineStatus status = ZeroTimelineStatus.Completed)
        {
            _items.Add(new ZeroTimelineItem(title, timestamp, description, status));
            Invalidate();
        }

        public void Clear()
        {
            _items.Clear();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int count = _items.Count;
            if (count == 0) return;

            int startY = 20;

            var palette = ZeroTheme.Colors;

            // 1. Draw Vertical Connecting Line
            if (count > 1)
            {
                int endY = startY + (count - 1) * _itemSpacing;
                using var linePen = new Pen(palette.Border, 2f);
                g.DrawLine(linePen, _nodeX, startY, _nodeX, endY);
            }

            // 2. Draw Nodes & Text
            for (int i = 0; i < count; i++)
            {
                var item = _items[i];
                int currentY = startY + (i * _itemSpacing);

                // Draw Node Circle
                int nodeDiameter = 12;
                var (nodeColor, ringColor) = GetNodeColors(item.Status);

                Rectangle nodeRect = new Rectangle(_nodeX - (nodeDiameter / 2), currentY - (nodeDiameter / 2), nodeDiameter, nodeDiameter);

                // Outer Ring
                using (var ringBrush = new SolidBrush(ringColor))
                {
                    g.FillEllipse(ringBrush, nodeRect.X - 3, nodeRect.Y - 3, nodeDiameter + 6, nodeDiameter + 6);
                }

                // Inner Dot
                using (var nodeBrush = new SolidBrush(nodeColor))
                {
                    g.FillEllipse(nodeBrush, nodeRect);
                }

                // Text: Title & Timestamp
                int textX = _nodeX + 16;
                int textWidth = Width - textX - 12;

                if (textWidth > 20)
                {
                    using var titleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                    Size titleSize = TextRenderer.MeasureText(g, item.Title, titleFont);
                    Rectangle titleRect = new Rectangle(textX, currentY - 8, titleSize.Width + 4, 18);
                    TextRenderer.DrawText(g, item.Title, titleFont, titleRect, palette.TextPrimary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                    // Timestamp
                    if (!string.IsNullOrEmpty(item.Timestamp))
                    {
                        using var timeFont = new Font("Segoe UI", 8f, FontStyle.Regular);
                        Rectangle timeRect = new Rectangle(titleRect.Right + 6, currentY - 8, textWidth - titleRect.Width - 10, 18);
                        TextRenderer.DrawText(g, item.Timestamp, timeFont, timeRect, palette.TextSecondary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                    }

                    // Description (if present)
                    if (!string.IsNullOrEmpty(item.Description))
                    {
                        using var descFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
                        Rectangle descRect = new Rectangle(textX, currentY + 11, textWidth, 18);
                        TextRenderer.DrawText(g, item.Description, descFont, descRect, palette.TextSecondary, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);
                    }
                }
            }
        }

        private static (Color node, Color ring) GetNodeColors(ZeroTimelineStatus status) => status switch
        {
            ZeroTimelineStatus.Completed => (Color.FromArgb(16, 185, 129), Color.FromArgb(209, 250, 229)), // Emerald
            ZeroTimelineStatus.InProgress => (Color.FromArgb(59, 130, 246), Color.FromArgb(219, 234, 254)), // Blue
            ZeroTimelineStatus.Error => (Color.FromArgb(239, 68, 68), Color.FromArgb(254, 226, 226)),      // Red
            _ => (Color.FromArgb(156, 163, 175), Color.FromArgb(243, 244, 246))                            // Slate
        };
    }
}
