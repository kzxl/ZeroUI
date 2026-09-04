using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Overlays;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Docking
{
    public enum ZeroDockPosition
    {
        Left,
        Right,
        Top,
        Bottom,
        Document,
        Float
    }

    /// <summary>
    /// Represents an individual docking panel container hosting child controls with a modern themed header,
    /// pin toggle, float button, and close actions.
    /// </summary>
    [ToolboxItem(false)]
    public class ZeroDockPanel : Panel
    {
        private string _title = "Panel";
        private ZeroDockPosition _dockPosition = ZeroDockPosition.Document;
        private bool _isPinned = true;
        private bool _autoHide = false;
        private bool _closable = true;
        private bool _floatable = true;
        private int _headerHeight = 28;

        private Rectangle _pinButtonRect;
        private Rectangle _floatButtonRect;
        private Rectangle _closeButtonRect;
        private bool _isPinHovered = false;
        private bool _isFloatHovered = false;
        private bool _isCloseHovered = false;

        private bool _isDraggingHeader = false;
        private Point _dragStartPoint;

        public event EventHandler? DockPositionChanged;
        public event EventHandler? PinStateChanged;
        public event EventHandler? CloseRequested;
        public event EventHandler? FloatRequested;

        [Category("Appearance")]
        [DefaultValue("Panel")]
        public string Title
        {
            get => _title;
            set { _title = value; Invalidate(); }
        }

        [Category("Layout")]
        [DefaultValue(ZeroDockPosition.Document)]
        public ZeroDockPosition DockPosition
        {
            get => _dockPosition;
            set
            {
                if (_dockPosition != value)
                {
                    _dockPosition = value;
                    DockPositionChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                if (_isPinned != value)
                {
                    _isPinned = value;
                    PinStateChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool AutoHide
        {
            get => _autoHide;
            set { _autoHide = value; Invalidate(); }
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool Closable
        {
            get => _closable;
            set { _closable = value; Invalidate(); }
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool Floatable
        {
            get => _floatable;
            set { _floatable = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(28)]
        public int HeaderHeight
        {
            get => _headerHeight;
            set { _headerHeight = Math.Max(20, value); Invalidate(); }
        }

        public Control? HostedContent { get; set; }

        public ZeroDockPanel()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            Padding = new Padding(0, _headerHeight, 0, 0);
        }

        public ZeroDockPanel(string title, ZeroDockPosition position = ZeroDockPosition.Document) : this()
        {
            Title = title;
            DockPosition = position;
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            if (e.Control != null && HostedContent == null)
            {
                HostedContent = e.Control;
                e.Control.Dock = DockStyle.Fill;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var colors = ZeroTheme.Colors;
            var headerRect = new Rectangle(0, 0, Width, _headerHeight);

            // Draw header background
            using (var brush = new SolidBrush(colors.HeaderBackground))
            {
                g.FillRectangle(brush, headerRect);
            }

            // Draw header bottom border
            using (var pen = new Pen(colors.Border))
            {
                g.DrawLine(pen, 0, _headerHeight - 1, Width, _headerHeight - 1);
            }

            // Draw panel title
            int textX = 10;
            int buttonSize = 16;
            int rightX = Width - 8;

            // Close button rect
            if (_closable)
            {
                rightX -= buttonSize;
                _closeButtonRect = new Rectangle(rightX, (_headerHeight - buttonSize) / 2, buttonSize, buttonSize);
                rightX -= 4;
            }
            else
            {
                _closeButtonRect = Rectangle.Empty;
            }

            // Float button rect
            if (_floatable)
            {
                rightX -= buttonSize;
                _floatButtonRect = new Rectangle(rightX, (_headerHeight - buttonSize) / 2, buttonSize, buttonSize);
                rightX -= 4;
            }
            else
            {
                _floatButtonRect = Rectangle.Empty;
            }

            // Pin button rect
            rightX -= buttonSize;
            _pinButtonRect = new Rectangle(rightX, (_headerHeight - buttonSize) / 2, buttonSize, buttonSize);

            // Draw Title Text
            using (var titleFont = new Font(Font.FontFamily, 9f, FontStyle.Bold))
            using (var textBrush = new SolidBrush(colors.TextPrimary))
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter
                };
                var titleBounds = new Rectangle(textX, 0, Math.Max(20, rightX - textX - 8), _headerHeight);
                g.DrawString(_title, titleFont, textBrush, titleBounds, sf);
            }

            // Render Header Buttons
            DrawHeaderButtons(g, colors);
        }

        private void DrawHeaderButtons(Graphics g, ZeroThemePalette colors)
        {
            // Draw Pin Button
            if (!_pinButtonRect.IsEmpty)
            {
                Color pinColor = _isPinHovered ? colors.Primary : (_isPinned ? colors.TextPrimary : colors.TextSecondary);
                using (var pen = new Pen(pinColor, 1.5f))
                {
                    int cx = _pinButtonRect.X + _pinButtonRect.Width / 2;
                    int cy = _pinButtonRect.Y + _pinButtonRect.Height / 2;
                    if (_isPinned)
                    {
                        // Vertical pin
                        g.DrawLine(pen, cx, cy - 4, cx, cy + 4);
                        g.DrawLine(pen, cx - 3, cy - 2, cx + 3, cy - 2);
                    }
                    else
                    {
                        // Horizontal pin
                        g.DrawLine(pen, cx - 4, cy, cx + 4, cy);
                        g.DrawLine(pen, cx - 2, cy - 3, cx - 2, cy + 3);
                    }
                }
            }

            // Draw Float Button
            if (_floatable && !_floatButtonRect.IsEmpty)
            {
                Color floatColor = _isFloatHovered ? colors.Primary : colors.TextSecondary;
                using (var pen = new Pen(floatColor, 1.3f))
                {
                    int x = _floatButtonRect.X + 2;
                    int y = _floatButtonRect.Y + 2;
                    g.DrawRectangle(pen, x, y + 2, 8, 8);
                    g.DrawRectangle(pen, x + 3, y, 7, 7);
                }
            }

            // Draw Close Button
            if (_closable && !_closeButtonRect.IsEmpty)
            {
                Color closeColor = _isCloseHovered ? colors.Danger : colors.TextSecondary;
                using (var pen = new Pen(closeColor, 1.5f))
                {
                    int x = _closeButtonRect.X + 3;
                    int y = _closeButtonRect.Y + 3;
                    int size = 10;
                    g.DrawLine(pen, x, y, x + size, y + size);
                    g.DrawLine(pen, x + size, y, x, y + size);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && e.Y <= _headerHeight)
            {
                if (_closeButtonRect.Contains(e.Location) && _closable)
                {
                    CloseRequested?.Invoke(this, EventArgs.Empty);
                    return;
                }

                if (_floatButtonRect.Contains(e.Location) && _floatable)
                {
                    FloatRequested?.Invoke(this, EventArgs.Empty);
                    return;
                }

                if (_pinButtonRect.Contains(e.Location))
                {
                    IsPinned = !IsPinned;
                    return;
                }

                // Start dragging header for floating or docking
                _isDraggingHeader = true;
                _dragStartPoint = e.Location;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool prevPin = _isPinHovered;
            bool prevFloat = _isFloatHovered;
            bool prevClose = _isCloseHovered;

            _isPinHovered = _pinButtonRect.Contains(e.Location);
            _isFloatHovered = _floatButtonRect.Contains(e.Location);
            _isCloseHovered = _closeButtonRect.Contains(e.Location);

            if (prevPin != _isPinHovered || prevFloat != _isFloatHovered || prevClose != _isCloseHovered)
            {
                Invalidate(new Rectangle(Width - 80, 0, 80, _headerHeight));
            }

            if (_isDraggingHeader && _floatable && e.Button == MouseButtons.Left)
            {
                int dx = Math.Abs(e.X - _dragStartPoint.X);
                int dy = Math.Abs(e.Y - _dragStartPoint.Y);
                if (dx > 8 || dy > 8)
                {
                    _isDraggingHeader = false;
                    FloatRequested?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDraggingHeader = false;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_isPinHovered || _isFloatHovered || _isCloseHovered)
            {
                _isPinHovered = false;
                _isFloatHovered = false;
                _isCloseHovered = false;
                Invalidate(new Rectangle(Width - 80, 0, 80, _headerHeight));
            }
        }
    }

    /// <summary>
    /// Independent floating tool window hosting a detached ZeroDockPanel across multiple monitors.
    /// </summary>
    public class ZeroFloatingWindow : Form
    {
        private readonly ZeroDockPanel _panel;
        private readonly ZeroDockManager _dockManager;

        public ZeroDockPanel DockPanel => _panel;

        public ZeroFloatingWindow(ZeroDockManager dockManager, ZeroDockPanel panel)
        {
            _dockManager = dockManager ?? throw new ArgumentNullException(nameof(dockManager));
            _panel = panel ?? throw new ArgumentNullException(nameof(panel));

            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            Text = panel.Title;
            Size = new Size(320, 420);
            BackColor = ZeroTheme.Colors.Surface;

            Controls.Add(panel);
            panel.Dock = DockStyle.Fill;

            panel.CloseRequested += (s, e) => Close();
            panel.FloatRequested += (s, e) => RedockToManager(ZeroDockPosition.Document);
        }

        public void RedockToManager(ZeroDockPosition targetPosition)
        {
            Controls.Remove(_panel);
            _dockManager.AddPanel(_panel, targetPosition);
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (Controls.Contains(_panel))
            {
                Controls.Remove(_panel);
            }
        }
    }

    /// <summary>
    /// Enterprise multi-region Dock Manager for WinForms.
    /// Manages Left, Right, Top, Bottom, and Document docking zones with resizable splitters,
    /// tabbed document groups, auto-hide sidebars, and multi-monitor floating windows.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Layout & Windowing")]
    [Description("Enterprise Visual Studio-style multi-region dock manager with splitters, tabs, and multi-monitor floating panels.")]
    public class ZeroDockManager : Control
    {
        private readonly List<ZeroDockPanel> _panels = new List<ZeroDockPanel>();
        private readonly List<ZeroFloatingWindow> _floatingWindows = new List<ZeroFloatingWindow>();

        // Layout Containers
        private readonly Panel _leftContainer = new Panel { Width = 260, Dock = DockStyle.Left, Visible = false };
        private readonly Splitter _leftSplitter = new Splitter { Dock = DockStyle.Left, Width = 5, Visible = false };

        private readonly Panel _rightContainer = new Panel { Width = 280, Dock = DockStyle.Right, Visible = false };
        private readonly Splitter _rightSplitter = new Splitter { Dock = DockStyle.Right, Width = 5, Visible = false };

        private readonly Panel _bottomContainer = new Panel { Height = 180, Dock = DockStyle.Bottom, Visible = false };
        private readonly Splitter _bottomSplitter = new Splitter { Dock = DockStyle.Bottom, Height = 5, Visible = false };

        private readonly Panel _topContainer = new Panel { Height = 140, Dock = DockStyle.Top, Visible = false };
        private readonly Splitter _topSplitter = new Splitter { Dock = DockStyle.Top, Height = 5, Visible = false };

        // Center Document Area using ZeroTabControl
        private readonly ZeroTabControl _documentTabControl = new ZeroTabControl
        {
            Dock = DockStyle.Fill,
            TabStyle = ZeroTabStyle.Card
        };

        // Auto-Hide Sidebars
        private readonly Panel _leftAutoHideBar = new Panel { Width = 28, Dock = DockStyle.Left, Visible = false };
        private readonly Panel _rightAutoHideBar = new Panel { Width = 28, Dock = DockStyle.Right, Visible = false };

        [Browsable(false)]
        public IReadOnlyList<ZeroDockPanel> Panels => _panels;

        [Browsable(false)]
        public ZeroTabControl DocumentTabs => _documentTabControl;

        public ZeroDockManager()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            BackColor = ZeroTheme.Colors.Background;

            // Assemble layout containers in correct WinForms docking hierarchy
            Controls.Add(_documentTabControl);

            Controls.Add(_bottomSplitter);
            Controls.Add(_bottomContainer);

            Controls.Add(_topSplitter);
            Controls.Add(_topContainer);

            Controls.Add(_rightSplitter);
            Controls.Add(_rightContainer);

            Controls.Add(_leftSplitter);
            Controls.Add(_leftContainer);

            Controls.Add(_leftAutoHideBar);
            Controls.Add(_rightAutoHideBar);

            _leftSplitter.BackColor = ZeroTheme.Colors.Border;
            _rightSplitter.BackColor = ZeroTheme.Colors.Border;
            _topSplitter.BackColor = ZeroTheme.Colors.Border;
            _bottomSplitter.BackColor = ZeroTheme.Colors.Border;

            ZeroTheme.ThemeChanged += (s, e) =>
            {
                BackColor = ZeroTheme.Colors.Background;
                _leftSplitter.BackColor = ZeroTheme.Colors.Border;
                _rightSplitter.BackColor = ZeroTheme.Colors.Border;
                _topSplitter.BackColor = ZeroTheme.Colors.Border;
                _bottomSplitter.BackColor = ZeroTheme.Colors.Border;
                Invalidate(true);
            };
        }

        public void AddPanel(ZeroDockPanel panel, ZeroDockPosition position)
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));
            panel.DockPosition = position;
            AddPanel(panel);
        }

        public void AddPanel(ZeroDockPanel panel)
        {
            if (panel == null || _panels.Contains(panel)) return;
            _panels.Add(panel);

            panel.CloseRequested += Panel_CloseRequested;
            panel.FloatRequested += Panel_FloatRequested;
            panel.PinStateChanged += Panel_PinStateChanged;

            ArrangePanel(panel);
        }

        public void RemovePanel(ZeroDockPanel panel)
        {
            if (panel == null || !_panels.Remove(panel)) return;

            panel.CloseRequested -= Panel_CloseRequested;
            panel.FloatRequested -= Panel_FloatRequested;
            panel.PinStateChanged -= Panel_PinStateChanged;

            if (panel.Parent != null)
            {
                panel.Parent.Controls.Remove(panel);
            }

            RebuildLayout();
        }

        private void ArrangePanel(ZeroDockPanel panel)
        {
            switch (panel.DockPosition)
            {
                case ZeroDockPosition.Left:
                    panel.Dock = DockStyle.Fill;
                    _leftContainer.Controls.Add(panel);
                    _leftContainer.Visible = true;
                    _leftSplitter.Visible = true;
                    break;

                case ZeroDockPosition.Right:
                    panel.Dock = DockStyle.Fill;
                    _rightContainer.Controls.Add(panel);
                    _rightContainer.Visible = true;
                    _rightSplitter.Visible = true;
                    break;

                case ZeroDockPosition.Bottom:
                    panel.Dock = DockStyle.Fill;
                    _bottomContainer.Controls.Add(panel);
                    _bottomContainer.Visible = true;
                    _bottomSplitter.Visible = true;
                    break;

                case ZeroDockPosition.Top:
                    panel.Dock = DockStyle.Fill;
                    _topContainer.Controls.Add(panel);
                    _topContainer.Visible = true;
                    _topSplitter.Visible = true;
                    break;

                case ZeroDockPosition.Document:
                    var page = new ZeroTabPage(panel.Title) { Closable = panel.Closable };
                    panel.Dock = DockStyle.Fill;
                    page.Controls.Add(panel);
                    _documentTabControl.TabPages.Add(page);
                    break;

                case ZeroDockPosition.Float:
                    FloatPanel(panel);
                    break;
            }
        }

        public void FloatPanel(ZeroDockPanel panel)
        {
            if (panel == null) return;
            if (panel.Parent != null)
            {
                panel.Parent.Controls.Remove(panel);
            }

            panel.DockPosition = ZeroDockPosition.Float;
            var floatWin = new ZeroFloatingWindow(this, panel);
            _floatingWindows.Add(floatWin);
            floatWin.FormClosed += (s, e) => _floatingWindows.Remove(floatWin);
            floatWin.Show(this);
            RebuildLayout();
        }

        private void Panel_FloatRequested(object? sender, EventArgs e)
        {
            if (sender is ZeroDockPanel panel)
            {
                FloatPanel(panel);
            }
        }

        private void Panel_CloseRequested(object? sender, EventArgs e)
        {
            if (sender is ZeroDockPanel panel)
            {
                RemovePanel(panel);
            }
        }

        private void Panel_PinStateChanged(object? sender, EventArgs e)
        {
            if (sender is ZeroDockPanel panel)
            {
                RebuildLayout();
            }
        }

        public void RebuildLayout()
        {
            _leftContainer.Visible = _leftContainer.Controls.Count > 0;
            _leftSplitter.Visible = _leftContainer.Visible;

            _rightContainer.Visible = _rightContainer.Controls.Count > 0;
            _rightSplitter.Visible = _rightContainer.Visible;

            _bottomContainer.Visible = _bottomContainer.Controls.Count > 0;
            _bottomSplitter.Visible = _bottomContainer.Visible;

            _topContainer.Visible = _topContainer.Controls.Count > 0;
            _topSplitter.Visible = _topContainer.Visible;

            Invalidate(true);
        }
    }
}
