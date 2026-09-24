using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ZeroUI.WinForms.Icons;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// Modern anti-aliased flat ComboBox control for ZeroUI.
    /// Canonical drop-in replacement for standard <see cref="System.Windows.Forms.ComboBox"/>.
    /// Supports lightweight dropdown selection, keyboard navigation, enum binding, and theme synchronization.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultProperty("SelectedIndex")]
    [DefaultEvent("SelectedIndexChanged")]
    [Description("Modern theme-aware ComboBox dropdown control adhering to the canonical Z-prefix standard")]
    [ToolboxBitmap(typeof(ZeroIcons), "ZeroComboBox.bmp")]
    public class ZComboBox : ComboBoxEdit
    {
    }

    /// <summary>
    /// Convenience alias for <see cref="ZComboBox"/>.
    /// </summary>
    public class ZComboBoxEdit : ZComboBox { }
}
