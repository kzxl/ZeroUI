using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.Rendering;
using ZeroUI.WinForms.Native;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Overlays
{
    /// <summary>
    /// Modern right-docked slide-out drawer panel for Master-Detail inspection and side forms.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Overlays")]
    [DefaultProperty("Title")]
    [Description("Right-docked slide-out drawer panel for Master-Detail inspection")]
    public class ZeroDrawer : Panel
    {

        private string _title = "Detail Inspection";
        private string? _subtitle;
        private int _drawerWidth = 420;
        private bool _isOpen = false;
        private readonly Panel _contentPanel;
        private Rectangle _closeRect;
        private bool _isCloseHovered = false;

        private IDisposable? _animSub;
        private int _currentWidth = 0;
        private int _targetWidth = 0;

        public event EventHandler? Opened;
        public event EventHandler? Closed;

        public ZeroDrawer()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            Dock = DockStyle.Right;
            Width = 0;
            Visible = false;
            BackColor = Color.White;

            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(16)
            };

            var headerSpacer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.Transparent
            };

            Controls.Add(_contentPanel);
            Controls.Add(headerSpacer);

            ZeroTheme.ThemeChanged += (s, e) =>
            {
                BackColor = ZeroTheme.Colors.Surface;
                Invalidate();
            };
        }

        [Category("Appearance")]
        [DefaultValue("Detail Inspection")]
        public string Title
        {
            get => _title;
            set { _title = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(null)]
        public string? Subtitle
        {
            get => _subtitle;
            set { _subtitle = value; Invalidate(); }
        }

        [Category("Layout")]
        [DefaultValue(420)]
        public int DrawerWidth
        {
            get => _drawerWidth;
            set { _drawerWidth = Math.Max(200, value); }
        }

        [Browsable(false)]
        public bool IsOpen => _isOpen;

        [Browsable(false)]
        public Panel ContentPanel => _contentPanel;

        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;
            Visible = true;
            BringToFront();
            _targetWidth = _drawerWidth;
            StartAnimation();
            Opened?.Invoke(this, EventArgs.Empty);
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            _targetWidth = 0;
            StartAnimation();
        }

        public void Toggle()
        {
            if (_isOpen) Close();
            else Open();
        }

        private void StartAnimation()
        {
            _animSub ??= ZeroAnimationClock.Subscribe(OnAnimationStep);
        }

        private void StopAnimation()
        {
            _animSub?.Dispose();
            _animSub = null;
        }

        private void OnAnimationStep(double deltaSeconds, long frameCount)
        {
            int step = Math.Max(30, Math.Abs(_targetWidth - _currentWidth) / 3);

            if (_currentWidth < _targetWidth)
            {
                _currentWidth += step;
                if (_currentWidth >= _targetWidth)
                {
                    _currentWidth = _targetWidth;
                    StopAnimation();
                }
            }
            else if (_currentWidth > _targetWidth)
            {
                _currentWidth -= step;
                if (_currentWidth <= _targetWidth)
                {
                    _currentWidth = _targetWidth;
                    StopAnimation();
                    Visible = false;
                    Closed?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                StopAnimation();
            }

            if (Parent != null)
            {
                Width = _currentWidth;
                Left = Parent.ClientSize.Width - _currentWidth;
                Height = Parent.ClientSize.Height;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var theme = ZeroTheme.Colors;

            // 1. Draw Left Border & Background
            using (var bgBrush = new SolidBrush(theme.Surface))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            using (var borderPen = new Pen(theme.Border, 1f))
            {
                g.DrawLine(borderPen, 0, 0, 0, Height);
                g.DrawLine(borderPen, 0, 56, Width, 56); // Header divider
            }

            // 2. Draw Title & Subtitle
            int textLeft = 16;
            using var titleFont = new Font("Segoe UI", 11f, FontStyle.Bold);
            Rectangle titleRect = new Rectangle(textLeft, string.IsNullOrEmpty(_subtitle) ? 16 : 8, Width - textLeft - 44, 22);
            TextRenderer.DrawText(g, _title, titleFont, titleRect, theme.TextPrimary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            if (!string.IsNullOrEmpty(_subtitle))
            {
                using var subFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
                Rectangle subRect = new Rectangle(textLeft, titleRect.Bottom + 1, Width - textLeft - 44, 18);
                TextRenderer.DrawText(g, _subtitle, subFont, subRect, theme.TextSecondary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            // 3. Draw Close Button (✕)
            _closeRect = new Rectangle(Width - 36, 16, 24, 24);
            Color closeBg = _isCloseHovered ? theme.Hover : Color.Transparent;
            if (_isCloseHovered)
            {
                using var bPath = CreateRoundedRectangle(_closeRect, 4);
                using var bBrush = new SolidBrush(closeBg);
                g.FillPath(bBrush, bPath);
            }

            TextRenderer.DrawText(
                g,
                "✕",
                new Font("Segoe UI", 9.5f, FontStyle.Bold),
                _closeRect,
                _isCloseHovered ? theme.TextPrimary : theme.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_closeRect.IsEmpty && _closeRect.Contains(e.Location))
            {
                if (!_isCloseHovered)
                {
                    _isCloseHovered = true;
                    Cursor = Cursors.Hand;
                    Invalidate(_closeRect);
                }
            }
            else if (_isCloseHovered)
            {
                _isCloseHovered = false;
                Cursor = Cursors.Default;
                Invalidate(_closeRect);
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_isCloseHovered)
            {
                _isCloseHovered = false;
                Cursor = Cursors.Default;
                Invalidate(_closeRect);
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button == MouseButtons.Left && !_closeRect.IsEmpty && _closeRect.Contains(e.Location))
            {
                Close();
            }
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius) =>
            ZeroUIConfig.CreateRoundedRectangle(rect, radius);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopAnimation();
            }
            base.Dispose(disposing);
        }
    }
}
