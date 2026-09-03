using System;
using System.Collections.Generic;
using System.Drawing;

namespace ZeroUI.WinForms.Charts.Model
{
    /// <summary>
    /// Represents a data series containing multiple data points, styling, and visibility state.
    /// </summary>
    public class ZeroChartSeries
    {
        public string Name { get; set; } = "Series";
        public List<ZeroChartPoint> Points { get; } = new List<ZeroChartPoint>();
        public Color Color { get; set; } = Color.FromArgb(79, 70, 229); // Default Indigo 600
        public float StrokeWidth { get; set; } = 2.5f;
        public float FillOpacity { get; set; } = 0.25f;
        public bool IsVisible { get; set; } = true;
        public ZeroChartType? ChartTypeOverride { get; set; }

        public ZeroChartSeries() { }

        public ZeroChartSeries(string name, Color color)
        {
            Name = name ?? "Series";
            Color = color;
        }

        public ZeroChartSeries(string name, Color color, IEnumerable<double> values, IEnumerable<string>? labels = null)
        {
            Name = name ?? "Series";
            Color = color;
            AddPoints(values, labels);
        }

        public ZeroChartSeries AddPoint(string label, double value, Color? colorOverride = null)
        {
            Points.Add(new ZeroChartPoint(label, value, colorOverride));
            return this;
        }

        public ZeroChartSeries AddPoints(IEnumerable<double> values, IEnumerable<string>? labels = null)
        {
            if (values == null) return this;
            using var valEnum = values.GetEnumerator();
            using var lblEnum = labels?.GetEnumerator();

            int index = 1;
            while (valEnum.MoveNext())
            {
                string label = (lblEnum != null && lblEnum.MoveNext()) ? lblEnum.Current : $"Item {index}";
                Points.Add(new ZeroChartPoint(label, valEnum.Current));
                index++;
            }
            return this;
        }

        public void Clear() => Points.Clear();
    }
}
