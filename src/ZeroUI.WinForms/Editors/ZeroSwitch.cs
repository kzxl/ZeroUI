using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Native;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// Smooth animated toggle switch control for ZeroUI with keyboard interaction and custom state labels.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultProperty("Checked")]
    [DefaultEvent("CheckedChanged")]
    [Category("ZeroUI - Editors")]
    [Description("Smooth sliding animated toggle switch")]
    public class ZeroSwitch : Control
    {
        private bool _checked = false;
        private string? _checkedText = "ON";
        private string? _uncheckedText = "OFF";

        private Color _checkedColor = Color.FromArgb(79, 70, 229);     // ZeroUI Indigo Accent
        private Color _uncheckedColor = Color.FromArgb(0, 0, 0, 65);   // Neutral Slate Track
        private Color _thumbColor = Color.White;

        private float _thumbPosition = 0f; // 0.0 (left) to 1.0 (right)
        private readonly Timer _animationTimer;

        public event EventHandler? CheckedChanged;

        public ZeroSwitch()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(52, 26);
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);

            _animationTimer = new Timer { Interval = 16 }; // ~60 FPS
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    if (!ZeroDesignHelper.IsInDesignMode(this))
                    {
                        _animationTimer.Start();
                    }
                    else
                    {
                        _thumbPosition = value ? 1f : 0f;
                        Invalidate();
                    }
                    CheckedChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }


        [Category("Appearance")]
        [DefaultValue("ON")]
        public string? CheckedText
        {
            get => _checkedText;
            set { _checkedText = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue("OFF")]
        public string? UncheckedText
        {
            get => _uncheckedText;
            set { _uncheckedText = value; Invalidate(); }
        }


        [Category("Appearance")]
        public Color CheckedColor
        {
            get => _checkedColor;
            set { _checkedColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        public Color UncheckedColor
        {
            get => _uncheckedColor;
            set { _uncheckedColor = value; Invalidate(); }
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            float target = _checked ? 1.0f : 0.0f;
            float step = 0.18f;

            if (Math.Abs(_thumbPosition - target) <= step)
            {
                _thumbPosition = target;
                _animationTimer.Stop();
            }
            else
            {
                _thumbPosition += (_thumbPosition < target) ? step : -step;
            }
            Invalidate();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Focus();
            Checked = !Checked;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                Checked = !Checked;
                e.Handled = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle trackRect = new Rectangle(0, 0, Width - 1, Height - 1);
            int radius = Height / 2;

            // 1. Interpolate Track Color
            Color currentTrackColor = InterpolateColor(_uncheckedColor, _checkedColor, _thumbPosition);

            using (var trackPath = CreatePillPath(trackRect, radius))
            {
                using var trackBrush = new SolidBrush(currentTrackColor);
                g.FillPath(trackBrush, trackPath);

                if (Focused)
                {
                    using var focusPen = new Pen(Color.FromArgb(145, 202, 255), 2f);
                    g.DrawPath(focusPen, trackPath);
                }
            }

            // 2. Draw Inside Text (Optional State Label)
            if (_thumbPosition > 0.5f && !string.IsNullOrEmpty(_checkedText))

            {
                Rectangle textRect = new Rectangle(6, 0, Width - Height, Height);
                TextRenderer.DrawText(
                    g,
                    _checkedText,
                    Font,
                    textRect,
                    Color.White,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            }
            else if (_thumbPosition <= 0.5f && !string.IsNullOrEmpty(_uncheckedText))
            {
                Rectangle textRect = new Rectangle(Height, 0, Width - Height - 6, Height);
                TextRenderer.DrawText(
                    g,
                    _uncheckedText,
                    Font,
                    textRect,
                    Color.FromArgb(220, 220, 220),
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            }

            // 3. Draw Thumb (Circle)
            int thumbDiameter = Height - 6;
            int minX = 3;
            int maxX = Width - thumbDiameter - 3;
            int thumbX = (int)(minX + (_thumbPosition * (maxX - minX)));
            int thumbY = 3;

            Rectangle thumbRect = new Rectangle(thumbX, thumbY, thumbDiameter, thumbDiameter);
            using var thumbBrush = new SolidBrush(_thumbColor);
            g.FillEllipse(thumbBrush, thumbRect);
        }

        private static Color InterpolateColor(Color c1, Color c2, float t)
        {
            int r = (int)(c1.R + ((c2.R - c1.R) * t));
            int g = (int)(c1.G + ((c2.G - c1.G) * t));
            int b = (int)(c1.B + ((c2.B - c1.B) * t));
            return Color.FromArgb(r, g, b);
        }

        private static GraphicsPath CreatePillPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 90, 180);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 180);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
