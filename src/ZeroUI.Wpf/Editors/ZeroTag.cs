using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Editors
{
    public enum ZeroTagType
    {
        Default,
        Success,
        Processing,
        Warning,
        Error
    }

    /// <summary>
    /// Lightweight status tag / badge component for ZeroUI in WPF with soft backgrounds,
    /// crisp borders, semantic colors, and optional close action.
    /// </summary>
    public class ZeroTag : FrameworkElement
    {
        private bool _isCloseHovered = false;

        #region Dependency Properties

        public static readonly DependencyProperty TagTypeProperty =
            DependencyProperty.Register(nameof(TagType), typeof(ZeroTagType), typeof(ZeroTag),
                new FrameworkPropertyMetadata(ZeroTagType.Default, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(ZeroTag),
                new FrameworkPropertyMetadata("Tag", FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(double), typeof(ZeroTag),
                new FrameworkPropertyMetadata(4.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ClosableProperty =
            DependencyProperty.Register(nameof(Closable), typeof(bool), typeof(ZeroTag),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        #endregion

        #region Properties & Events

        public ZeroTagType TagType
        {
            get => (ZeroTagType)GetValue(TagTypeProperty);
            set => SetValue(TagTypeProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public double CornerRadius
        {
            get => (double)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public bool Closable
        {
            get => (bool)GetValue(ClosableProperty);
            set => SetValue(ClosableProperty, value);
        }

        public event EventHandler? Closed;

        #endregion

        public ZeroTag()
        {
            Height = 24;
            ZeroWpfTheme.ThemeChanged += () => InvalidateVisual();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var formattedText = new FormattedText(
                Text ?? string.Empty,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                ZeroWpfTheme.MediumTypeface,
                11.5,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            double width = formattedText.Width + 16;
            if (Closable)
            {
                width += 14;
            }

            double height = Math.Max(22, Height);
            return new Size(
                double.IsPositiveInfinity(availableSize.Width) ? width : Math.Min(width, availableSize.Width),
                double.IsPositiveInfinity(availableSize.Height) ? height : Math.Min(height, availableSize.Height));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!Closable) return;

            bool hov = e.GetPosition(this).X >= (ActualWidth - 20);
            if (_isCloseHovered != hov)
            {
                _isCloseHovered = hov;
                Cursor = hov ? Cursors.Hand : Cursors.Arrow;
                InvalidateVisual();
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (_isCloseHovered)
            {
                _isCloseHovered = false;
                Cursor = Cursors.Arrow;
                InvalidateVisual();
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (Closable && e.LeftButton == MouseButtonState.Pressed && e.GetPosition(this).X >= (ActualWidth - 20))
            {
                Closed?.Invoke(this, EventArgs.Empty);
                Visibility = Visibility.Collapsed;
                e.Handled = true;
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            var (bgBrush, borderPen, textBrush) = GetTagColors(TagType);
            Rect rect = new Rect(0.5, 0.5, w - 1.0, h - 1.0);
            dc.DrawRoundedRectangle(bgBrush, borderPen, rect, CornerRadius, CornerRadius);

            var formattedText = new FormattedText(
                Text ?? string.Empty,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                ZeroWpfTheme.MediumTypeface,
                11.5,
                textBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            double textX = 8.0;
            double textY = Math.Round((h - formattedText.Height) / 2.0);
            dc.DrawText(formattedText, new Point(textX, textY));

            // Close button icon
            if (Closable)
            {
                double cx = w - 11.0;
                double cy = h / 2.0;
                Brush closeBrush = _isCloseHovered ? textBrush : new SolidColorBrush(textBrush.Color) { Opacity = 0.55 };
                Pen closePen = new Pen(closeBrush, 1.2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

                dc.DrawLine(closePen, new Point(cx - 3.5, cy - 3.5), new Point(cx + 3.5, cy + 3.5));
                dc.DrawLine(closePen, new Point(cx + 3.5, cy - 3.5), new Point(cx - 3.5, cy + 3.5));
            }
        }

        private static (Brush Bg, Pen Border, SolidColorBrush Text) GetTagColors(ZeroTagType type)
        {
            Color accentColor;
            switch (type)
            {
                case ZeroTagType.Success:
                    accentColor = ZeroWpfTheme.SuccessAccent.Color;
                    break;
                case ZeroTagType.Processing:
                    accentColor = ZeroWpfTheme.PrimaryAccent.Color;
                    break;
                case ZeroTagType.Warning:
                    accentColor = ZeroWpfTheme.WarningAccent.Color;
                    break;
                case ZeroTagType.Error:
                    accentColor = ZeroWpfTheme.DangerAccent.Color;
                    break;
                default:
                    // Default Neutral Tag
                    return (
                        ZeroWpfTheme.BgHover,
                        ZeroWpfTheme.BorderPen,
                        ZeroWpfTheme.TextPrimary
                    );
            }

            Brush bg = new SolidColorBrush(Color.FromArgb(32, accentColor.R, accentColor.G, accentColor.B));
            bg.Freeze();

            Brush borderBrush = new SolidColorBrush(Color.FromArgb(90, accentColor.R, accentColor.G, accentColor.B));
            borderBrush.Freeze();
            Pen border = new Pen(borderBrush, 1.0);
            border.Freeze();

            SolidColorBrush fg = new SolidColorBrush(accentColor);
            fg.Freeze();

            return (bg, border, fg);
        }
    }
}
