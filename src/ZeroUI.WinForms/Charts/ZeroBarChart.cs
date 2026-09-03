using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using ZeroUI.WinForms.Charts.Model;

namespace ZeroUI.WinForms.Charts
{
    /// <summary>
    /// Specialized convenience Bar and Column chart control for metric comparisons.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Charts & Analytics")]
    [Description("Specialized Column and Bar comparison chart")]
    public class ZeroBarChart : ZeroChart
    {
        private bool _isHorizontal = false;
        private bool _isStacked = false;

        [Category("Appearance")]
        [DefaultValue(false)]
        public bool IsHorizontal
        {
            get => _isHorizontal;
            set
            {
                _isHorizontal = value;
                UpdateChartType();
            }
        }

        [Category("Appearance")]
        [DefaultValue(false)]
        public bool IsStacked
        {
            get => _isStacked;
            set
            {
                _isStacked = value;
                UpdateChartType();
            }
        }

        public ZeroBarChart()
        {
            ChartType = ZeroChartType.Column;
        }

        private void UpdateChartType()
        {
            if (_isHorizontal)
            {
                ChartType = _isStacked ? ZeroChartType.StackedBar : ZeroChartType.Bar;
            }
            else
            {
                ChartType = _isStacked ? ZeroChartType.StackedColumn : ZeroChartType.Column;
            }
        }

        public ZeroChartSeries SetData(string seriesName, IEnumerable<string> categories, IEnumerable<double> values, Color? color = null)
        {
            var series = AddSeries(seriesName, color);
            series.AddPoints(values, categories);
            Invalidate();
            return series;
        }
    }
}
