using System;

namespace ZeroUI.WinForms.Charts.Model
{
    /// <summary>
    /// Supported visualization types for ZeroChart and specialized chart controls.
    /// </summary>
    public enum ZeroChartType
    {
        /// <summary>Vertical grouped column chart.</summary>
        Column,

        /// <summary>Vertical stacked column chart.</summary>
        StackedColumn,

        /// <summary>Horizontal grouped bar chart.</summary>
        Bar,

        /// <summary>Horizontal stacked bar chart.</summary>
        StackedBar,

        /// <summary>Standard linear point-to-point line chart.</summary>
        Line,

        /// <summary>Smooth Catmull-Rom spline curve line chart.</summary>
        Spline,

        /// <summary>Linear area chart with translucent gradient fill under the line.</summary>
        Area,

        /// <summary>Smooth curved spline area chart with gradient fill.</summary>
        SplineArea,

        /// <summary>Circular pie chart with proportional sectors.</summary>
        Pie,

        /// <summary>Donut chart with hollow center for KPI summaries and statistics.</summary>
        Donut
    }

    /// <summary>
    /// Position of the chart legend.
    /// </summary>
    public enum ZeroChartLegendPosition
    {
        None,
        Top,
        Bottom,
        Right
    }
}
