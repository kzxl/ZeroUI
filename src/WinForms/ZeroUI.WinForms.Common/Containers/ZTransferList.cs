using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ZeroUI.WinForms.Base;
using ZeroUI.Core.Theme;

namespace ZeroUI.WinForms.Containers
{
    [ToolboxItem(true)]
    [Category("ZeroUI - Containers")]
    public class ZTransferList : ControlBase
    {
        public object AvailableItems { get; set; } public object SelectedItems { get; set; } public event EventHandler ItemsTransferred;
        
        protected override void OnThemeChanged(ZeroSkin skin)
        {
            base.OnThemeChanged(skin);
            Invalidate();
        }
    }

    [Obsolete("ZeroTransferList is deprecated and will be removed in 5 release cycles. Please migrate to ZTransferList instead.")]
    [ToolboxItem(false)]
    public class ZeroTransferList : ZTransferList
    {
    }
}
