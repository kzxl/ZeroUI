using ZeroUI.Core.Data;

namespace ZeroUI.Wpf.Charts
{
    /// <summary>
    /// Hierarchical tapered pyramid chart for organizational tiers, demographic bands,
    /// and bottom-up manufacturing yield aggregations in WPF.
    /// </summary>
    public class PyramidChart : FunnelChart
    {
        public PyramidChart()
        {
            Mode = FunnelChartMode.Pyramid;
        }
    }
}
