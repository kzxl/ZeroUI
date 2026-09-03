using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using ZeroUI.WinForms.Charts.Model;

namespace ZeroUI.WinForms.Charts
{
    /// <summary>
    /// Specialized convenience Line, Spline, and Area trend chart control.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Charts & Analytics")]
    [Description("Specialized Line and Area trend chart")]
    public class ZeroLineChart : ZeroChart
    {
        private bool _isCurved = true;
        private bool _isArea = true;

        [Category("Appearance")]
        [DefaultValue(true)]
        public bool IsCurved
        {
            get => _isCurved;
            set
            {
                _isCurved = value;
                UpdateChartType();
            }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        public bool IsArea
        {
            get => _isArea;
            set
            {
                _isArea = value;
                UpdateChartType();
            }
        }

        public ZeroLineChart()
        {
            ChartType = ZeroChartType.SplineArea;
        }

        private void UpdateChartType()
        {
            if (_isArea)
            {
                ChartType = _isCurved ? ZeroChartType.SplineArea : ZeroChartType.Area;
            }
            else
            {
                ChartType = _isCurved ? ZeroChartType.Spline : ZeroChartType.Line;
            }
        }

        public ZeroChartSeries AddTrendSeries(string name, IEnumerable<double> values, IEnumerable<string>? categories = null, Color? color = null)
        {
            var series = AddSeries(name, color);
            series.AddPoints(values, categories);
            Invalidate();
            return series;
        }
    }
}
