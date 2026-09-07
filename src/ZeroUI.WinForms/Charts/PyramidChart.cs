using System.ComponentModel;
using System.Drawing;
using ZeroUI.Core.Data;
using ZeroUI.WinForms.Icons;

namespace ZeroUI.WinForms.Charts
{
    /// <summary>
    /// Hierarchical tapered pyramid chart for organizational tiers, demographic bands,
    /// and bottom-up manufacturing yield aggregations.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Charts")]
    [DefaultProperty("Stages")]
    [DefaultEvent("SelectedStageChanged")]
    [Description("Hierarchical pyramid chart visualizing tiered structures and cumulative yield metrics")]
    [ToolboxBitmap(typeof(ZeroIcons), "PyramidChart.bmp")]
    public class PyramidChart : ZeroFunnelChart
    {
        public PyramidChart()
        {
            Mode = FunnelChartMode.Pyramid;
        }
    }

    /// <summary>
    /// ZeroUI naming alias for PyramidChart.
    /// </summary>
    [ToolboxItem(false)]
    public class ZeroPyramidChart : PyramidChart
    {
    }
}
