using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ZeroUI.Core;

namespace ZeroUI.Wpf.Editors
{
    public class ZCronEditor : Control
    {
        public static readonly DependencyProperty CronExpressionProperty = DependencyProperty.Register(nameof(CronExpression), typeof(string), typeof(ZCronEditor), new PropertyMetadata(string.Empty)); public string CronExpression { get => (string)GetValue(CronExpressionProperty); set => SetValue(CronExpressionProperty, value); } public static readonly DependencyProperty ShowPreviewProperty = DependencyProperty.Register(nameof(ShowPreview), typeof(bool), typeof(ZCronEditor), new PropertyMetadata(false)); public bool ShowPreview { get => (bool)GetValue(ShowPreviewProperty); set => SetValue(ShowPreviewProperty, value); } public event EventHandler CronChanged;
    }
    
    [Obsolete("CronEditor is deprecated and will be removed in 5 release cycles. Please migrate to ZCronEditor instead.")]
    public class CronEditor : ZCronEditor { }
    
    [Obsolete("ZeroCronEditor is deprecated and will be removed in 5 release cycles. Please migrate to ZCronEditor instead.")]
    public class ZeroCronEditor : ZCronEditor { }
}
