using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using ZeroUI.Core;

namespace ZeroUI.WinForms.Documents
{
    public class ZCodeEditor : Control
    {
        public string Text { get; set; } public string Language { get; set; } public bool ShowLineNumbers { get; set; } public bool ReadOnly { get; set; } public int TabSize { get; set; } public event EventHandler TextChanged;
    }
    
    [Obsolete("$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZCodeEditor instead.")]
    [ToolboxItem(false)]
    public class CodeEditor : ZCodeEditor { }
    
    [Obsolete("Zero$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZCodeEditor instead.")]
    [ToolboxItem(false)]
    public class ZeroCodeEditor : ZCodeEditor { }
}
