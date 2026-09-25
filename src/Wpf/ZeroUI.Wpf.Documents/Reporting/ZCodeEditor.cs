using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ZeroUI.Core;

namespace ZeroUI.Wpf.Documents
{
    public class ZCodeEditor : Control
    {
        public string Text { get; set; } public string Language { get; set; } public bool ShowLineNumbers { get; set; } public bool ReadOnly { get; set; } public int TabSize { get; set; } public event EventHandler TextChanged;
    }
    
    [Obsolete("CodeEditor is deprecated and will be removed in 5 release cycles. Please migrate to ZCodeEditor instead.")]
    public class CodeEditor : ZCodeEditor { }
    
    [Obsolete("ZeroCodeEditor is deprecated and will be removed in 5 release cycles. Please migrate to ZCodeEditor instead.")]
    public class ZeroCodeEditor : ZCodeEditor { }
}
