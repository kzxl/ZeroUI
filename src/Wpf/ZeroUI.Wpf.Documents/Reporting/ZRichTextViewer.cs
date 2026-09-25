using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ZeroUI.Core;

namespace ZeroUI.Wpf.Documents
{
    public class ZRichTextViewer : Control
    {
        public string HtmlContent { get; set; } public bool ShowToolbar { get; set; } public double ZoomLevel { get; set; }
    }
    
    [Obsolete("$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZRichTextViewer instead.")]
    public class RichTextViewer : ZRichTextViewer { }
    
    [Obsolete("Zero$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZRichTextViewer instead.")]
    public class ZeroRichTextViewer : ZRichTextViewer { }
}
