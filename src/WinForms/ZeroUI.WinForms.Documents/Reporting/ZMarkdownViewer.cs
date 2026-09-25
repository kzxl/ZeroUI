using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using ZeroUI.Core;

namespace ZeroUI.WinForms.Documents
{
    public class ZMarkdownViewer : Control
    {
        public string MarkdownText { get; set; } public bool ShowSourceToggle { get; set; } public event EventHandler LinkClicked;
    }
    
    [Obsolete("$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZMarkdownViewer instead.")]
    [ToolboxItem(false)]
    public class MarkdownViewer : ZMarkdownViewer { }
    
    [Obsolete("Zero$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZMarkdownViewer instead.")]
    [ToolboxItem(false)]
    public class ZeroMarkdownViewer : ZMarkdownViewer { }
}
