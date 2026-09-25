using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ZeroUI.Core;

namespace ZeroUI.Wpf.Editors
{
    public class ZJsonEditor : Control
    {
        public static readonly DependencyProperty JsonTextProperty = DependencyProperty.Register(nameof(JsonText), typeof(string), typeof(ZJsonEditor), new PropertyMetadata(string.Empty)); public string JsonText { get => (string)GetValue(JsonTextProperty); set => SetValue(JsonTextProperty, value); } public bool ReadOnly { get; set; } public int IndentSize { get; set; } public bool ShowLineNumbers { get; set; } public event EventHandler JsonChanged;
    }
    
    [Obsolete("$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZJsonEditor instead.")]
    public class JsonEditor : ZJsonEditor { }
    
    [Obsolete("Zero$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZJsonEditor instead.")]
    public class ZeroJsonEditor : ZJsonEditor { }
}
