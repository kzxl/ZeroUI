using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Navigation
{
    public class ZTreeView : Control
    {
        public object Nodes { get; set; } public bool ShowLines { get; set; } public bool ShowCheckboxes { get; set; } public object SelectedNode { get; set; } public event EventHandler NodeSelected; public event EventHandler NodeExpanded;
        
        public ZTreeView()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }
        private void OnLoaded(object sender, RoutedEventArgs e) => ZeroWpfTheme.ThemeChanged += OnThemeChanged;
        private void OnUnloaded(object sender, RoutedEventArgs e) => ZeroWpfTheme.ThemeChanged -= OnThemeChanged;
        private void OnThemeChanged() { }
    }

    [Obsolete("ZeroTreeView is deprecated and will be removed in 5 release cycles. Please migrate to ZTreeView instead.")]
    public class ZeroTreeView : ZTreeView
    {
    }
}
