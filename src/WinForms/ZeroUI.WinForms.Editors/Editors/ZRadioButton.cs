using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ZeroUI.WinForms.Icons;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// Modern anti-aliased flat RadioButton control for ZeroUI.
    /// Provides mutual exclusion across siblings or GroupName, keyboard navigation,
    /// and responsive ZeroTheme styling.
    /// Canonical drop-in replacement for standard <see cref="System.Windows.Forms.RadioButton"/>.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultProperty("Checked")]
    [DefaultEvent("CheckedChanged")]
    [Description("Modern theme-aware radio button control adhering to the canonical Z-prefix standard")]
    [ToolboxBitmap(typeof(ZeroIcons), "ZeroRadioButton.bmp")]
    public class ZRadioButton : RadioButtonControl
    {
    }
}
