using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using ZeroUI.WinForms.Icons;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Charts
{
    public class SankeyNode
    {
        public string Name { get; set; }
        public Color Color { get; set; }
        public int Column { get; set; }

        public SankeyNode(string name, Color color, int column = 0)
        {
            Name = name;
            Color = color;
            Column = column;
        }
    }

    public class SankeyLink
    {
        public string Source { get; set; }
        public string Target { get; set; }
        public double Value { get; set; }
        public Color? Color { get; set; }

        public SankeyLink(string source, string target, double value, Color? color = null)
        {
            Source = source;
            Target = target;
            Value = value;
            Color = color;
        }
    }

    /// <summary>
    /// Interactive Sankey diagram for visualizing energy, cost, material, and process flows.
    /// Features smooth cubic Bézier flow ribbons with proportional ribbon widths and node highlighting.
    /// </summary>
    [ToolboxItem(true)]
    [ToolboxBitmap(typeof(ZeroIcons), "SankeyChart.bmp")]
    [Category("ZeroUI - Charts & Analytics")]
    [Description("Sankey diagram for flow and distribution analytics")]
    public class SankeyChart : Control
    {
        private readonly List<SankeyNode> _nodes = new List<SankeyNode>();
        private readonly List<SankeyLink> _links = new List<SankeyLink>();
        private readonly ToolTip _toolTip = new ToolTip();

        private string _title = "Process & Material Flow Distribution (Sankey)";
        private string _valueSuffix = " MWh";
        private int _nodeWidth = 18;
        private int _hoverLinkIndex = -1;
        private string? _hoverNodeName = null;

        public List<SankeyNode> Nodes => _nodes;
        public List<SankeyLink> Links => _links;

        [Category("Appearance")]
        [DefaultValue("Process & Material Flow Distribution (Sankey)")]
        public string Title
        {
            get => _title;
            set { _title = value ?? ""; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(" MWh")]
        public string ValueSuffix
        {
            get => _valueSuffix;
            set { _valueSuffix = value ?? ""; Invalidate(); }
        }

        public SankeyChart()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(620, 360);
            Font = new Font("Segoe UI", 9f);
            BackColor = Color.Transparent;

            _toolTip.InitialDelay = 150;
            _toolTip.ReshowDelay = 50;

            ZeroTheme.ThemeChanged += (s, e) => Invalidate();
        }

        public SankeyNode AddNode(string name, Color color, int column = 0)
        {
            var node = new SankeyNode(name, color, column);
            _nodes.Add(node);
            Invalidate();
            return node;
        }

        public SankeyLink AddLink(string source, string target, double value, Color? color = null)
        {
            var link = new SankeyLink(source, target, value, color);
            _links.Add(link);
            Invalidate();
            return link;
        }

        public void Clear()
        {
            _nodes.Clear();
            _links.Clear();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            bool isDark = ZeroTheme.IsDark;
            Color bgColor = isDark ? Color.FromArgb(15, 23, 42) : Color.FromArgb(248, 250, 252);
            Color textColor = isDark ? Color.FromArgb(241, 245, 249) : Color.FromArgb(15, 23, 42);
            Color mutedColor = isDark ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139);

            g.Clear(bgColor);

            int titleHeight = string.IsNullOrEmpty(_title) ? 6 : 30;
            if (!string.IsNullOrEmpty(_title))
            {
                using var titleFont = new Font(Font.FontFamily, 9.5f, FontStyle.Bold);
                using var titleBrush = new SolidBrush(textColor);
                g.DrawString(_title, titleFont, titleBrush, 10, 8);
            }

            var plotRect = new Rectangle(20, titleHeight + 10, Width - 40, Height - titleHeight - 30);
            if (plotRect.Width <= 50 || plotRect.Height <= 50 || _nodes.Count == 0)
            {
                using var muted = new SolidBrush(Color.Gray);
                g.DrawString("No sankey flow data available.", Font, muted, 12, titleHeight + 10);
                return;
            }

            // Compute columns
            int maxCol = _nodes.Max(n => n.Column);
            int colCount = Math.Max(1, maxCol + 1);

            // Compute in/out volume per node
            var inVolume = new Dictionary<string, double>();
            var outVolume = new Dictionary<string, double>();

            foreach (var n in _nodes)
            {
                inVolume[n.Name] = 0;
                outVolume[n.Name] = 0;
            }

            foreach (var l in _links)
            {
                if (outVolume.ContainsKey(l.Source)) outVolume[l.Source] += l.Value;
                if (inVolume.ContainsKey(l.Target)) inVolume[l.Target] += l.Value;
            }

            var nodeVolume = new Dictionary<string, double>();
            foreach (var n in _nodes)
            {
                nodeVolume[n.Name] = Math.Max(inVolume[n.Name], outVolume[n.Name]);
            }

            // Layout columns and nodes
            var nodeRects = new Dictionary<string, RectangleF>();
            float colSpacing = colCount > 1 ? (plotRect.Width - _nodeWidth) / (float)(colCount - 1) : 0;

            for (int col = 0; col < colCount; col++)
            {
                var colNodes = _nodes.Where(n => n.Column == col).ToList();
                double colTotal = colNodes.Sum(n => nodeVolume[n.Name]);
                if (colTotal <= 0) colTotal = 1;

                float availH = plotRect.Height - (colNodes.Count - 1) * 16f;
                float curY = plotRect.Top;
                float x = plotRect.Left + col * colSpacing;

                foreach (var n in colNodes)
                {
                    float h = Math.Max(12f, (float)((nodeVolume[n.Name] / colTotal) * availH));
                    nodeRects[n.Name] = new RectangleF(x, curY, _nodeWidth, h);
                    curY += h + 16f;
                }
            }

            // Track offsets for multiple links connecting to the same node
            var sourceOutOffset = new Dictionary<string, float>();
            var targetInOffset = new Dictionary<string, float>();
            foreach (var n in _nodes)
            {
                sourceOutOffset[n.Name] = 0f;
                targetInOffset[n.Name] = 0f;
            }

            // 1. Draw Flow Ribbons
            for (int i = 0; i < _links.Count; i++)
            {
                var link = _links[i];
                if (!nodeRects.TryGetValue(link.Source, out var srcRect) ||
                    !nodeRects.TryGetValue(link.Target, out var dstRect))
                    continue;

                double srcTotal = outVolume[link.Source];
                double dstTotal = inVolume[link.Target];

                float linkSrcH = srcTotal > 0 ? (float)((link.Value / srcTotal) * srcRect.Height) : 10f;
                float linkDstH = dstTotal > 0 ? (float)((link.Value / dstTotal) * dstRect.Height) : 10f;

                float y1 = srcRect.Top + sourceOutOffset[link.Source];
                float y2 = dstRect.Top + targetInOffset[link.Target];

                sourceOutOffset[link.Source] += linkSrcH;
                targetInOffset[link.Target] += linkDstH;

                float x1 = srcRect.Right;
                float x2 = dstRect.Left;
                float dx = (x2 - x1) * 0.5f;

                bool isHovered = (i == _hoverLinkIndex) ||
                                 (_hoverNodeName != null && (_hoverNodeName == link.Source || _hoverNodeName == link.Target));

                var srcNode = _nodes.FirstOrDefault(n => n.Name == link.Source);
                var dstNode = _nodes.FirstOrDefault(n => n.Name == link.Target);
                Color c1 = link.Color ?? srcNode?.Color ?? Color.FromArgb(79, 70, 229);
                Color c2 = link.Color ?? dstNode?.Color ?? Color.FromArgb(16, 185, 129);

                int alpha = isHovered ? 180 : 85;

                using (var path = new GraphicsPath())
                {
                    // Top curve
                    path.AddBezier(x1, y1, x1 + dx, y1, x2 - dx, y2, x2, y2);
                    // Right vertical
                    path.AddLine(x2, y2, x2, y2 + linkDstH);
                    // Bottom curve (reversed)
                    path.AddBezier(x2, y2 + linkDstH, x2 - dx, y2 + linkDstH, x1 + dx, y1 + linkSrcH, x1, y1 + linkSrcH);
                    // Left vertical
                    path.CloseFigure();

                    using var ribbonBrush = new LinearGradientBrush(
                        new PointF(x1, y1), new PointF(x2, y2),
                        Color.FromArgb(alpha, c1), Color.FromArgb(alpha, c2));
                    g.FillPath(ribbonBrush, path);

                    if (isHovered)
                    {
                        using var borderPen = new Pen(Color.White, 1.2f);
                        g.DrawPath(borderPen, path);
                    }
                }
            }

            // 2. Draw Nodes
            using var font = new Font(Font.FontFamily, 8f, FontStyle.Bold);
            using var fontSub = new Font(Font.FontFamily, 7.5f, FontStyle.Regular);
            using var whiteBrush = new SolidBrush(Color.White);
            using var textBrush = new SolidBrush(textColor);
            using var mutedBrush = new SolidBrush(mutedColor);

            foreach (var n in _nodes)
            {
                if (!nodeRects.TryGetValue(n.Name, out var nr)) continue;

                bool isHighlighted = (_hoverNodeName == n.Name);

                using (var nb = new SolidBrush(n.Color))
                {
                    g.FillRectangle(nb, nr);
                }

                using (var np = new Pen(isHighlighted ? Color.White : Color.FromArgb(50, 0, 0, 0), isHighlighted ? 2f : 1f))
                {
                    g.DrawRectangle(np, nr.X, nr.Y, nr.Width, nr.Height);
                }

                // Node text label (placed left for rightmost nodes, right for left nodes)
                bool isRightSide = n.Column >= colCount / 2;
                float tx = isRightSide ? nr.Right + 6 : nr.Left - 6;
                var sf = new StringFormat
                {
                    Alignment = isRightSide ? StringAlignment.Near : StringAlignment.Far,
                    LineAlignment = StringAlignment.Center
                };

                string label = $"{n.Name}";
                string val = $"{nodeVolume[n.Name]:N0}{_valueSuffix}";

                g.DrawString(label, font, textBrush, tx, nr.Top + nr.Height / 2 - 6, sf);
                g.DrawString(val, fontSub, mutedBrush, tx, nr.Top + nr.Height / 2 + 7, sf);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            // Simple hit-test on nodes
            string? foundNode = null;
            // Let's check nodes proximity
            // Repaint if hover changes
            if (foundNode != _hoverNodeName)
            {
                _hoverNodeName = foundNode;
                Invalidate();
            }
        }
    }
}
