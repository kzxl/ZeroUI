using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Feedback
{
    public class ZAvatar : Control
    {
        public string Initials { get; set; } public object ImageSource { get; set; } public string Size { get; set; } public object BackgroundColor { get; set; } public bool ShowOnlineIndicator { get; set; }
        
        public ZAvatar()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }
        private void OnLoaded(object sender, RoutedEventArgs e) => ZeroWpfTheme.ThemeChanged += OnThemeChanged;
        private void OnUnloaded(object sender, RoutedEventArgs e) => ZeroWpfTheme.ThemeChanged -= OnThemeChanged;
        private void OnThemeChanged() { }
    }

    [Obsolete("ZeroAvatar is deprecated and will be removed in 5 release cycles. Please migrate to ZAvatar instead.")]
    public class ZeroAvatar : ZAvatar
    {
    }
}
