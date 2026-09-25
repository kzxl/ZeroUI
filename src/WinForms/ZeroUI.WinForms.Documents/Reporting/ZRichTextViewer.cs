using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using ZeroUI.Core;

namespace ZeroUI.WinForms.Documents
{
    public class ZRichTextViewer : Control
    {
        public string HtmlContent { get; set; } public bool ShowToolbar { get; set; } public double ZoomLevel { get; set; }
    }
    
    [Obsolete("$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZRichTextViewer instead.")]
    [ToolboxItem(false)]
    public class RichTextViewer : ZRichTextViewer { }
    
    [Obsolete("Zero$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZRichTextViewer instead.")]
    [ToolboxItem(false)]
    public class ZeroRichTextViewer : ZRichTextViewer { }
}
