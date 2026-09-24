using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ZeroUI.WinForms.Icons;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// Modern flat progress bar with smooth anti-aliased fill, percentage overlay, and indeterminate animation.
    /// Canonical drop-in replacement for standard <see cref="System.Windows.Forms.ProgressBar"/>.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultProperty("Value")]
    [Description("Modern theme-aware flat progress bar with percentage overlay and indeterminate shimmer")]
    [ToolboxBitmap(typeof(ZeroIcons), "ZeroProgressBar.bmp")]
    public class ZProgressBar : ProgressBarControl
    {
    }
}
