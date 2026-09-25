using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Charts
{
    public class ZGaugeChart : FrameworkElement
    {
        public double Value { get; set; } public double Min { get; set; } public double Max { get; set; } public object Zones { get; set; } public bool ShowLabel { get; set; }
        
        public ZGaugeChart()
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

    [Obsolete("ZeroGaugeChart is deprecated and will be removed in 5 release cycles. Please migrate to ZGaugeChart instead.")]
    public class ZeroGaugeChart : ZGaugeChart
    {
    }
}
