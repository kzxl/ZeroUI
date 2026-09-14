using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using ZeroUI.Core.Process;
using ZeroUI.WinForms.Icons;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Industrial
{
    /// <summary>
    /// High-performance interactive business flowchart and process map control.
    /// Supports multi-lane workflow visualization, card tasks, decision diamonds,
    /// orthogonal transition arrows with branching labels, UserControl action triggers,
    /// and interactive design-mode customization.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Industrial & SCADA")]
    [DefaultEvent("NodeClicked")]
    [Description("Interactive Business Process Flowchart & Navigation Map control with custom action binding")]
    [ToolboxBitmap(typeof(ZeroIcons), "ProcessMap.bmp")]
    public class ProcessMap : Control
    {
        private ProcessFlowDefinition _definition = new ProcessFlowDefinition();
        private bool _isDesignMode = false;
        private bool _showGrid = true;
        private bool _autoExecuteAction = true;

        private float _zoom = 1.0f;
        private PointF _panOffset = new PointF(0, 0);

        private bool _isPanning = false;
        private PointF _panStartMouse;
        private PointF _panStartOffset;

        private ProcessFlowNode? _selectedNode;
        private ProcessFlowNode? _hoveredNode;
        private bool _isDraggingNode = false;
        private PointF _nodeDragStartMouse;
        private PointF _nodeDragStartPos;

        private bool _isConnecting = false;
        private ProcessFlowNode? _connectSourceNode;
        private ProcessPortPosition _connectSourcePort;
        private PointF _connectCurrentMouse;
        private ProcessPortPosition? _hoveredPort;
        private ProcessFlowNode? _hoveredPortNode;

        private ProcessFlowConnection? _selectedConnection;
        private ProcessFlowConnection? _hoveredConnection;

        private enum ZoomHudButton
        {
            None,
            ZoomOut,
            ResetZoom,
            ZoomIn,
            Fit
        }

        private bool _wheelZoomRequiresCtrl = true;
        private bool _enableWheelPan = true;
        private ZoomHudButton _hoveredHudButton = ZoomHudButton.None;

        private ContextMenuStrip _contextMenu = null!;
        private ToolStripMenuItem _mnuEditTitle = null!;
        private ToolStripMenuItem _mnuConnectTo = null!;
        private ToolStripMenuItem _mnuAssignAction = null!;
        private ToolStripMenuItem _mnuChangeShape = null!;
        private ToolStripMenuItem _mnuDeleteNode = null!;
        private ToolStripMenuItem _mnuAddStep = null!;

        private ContextMenuStrip _connContextMenu = null!;
        private ToolStripMenuItem _mnuEditConnLabel = null!;
        private ToolStripMenuItem _mnuDeleteConn = null!;
        private ToolStripMenuItem _mnuChangeConnColor = null!;

        public event EventHandler<ProcessFlowNode>? NodeClicked;
        public event EventHandler<ProcessActionContext>? ActionTriggered;
        public event EventHandler? DefinitionChanged;

        public ProcessMap()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(900, 600);
            Font = new Font("Segoe UI", 9f);
            BackColor = Color.FromArgb(248, 250, 252);

            InitContextMenu();

            ZeroTheme.ThemeChanged += (s, e) => Invalidate();
        }

        private void InitContextMenu()
        {
            _contextMenu = new ContextMenuStrip();
            _mnuEditTitle = new ToolStripMenuItem("Edit Title & Subtitle...", null, OnEditTitleClicked);
            _mnuConnectTo = new ToolStripMenuItem("🔗 Connect to Step...", null);
            _mnuAssignAction = new ToolStripMenuItem("Assign Navigation Action...", null);
            _mnuChangeShape = new ToolStripMenuItem("Change Shape", null);

            var mnuShapeCard = new ToolStripMenuItem("Task Card", null, (s, e) => SetSelectedNodeShape(ProcessNodeShape.TaskCard));
            var mnuShapeDiamond = new ToolStripMenuItem("Decision Diamond", null, (s, e) => SetSelectedNodeShape(ProcessNodeShape.DecisionDiamond));
            var mnuShapeStart = new ToolStripMenuItem("Start Terminal", null, (s, e) => SetSelectedNodeShape(ProcessNodeShape.StartTerminal));
            var mnuShapeEnd = new ToolStripMenuItem("End Terminal", null, (s, e) => SetSelectedNodeShape(ProcessNodeShape.EndTerminal));
            _mnuChangeShape.DropDownItems.AddRange(new ToolStripItem[] { mnuShapeCard, mnuShapeDiamond, mnuShapeStart, mnuShapeEnd });

            _mnuDeleteNode = new ToolStripMenuItem("Delete Step", null, OnDeleteNodeClicked);
            _mnuAddStep = new ToolStripMenuItem("Add New Step Here", null, OnAddStepClicked);

            _contextMenu.Items.AddRange(new ToolStripItem[] {
                _mnuAddStep,
                new ToolStripSeparator(),
                _mnuConnectTo,
                _mnuEditTitle,
                _mnuAssignAction,
                _mnuChangeShape,
                new ToolStripSeparator(),
                _mnuDeleteNode
            });

            _contextMenu.Opening += OnContextMenuOpening;

            // Connection context menu
            _connContextMenu = new ContextMenuStrip();
            _mnuEditConnLabel = new ToolStripMenuItem("✏ Edit Branch Label...", null, OnEditConnLabelClicked);
            _mnuChangeConnColor = new ToolStripMenuItem("🎨 Change Line Color", null);

            var colors = new (string Name, string Hex)[]
            {
                ("Sky Blue (Default)", "#0EA5E9"),
                ("Emerald Green (Pass/Approve)", "#10B981"),
                ("Rose Red (Reject/Fail)", "#EF4444"),
                ("Amber Orange (Warning)", "#F59E0B"),
                ("Purple (Alternative)", "#8B5CF6")
            };
            foreach (var c in colors)
            {
                string hex = c.Hex;
                var itm = new ToolStripMenuItem(c.Name, null, (s, e) =>
                {
                    if (_selectedConnection != null)
                    {
                        _selectedConnection.StrokeColorHex = hex;
                        Invalidate();
                        DefinitionChanged?.Invoke(this, EventArgs.Empty);
                    }
                });
                _mnuChangeConnColor.DropDownItems.Add(itm);
            }

            var mnuToggleDashed = new ToolStripMenuItem("➖ Toggle Dashed Style", null, (s, e) =>
            {
                if (_selectedConnection != null)
                {
                    _selectedConnection.IsDashed = !_selectedConnection.IsDashed;
                    Invalidate();
                    DefinitionChanged?.Invoke(this, EventArgs.Empty);
                }
            });

            _mnuDeleteConn = new ToolStripMenuItem("🗑 Delete Connection", null, OnDeleteConnClicked);

            _connContextMenu.Items.AddRange(new ToolStripItem[] {
                _mnuEditConnLabel,
                _mnuChangeConnColor,
                mnuToggleDashed,
                new ToolStripSeparator(),
                _mnuDeleteConn
            });
        }

        #region Public Properties

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ProcessFlowDefinition Definition
        {
            get => _definition;
            set
            {
                _definition = value ?? new ProcessFlowDefinition();
                _selectedNode = null;
                _hoveredNode = null;
                Invalidate();
                DefinitionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        [Description("Enables visual drag-and-drop workflow editing and connection reconfiguration.")]
        public bool IsDesignMode
        {
            get => _isDesignMode;
            set
            {
                _isDesignMode = value;
                Cursor = Cursors.Default;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        [Description("Displays dot grid in the background.")]
        public bool ShowGrid
        {
            get => _showGrid;
            set
            {
                _showGrid = value;
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        [Description("Automatically resolves and executes actions registered in ProcessActionRegistry on node click.")]
        public bool AutoExecuteAction
        {
            get => _autoExecuteAction;
            set => _autoExecuteAction = value;
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        [Description("When true, zooming with mouse wheel requires holding Ctrl key (industry standard: Figma/Miro/VSCode), preventing accidental zoom while scrolling. When false, normal mouse wheel zooms directly.")]
        public bool WheelZoomRequiresCtrl
        {
            get => _wheelZoomRequiresCtrl;
            set => _wheelZoomRequiresCtrl = value;
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        [Description("When true, mouse wheel without Ctrl pans canvas vertically (or horizontally with Shift).")]
        public bool EnableWheelPan
        {
            get => _enableWheelPan;
            set => _enableWheelPan = value;
        }

        [Category("View")]
        [DefaultValue(1.0f)]
        [Description("Zoom factor of the diagram canvas (0.2x to 3.0x).")]
        public float ZoomFactor
        {
            get => _zoom;
            set
            {
                _zoom = Math.Max(0.2f, Math.Min(3.0f, value));
                Invalidate();
            }
        }

        [Browsable(false)]
        public PointF PanOffset
        {
            get => _panOffset;
            set
            {
                _panOffset = value;
                Invalidate();
            }
        }

        [Browsable(false)]
        public ProcessFlowNode? SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (_selectedNode != value)
                {
                    _selectedNode = value;
                    Invalidate();
                }
            }
        }

        #endregion

        #region Coordinate Transforms

        public PointF ScreenToWorld(PointF screen)
        {
            return new PointF((screen.X - _panOffset.X) / _zoom, (screen.Y - _panOffset.Y) / _zoom);
        }

        public PointF WorldToScreen(PointF world)
        {
            return new PointF(world.X * _zoom + _panOffset.X, world.Y * _zoom + _panOffset.Y);
        }

        public RectangleF WorldToScreen(RectangleF world)
        {
            var p = WorldToScreen(new PointF(world.X, world.Y));
            return new RectangleF(p.X, p.Y, world.Width * _zoom, world.Height * _zoom);
        }

        #endregion

        #region Rendering Pipeline

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;

            // 1. Background
            Color bg = ZeroTheme.Colors.Background;
            using (var b = new SolidBrush(bg))
            {
                g.FillRectangle(b, ClientRectangle);
            }

            // 2. Background Grid
            if (_showGrid)
            {
                DrawGrid(g);
            }

            // 3. Save Matrix & Apply Pan/Zoom Transform
            var state = g.Save();
            g.TranslateTransform(_panOffset.X, _panOffset.Y);
            g.ScaleTransform(_zoom, _zoom);

            // 4. Swimlanes
            foreach (var lane in _definition.Lanes)
            {
                DrawLane(g, lane);
            }

            // 5. Connections (Orthogonal Lines & Branch Labels)
            foreach (var conn in _definition.Connections)
            {
                DrawConnection(g, conn, conn == _selectedConnection, conn == _hoveredConnection);
            }

            // 6. Nodes (Task Cards, Decision Diamonds, Terminals)
            foreach (var node in _definition.Nodes)
            {
                DrawNode(g, node, node == _selectedNode, node == _hoveredNode);
            }

            // 6b. Ports & Rubberband Wire in Design Mode
            if (_isDesignMode)
            {
                if (_hoveredNode != null)
                {
                    DrawPortDots(g, _hoveredNode, _hoveredNode == _hoveredPortNode ? _hoveredPort : null);
                }
                if (_selectedNode != null && _selectedNode != _hoveredNode)
                {
                    DrawPortDots(g, _selectedNode, _selectedNode == _hoveredPortNode ? _hoveredPort : null);
                }

                if (_isConnecting && _connectSourceNode != null)
                {
                    PointF p1 = GetPortLocation(_connectSourceNode, _connectSourcePort);
                    PointF p2 = _connectCurrentMouse;
                    using var rubberPen = new Pen(Color.FromArgb(245, 158, 11), 2.5f) { DashStyle = DashStyle.Dash };
                    g.DrawLine(rubberPen, p1, p2);
                    DrawArrowhead(g, rubberPen, p1, p2);
                }
            }

            g.Restore(state);

            // 7. HUD Overlays (Title banner, mode badge, zoom info)
            DrawHudOverlay(g);
        }

        private void DrawGrid(Graphics g)
        {
            float step = 25f * _zoom;
            if (step < 8f) return;

            float startX = _panOffset.X % step;
            if (startX < 0) startX += step;
            float startY = _panOffset.Y % step;
            if (startY < 0) startY += step;

            Color dotColor = Color.FromArgb(30, ZeroTheme.Colors.TextSecondary);
            using (var brush = new SolidBrush(dotColor))
            {
                for (float x = startX; x < Width; x += step)
                {
                    for (float y = startY; y < Height; y += step)
                    {
                        g.FillRectangle(brush, x - 1, y - 1, 2, 2);
                    }
                }
            }
        }

        private void DrawLane(Graphics g, ProcessFlowLane lane)
        {
            var bounds = new RectangleF((float)lane.X, (float)lane.Y, (float)lane.Width, (float)lane.Height);
            Color bg = ParseColor(lane.BackgroundColorHex, Color.FromArgb(248, 250, 252));
            Color border = Color.FromArgb(40, ZeroTheme.Colors.Border);

            using (var path = GetRoundedRectPath(bounds, 12))
            {
                using (var fillBrush = new SolidBrush(bg))
                {
                    g.FillPath(fillBrush, path);
                }
                using (var pen = new Pen(border, 1.5f) { DashStyle = DashStyle.Dash })
                {
                    g.DrawPath(pen, path);
                }
            }

            // Lane Header Tag
            string title = "⚙ " + lane.Title.ToUpperInvariant();
            Color headerColor = ParseColor(lane.HeaderColorHex, ZeroTheme.Colors.TextSecondary);
            using (var font = new Font(Font.FontFamily, 8f, FontStyle.Bold))
            using (var brush = new SolidBrush(headerColor))
            {
                g.DrawString(title, font, brush, bounds.X + 16, bounds.Y + 12);
            }
        }

        private void DrawConnection(Graphics g, ProcessFlowConnection conn, bool isSelected = false, bool isHovered = false)
        {
            var srcNode = _definition.Nodes.FirstOrDefault(n => n.Id == conn.SourceNodeId);
            var tgtNode = _definition.Nodes.FirstOrDefault(n => n.Id == conn.TargetNodeId);
            if (srcNode == null || tgtNode == null) return;

            PointF p1 = GetPortLocation(srcNode, conn.SourcePort);
            PointF p2 = GetPortLocation(tgtNode, conn.TargetPort);

            Color stroke = isSelected
                ? Color.FromArgb(245, 158, 11)
                : (isHovered ? Color.FromArgb(59, 130, 246) : ParseColor(conn.StrokeColorHex, Color.FromArgb(14, 165, 233)));
            float thick = (float)conn.StrokeThickness + (isSelected || isHovered ? 1.5f : 0f);

            using (var pen = new Pen(stroke, thick))
            {
                if (conn.IsDashed) pen.DashStyle = DashStyle.Dash;

                // Orthogonal routing (Waypoints)
                var points = CalculateOrthogonalRoute(p1, p2, conn.SourcePort, conn.TargetPort);
                if (points.Length >= 2)
                {
                    g.DrawLines(pen, points);

                    // Draw arrowhead at end point
                    DrawArrowhead(g, pen, points[points.Length - 2], points[points.Length - 1]);

                    // Draw transition label capsule if present
                    if (!string.IsNullOrWhiteSpace(conn.Label))
                    {
                        DrawConnectionLabel(g, conn.Label, points, stroke);
                    }
                }
            }
        }

        private PointF[] CalculateOrthogonalRoute(PointF p1, PointF p2, ProcessPortPosition sp, ProcessPortPosition tp)
        {
            var list = new List<PointF>();
            list.Add(p1);

            // Same vertical line
            if (Math.Abs(p1.X - p2.X) < 4 && p2.Y > p1.Y)
            {
                list.Add(p2);
                return list.ToArray();
            }

            if (sp == ProcessPortPosition.Bottom && tp == ProcessPortPosition.Top)
            {
                float midY = (p1.Y + p2.Y) / 2;
                list.Add(new PointF(p1.X, midY));
                list.Add(new PointF(p2.X, midY));
            }
            else if (sp == ProcessPortPosition.Right && tp == ProcessPortPosition.Top)
            {
                list.Add(new PointF(p2.X, p1.Y));
            }
            else if (sp == ProcessPortPosition.Left && tp == ProcessPortPosition.Top)
            {
                list.Add(new PointF(p2.X, p1.Y));
            }
            else if (sp == ProcessPortPosition.Left && tp == ProcessPortPosition.Right)
            {
                float midX = (p1.X + p2.X) / 2;
                list.Add(new PointF(midX, p1.Y));
                list.Add(new PointF(midX, p2.Y));
            }
            else
            {
                float midX = (p1.X + p2.X) / 2;
                list.Add(new PointF(midX, p1.Y));
                list.Add(new PointF(midX, p2.Y));
            }

            list.Add(p2);
            return list.ToArray();
        }

        private void DrawArrowhead(Graphics g, Pen pen, PointF from, PointF to)
        {
            float arrowSize = 6.0f + pen.Width;
            float dx = to.X - from.X;
            float dy = to.Y - from.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.001f) return;

            float uX = dx / len;
            float uY = dy / len;
            float vX = -uY;
            float vY = uX;

            PointF pLeft = new PointF(to.X - uX * arrowSize + vX * (arrowSize * 0.5f), to.Y - uY * arrowSize + vY * (arrowSize * 0.5f));
            PointF pRight = new PointF(to.X - uX * arrowSize - vX * (arrowSize * 0.5f), to.Y - uY * arrowSize - vY * (arrowSize * 0.5f));

            using (var brush = new SolidBrush(pen.Color))
            {
                g.FillPolygon(brush, new PointF[] { to, pLeft, pRight });
            }
        }

        private void DrawConnectionLabel(Graphics g, string label, PointF[] points, Color color)
        {
            // Pick midpoint segment
            int midIdx = points.Length / 2;
            PointF a = points[midIdx - 1];
            PointF b = points[midIdx];
            PointF center = new PointF((a.X + b.X) / 2, (a.Y + b.Y) / 2);

            using (var font = new Font(Font.FontFamily, 7.5f, FontStyle.Bold))
            {
                var sz = g.MeasureString(label, font);
                var rect = new RectangleF(center.X - sz.Width / 2 - 6, center.Y - sz.Height / 2 - 3, sz.Width + 12, sz.Height + 6);

                using (var path = GetRoundedRectPath(rect, 4))
                {
                    using (var fill = new SolidBrush(Color.FromArgb(240, 255, 255, 255)))
                    {
                        g.FillPath(fill, path);
                    }
                    using (var pen = new Pen(color, 1.2f))
                    {
                        g.DrawPath(pen, path);
                    }
                }

                using (var brush = new SolidBrush(color))
                {
                    g.DrawString(label, font, brush, rect.X + 6, rect.Y + 3);
                }
            }
        }

        private void DrawNode(Graphics g, ProcessFlowNode node, bool isSelected, bool isHovered)
        {
            var bounds = new RectangleF((float)node.X, (float)node.Y, (float)node.Width, (float)node.Height);

            switch (node.Shape)
            {
                case ProcessNodeShape.DecisionDiamond:
                    DrawDecisionDiamond(g, node, bounds, isSelected, isHovered);
                    break;
                case ProcessNodeShape.StartTerminal:
                case ProcessNodeShape.EndTerminal:
                    DrawTerminalNode(g, node, bounds, isSelected, isHovered);
                    break;
                case ProcessNodeShape.TaskCard:
                default:
                    DrawTaskCard(g, node, bounds, isSelected, isHovered);
                    break;
            }

            // Design mode connection port dots
            if (_isDesignMode && isSelected)
            {
                DrawPortDots(g, node);
            }
        }

        private void DrawTaskCard(Graphics g, ProcessFlowNode node, RectangleF bounds, bool isSelected, bool isHovered)
        {
            float cornerRadius = 10f;
            Color headerColor = ParseColor(node.HeaderColorHex, ZeroTheme.Colors.Primary);
            Color borderColor = isSelected ? ZeroTheme.Colors.Primary : (isHovered ? headerColor : ParseColor(node.BorderColorHex, ZeroTheme.Colors.Border));
            float borderWidth = isSelected ? 2.2f : (isHovered ? 1.8f : 1.0f);

            // Card Shadow if hovered
            if (isHovered)
            {
                var shadowRect = bounds;
                shadowRect.Offset(0, 3);
                using (var shadowPath = GetRoundedRectPath(shadowRect, cornerRadius))
                using (var shadowBrush = new SolidBrush(Color.FromArgb(20, 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }
            }

            using (var path = GetRoundedRectPath(bounds, cornerRadius))
            {
                // Background fill
                using (var fill = new SolidBrush(Color.White))
                {
                    g.FillPath(fill, path);
                }

                // Header stripe (top ~30px)
                float headerHeight = 28f;
                var headerRect = new RectangleF(bounds.X, bounds.Y, bounds.Width, headerHeight);
                using (var headerPath = GetTopRoundedRectPath(headerRect, cornerRadius))
                using (var headerFill = new SolidBrush(Color.FromArgb(16, headerColor)))
                {
                    g.FillPath(headerFill, headerPath);
                }

                // Border
                using (var pen = new Pen(borderColor, borderWidth))
                {
                    if (isSelected && _isDesignMode) pen.DashStyle = DashStyle.Dash;
                    g.DrawPath(pen, path);
                }
            }

            // Icon + Title in Header
            using (var titleFont = new Font(Font.FontFamily, 8.8f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(headerColor))
            {
                string headerText = $"{node.IconGlyph} {node.Title}";
                g.DrawString(headerText, titleFont, titleBrush, bounds.X + 10, bounds.Y + 6);
            }

            // Subtitle / Description Body
            if (!string.IsNullOrWhiteSpace(node.Subtitle))
            {
                var bodyRect = new RectangleF(bounds.X + 10, bounds.Y + 32, bounds.Width - 20, bounds.Height - 36);
                using (var bodyFont = new Font(Font.FontFamily, 8.0f, FontStyle.Regular))
                using (var bodyBrush = new SolidBrush(Color.FromArgb(71, 85, 105)))
                using (var sf = new StringFormat { Trimming = StringTrimming.EllipsisWord })
                {
                    g.DrawString(node.Subtitle, bodyFont, bodyBrush, bodyRect, sf);
                }
            }

            // Action Indicator Badge (small link icon on bottom-right if bound)
            if (!string.IsNullOrWhiteSpace(node.ActionKey))
            {
                using (var badgeFont = new Font(Font.FontFamily, 7.0f, FontStyle.Regular))
                using (var badgeBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    g.DrawString("⚡ Action", badgeFont, badgeBrush, bounds.Right - 48, bounds.Bottom - 16);
                }
            }
        }

        private void DrawDecisionDiamond(Graphics g, ProcessFlowNode node, RectangleF bounds, bool isSelected, bool isHovered)
        {
            PointF top = new PointF(bounds.X + bounds.Width / 2, bounds.Y);
            PointF right = new PointF(bounds.Right, bounds.Y + bounds.Height / 2);
            PointF bottom = new PointF(bounds.X + bounds.Width / 2, bounds.Bottom);
            PointF left = new PointF(bounds.X, bounds.Y + bounds.Height / 2);
            PointF[] diamond = new PointF[] { top, right, bottom, left };

            Color accent = ParseColor(node.HeaderColorHex, Color.FromArgb(2, 132, 199));
            Color border = isSelected ? ZeroTheme.Colors.Primary : (isHovered ? accent : ParseColor(node.BorderColorHex, Color.FromArgb(56, 189, 248)));
            float thick = isSelected ? 2.5f : (isHovered ? 2.0f : 1.4f);

            // Fill
            using (var fill = new SolidBrush(Color.FromArgb(245, 250, 255)))
            {
                g.FillPolygon(fill, diamond);
            }

            // Border
            using (var pen = new Pen(border, thick))
            {
                if (isSelected && _isDesignMode) pen.DashStyle = DashStyle.Dash;
                g.DrawPolygon(pen, diamond);
            }

            // Text Center
            using (var titleFont = new Font(Font.FontFamily, 8.5f, FontStyle.Bold))
            using (var subFont = new Font(Font.FontFamily, 7.5f, FontStyle.Regular))
            using (var titleBrush = new SolidBrush(accent))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                string text = node.Title;
                if (!string.IsNullOrWhiteSpace(node.Subtitle))
                {
                    text += "\n" + node.Subtitle;
                }
                g.DrawString(text, string.IsNullOrWhiteSpace(node.Subtitle) ? titleFont : subFont, titleBrush, bounds, sf);
            }
        }

        private void DrawTerminalNode(Graphics g, ProcessFlowNode node, RectangleF bounds, bool isSelected, bool isHovered)
        {
            float corner = bounds.Height / 2;
            Color bg = ParseColor(node.HeaderColorHex, ZeroTheme.Colors.Success);

            using (var path = GetRoundedRectPath(bounds, corner))
            {
                using (var fill = new SolidBrush(bg))
                {
                    g.FillPath(fill, path);
                }
                using (var pen = new Pen(isSelected ? Color.White : Color.FromArgb(40, Color.Black), 2f))
                {
                    g.DrawPath(pen, path);
                }
            }

            using (var font = new Font(Font.FontFamily, 8.5f, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.White))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(node.Title, font, brush, bounds, sf);
            }
        }

        private void DrawPortDots(Graphics g, ProcessFlowNode node, ProcessPortPosition? activePort = null)
        {
            var ports = new ProcessPortPosition[] { ProcessPortPosition.Top, ProcessPortPosition.Bottom, ProcessPortPosition.Left, ProcessPortPosition.Right };
            using (var fillDefault = new SolidBrush(ZeroTheme.Colors.Primary))
            using (var fillActive = new SolidBrush(Color.FromArgb(245, 158, 11)))
            using (var borderPen = new Pen(Color.White, 1.5f))
            {
                foreach (var p in ports)
                {
                    var pt = GetPortLocation(node, p);
                    bool isActive = (p == activePort);
                    float r = isActive ? 6f : 4f;
                    g.FillEllipse(isActive ? fillActive : fillDefault, pt.X - r, pt.Y - r, r * 2, r * 2);
                    g.DrawEllipse(borderPen, pt.X - r, pt.Y - r, r * 2, r * 2);
                }
            }
        }

        private void DrawHudOverlay(Graphics g)
        {
            // 1. Top Title Bar
            string title = _definition.Title;
            string desc = _definition.Description;
            using (var titleFont = new Font(Font.FontFamily, 10.5f, FontStyle.Bold))
            using (var descFont = new Font(Font.FontFamily, 8.0f, FontStyle.Regular))
            using (var titleBrush = new SolidBrush(ZeroTheme.Colors.TextPrimary))
            using (var descBrush = new SolidBrush(ZeroTheme.Colors.TextSecondary))
            {
                g.DrawString(title, titleFont, titleBrush, 24, 16);
                if (!string.IsNullOrWhiteSpace(desc))
                {
                    g.DrawString("ℹ " + desc, descFont, descBrush, Width - g.MeasureString("ℹ " + desc, descFont).Width - 24, 18);
                }
            }

            // 2. Mode Badge (Bottom Left)
            string modeText = _isDesignMode 
                ? "✏ DESIGN MODE  •  Kéo cổng nối  •  Phải chuột: Cấu hình/Xóa  •  Delete: Xóa" 
                : "▶ RUN MODE  •  Ctrl + Cuộn: Zoom  •  Cuộn: Di chuyển  •  Double-click: Vừa khung";
            Color badgeBg = _isDesignMode ? Color.FromArgb(245, 158, 11) : Color.FromArgb(16, 185, 129);

            using (var font = new Font(Font.FontFamily, 8.0f, FontStyle.Bold))
            {
                var sz = g.MeasureString(modeText, font);
                var rect = new RectangleF(16, Height - 36, sz.Width + 20, 24);

                using (var path = GetRoundedRectPath(rect, 12))
                using (var fill = new SolidBrush(badgeBg))
                using (var textBrush = new SolidBrush(Color.White))
                {
                    g.FillPath(fill, path);
                    g.DrawString(modeText, font, textBrush, rect.X + 10, rect.Y + 4);
                }
            }

            // 3. Floating Interactive Zoom HUD (Bottom Right)
            var hudRect = GetZoomHudRect();
            using (var hudPath = GetRoundedRectPath(hudRect, 14))
            using (var hudBg = new SolidBrush(Color.FromArgb(245, 255, 255, 255)))
            using (var hudBorder = new Pen(Color.FromArgb(210, 220, 230), 1.0f))
            using (var font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular))
            using (var textBrush = new SolidBrush(ZeroTheme.Colors.TextPrimary))
            using (var highlightBrush = new SolidBrush(Color.FromArgb(220, 235, 252)))
            {
                g.FillPath(hudBg, hudPath);
                g.DrawPath(hudBorder, hudPath);

                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                // [ - ] ZoomOut
                var rZoomOut = new RectangleF(hudRect.X, hudRect.Y, 36, hudRect.Height);
                if (_hoveredHudButton == ZoomHudButton.ZoomOut)
                {
                    using (var hPath = GetRoundedRectPath(new RectangleF(rZoomOut.X + 2, rZoomOut.Y + 2, rZoomOut.Width - 2, rZoomOut.Height - 4), 12))
                        g.FillPath(highlightBrush, hPath);
                }
                g.DrawString("➖", font, textBrush, rZoomOut, sf);

                // [ 100% ] Reset
                var rReset = new RectangleF(hudRect.X + 36, hudRect.Y, 60, hudRect.Height);
                if (_hoveredHudButton == ZoomHudButton.ResetZoom)
                {
                    using (var hPath = GetRoundedRectPath(new RectangleF(rReset.X + 1, rReset.Y + 2, rReset.Width - 2, rReset.Height - 4), 4))
                        g.FillPath(highlightBrush, hPath);
                }
                string zoomText = $"{(_zoom * 100):0}%";
                g.DrawString(zoomText, font, textBrush, rReset, sf);

                // [ + ] ZoomIn
                var rZoomIn = new RectangleF(hudRect.X + 96, hudRect.Y, 36, hudRect.Height);
                if (_hoveredHudButton == ZoomHudButton.ZoomIn)
                {
                    using (var hPath = GetRoundedRectPath(new RectangleF(rZoomIn.X + 1, rZoomIn.Y + 2, rZoomIn.Width - 2, rZoomIn.Height - 4), 4))
                        g.FillPath(highlightBrush, hPath);
                }
                g.DrawString("➕", font, textBrush, rZoomIn, sf);

                // [ ⛶ ] Fit
                var rFit = new RectangleF(hudRect.X + 132, hudRect.Y, 38, hudRect.Height);
                if (_hoveredHudButton == ZoomHudButton.Fit)
                {
                    using (var hPath = GetRoundedRectPath(new RectangleF(rFit.X, rFit.Y + 2, rFit.Width - 2, rFit.Height - 4), 12))
                        g.FillPath(highlightBrush, hPath);
                }
                g.DrawString("⛶", font, textBrush, rFit, sf);
            }
        }

        private RectangleF GetZoomHudRect()
        {
            return new RectangleF(Width - 186, Height - 38, 170, 30);
        }

        private ZoomHudButton HitTestZoomHud(PointF screenPt)
        {
            var hud = GetZoomHudRect();
            if (!hud.Contains(screenPt)) return ZoomHudButton.None;

            float relX = screenPt.X - hud.X;
            if (relX < 36) return ZoomHudButton.ZoomOut;
            if (relX < 96) return ZoomHudButton.ResetZoom;
            if (relX < 132) return ZoomHudButton.ZoomIn;
            return ZoomHudButton.Fit;
        }

        #endregion

        #region Geometry & Port Helpers

        public PointF GetPortLocation(ProcessFlowNode node, ProcessPortPosition port)
        {
            float x = (float)node.X;
            float y = (float)node.Y;
            float w = (float)node.Width;
            float h = (float)node.Height;

            switch (port)
            {
                case ProcessPortPosition.Top:
                    return new PointF(x + w / 2, y);
                case ProcessPortPosition.Bottom:
                    return new PointF(x + w / 2, y + h);
                case ProcessPortPosition.Left:
                    return new PointF(x, y + h / 2);
                case ProcessPortPosition.Right:
                    return new PointF(x + w, y + h / 2);
                case ProcessPortPosition.Center:
                default:
                    return new PointF(x + w / 2, y + h / 2);
            }
        }

        private static GraphicsPath GetRoundedRectPath(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static GraphicsPath GetTopRoundedRectPath(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddLine(rect.Right, rect.Bottom, rect.X, rect.Bottom);
            path.CloseFigure();
            return path;
        }

        private static Color ParseColor(string hex, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(hex)) return fallback;
            try
            {
                return ColorTranslator.FromHtml(hex);
            }
            catch
            {
                return fallback;
            }
        }

        #endregion

        #region Mouse Interaction & Events

        private ProcessFlowNode? HitTestNode(PointF worldPt)
        {
            for (int i = _definition.Nodes.Count - 1; i >= 0; i--)
            {
                var n = _definition.Nodes[i];
                var rect = new RectangleF((float)n.X, (float)n.Y, (float)n.Width, (float)n.Height);
                if (rect.Contains(worldPt))
                {
                    return n;
                }
            }
            return null;
        }

        public bool HitTestPort(PointF worldPt, out ProcessFlowNode? hitNode, out ProcessPortPosition hitPort)
        {
            hitNode = null;
            hitPort = ProcessPortPosition.Center;
            if (!_isDesignMode) return false;

            float threshold = 12f / _zoom;
            var ports = new[] { ProcessPortPosition.Top, ProcessPortPosition.Bottom, ProcessPortPosition.Left, ProcessPortPosition.Right };

            var candidates = new List<ProcessFlowNode>();
            if (_hoveredNode != null) candidates.Add(_hoveredNode);
            if (_selectedNode != null && _selectedNode != _hoveredNode) candidates.Add(_selectedNode);
            foreach (var n in _definition.Nodes)
            {
                if (!candidates.Contains(n)) candidates.Add(n);
            }

            foreach (var node in candidates)
            {
                foreach (var p in ports)
                {
                    var pt = GetPortLocation(node, p);
                    float dx = worldPt.X - pt.X;
                    float dy = worldPt.Y - pt.Y;
                    if ((dx * dx + dy * dy) <= threshold * threshold)
                    {
                        hitNode = node;
                        hitPort = p;
                        return true;
                    }
                }
            }
            return false;
        }

        public ProcessFlowConnection? HitTestConnection(PointF worldPt)
        {
            float threshold = 8f / _zoom;
            foreach (var conn in _definition.Connections)
            {
                var srcNode = _definition.Nodes.FirstOrDefault(n => n.Id == conn.SourceNodeId);
                var tgtNode = _definition.Nodes.FirstOrDefault(n => n.Id == conn.TargetNodeId);
                if (srcNode == null || tgtNode == null) continue;

                PointF p1 = GetPortLocation(srcNode, conn.SourcePort);
                PointF p2 = GetPortLocation(tgtNode, conn.TargetPort);
                var points = CalculateOrthogonalRoute(p1, p2, conn.SourcePort, conn.TargetPort);
                for (int i = 0; i < points.Length - 1; i++)
                {
                    if (DistanceToLineSegment(worldPt, points[i], points[i + 1]) <= threshold)
                    {
                        return conn;
                    }
                }
            }
            return null;
        }

        private static float DistanceToLineSegment(PointF pt, PointF a, PointF b)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            float lenSq = dx * dx + dy * dy;
            if (lenSq < 0.0001f)
            {
                float ddx = pt.X - a.X;
                float ddy = pt.Y - a.Y;
                return (float)Math.Sqrt(ddx * ddx + ddy * ddy);
            }
            float t = Math.Max(0, Math.Min(1, ((pt.X - a.X) * dx + (pt.Y - a.Y) * dy) / lenSq));
            PointF proj = new PointF(a.X + t * dx, a.Y + t * dy);
            float px = pt.X - proj.X;
            float py = pt.Y - proj.Y;
            return (float)Math.Sqrt(px * px + py * py);
        }

        private static ProcessPortPosition GetBestSourcePort(ProcessFlowNode from, ProcessFlowNode to)
        {
            double dx = to.X - from.X;
            double dy = to.Y - from.Y;
            if (Math.Abs(dx) > Math.Abs(dy))
            {
                return dx > 0 ? ProcessPortPosition.Right : ProcessPortPosition.Left;
            }
            else
            {
                return dy > 0 ? ProcessPortPosition.Bottom : ProcessPortPosition.Top;
            }
        }

        private static ProcessPortPosition GetBestTargetPort(ProcessFlowNode from, ProcessFlowNode to, ProcessPortPosition sp)
        {
            return sp switch
            {
                ProcessPortPosition.Right => ProcessPortPosition.Left,
                ProcessPortPosition.Left => ProcessPortPosition.Right,
                ProcessPortPosition.Bottom => ProcessPortPosition.Top,
                ProcessPortPosition.Top => ProcessPortPosition.Bottom,
                _ => ProcessPortPosition.Top
            };
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            // 0. Check Zoom HUD interaction
            var hudBtn = HitTestZoomHud(e.Location);
            if (hudBtn != ZoomHudButton.None)
            {
                switch (hudBtn)
                {
                    case ZoomHudButton.ZoomOut:
                        ZoomOut();
                        break;
                    case ZoomHudButton.ResetZoom:
                        ResetZoom();
                        break;
                    case ZoomHudButton.ZoomIn:
                        ZoomIn();
                        break;
                    case ZoomHudButton.Fit:
                        ZoomToFit();
                        break;
                }
                return;
            }

            PointF worldPt = ScreenToWorld(e.Location);

            if (e.Button == MouseButtons.Middle || (e.Button == MouseButtons.Right && _selectedNode == null && _selectedConnection == null))
            {
                _isPanning = true;
                _panStartMouse = e.Location;
                _panStartOffset = _panOffset;
                Capture = true;
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                // 1. Check if starting connection drag from a port in design mode
                if (_isDesignMode && HitTestPort(worldPt, out var pNode, out var pPort) && pNode != null)
                {
                    _isConnecting = true;
                    _connectSourceNode = pNode;
                    _connectSourcePort = pPort;
                    _connectCurrentMouse = worldPt;
                    Capture = true;
                    Invalidate();
                    return;
                }

                // 2. Check if clicking on node
                var hit = HitTestNode(worldPt);
                if (hit != null)
                {
                    SelectedNode = hit;
                    _selectedConnection = null;
                    if (_isDesignMode)
                    {
                        _isDraggingNode = true;
                        _nodeDragStartMouse = e.Location;
                        _nodeDragStartPos = new PointF((float)hit.X, (float)hit.Y);
                        Capture = true;
                    }
                    Invalidate();
                    return;
                }

                // 3. Check if clicking on connection line
                var hitConn = HitTestConnection(worldPt);
                if (hitConn != null)
                {
                    _selectedConnection = hitConn;
                    SelectedNode = null;
                    Invalidate();
                    return;
                }

                // 4. Clicking on blank canvas
                SelectedNode = null;
                _selectedConnection = null;
                _isPanning = true;
                _panStartMouse = e.Location;
                _panStartOffset = _panOffset;
                Capture = true;
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            // Check Zoom HUD hover
            var hudBtn = HitTestZoomHud(e.Location);
            if (hudBtn != _hoveredHudButton)
            {
                _hoveredHudButton = hudBtn;
                Invalidate();
            }
            if (hudBtn != ZoomHudButton.None)
            {
                Cursor = Cursors.Hand;
                return;
            }

            PointF worldPt = ScreenToWorld(e.Location);

            if (_isConnecting)
            {
                _connectCurrentMouse = worldPt;
                HitTestPort(worldPt, out _hoveredPortNode, out var hp);
                _hoveredPort = hp;
                Cursor = Cursors.Cross;
                Invalidate();
                return;
            }

            if (_isPanning)
            {
                _panOffset = new PointF(
                    _panStartOffset.X + (e.X - _panStartMouse.X),
                    _panStartOffset.Y + (e.Y - _panStartMouse.Y)
                );
                Invalidate();
                return;
            }

            if (_isDraggingNode && _selectedNode != null && _isDesignMode)
            {
                float dx = (e.X - _nodeDragStartMouse.X) / _zoom;
                float dy = (e.Y - _nodeDragStartMouse.Y) / _zoom;
                _selectedNode.X = Math.Max(0, _nodeDragStartPos.X + dx);
                _selectedNode.Y = Math.Max(0, _nodeDragStartPos.Y + dy);
                Invalidate();
                return;
            }

            // Design mode port hovering
            if (_isDesignMode)
            {
                if (HitTestPort(worldPt, out var pNode, out var pPort))
                {
                    Cursor = Cursors.Cross;
                    if (_hoveredPortNode != pNode || _hoveredPort != pPort)
                    {
                        _hoveredPortNode = pNode;
                        _hoveredPort = pPort;
                        Invalidate();
                    }
                    return;
                }
                else if (_hoveredPortNode != null || _hoveredPort != null)
                {
                    _hoveredPortNode = null;
                    _hoveredPort = null;
                    Invalidate();
                }
            }

            // Connection line hovering
            var hoveredConn = HitTestConnection(worldPt);
            if (hoveredConn != _hoveredConnection)
            {
                _hoveredConnection = hoveredConn;
                Invalidate();
            }

            // Node hovering
            var hovered = HitTestNode(worldPt);
            if (hovered != _hoveredNode)
            {
                _hoveredNode = hovered;
                if (_hoveredNode != null)
                {
                    Cursor = _isDesignMode ? Cursors.SizeAll : Cursors.Hand;
                }
                else if (_hoveredConnection != null)
                {
                    Cursor = Cursors.Hand;
                }
                else
                {
                    Cursor = Cursors.Default;
                }
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            Capture = false;

            if (_isConnecting)
            {
                _isConnecting = false;
                PointF worldPt = ScreenToWorld(e.Location);
                var targetNode = HitTestNode(worldPt);
                if (targetNode != null && _connectSourceNode != null && targetNode.Id != _connectSourceNode.Id)
                {
                    ProcessPortPosition targetPort;
                    if (HitTestPort(worldPt, out var tNode, out var tPort) && tNode?.Id == targetNode.Id)
                    {
                        targetPort = tPort;
                    }
                    else
                    {
                        targetPort = GetBestTargetPort(_connectSourceNode, targetNode, _connectSourcePort);
                    }

                    AddConnection(_connectSourceNode.Id, targetNode.Id, "", "#0EA5E9", _connectSourcePort, targetPort);
                }

                _connectSourceNode = null;
                _hoveredPort = null;
                _hoveredPortNode = null;
                Invalidate();
                return;
            }

            bool wasDragging = _isDraggingNode;
            _isPanning = false;
            _isDraggingNode = false;

            PointF worldPtAfter = ScreenToWorld(e.Location);
            var hit = HitTestNode(worldPtAfter);

            if (e.Button == MouseButtons.Left && !wasDragging && hit != null)
            {
                NodeClicked?.Invoke(this, hit);

                if (_autoExecuteAction && !string.IsNullOrWhiteSpace(hit.ActionKey) && !_isDesignMode)
                {
                    var ctx = new ProcessActionContext(hit, this);
                    ActionTriggered?.Invoke(this, ctx);
                    if (!ctx.Handled)
                    {
                        ProcessActionRegistry.Execute(hit.ActionKey, ctx);
                    }
                }
            }
            else if (e.Button == MouseButtons.Right && _isDesignMode)
            {
                _rightClickLocation = worldPtAfter;

                // Check connection right-click
                var hitConn = HitTestConnection(worldPtAfter);
                if (hitConn != null)
                {
                    _selectedConnection = hitConn;
                    SelectedNode = null;
                    Invalidate();
                    _connContextMenu.Show(this, e.Location);
                    return;
                }

                // Check node right-click or blank canvas
                SelectedNode = hit;
                _selectedConnection = null;
                Invalidate();
                _contextMenu.Show(this, e.Location);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_isDesignMode && e.KeyCode == Keys.Delete)
            {
                if (_selectedConnection != null)
                {
                    _definition.Connections.Remove(_selectedConnection);
                    _selectedConnection = null;
                    Invalidate();
                    DefinitionChanged?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                }
                else if (_selectedNode != null)
                {
                    OnDeleteNodeClicked(this, EventArgs.Empty);
                    e.Handled = true;
                }
            }
        }

        private PointF _rightClickLocation;

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoveredHudButton != ZoomHudButton.None)
            {
                _hoveredHudButton = ZoomHudButton.None;
                Invalidate();
            }
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);
            if (_selectedNode == null && _selectedConnection == null)
            {
                ZoomToFit();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            bool isCtrl = (ModifierKeys & Keys.Control) == Keys.Control;
            bool isShift = (ModifierKeys & Keys.Shift) == Keys.Shift;

            // When WheelZoomRequiresCtrl is true and Ctrl is not pressed, perform smooth panning
            if (_wheelZoomRequiresCtrl && !isCtrl)
            {
                if (_enableWheelPan)
                {
                    float delta = e.Delta;
                    if (isShift)
                    {
                        _panOffset = new PointF(_panOffset.X + delta, _panOffset.Y);
                    }
                    else
                    {
                        _panOffset = new PointF(_panOffset.X, _panOffset.Y + delta);
                    }
                    Invalidate();
                }
                return;
            }

            // Zoom centering around mouse point
            float oldZoom = _zoom;
            float zoomDelta = e.Delta > 0 ? 1.15f : 0.87f;
            ZoomFactor = _zoom * zoomDelta;

            PointF mouse = e.Location;
            _panOffset = new PointF(
                mouse.X - (mouse.X - _panOffset.X) * (_zoom / oldZoom),
                mouse.Y - (mouse.Y - _panOffset.Y) * (_zoom / oldZoom)
            );
            Invalidate();
        }

        #endregion

        #region Context Menu & Customization Handlers

        private void OnContextMenuOpening(object? sender, CancelEventArgs e)
        {
            bool hasNode = _selectedNode != null;
            _mnuAddStep.Visible = !hasNode;
            _mnuConnectTo.Visible = hasNode;
            _mnuEditTitle.Visible = hasNode;
            _mnuAssignAction.Visible = hasNode;
            _mnuChangeShape.Visible = hasNode;
            _mnuDeleteNode.Visible = hasNode;

            if (!hasNode || _selectedNode == null)
            {
                return;
            }

            // Populate "Connect to Step..." submenu
            _mnuConnectTo.DropDownItems.Clear();
            var otherNodes = _definition.Nodes.Where(n => n.Id != _selectedNode.Id).ToList();
            if (otherNodes.Count == 0)
            {
                _mnuConnectTo.DropDownItems.Add(new ToolStripMenuItem("(No other steps)") { Enabled = false });
            }
            else
            {
                foreach (var target in otherNodes)
                {
                    var tgt = target;
                    var item = new ToolStripMenuItem($"➜ {tgt.Title}", null, (s, ev) =>
                    {
                        if (_selectedNode != null)
                        {
                            var sp = GetBestSourcePort(_selectedNode, tgt);
                            var tp = GetBestTargetPort(_selectedNode, tgt, sp);
                            AddConnection(_selectedNode.Id, tgt.Id, "", "#0EA5E9", sp, tp);
                        }
                    });
                    _mnuConnectTo.DropDownItems.Add(item);
                }
            }

            // Populate actions submenu from ProcessActionRegistry
            _mnuAssignAction.DropDownItems.Clear();
            var actions = ProcessActionRegistry.GetAllActions();

            var mnuNone = new ToolStripMenuItem("(None / Clear Action)", null, (s, ev) =>
            {
                if (_selectedNode != null)
                {
                    _selectedNode.ActionKey = string.Empty;
                    Invalidate();
                }
            });
            _mnuAssignAction.DropDownItems.Add(mnuNone);
            _mnuAssignAction.DropDownItems.Add(new ToolStripSeparator());

            if (actions.Count == 0)
            {
                var emptyItem = new ToolStripMenuItem("(No registered actions in catalog)") { Enabled = false };
                _mnuAssignAction.DropDownItems.Add(emptyItem);
            }
            else
            {
                // Group by category
                var groups = actions.GroupBy(a => a.Category);
                foreach (var grp in groups)
                {
                    var catMenu = new ToolStripMenuItem(grp.Key);
                    foreach (var act in grp)
                    {
                        string actionKey = act.Key;
                        var actItem = new ToolStripMenuItem(act.Title, null, (s, ev) =>
                        {
                            if (_selectedNode != null)
                            {
                                _selectedNode.ActionKey = actionKey;
                                Invalidate();
                            }
                        })
                        {
                            Checked = (_selectedNode?.ActionKey == actionKey)
                        };
                        catMenu.DropDownItems.Add(actItem);
                    }
                    _mnuAssignAction.DropDownItems.Add(catMenu);
                }
            }
        }

        private void OnEditTitleClicked(object? sender, EventArgs e)
        {
            if (_selectedNode == null) return;

            using (var dlg = new Form())
            {
                dlg.Text = "Configure Process Node";
                dlg.Size = new Size(400, 260);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;

                var lbl1 = new Label { Text = "Title:", Top = 16, Left = 16, Width = 80 };
                var txtTitle = new TextBox { Text = _selectedNode.Title, Top = 14, Left = 100, Width = 260 };

                var lbl2 = new Label { Text = "Subtitle:", Top = 50, Left = 16, Width = 80 };
                var txtSub = new TextBox { Text = _selectedNode.Subtitle, Top = 48, Left = 100, Width = 260, Multiline = true, Height = 60 };

                var lbl3 = new Label { Text = "Icon Glyph:", Top = 120, Left = 16, Width = 80 };
                var txtIcon = new TextBox { Text = _selectedNode.IconGlyph, Top = 118, Left = 100, Width = 80 };

                var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Top = 170, Left = 190, Width = 80 };
                var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Top = 170, Left = 280, Width = 80 };

                dlg.Controls.AddRange(new Control[] { lbl1, txtTitle, lbl2, txtSub, lbl3, txtIcon, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _selectedNode.Title = txtTitle.Text;
                    _selectedNode.Subtitle = txtSub.Text;
                    _selectedNode.IconGlyph = txtIcon.Text;
                    Invalidate();
                    DefinitionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void SetSelectedNodeShape(ProcessNodeShape shape)
        {
            if (_selectedNode == null) return;
            _selectedNode.Shape = shape;
            Invalidate();
            DefinitionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnDeleteNodeClicked(object? sender, EventArgs e)
        {
            if (_selectedNode == null) return;

            string id = _selectedNode.Id;
            _definition.Nodes.Remove(_selectedNode);
            _definition.Connections.RemoveAll(c => c.SourceNodeId == id || c.TargetNodeId == id);
            _selectedNode = null;
            Invalidate();
            DefinitionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnEditConnLabelClicked(object? sender, EventArgs e)
        {
            if (_selectedConnection == null) return;
            using (var dlg = new Form())
            {
                dlg.Text = "Configure Connection Label";
                dlg.Size = new Size(380, 160);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;

                var lbl = new Label { Text = "Branch Label (e.g. Approved, Rejected, In Stock):", Top = 16, Left = 16, Width = 340 };
                var txtLabel = new TextBox { Text = _selectedConnection.Label, Top = 42, Left = 16, Width = 330 };
                var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Top = 80, Left = 170, Width = 80 };
                var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Top = 80, Left = 266, Width = 80 };

                dlg.Controls.AddRange(new Control[] { lbl, txtLabel, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _selectedConnection.Label = txtLabel.Text.Trim();
                    Invalidate();
                    DefinitionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void OnDeleteConnClicked(object? sender, EventArgs e)
        {
            if (_selectedConnection == null) return;
            _definition.Connections.Remove(_selectedConnection);
            _selectedConnection = null;
            Invalidate();
            DefinitionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnAddStepClicked(object? sender, EventArgs e)
        {
            int nextIndex = _definition.Nodes.Count + 1;
            var newNode = new ProcessFlowNode(
                Guid.NewGuid().ToString("N"),
                $"Step {nextIndex}",
                "Process description",
                _rightClickLocation.X,
                _rightClickLocation.Y,
                220,
                75
            )
            {
                Shape = ProcessNodeShape.TaskCard,
                HeaderColorHex = "#3B82F6",
                IconGlyph = "📄"
            };

            _definition.Nodes.Add(newNode);
            SelectedNode = newNode;
            Invalidate();
            DefinitionChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Fluent Dynamic Builder API

        /// <summary>
        /// Clears all nodes, lanes, and connections from the process map.
        /// </summary>
        public void Clear()
        {
            _definition.Lanes.Clear();
            _definition.Nodes.Clear();
            _definition.Connections.Clear();
            _selectedNode = null;
            _hoveredNode = null;
            Invalidate();
            DefinitionChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Programmatically adds a process task card node.
        /// </summary>
        public ProcessFlowNode AddNode(string id, string title, string subtitle, double x, double y, double width = 220, double height = 75, string actionKey = "", string icon = "📄", string headerColorHex = "#3B82F6", string? laneId = null)
        {
            var node = new ProcessFlowNode(id, title, subtitle, x, y, width, height)
            {
                Shape = ProcessNodeShape.TaskCard,
                ActionKey = actionKey,
                IconGlyph = icon,
                HeaderColorHex = headerColorHex,
                LaneId = laneId
            };
            _definition.Nodes.Add(node);
            Invalidate();
            DefinitionChanged?.Invoke(this, EventArgs.Empty);
            return node;
        }

        /// <summary>
        /// Programmatically adds a decision diamond node.
        /// </summary>
        public ProcessFlowNode AddDecision(string id, string title, string subtitle, double x, double y, double width = 180, double height = 80, string actionKey = "", string headerColorHex = "#0284C7", string? laneId = null)
        {
            var node = new ProcessFlowNode(id, title, subtitle, x, y, width, height)
            {
                Shape = ProcessNodeShape.DecisionDiamond,
                ActionKey = actionKey,
                IconGlyph = "🔍",
                HeaderColorHex = headerColorHex,
                BorderColorHex = "#38BDF8",
                LaneId = laneId
            };
            _definition.Nodes.Add(node);
            Invalidate();
            DefinitionChanged?.Invoke(this, EventArgs.Empty);
            return node;
        }

        /// <summary>
        /// Programmatically adds a swimlane boundary.
        /// </summary>
        public ProcessFlowLane AddLane(string id, string title, double x, double y, double width, double height, string headerColorHex = "#64748B", string bgColorHex = "#F8FAFC")
        {
            var lane = new ProcessFlowLane(id, title, x, y, width, height)
            {
                HeaderColorHex = headerColorHex,
                BackgroundColorHex = bgColorHex
            };
            _definition.Lanes.Add(lane);
            Invalidate();
            DefinitionChanged?.Invoke(this, EventArgs.Empty);
            return lane;
        }

        /// <summary>
        /// Programmatically adds a directional orthogonal connection between two steps.
        /// </summary>
        public ProcessFlowConnection AddConnection(string sourceNodeId, string targetNodeId, string label = "", string strokeColorHex = "#0EA5E9", ProcessPortPosition sourcePort = ProcessPortPosition.Bottom, ProcessPortPosition targetPort = ProcessPortPosition.Top, bool isDashed = false)
        {
            var conn = new ProcessFlowConnection(sourceNodeId, targetNodeId, label, strokeColorHex)
            {
                SourcePort = sourcePort,
                TargetPort = targetPort,
                IsDashed = isDashed,
                RoutingMode = ProcessRoutingMode.Orthogonal
            };
            _definition.Connections.Add(conn);
            Invalidate();
            DefinitionChanged?.Invoke(this, EventArgs.Empty);
            return conn;
        }

        #endregion

        #region Zoom and Viewport Navigation API

        /// <summary>
        /// Automatically scales and centers the diagram to fit perfectly inside the viewport.
        /// </summary>
        public void ZoomToFit(int padding = 40)
        {
            if (_definition.Nodes.Count == 0)
            {
                _zoom = 1.0f;
                _panOffset = new PointF(0, 0);
                Invalidate();
                return;
            }

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            foreach (var node in _definition.Nodes)
            {
                minX = Math.Min(minX, (float)node.X);
                minY = Math.Min(minY, (float)node.Y);
                maxX = Math.Max(maxX, (float)(node.X + node.Width));
                maxY = Math.Max(maxY, (float)(node.Y + node.Height));
            }

            float contentWidth = maxX - minX;
            float contentHeight = maxY - minY;

            if (contentWidth <= 0 || contentHeight <= 0)
            {
                _zoom = 1.0f;
                _panOffset = new PointF(0, 0);
                Invalidate();
                return;
            }

            float availWidth = Math.Max(100, Width - padding * 2);
            float availHeight = Math.Max(100, Height - padding * 2);

            float targetZoom = Math.Min(availWidth / contentWidth, availHeight / contentHeight);
            targetZoom = Math.Max(0.25f, Math.Min(1.5f, targetZoom));

            _zoom = targetZoom;
            _panOffset = new PointF(
                padding + (availWidth - contentWidth * _zoom) / 2f - minX * _zoom,
                padding + (availHeight - contentHeight * _zoom) / 2f - minY * _zoom
            );

            Invalidate();
        }

        /// <summary>
        /// Zooms in towards the center of the control viewport.
        /// </summary>
        public void ZoomIn(float factor = 1.15f)
        {
            PointF center = new PointF(Width / 2f, Height / 2f);
            float oldZoom = _zoom;
            ZoomFactor = _zoom * factor;
            _panOffset = new PointF(
                center.X - (center.X - _panOffset.X) * (_zoom / oldZoom),
                center.Y - (center.Y - _panOffset.Y) * (_zoom / oldZoom)
            );
            Invalidate();
        }

        /// <summary>
        /// Zooms out away from the center of the control viewport.
        /// </summary>
        public void ZoomOut(float factor = 0.87f)
        {
            PointF center = new PointF(Width / 2f, Height / 2f);
            float oldZoom = _zoom;
            ZoomFactor = _zoom * factor;
            _panOffset = new PointF(
                center.X - (center.X - _panOffset.X) * (_zoom / oldZoom),
                center.Y - (center.Y - _panOffset.Y) * (_zoom / oldZoom)
            );
            Invalidate();
        }

        /// <summary>
        /// Resets the zoom level to 100% (1.0x).
        /// </summary>
        public void ResetZoom()
        {
            PointF center = new PointF(Width / 2f, Height / 2f);
            float oldZoom = _zoom;
            ZoomFactor = 1.0f;
            _panOffset = new PointF(
                center.X - (center.X - _panOffset.X) * (_zoom / oldZoom),
                center.Y - (center.Y - _panOffset.Y) * (_zoom / oldZoom)
            );
            Invalidate();
        }

        #endregion

        #region Persistence Methods

        /// <summary>
        /// Exports the current process diagram definition to a clean JSON string.
        /// </summary>
        public string ExportJson(bool indented = true)
        {
            return ProcessFlowSerializer.ToJson(_definition, indented);
        }

        /// <summary>
        /// Imports and loads a process diagram definition from a JSON string.
        /// </summary>
        public void ImportJson(string json)
        {
            Definition = ProcessFlowSerializer.FromJson(json);
        }

        #endregion
    }

    /// <summary>
    /// Legacy alias for <see cref="ProcessMap"/>.
    /// </summary>
    [Obsolete("ZeroProcessMap is deprecated. Use ProcessMap instead.")]
    public class ZeroProcessMap : ProcessMap
    {
    }
}
