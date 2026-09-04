using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// Modern anti-aliased slider / trackbar control for ZeroUI.
    /// Ideal for SCADA setpoints, motor speeds, thresholds, and continuous parameter tuning.
    /// Supports live value tooltip badge, keyboard stepping, and theme synchronization.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultProperty("Value")]
    [DefaultEvent("ValueChanged")]
    [Description("Modern anti-aliased slider control")]
    public class ZeroSlider : Control
    {
        private float _minimum = 0f;
        private float _maximum = 100f;
        private float _value = 0f;
        private float _step = 1f;
        private Orientation _orientation = Orientation.Horizontal;
        private string _unit = "%";
        private bool _showValueBadge = true;
        private int _trackThickness = 6;
        private int _thumbSize = 18;

        private bool _isDragging = false;
        private bool _isHovered = false;
        private bool _isThumbHovered = false;
        private bool _isFocused = false;

        public event EventHandler? ValueChanged;
        public event EventHandler? Scroll;

        public ZeroSlider()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(200, 36);
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            BackColor = Color.Transparent;

            ZeroTheme.ThemeChanged += (s, e) => Invalidate();
        }

        [Category("Behavior")]
        [DefaultValue(0f)]
        public float Minimum
        {
            get => _minimum;
            set
            {
                if (_minimum != value)
                {
                    _minimum = value;
                    if (_value < _minimum) Value = _minimum;
                    Invalidate();
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(100f)]
        public float Maximum
        {
            get => _maximum;
            set
            {
                if (_maximum != value)
                {
                    _maximum = Math.Max(_minimum + 0.001f, value);
                    if (_value > _maximum) Value = _maximum;
                    Invalidate();
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(0f)]
        public float Value
        {
            get => _value;
            set
            {
                float clamped = Math.Max(_minimum, Math.Min(_maximum, value));
                if (_step > 0)
                {
                    clamped = _minimum + (float)Math.Round((clamped - _minimum) / _step) * _step;
                    clamped = Math.Max(_minimum, Math.Min(_maximum, clamped));
                }

                if (Math.Abs(_value - clamped) > 0.0001f)
                {
                    _value = clamped;
                    Invalidate();
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                    Scroll?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(1f)]
        public float Step
        {
            get => _step;
            set => _step = Math.Max(0.001f, value);
        }

        [Category("Appearance")]
        [DefaultValue(Orientation.Horizontal)]
        public Orientation Orientation
        {
            get => _orientation;
            set
            {
                if (_orientation != value)
                {
                    _orientation = value;
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue("%")]
        public string Unit
        {
            get => _unit;
            set
            {
                _unit = value ?? "";
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        public bool ShowValueBadge
        {
            get => _showValueBadge;
            set
            {
                _showValueBadge = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(6)]
        public int TrackThickness
        {
            get => _trackThickness;
            set
            {
                _trackThickness = Math.Max(2, Math.Min(20, value));
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(18)]
        public int ThumbSize
        {
            get => _thumbSize;
            set
            {
                _thumbSize = Math.Max(10, Math.Min(36, value));
                Invalidate();
            }
        }

        private float GetFraction()
        {
            float range = _maximum - _minimum;
            return range > 0 ? (_value - _minimum) / range : 0f;
        }

        private void SetValueFromPoint(Point pt)
        {
            float range = _maximum - _minimum;
            if (range <= 0) return;

            if (_orientation == Orientation.Horizontal)
            {
                int trackMargin = _thumbSize / 2 + 4;
                int trackLength = Math.Max(10, Width - (trackMargin * 2));
                float fraction = Math.Max(0f, Math.Min(1f, (float)(pt.X - trackMargin) / trackLength));
                Value = _minimum + (fraction * range);
            }
            else
            {
                int trackMargin = _thumbSize / 2 + 4;
                int trackLength = Math.Max(10, Height - (trackMargin * 2));
                // Vertical slider: bottom is minimum, top is maximum
                float fraction = Math.Max(0f, Math.Min(1f, (float)(Height - trackMargin - pt.Y) / trackLength));
                Value = _minimum + (fraction * range);
            }
        }

        private RectangleF GetThumbRect()
        {
            float fraction = GetFraction();
            int trackMargin = _thumbSize / 2 + 4;

            if (_orientation == Orientation.Horizontal)
            {
                int trackLength = Math.Max(10, Width - (trackMargin * 2));
                float thumbX = trackMargin + (fraction * trackLength) - (_thumbSize / 2f);
                float thumbY = (Height - _thumbSize) / 2f;
                return new RectangleF(thumbX, thumbY, _thumbSize, _thumbSize);
            }
            else
            {
                int trackLength = Math.Max(10, Height - (trackMargin * 2));
                float thumbY = (Height - trackMargin) - (fraction * trackLength) - (_thumbSize / 2f);
                float thumbX = (Width - _thumbSize) / 2f;
                return new RectangleF(thumbX, thumbY, _thumbSize, _thumbSize);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (e.Button == MouseButtons.Left && Enabled)
            {
                _isDragging = true;
                SetValueFromPoint(e.Location);
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDragging && Enabled)
            {
                SetValueFromPoint(e.Location);
            }
            else
            {
                var thumb = GetThumbRect();
                bool overThumb = thumb.Contains(e.Location);
                if (_isThumbHovered != overThumb)
                {
                    _isThumbHovered = overThumb;
                    Invalidate();
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_isDragging)
            {
                _isDragging = false;
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
            _isThumbHovered = false;
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!Enabled) return;

            float delta = _step > 0 ? _step : 1f;
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down)
            {
                Value -= delta;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up)
            {
                Value += delta;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.PageDown)
            {
                Value -= delta * 5;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.PageUp)
            {
                Value += delta * 5;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Home)
            {
                Value = _minimum;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.End)
            {
                Value = _maximum;
                e.Handled = true;
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            _isFocused = true;
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            _isFocused = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var palette = ZeroTheme.Colors;
            bool isDark = ZeroTheme.IsDark;

            int trackMargin = _thumbSize / 2 + 4;
            float fraction = GetFraction();

            Color trackBg = isDark ? Color.FromArgb(40, 48, 62) : Color.FromArgb(226, 232, 240);
            Color activeColor = Enabled ? palette.Primary : (isDark ? Color.FromArgb(60, 70, 85) : Color.FromArgb(180, 190, 200));

            if (_orientation == Orientation.Horizontal)
            {
                int trackLength = Math.Max(10, Width - (trackMargin * 2));
                float trackY = (Height - _trackThickness) / 2f;
                var trackRect = new RectangleF(trackMargin, trackY, trackLength, _trackThickness);

                // Background track
                using (var trackPath = ZeroUIConfig.CreateRoundedRectangle(Rectangle.Round(trackRect), _trackThickness / 2))
                using (var trackBrush = new SolidBrush(trackBg))
                {
                    g.FillPath(trackBrush, trackPath);
                }

                // Active track
                float activeWidth = fraction * trackLength;
                if (activeWidth > 0)
                {
                    var activeRect = new RectangleF(trackMargin, trackY, Math.Max(_trackThickness, activeWidth), _trackThickness);
                    using var activePath = ZeroUIConfig.CreateRoundedRectangle(Rectangle.Round(activeRect), _trackThickness / 2);
                    using var activeBrush = new SolidBrush(activeColor);
                    g.FillPath(activeBrush, activePath);
                }
            }
            else
            {
                int trackLength = Math.Max(10, Height - (trackMargin * 2));
                float trackX = (Width - _trackThickness) / 2f;
                var trackRect = new RectangleF(trackX, trackMargin, _trackThickness, trackLength);

                // Background track
                using (var trackPath = ZeroUIConfig.CreateRoundedRectangle(Rectangle.Round(trackRect), _trackThickness / 2))
                using (var trackBrush = new SolidBrush(trackBg))
                {
                    g.FillPath(trackBrush, trackPath);
                }

                // Active track (from bottom up)
                float activeHeight = fraction * trackLength;
                if (activeHeight > 0)
                {
                    float activeY = (Height - trackMargin) - activeHeight;
                    var activeRect = new RectangleF(trackX, activeY, _trackThickness, Math.Max(_trackThickness, activeHeight));
                    using var activePath = ZeroUIConfig.CreateRoundedRectangle(Rectangle.Round(activeRect), _trackThickness / 2);
                    using var activeBrush = new SolidBrush(activeColor);
                    g.FillPath(activeBrush, activePath);
                }
            }

            // Draw Thumb
            var thumbRect = GetThumbRect();

            // Focus or drag outer halo
            if ((_isFocused || _isDragging || _isThumbHovered) && Enabled)
            {
                float haloPad = _isDragging ? 5f : 3f;
                using var haloBrush = new SolidBrush(Color.FromArgb(_isDragging ? 50 : 30, palette.Primary));
                g.FillEllipse(haloBrush, thumbRect.X - haloPad, thumbRect.Y - haloPad, thumbRect.Width + (haloPad * 2), thumbRect.Height + (haloPad * 2));
            }

            // Thumb body (White circle with subtle border)
            Color thumbFill = Enabled ? Color.White : (isDark ? Color.FromArgb(80, 85, 95) : Color.FromArgb(200, 205, 215));
            using (var thumbBrush = new SolidBrush(thumbFill))
            {
                g.FillEllipse(thumbBrush, thumbRect);
            }

            using (var borderPen = new Pen(activeColor, 1.75f))
            {
                g.DrawEllipse(borderPen, thumbRect);
            }

            // Value Tooltip / Badge when dragging or hovering
            if (_showValueBadge && (_isDragging || _isThumbHovered || _isFocused || _isHovered))
            {
                string badgeText = $"{_value:0.##}{_unit}";
                using var badgeFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
                Size badgeSize = TextRenderer.MeasureText(badgeText, badgeFont);
                int bw = badgeSize.Width + 10;
                int bh = 18;

                float bx;
                float by;

                if (_orientation == Orientation.Horizontal)
                {
                    bx = thumbRect.X + (thumbRect.Width / 2f) - (bw / 2f);
                    by = thumbRect.Y - bh - 4;
                    if (by < 2) by = thumbRect.Bottom + 4;
                }
                else
                {
                    bx = thumbRect.Right + 6;
                    by = thumbRect.Y + (thumbRect.Height / 2f) - (bh / 2f);
                }

                var badgeRect = new Rectangle((int)bx, (int)by, bw, bh);
                using (var badgePath = ZeroUIConfig.CreateRoundedRectangle(badgeRect, 4))
                using (var badgeBrush = new SolidBrush(isDark ? Color.FromArgb(20, 25, 35) : Color.FromArgb(40, 50, 65)))
                using (var badgeBorderPen = new Pen(palette.Primary, 1f))
                {
                    g.FillPath(badgeBrush, badgePath);
                    g.DrawPath(badgeBorderPen, badgePath);
                }

                TextRenderer.DrawText(g, badgeText, badgeFont, badgeRect, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }
    }
}
