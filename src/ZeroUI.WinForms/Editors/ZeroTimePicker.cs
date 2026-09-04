using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{
    public enum TimeSegment
    {
        Hour,
        Minute,
        Second,
        AmPm
    }

    /// <summary>
    /// Modern anti-aliased time picker control for ZeroUI.
    /// Supports segmented keyboard/wheel editing (HH:mm:ss), 12h/24h formats,
    /// flyweight preset popup, and theme synchronization.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultProperty("Value")]
    [DefaultEvent("ValueChanged")]
    [Description("Modern anti-aliased time picker control")]
    public class ZeroTimePicker : Control
    {
        private TimeSpan _value = DateTime.Now.TimeOfDay;
        private bool _showSeconds = false;
        private bool _is24Hour = true;
        private int _stepMinutes = 1;

        private TimeSegment _focusedSegment = TimeSegment.Hour;
        private bool _isHovered = false;
        private bool _isFocused = false;
        private bool _isDroppedDown = false;

        private Rectangle _hourRect;
        private Rectangle _minuteRect;
        private Rectangle _secondRect;
        private Rectangle _clockIconRect;

        private readonly ToolStripDropDown _dropdown;
        private readonly TimePresetListControl _presetControl;

        public event EventHandler? ValueChanged;

        public ZeroTimePicker()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(160, 36);
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            BackColor = Color.Transparent;

            _presetControl = new TimePresetListControl(this);
            var host = new ToolStripControlHost(_presetControl)
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AutoSize = false
            };

            _dropdown = new ToolStripDropDown
            {
                AutoClose = true,
                DropShadowEnabled = true,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            _dropdown.Items.Add(host);
            _dropdown.Opened += (s, e) => { _isDroppedDown = true; Invalidate(); };
            _dropdown.Closed += (s, e) => { _isDroppedDown = false; Invalidate(); };

            ZeroTheme.ThemeChanged += (s, e) =>
            {
                _presetControl.UpdateTheme();
                Invalidate();
            };
            ZeroUIConfig.CornerStyleChanged += (s, e) => Invalidate();
            ZeroUIConfig.FontChanged += (s, e) =>
            {
                Font = ZeroUIConfig.DefaultFont;
                Invalidate();
            };

            // Truncate milliseconds
            _value = new TimeSpan(_value.Hours, _value.Minutes, _value.Seconds);
        }

        [Category("Behavior")]
        public TimeSpan Value
        {
            get => _value;
            set
            {
                var clean = new TimeSpan(value.Hours, value.Minutes, _showSeconds ? value.Seconds : 0);
                if (_value != clean)
                {
                    _value = clean;
                    Invalidate();
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue(false)]
        public bool ShowSeconds
        {
            get => _showSeconds;
            set
            {
                if (_showSeconds != value)
                {
                    _showSeconds = value;
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        public bool Is24Hour
        {
            get => _is24Hour;
            set
            {
                if (_is24Hour != value)
                {
                    _is24Hour = value;
                    Invalidate();
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(1)]
        public int StepMinutes
        {
            get => _stepMinutes;
            set => _stepMinutes = Math.Max(1, Math.Min(60, value));
        }

        private void AdjustFocusedSegment(int delta)
        {
            int h = _value.Hours;
            int m = _value.Minutes;
            int s = _value.Seconds;

            switch (_focusedSegment)
            {
                case TimeSegment.Hour:
                    h = (h + delta) % 24;
                    if (h < 0) h += 24;
                    break;
                case TimeSegment.Minute:
                    int stepM = _stepMinutes * delta;
                    m = (m + stepM) % 60;
                    if (m < 0) m += 60;
                    break;
                case TimeSegment.Second:
                    s = (s + delta) % 60;
                    if (s < 0) s += 60;
                    break;
            }

            Value = new TimeSpan(h, m, s);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int delta = e.Delta > 0 ? 1 : -1;
            AdjustFocusedSegment(delta);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Up)
            {
                AdjustFocusedSegment(1);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                AdjustFocusedSegment(-1);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Right)
            {
                if (_focusedSegment == TimeSegment.Hour) _focusedSegment = TimeSegment.Minute;
                else if (_focusedSegment == TimeSegment.Minute && _showSeconds) _focusedSegment = TimeSegment.Second;
                Invalidate();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Left)
            {
                if (_focusedSegment == TimeSegment.Second) _focusedSegment = TimeSegment.Minute;
                else if (_focusedSegment == TimeSegment.Minute) _focusedSegment = TimeSegment.Hour;
                Invalidate();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                ShowDropDown();
                e.Handled = true;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            if (_clockIconRect.Contains(e.Location))
            {
                if (_isDroppedDown) _dropdown.Close();
                else ShowDropDown();
                return;
            }

            if (_hourRect.Contains(e.Location))
            {
                _focusedSegment = TimeSegment.Hour;
                Invalidate();
            }
            else if (_minuteRect.Contains(e.Location))
            {
                _focusedSegment = TimeSegment.Minute;
                Invalidate();
            }
            else if (_showSeconds && _secondRect.Contains(e.Location))
            {
                _focusedSegment = TimeSegment.Second;
                Invalidate();
            }
        }

        public void ShowDropDown()
        {
            if (!Enabled) return;

            int popW = 200;
            int popH = 190;
            _presetControl.Size = new Size(popW, popH);

            Point screenPt = PointToScreen(new Point(0, Height + 2));
            Screen currentScreen = Screen.FromControl(this);

            if (screenPt.Y + popH > currentScreen.WorkingArea.Bottom)
            {
                screenPt = PointToScreen(new Point(0, -popH - 2));
            }

            _dropdown.Show(screenPt);
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

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateSegments();
        }

        private void UpdateSegments()
        {
            int startX = 34;
            int segW = 28;
            int segH = Height - 8;
            int segY = 4;

            _hourRect = new Rectangle(startX, segY, segW, segH);
            _minuteRect = new Rectangle(startX + segW + 10, segY, segW, segH);
            _secondRect = new Rectangle(startX + (segW * 2) + 20, segY, segW, segH);
            _clockIconRect = new Rectangle(Width - 28, (Height - 16) / 2, 16, 16);
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
            else if (_isFocused || _isDroppedDown)
            {
                borderColor = palette.Primary;
                bgColor = palette.Surface;
            }
            else if (_isHovered)
            {
                borderColor = palette.Primary;
                bgColor = palette.Surface;
            }
            else
            {
                borderColor = palette.Border;
                bgColor = palette.Surface;
            }

            // Fill & Border
            using (var path = ZeroUIConfig.CreateRoundedRectangle(borderRect, effRadius))
            {
                using (var bgBrush = new SolidBrush(bgColor))
                {
                    g.FillPath(bgBrush, path);
                }

                float penWidth = (_isFocused || _isDroppedDown) ? 1.5f : 1.0f;
                using (var pen = new Pen(borderColor, penWidth))
                {
                    g.DrawPath(pen, path);
                }
            }

            // Left Vector Clock Icon
            int iconX = 12;
            int iconY = (Height - 14) / 2;
            using (var clockPen = new Pen(palette.TextSecondary, 1.3f))
            {
                g.DrawEllipse(clockPen, iconX, iconY, 14, 14);
                g.DrawLine(clockPen, iconX + 7, iconY + 3, iconX + 7, iconY + 7);
                g.DrawLine(clockPen, iconX + 7, iconY + 7, iconX + 10, iconY + 7);
            }

            // Draw Segments
            int h = _is24Hour ? _value.Hours : ((_value.Hours % 12 == 0) ? 12 : _value.Hours % 12);
            string hStr = h.ToString("D2");
            string mStr = _value.Minutes.ToString("D2");
            string sStr = _value.Seconds.ToString("D2");

            UpdateSegments();

            using var focusSegBrush = new SolidBrush(Color.FromArgb(40, palette.Primary));
            var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix;

            // Hour segment
            if (_isFocused && _focusedSegment == TimeSegment.Hour)
            {
                using var segPath = ZeroUIConfig.CreateRoundedRectangle(_hourRect, 4);
                g.FillPath(focusSegBrush, segPath);
            }
            TextRenderer.DrawText(g, hStr, Font, _hourRect, (_isFocused && _focusedSegment == TimeSegment.Hour) ? palette.Primary : palette.TextPrimary, flags);

            // Separator 1 (:)
            int sep1X = _hourRect.Right;
            var sep1Rect = new Rectangle(sep1X, 0, 10, Height);
            TextRenderer.DrawText(g, ":", Font, sep1Rect, palette.TextSecondary, flags);

            // Minute segment
            if (_isFocused && _focusedSegment == TimeSegment.Minute)
            {
                using var segPath = ZeroUIConfig.CreateRoundedRectangle(_minuteRect, 4);
                g.FillPath(focusSegBrush, segPath);
            }
            TextRenderer.DrawText(g, mStr, Font, _minuteRect, (_isFocused && _focusedSegment == TimeSegment.Minute) ? palette.Primary : palette.TextPrimary, flags);

            if (_showSeconds)
            {
                // Separator 2 (:)
                int sep2X = _minuteRect.Right;
                var sep2Rect = new Rectangle(sep2X, 0, 10, Height);
                TextRenderer.DrawText(g, ":", Font, sep2Rect, palette.TextSecondary, flags);

                // Second segment
                if (_isFocused && _focusedSegment == TimeSegment.Second)
                {
                    using var segPath = ZeroUIConfig.CreateRoundedRectangle(_secondRect, 4);
                    g.FillPath(focusSegBrush, segPath);
                }
                TextRenderer.DrawText(g, sStr, Font, _secondRect, (_isFocused && _focusedSegment == TimeSegment.Second) ? palette.Primary : palette.TextPrimary, flags);
            }

            // Right Chevron (▼)
            using var chevBrush = new SolidBrush(palette.TextSecondary);
            float cx = _clockIconRect.X + 8;
            float cy = _clockIconRect.Y + 8;
            PointF[] pts = new[]
            {
                new PointF(cx - 3.5f, cy - 2f),
                new PointF(cx + 3.5f, cy - 2f),
                new PointF(cx, cy + 2.5f)
            };
            g.FillPolygon(chevBrush, pts);
        }

        /// <summary>
        /// Flyweight preset list popup for common factory shifts and intervals.
        /// </summary>
        private class TimePresetListControl : Control
        {
            private readonly ZeroTimePicker _owner;
            private int _hoveredIndex = -1;

            private readonly (string label, TimeSpan time)[] _presets = new[]
            {
                ("Current Time (Now)", DateTime.Now.TimeOfDay),
                ("00:00 - Midnight Shift", new TimeSpan(0, 0, 0)),
                ("06:00 - Early Morning Shift", new TimeSpan(6, 0, 0)),
                ("08:00 - Day Shift Start", new TimeSpan(8, 0, 0)),
                ("12:00 - Noon Break", new TimeSpan(12, 0, 0)),
                ("14:00 - Afternoon Shift", new TimeSpan(14, 0, 0)),
                ("18:00 - Day Shift End", new TimeSpan(18, 0, 0)),
                ("22:00 - Night Shift Start", new TimeSpan(22, 0, 0))
            };

            public TimePresetListControl(ZeroTimePicker owner)
            {
                _owner = owner;
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw, true);

                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
                UpdateTheme();
            }

            public void UpdateTheme()
            {
                BackColor = ZeroTheme.Colors.CardBackground;
                Invalidate();
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                int itemH = Height / _presets.Length;
                if (itemH <= 0) itemH = 22;
                int idx = e.Y / itemH;
                if (idx >= 0 && idx < _presets.Length && idx != _hoveredIndex)
                {
                    _hoveredIndex = idx;
                    Invalidate();
                }
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                int itemH = Height / _presets.Length;
                if (itemH <= 0) itemH = 22;
                int idx = e.Y / itemH;
                if (idx >= 0 && idx < _presets.Length)
                {
                    TimeSpan selected = idx == 0 ? DateTime.Now.TimeOfDay : _presets[idx].time;
                    _owner.Value = selected;
                    _owner._dropdown.Close();
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var palette = ZeroTheme.Colors;
                int itemH = Height / _presets.Length;

                using var borderPen = new Pen(palette.Border, 1f);
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

                using var hoverBrush = new SolidBrush(Color.FromArgb(25, palette.Primary));

                for (int i = 0; i < _presets.Length; i++)
                {
                    int y = i * itemH;
                    var itemRect = new Rectangle(2, y, Width - 4, itemH);

                    if (i == _hoveredIndex)
                    {
                        g.FillRectangle(hoverBrush, itemRect);
                    }

                    var textRect = new Rectangle(10, y, Width - 20, itemH);
                    var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix;
                    Color txtColor = i == _hoveredIndex ? palette.Primary : palette.TextPrimary;
                    TextRenderer.DrawText(g, _presets[i].label, Font, textRect, txtColor, flags);
                }
            }
        }
    }
}
