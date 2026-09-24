using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ZeroUI.WinForms.Icons;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// Modern theme-aware flat button control for ZeroUI.
    /// Canonical drop-in replacement for standard <see cref="System.Windows.Forms.Button"/>.
    /// Supports stateful hover, click animations, rounded corners, and badge counters.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultEvent("Click")]
    [DefaultProperty("Text")]
    [Description("Modern theme-aware flat button adhering to the canonical Z-prefix standard")]
    [ToolboxBitmap(typeof(ZeroIcons), "ZeroButton.bmp")]
    public class ZButton : SimpleButton
    {
    }
}
