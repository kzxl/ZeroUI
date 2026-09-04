using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Overlays
{
    public enum WpfToastType
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Modern lightweight non-blocking floating Toast notification for ZeroUI WPF.
    /// Slides in from the top-right corner, displays state glyph, and automatically fades out.
    /// </summary>
    public sealed class ZeroToast : Window
    {
        private readonly DispatcherTimer _stayTimer;

        private ZeroToast(Window owner, string message, WpfToastType type, int durationMs = 3000)
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            Topmost = true;
            SizeToContent = SizeToContent.WidthAndHeight;

            Color accent = type switch
            {
                WpfToastType.Success => Color.FromRgb(34, 197, 94),
                WpfToastType.Warning => Color.FromRgb(234, 179, 8),
                WpfToastType.Error => Color.FromRgb(239, 68, 68),
                _ => Color.FromRgb(79, 70, 229)
            };

            string icon = type switch
            {
                WpfToastType.Success => "✔",
                WpfToastType.Warning => "⚠",
                WpfToastType.Error => "✕",
                _ => "ℹ"
            };

            var border = new Border
            {
                Background = ZeroWpfTheme.BgCard,
                BorderBrush = new SolidColorBrush(accent),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(14, 10, 16, 10),
                MinWidth = 260,
                MaxWidth = 420
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var iconBlock = new TextBlock
            {
                Text = icon,
                FontSize = 14.0,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(accent),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetColumn(iconBlock, 0);
            grid.Children.Add(iconBlock);

            var textBlock = new TextBlock
            {
                Text = message,
                FontSize = 12.5,
                Foreground = ZeroWpfTheme.TextPrimary,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(textBlock, 1);
            grid.Children.Add(textBlock);

            border.Child = grid;
            Content = border;

            // Position at top-right of owner
            Loaded += (s, e) =>
            {
                if (owner != null && owner.IsLoaded)
                {
                    Left = owner.Left + owner.ActualWidth - ActualWidth - 24;
                    Top = owner.Top + 40;
                }
                else
                {
                    Left = SystemParameters.WorkArea.Right - ActualWidth - 24;
                    Top = SystemParameters.WorkArea.Top + 40;
                }

                // Fade-in animation
                var fadeIn = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(200));
                BeginAnimation(OpacityProperty, fadeIn);
            };

            MouseDown += (s, e) => Dismiss();

            _stayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Max(1000, durationMs)) };
            _stayTimer.Tick += (s, e) =>
            {
                _stayTimer.Stop();
                Dismiss();
            };
            _stayTimer.Start();
        }

        private void Dismiss()
        {
            var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(200));
            fadeOut.Completed += (s, e) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }

        public static void Show(Window owner, string message, WpfToastType type = WpfToastType.Info, int durationMs = 3000)
        {
            var toast = new ZeroToast(owner, message, type, durationMs);
            toast.Show();
        }

        public static void Success(Window owner, string message, int durationMs = 3000) => Show(owner, message, WpfToastType.Success, durationMs);
        public static void Warning(Window owner, string message, int durationMs = 3500) => Show(owner, message, WpfToastType.Warning, durationMs);
        public static void Error(Window owner, string message, int durationMs = 4000) => Show(owner, message, WpfToastType.Error, durationMs);
        public static void Info(Window owner, string message, int durationMs = 3000) => Show(owner, message, WpfToastType.Info, durationMs);
    }
}
