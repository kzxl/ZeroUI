using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// Modern anti-aliased multi-line text editor (Memo / Text Area) for ZeroUI.
    /// Provides smooth scrolling, word wrap, placeholder, character counter, and theme synchronization.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultProperty("Text")]
    [DefaultEvent("TextChanged")]
    [Description("Modern anti-aliased multi-line text editor")]
    public class ZeroMemoEdit : Control
    {
        private readonly TextBox _innerBox;
        private string _placeholderText = "";
        private bool _isFocused = false;
        private bool _isHovered = false;
        private bool _showCharacterCount = false;

        public ZeroMemoEdit()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            _innerBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = true,
                BorderStyle = BorderStyle.None,
                Font = Font,
                BackColor = ZeroTheme.Colors.Surface,
                ForeColor = ZeroTheme.Colors.TextPrimary,
                AcceptsReturn = true,
                AcceptsTab = false
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
            Size = new Size(320, 100);

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
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string[] Lines
        {
            get => _innerBox.Lines;
            set => _innerBox.Lines = value;
        }

        [Category("Appearance")]
        [DefaultValue("")]
        public string PlaceholderText
        {
            get => _placeholderText;
            set
            {
                _placeholderText = value ?? "";
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
        [DefaultValue(true)]
        public bool WordWrap
        {
            get => _innerBox.WordWrap;
            set => _innerBox.WordWrap = value;
        }

        [Category("Behavior")]
        [DefaultValue(ScrollBars.Vertical)]
        public ScrollBars ScrollBars
        {
            get => _innerBox.ScrollBars;
            set => _innerBox.ScrollBars = value;
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool AcceptsReturn
        {
            get => _innerBox.AcceptsReturn;
            set => _innerBox.AcceptsReturn = value;
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool AcceptsTab
        {
            get => _innerBox.AcceptsTab;
            set => _innerBox.AcceptsTab = value;
        }

        [Category("Behavior")]
        [DefaultValue(32767)]
        public int MaxLength
        {
            get => _innerBox.MaxLength;
            set
            {
                _innerBox.MaxLength = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(false)]
        public bool ShowCharacterCount
        {
            get => _showCharacterCount;
            set
            {
                _showCharacterCount = value;
                UpdateInnerBounds();
                Invalidate();
            }
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
            if (_innerBox == null) return;

            int padX = 10;
            int padTop = 8;
            int bottomReserve = _showCharacterCount ? 20 : 8;

            int boxW = Math.Max(20, Width - (padX * 2));
            int boxH = Math.Max(20, Height - padTop - bottomReserve);

            _innerBox.SetBounds(padX, padTop, boxW, boxH);
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
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var palette = ZeroTheme.Colors;
            bool isDark = ZeroTheme.IsDark;

            int effRadius = ZeroUIConfig.GetEffectiveRadius(6);
            var borderRect = new Rectangle(1, 1, Width - 2, Height - 2);

            Color borderColor;
            Color bgColor;

            if (!Enabled)
            {
                borderColor = isDark ? Color.FromArgb(55, 60, 70) : Color.FromArgb(215, 220, 228);
                bgColor = isDark ? Color.FromArgb(28, 33, 43) : Color.FromArgb(243, 246, 249);
            }
            else if (_isFocused)
            {
                borderColor = palette.Primary;
                bgColor = ReadOnly ? palette.HeaderBackground : palette.Surface;
            }
            else if (_isHovered)
            {
                borderColor = palette.Primary;
                bgColor = ReadOnly ? palette.HeaderBackground : palette.Surface;
            }
            else
            {
                borderColor = palette.Border;
                bgColor = ReadOnly ? palette.HeaderBackground : palette.Surface;
            }

            // Fill & Border
            using (var path = ZeroUIConfig.CreateRoundedRectangle(borderRect, effRadius))
            {
                using (var bgBrush = new SolidBrush(bgColor))
                {
                    g.FillPath(bgBrush, path);
                }

                float penWidth = _isFocused ? 1.5f : 1.0f;
                using (var pen = new Pen(borderColor, penWidth))
                {
                    g.DrawPath(pen, path);
                }
            }

            // Placeholder Text
            if (string.IsNullOrEmpty(_innerBox.Text) && !string.IsNullOrEmpty(_placeholderText) && !_isFocused)
            {
                Color phColor = isDark ? Color.FromArgb(100, 105, 115) : Color.FromArgb(160, 165, 175);
                var phRect = new Rectangle(12, 8, Width - 24, Height - 16);
                var flags = TextFormatFlags.Top | TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix;
                TextRenderer.DrawText(g, _placeholderText, Font, phRect, phColor, flags);
            }

            // Character count in bottom right
            if (_showCharacterCount)
            {
                string countText = MaxLength < 32767
                    ? $"{_innerBox.TextLength} / {MaxLength}"
                    : $"{_innerBox.TextLength}";

                Color countColor = isDark ? Color.FromArgb(120, 125, 135) : Color.FromArgb(150, 155, 165);
                using var countFont = new Font("Segoe UI", 7.5f);
                var countRect = new Rectangle(Width - 110, Height - 18, 100, 14);
                var flags = TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix;
                TextRenderer.DrawText(g, countText, countFont, countRect, countColor, flags);
            }
        }
    }
}
