using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// Modern, anti-aliased text input control for ZeroUI with built-in placeholder text,
    /// one-click clear button, password masking, character casing, and action icon slots.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultProperty("Text")]
    [DefaultEvent("TextChanged")]
    [Description("Modern text input control with clear button, placeholder, and action icons")]
    public class ZeroTextBox : Control
    {
        private readonly TextBox _innerBox;
        private string _placeholder = "";
        private bool _showClearButton = true;
        private string _leadingIcon = "";
        private string _trailingIcon = "";

        private bool _isHovered = false;
        private bool _isFocused = false;
        private bool _hoverOnClear = false;
        private bool _hoverOnTrailing = false;

        private Rectangle _clearButtonRect;
        private Rectangle _trailingIconRect;

        public event EventHandler? TrailingIconClick;
        public event EventHandler? ClearClicked;

        public ZeroTextBox()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(220, 36);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            _innerBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = Font,
                BackColor = ZeroTheme.Colors.Surface,
                ForeColor = ZeroTheme.Colors.TextPrimary
            };

            _innerBox.TextChanged += (s, e) =>
            {
                OnTextChanged(e);
                Invalidate();
            };
            _innerBox.GotFocus += (s, e) =>
            {
                _isFocused = true;
                Invalidate();
            };
            _innerBox.LostFocus += (s, e) =>
            {
                _isFocused = false;
                Invalidate();
            };
            _innerBox.KeyDown += (s, e) => OnKeyDown(e);
            _innerBox.KeyPress += (s, e) => OnKeyPress(e);
            _innerBox.KeyUp += (s, e) => OnKeyUp(e);

            Controls.Add(_innerBox);

            ZeroTheme.ThemeChanged += (s, e) => UpdateTheme();
            ZeroUIConfig.CornerStyleChanged += (s, e) => Invalidate();
            ZeroUIConfig.FontChanged += (s, e) =>
            {
                Font = ZeroUIConfig.DefaultFont;
                _innerBox.Font = ZeroUIConfig.DefaultFont;
                UpdateInnerBounds();
                Invalidate();
            };

            UpdateTheme();
            UpdateInnerBounds();
        }

        private void UpdateTheme()
        {
            var p = ZeroTheme.Colors;
            _innerBox.BackColor = ReadOnly ? p.HeaderBackground : p.Surface;
            _innerBox.ForeColor = Enabled ? p.TextPrimary : p.TextSecondary;
            Invalidate();
        }

        [Category("Appearance")]
        [DefaultValue("")]
#pragma warning disable CS8765, CS8764
        public override string Text
        {
            get => _innerBox.Text;
            set
            {
                if (_innerBox.Text != value)
                {
                    _innerBox.Text = value ?? "";
                    Invalidate();
                }
            }
        }
#pragma warning restore CS8765, CS8764

        [Category("Appearance")]
        [DefaultValue("")]
        public string PlaceholderText
        {
            get => _placeholder;
            set
            {
                _placeholder = value ?? "";
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool ShowClearButton
        {
            get => _showClearButton;
            set
            {
                _showClearButton = value;
                UpdateInnerBounds();
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue("")]
        public string LeadingIcon
        {
            get => _leadingIcon;
            set
            {
                _leadingIcon = value ?? "";
                UpdateInnerBounds();
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue("")]
        public string TrailingIcon
        {
            get => _trailingIcon;
            set
            {
                _trailingIcon = value ?? "";
                UpdateInnerBounds();
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool ReadOnly
        {
            get => _innerBox.ReadOnly;
            set
            {
                _innerBox.ReadOnly = value;
                UpdateTheme();
            }
        }

        [Category("Behavior")]
        [DefaultValue('\0')]
        public char PasswordChar
        {
            get => _innerBox.PasswordChar;
            set
            {
                _innerBox.PasswordChar = value;
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool UseSystemPasswordChar
        {
            get => _innerBox.UseSystemPasswordChar;
            set
            {
                _innerBox.UseSystemPasswordChar = value;
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(CharacterCasing.Normal)]
        public CharacterCasing CharacterCasing
        {
            get => _innerBox.CharacterCasing;
            set
            {
                _innerBox.CharacterCasing = value;
            }
        }

        [Category("Appearance")]
        [DefaultValue(HorizontalAlignment.Left)]
        public HorizontalAlignment TextAlign
        {
            get => _innerBox.TextAlign;
            set
            {
                _innerBox.TextAlign = value;
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(32767)]
        public int MaxLength
        {
            get => _innerBox.MaxLength;
            set => _innerBox.MaxLength = value;
        }

        public void SelectAll() => _innerBox.SelectAll();
        public void Clear() => _innerBox.Clear();

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateInnerBounds();
        }

        private void UpdateInnerBounds()
        {
            int leftPad = 12;
            if (!string.IsNullOrEmpty(_leadingIcon))
            {
                leftPad += 22;
            }

            int rightPad = 12;
            if (!string.IsNullOrEmpty(_trailingIcon))
            {
                rightPad += 22;
            }
            if (_showClearButton && !string.IsNullOrEmpty(_innerBox.Text) && !ReadOnly)
            {
                rightPad += 20;
            }

            int innerH = _innerBox.PreferredHeight;
            int innerY = Math.Max(2, (Height - innerH) / 2);
            int innerW = Math.Max(10, Width - leftPad - rightPad);

            _innerBox.SetBounds(leftPad, innerY, innerW, innerH);

            // Button rects
            int iconY = (Height - 16) / 2;
            int curRight = Width - 10;

            if (!string.IsNullOrEmpty(_trailingIcon))
            {
                _trailingIconRect = new Rectangle(curRight - 16, iconY, 16, 16);
                curRight -= 22;
            }
            else
            {
                _trailingIconRect = Rectangle.Empty;
            }

            if (_showClearButton && !string.IsNullOrEmpty(_innerBox.Text) && !ReadOnly)
            {
                _clearButtonRect = new Rectangle(curRight - 16, iconY, 16, 16);
            }
            else
            {
                _clearButtonRect = Rectangle.Empty;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (_clearButtonRect.Contains(e.Location) && !ReadOnly && !string.IsNullOrEmpty(_innerBox.Text))
            {
                _innerBox.Clear();
                _innerBox.Focus();
                ClearClicked?.Invoke(this, EventArgs.Empty);
                UpdateInnerBounds();
                Invalidate();
                return;
            }

            if (_trailingIconRect.Contains(e.Location))
            {
                TrailingIconClick?.Invoke(this, EventArgs.Empty);
                return;
            }

            _innerBox.Focus();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool hoverClear = _clearButtonRect.Contains(e.Location);
            bool hoverTrail = _trailingIconRect.Contains(e.Location);

            if (hoverClear != _hoverOnClear || hoverTrail != _hoverOnTrailing)
            {
                _hoverOnClear = hoverClear;
                _hoverOnTrailing = hoverTrail;
                Cursor = (hoverClear || hoverTrail) ? Cursors.Hand : Cursors.IBeam;
                Invalidate();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            _hoverOnClear = false;
            _hoverOnTrailing = false;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var p = ZeroTheme.Colors;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int radius = ZeroUIConfig.RoundedCorners ? ZeroUIConfig.DefaultBorderRadius : 0;

            Color bgColor = ReadOnly ? p.HeaderBackground : p.Surface;
            Color borderColor = _isFocused ? p.Primary : (_isHovered ? p.TextSecondary : p.Border);
            float borderWidth = _isFocused ? 1.75f : 1.0f;

            // 1. Fill & Border
            using (var path = CreateRoundedRectangle(rect, radius))
            {
                using (var brush = new SolidBrush(bgColor))
                {
                    g.FillPath(brush, path);
                }
                using (var pen = new Pen(borderColor, borderWidth))
                {
                    g.DrawPath(pen, path);
                }
            }

            // 2. Draw Leading Icon
            if (!string.IsNullOrEmpty(_leadingIcon))
            {
                Rectangle leadRect = new Rectangle(10, 0, 20, Height);
                TextRenderer.DrawText(g, _leadingIcon, Font, leadRect, p.TextSecondary,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            }

            // 3. Draw Placeholder if empty
            if (string.IsNullOrEmpty(_innerBox.Text) && !string.IsNullOrEmpty(_placeholder) && !_isFocused)
            {
                Rectangle placeRect = new Rectangle(_innerBox.Left + 2, 0, _innerBox.Width, Height);
                TextRenderer.DrawText(g, _placeholder, Font, placeRect, p.TextSecondary,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }

            // 4. Draw Clear Button ('✕')
            if (!_clearButtonRect.IsEmpty)
            {
                Color clearColor = _hoverOnClear ? p.Primary : p.TextSecondary;
                using (var pen = new Pen(clearColor, 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    int cx = _clearButtonRect.X + _clearButtonRect.Width / 2;
                    int cy = _clearButtonRect.Y + _clearButtonRect.Height / 2;
                    int sz = 4;
                    g.DrawLine(pen, cx - sz, cy - sz, cx + sz, cy + sz);
                    g.DrawLine(pen, cx - sz, cy + sz, cx + sz, cy - sz);
                }
            }

            // 5. Draw Trailing Icon
            if (!_trailingIconRect.IsEmpty && !string.IsNullOrEmpty(_trailingIcon))
            {
                Color trailColor = _hoverOnTrailing ? p.Primary : p.TextSecondary;
                TextRenderer.DrawText(g, _trailingIcon, Font, _trailingIconRect, trailColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            }
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
