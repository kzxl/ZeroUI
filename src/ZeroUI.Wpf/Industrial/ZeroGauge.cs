using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Industrial
{
    /// <summary>
    /// Industrial Radial Speedometer / Gauge with colored threshold zones and needle pointer.
    /// Rendered directly via DrawingContext for 60+ FPS real-time telemetry.
    /// </summary>
    public class ZeroGauge : FrameworkElement
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(ZeroGauge),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double Minimum { get; set; } = 0;
        public double Maximum { get; set; } = 100;
        public double WarningThreshold { get; set; } = 70;
        public double DangerThreshold { get; set; } = 85;
        public string Unit { get; set; } = "RPM";
        public string Title { get; set; } = "Speed";

        public ZeroGauge()
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
            if (!string.IsNullOrEmpty(Title))
            {
                var titleFt = CreateFormattedText(Title, ZeroWpfTheme.BoldTypeface, 12.0, ZeroWpfTheme.TextSecondary, dpi);
                dc.DrawText(titleFt, new Point(14, 12));
            }

            // Gauge Center & Radius
            Point center = new Point(w / 2.0, h * 0.62);
            double radius = Math.Min(w, h) * 0.38;
            if (radius <= 10) return;

            double startAngle = 140.0;
            double sweepAngle = 260.0;

            // Draw Background Arc
            DrawArc(dc, center, radius, startAngle, sweepAngle, ZeroWpfTheme.BgInput, 10.0);

            // Draw Threshold Arcs (Normal, Warning, Danger)
            double range = Math.Max(1, Maximum - Minimum);
            double warnRatio = Math.Max(0, Math.Min(1, (WarningThreshold - Minimum) / range));
            double dangerRatio = Math.Max(0, Math.Min(1, (DangerThreshold - Minimum) / range));

            // Safe Zone (Green/Accent)
            DrawArc(dc, center, radius, startAngle, sweepAngle * warnRatio, ZeroWpfTheme.SuccessAccent, 6.0);
            // Warning Zone (Yellow)
            DrawArc(dc, center, radius, startAngle + sweepAngle * warnRatio, sweepAngle * (dangerRatio - warnRatio), ZeroWpfTheme.WarningAccent, 6.0);
            // Danger Zone (Red)
            DrawArc(dc, center, radius, startAngle + sweepAngle * dangerRatio, sweepAngle * (1.0 - dangerRatio), ZeroWpfTheme.DangerAccent, 6.0);

            // Needle Angle
            double clampedVal = Math.Max(Minimum, Math.Min(Maximum, Value));
            double valRatio = (clampedVal - Minimum) / range;
            double needleAngle = (startAngle + sweepAngle * valRatio) * Math.PI / 180.0;

            // Draw Needle
            Point needleTip = new Point(center.X + (radius - 12) * Math.Cos(needleAngle), center.Y + (radius - 12) * Math.Sin(needleAngle));
            Pen needlePen = new Pen(ZeroWpfTheme.PrimaryAccent, 2.5);
            needlePen.Freeze();
            dc.DrawLine(needlePen, center, needleTip);

            // Center Pin
            dc.DrawEllipse(ZeroWpfTheme.PrimaryAccent, null, center, 6, 6);
            dc.DrawEllipse(ZeroWpfTheme.BgCard, null, center, 2.5, 2.5);

            // Digital Value readout
            var valFt = CreateFormattedText($"{clampedVal:0.#}", ZeroWpfTheme.BoldTypeface, 18.0, ZeroWpfTheme.TextPrimary, dpi);
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
}
