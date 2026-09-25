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
    public class ZAvatar : ControlBase
    {
        public string Initials { get; set; } public object ImageSource { get; set; } public string Size { get; set; } public object BackgroundColor { get; set; } public bool ShowOnlineIndicator { get; set; }
        
        protected override void OnThemeChanged(ZeroSkin skin)
        {
            base.OnThemeChanged(skin);
            Invalidate();
        }
    }

    [Obsolete("ZeroAvatar is deprecated and will be removed in 5 release cycles. Please migrate to ZAvatar instead.")]
    [ToolboxItem(false)]
    public class ZeroAvatar : ZAvatar
    {
    }
}
