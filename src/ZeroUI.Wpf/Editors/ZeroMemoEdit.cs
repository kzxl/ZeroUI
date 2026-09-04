using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Editors
{
    /// <summary>
    /// Modern anti-aliased multi-line text editor (Memo / Text Area) for ZeroUI in WPF.
    /// Provides smooth scrolling, word wrap, placeholder, character counter, and theme synchronization.
    /// </summary>
    public class ZeroMemoEdit : TextBox
    {
        #region Dependency Properties

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(ZeroMemoEdit),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShowCharacterCountProperty =
            DependencyProperty.Register(nameof(ShowCharacterCount), typeof(bool), typeof(ZeroMemoEdit),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(ZeroMemoEdit),
                new FrameworkPropertyMetadata(new CornerRadius(5)));

        #endregion

        #region Properties

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public bool ShowCharacterCount
        {
            get => (bool)GetValue(ShowCharacterCountProperty);
            set => SetValue(ShowCharacterCountProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        #endregion

        static ZeroMemoEdit()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZeroMemoEdit),
                new FrameworkPropertyMetadata(typeof(ZeroTextBox)));
        }

        public ZeroMemoEdit()
        {
            AcceptsReturn = true;
            AcceptsTab = false;
            TextWrapping = TextWrapping.Wrap;
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;

            MinHeight = 64;
            Padding = new Thickness(10, 8, 10, 8);
            SnapsToDevicePixels = true;

            ZeroWpfTheme.ThemeChanged += () => InvalidateVisual();
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            if (ShowCharacterCount || !string.IsNullOrEmpty(Placeholder))
            {
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            // 1. Placeholder Text when empty
            if (string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(Placeholder) && !IsFocused)
            {
                var ft = new FormattedText(
                    Placeholder,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    ZeroWpfTheme.RegularTypeface,
                    FontSize > 0 ? FontSize : 12.0,
                    ZeroWpfTheme.TextMuted,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                dc.DrawText(ft, new Point(Padding.Left + 2, Padding.Top + 2));
            }

            // 2. Character Counter Badge at bottom-right
            if (ShowCharacterCount)
            {
                int currentLen = Text?.Length ?? 0;
                string counterText = MaxLength > 0 ? $"{currentLen} / {MaxLength}" : $"{currentLen} chars";

                var ft = new FormattedText(
                    counterText,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    ZeroWpfTheme.RegularTypeface,
                    9.5,
                    (MaxLength > 0 && currentLen >= MaxLength) ? ZeroWpfTheme.DangerAccent : ZeroWpfTheme.TextMuted,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                double badgeW = ft.Width + 8;
                double badgeH = ft.Height + 4;
                double badgeX = w - badgeW - 16;
                double badgeY = h - badgeH - 6;

                Brush badgeBg = ZeroWpfTheme.BgHover;
                dc.DrawRoundedRectangle(badgeBg, null, new Rect(badgeX, badgeY, badgeW, badgeH), 3, 3);
                dc.DrawText(ft, new Point(badgeX + 4, badgeY + 2));
            }
        }
    }
}
