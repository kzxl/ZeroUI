using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using ZeroUI.Core;

namespace ZeroUI.WinForms.Editors
{
    public class ZPasswordBox : Control
    {
        public string Password { get; set; } public bool ShowStrengthMeter { get; set; } public bool ShowToggleVisibility { get; set; } public string StrengthLevel { get; set; } public string PlaceholderText { get; set; }
    }
    
    [Obsolete("PasswordBox is deprecated and will be removed in 5 release cycles. Please migrate to ZPasswordBox instead.")]
    [ToolboxItem(false)]
    public class PasswordBox : ZPasswordBox { }
    
    [Obsolete("ZeroPasswordBox is deprecated and will be removed in 5 release cycles. Please migrate to ZPasswordBox instead.")]
    [ToolboxItem(false)]
    public class ZeroPasswordBox : ZPasswordBox { }
}
