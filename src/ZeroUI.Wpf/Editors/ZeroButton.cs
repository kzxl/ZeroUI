using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Editors
{
    public enum ZeroButtonVariant
    {
        Primary,
        Secondary,
        Success,
        Danger,
        Ghost
    }

    /// <summary>
    /// Modern styled button adhering to AgentOption WPF UI standards.
    /// </summary>
    public class SimpleButton : Button
    {
        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(ZeroButtonVariant),
                typeof(SimpleButton),
                new FrameworkPropertyMetadata(ZeroButtonVariant.Primary, OnVariantChanged));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(SimpleButton),
                new FrameworkPropertyMetadata(new CornerRadius(6)));

        public ZeroButtonVariant Variant
        {
            get => (ZeroButtonVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public SimpleButton()
        {
            Height = 32;
            Padding = new Thickness(14, 6, 14, 6);
            FontSize = 12.5;
            FontWeight = FontWeights.SemiBold;
            Cursor = Cursors.Hand;
            Style = ZeroWpfStyles.ButtonStyle;
            ApplyStyle();

            ZeroWpfTheme.ThemeChanged += ApplyStyle;
        }

        private static void OnVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SimpleButton btn) btn.ApplyStyle();
        }

        private void ApplyStyle()
        {
            switch (Variant)
            {
                case ZeroButtonVariant.Primary:
                    Background = ZeroWpfTheme.PrimaryAccent;
                    Foreground = Brushes.White;
                    BorderBrush = ZeroWpfTheme.PrimaryAccentDark;
                    BorderThickness = new Thickness(1);
                    break;
                case ZeroButtonVariant.Secondary:
                    Background = ZeroWpfTheme.BgInput;
                    Foreground = ZeroWpfTheme.TextPrimary;
                    BorderBrush = ZeroWpfTheme.BorderDefault;
                    BorderThickness = new Thickness(1);
                    break;
                case ZeroButtonVariant.Success:
                    Background = ZeroWpfTheme.SuccessAccent;
                    Foreground = ZeroWpfTheme.IsDark ? Brushes.Black : Brushes.White;
                    BorderBrush = Brushes.Transparent;
                    BorderThickness = new Thickness(0);
                    break;
                case ZeroButtonVariant.Danger:
                    Background = ZeroWpfTheme.DangerAccent;
                    Foreground = Brushes.White;
                    BorderBrush = Brushes.Transparent;
                    BorderThickness = new Thickness(0);
                    break;
                case ZeroButtonVariant.Ghost:
                    Background = Brushes.Transparent;
                    Foreground = ZeroWpfTheme.TextPrimary;
                    BorderBrush = Brushes.Transparent;
                    BorderThickness = new Thickness(0);
                    break;
            }
        }
    }

    /// <summary>
    /// Legacy alias for SimpleButton.
    /// Preserved for 100% backward compatibility.
    /// </summary>
    public class ZeroButton : SimpleButton
    {
    }
}
