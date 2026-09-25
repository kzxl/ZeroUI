using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using ZeroUI.Core;

namespace ZeroUI.WinForms.Editors
{
    public class ZMultiSelect : Control
    {
        public object ItemsSource { get; set; } public List<object> SelectedItems { get; set; } = new List<object>(); public string DisplayMember { get; set; } public string Placeholder { get; set; } public int MaxSelections { get; set; } public event EventHandler SelectionChanged;
    }
    
    [Obsolete("MultiSelect is deprecated and will be removed in 5 release cycles. Please migrate to ZMultiSelect instead.")]
    [ToolboxItem(false)]
    public class MultiSelect : ZMultiSelect { }
    
    [Obsolete("ZeroMultiSelect is deprecated and will be removed in 5 release cycles. Please migrate to ZMultiSelect instead.")]
    [ToolboxItem(false)]
    public class ZeroMultiSelect : ZMultiSelect { }
}
