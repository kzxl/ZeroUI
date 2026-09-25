using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using ZeroUI.WinForms.Charts.Model;

namespace ZeroUI.WinForms.Charts
{
    [ToolboxItem(true)]
    [Category("ZeroUI - Charts")]
    public class ZBubbleChart : ZChart
    {
        public object DataSource { get; set; } public bool ShowLabels { get; set; } public string XAxisTitle { get; set; } public string YAxisTitle { get; set; }
    }

    [Obsolete("ZeroBubbleChart is deprecated and will be removed in 5 release cycles. Please migrate to ZBubbleChart instead.")]
    [ToolboxItem(false)]
    public class ZeroBubbleChart : ZBubbleChart
    {
    }
}

