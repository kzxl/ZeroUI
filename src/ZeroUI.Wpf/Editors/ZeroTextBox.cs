using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ZeroUI.Wpf.Editors
{
    /// <summary>
    /// Enhanced Fluent TextBox with placeholder / watermark text, leading/trailing icons,
    /// 1-click clear button, and animated focus borders.
    /// </summary>
    public class ZeroTextBox : TextBox
    {
        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(ZeroTextBox),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty LeadingTextProperty =
            DependencyProperty.Register(nameof(LeadingText), typeof(string), typeof(ZeroTextBox),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ShowClearButtonProperty =
            DependencyProperty.Register(nameof(ShowClearButton), typeof(bool), typeof(ZeroTextBox),
                new PropertyMetadata(true));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(ZeroTextBox),
                new PropertyMetadata(new CornerRadius(5)));

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public string LeadingText
        {
            get => (string)GetValue(LeadingTextProperty);
            set => SetValue(LeadingTextProperty, value);
        }

        public bool ShowClearButton
        {
            get => (bool)GetValue(ShowClearButtonProperty);
            set => SetValue(ShowClearButtonProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        static ZeroTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZeroTextBox),
                new FrameworkPropertyMetadata(typeof(ZeroTextBox)));
        }

        public ZeroTextBox()
        {
            Height = 32;
            VerticalContentAlignment = VerticalAlignment.Center;
            Padding = new Thickness(10, 0, 10, 0);
            SnapsToDevicePixels = true;
        }
    }
}
