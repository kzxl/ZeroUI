using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Industrial
{
    /// <summary>
    /// Represents a hierarchical node within the ZeroTreeList control.
    /// </summary>
    public class ZeroTreeNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Text { get; set; } = "";
        public string SubText { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Badge { get; set; } = "";
        public Color? BadgeColor { get; set; }
        public bool IsExpanded { get; set; } = true;
        public CheckState CheckState { get; set; } = CheckState.Unchecked;
        public object? Tag { get; set; }

        public ZeroTreeNode? Parent { get; internal set; }
        public List<ZeroTreeNode> Children { get; } = new List<ZeroTreeNode>();

        internal Rectangle RowBounds;
        internal Rectangle ChevronBounds;
        internal Rectangle CheckBounds;

        public ZeroTreeNode() { }

        public ZeroTreeNode(string text, string icon = "", string subText = "")
        {
            Text = text;
            Icon = icon;
            SubText = subText;
        }

        public int Level
        {
            get
            {
                int lvl = 0;
                var curr = Parent;
                while (curr != null)
                {
                    lvl++;
                    curr = curr.Parent;
                }
                return lvl;
            }
        }

        public bool HasChildren => Children.Count > 0;

        public ZeroTreeNode AddChild(ZeroTreeNode child)
        {
            child.Parent = this;
            Children.Add(child);
            return child;
        }

        public ZeroTreeNode AddChild(string text, string icon = "", string subText = "")
        {
            var child = new ZeroTreeNode(text, icon, subText) { Parent = this };
            Children.Add(child);
            return child;
        }

        public void ExpandAll()
        {
            IsExpanded = true;
            foreach (var child in Children)
            {
                child.ExpandAll();
            }
        }

        public void CollapseAll()
        {
            IsExpanded = false;
            foreach (var child in Children)
            {
                child.CollapseAll();
            }
        }

        public void SetCheckState(CheckState state, bool cascade = true)
        {
            CheckState = state;
            if (cascade)
            {
                foreach (var child in Children)
                {
                    child.SetCheckState(state, true);
                }
            }

            if (Parent != null && cascade)
            {
                Parent.UpdateParentCheckState();
            }
        }

        internal void UpdateParentCheckState()
        {
            if (Children.Count == 0) return;

            int checkedCount = 0;
            int indeterminateCount = 0;

            foreach (var child in Children)
            {
                if (child.CheckState == CheckState.Checked) checkedCount++;
                else if (child.CheckState == CheckState.Indeterminate) indeterminateCount++;
            }

            if (checkedCount == Children.Count)
            {
                CheckState = CheckState.Checked;
            }
            else if (checkedCount == 0 && indeterminateCount == 0)
            {
                CheckState = CheckState.Unchecked;
            }
            else
            {
                CheckState = CheckState.Indeterminate;
            }

            Parent?.UpdateParentCheckState();
        }
    }

    /// <summary>
    /// High-performance, virtualized hierarchical Tree and Multi-Level BOM TreeList control for ZeroUI.
    /// Supports expand/collapse, tri-state cascading checkboxes, hierarchy connection lines, search filtering,
    /// and theme reactivity (Clean Light / Obsidian Dark).
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Industrial & SCADA")]
    [DefaultEvent("NodeSelected")]
    [Description("High-performance virtualized hierarchical Tree and BOM TreeList control")]
    public class ZeroTreeList : Control
    {
        private readonly List<ZeroTreeNode> _nodes = new List<ZeroTreeNode>();
        private readonly List<ZeroTreeNode> _visibleNodes = new List<ZeroTreeNode>();

        private int _rowHeight = 30;
        private int _indentWidth = 24;
        private bool _showCheckBoxes = true;
        private bool _showLines = true;
        private string _filterText = "";

        private ZeroTreeNode? _selectedNode;
        private ZeroTreeNode? _hoveredNode;
        private bool _hoveredOnChevron = false;
        private bool _hoveredOnCheck = false;

        private int _scrollOffset = 0;
        private readonly VScrollBar _vScrollBar;

        public event EventHandler<ZeroTreeNode>? NodeSelected;
        public event EventHandler<ZeroTreeNode>? NodeCheckChanged;
        public event EventHandler<ZeroTreeNode>? NodeExpandedChanged;

        public ZeroTreeList()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable, true);

            Size = new Size(380, 420);
            Font = new Font("Segoe UI", 9f);
            BackColor = Color.FromArgb(15, 23, 42); // Obsidian Dark default

            _vScrollBar = new VScrollBar
            {
                Dock = DockStyle.Right,
                Width = 14,
                Visible = false
            };
            _vScrollBar.Scroll += (s, e) =>
            {
                _scrollOffset = _vScrollBar.Value;
                Invalidate();
            };
            Controls.Add(_vScrollBar);

            MouseWheel += OnMouseWheelScroll;
            ZeroTheme.ThemeChanged += (s, e) => Invalidate();
        }

        [Browsable(false)]
        public List<ZeroTreeNode> Nodes => _nodes;

        [Category("Appearance")]
        [DefaultValue(30)]
        public int RowHeight
        {
            get => _rowHeight;
            set
            {
                if (_rowHeight != value && value >= 18)
                {
                    _rowHeight = value;
                    UpdateVisibleNodes();
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue(24)]
        public int IndentWidth
        {
            get => _indentWidth;
            set
            {
                if (_indentWidth != value && value >= 12)
                {
                    _indentWidth = value;
                    Invalidate();
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool ShowCheckBoxes
        {
            get => _showCheckBoxes;
            set
            {
                if (_showCheckBoxes != value)
                {
                    _showCheckBoxes = value;
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        public bool ShowLines
        {
            get => _showLines;
            set
            {
                if (_showLines != value)
                {
                    _showLines = value;
                    Invalidate();
                }
            }
        }

        [Category("Behavior")]
        public string FilterText
        {
            get => _filterText;
            set
            {
                var val = value?.Trim() ?? "";
                if (_filterText != val)
                {
                    _filterText = val;
                    UpdateVisibleNodes();
                    Invalidate();
                }
            }
        }

        [Browsable(false)]
        public ZeroTreeNode? SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (_selectedNode != value)
                {
                    _selectedNode = value;
                    Invalidate();
                    if (_selectedNode != null)
                    {
                        NodeSelected?.Invoke(this, _selectedNode);
                    }
                }
            }
        }

        public void AddNode(ZeroTreeNode node)
        {
            _nodes.Add(node);
            UpdateVisibleNodes();
            Invalidate();
        }

        public void ClearNodes()
        {
            _nodes.Clear();
            _visibleNodes.Clear();
            _selectedNode = null;
            _hoveredNode = null;
            UpdateScrollBar();
            Invalidate();
        }

        public void ExpandAll()
        {
            foreach (var node in _nodes)
            {
                node.ExpandAll();
            }
            UpdateVisibleNodes();
            Invalidate();
        }

        public void CollapseAll()
        {
            foreach (var node in _nodes)
            {
                node.CollapseAll();
            }
            UpdateVisibleNodes();
            Invalidate();
        }

        public void UpdateVisibleNodes()
        {
            _visibleNodes.Clear();
            bool hasFilter = !string.IsNullOrEmpty(_filterText);

            foreach (var root in _nodes)
            {
                CollectVisible(root, hasFilter);
            }

            UpdateScrollBar();
        }

        private bool CollectVisible(ZeroTreeNode node, bool hasFilter)
        {
            if (!hasFilter)
            {
                _visibleNodes.Add(node);
                if (node.IsExpanded && node.HasChildren)
                {
                    foreach (var child in node.Children)
                    {
                        CollectVisible(child, false);
                    }
                }
                return true;
            }

            // In filter mode, match self or any descendants
            bool selfMatches = node.Text.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                               node.SubText.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0;

            var matchingChildren = new List<ZeroTreeNode>();
            foreach (var child in node.Children)
            {
                if (CollectVisibleChildCheck(child))
                {
                    matchingChildren.Add(child);
                }
            }

            if (selfMatches || matchingChildren.Count > 0)
            {
                _visibleNodes.Add(node);
                node.IsExpanded = true; // Auto-expand when filtering
                foreach (var child in matchingChildren)
                {
                    CollectVisible(child, true);
                }
                return true;
            }

            return false;
        }

        private bool CollectVisibleChildCheck(ZeroTreeNode node)
        {
            if (node.Text.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                node.SubText.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            foreach (var child in node.Children)
            {
                if (CollectVisibleChildCheck(child)) return true;
            }

            return false;
        }

        private void UpdateScrollBar()
        {
            int totalHeight = _visibleNodes.Count * _rowHeight;
            int viewHeight = Height;

            if (totalHeight > viewHeight)
            {
                _vScrollBar.Visible = true;
                _vScrollBar.Maximum = Math.Max(0, _visibleNodes.Count - (viewHeight / _rowHeight) + 1);
                _vScrollBar.LargeChange = Math.Max(1, viewHeight / _rowHeight);
            }
            else
            {
                _vScrollBar.Visible = false;
                _scrollOffset = 0;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScrollBar();
        }

        private void OnMouseWheelScroll(object? sender, MouseEventArgs e)
        {
            if (!_vScrollBar.Visible) return;
            int delta = e.Delta > 0 ? -2 : 2;
            int newVal = Math.Max(0, Math.Min(_vScrollBar.Maximum, _scrollOffset + delta));
            if (newVal != _scrollOffset)
            {
                _scrollOffset = newVal;
                _vScrollBar.Value = _scrollOffset;
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int clientWidth = _vScrollBar.Visible ? Width - _vScrollBar.Width : Width;
            if (e.X > clientWidth) return;

            int index = (e.Y / _rowHeight) + _scrollOffset;
            if (index >= 0 && index < _visibleNodes.Count)
            {
                var node = _visibleNodes[index];
                bool onChevron = node.ChevronBounds.Contains(e.Location);
                bool onCheck = node.CheckBounds.Contains(e.Location);

                if (_hoveredNode != node || _hoveredOnChevron != onChevron || _hoveredOnCheck != onCheck)
                {
                    _hoveredNode = node;
                    _hoveredOnChevron = onChevron;
                    _hoveredOnCheck = onCheck;
                    Cursor = (onChevron || onCheck) ? Cursors.Hand : Cursors.Default;
                    Invalidate();
                }
            }
            else if (_hoveredNode != null)
            {
                _hoveredNode = null;
                Cursor = Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoveredNode = null;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            int clientWidth = _vScrollBar.Visible ? Width - _vScrollBar.Width : Width;
            if (e.X > clientWidth) return;

            int index = (e.Y / _rowHeight) + _scrollOffset;
            if (index < 0 || index >= _visibleNodes.Count) return;

            var node = _visibleNodes[index];

            if (node.ChevronBounds.Contains(e.Location) && node.HasChildren)
            {
                node.IsExpanded = !node.IsExpanded;
                UpdateVisibleNodes();
                NodeExpandedChanged?.Invoke(this, node);
                Invalidate();
                return;
            }

            if (_showCheckBoxes && node.CheckBounds.Contains(e.Location))
            {
                var nextState = (node.CheckState == CheckState.Checked) ? CheckState.Unchecked : CheckState.Checked;
                node.SetCheckState(nextState, true);
                NodeCheckChanged?.Invoke(this, node);
                Invalidate();
                return;
            }

            SelectedNode = node;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var palette = ZeroTheme.Colors;
            g.Clear(palette.Background);

            int clientWidth = _vScrollBar.Visible ? Width - _vScrollBar.Width : Width;
            int startIdx = _scrollOffset;
            int maxVisible = (Height / _rowHeight) + 2;
            int endIdx = Math.Min(_visibleNodes.Count, startIdx + maxVisible);

            using var penGuide = new Pen(palette.Border, 1f) { DashStyle = DashStyle.Dot };
            using var fontText = new Font(Font.FontFamily, 9.2f, FontStyle.Regular);
            using var fontBold = new Font(Font.FontFamily, 9.2f, FontStyle.Bold);
            using var fontSub = new Font(Font.FontFamily, 8f, FontStyle.Regular);
            using var fontBadge = new Font(Font.FontFamily, 7.5f, FontStyle.Bold);

            for (int i = startIdx; i < endIdx; i++)
            {
                var node = _visibleNodes[i];
                int y = (i - startIdx) * _rowHeight;
                node.RowBounds = new Rectangle(0, y, clientWidth, _rowHeight);

                bool isSelected = node == _selectedNode;
                bool isHovered = node == _hoveredNode;

                // 1. Draw Row Background
                if (isSelected)
                {
                    using var brushSel = new SolidBrush(Color.FromArgb(40, palette.Primary));
                    g.FillRectangle(brushSel, node.RowBounds);
                    using var penLeft = new SolidBrush(palette.Primary);
                    g.FillRectangle(penLeft, new Rectangle(0, y, 3, _rowHeight));
                }
                else if (isHovered)
                {
                    using var brushHov = new SolidBrush(Color.FromArgb(15, palette.Primary));
                    g.FillRectangle(brushHov, node.RowBounds);
                }

                int indentX = 12 + (node.Level * _indentWidth);

                // 2. Draw Connecting Hierarchy Guidelines
                if (_showLines && node.Level > 0)
                {
                    int parentLineX = indentX - (_indentWidth / 2);
                    int midY = y + (_rowHeight / 2);
                    g.DrawLine(penGuide, parentLineX, y, parentLineX, midY);
                    g.DrawLine(penGuide, parentLineX, midY, indentX - 4, midY);
                }

                // 3. Draw Chevron Glyph (▶ / ▼)
                node.ChevronBounds = new Rectangle(indentX, y + ((_rowHeight - 16) / 2), 16, 16);
                if (node.HasChildren)
                {
                    Color chevColor = (_hoveredNode == node && _hoveredOnChevron) ? palette.Primary : palette.TextSecondary;
                    DrawChevron(g, node.ChevronBounds, node.IsExpanded, chevColor);
                }

                int curX = indentX + 18;

                // 4. Draw Checkbox
                if (_showCheckBoxes)
                {
                    node.CheckBounds = new Rectangle(curX, y + ((_rowHeight - 16) / 2), 16, 16);
                    DrawCheckBox(g, node.CheckBounds, node.CheckState, palette);
                    curX += 22;
                }
                else
                {
                    node.CheckBounds = Rectangle.Empty;
                }

                // 5. Draw Icon / Glyph
                if (!string.IsNullOrEmpty(node.Icon))
                {
                    using var iconFont = new Font("Segoe UI Emoji", 10f);
                    using var brushIcon = new SolidBrush(palette.TextPrimary);
                    g.DrawString(node.Icon, iconFont, brushIcon, curX, y + ((_rowHeight - 18) / 2));
                    curX += 20;
                }

                // 6. Draw Primary Text
                Color textColor = isSelected ? palette.Primary : palette.TextPrimary;
                using (var brushText = new SolidBrush(textColor))
                {
                    var activeFont = node.HasChildren ? fontBold : fontText;
                    g.DrawString(node.Text, activeFont, brushText, curX, y + ((_rowHeight - 18) / 2));
                    var sz = g.MeasureString(node.Text, activeFont);
                    curX += (int)sz.Width + 8;
                }

                // 7. Draw SubText (e.g. description, part code, quantity)
                if (!string.IsNullOrEmpty(node.SubText) && curX < clientWidth - 70)
                {
                    using var brushSub = new SolidBrush(palette.TextSecondary);
                    g.DrawString(node.SubText, fontSub, brushSub, curX, y + ((_rowHeight - 14) / 2) + 1);
                    var szSub = g.MeasureString(node.SubText, fontSub);
                    curX += (int)szSub.Width + 8;
                }

                // 8. Draw Status Badge (if any)
                if (!string.IsNullOrEmpty(node.Badge))
                {
                    var bColor = node.BadgeColor ?? palette.Primary;
                    var bTextSz = g.MeasureString(node.Badge, fontBadge);
                    int badgeW = (int)bTextSz.Width + 10;
                    int badgeH = 18;
                    int badgeX = clientWidth - badgeW - 12;
                    int badgeY = y + ((_rowHeight - badgeH) / 2);

                    var bRect = new Rectangle(badgeX, badgeY, badgeW, badgeH);
                    using var bBrush = new SolidBrush(Color.FromArgb(35, bColor));
                    using var bPen = new Pen(bColor, 1f);
                    using var bPath = CreateRoundedRect(bRect, 4);
                    g.FillPath(bBrush, bPath);
                    g.DrawPath(bPen, bPath);

                    using var bTextBrush = new SolidBrush(bColor);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(node.Badge, fontBadge, bTextBrush, bRect, sf);
                }

                // Separator bottom line
                using var penSep = new Pen(Color.FromArgb(12, palette.Border));
                g.DrawLine(penSep, 0, y + _rowHeight - 1, clientWidth, y + _rowHeight - 1);
            }
        }

        private void DrawChevron(Graphics g, Rectangle bounds, bool isExpanded, Color color)
        {
            using var brush = new SolidBrush(color);
            var center = new PointF(bounds.X + (bounds.Width / 2f), bounds.Y + (bounds.Height / 2f));
            PointF[] points;

            if (isExpanded)
            {
                // Down arrow (▼)
                points = new PointF[]
                {
                    new PointF(center.X - 4f, center.Y - 2.5f),
                    new PointF(center.X + 4f, center.Y - 2.5f),
                    new PointF(center.X, center.Y + 3.5f)
                };
            }
            else
            {
                // Right arrow (▶)
                points = new PointF[]
                {
                    new PointF(center.X - 2.5f, center.Y - 4f),
                    new PointF(center.X + 3.5f, center.Y),
                    new PointF(center.X - 2.5f, center.Y + 4f)
                };
            }

            g.FillPolygon(brush, points);
        }

        private void DrawCheckBox(Graphics g, Rectangle bounds, CheckState state, ZeroThemePalette palette)
        {
            using var path = CreateRoundedRect(bounds, 3);

            if (state == CheckState.Checked)
            {
                using var brush = new SolidBrush(palette.Primary);
                g.FillPath(brush, path);

                using var penCheck = new Pen(Color.White, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                PointF p1 = new PointF(bounds.X + 3.5f, bounds.Y + 8f);
                PointF p2 = new PointF(bounds.X + 6.5f, bounds.Y + 11.5f);
                PointF p3 = new PointF(bounds.X + 12.5f, bounds.Y + 4.5f);
                g.DrawLines(penCheck, new[] { p1, p2, p3 });
            }
            else if (state == CheckState.Indeterminate)
            {
                using var brush = new SolidBrush(palette.Primary);
                g.FillPath(brush, path);

                using var brushBar = new SolidBrush(Color.White);
                g.FillRectangle(brushBar, new Rectangle(bounds.X + 3, bounds.Y + 7, 10, 2));
            }
            else
            {
                using var brushBg = new SolidBrush(palette.Surface);
                using var penBorder = new Pen(palette.Border, 1.2f);
                g.FillPath(brushBg, path);
                g.DrawPath(penBorder, path);
            }
        }

        private static GraphicsPath CreateRoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
