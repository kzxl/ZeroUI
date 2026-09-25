using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Containers
{
    public class ZTransferList : Control
    {
        public object AvailableItems { get; set; } public object SelectedItems { get; set; } public event EventHandler ItemsTransferred;
        
        public ZTransferList()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }
        private void OnLoaded(object sender, RoutedEventArgs e) => ZeroWpfTheme.ThemeChanged += OnThemeChanged;
        private void OnUnloaded(object sender, RoutedEventArgs e) => ZeroWpfTheme.ThemeChanged -= OnThemeChanged;
        private void OnThemeChanged() { }
    }

    [Obsolete("ZeroTransferList is deprecated and will be removed in 5 release cycles. Please migrate to ZTransferList instead.")]
    public class ZeroTransferList : ZTransferList
    {
    }
}
