using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using ZeroUI.Core;

namespace ZeroUI.WinForms.Editors
{
    public class ZJsonEditor : Control
    {
        public string JsonText { get; set; } public bool ReadOnly { get; set; } public int IndentSize { get; set; } public bool ShowLineNumbers { get; set; } public event EventHandler JsonChanged;
    }
    
    [Obsolete("JsonEditor is deprecated and will be removed in 5 release cycles. Please migrate to ZJsonEditor instead.")]
    [ToolboxItem(false)]
    public class JsonEditor : ZJsonEditor { }
    
    [Obsolete("ZeroJsonEditor is deprecated and will be removed in 5 release cycles. Please migrate to ZJsonEditor instead.")]
    [ToolboxItem(false)]
    public class ZeroJsonEditor : ZJsonEditor { }
}
