using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.Editors;
using ZeroUI.Core.Theme;
using ZeroUI.WinForms.Base;
using ZeroUI.WinForms.Icons;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// Modern, anti-aliased text input control for ZeroUI with built-in placeholder text,
    /// one-click clear button, password masking, character casing, and action icon slots.
    /// Standardized on ZeroEditorBase for unified theme, focus rings, and data binding.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultProperty("Text")]
    [DefaultEvent("TextChanged")]
    [Description("Modern text input control with clear button, placeholder, and action icons")]
    [ToolboxBitmap(typeof(ZeroIcons), "TextEdit.bmp")]
    public class TextEdit : ZeroEditorBase<string>
    {
        private readonly TextBox _innerBox;
        private string _leadingIcon = "";
        private string _trailingIcon = "";
        private bool _hoverOnTrailing = false;
        private Rectangle _trailingIconRect;

        public event EventHandler? TrailingIconClick;
        public event EventHandler? ClearClicked;

        [Category("Data")]
        [Description("The string value of the text editor.")]
        public override string Value
        {
            get => _innerBox?.Text ?? base.Value ?? string.Empty;
            set
            {
                string newVal = value ?? string.Empty;
                if (_innerBox != null && _innerBox.Text != newVal)
                {
                    _innerBox.Text = newVal;
                }
                base.Value = newVal;
            }
        }

        [Category("Appearance")]
        [DefaultValue("")]
#pragma warning disable CS8765, CS8764
        public override string Text
        {
            get => Value;
            set => Value = value;
        }
#pragma warning restore CS8765, CS8764

        [Category("Appearance")]
        [DefaultValue("")]
        public string PlaceholderText
        {
            get => Placeholder;
            set => Placeholder = value;
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
        public override bool ReadOnly
        {
            get => base.ReadOnly;
            set
            {
                base.ReadOnly = value;
                if (_innerBox != null)
                {
                    _innerBox.ReadOnly = value;
                    UpdateInnerTheme();
                }
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
            set => _innerBox.CharacterCasing = value;
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

        public TextEdit()
        {
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            ShowClearButton = true;

            _innerBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = Font,
                BackColor = CurrentPalette.Surface,
                ForeColor = CurrentPalette.TextPrimary
            };

            _innerBox.TextChanged += (s, e) =>
            {
                IsModified = true;
                OnTextChanged(e);
                OnValueChanged();
                UpdateInnerBounds();
                Invalidate();
            };
            _innerBox.GotFocus += (s, e) =>
            {
                OnGotFocus(e);
                Invalidate();
            };
            _innerBox.LostFocus += (s, e) =>
            {
                OnLostFocus(e);
                Invalidate();
            };
            _innerBox.KeyDown += (s, e) => OnKeyDown(e);
            _innerBox.KeyPress += (s, e) => OnKeyPress(e);
            _innerBox.KeyUp += (s, e) => OnKeyUp(e);

            Controls.Add(_innerBox);
            Size = new Size(220, 36);

            ZeroUIConfig.CornerStyleChanged += (s, e) => Invalidate();
            ZeroUIConfig.FontChanged += (s, e) =>
            {
                Font = ZeroUIConfig.DefaultFont;
                _innerBox.Font = ZeroUIConfig.DefaultFont;
                UpdateInnerBounds();
                Invalidate();
            };

            UpdateInnerTheme();
            UpdateInnerBounds();
        }

        protected override void OnThemeChanged(ZeroSkin skin)
        {
            base.OnThemeChanged(skin);
            BackColor = Color.Transparent;
            UpdateInnerTheme();
            Invalidate();
        }

        private void UpdateInnerTheme()
        {
            if (_innerBox == null) return;
            var p = CurrentPalette;
            _innerBox.BackColor = ReadOnly ? p.HeaderBackground : p.Surface;
            _innerBox.ForeColor = Enabled ? p.TextPrimary : p.TextSecondary;
        }

        public void SelectAll() => _innerBox.SelectAll();

        public override void Clear()
        {
            _innerBox.Clear();
            base.Clear();
        }

        public override void Reset()
        {
            _innerBox.Clear();
            base.Reset();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateInnerBounds();
        }

        private void UpdateInnerBounds()
        {
            if (_innerBox == null) return;

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
            if (ShowClearButton && !string.IsNullOrEmpty(_innerBox.Text) && !ReadOnly)
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

            if (ShowClearButton && !string.IsNullOrEmpty(_innerBox.Text) && !ReadOnly)
            {
                ClearButtonRect = new Rectangle(curRight - 16, iconY, 16, 16);
            }
            else
            {
                ClearButtonRect = Rectangle.Empty;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (ClearButtonRect.Contains(e.Location) && !ReadOnly && !string.IsNullOrEmpty(_innerBox.Text))
            {
                Clear();
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
            bool hoverClear = ClearButtonRect.Contains(e.Location);
            bool hoverTrail = _trailingIconRect.Contains(e.Location);

            if (hoverClear != HoverOnClear || hoverTrail != _hoverOnTrailing)
            {
                HoverOnClear = hoverClear;
                _hoverOnTrailing = hoverTrail;
                Cursor = (hoverClear || hoverTrail) ? Cursors.Hand : Cursors.IBeam;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var p = CurrentPalette;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int radius = ZeroUIConfig.RoundedCorners ? ZeroUIConfig.DefaultBorderRadius : 0;

            Color bgColor = ReadOnly ? p.HeaderBackground : p.Surface;
            Color borderColor = HasError ? p.Danger : (IsEditorFocused ? p.Primary : (IsHovered ? p.PrimaryHover : p.Border));
            float borderWidth = IsEditorFocused ? 1.75f : 1.0f;

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
            if (string.IsNullOrEmpty(_innerBox.Text) && !string.IsNullOrEmpty(Placeholder) && !IsEditorFocused)
            {
                Rectangle placeRect = new Rectangle(_innerBox.Left + 2, 0, _innerBox.Width, Height);
                TextRenderer.DrawText(g, Placeholder, Font, placeRect, p.TextSecondary,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }

            // 4. Draw Clear Button ('✕')
            if (!ClearButtonRect.IsEmpty)
            {
                Color clearColor = HoverOnClear ? p.Primary : p.TextSecondary;
                using (var pen = new Pen(clearColor, 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    int cx = ClearButtonRect.X + ClearButtonRect.Width / 2;
                    int cy = ClearButtonRect.Y + ClearButtonRect.Height / 2;
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

    /// <summary>
    /// Legacy alias for TextEdit.
    /// Preserved for 100% backward compatibility.
    /// </summary>
    [Obsolete("ZeroTextBox is deprecated. Please use TextEdit instead.")]
    [ToolboxItem(false)]
    public class ZeroTextBox : TextEdit
    {
    }
}
