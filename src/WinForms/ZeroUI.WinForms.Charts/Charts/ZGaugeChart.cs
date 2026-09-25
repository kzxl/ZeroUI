using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using ZeroUI.WinForms.Charts.Model;

namespace ZeroUI.WinForms.Charts
{
    [ToolboxItem(true)]
    [Category("ZeroUI - Charts")]
    public class ZGaugeChart : ZChart
    {
        public double Value { get; set; } public double Min { get; set; } public double Max { get; set; } public object Zones { get; set; } public bool ShowLabel { get; set; }
    }

    [Obsolete("ZeroGaugeChart is deprecated and will be removed in 5 release cycles. Please migrate to ZGaugeChart instead.")]
    [ToolboxItem(false)]
    public class ZeroGaugeChart : ZGaugeChart
    {
    }
}

