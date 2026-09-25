using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ZeroUI.WinForms.Base;
using ZeroUI.Core.Theme;

namespace ZeroUI.WinForms.Navigation
{
    [ToolboxItem(true)]
    [Category("ZeroUI - Navigation")]
    public class ZTreeView : ControlBase
    {
        public object Nodes { get; set; } public bool ShowLines { get; set; } public bool ShowCheckboxes { get; set; } public object SelectedNode { get; set; } public event EventHandler NodeSelected; public event EventHandler NodeExpanded;
        
        protected override void OnThemeChanged(ZeroSkin skin)
        {
            base.OnThemeChanged(skin);
            Invalidate();
        }
    }

    [Obsolete("ZeroTreeView is deprecated and will be removed in 5 release cycles. Please migrate to ZTreeView instead.")]
    [ToolboxItem(false)]
    public class ZeroTreeView : ZTreeView
    {
    }
}
