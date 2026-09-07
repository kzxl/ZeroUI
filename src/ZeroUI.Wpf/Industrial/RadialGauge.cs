using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using ZeroUI.Core.Scada;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Industrial
{
    /// <summary>
    /// High-precision circular industrial instrumentation dial gauge for SCADA and MES monitoring in WPF.
    /// Supports 180° / 270° sweep scales, multi-zone colored threshold bands, major/minor tick marks,
    /// inertial needle damping, and direct binding to SCADA telemetry tags via <see cref="IScadaBindable"/>.
    /// </summary>
    public class RadialGauge : FrameworkElement, IScadaBindable
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(RadialGauge),
                new FrameworkPropertyMetadata(65.0, FrameworkPropertyMetadataOptions.AffectsRender, OnValueChanged));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(RadialGauge),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(RadialGauge),
                new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(RadialGauge),
                new FrameworkPropertyMetadata("Pressure", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register(
                nameof(Unit),
                typeof(string),
                typeof(RadialGauge),
                new FrameworkPropertyMetadata("bar", FrameworkPropertyMetadataOptions.AffectsRender));

        private double _indicatedValue = 65.0;
        private double _targetValue = 65.0;
        private float _startAngle = 135f;
        private float _sweepAngle = 270f;
        private int _majorTicks = 10;
        private int _minorTicks = 4;
        private bool _enableDamping = true;
        private double _dampingFactor = 0.25;
        private readonly DispatcherTimer? _animationTimer;
        private readonly List<GaugeThresholdRange> _thresholds = new List<GaugeThresholdRange>();

        public event EventHandler? ValueChanged;

        #region Properties

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Unit
        {
            get => (string)GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }

        public float StartAngle
        {
            get => _startAngle;
            set { _startAngle = value; InvalidateVisual(); }
        }

        public float SweepAngle
        {
            get => _sweepAngle;
            set { _sweepAngle = Math.Max(30f, Math.Min(360f, value)); InvalidateVisual(); }
        }

        public int MajorTicks
        {
            get => _majorTicks;
            set { _majorTicks = Math.Max(2, value); InvalidateVisual(); }
        }

        public int MinorTicks
        {
            get => _minorTicks;
            set { _minorTicks = Math.Max(0, value); InvalidateVisual(); }
        }

        public bool EnableDamping
        {
            get => _enableDamping;
            set => _enableDamping = value;
        }

        public double DampingFactor
        {
            get => _dampingFactor;
            set => _dampingFactor = Math.Max(0.01, Math.Min(1.0, value));
        }

        public List<GaugeThresholdRange> Thresholds => _thresholds;

        public double WarningThreshold
        {
            get => _thresholds.Count > 1 ? _thresholds[1].From : 70.0;
            set
            {
                if (_thresholds.Count >= 2)
                {
                    _thresholds[0].To = value;
                    _thresholds[1].From = value;
                    InvalidateVisual();
                }
            }
        }

        public double DangerThreshold
        {
            get => _thresholds.Count > 2 ? _thresholds[2].From : 85.0;
            set
            {
                if (_thresholds.Count >= 3)
                {
                    _thresholds[1].To = value;
                    _thresholds[2].From = value;
                    InvalidateVisual();
                }
            }
        }

        #region IScadaBindable Implementation

        public string? BoundTagPath { get; set; }

        public void OnTagValueChanged(IScadaTag tag)
        {
            if (tag != null)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Value = tag.GetValue<double>();
                }));
            }
        }

        #endregion

        #endregion

        public RadialGauge()
        {
            ClipToBounds = true;

            // Default industrial thresholds: 0-70 Normal (Green), 70-85 Warning (Amber), 85-100 Danger (Red)
            _thresholds.Add(new GaugeThresholdRange(0, 70, GaugeSeverity.Normal, 0xFF10B981u, "Normal"));
            _thresholds.Add(new GaugeThresholdRange(70, 85, GaugeSeverity.Warning, 0xFFF59E0Bu, "Warning"));
            _thresholds.Add(new GaugeThresholdRange(85, 100, GaugeSeverity.Critical, 0xFFEF4444u, "Danger"));

            _animationTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _animationTimer.Tick += OnAnimationTick;

            ZeroWpfTheme.ThemeChanged += () => InvalidateVisual();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RadialGauge gauge)
            {
                double newVal = (double)e.NewValue;
                gauge.HandleNewValue(newVal);
            }
        }

        private void HandleNewValue(double val)
        {
            double clamped = Math.Max(Minimum, Math.Min(Maximum, val));
            if (_enableDamping)
            {
                _targetValue = clamped;
                if (_animationTimer != null && !_animationTimer.IsEnabled)
                {
                    _animationTimer.Start();
                }
            }
            else
            {
                _indicatedValue = clamped;
                _targetValue = clamped;
                InvalidateVisual();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnAnimationTick(object? sender, EventArgs e)
        {
            double next = GaugeMath.ApplyDamping(_indicatedValue, _targetValue, _dampingFactor);
            if (Math.Abs(next - _indicatedValue) > 1e-4)
            {
                _indicatedValue = next;
                InvalidateVisual();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _indicatedValue = _targetValue;
                _animationTimer?.Stop();
                InvalidateVisual();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        #if NETFRAMEWORK
        private static FormattedText CreateFormattedText(string text, Typeface typeface, double fontSize, Brush brush, double pixelsPerDip = 1.0)
        {
            return new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize, brush);
        }
        #else
        private static FormattedText CreateFormattedText(string text, Typeface typeface, double fontSize, Brush brush, double pixelsPerDip = 1.0)
        {
            return new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize, brush, pixelsPerDip);
        }
        #endif

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            #if NETFRAMEWORK
            double dpi = 1.0;
            #else
            double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            #endif

            // Background Card
            dc.DrawRectangle(ZeroWpfTheme.BgCard, null, new Rect(0, 0, w, h));
            dc.DrawRectangle(null, ZeroWpfTheme.BorderPen, new Rect(0.5, 0.5, w - 1, h - 1));

            // Title
            if (!string.IsNullOrEmpty(Title))
            {
                var titleFt = CreateFormattedText(Title, ZeroWpfTheme.BoldTypeface, 12.0, ZeroWpfTheme.TextSecondary, dpi);
                dc.DrawText(titleFt, new Point(14, 12));
            }

            // Gauge Center & Radius
            Point center = new Point(w / 2.0, h * 0.58);
            double radius = Math.Min(w, h) * 0.38;
            if (radius <= 10) return;

            // Draw Background Track Arc
            DrawArc(dc, center, radius, _startAngle, _sweepAngle, ZeroWpfTheme.BgInput, 10.0);

            double range = Math.Max(1.0, Maximum - Minimum);

            // Draw Threshold Arcs
            if (_thresholds.Count > 0)
            {
                foreach (var th in _thresholds)
                {
                    double tFrom = Math.Max(Minimum, Math.Min(Maximum, th.From));
                    double tTo = Math.Max(Minimum, Math.Min(Maximum, th.To));
                    if (tTo <= tFrom) continue;

                    double rFrom = (tFrom - Minimum) / range;
                    double rTo = (tTo - Minimum) / range;

                    double arcStart = _startAngle + _sweepAngle * rFrom;
                    double arcSweep = _sweepAngle * (rTo - rFrom);

                    Color c = Color.FromArgb(
                        (byte)((th.ArgbColor >> 24) & 0xFF),
                        (byte)((th.ArgbColor >> 16) & 0xFF),
                        (byte)((th.ArgbColor >> 8) & 0xFF),
                        (byte)(th.ArgbColor & 0xFF));

                    var brush = new SolidColorBrush(c);
                    brush.Freeze();
                    DrawArc(dc, center, radius, arcStart, arcSweep, brush, 6.0);
                }
            }

            // Draw Major & Minor Ticks
            int totalMajor = _majorTicks;
            for (int i = 0; i <= totalMajor; i++)
            {
                double tickRatio = (double)i / totalMajor;
                double angleDeg = _startAngle + _sweepAngle * tickRatio;
                double angleRad = angleDeg * Math.PI / 180.0;
                double cos = Math.Cos(angleRad);
                double sin = Math.Sin(angleRad);

                Point pOuter = new Point(center.X + (radius + 2) * cos, center.Y + (radius + 2) * sin);
                Point pInner = new Point(center.X + (radius - 8) * cos, center.Y + (radius - 8) * sin);

                dc.DrawLine(ZeroWpfTheme.GridLinePen, pInner, pOuter);

                // Tick numeric label
                if (radius > 35)
                {
                    double tickVal = Minimum + tickRatio * range;
                    Point pText = new Point(center.X + (radius - 18) * cos, center.Y + (radius - 18) * sin);
                    var tFt = CreateFormattedText($"{tickVal:0}", ZeroWpfTheme.RegularTypeface, 8.5, ZeroWpfTheme.TextMuted, dpi);
                    dc.DrawText(tFt, new Point(pText.X - tFt.Width / 2.0, pText.Y - tFt.Height / 2.0));
                }

                // Minor ticks
                if (_minorTicks > 0 && i < totalMajor)
                {
                    for (int m = 1; m <= _minorTicks; m++)
                    {
                        double mRatio = tickRatio + ((double)m / (_minorTicks + 1)) * (1.0 / totalMajor);
                        double mAngle = (_startAngle + _sweepAngle * mRatio) * Math.PI / 180.0;
                        Point mpOuter = new Point(center.X + radius * Math.Cos(mAngle), center.Y + radius * Math.Sin(mAngle));
                        Point mpInner = new Point(center.X + (radius - 4) * Math.Cos(mAngle), center.Y + (radius - 4) * Math.Sin(mAngle));
                        dc.DrawLine(ZeroWpfTheme.GridLinePen, mpInner, mpOuter);
                    }
                }
            }

            // Needle Angle based on indicated damped value
            double needleVal = Math.Max(Minimum, Math.Min(Maximum, _indicatedValue));
            float needleAngleDeg = GaugeMath.ValueToAngle(needleVal, Minimum, Maximum, _startAngle, _sweepAngle);
            double needleAngleRad = needleAngleDeg * Math.PI / 180.0;

            // Draw Needle
            Point needleTip = new Point(center.X + (radius - 6) * Math.Cos(needleAngleRad), center.Y + (radius - 6) * Math.Sin(needleAngleRad));
            Pen needlePen = new Pen(ZeroWpfTheme.PrimaryAccent, 2.5);
            needlePen.Freeze();
            dc.DrawLine(needlePen, center, needleTip);

            // Center Pin
            dc.DrawEllipse(ZeroWpfTheme.PrimaryAccent, null, center, 6, 6);
            dc.DrawEllipse(ZeroWpfTheme.BgCard, null, center, 2.5, 2.5);

            // Digital Value Readout
            var valFt = CreateFormattedText($"{needleVal:0.#}", ZeroWpfTheme.BoldTypeface, 18.0, ZeroWpfTheme.TextPrimary, dpi);
            var unitFt = CreateFormattedText(Unit, ZeroWpfTheme.RegularTypeface, 10.0, ZeroWpfTheme.TextMuted, dpi);

            dc.DrawText(valFt, new Point(center.X - valFt.Width / 2.0, center.Y + 16));
            dc.DrawText(unitFt, new Point(center.X - unitFt.Width / 2.0, center.Y + 38));
        }

        private static void DrawArc(DrawingContext dc, Point center, double radius, double startAngleDeg, double sweepAngleDeg, Brush brush, double thickness)
        {
            if (sweepAngleDeg <= 0) return;

            var geom = new PathGeometry();
            var fig = new PathFigure();

            double startRad = startAngleDeg * Math.PI / 180.0;
            double endRad = (startAngleDeg + sweepAngleDeg) * Math.PI / 180.0;

            Point pStart = new Point(center.X + radius * Math.Cos(startRad), center.Y + radius * Math.Sin(startRad));
            Point pEnd = new Point(center.X + radius * Math.Cos(endRad), center.Y + radius * Math.Sin(endRad));

            fig.StartPoint = pStart;
            fig.Segments.Add(new ArcSegment(pEnd, new Size(radius, radius), 0, sweepAngleDeg > 180, SweepDirection.Clockwise, true));

            geom.Figures.Add(fig);
            geom.Freeze();

            var pen = new Pen(brush, thickness);
            pen.Freeze();
            dc.DrawGeometry(null, pen, geom);
        }
    }

    /// <summary>
    /// Backward-compatibility alias for <see cref="RadialGauge"/>.
    /// </summary>
    [Obsolete("ZeroGauge is deprecated. Use RadialGauge instead.")]
    public class ZeroGauge : RadialGauge
    {
    }
}
