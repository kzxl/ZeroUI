using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace ZeroUI.Wpf.Charts.Model
{
    public enum ZeroChartType
    {
        Column,
        Bar,
        Line,
        Spline,
        Area,
        Candlestick,
        Pie,
        Donut
    }

    public class ZeroChartPoint
    {
        public string Label { get; set; } = string.Empty;
        public double Value { get; set; }
        public string? FormattedValue { get; set; }
        public Color? ColorOverride { get; set; }

        public ZeroChartPoint() { }

        public ZeroChartPoint(string label, double value, Color? colorOverride = null)
        {
            Label = label ?? string.Empty;
            Value = value;
            ColorOverride = colorOverride;
        }

        public override string ToString() => $"{Label}: {Value}";
    }

    public class ZeroCandlePoint
    {
        public DateTime Time { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double Volume { get; set; }

        public bool IsBullish => Close >= Open;

        public ZeroCandlePoint(DateTime time, double open, double high, double low, double close, double volume = 0)
        {
            Time = time;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }
    }

    public class ZeroChartSeries
    {
        public string Name { get; set; } = "Series";
        public List<ZeroChartPoint> Points { get; } = new List<ZeroChartPoint>();
        public Color Color { get; set; } = Color.FromRgb(129, 140, 248); // Indigo / PrimaryAccent
        public double StrokeThickness { get; set; } = 2.0;
        public double FillOpacity { get; set; } = 0.25;
        public bool IsVisible { get; set; } = true;
        public ZeroChartType? TypeOverride { get; set; }

        public ZeroChartSeries() { }

        public ZeroChartSeries(string name, Color color)
        {
            Name = name;
            Color = color;
        }

        public ZeroChartSeries AddPoint(string label, double value, Color? colorOverride = null)
        {
            Points.Add(new ZeroChartPoint(label, value, colorOverride));
            return this;
        }
    }
}
