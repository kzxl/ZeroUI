using System;
using System.ComponentModel;
using System.Drawing;
using ZeroUI.WinForms.Icons;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// Modern color picker editor for WinForms with inline swatch, hex code, and palette popup.
    /// Inherits from <see cref="ColorPickEdit"/> for unified architecture and zero code duplication.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultEvent(nameof(ColorChanged))]
    [DefaultProperty(nameof(Color))]
    [Description("Color picker editor with live swatch preview, palette popup, and eyedropper")]
    [ToolboxBitmap(typeof(ZeroIcons), "ColorPickEdit.bmp")]
    public class ColorPickerEdit : ColorPickEdit
    {
        /// <summary>
        /// Gets or sets the currently selected color (alias of <see cref="ColorPickEdit.SelectedColor"/> for backward compatibility).
        /// </summary>
        [Category("Appearance")]
        [Description("The currently selected color.")]
        public Color Color
        {
            get => SelectedColor;
            set => SelectedColor = value;
        }
    }
}
