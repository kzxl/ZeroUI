using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using ZeroUI.WinForms.Charts.Model;

namespace ZeroUI.WinForms.Charts
{
    /// <summary>
    /// Specialized convenience Pie and Donut chart control for categorical distributions.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Charts & Analytics")]
    [Description("Specialized Pie and Donut distribution chart")]
    public class ZeroPieChart : ZeroChart
    {
        private bool _isDonut = true;

        [Category("Appearance")]
        [DefaultValue(true)]
        public bool IsDonut
        {
            get => _isDonut;
            set
            {
                _isDonut = value;
                ChartType = _isDonut ? ZeroChartType.Donut : ZeroChartType.Pie;
                Invalidate();
            }
        }

        public ZeroPieChart()
        {
            ChartType = ZeroChartType.Donut;
        }

        public ZeroPieChart AddSlice(string label, double value, Color? color = null)
        {
            if (Series.Count == 0)
            {
                AddSeries("Default");
            }

            var assignedColor = color ?? ZeroChartPalette.GetColor(Series[0].Points.Count);
            Series[0].AddPoint(label, value, assignedColor);
            Invalidate();
            return this;
        }

        public void SetSlices(IEnumerable<(string Label, double Value, Color? Color)> slices)
        {
            Clear();
            var series = AddSeries("Distribution");
            int idx = 0;
            foreach (var slice in slices)
            {
                var col = slice.Color ?? ZeroChartPalette.GetColor(idx++);
                series.AddPoint(slice.Label, slice.Value, col);
            }
            Invalidate();
        }
    }
}
