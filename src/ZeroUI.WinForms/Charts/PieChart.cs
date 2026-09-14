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
    public class PieChart : ChartControl
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
                ChartType = _isDonut ? ChartType.Donut : ChartType.Pie;
                Invalidate();
            }
        }

        public PieChart()
        {
            ChartType = ChartType.Donut;
        }

        public PieChart AddSlice(string label, double value, Color? color = null)
        {
            if (Series.Count == 0)
            {
                AddSeries("Default");
            }

            var assignedColor = color ?? ChartPalette.GetColor(Series[0].Points.Count);
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
                var col = slice.Color ?? ChartPalette.GetColor(idx++);
                series.AddPoint(slice.Label, slice.Value, col);
            }
            Invalidate();
        }
    }

    /// <summary>
    /// Legacy alias for <see cref="PieChart"/>.
    /// Preserved for backward compatibility.
    /// </summary>
    [Obsolete("ZeroPieChart is deprecated. Please use PieChart instead.")]
    [ToolboxItem(false)]
    public class ZeroPieChart : PieChart
    {
    }
}
