using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{

    /// <summary>
    /// Modern search input with placeholder text, clear button, and debounced text change events.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultEvent("DebouncedTextChanged")]
    [DefaultProperty("PlaceholderText")]
    [Description("Modern search box with debounced input and clear button")]
    public class ZeroSearchBox : Control
    {

        private readonly TextBox _textBox;
        private readonly Timer _debounceTimer;
        private string _placeholder = "🔍 Search...";
        private int _debounceMs = 200;

        private bool _isFocused = false;

        public event EventHandler<string>? DebouncedTextChanged;

        public ZeroSearchBox()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(240, 34);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            _textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = Font,
                Location = new Point(10, 7),
                Width = Width - 36,
                BackColor = Color.White
            };

            _textBox.TextChanged += TextBox_TextChanged;
            _textBox.GotFocus += (s, e) => { _isFocused = true; Invalidate(); };
            _textBox.LostFocus += (s, e) => { _isFocused = false; Invalidate(); };
            _textBox.KeyDown += TextBox_KeyDown;

            Controls.Add(_textBox);

            ZeroUIConfig.ConfigChanged += (s, e) =>
            {
                Font = ZeroUIConfig.DefaultFont;
                _textBox.Font = ZeroUIConfig.DefaultFont;
                Invalidate();
            };

            _debounceTimer = new Timer { Interval = _debounceMs };
            _debounceTimer.Tick += (s, e) =>
            {
                _debounceTimer.Stop();
                DebouncedTextChanged?.Invoke(this, _textBox.Text);
            };
        }

        [Category("Appearance")]
        public string PlaceholderText
        {
            get => _placeholder;
            set { _placeholder = value; Invalidate(); }
        }

        [Category("Behavior")]
        [DefaultValue(200)]
        public int DebounceIntervalMs
        {
            get => _debounceMs;
            set { _debounceMs = Math.Max(50, value); _debounceTimer.Interval = _debounceMs; }
        }

        [Browsable(false)]
#if NET8_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.AllowNull]
#endif
        public override string Text
        {
            get => _textBox.Text;
            set => _textBox.Text = value ?? string.Empty;
        }





        private void TextBox_TextChanged(object? sender, EventArgs e)
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
            Invalidate();
        }

        private void TextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && !string.IsNullOrEmpty(_textBox.Text))
            {
                _textBox.Clear();
                e.Handled = true;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_textBox != null)
            {
                _textBox.Location = new Point(10, (Height - _textBox.PreferredHeight) / 2);
                _textBox.Width = Width - 36;
            }
        }


        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // Click Clear Button
            if (!string.IsNullOrEmpty(_textBox.Text) && e.X >= Width - 28 && e.X <= Width - 8)
            {
                _textBox.Clear();
                _textBox.Focus();
            }
            else
            {
                _textBox.Focus();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 1. Fill parent background to eliminate black corner clipping artifacts
            Color parentBg = ZeroUIConfig.GetParentBackground(this, ZeroTheme.Colors.Background);
            using (var brushParent = new SolidBrush(parentBg))
            {
                g.FillRectangle(brushParent, ClientRectangle);
            }

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int effRadius = ZeroUIConfig.GetEffectiveRadius(6);

            // 2. Background and Border
            Color borderColor = _isFocused ? Color.FromArgb(79, 70, 229) : Color.FromArgb(209, 213, 219);
            float borderWidth = _isFocused ? 1.5f : 1f;

            using (var path = ZeroUIConfig.CreateRoundedRectangle(rect, effRadius))
            {
                using var bgBrush = new SolidBrush(ZeroTheme.Colors.Surface);
                g.FillPath(bgBrush, path);

                using var borderPen = new Pen(borderColor, borderWidth);
                g.DrawPath(borderPen, path);
            }

            // 2. Placeholder Text (when empty and not typing)
            if (string.IsNullOrEmpty(_textBox.Text) && !_isFocused)
            {
                Rectangle phRect = new Rectangle(12, 0, Width - 36, Height);
                TextRenderer.DrawText(
                    g,
                    _placeholder,
                    Font,
                    phRect,
                    Color.FromArgb(156, 163, 175),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            }

            // 3. Clear Button (✕) when text exists
            if (!string.IsNullOrEmpty(_textBox.Text))
            {
                Rectangle clearRect = new Rectangle(Width - 28, (Height - 18) / 2, 18, 18);
                using var clearBg = new SolidBrush(Color.FromArgb(229, 231, 235));
                g.FillEllipse(clearBg, clearRect);

                TextRenderer.DrawText(
                    g,
                    "✕",
                    new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    clearRect,
                    Color.FromArgb(107, 114, 128),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            }
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius) =>
            ZeroUIConfig.CreateRoundedRectangle(rect, radius);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _debounceTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
