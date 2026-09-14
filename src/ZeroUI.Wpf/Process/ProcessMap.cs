using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Core.Process;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Process
{
    /// <summary>
    /// High-performance interactive business process map and workflow navigation control for WPF.
    /// Supports multi-lane workflow visualization, card tasks, decision diamonds,
    /// orthogonal transition arrows with branching labels, UserControl action triggers,
    /// and interactive design-mode customization.
    /// </summary>
    public class ProcessMap : FrameworkElement
    {
        private ProcessFlowDefinition _definition = new ProcessFlowDefinition();
        private bool _isDesignMode = false;
        private bool _showGrid = true;
        private bool _autoExecuteAction = true;

        private double _zoom = 1.0;
        private Point _panOffset = new Point(0, 0);

        private bool _isPanning = false;
        private Point _panStartMouse;
        private Point _panStartOffset;

        private ProcessFlowNode? _selectedNode;
        private ProcessFlowNode? _hoveredNode;
        private bool _isDraggingNode = false;
        private Point _nodeDragStartMouse;
        private Point _nodeDragStartPos;
        private Point _rightClickLocation;

        private ContextMenu _contextMenu = null!;

        public event EventHandler<ProcessFlowNode>? NodeClicked;
        public event EventHandler<ProcessActionContext>? ActionTriggered;
        public event EventHandler? DefinitionChanged;

        public ProcessMap()
        {
            ClipToBounds = true;
            Focusable = true;
            Cursor = Cursors.Arrow;

            InitContextMenu();

            ZeroWpfTheme.ThemeChanged += () => InvalidateVisual();
        }

        #region Public Properties

        public ProcessFlowDefinition Definition
        {
            get => _definition;
            set
            {
                _definition = value ?? new ProcessFlowDefinition();
                _selectedNode = null;
                _hoveredNode = null;
                InvalidateVisual();
                DefinitionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsDesignMode
        {
            get => _isDesignMode;
            set
            {
                _isDesignMode = value;
                Cursor = Cursors.Arrow;
                InvalidateVisual();
            }
        }

        public bool ShowGrid
        {
            get => _showGrid;
            set
            {
                _showGrid = value;
                InvalidateVisual();
            }
        }

        public bool AutoExecuteAction
        {
            get => _autoExecuteAction;
            set => _autoExecuteAction = value;
        }

        public double ZoomFactor
        {
            get => _zoom;
            set
            {
                _zoom = Math.Max(0.2, Math.Min(3.0, value));
                InvalidateVisual();
            }
        }

        public Point PanOffset
        {
            get => _panOffset;
            set
            {
                _panOffset = value;
                InvalidateVisual();
            }
        }

        public ProcessFlowNode? SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (_selectedNode != value)
                {
                    _selectedNode = value;
                    InvalidateVisual();
                }
            }
        }

        #endregion

        #region Coordinate Transforms

        public Point ScreenToWorld(Point screen)
        {
            return new Point((screen.X - _panOffset.X) / _zoom, (screen.Y - _panOffset.Y) / _zoom);
        }

        public Point WorldToScreen(Point world)
        {
            return new Point(world.X * _zoom + _panOffset.X, world.Y * _zoom + _panOffset.Y);
        }

        #endregion

        #region Context Menu

        private void InitContextMenu()
        {
            _contextMenu = new ContextMenu();
            _contextMenu.Opened += OnContextMenuOpened;
        }

        private void OnContextMenuOpened(object sender, RoutedEventArgs e)
        {
            _contextMenu.Items.Clear();

            if (_selectedNode == null)
            {
                var mnuAdd = new MenuItem { Header = "Add New Step Here" };
                mnuAdd.Click += (s, ev) =>
                {
                    int nextIdx = _definition.Nodes.Count + 1;
                    var newNode = new ProcessFlowNode(
                        Guid.NewGuid().ToString("N"),
                        $"Step {nextIdx}",
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
                    InvalidateVisual();
                    DefinitionChanged?.Invoke(this, EventArgs.Empty);
                };
                _contextMenu.Items.Add(mnuAdd);
                return;
            }

            // Node Context Menu
            var mnuEdit = new MenuItem { Header = "Edit Title & Subtitle..." };
            mnuEdit.Click += (s, ev) => ShowEditTitleDialog();
            _contextMenu.Items.Add(mnuEdit);

            // Assign Action Submenu
            var mnuAction = new MenuItem { Header = "Assign Navigation Action" };
            var mnuClearAction = new MenuItem { Header = "(None / Clear Action)" };
            mnuClearAction.Click += (s, ev) =>
            {
                if (_selectedNode != null)
                {
                    _selectedNode.ActionKey = string.Empty;
                    InvalidateVisual();
                }
            };
            mnuAction.Items.Add(mnuClearAction);
            mnuAction.Items.Add(new Separator());

            var actions = ProcessActionRegistry.GetAllActions();
            if (actions.Count == 0)
            {
                mnuAction.Items.Add(new MenuItem { Header = "(No registered actions)", IsEnabled = false });
            }
            else
            {
                var groups = actions.GroupBy(a => a.Category);
                foreach (var grp in groups)
                {
                    var catMenu = new MenuItem { Header = grp.Key };
                    foreach (var act in grp)
                    {
                        string actionKey = act.Key;
                        var actItem = new MenuItem
                        {
                            Header = act.Title,
                            IsChecked = (_selectedNode?.ActionKey == actionKey)
                        };
                        actItem.Click += (s, ev) =>
                        {
                            if (_selectedNode != null)
                            {
                                _selectedNode.ActionKey = actionKey;
                                InvalidateVisual();
                            }
                        };
                        catMenu.Items.Add(actItem);
                    }
                    mnuAction.Items.Add(catMenu);
                }
            }
            _contextMenu.Items.Add(mnuAction);

            // Change Shape Submenu
            var mnuShape = new MenuItem { Header = "Change Shape" };
            AddShapeOption(mnuShape, "Task Card", ProcessNodeShape.TaskCard);
            AddShapeOption(mnuShape, "Decision Diamond", ProcessNodeShape.DecisionDiamond);
            AddShapeOption(mnuShape, "Start Terminal", ProcessNodeShape.StartTerminal);
            AddShapeOption(mnuShape, "End Terminal", ProcessNodeShape.EndTerminal);
            _contextMenu.Items.Add(mnuShape);

            _contextMenu.Items.Add(new Separator());

            var mnuDelete = new MenuItem { Header = "Delete Step" };
            mnuDelete.Click += (s, ev) =>
            {
                if (_selectedNode != null)
                {
                    string id = _selectedNode.Id;
                    _definition.Nodes.Remove(_selectedNode);
                    _definition.Connections.RemoveAll(c => c.SourceNodeId == id || c.TargetNodeId == id);
                    _selectedNode = null;
                    InvalidateVisual();
                    DefinitionChanged?.Invoke(this, EventArgs.Empty);
                }
            };
            _contextMenu.Items.Add(mnuDelete);
        }

        private void AddShapeOption(MenuItem parent, string title, ProcessNodeShape shape)
        {
            var item = new MenuItem { Header = title };
            item.Click += (s, ev) =>
            {
                if (_selectedNode != null)
                {
                    _selectedNode.Shape = shape;
                    InvalidateVisual();
                    DefinitionChanged?.Invoke(this, EventArgs.Empty);
                }
            };
            parent.Items.Add(item);
        }

        private void ShowEditTitleDialog()
        {
            if (_selectedNode == null) return;

            var win = new Window
            {
                Title = "Configure Process Node",
                Width = 380,
                Height = 260,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Background = ZeroWpfTheme.BgCard
            };

            var stack = new StackPanel { Margin = new Thickness(16) };

            stack.Children.Add(new TextBlock { Text = "Title:", Foreground = ZeroWpfTheme.TextPrimary, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            var txtTitle = new TextBox { Text = _selectedNode.Title, Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(4) };
            stack.Children.Add(txtTitle);

            stack.Children.Add(new TextBlock { Text = "Subtitle / Description:", Foreground = ZeroWpfTheme.TextPrimary, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            var txtSub = new TextBox { Text = _selectedNode.Subtitle, Height = 55, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(4) };
            stack.Children.Add(txtSub);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 6, 0, 0) };
            var btnOk = new Button { Content = "OK", Width = 75, Height = 26, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            var btnCancel = new Button { Content = "Cancel", Width = 75, Height = 26, IsCancel = true };

            btnOk.Click += (s, e) =>
            {
                _selectedNode.Title = txtTitle.Text;
                _selectedNode.Subtitle = txtSub.Text;
                InvalidateVisual();
                DefinitionChanged?.Invoke(this, EventArgs.Empty);
                win.DialogResult = true;
                win.Close();
            };

            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);
            stack.Children.Add(btnPanel);

            win.Content = stack;
            win.ShowDialog();
        }

        #endregion

        #region Rendering Pipeline

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            // 1. Background
            dc.DrawRectangle(ZeroWpfTheme.BgPrimary, null, new Rect(0, 0, w, h));

            // 2. Background Grid
            if (_showGrid)
            {
                DrawGrid(dc, w, h);
            }

            // 3. Transform Matrix for Pan & Zoom
            dc.PushTransform(new MatrixTransform(_zoom, 0, 0, _zoom, _panOffset.X, _panOffset.Y));

            // 4. Swimlanes
            foreach (var lane in _definition.Lanes)
            {
                DrawLane(dc, lane);
            }

            // 5. Connections (Orthogonal Lines & Arrowheads)
            foreach (var conn in _definition.Connections)
            {
                DrawConnection(dc, conn);
            }

            // 6. Nodes
            foreach (var node in _definition.Nodes)
            {
                DrawNode(dc, node, node == _selectedNode, node == _hoveredNode);
            }

            dc.Pop(); // Restore Transform

            // 7. HUD Overlays
            DrawHudOverlay(dc, w, h);
        }

        private void DrawGrid(DrawingContext dc, double w, double h)
        {
            double step = 25 * _zoom;
            if (step < 8) return;

            double startX = _panOffset.X % step;
            if (startX < 0) startX += step;
            double startY = _panOffset.Y % step;
            if (startY < 0) startY += step;

            var dotBrush = new SolidColorBrush(Color.FromArgb(28, 148, 163, 184));
            dotBrush.Freeze();

            for (double x = startX; x < w; x += step)
            {
                for (double y = startY; y < h; y += step)
                {
                    dc.DrawRectangle(dotBrush, null, new Rect(x - 1, y - 1, 2, 2));
                }
            }
        }

        private void DrawLane(DrawingContext dc, ProcessFlowLane lane)
        {
            var rect = new Rect(lane.X, lane.Y, lane.Width, lane.Height);
            Color bg = ParseColor(lane.BackgroundColorHex, Color.FromArgb(245, 248, 250, 252));
            Color border = Color.FromArgb(40, 148, 163, 184);

            var bgBrush = new SolidColorBrush(bg);
            bgBrush.Freeze();
            var borderPen = new Pen(new SolidColorBrush(border), 1.5) { DashStyle = DashStyles.Dash };
            borderPen.Freeze();

            dc.DrawRoundedRectangle(bgBrush, borderPen, rect, 12, 12);

            // Lane Header Title
            string title = "⚙ " + lane.Title.ToUpperInvariant();
            Color headerColor = ParseColor(lane.HeaderColorHex, Color.FromRgb(100, 116, 139));
            var font = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            var headerText = new FormattedText(
                title,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                font,
                11,
                new SolidColorBrush(headerColor),
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );

            dc.DrawText(headerText, new Point(rect.X + 16, rect.Y + 12));
        }

        private void DrawConnection(DrawingContext dc, ProcessFlowConnection conn)
        {
            var srcNode = _definition.Nodes.FirstOrDefault(n => n.Id == conn.SourceNodeId);
            var tgtNode = _definition.Nodes.FirstOrDefault(n => n.Id == conn.TargetNodeId);
            if (srcNode == null || tgtNode == null) return;

            Point p1 = GetPortLocation(srcNode, conn.SourcePort);
            Point p2 = GetPortLocation(tgtNode, conn.TargetPort);

            Color stroke = ParseColor(conn.StrokeColorHex, Color.FromRgb(14, 165, 233));
            var brush = new SolidColorBrush(stroke);
            brush.Freeze();
            var pen = new Pen(brush, conn.StrokeThickness);
            if (conn.IsDashed) pen.DashStyle = DashStyles.Dash;
            pen.Freeze();

            var points = CalculateOrthogonalRoute(p1, p2, conn.SourcePort, conn.TargetPort);
            if (points.Count >= 2)
            {
                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    ctx.BeginFigure(points[0], false, false);
                    for (int i = 1; i < points.Count; i++)
                    {
                        ctx.LineTo(points[i], true, false);
                    }
                }
                geometry.Freeze();
                dc.DrawGeometry(null, pen, geometry);

                // Arrowhead
                DrawArrowhead(dc, brush, points[points.Count - 2], points[points.Count - 1], conn.StrokeThickness);

                // Label
                if (!string.IsNullOrWhiteSpace(conn.Label))
                {
                    DrawConnectionLabel(dc, conn.Label, points, stroke);
                }
            }
        }

        private List<Point> CalculateOrthogonalRoute(Point p1, Point p2, ProcessPortPosition sp, ProcessPortPosition tp)
        {
            var list = new List<Point> { p1 };

            if (Math.Abs(p1.X - p2.X) < 4 && p2.Y > p1.Y)
            {
                list.Add(p2);
                return list;
            }

            if (sp == ProcessPortPosition.Bottom && tp == ProcessPortPosition.Top)
            {
                double midY = (p1.Y + p2.Y) / 2;
                list.Add(new Point(p1.X, midY));
                list.Add(new Point(p2.X, midY));
            }
            else if (sp == ProcessPortPosition.Right && tp == ProcessPortPosition.Top)
            {
                list.Add(new Point(p2.X, p1.Y));
            }
            else if (sp == ProcessPortPosition.Left && tp == ProcessPortPosition.Top)
            {
                list.Add(new Point(p2.X, p1.Y));
            }
            else if (sp == ProcessPortPosition.Left && tp == ProcessPortPosition.Right)
            {
                double midX = (p1.X + p2.X) / 2;
                list.Add(new Point(midX, p1.Y));
                list.Add(new Point(midX, p2.Y));
            }
            else
            {
                double midX = (p1.X + p2.X) / 2;
                list.Add(new Point(midX, p1.Y));
                list.Add(new Point(midX, p2.Y));
            }

            list.Add(p2);
            return list;
        }

        private void DrawArrowhead(DrawingContext dc, Brush brush, Point from, Point to, double thickness)
        {
            double arrowSize = 6.0 + thickness;
            double dx = to.X - from.X;
            double dy = to.Y - from.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.001) return;

            double uX = dx / len;
            double uY = dy / len;
            double vX = -uY;
            double vY = uX;

            Point pLeft = new Point(to.X - uX * arrowSize + vX * (arrowSize * 0.5), to.Y - uY * arrowSize + vY * (arrowSize * 0.5));
            Point pRight = new Point(to.X - uX * arrowSize - vX * (arrowSize * 0.5), to.Y - uY * arrowSize - vY * (arrowSize * 0.5));

            var geom = new StreamGeometry();
            using (var ctx = geom.Open())
            {
                ctx.BeginFigure(to, true, true);
                ctx.LineTo(pLeft, true, false);
                ctx.LineTo(pRight, true, false);
            }
            geom.Freeze();
            dc.DrawGeometry(brush, null, geom);
        }

        private void DrawConnectionLabel(DrawingContext dc, string label, List<Point> points, Color color)
        {
            int midIdx = points.Count / 2;
            Point a = points[midIdx - 1];
            Point b = points[midIdx];
            Point center = new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2);

            var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            var text = new FormattedText(
                label,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                10,
                new SolidColorBrush(color),
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );

            var rect = new Rect(center.X - text.Width / 2 - 6, center.Y - text.Height / 2 - 3, text.Width + 12, text.Height + 6);
            var bgBrush = new SolidColorBrush(Color.FromArgb(245, 255, 255, 255));
            bgBrush.Freeze();
            var pen = new Pen(new SolidColorBrush(color), 1.2);
            pen.Freeze();

            dc.DrawRoundedRectangle(bgBrush, pen, rect, 4, 4);
            dc.DrawText(text, new Point(rect.X + 6, rect.Y + 3));
        }

        private void DrawNode(DrawingContext dc, ProcessFlowNode node, bool isSelected, bool isHovered)
        {
            var bounds = new Rect(node.X, node.Y, node.Width, node.Height);

            switch (node.Shape)
            {
                case ProcessNodeShape.DecisionDiamond:
                    DrawDecisionDiamond(dc, node, bounds, isSelected, isHovered);
                    break;
                case ProcessNodeShape.StartTerminal:
                case ProcessNodeShape.EndTerminal:
                    DrawTerminalNode(dc, node, bounds, isSelected, isHovered);
                    break;
                case ProcessNodeShape.TaskCard:
                default:
                    DrawTaskCard(dc, node, bounds, isSelected, isHovered);
                    break;
            }

            if (_isDesignMode && isSelected)
            {
                DrawPortDots(dc, node);
            }
        }

        private void DrawTaskCard(DrawingContext dc, ProcessFlowNode node, Rect bounds, bool isSelected, bool isHovered)
        {
            Color headerColor = ParseColor(node.HeaderColorHex, Color.FromRgb(59, 130, 246));
            Color borderColor = isSelected ? Color.FromRgb(59, 130, 246) : (isHovered ? headerColor : ParseColor(node.BorderColorHex, Color.FromRgb(203, 213, 225)));
            double borderWidth = isSelected ? 2.2 : (isHovered ? 1.8 : 1.0);

            // Card Background & Border
            var borderPen = new Pen(new SolidColorBrush(borderColor), borderWidth);
            if (isSelected && _isDesignMode) borderPen.DashStyle = DashStyles.Dash;
            borderPen.Freeze();

            var bgBrush = new SolidColorBrush(Colors.White);
            bgBrush.Freeze();
            dc.DrawRoundedRectangle(bgBrush, borderPen, bounds, 10, 10);

            // Header Background Ribbon
            var headerRect = new Rect(bounds.X, bounds.Y, bounds.Width, 28);
            var headerBg = new SolidColorBrush(Color.FromArgb(20, headerColor.R, headerColor.G, headerColor.B));
            headerBg.Freeze();
            dc.DrawRoundedRectangle(headerBg, null, headerRect, 10, 10);

            // Header Text (Icon + Title)
            var titleTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            var titleText = new FormattedText(
                $"{node.IconGlyph} {node.Title}",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                titleTypeface,
                11.5,
                new SolidColorBrush(headerColor),
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );
            dc.DrawText(titleText, new Point(bounds.X + 10, bounds.Y + 5));

            // Subtitle Description
            if (!string.IsNullOrWhiteSpace(node.Subtitle))
            {
                var subTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Regular, FontStretches.Normal);
                var subText = new FormattedText(
                    node.Subtitle,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    subTypeface,
                    10.5,
                    new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                    VisualTreeHelper.GetDpi(this).PixelsPerDip
                )
                {
                    MaxTextWidth = Math.Max(10, bounds.Width - 20),
                    MaxTextHeight = Math.Max(10, bounds.Height - 34)
                };
                dc.DrawText(subText, new Point(bounds.X + 10, bounds.Y + 32));
            }

            // Action Badge Indicator
            if (!string.IsNullOrWhiteSpace(node.ActionKey))
            {
                var badgeTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Regular, FontStretches.Normal);
                var badgeText = new FormattedText(
                    "⚡ Action",
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    badgeTypeface,
                    9.0,
                    new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    VisualTreeHelper.GetDpi(this).PixelsPerDip
                );
                dc.DrawText(badgeText, new Point(bounds.Right - 46, bounds.Bottom - 16));
            }
        }

        private void DrawDecisionDiamond(DrawingContext dc, ProcessFlowNode node, Rect bounds, bool isSelected, bool isHovered)
        {
            Point top = new Point(bounds.X + bounds.Width / 2, bounds.Y);
            Point right = new Point(bounds.Right, bounds.Y + bounds.Height / 2);
            Point bottom = new Point(bounds.X + bounds.Width / 2, bounds.Bottom);
            Point left = new Point(bounds.X, bounds.Y + bounds.Height / 2);

            Color accent = ParseColor(node.HeaderColorHex, Color.FromRgb(2, 132, 199));
            Color border = isSelected ? Color.FromRgb(59, 130, 246) : (isHovered ? accent : ParseColor(node.BorderColorHex, Color.FromRgb(56, 189, 248)));
            double thick = isSelected ? 2.5 : (isHovered ? 2.0 : 1.4);

            var geom = new StreamGeometry();
            using (var ctx = geom.Open())
            {
                ctx.BeginFigure(top, true, true);
                ctx.LineTo(right, true, false);
                ctx.LineTo(bottom, true, false);
                ctx.LineTo(left, true, false);
            }
            geom.Freeze();

            var fill = new SolidColorBrush(Color.FromRgb(245, 250, 255));
            fill.Freeze();
            var pen = new Pen(new SolidColorBrush(border), thick);
            if (isSelected && _isDesignMode) pen.DashStyle = DashStyles.Dash;
            pen.Freeze();

            dc.DrawGeometry(fill, pen, geom);

            // Centered Title Text
            string text = node.Title;
            if (!string.IsNullOrWhiteSpace(node.Subtitle)) text += "\n" + node.Subtitle;

            var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            var formatted = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                11,
                new SolidColorBrush(accent),
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            )
            {
                TextAlignment = TextAlignment.Center,
                MaxTextWidth = bounds.Width - 16
            };
            dc.DrawText(formatted, new Point(bounds.X + 8, bounds.Y + (bounds.Height - formatted.Height) / 2));
        }

        private void DrawTerminalNode(DrawingContext dc, ProcessFlowNode node, Rect bounds, bool isSelected, bool isHovered)
        {
            Color bg = ParseColor(node.HeaderColorHex, Color.FromRgb(22, 163, 74));
            var fill = new SolidColorBrush(bg);
            fill.Freeze();
            var pen = new Pen(isSelected ? Brushes.White : new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)), 2.0);
            pen.Freeze();

            dc.DrawRoundedRectangle(fill, pen, bounds, bounds.Height / 2, bounds.Height / 2);

            var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            var text = new FormattedText(
                node.Title,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                11,
                Brushes.White,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            )
            {
                TextAlignment = TextAlignment.Center,
                MaxTextWidth = bounds.Width
            };
            dc.DrawText(text, new Point(bounds.X, bounds.Y + (bounds.Height - text.Height) / 2));
        }

        private void DrawPortDots(DrawingContext dc, ProcessFlowNode node)
        {
            var ports = new ProcessPortPosition[] { ProcessPortPosition.Top, ProcessPortPosition.Bottom, ProcessPortPosition.Left, ProcessPortPosition.Right };
            var brush = new SolidColorBrush(Color.FromRgb(59, 130, 246));
            brush.Freeze();
            var pen = new Pen(Brushes.White, 1.5);
            pen.Freeze();

            foreach (var p in ports)
            {
                var pt = GetPortLocation(node, p);
                dc.DrawEllipse(brush, pen, pt, 4, 4);
            }
        }

        private void DrawHudOverlay(DrawingContext dc, double w, double h)
        {
            // Title & Description
            var titleTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            var titleText = new FormattedText(
                _definition.Title,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                titleTypeface,
                13.5,
                ZeroWpfTheme.TextPrimary,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );
            dc.DrawText(titleText, new Point(24, 16));

            if (!string.IsNullOrWhiteSpace(_definition.Description))
            {
                var descTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Regular, FontStretches.Normal);
                var descText = new FormattedText(
                    "ℹ " + _definition.Description,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    descTypeface,
                    11,
                    ZeroWpfTheme.TextSecondary,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip
                );
                dc.DrawText(descText, new Point(w - descText.Width - 24, 18));
            }

            // Mode Badge
            string modeStr = _isDesignMode ? "✏ DESIGN MODE (Drag to move, Right-click to edit)" : "▶ RUN MODE (Click step to navigate)";
            Color badgeBg = _isDesignMode ? Color.FromRgb(245, 158, 11) : Color.FromRgb(16, 185, 129);

            var modeTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            var modeText = new FormattedText(
                modeStr,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                modeTypeface,
                10.5,
                Brushes.White,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );

            var badgeRect = new Rect(16, h - 36, modeText.Width + 20, 24);
            var badgeBrush = new SolidColorBrush(badgeBg);
            badgeBrush.Freeze();
            dc.DrawRoundedRectangle(badgeBrush, null, badgeRect, 12, 12);
            dc.DrawText(modeText, new Point(badgeRect.X + 10, badgeRect.Y + 4));

            // Zoom Info
            var zoomText = new FormattedText(
                $"{(_zoom * 100):0}%",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                titleTypeface,
                10.5,
                ZeroWpfTheme.TextSecondary,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );
            dc.DrawText(zoomText, new Point(w - 50, h - 30));
        }

        #endregion

        #region Helpers & Ports

        public Point GetPortLocation(ProcessFlowNode node, ProcessPortPosition port)
        {
            double x = node.X;
            double y = node.Y;
            double w = node.Width;
            double h = node.Height;

            switch (port)
            {
                case ProcessPortPosition.Top:
                    return new Point(x + w / 2, y);
                case ProcessPortPosition.Bottom:
                    return new Point(x + w / 2, y + h);
                case ProcessPortPosition.Left:
                    return new Point(x, y + h / 2);
                case ProcessPortPosition.Right:
                    return new Point(x + w, y + h / 2);
                case ProcessPortPosition.Center:
                default:
                    return new Point(x + w / 2, y + h / 2);
            }
        }

        private static Color ParseColor(string hex, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(hex)) return fallback;
            try
            {
                return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch
            {
                return fallback;
            }
        }

        #endregion

        #region Mouse Interaction

        private ProcessFlowNode? HitTestNode(Point worldPt)
        {
            for (int i = _definition.Nodes.Count - 1; i >= 0; i--)
            {
                var n = _definition.Nodes[i];
                var rect = new Rect(n.X, n.Y, n.Width, n.Height);
                if (rect.Contains(worldPt))
                {
                    return n;
                }
            }
            return null;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            Point screenPt = e.GetPosition(this);
            Point worldPt = ScreenToWorld(screenPt);

            if (e.ChangedButton == MouseButton.Middle || (e.ChangedButton == MouseButton.Right && !_isDesignMode))
            {
                _isPanning = true;
                _panStartMouse = screenPt;
                _panStartOffset = _panOffset;
                CaptureMouse();
                e.Handled = true;
                return;
            }

            if (e.ChangedButton == MouseButton.Left)
            {
                var hit = HitTestNode(worldPt);
                if (hit != null)
                {
                    SelectedNode = hit;
                    if (_isDesignMode)
                    {
                        _isDraggingNode = true;
                        _nodeDragStartMouse = screenPt;
                        _nodeDragStartPos = new Point(hit.X, hit.Y);
                        CaptureMouse();
                    }
                }
                else
                {
                    SelectedNode = null;
                    _isPanning = true;
                    _panStartMouse = screenPt;
                    _panStartOffset = _panOffset;
                    CaptureMouse();
                }
                e.Handled = true;
            }
            else if (e.ChangedButton == MouseButton.Right && _isDesignMode)
            {
                _rightClickLocation = worldPt;
                SelectedNode = HitTestNode(worldPt);
                _contextMenu.PlacementTarget = this;
                _contextMenu.IsOpen = true;
                e.Handled = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Point screenPt = e.GetPosition(this);
            Point worldPt = ScreenToWorld(screenPt);

            if (_isPanning)
            {
                _panOffset = new Point(
                    _panStartOffset.X + (screenPt.X - _panStartMouse.X),
                    _panStartOffset.Y + (screenPt.Y - _panStartMouse.Y)
                );
                InvalidateVisual();
                return;
            }

            if (_isDraggingNode && _selectedNode != null && _isDesignMode)
            {
                double dx = (screenPt.X - _nodeDragStartMouse.X) / _zoom;
                double dy = (screenPt.Y - _nodeDragStartMouse.Y) / _zoom;
                _selectedNode.X = Math.Max(0, _nodeDragStartPos.X + dx);
                _selectedNode.Y = Math.Max(0, _nodeDragStartPos.Y + dy);
                InvalidateVisual();
                return;
            }

            var hovered = HitTestNode(worldPt);
            if (hovered != _hoveredNode)
            {
                _hoveredNode = hovered;
                Cursor = _hoveredNode != null ? (_isDesignMode ? Cursors.SizeAll : Cursors.Hand) : Cursors.Arrow;
                InvalidateVisual();
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            ReleaseMouseCapture();

            bool wasDragging = _isDraggingNode;
            _isPanning = false;
            _isDraggingNode = false;

            Point worldPt = ScreenToWorld(e.GetPosition(this));
            var hit = HitTestNode(worldPt);

            if (e.ChangedButton == MouseButton.Left && !wasDragging && hit != null)
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
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            double oldZoom = _zoom;
            double zoomDelta = e.Delta > 0 ? 1.1 : 0.9;
            ZoomFactor = _zoom * zoomDelta;

            Point mouse = e.GetPosition(this);
            _panOffset = new Point(
                mouse.X - (mouse.X - _panOffset.X) * (_zoom / oldZoom),
                mouse.Y - (mouse.Y - _panOffset.Y) * (_zoom / oldZoom)
            );
            InvalidateVisual();
        }

        #endregion

        #region Fluent Builder API & Persistence

        public void Clear()
        {
            _definition.Lanes.Clear();
            _definition.Nodes.Clear();
            _definition.Connections.Clear();
            _selectedNode = null;
            _hoveredNode = null;
            InvalidateVisual();
            DefinitionChanged?.Invoke(this, EventArgs.Empty);
        }

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
            InvalidateVisual();
            DefinitionChanged?.Invoke(this, EventArgs.Empty);
            return node;
        }

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
            InvalidateVisual();
            DefinitionChanged?.Invoke(this, EventArgs.Empty);
            return node;
        }

        public ProcessFlowLane AddLane(string id, string title, double x, double y, double width, double height, string headerColorHex = "#64748B", string bgColorHex = "#F8FAFC")
        {
            var lane = new ProcessFlowLane(id, title, x, y, width, height)
            {
                HeaderColorHex = headerColorHex,
                BackgroundColorHex = bgColorHex
            };
            _definition.Lanes.Add(lane);
            InvalidateVisual();
            DefinitionChanged?.Invoke(this, EventArgs.Empty);
            return lane;
        }

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
            InvalidateVisual();
            DefinitionChanged?.Invoke(this, EventArgs.Empty);
            return conn;
        }

        public string ExportJson(bool indented = true)
        {
            return ProcessFlowSerializer.ToJson(_definition, indented);
        }

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
