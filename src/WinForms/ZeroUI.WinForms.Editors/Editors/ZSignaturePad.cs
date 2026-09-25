using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using ZeroUI.Core;

namespace ZeroUI.WinForms.Editors
{
    public class ZSignaturePad : Control
    {
        public System.Drawing.Color StrokeColor { get; set; } public int StrokeWidth { get; set; } public System.Drawing.Color BackgroundColor { get; set; } public bool IsEmpty { get; set; } public event EventHandler SignatureChanged; public void Clear() {} public System.Drawing.Bitmap GetSignatureImage() { return null; }
    }
    
    [Obsolete("$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZSignaturePad instead.")]
    [ToolboxItem(false)]
    public class SignaturePad : ZSignaturePad { }
    
    [Obsolete("Zero$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZSignaturePad instead.")]
    [ToolboxItem(false)]
    public class ZeroSignaturePad : ZSignaturePad { }
}
