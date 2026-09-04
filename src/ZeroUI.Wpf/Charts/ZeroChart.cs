using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Wpf.Charts.Model;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Charts
{
    /// <summary>
    /// High-performance vector chart engine for WPF rendered directly via DrawingContext.
    /// Supports Column, Line, Spline, Area, Candlestick, and Pie/Donut visualizations.
    /// </summary>
    public class ZeroChart : FrameworkElement
    {
        public List<ZeroChartSeries> Series { get; } = new List<ZeroChartSeries>();
        public List<ZeroCandlePoint> CandleData { get; } = new List<ZeroCandlePoint>();

        public static readonly DependencyProperty ChartTypeProperty =
            DependencyProperty.Register(
                nameof(ChartType),
                typeof(ZeroChartType),
                typeof(ZeroChart),
                new FrameworkPropertyMetadata(ZeroChartType.Column, FrameworkPropertyMetadataOptions.AffectsRender));

        public ZeroChartType ChartType
        {
            get => (ZeroChartType)GetValue(ChartTypeProperty);
            set => SetValue(ChartTypeProperty, value);
        }

        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public bool ShowLegend { get; set; } = true;
        public bool ShowCrosshair { get; set; } = true;

        // Interaction state
        private Point? _mousePos;

        public ZeroChart()
        {
            ClipToBounds = true;
            ZeroWpfTheme.ThemeChanged += () => InvalidateVisual();
        }

        #pragma warning disable CS0618
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

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _mousePos = e.GetPosition(this);
            InvalidateVisual();
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            _mousePos = null;
            InvalidateVisual();
        }

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

            // Background & Border
            dc.DrawRectangle(ZeroWpfTheme.BgCard, null, new Rect(0, 0, w, h));
            dc.DrawRectangle(null, ZeroWpfTheme.BorderPen, new Rect(0.5, 0.5, w - 1, h - 1));

            // Title & Subtitle
            double contentTop = 16;
            if (!string.IsNullOrEmpty(Title))
            {
                var titleFt = CreateFormattedText(Title, ZeroWpfTheme.BoldTypeface, 14.0, ZeroWpfTheme.TextPrimary, dpi);
                dc.DrawText(titleFt, new Point(16, contentTop));
                contentTop += titleFt.Height + 2;

                if (!string.IsNullOrEmpty(Subtitle))
                {
                    var subFt = CreateFormattedText(Subtitle, ZeroWpfTheme.RegularTypeface, 11.0, ZeroWpfTheme.TextMuted, dpi);
                    dc.DrawText(subFt, new Point(16, contentTop));
                    contentTop += subFt.Height + 10;
                }
                else
                {
                    contentTop += 8;
                }
            }

            // Plot Area
            double padLeft = 45;
            double padRight = 20;
            double padBottom = 30;
            Rect plotRect = new Rect(padLeft, contentTop, Math.Max(10, w - padLeft - padRight), Math.Max(10, h - contentTop - padBottom));

            if (ChartType == ZeroChartType.Pie || ChartType == ZeroChartType.Donut)
            {
                RenderPieDonut(dc, plotRect, dpi);
            }
            else if (ChartType == ZeroChartType.Candlestick)
            {
                RenderCandlestick(dc, plotRect, dpi);
            }
            else
            {
                RenderCartesian(dc, plotRect, dpi);
            }
        }

        private void RenderCartesian(DrawingContext dc, Rect plot, double dpi)
        {
            // Compute range
            double minY = 0;
            double maxY = 10;
            int maxPoints = 0;

            foreach (var s in Series)
            {
                if (!s.IsVisible) continue;
                maxPoints = Math.Max(maxPoints, s.Points.Count);
                foreach (var p in s.Points)
                {
                    if (p.Value < minY) minY = p.Value;
                    if (p.Value > maxY) maxY = p.Value;
                }
            }

            if (maxY <= minY) maxY = minY + 10;
            maxY = Math.Ceiling(maxY * 1.1);

            // Draw Y-Axis grid lines
            int yTicks = 4;
            for (int i = 0; i <= yTicks; i++)
            {
                double ratio = (double)i / yTicks;
                double yVal = minY + (maxY - minY) * ratio;
                double yPos = plot.Bottom - ratio * plot.Height;

                dc.DrawLine(ZeroWpfTheme.GridLinePen, new Point(plot.Left, yPos), new Point(plot.Right, yPos));

                var ft = CreateFormattedText($"{yVal:0.#}", ZeroWpfTheme.RegularTypeface, 10.0, ZeroWpfTheme.TextMuted, dpi);
                dc.DrawText(ft, new Point(plot.Left - ft.Width - 6, yPos - ft.Height / 2.0));
            }

            if (maxPoints == 0) return;

            // Draw X-Axis labels
            double stepX = plot.Width / Math.Max(1, maxPoints);
            for (int i = 0; i < maxPoints; i++)
            {
                double xPos = plot.Left + i * stepX + stepX / 2.0;
                string label = i < Series[0].Points.Count ? Series[0].Points[i].Label : $"{i + 1}";
                var xFt = CreateFormattedText(label, ZeroWpfTheme.RegularTypeface, 10.0, ZeroWpfTheme.TextMuted, dpi);
                dc.DrawText(xFt, new Point(xPos - xFt.Width / 2.0, plot.Bottom + 6));
            }

            // Render Series
            foreach (var s in Series)
            {
                if (!s.IsVisible || s.Points.Count == 0) continue;

                var type = s.TypeOverride ?? ChartType;
                var brush = new SolidColorBrush(s.Color);
                brush.Freeze();
                var pen = new Pen(brush, s.StrokeThickness);
                pen.Freeze();

                if (type == ZeroChartType.Column)
                {
                    double colWidth = Math.Max(4, stepX * 0.5);
                    for (int i = 0; i < s.Points.Count; i++)
                    {
                        var p = s.Points[i];
                        double pRatio = (p.Value - minY) / (maxY - minY);
                        double barH = pRatio * plot.Height;
                        double xPos = plot.Left + i * stepX + (stepX - colWidth) / 2.0;
                        double yPos = plot.Bottom - barH;

                        dc.DrawRoundedRectangle(brush, null, new Rect(xPos, yPos, colWidth, barH), 2, 2);
                    }
                }
                else if (type == ZeroChartType.Line || type == ZeroChartType.Spline || type == ZeroChartType.Area || type == ZeroChartType.SplineArea || type == ZeroChartType.AreaSpline)
                {
                    var geom = new StreamGeometry();
                    var areaGeom = new StreamGeometry();

                    using (var ctx = geom.Open())
                    using (var actx = areaGeom.Open())
                    {
                        Point firstPt = new Point(plot.Left + stepX / 2.0, plot.Bottom - ((s.Points[0].Value - minY) / (maxY - minY)) * plot.Height);
                        ctx.BeginFigure(firstPt, false, false);
                        actx.BeginFigure(new Point(firstPt.X, plot.Bottom), true, true);
                        actx.LineTo(firstPt, true, false);

                        for (int i = 1; i < s.Points.Count; i++)
                        {
                            var p = s.Points[i];
                            double pRatio = (p.Value - minY) / (maxY - minY);
                            Point pt = new Point(plot.Left + i * stepX + stepX / 2.0, plot.Bottom - pRatio * plot.Height);
                            ctx.LineTo(pt, true, false);
                            actx.LineTo(pt, true, false);
                        }

                        actx.LineTo(new Point(plot.Left + (s.Points.Count - 1) * stepX + stepX / 2.0, plot.Bottom), true, false);
                    }

                    geom.Freeze();
                    areaGeom.Freeze();

                    if (type == ZeroChartType.Area || type == ZeroChartType.SplineArea || type == ZeroChartType.AreaSpline)
                    {
                        var areaFill = new SolidColorBrush(Color.FromArgb((byte)(s.FillOpacity * 255), s.Color.R, s.Color.G, s.Color.B));
                        areaFill.Freeze();
                        dc.DrawGeometry(areaFill, null, areaGeom);
                    }

                    dc.DrawGeometry(null, pen, geom);

                    // Draw points
                    for (int i = 0; i < s.Points.Count; i++)
                    {
                        double pRatio = (s.Points[i].Value - minY) / (maxY - minY);
                        Point pt = new Point(plot.Left + i * stepX + stepX / 2.0, plot.Bottom - pRatio * plot.Height);
                        dc.DrawEllipse(ZeroWpfTheme.BgCard, pen, pt, 3.5, 3.5);
                    }
                }
            }

            // Crosshair & Tooltip
            if (ShowCrosshair && _mousePos.HasValue && plot.Contains(_mousePos.Value))
            {
                double mx = _mousePos.Value.X;
                int hoveredIdx = (int)((mx - plot.Left) / stepX);
                if (hoveredIdx >= 0 && hoveredIdx < maxPoints)
                {
                    double cx = plot.Left + hoveredIdx * stepX + stepX / 2.0;
                    dc.DrawLine(ZeroWpfTheme.AccentPen, new Point(cx, plot.Top), new Point(cx, plot.Bottom));

                    // Tooltip Bubble
                    string tooltipText = $"{Series[0].Points[hoveredIdx].Label}: {Series[0].Points[hoveredIdx].Value:N0}";
                    var ttFt = CreateFormattedText(tooltipText, ZeroWpfTheme.BoldTypeface, 11.0, ZeroWpfTheme.TextPrimary, dpi);

                    double tipW = ttFt.Width + 16;
                    double tipH = ttFt.Height + 8;
                    double tipX = Math.Min(plot.Right - tipW, Math.Max(plot.Left, cx - tipW / 2.0));
                    double tipY = Math.Max(plot.Top + 4, _mousePos.Value.Y - tipH - 8);

                    dc.DrawRoundedRectangle(ZeroWpfTheme.BgInput, ZeroWpfTheme.BorderPen, new Rect(tipX, tipY, tipW, tipH), 4, 4);
                    dc.DrawText(ttFt, new Point(tipX + 8, tipY + 4));
                }
            }
        }

        private void RenderPieDonut(DrawingContext dc, Rect plot, double dpi)
        {
            if (Series.Count == 0 || Series[0].Points.Count == 0) return;

            var points = Series[0].Points;
            double total = 0;
            foreach (var p in points) total += Math.Max(0, p.Value);
            if (total <= 0) return;

            Point center = new Point(plot.Left + plot.Width / 2.0, plot.Top + plot.Height / 2.0);
            double outerRadius = Math.Min(plot.Width, plot.Height) / 2.2;
            double innerRadius = (ChartType == ZeroChartType.Donut) ? outerRadius * 0.6 : 0;

            Color[] palette = new Color[]
            {
                Color.FromRgb(129, 140, 248), // Primary Indigo
                Color.FromRgb(167, 139, 250), // Purple
                Color.FromRgb(166, 227, 161), // Green
                Color.FromRgb(249, 226, 175), // Yellow
                Color.FromRgb(243, 139, 168), // Pink
                Color.FromRgb(137, 180, 250)  // Sky
            };

            double curAngle = -90.0;
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                double sliceAngle = (p.Value / total) * 360.0;
                double nextAngle = curAngle + sliceAngle;

                Color sliceColor = p.ColorOverride ?? palette[i % palette.Length];
                var sliceBrush = new SolidColorBrush(sliceColor);
                sliceBrush.Freeze();

                var pathGeom = new PathGeometry();
                var fig = new PathFigure();

                double radStart = curAngle * Math.PI / 180.0;
                double radEnd = nextAngle * Math.PI / 180.0;

                Point p1 = new Point(center.X + outerRadius * Math.Cos(radStart), center.Y + outerRadius * Math.Sin(radStart));
                Point p2 = new Point(center.X + outerRadius * Math.Cos(radEnd), center.Y + outerRadius * Math.Sin(radEnd));

                fig.StartPoint = (innerRadius > 0) ?
                    new Point(center.X + innerRadius * Math.Cos(radStart), center.Y + innerRadius * Math.Sin(radStart)) : center;

                fig.Segments.Add(new LineSegment(p1, true));
                fig.Segments.Add(new ArcSegment(p2, new Size(outerRadius, outerRadius), 0, sliceAngle > 180, SweepDirection.Clockwise, true));

                if (innerRadius > 0)
                {
                    Point p3 = new Point(center.X + innerRadius * Math.Cos(radEnd), center.Y + innerRadius * Math.Sin(radEnd));
                    fig.Segments.Add(new LineSegment(p3, true));
                    fig.Segments.Add(new ArcSegment(fig.StartPoint, new Size(innerRadius, innerRadius), 0, sliceAngle > 180, SweepDirection.Counterclockwise, true));
                }

                fig.IsClosed = true;
                pathGeom.Figures.Add(fig);
                pathGeom.Freeze();

                dc.DrawGeometry(sliceBrush, ZeroWpfTheme.BorderPen, pathGeom);

                curAngle = nextAngle;
            }

            // Donut Center Text
            if (ChartType == ZeroChartType.Donut)
            {
                var totalFt = CreateFormattedText($"{total:N0}", ZeroWpfTheme.BoldTypeface, 16.0, ZeroWpfTheme.TextPrimary, dpi);
                var labelFt = CreateFormattedText("Total", ZeroWpfTheme.RegularTypeface, 10.0, ZeroWpfTheme.TextMuted, dpi);

                dc.DrawText(totalFt, new Point(center.X - totalFt.Width / 2.0, center.Y - totalFt.Height / 2.0 - 6));
                dc.DrawText(labelFt, new Point(center.X - labelFt.Width / 2.0, center.Y + 10));
            }
        }

        private void RenderCandlestick(DrawingContext dc, Rect plot, double dpi)
        {
            if (CandleData.Count == 0) return;

            double minP = double.MaxValue;
            double maxP = double.MinValue;
            foreach (var c in CandleData)
            {
                if (c.Low < minP) minP = c.Low;
                if (c.High > maxP) maxP = c.High;
            }
            if (maxP <= minP) maxP = minP + 10;

            double stepX = plot.Width / CandleData.Count;
            double candleWidth = Math.Max(3, stepX * 0.65);

            for (int i = 0; i < CandleData.Count; i++)
            {
                var c = CandleData[i];
                Brush cBrush = c.IsBullish ? ZeroWpfTheme.SuccessAccent : ZeroWpfTheme.DangerAccent;
                Pen cPen = new Pen(cBrush, 1.0);
                cPen.Freeze();

                double cx = plot.Left + i * stepX + stepX / 2.0;
                double highY = plot.Bottom - ((c.High - minP) / (maxP - minP)) * plot.Height;
                double lowY = plot.Bottom - ((c.Low - minP) / (maxP - minP)) * plot.Height;
                double openY = plot.Bottom - ((c.Open - minP) / (maxP - minP)) * plot.Height;
                double closeY = plot.Bottom - ((c.Close - minP) / (maxP - minP)) * plot.Height;

                // Wick
                dc.DrawLine(cPen, new Point(cx, highY), new Point(cx, lowY));

                // Body
                double topY = Math.Min(openY, closeY);
                double bodyH = Math.Max(2, Math.Abs(openY - closeY));
                dc.DrawRectangle(cBrush, null, new Rect(cx - candleWidth / 2.0, topY, candleWidth, bodyH));
            }
        }
    }
}
