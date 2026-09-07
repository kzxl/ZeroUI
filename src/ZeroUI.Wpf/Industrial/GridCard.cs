using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Industrial
{
    /// <summary>
    /// Modern Fluent Design Card container with header, subtitle, and elevation styling.
    /// </summary>
    public class GridCard : ContentControl
    {
        public static readonly DependencyProperty HeaderTextProperty =
            DependencyProperty.Register(nameof(HeaderText), typeof(string), typeof(GridCard), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty SubtitleTextProperty =
            DependencyProperty.Register(nameof(SubtitleText), typeof(string), typeof(GridCard), new PropertyMetadata(string.Empty));

        public string HeaderText
        {
            get => (string)GetValue(HeaderTextProperty);
            set => SetValue(HeaderTextProperty, value);
        }

        public string SubtitleText
        {
            get => (string)GetValue(SubtitleTextProperty);
            set => SetValue(SubtitleTextProperty, value);
        }

        public GridCard()
        {
            Background = ZeroWpfTheme.BgCard;
            BorderBrush = ZeroWpfTheme.BorderDefault;
            BorderThickness = new Thickness(1);
            Padding = new Thickness(16);

            ZeroWpfTheme.ThemeChanged += () =>
            {
                Background = ZeroWpfTheme.BgCard;
                BorderBrush = ZeroWpfTheme.BorderDefault;
            };
        }
    }

    /// <summary>
    /// Backward-compatibility alias for <see cref="GridCard"/>.
    /// </summary>
    [Obsolete("ZeroCard is deprecated. Use GridCard instead.")]
    public class ZeroCard : GridCard
    {
    }
}
