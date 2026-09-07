using System.ComponentModel;
using System.Drawing;
using ZeroUI.WinForms.Icons;

namespace ZeroUI.WinForms.Industrial
{
    /// <summary>
    /// Industrial linear thermometer and level gauge with threshold zones.
    /// Clean enterprise alias for ZeroLinearGauge.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Industrial & SCADA")]
    [DefaultProperty("Value")]
    [Description("Industrial linear thermometer and level gauge with threshold zones")]
    [ToolboxBitmap(typeof(ZeroIcons), "LinearGauge.bmp")]
    public class LinearGauge : ZeroLinearGauge
    {
    }
}
