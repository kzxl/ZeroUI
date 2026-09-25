using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ZeroUI.Core;

namespace ZeroUI.Wpf.Data
{
    public class ZVirtualGrid : Control
    {
        public int RowCount { get; set; } public int ColumnCount { get; set; } public bool ReadOnly { get; set; } public event EventHandler CellValueNeeded;
    }
    
    [Obsolete("$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZVirtualGrid instead.")]
    public class VirtualGrid : ZVirtualGrid { }
    
    [Obsolete("Zero$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZVirtualGrid instead.")]
    public class ZeroVirtualGrid : ZVirtualGrid { }
}
