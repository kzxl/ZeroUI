using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Charts
{
    public class ZBubbleChart : FrameworkElement
    {
        public object DataSource { get; set; } public bool ShowLabels { get; set; } public string XAxisTitle { get; set; } public string YAxisTitle { get; set; }
        
        public ZBubbleChart()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }
        private void OnLoaded(object sender, RoutedEventArgs e) => ZeroWpfTheme.ThemeChanged += OnThemeChanged;
        private void OnUnloaded(object sender, RoutedEventArgs e) => ZeroWpfTheme.ThemeChanged -= OnThemeChanged;
        private void OnThemeChanged() => InvalidateVisual();
        
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            // Rendering logic
        }
    }

    [Obsolete("ZeroBubbleChart is deprecated and will be removed in 5 release cycles. Please migrate to ZBubbleChart instead.")]
    public class ZeroBubbleChart : ZBubbleChart
    {
    }
}
