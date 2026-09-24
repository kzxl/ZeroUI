using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ZeroUI.WinForms.Icons;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// Modern anti-aliased text input control for ZeroUI with built-in placeholder text,
    /// one-click clear button, password masking, character casing, and action icon slots.
    /// Canonical drop-in replacement for standard <see cref="System.Windows.Forms.TextBox"/>.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultProperty("Text")]
    [DefaultEvent("TextChanged")]
    [Description("Modern theme-aware text input control with clear button and placeholder")]
    [ToolboxBitmap(typeof(ZeroIcons), "TextEdit.bmp")]
    public class ZTextBox : TextEdit
    {
    }

    /// <summary>
    /// Convenience alias for <see cref="ZTextBox"/>.
    /// </summary>
    public class ZTextEdit : ZTextBox { }
}
