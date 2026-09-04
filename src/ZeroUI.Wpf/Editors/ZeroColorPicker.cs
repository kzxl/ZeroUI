using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Editors
{
    /// <summary>
    /// Modern anti-aliased Color Picker editor for ZeroUI WPF.
    /// Provides live swatch preview, standard enterprise palette matrix, and HEX input.
    /// </summary>
    public class ZeroColorPicker : Control
    {
        private static readonly Color[] Palette = new[]
        {
            Color.FromRgb(79, 70, 229),   // Indigo (Primary)
            Color.FromRgb(37, 99, 235),   // Blue
            Color.FromRgb(14, 165, 233),  // Sky
            Color.FromRgb(20, 184, 166),  // Teal
            Color.FromRgb(34, 197, 94),   // Green (Success)
            Color.FromRgb(234, 179, 8),   // Yellow (Warning)
            Color.FromRgb(249, 115, 22),  // Orange
            Color.FromRgb(239, 68, 68),   // Red (Danger)
            Color.FromRgb(236, 72, 153),  // Pink
            Color.FromRgb(168, 85, 247),  // Purple
            Color.FromRgb(15, 23, 42),    // Slate Dark
            Color.FromRgb(100, 116, 139), // Slate Muted
            Color.FromRgb(148, 163, 184), // Slate Light
            Color.FromRgb(226, 232, 240), // Slate Border
            Color.FromRgb(255, 255, 255)  // Pure White
        };

        private Border? _swatchBorder;
        private TextBlock? _hexTextBlock;
        private Popup? _popup;
        private TextBox? _hexInputBox;
        private Border? _previewBorder;

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(
                nameof(SelectedColor),
                typeof(Color),
                typeof(ZeroColorPicker),
                new FrameworkPropertyMetadata(Color.FromRgb(79, 70, 229), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));

        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register(
                nameof(IsDropDownOpen),
                typeof(bool),
                typeof(ZeroColorPicker),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsDropDownOpenChanged));

        public Color SelectedColor
        {
            get => (Color)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        public event EventHandler<Color>? ColorChanged;

        static ZeroColorPicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZeroColorPicker), new FrameworkPropertyMetadata(typeof(ZeroColorPicker)));
        }

        public ZeroColorPicker()
        {
            Background = ZeroWpfTheme.BgInput;
            BorderBrush = ZeroWpfTheme.BorderDefault;
            BorderThickness = new Thickness(1);
            Height = 36;
            Width = 150;
            FontSize = 13.0;
            Cursor = Cursors.Hand;

            BuildVisualTemplate();
        }

        private void BuildVisualTemplate()
        {
            var rootGrid = new Grid();

            var border = new Border
            {
                Background = Background,
                BorderBrush = BorderBrush,
                BorderThickness = BorderThickness,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 0, 8, 0)
            };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24, GridUnitType.Pixel) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16, GridUnitType.Pixel) });

            _swatchBorder = new Border
            {
                Width = 20,
                Height = 20,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(SelectedColor),
                BorderBrush = ZeroWpfTheme.BorderDefault,
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_swatchBorder, 0);
            headerGrid.Children.Add(_swatchBorder);

            _hexTextBlock = new TextBlock
            {
                Text = GetHex(SelectedColor),
                Foreground = ZeroWpfTheme.TextPrimary,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            Grid.SetColumn(_hexTextBlock, 1);
            headerGrid.Children.Add(_hexTextBlock);

            var arrow = new TextBlock
            {
                Text = "▼",
                FontSize = 9.0,
                Foreground = ZeroWpfTheme.TextSecondary,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(arrow, 2);
            headerGrid.Children.Add(arrow);

            border.Child = headerGrid;
            rootGrid.Children.Add(border);

            // Popup construction
            _popup = new Popup
            {
                PlacementTarget = this,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true
            };

            var popupBorder = new Border
            {
                Background = ZeroWpfTheme.BgCard,
                BorderBrush = ZeroWpfTheme.BorderDefault,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10),
                Width = 200
            };

            var popupStack = new StackPanel();

            // Swatch Grid (3 rows x 5 cols)
            var swatchGrid = new UniformGrid { Columns = 5, Rows = 3, Margin = new Thickness(0, 0, 0, 10) };
            for (int i = 0; i < Palette.Length; i++)
            {
                var color = Palette[i];
                var btn = new Border
                {
                    Width = 26,
                    Height = 26,
                    Margin = new Thickness(3),
                    CornerRadius = new CornerRadius(3),
                    Background = new SolidColorBrush(color),
                    BorderBrush = ZeroWpfTheme.BorderDefault,
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand
                };
                btn.MouseDown += (s, e) =>
                {
                    SelectedColor = color;
                    IsDropDownOpen = false;
                };
                swatchGrid.Children.Add(btn);
            }
            popupStack.Children.Add(swatchGrid);

            // HEX input and preview
            var bottomGrid = new Grid();
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40, GridUnitType.Pixel) });

            _hexInputBox = new TextBox
            {
                Height = 28,
                Text = GetHex(SelectedColor),
                Background = ZeroWpfTheme.BgInput,
                Foreground = ZeroWpfTheme.TextPrimary,
                BorderBrush = ZeroWpfTheme.BorderDefault,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(4, 0, 4, 0)
            };
            _hexInputBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    TryApplyHex(_hexInputBox.Text);
                    IsDropDownOpen = false;
                }
            };
            Grid.SetColumn(_hexInputBox, 0);
            bottomGrid.Children.Add(_hexInputBox);

            _previewBorder = new Border
            {
                Width = 32,
                Height = 28,
                Margin = new Thickness(8, 0, 0, 0),
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(SelectedColor),
                BorderBrush = ZeroWpfTheme.BorderDefault,
                BorderThickness = new Thickness(1)
            };
            Grid.SetColumn(_previewBorder, 1);
            bottomGrid.Children.Add(_previewBorder);

            popupStack.Children.Add(bottomGrid);
            popupBorder.Child = popupStack;
            _popup.Child = popupBorder;

            rootGrid.Children.Add(_popup);
            AddVisualChild(rootGrid);
            AddLogicalChild(rootGrid);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (!IsDropDownOpen)
            {
                IsDropDownOpen = true;
                if (_hexInputBox != null)
                {
                    _hexInputBox.Text = GetHex(SelectedColor);
                    _hexInputBox.Focus();
                    _hexInputBox.SelectAll();
                }
            }
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZeroColorPicker cp)
            {
                Color color = (Color)e.NewValue;
                if (cp._swatchBorder != null) cp._swatchBorder.Background = new SolidColorBrush(color);
                if (cp._previewBorder != null) cp._previewBorder.Background = new SolidColorBrush(color);
                if (cp._hexTextBlock != null) cp._hexTextBlock.Text = GetHex(color);
                if (cp._hexInputBox != null) cp._hexInputBox.Text = GetHex(color);
                cp.ColorChanged?.Invoke(cp, color);
            }
        }

        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZeroColorPicker cp && cp._popup != null)
            {
                cp._popup.IsOpen = (bool)e.NewValue;
            }
        }

        private void TryApplyHex(string hex)
        {
            try
            {
                hex = hex.Trim().TrimStart('#');
                if (hex.Length == 6)
                {
                    byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                    byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                    byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                    SelectedColor = Color.FromRgb(r, g, b);
                }
            }
            catch { }
        }

        private static string GetHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}
