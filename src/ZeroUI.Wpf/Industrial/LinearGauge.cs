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
    /// Industrial Linear Level & Pressure Gauge for SCADA / MES telemetry in WPF.
    /// Features multi-zone scale thresholds (Normal, Warning, Critical), graduations with tick marks,
    /// inertial damping, and direct binding to SCADA telemetry tags via <see cref="IScadaBindable"/>.
    /// </summary>
    public class LinearGauge : FrameworkElement, IScadaBindable
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(LinearGauge),
                new FrameworkPropertyMetadata(65.0, FrameworkPropertyMetadataOptions.AffectsRender, OnValueChanged));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(LinearGauge),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(LinearGauge),
                new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(LinearGauge),
                new FrameworkPropertyMetadata("Pressure", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register(
                nameof(Unit),
                typeof(string),
                typeof(LinearGauge),
                new FrameworkPropertyMetadata("PSI", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsHorizontalProperty =
            DependencyProperty.Register(
                nameof(IsHorizontal),
                typeof(bool),
                typeof(LinearGauge),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        private double _indicatedValue = 65.0;
        private double _targetValue = 65.0;
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

        public bool IsHorizontal
        {
            get => (bool)GetValue(IsHorizontalProperty);
            set => SetValue(IsHorizontalProperty, value);
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

        public LinearGauge()
        {
            ClipToBounds = true;

            // Default industrial thresholds: 0-75 Normal (Green), 75-90 Warning (Amber), 90-100 Danger (Red)
            _thresholds.Add(new GaugeThresholdRange(0, 75, GaugeSeverity.Normal, 0xFF10B981u, "Normal"));
            _thresholds.Add(new GaugeThresholdRange(75, 90, GaugeSeverity.Warning, 0xFFF59E0Bu, "Warning"));
            _thresholds.Add(new GaugeThresholdRange(90, 100, GaugeSeverity.Critical, 0xFFEF4444u, "Danger"));

            _animationTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _animationTimer.Tick += OnAnimationTick;

            ZeroWpfTheme.ThemeChanged += () => InvalidateVisual();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LinearGauge gauge)
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
            var titleFt = CreateFormattedText(Title, ZeroWpfTheme.BoldTypeface, 11.0, ZeroWpfTheme.TextSecondary, dpi);
            dc.DrawText(titleFt, new Point(12, 10));

            double range = Math.Max(1.0, Maximum - Minimum);
            double clampedVal = Math.Max(Minimum, Math.Min(Maximum, _indicatedValue));
            double ratio = (clampedVal - Minimum) / range;

            // Pick fill brush based on threshold ranges or severity
            GaugeSeverity sev = GaugeMath.EvaluateSeverity(clampedVal, _thresholds);
            Brush fillBrush = sev switch
            {
                GaugeSeverity.Critical => ZeroWpfTheme.DangerAccent,
                GaugeSeverity.Warning => ZeroWpfTheme.WarningAccent,
                _ => ZeroWpfTheme.PrimaryAccent
            };

            if (IsHorizontal)
            {
                double trackX = 12;
                double trackY = 32;
                double trackW = Math.Max(10, w - 24);
                double trackH = 14;

                // Track
                dc.DrawRoundedRectangle(ZeroWpfTheme.BgInput, null, new Rect(trackX, trackY, trackW, trackH), 4, 4);

                // Fill
                if (ratio > 0)
                {
                    dc.DrawRoundedRectangle(fillBrush, null, new Rect(trackX, trackY, trackW * ratio, trackH), 4, 4);
                }

                // Readout
                var valFt = CreateFormattedText($"{clampedVal:0.#} {Unit}", ZeroWpfTheme.BoldTypeface, 12.0, ZeroWpfTheme.TextPrimary, dpi);
                dc.DrawText(valFt, new Point(trackX, trackY + trackH + 6));
            }
            else
            {
                // Vertical Bar
                double barW = 22;
                double barX = 24;
                double barTop = 32;
                double barH = Math.Max(10, h - 68);

                // Track
                dc.DrawRoundedRectangle(ZeroWpfTheme.BgInput, null, new Rect(barX, barTop, barW, barH), 4, 4);

                // Fill from bottom up
                if (ratio > 0)
                {
                    double fillH = barH * ratio;
                    dc.DrawRoundedRectangle(fillBrush, null, new Rect(barX, barTop + barH - fillH, barW, fillH), 4, 4);
                }

                // Ticks on the right
                int ticks = 5;
                for (int i = 0; i <= ticks; i++)
                {
                    double tRatio = (double)i / ticks;
                    double ty = barTop + barH - tRatio * barH;
                    double tVal = Minimum + tRatio * range;

                    dc.DrawLine(ZeroWpfTheme.GridLinePen, new Point(barX + barW + 4, ty), new Point(barX + barW + 10, ty));

                    var tFt = CreateFormattedText($"{tVal:0}", ZeroWpfTheme.RegularTypeface, 9.0, ZeroWpfTheme.TextMuted, dpi);
                    dc.DrawText(tFt, new Point(barX + barW + 14, ty - tFt.Height / 2.0));
                }

                // Digital readout at bottom
                var valFt = CreateFormattedText($"{clampedVal:0.#} {Unit}", ZeroWpfTheme.BoldTypeface, 12.0, ZeroWpfTheme.TextPrimary, dpi);
                dc.DrawText(valFt, new Point(w / 2.0 - valFt.Width / 2.0, h - 26));
            }
        }
    }

    /// <summary>
    /// Backward-compatibility alias for <see cref="LinearGauge"/>.
    /// </summary>
    [Obsolete("ZeroLinearGauge is deprecated. Use LinearGauge instead.")]
    public class ZeroLinearGauge : LinearGauge
    {
    }
}
