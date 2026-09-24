using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ZeroUI.WinForms.Icons;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// Modern anti-aliased flat CheckBox control for ZeroUI.
    /// Supports two-state and three-state (Checked, Unchecked, Indeterminate),
    /// keyboard spacebar toggling, custom check alignment, and theme synchronization.
    /// Canonical drop-in replacement for standard <see cref="System.Windows.Forms.CheckBox"/>.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultProperty("Checked")]
    [DefaultEvent("CheckedChanged")]
    [Description("Modern theme-aware checkbox control with tri-state support")]
    [ToolboxBitmap(typeof(ZeroIcons), "CheckEdit.bmp")]
    public class ZCheckBox : CheckEdit
    {
    }

    /// <summary>
    /// Convenience alias for <see cref="ZCheckBox"/>.
    /// </summary>
    public class ZCheckEdit : ZCheckBox { }
}
