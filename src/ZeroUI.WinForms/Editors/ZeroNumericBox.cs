using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// High-precision numeric stepper and spin box editor for industrial tolerances, setpoints,
    /// and quantities with unit prefixes/suffixes, acceleration on hold, and decimal formatting.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultEvent("ValueChanged")]
    [DefaultProperty("Value")]
    [Description("Precision numeric stepper and spin box editor with unit formatting")]
    public class ZeroNumericBox : Control
    {
        private decimal _value = 0m;
        private decimal _minValue = 0m;
        private decimal _maxValue = 1000000m;
        private decimal _step = 1m;
        private int _decimalPlaces = 0;
        private string _prefix = "";
        private string _suffix = "";
        private bool _thousandsSeparator = true;

        private readonly TextBox _innerBox;
        private Rectangle _upButtonRect;
        private Rectangle _downButtonRect;
        private bool _hoverUp = false;
        private bool _hoverDown = false;
        private bool _pressUp = false;
        private bool _pressDown = false;

        private readonly Timer _repeatTimer;
        private int _repeatCount = 0;
        private int _direction = 0; // 1 = up, -1 = down

        public event EventHandler? ValueChanged;

        public ZeroNumericBox()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);

            Size = new Size(180, 36);
            Font = new Font("Segoe UI", 9.5f);
            BackColor = Color.FromArgb(15, 23, 42); // Obsidian Dark

            _innerBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = Font,
                BackColor = BackColor,
                ForeColor = Color.White,
                TextAlign = HorizontalAlignment.Left
            };
            _innerBox.TextChanged += OnInnerBoxTextChanged;
            _innerBox.KeyDown += OnInnerBoxKeyDown;
            _innerBox.LostFocus += (s, e) => FormatText();
            _innerBox.GotFocus += (s, e) => Invalidate();
            Controls.Add(_innerBox);

            _repeatTimer = new Timer();
            _repeatTimer.Tick += OnRepeatTimerTick;

            ZeroTheme.ThemeChanged += (s, e) => UpdateTheme();
            UpdateTheme();
            FormatText();
        }

        [Category("Data")]
        [DefaultValue(typeof(decimal), "0")]
        public decimal Value
        {
            get => _value;
            set
            {
                decimal clamped = Math.Max(_minValue, Math.Min(_maxValue, value));
                if (_value != clamped)
                {
                    _value = clamped;
                    FormatText();
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        [Category("Data")]
        [DefaultValue(typeof(decimal), "0")]
        public decimal MinValue
        {
            get => _minValue;
            set
            {
                _minValue = value;
                if (_value < _minValue) Value = _minValue;
            }
        }

        [Category("Data")]
        [DefaultValue(typeof(decimal), "1000000")]
        public decimal MaxValue
        {
            get => _maxValue;
            set
            {
                _maxValue = value;
                if (_value > _maxValue) Value = _maxValue;
            }
        }

        [Category("Data")]
        [DefaultValue(typeof(decimal), "1")]
        public decimal Step
        {
            get => _step;
            set => _step = value > 0 ? value : 1m;
        }

        [Category("Appearance")]
        [DefaultValue(0)]
        public int DecimalPlaces
        {
            get => _decimalPlaces;
            set
            {
                _decimalPlaces = Math.Max(0, Math.Min(6, value));
                FormatText();
            }
        }

        [Category("Appearance")]
        [DefaultValue("")]
        public string Prefix
        {
            get => _prefix;
            set
            {
                _prefix = value ?? "";
                FormatText();
            }
        }

        [Category("Appearance")]
        [DefaultValue("")]
        public string Suffix
        {
            get => _suffix;
            set
            {
                _suffix = value ?? "";
                FormatText();
            }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        public bool ThousandsSeparator
        {
            get => _thousandsSeparator;
            set
            {
                _thousandsSeparator = value;
                FormatText();
            }
        }

        private void FormatText()
        {
            string numFormat = _thousandsSeparator ? $"N{_decimalPlaces}" : $"F{_decimalPlaces}";
            string formatted = _value.ToString(numFormat, CultureInfo.InvariantCulture);

            string full = "";
            if (!string.IsNullOrEmpty(_prefix)) full += _prefix + " ";
            full += formatted;
            if (!string.IsNullOrEmpty(_suffix)) full += " " + _suffix;

            if (_innerBox.Text != full)
            {
                _innerBox.Text = full;
                _innerBox.SelectionStart = _innerBox.Text.Length;
            }
        }

        private void OnInnerBoxTextChanged(object? sender, EventArgs e)
        {
            if (!_innerBox.Focused) return;

            string clean = _innerBox.Text;
            if (!string.IsNullOrEmpty(_prefix)) clean = clean.Replace(_prefix, "");
            if (!string.IsNullOrEmpty(_suffix)) clean = clean.Replace(_suffix, "");
            clean = clean.Trim().Replace(",", "");

            if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed))
            {
                decimal clamped = Math.Max(_minValue, Math.Min(_maxValue, parsed));
                if (_value != clamped)
                {
                    _value = clamped;
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        private void OnInnerBoxKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                StepUp();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                StepDown();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                FormatText();
                e.Handled = true;
            }
        }

        public void StepUp()
        {
            Value = Math.Min(_maxValue, _value + _step);
        }

        public void StepDown()
        {
            Value = Math.Max(_minValue, _value - _step);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta > 0) StepUp();
            else StepDown();
        }

        private void UpdateTheme()
        {
            var palette = ZeroTheme.Colors;
            BackColor = palette.Surface;
            _innerBox.BackColor = palette.Surface;
            _innerBox.ForeColor = palette.TextPrimary;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            int btnW = 22;
            int btnH = (Height - 4) / 2;

            _upButtonRect = new Rectangle(Width - btnW - 3, 2, btnW, btnH);
            _downButtonRect = new Rectangle(Width - btnW - 3, 2 + btnH, btnW, btnH);

            _innerBox.Location = new Point(10, (Height - _innerBox.Height) / 2);
            _innerBox.Width = Width - btnW - 16;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool overUp = _upButtonRect.Contains(e.Location);
            bool overDown = _downButtonRect.Contains(e.Location);

            if (_hoverUp != overUp || _hoverDown != overDown)
            {
                _hoverUp = overUp;
                _hoverDown = overDown;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverUp = false;
            _hoverDown = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_upButtonRect.Contains(e.Location))
            {
                _pressUp = true;
                _direction = 1;
                StepUp();
                StartRepeatTimer();
                Invalidate();
            }
            else if (_downButtonRect.Contains(e.Location))
            {
                _pressDown = true;
                _direction = -1;
                StepDown();
                StartRepeatTimer();
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _pressUp = false;
            _pressDown = false;
            _repeatTimer.Stop();
            Invalidate();
        }

        private void StartRepeatTimer()
        {
            _repeatCount = 0;
            _repeatTimer.Interval = 300; // Initial delay
            _repeatTimer.Start();
        }

        private void OnRepeatTimerTick(object? sender, EventArgs e)
        {
            _repeatCount++;
            if (_repeatCount > 3)
            {
                _repeatTimer.Interval = Math.Max(30, 100 - (_repeatCount * 5)); // Accelerate
            }

            if (_direction == 1) StepUp();
            else if (_direction == -1) StepDown();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var palette = ZeroTheme.Colors;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // 1. Box Background & Border
            using (var path = CreateRoundedRect(rect, 6))
            {
                using var brushBg = new SolidBrush(palette.Surface);
                g.FillPath(brushBg, path);

                Color borderCol = _innerBox.Focused ? palette.Primary : palette.Border;
                using var penBorder = new Pen(borderCol, _innerBox.Focused ? 1.5f : 1f);
                g.DrawPath(penBorder, path);
            }

            // 2. Up Button (▲)
            Color upBg = _pressUp ? Color.FromArgb(40, palette.Primary) : (_hoverUp ? Color.FromArgb(20, palette.Primary) : Color.Transparent);
            if (upBg != Color.Transparent)
            {
                using var brushUp = new SolidBrush(upBg);
                using var pathUp = CreateRoundedRect(_upButtonRect, 3);
                g.FillPath(brushUp, pathUp);
            }

            using (var brushUpArrow = new SolidBrush(_hoverUp ? palette.Primary : palette.TextSecondary))
            {
                int cx = _upButtonRect.X + (_upButtonRect.Width / 2);
                int cy = _upButtonRect.Y + (_upButtonRect.Height / 2);
                PointF[] pts = new[]
                {
                    new PointF(cx - 3.5f, cy + 1.5f),
                    new PointF(cx + 3.5f, cy + 1.5f),
                    new PointF(cx, cy - 2.5f)
                };
                g.FillPolygon(brushUpArrow, pts);
            }

            // 3. Down Button (▼)
            Color downBg = _pressDown ? Color.FromArgb(40, palette.Primary) : (_hoverDown ? Color.FromArgb(20, palette.Primary) : Color.Transparent);
            if (downBg != Color.Transparent)
            {
                using var brushDown = new SolidBrush(downBg);
                using var pathDown = CreateRoundedRect(_downButtonRect, 3);
                g.FillPath(brushDown, pathDown);
            }

            using (var brushDownArrow = new SolidBrush(_hoverDown ? palette.Primary : palette.TextSecondary))
            {
                int cx = _downButtonRect.X + (_downButtonRect.Width / 2);
                int cy = _downButtonRect.Y + (_downButtonRect.Height / 2);
                PointF[] pts = new[]
                {
                    new PointF(cx - 3.5f, cy - 1.5f),
                    new PointF(cx + 3.5f, cy - 1.5f),
                    new PointF(cx, cy + 2.5f)
                };
                g.FillPolygon(brushDownArrow, pts);
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
