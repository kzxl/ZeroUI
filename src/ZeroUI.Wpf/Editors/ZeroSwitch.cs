using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Editors
{
    /// <summary>
    /// Modern toggle switch for WPF.
    /// </summary>
    public class ZeroSwitch : FrameworkElement
    {
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(
                nameof(IsChecked),
                typeof(bool),
                typeof(ZeroSwitch),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender, OnIsCheckedChanged));

        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        public event EventHandler<bool>? CheckedChanged;

        public ZeroSwitch()
        {
            Width = 44;
            Height = 24;
            Cursor = Cursors.Hand;
            ZeroWpfTheme.ThemeChanged += () => InvalidateVisual();
        }

        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZeroSwitch sw)
            {
                sw.CheckedChanged?.Invoke(sw, (bool)e.NewValue);
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                IsChecked = !IsChecked;
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            Brush trackBrush = IsChecked ? ZeroWpfTheme.PrimaryAccent : ZeroWpfTheme.BgInput;
            Pen trackPen = IsChecked ? ZeroWpfTheme.AccentPen : ZeroWpfTheme.BorderPen;

            // Track pill
            dc.DrawRoundedRectangle(trackBrush, trackPen, new Rect(0.5, 0.5, w - 1, h - 1), h / 2.0, h / 2.0);

            // Thumb
            double thumbDiameter = h - 6;
            double thumbX = IsChecked ? (w - thumbDiameter - 3) : 3;
            double thumbY = 3;

            dc.DrawEllipse(Brushes.White, null, new Point(thumbX + thumbDiameter / 2.0, thumbY + thumbDiameter / 2.0), thumbDiameter / 2.0, thumbDiameter / 2.0);
        }
    }
}
