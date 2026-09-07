using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Overlays
{
    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }

    [Obsolete("WpfToastType is deprecated. Use ToastType instead.")]
    public enum WpfToastType
    {
        Info = ToastType.Info,
        Success = ToastType.Success,
        Warning = ToastType.Warning,
        Error = ToastType.Error
    }

    /// <summary>
    /// Modern lightweight non-blocking floating Toast notification for ZeroUI WPF.
    /// Slides in from the top-right corner, displays state glyph, and automatically fades out.
    /// </summary>
    public class ToastNotification : Window
    {
        private readonly DispatcherTimer _stayTimer;

        public ToastNotification(Window owner, string message, ToastType type = ToastType.Info, int durationMs = 3000)
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            Topmost = true;
            SizeToContent = SizeToContent.WidthAndHeight;

            Color accent = type switch
            {
                ToastType.Success => Color.FromRgb(34, 197, 94),
                ToastType.Warning => Color.FromRgb(234, 179, 8),
                ToastType.Error => Color.FromRgb(239, 68, 68),
                _ => Color.FromRgb(79, 70, 229)
            };

            string icon = type switch
            {
                ToastType.Success => "✔",
                ToastType.Warning => "⚠",
                ToastType.Error => "✕",
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
                MaxWidth = 420,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 14,
                    Color = Colors.Black,
                    Opacity = 0.28,
                    ShadowDepth = 3
                }
            };

            var stack = new StackPanel { Orientation = Orientation.Horizontal };

            var iconBlock = new TextBlock
            {
                Text = icon,
                FontWeight = FontWeights.Bold,
                FontSize = 14.0,
                Foreground = new SolidColorBrush(accent),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            stack.Children.Add(iconBlock);

            var textBlock = new TextBlock
            {
                Text = message,
                FontSize = 12.5,
                Foreground = ZeroWpfTheme.TextPrimary,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            stack.Children.Add(textBlock);

            border.Child = stack;
            Content = border;

            Loaded += (s, e) =>
            {
                if (owner != null && owner.IsLoaded)
                {
                    Left = owner.Left + owner.ActualWidth - ActualWidth - 24;
                    Top = owner.Top + 32;
                }
                else
                {
                    Left = SystemParameters.WorkArea.Right - ActualWidth - 24;
                    Top = SystemParameters.WorkArea.Top + 32;
                }

                var anim = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(180));
                BeginAnimation(OpacityProperty, anim);
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

        public static void Show(Window owner, string message, ToastType type = ToastType.Info, int durationMs = 3000)
        {
            var toast = new ToastNotification(owner, message, type, durationMs);
            toast.Show();
        }

        public static void Success(Window owner, string message, int durationMs = 3000) => Show(owner, message, ToastType.Success, durationMs);
        public static void Warning(Window owner, string message, int durationMs = 3500) => Show(owner, message, ToastType.Warning, durationMs);
        public static void Error(Window owner, string message, int durationMs = 4000) => Show(owner, message, ToastType.Error, durationMs);
        public static void Info(Window owner, string message, int durationMs = 3000) => Show(owner, message, ToastType.Info, durationMs);
    }

    /// <summary>
    /// Backward-compatibility alias for <see cref="ToastNotification"/>.
    /// </summary>
    [Obsolete("ZeroToast is deprecated. Use ToastNotification instead.")]
    public class ZeroToast : ToastNotification
    {
        public ZeroToast(Window owner, string message, ToastType type, int durationMs = 3000)
            : base(owner, message, type, durationMs) { }

        public ZeroToast(Window owner, string message, WpfToastType type, int durationMs = 3000)
            : base(owner, message, (ToastType)type, durationMs) { }
    }
}
