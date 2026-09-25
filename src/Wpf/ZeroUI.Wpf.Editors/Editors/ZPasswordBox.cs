using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ZeroUI.Core;

namespace ZeroUI.Wpf.Editors
{
    public class ZPasswordBox : Control
    {
        public string Password { get; set; } public bool ShowStrengthMeter { get; set; } public bool ShowToggleVisibility { get; set; } public string StrengthLevel { get; set; } public string PlaceholderText { get; set; }
    }
    
    [Obsolete("$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZPasswordBox instead.")]
    public class PasswordBox : ZPasswordBox { }
    
    [Obsolete("Zero$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZPasswordBox instead.")]
    public class ZeroPasswordBox : ZPasswordBox { }
}
