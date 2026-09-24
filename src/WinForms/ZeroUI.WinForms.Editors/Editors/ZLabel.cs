using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ZeroUI.WinForms.Icons;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// Modern theme-aware label control featuring automatic text ellipsis trimming (AutoEllipsis),
    /// dynamic tooltip expansion on text truncation, and skin-aware ClearType typography.
    /// Canonical drop-in replacement for standard <see cref="System.Windows.Forms.Label"/>.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultProperty("Text")]
    [Description("Modern theme-aware label with automatic ellipsis trimming and tooltip overflow")]
    [ToolboxBitmap(typeof(ZeroIcons), "ZeroLabel.bmp")]
    public class ZLabel : LabelControl
    {
    }

    #region Backward Compatibility Shims (5-Release Deprecation Policy)

    /// <summary>
    /// Backward-compatibility alias for <see cref="ZLabel"/>.
    /// </summary>
    [Obsolete("ZeroLabel is deprecated and will be removed in 5 release cycles. Please migrate to ZLabel instead.")]
    public class ZeroLabel : ZLabel { }

    #endregion
}
