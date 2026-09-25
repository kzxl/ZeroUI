using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ZeroUI.Core;

namespace ZeroUI.Wpf.Documents
{
    public class ZMarkdownViewer : Control
    {
        public string MarkdownText { get; set; } public bool ShowSourceToggle { get; set; } public event EventHandler LinkClicked;
    }
    
    [Obsolete("$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZMarkdownViewer instead.")]
    public class MarkdownViewer : ZMarkdownViewer { }
    
    [Obsolete("Zero$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZMarkdownViewer instead.")]
    public class ZeroMarkdownViewer : ZMarkdownViewer { }
}
