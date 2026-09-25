using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using ZeroUI.Core;

namespace ZeroUI.WinForms.Data
{
    public class ZVirtualGrid : Control
    {
        public int RowCount { get; set; } public int ColumnCount { get; set; } public bool ReadOnly { get; set; } public event EventHandler CellValueNeeded;
    }
    
    [Obsolete("VirtualGrid is deprecated and will be removed in 5 release cycles. Please migrate to ZVirtualGrid instead.")]
    [ToolboxItem(false)]
    public class VirtualGrid : ZVirtualGrid { }
    
    [Obsolete("ZeroVirtualGrid is deprecated and will be removed in 5 release cycles. Please migrate to ZVirtualGrid instead.")]
    [ToolboxItem(false)]
    public class ZeroVirtualGrid : ZVirtualGrid { }
}
