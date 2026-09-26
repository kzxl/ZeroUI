using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Core.Input;
using ZeroUI.Wpf.Base;

namespace ZeroUI.Wpf.Input
{
    /// <summary>
    /// WPF touch-screen virtual keyboard supporting QWERTY and Numpad modes
    /// with large touch-friendly keys for industrial panel PCs.
    /// </summary>
    public class ZVirtualKeyboard : WpfControlBase
    {
        public static readonly DependencyProperty LayoutModeProperty =
            DependencyProperty.Register(
                nameof(LayoutMode),
                typeof(VirtualKeyboardLayout),
                typeof(ZVirtualKeyboard),
                new PropertyMetadata(VirtualKeyboardLayout.AlphaNumeric, OnLayoutChanged));

        public static readonly DependencyProperty TargetElementProperty =
            DependencyProperty.Register(
                nameof(TargetElement),
                typeof(UIElement),
                typeof(ZVirtualKeyboard),
                new PropertyMetadata(null));

        public static readonly DependencyProperty KeyHeightProperty =
            DependencyProperty.Register(
                nameof(KeyHeight),
                typeof(double),
                typeof(ZVirtualKeyboard),
                new PropertyMetadata(46.0, OnLayoutChanged));

        public static readonly DependencyProperty KeySpacingProperty =
            DependencyProperty.Register(
                nameof(KeySpacing),
                typeof(double),
                typeof(ZVirtualKeyboard),
                new PropertyMetadata(4.0, OnLayoutChanged));

        public VirtualKeyboardLayout LayoutMode
        {
            get => (VirtualKeyboardLayout)GetValue(LayoutModeProperty);
            set => SetValue(LayoutModeProperty, value);
        }

        public UIElement? TargetElement
        {
            get => (UIElement?)GetValue(TargetElementProperty);
            set => SetValue(TargetElementProperty, value);
        }

        public double KeyHeight
        {
            get => (double)GetValue(KeyHeightProperty);
            set => SetValue(KeyHeightProperty, value);
        }

        public double KeySpacing
        {
            get => (double)GetValue(KeySpacingProperty);
            set => SetValue(KeySpacingProperty, value);
        }

        public event EventHandler<VirtualKeyEventArgs>? KeyPressed;
        public event EventHandler? EnterPressed;
        public event EventHandler? EscapePressed;

        private bool _shiftActive;
        private bool _capsActive;
        private readonly Grid _mainGrid = new Grid();

        static ZVirtualKeyboard()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ZVirtualKeyboard),
                new FrameworkPropertyMetadata(typeof(ZVirtualKeyboard)));
        }

        public ZVirtualKeyboard()
        {
            Width = 680;
            Height = 240;
            Background = new SolidColorBrush(Color.FromRgb(20, 24, 33));
            AddVisualChild(_mainGrid);
            AddLogicalChild(_mainGrid);
            RebuildLayout();
        }

        protected override int VisualChildrenCount => 1;
        protected override Visual GetVisualChild(int index) => _mainGrid;

        protected override Size MeasureOverride(Size constraint)
        {
            _mainGrid.Measure(constraint);
            return _mainGrid.DesiredSize;
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            _mainGrid.Arrange(new Rect(arrangeBounds));
            return arrangeBounds;
        }

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZVirtualKeyboard kb)
            {
                kb.RebuildLayout();
            }
        }

        private void RebuildLayout()
        {
            _mainGrid.Children.Clear();
            _mainGrid.RowDefinitions.Clear();
            _mainGrid.ColumnDefinitions.Clear();
            _mainGrid.Margin = new Thickness(KeySpacing);

            if (LayoutMode == VirtualKeyboardLayout.Numpad)
            {
                BuildNumpad();
            }
            else
            {
                BuildAlphaNumeric();
            }
        }

        private void BuildNumpad()
        {
            int rows = 4;
            int cols = 4;

            for (int r = 0; r < rows; r++)
                _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            for (int c = 0; c < cols; c++)
                _mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            string[,] matrix = new string[,]
            {
                { "7", "8", "9", "Bksp" },
                { "4", "5", "6", "Clear" },
                { "1", "2", "3", "+/-" },
                { "0", ".", "Esc", "Enter" }
            };

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    string label = matrix[r, c];
                    var btn = CreateKeyButton(label, GetKeyType(label));
                    Grid.SetRow(btn, r);
                    Grid.SetColumn(btn, c);
                    _mainGrid.Children.Add(btn);
                }
            }
        }

        private void BuildAlphaNumeric()
        {
            string[][] rows = new string[][]
            {
                new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=", "Bksp" },
                new[] { "Tab", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]" },
                new[] { "Caps", "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'", "Enter" },
                new[] { "Shift", "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/", "Shift" },
                new[] { "?123", "Clear", "Space", "Esc" }
            };

            for (int r = 0; r < rows.Length; r++)
                _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            for (int r = 0; r < rows.Length; r++)
            {
                var rowGrid = new Grid();
                Grid.SetRow(rowGrid, r);

                var items = rows[r];
                foreach (var item in items)
                {
                    double weight = 1.0;
                    if (item == "Space") weight = 4.5;
                    else if (item == "Enter" || item == "Shift") weight = 1.6;
                    else if (item == "Bksp" || item == "Caps" || item == "Clear" || item == "?123" || item == "Esc") weight = 1.3;

                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(weight, GridUnitType.Star) });
                }

                for (int c = 0; c < items.Length; c++)
                {
                    string label = items[c];
                    var btn = CreateKeyButton(label, GetKeyType(label));
                    Grid.SetColumn(btn, c);
                    rowGrid.Children.Add(btn);
                }

                _mainGrid.Children.Add(rowGrid);
            }
        }

        private static VirtualKeyType GetKeyType(string label)
        {
            if (label == "Bksp") return VirtualKeyType.Backspace;
            if (label == "Clear") return VirtualKeyType.Clear;
            if (label == "Enter") return VirtualKeyType.Enter;
            if (label == "Esc") return VirtualKeyType.Escape;
            if (label == "Space") return VirtualKeyType.Space;
            if (label == "Shift" || label == "Caps") return VirtualKeyType.Shift;
            if (label == "Tab") return VirtualKeyType.Tab;
            if (label == "?123") return VirtualKeyType.SymbolToggle;
            return VirtualKeyType.Character;
        }

        private Button CreateKeyButton(string label, VirtualKeyType type)
        {
            var btn = new Button
            {
                Content = label,
                Margin = new Thickness(KeySpacing / 2),
                Focusable = false,
                FontSize = (type != VirtualKeyType.Character && label.Length > 2) ? 12 : 16,
                FontWeight = (type != VirtualKeyType.Character) ? FontWeights.Bold : FontWeights.Normal,
                Foreground = Brushes.White,
                Background = (type == VirtualKeyType.Character || type == VirtualKeyType.Space)
                    ? new SolidColorBrush(Color.FromRgb(32, 38, 52))
                    : new SolidColorBrush(Color.FromRgb(26, 31, 44)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };

            btn.Click += (s, e) => ProcessKey(label, type);
            return btn;
        }

        private void ProcessKey(string label, VirtualKeyType type)
        {
            switch (type)
            {
                case VirtualKeyType.Shift:
                    if (label == "Caps") _capsActive = !_capsActive;
                    else _shiftActive = !_shiftActive;
                    UpdateLabels();
                    return;

                case VirtualKeyType.Backspace:
                    ApplyBackspace();
                    KeyPressed?.Invoke(this, new VirtualKeyEventArgs("", VirtualKeyType.Backspace));
                    return;

                case VirtualKeyType.Clear:
                    ApplyClear();
                    KeyPressed?.Invoke(this, new VirtualKeyEventArgs("", VirtualKeyType.Clear));
                    return;

                case VirtualKeyType.Enter:
                    ApplyEnter();
                    KeyPressed?.Invoke(this, new VirtualKeyEventArgs("\r\n", VirtualKeyType.Enter));
                    EnterPressed?.Invoke(this, EventArgs.Empty);
                    return;

                case VirtualKeyType.Escape:
                    KeyPressed?.Invoke(this, new VirtualKeyEventArgs("", VirtualKeyType.Escape));
                    EscapePressed?.Invoke(this, EventArgs.Empty);
                    return;

                case VirtualKeyType.Space:
                    ApplyText(" ");
                    KeyPressed?.Invoke(this, new VirtualKeyEventArgs(" ", VirtualKeyType.Space));
                    return;

                case VirtualKeyType.Tab:
                    ApplyText("\t");
                    KeyPressed?.Invoke(this, new VirtualKeyEventArgs("\t", VirtualKeyType.Tab));
                    return;

                default:
                    string outText = label;
                    if (outText.Length == 1 && char.IsLetter(outText[0]))
                    {
                        bool upper = _capsActive ^ _shiftActive;
                        outText = upper ? outText.ToUpperInvariant() : outText.ToLowerInvariant();
                        if (_shiftActive) { _shiftActive = false; UpdateLabels(); }
                    }
                    ApplyText(outText);
                    KeyPressed?.Invoke(this, new VirtualKeyEventArgs(outText, type));
                    break;
            }
        }

        private void UpdateLabels()
        {
            // Update child button labels for Shift/Caps state
            foreach (UIElement child in _mainGrid.Children)
            {
                if (child is Button b && b.Content is string s && s.Length == 1 && char.IsLetter(s[0]))
                {
                    bool upper = _capsActive ^ _shiftActive;
                    b.Content = upper ? s.ToUpperInvariant() : s.ToLowerInvariant();
                }
                else if (child is Grid rowGrid)
                {
                    foreach (UIElement rowChild in rowGrid.Children)
                    {
                        if (rowChild is Button rb && rb.Content is string rs && rs.Length == 1 && char.IsLetter(rs[0]))
                        {
                            bool upper = _capsActive ^ _shiftActive;
                            rb.Content = upper ? rs.ToUpperInvariant() : rs.ToLowerInvariant();
                        }
                    }
                }
            }
        }

        private void ApplyText(string text)
        {
            if (TargetElement is TextBox tb)
            {
                int start = tb.SelectionStart;
                int len = tb.SelectionLength;
                tb.Text = tb.Text.Remove(start, len).Insert(start, text);
                tb.SelectionStart = start + text.Length;
                tb.SelectionLength = 0;
            }
            else if (TargetElement is PasswordBox pb)
            {
                pb.Password += text;
            }
        }

        private void ApplyBackspace()
        {
            if (TargetElement is TextBox tb)
            {
                int start = tb.SelectionStart;
                int len = tb.SelectionLength;
                if (len > 0)
                {
                    tb.Text = tb.Text.Remove(start, len);
                    tb.SelectionStart = start;
                }
                else if (start > 0)
                {
                    tb.Text = tb.Text.Remove(start - 1, 1);
                    tb.SelectionStart = start - 1;
                }
            }
            else if (TargetElement is PasswordBox pb && pb.Password.Length > 0)
            {
                pb.Password = pb.Password.Substring(0, pb.Password.Length - 1);
            }
        }

        private void ApplyClear()
        {
            if (TargetElement is TextBox tb) tb.Text = string.Empty;
            else if (TargetElement is PasswordBox pb) pb.Password = string.Empty;
        }

        private void ApplyEnter()
        {
            if (TargetElement is TextBox tb && tb.AcceptsReturn)
            {
                ApplyText("\r\n");
            }
        }

        /// <summary>
        /// Displays a floating touch-screen virtual keyboard popup.
        /// </summary>
        public static Popup ShowFloatingPopup(UIElement target, VirtualKeyboardLayout layout = VirtualKeyboardLayout.AlphaNumeric)
        {
            var popup = new Popup
            {
                PlacementTarget = target,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true
            };

            var kb = new ZVirtualKeyboard
            {
                LayoutMode = layout,
                TargetElement = target,
                Width = (layout == VirtualKeyboardLayout.Numpad) ? 320 : 700,
                Height = (layout == VirtualKeyboardLayout.Numpad) ? 280 : 250
            };

            kb.EscapePressed += (s, e) => popup.IsOpen = false;
            kb.EnterPressed += (s, e) => popup.IsOpen = false;

            popup.Child = kb;
            popup.IsOpen = true;
            return popup;
        }
    }
}
