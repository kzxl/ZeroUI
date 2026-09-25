using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ZeroUI.WinForms.Base;
using ZeroUI.Core.Theme;

namespace ZeroUI.WinForms.Feedback
{
    [ToolboxItem(true)]
    [Category("ZeroUI - Feedback")]
    public class ZEmptyState : ControlBase
    {
        public string Glyph { get; set; } public string Title { get; set; } public string Description { get; set; } public string ActionText { get; set; } public event EventHandler ActionClicked;
        
        protected override void OnThemeChanged(ZeroSkin skin)
        {
            base.OnThemeChanged(skin);
            Invalidate();
        }
    }

    [Obsolete("ZeroEmptyState is deprecated and will be removed in 5 release cycles. Please migrate to ZEmptyState instead.")]
    [ToolboxItem(false)]
    public class ZeroEmptyState : ZEmptyState
    {
    }
}
