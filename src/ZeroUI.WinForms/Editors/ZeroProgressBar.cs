using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.Rendering;
using ZeroUI.WinForms.Native;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// Modern flat progress bar with smooth anti-aliased fill, percentage overlay, and indeterminate animation.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultProperty("Value")]
    [Description("Modern flat progress bar with percentage overlay and indeterminate shimmer")]
    public class ZeroProgressBar : Control
    {

        private int _value = 0;
        private int _maximum = 100;
        private int _minimum = 0;
        private bool _showPercentage = true;
        private bool _isIndeterminate = false;
        private int _marqueeOffset = 0;
        private IDisposable? _clockToken;

        private Color _progressColor = Color.FromArgb(79, 70, 229); // Indigo 600
        private Color _trackColor = Color.FromArgb(243, 244, 246);   // Gray 100
        private int _borderRadius = 4;

        public ZeroProgressBar()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);

            Size = new Size(200, 20);
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        }

        [Category("Behavior")]
        [DefaultValue(0)]
        public int Value
        {
            get => _value;
            set
            {
                int clamped = Math.Max(_minimum, Math.Min(_maximum, value));
                if (_value != clamped)
                {
                    _value = clamped;
                    Invalidate();
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(100)]
        public int Maximum
        {
            get => _maximum;
            set { _maximum = Math.Max(_minimum + 1, value); Invalidate(); }
        }

        [Category("Behavior")]
        [DefaultValue(0)]
        public int Minimum
        {
            get => _minimum;
            set { _minimum = Math.Max(0, value); Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        public bool ShowPercentage
        {
            get => _showPercentage;
            set { _showPercentage = value; Invalidate(); }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set
            {
                _isIndeterminate = value;
                if (_isIndeterminate)
                {
                    if (!ZeroDesignHelper.IsInDesignMode(this) && IsHandleCreated)
                    {
                        _clockToken ??= ZeroAnimationClock.Subscribe(OnAnimationFrameTick);
                    }
                }
                else
                {
                    _clockToken?.Dispose();
                    _clockToken = null;
                    Invalidate();
                }
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!ZeroDesignHelper.IsInDesignMode(this) && _isIndeterminate)
            {
                _clockToken ??= ZeroAnimationClock.Subscribe(OnAnimationFrameTick);
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);
            _clockToken?.Dispose();
            _clockToken = null;
        }

        private void OnAnimationFrameTick(double deltaSeconds, long frameCount)
        {
            if (_isIndeterminate)
            {
                int span = Width + 60;
                if (span > 0)
                {
                    _marqueeOffset = (int)(ZeroAnimationClock.FluidPhase * span);
                    if (IsHandleCreated && Visible)
                    {
                        Invalidate();
                    }
                }
            }
        }

        [Category("Appearance")]
        public Color ProgressColor
        {
            get => _progressColor;
            set { _progressColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        public Color TrackColor
        {
            get => _trackColor;
            set { _trackColor = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle trackRect = new Rectangle(0, 0, Width, Height);

            // 1. Draw Track
            using (var path = CreateRoundedRectangle(trackRect, _borderRadius))
            {
                using var trackBrush = new SolidBrush(_trackColor);
                g.FillPath(trackBrush, path);
            }

            // 2. Draw Progress
            if (_isIndeterminate)
            {
                int blockW = Math.Max(40, Width / 3);
                Rectangle blockRect = new Rectangle(_marqueeOffset - blockW, 0, blockW, Height);
                using var progBrush = new SolidBrush(_progressColor);
                g.SetClip(CreateRoundedRectangle(trackRect, _borderRadius));
                g.FillRectangle(progBrush, blockRect);
                g.ResetClip();
            }
            else
            {
                int range = _maximum - _minimum;
                int val = _value - _minimum;
                float pct = range > 0 ? (float)val / range : 0f;
                int fillW = (int)(Width * pct);

                if (fillW > 0)
                {
                    Rectangle fillRect = new Rectangle(0, 0, fillW, Height);
                    using var fillPath = CreateRoundedRectangle(fillRect, _borderRadius);
                    using var progBrush = new SolidBrush(_progressColor);
                    g.FillPath(progBrush, fillPath);
                }

                // 3. Draw Percentage Text
                if (_showPercentage && Height >= 14)
                {
                    string text = $"{(int)(pct * 100)}%";
                    Color textColor = pct > 0.55f ? Color.White : Color.FromArgb(55, 65, 81);
                    TextRenderer.DrawText(
                        g,
                        text,
                        Font,
                        trackRect,
                        textColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                }
            }
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius) =>
            ZeroUIConfig.CreateRoundedRectangle(rect, radius);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _clockToken?.Dispose();
                _clockToken = null;
            }
            base.Dispose(disposing);
        }
    }
}
