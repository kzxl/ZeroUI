using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Feedback
{
    public class ZEmptyState : Control
    {
        public string Glyph { get; set; } public string Title { get; set; } public string Description { get; set; } public string ActionText { get; set; } public event EventHandler ActionClicked;
        
        public ZEmptyState()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }
        private void OnLoaded(object sender, RoutedEventArgs e) => ZeroWpfTheme.ThemeChanged += OnThemeChanged;
        private void OnUnloaded(object sender, RoutedEventArgs e) => ZeroWpfTheme.ThemeChanged -= OnThemeChanged;
        private void OnThemeChanged() { }
    }

    [Obsolete("ZeroEmptyState is deprecated and will be removed in 5 release cycles. Please migrate to ZEmptyState instead.")]
    public class ZeroEmptyState : ZEmptyState
    {
    }
}
