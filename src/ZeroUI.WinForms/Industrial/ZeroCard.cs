using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZeroUI.WinForms.Industrial
{

    /// <summary>
    /// Modern container card for ZeroUI with rounded corners, optional Step Badge, Title, Subtitle, and Action Link.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Industrial & SCADA")]
    [DefaultProperty("Title")]
    [Description("Modern container card with rounded corners, optional Step Badge, and Title")]
    public class ZeroCard : Panel
    {

        private int? _stepNumber = 1;
        private Color _badgeColor = Color.FromArgb(79, 70, 229); // Indigo Accent
        private string _title = "Card Title";
        private string? _subtitle;
        private string? _actionText;
        private Color _actionColor = Color.FromArgb(22, 119, 255);
        private int _borderRadius = 8;
        private Color _borderColor = Color.FromArgb(229, 231, 235);
        private int _headerHeight = 44;

        private readonly Panel _contentPanel;
        private Rectangle _actionRect;
        private bool _isActionHovered = false;

        public event EventHandler? ActionClicked;

        public ZeroCard()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);

            BackColor = Color.White;
            Padding = new Padding(12);

            _contentPanel = new Panel
            {
                BackColor = Color.Transparent,
                Location = new Point(12, _headerHeight),
                Size = new Size(Width - 24, Height - _headerHeight - 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_contentPanel);
        }

        [Category("Appearance")]
        [DefaultValue(1)]
        public int? StepNumber
        {
            get => _stepNumber;
            set { _stepNumber = value; Invalidate(); }
        }

        [Category("Appearance")]
        public Color BadgeColor
        {
            get => _badgeColor;
            set { _badgeColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue("Card Title")]
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
            set
            {
                _subtitle = value;
                _headerHeight = string.IsNullOrEmpty(value) ? 44 : 58;
                UpdateContentLayout();
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(null)]
        public string? ActionText
        {
            get => _actionText;
            set { _actionText = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(8)]
        public int BorderRadius
        {
            get => _borderRadius;
            set { _borderRadius = Math.Max(0, value); Invalidate(); }
        }

        [Category("Appearance")]
        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        [Browsable(false)]
        public Panel ContentPanel => _contentPanel;

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            UpdateContentLayout();
        }

        private void UpdateContentLayout()
        {
            if (_contentPanel != null)
            {
                int bottomPadding = string.IsNullOrEmpty(_actionText) ? 12 : 30;
                _contentPanel.Location = new Point(12, _headerHeight);
                _contentPanel.Size = new Size(Math.Max(10, Width - 24), Math.Max(10, Height - _headerHeight - bottomPadding));
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // 1. Draw Card Background & Rounded Border
            using (var path = CreateRoundedRectangle(rect, _borderRadius))
            {
                using var bgBrush = new SolidBrush(BackColor);
                g.FillPath(bgBrush, path);

                using var borderPen = new Pen(_borderColor, 1f);
                g.DrawPath(borderPen, path);
            }

            // 2. Draw Header Area
            int currentX = 14;
            int headerCenterY = string.IsNullOrEmpty(_subtitle) ? _headerHeight / 2 : 20;

            // Draw Step Badge
            if (_stepNumber.HasValue)
            {
                int badgeSize = 22;
                Rectangle badgeRect = new Rectangle(currentX, headerCenterY - (badgeSize / 2), badgeSize, badgeSize);
                using (var badgePath = CreateRoundedRectangle(badgeRect, 4))
                {
                    using var badgeBrush = new SolidBrush(_badgeColor);
                    g.FillPath(badgeBrush, badgePath);
                }

                TextRenderer.DrawText(
                    g,
                    _stepNumber.Value.ToString(),
                    new Font("Segoe UI", 9f, FontStyle.Bold),
                    badgeRect,
                    Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                currentX += badgeSize + 8;
            }

            // Draw Title
            using var titleFont = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            Size titleSize = TextRenderer.MeasureText(g, _title, titleFont);
            Rectangle titleRect = new Rectangle(currentX, headerCenterY - (titleSize.Height / 2), Width - currentX - 16, titleSize.Height);
            TextRenderer.DrawText(g, _title, titleFont, titleRect, Color.FromArgb(17, 24, 39), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            // Draw Subtitle (if available)
            if (!string.IsNullOrEmpty(_subtitle))
            {
                using var subFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
                Rectangle subRect = new Rectangle(currentX, titleRect.Bottom + 2, Width - currentX - 16, 20);
                TextRenderer.DrawText(g, _subtitle, subFont, subRect, Color.FromArgb(75, 85, 99), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            // 3. Draw Action Link (Bottom-left or footer)
            if (!string.IsNullOrEmpty(_actionText))
            {
                using var actionFont = new Font("Segoe UI", 9f, _isActionHovered ? FontStyle.Underline : FontStyle.Regular);
                Size actSize = TextRenderer.MeasureText(g, _actionText, actionFont);
                _actionRect = new Rectangle(14, Height - actSize.Height - 8, actSize.Width + 4, actSize.Height + 2);
                TextRenderer.DrawText(g, _actionText, actionFont, _actionRect, _actionColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
            else
            {
                _actionRect = Rectangle.Empty;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_actionRect.IsEmpty && _actionRect.Contains(e.Location))
            {
                if (!_isActionHovered)
                {
                    _isActionHovered = true;
                    Cursor = Cursors.Hand;
                    Invalidate(_actionRect);
                }
            }
            else if (_isActionHovered)
            {
                _isActionHovered = false;
                Cursor = Cursors.Default;
                Invalidate(_actionRect);
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_isActionHovered)
            {
                _isActionHovered = false;
                Cursor = Cursors.Default;
                Invalidate(_actionRect);
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button == MouseButtons.Left && !_actionRect.IsEmpty && _actionRect.Contains(e.Location))
            {
                ActionClicked?.Invoke(this, EventArgs.Empty);
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
