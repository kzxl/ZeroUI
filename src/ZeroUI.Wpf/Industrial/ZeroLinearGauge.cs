using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Industrial
{
    /// <summary>
    /// Industrial Linear Level Gauge / Tank bar with graduation ticks and threshold indicator.
    /// </summary>
    public class ZeroLinearGauge : FrameworkElement
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(ZeroLinearGauge),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double Minimum { get; set; } = 0;
        public double Maximum { get; set; } = 100;
        public string Title { get; set; } = "Pressure";
        public string Unit { get; set; } = "PSI";
        public bool IsHorizontal { get; set; } = false;

        public ZeroLinearGauge()
        {
            ClipToBounds = true;
            ZeroWpfTheme.ThemeChanged += () => InvalidateVisual();
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

            double range = Math.Max(1, Maximum - Minimum);
            double clampedVal = Math.Max(Minimum, Math.Min(Maximum, Value));
            double ratio = (clampedVal - Minimum) / range;

            Brush fillBrush = (ratio > 0.85) ? ZeroWpfTheme.DangerAccent :
                             (ratio > 0.70) ? ZeroWpfTheme.WarningAccent : ZeroWpfTheme.PrimaryAccent;

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
}
