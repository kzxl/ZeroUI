using System;
using System.Drawing;

namespace ZeroUI.WinForms.Charts.Model
{
    /// <summary>
    /// Represents a single discrete data point in a chart series or circular slice.
    /// </summary>
    public class ZeroChartPoint
    {
        public string Label { get; set; } = string.Empty;
        public double Value { get; set; }
        public string? FormattedValue { get; set; }
        public Color? ColorOverride { get; set; }
        public object? Tag { get; set; }

        public ZeroChartPoint() { }

        public ZeroChartPoint(string label, double value, Color? colorOverride = null)
        {
            Label = label ?? string.Empty;
            Value = value;
            ColorOverride = colorOverride;
        }

        public ZeroChartPoint(double value, string? formattedValue = null)
        {
            Value = value;
            FormattedValue = formattedValue;
        }

        public override string ToString() => $"{Label}: {Value}";
    }
}
