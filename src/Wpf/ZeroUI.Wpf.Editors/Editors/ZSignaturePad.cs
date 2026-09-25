using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ZeroUI.Core;

namespace ZeroUI.Wpf.Editors
{
    public class ZSignaturePad : Control
    {
        public System.Windows.Media.Color StrokeColor { get; set; } public int StrokeWidth { get; set; } public System.Windows.Media.Color BackgroundColor { get; set; } public bool IsEmpty { get; set; } public event EventHandler SignatureChanged; public void Clear() {} public System.Windows.Media.ImageSource GetSignatureImage() { return null; }
    }
    
    [Obsolete("$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZSignaturePad instead.")]
    public class SignaturePad : ZSignaturePad { }
    
    [Obsolete("Zero$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZSignaturePad instead.")]
    public class ZeroSignaturePad : ZSignaturePad { }
}
