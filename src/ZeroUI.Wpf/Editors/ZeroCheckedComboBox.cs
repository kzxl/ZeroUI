using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Core.Editors;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Editors
{
    public class CheckedComboItem : INotifyPropertyChanged
    {
        private bool _isChecked;
        private string _text = string.Empty;
        private object _value;

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
                }
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
                }
            }
        }

        public object Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public CheckedComboItem(object value, string text, bool isChecked = false)
        {
            _value = value;
            _text = text;
            _isChecked = isChecked;
        }

        public override string ToString() => Text;
    }

    /// <summary>
    /// Modern anti-aliased Multi-Select CheckedComboBox for ZeroUI WPF.
    /// Features drop-down list with check boxes, "Select All" toggle, live search filter,
    /// and dynamic summary formatting. Implements <see cref="IZeroEditor"/>.
    /// </summary>
    public class CheckedComboBoxEdit : Control, IZeroEditor
    {
        private readonly ObservableCollection<CheckedComboItem> _items = new ObservableCollection<CheckedComboItem>();
        private readonly ObservableCollection<CheckedComboItem> _filteredItems = new ObservableCollection<CheckedComboItem>();

        private Popup? _popup;
        private TextBox? _searchBox;
        private TextBlock? _displayTextBlock;
        private CheckBox? _selectAllBox;
        private ListBox? _listBox;
        private bool _isUpdatingSelectAll = false;

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(CheckedComboBoxEdit), new PropertyMetadata("Select items..."));

        public static readonly DependencyProperty SummaryFormatProperty =
            DependencyProperty.Register(nameof(SummaryFormat), typeof(string), typeof(CheckedComboBoxEdit), new PropertyMetadata("{0} items selected"));

        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register(nameof(IsDropDownOpen), typeof(bool), typeof(CheckedComboBoxEdit), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsDropDownOpenChanged));

        public static readonly DependencyProperty ReadOnlyProperty =
            DependencyProperty.Register(nameof(ReadOnly), typeof(bool), typeof(CheckedComboBoxEdit), new PropertyMetadata(false));

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public string SummaryFormat
        {
            get => (string)GetValue(SummaryFormatProperty);
            set => SetValue(SummaryFormatProperty, value);
        }

        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        public bool ReadOnly
        {
            get => (bool)GetValue(ReadOnlyProperty);
            set => SetValue(ReadOnlyProperty, value);
        }

        public ObservableCollection<CheckedComboItem> Items => _items;

        public IEnumerable<object> CheckedValues => _items.Where(i => i.IsChecked).Select(i => i.Value);

        public object? EditValue
        {
            get => CheckedValues.ToList();
            set
            {
                if (value is IEnumerable enumerable && !(value is string))
                {
                    var set = new HashSet<object>();
                    foreach (var item in enumerable)
                    {
                        if (item != null) set.Add(item);
                    }
                    foreach (var itm in _items)
                    {
                        itm.IsChecked = set.Contains(itm.Value);
                    }
                }
                else if (value == null)
                {
                    Reset();
                }
                else
                {
                    foreach (var itm in _items)
                    {
                        itm.IsChecked = Equals(itm.Value, value);
                    }
                }
                IsModified = true;
                EditValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsModified { get; set; }

        public event EventHandler? SelectionChanged;
        public event EventHandler? EditValueChanged;

        static CheckedComboBoxEdit()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CheckedComboBoxEdit), new FrameworkPropertyMetadata(typeof(CheckedComboBoxEdit)));
        }

        public CheckedComboBoxEdit()
        {
            Background = ZeroWpfTheme.BgInput;
            Foreground = ZeroWpfTheme.TextPrimary;
            BorderBrush = ZeroWpfTheme.BorderDefault;
            BorderThickness = new Thickness(1);
            Height = 36;
            FontSize = 13.0;
            Cursor = Cursors.Hand;

            _items.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (CheckedComboItem item in e.NewItems)
                    {
                        item.PropertyChanged += Item_PropertyChanged;
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (CheckedComboItem item in e.OldItems)
                    {
                        item.PropertyChanged -= Item_PropertyChanged;
                    }
                }
                UpdateFilteredItems();
                UpdateDisplayText();
            };

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
                Padding = new Thickness(10, 0, 10, 0)
            };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24, GridUnitType.Pixel) });

            _displayTextBlock = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = ZeroWpfTheme.TextMuted,
                Text = Placeholder
            };
            Grid.SetColumn(_displayTextBlock, 0);
            headerGrid.Children.Add(_displayTextBlock);

            var arrow = new TextBlock
            {
                Text = "▼",
                FontSize = 9.0,
                Foreground = ZeroWpfTheme.TextSecondary,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(arrow, 1);
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
                Padding = new Thickness(6),
                Width = 260,
                MaxHeight = 320
            };

            var popupStack = new StackPanel();

            _searchBox = new TextBox
            {
                Height = 28,
                Margin = new Thickness(2, 2, 2, 6),
                Background = ZeroWpfTheme.BgInput,
                Foreground = ZeroWpfTheme.TextPrimary,
                BorderBrush = ZeroWpfTheme.BorderDefault,
                Padding = new Thickness(4, 2, 4, 2)
            };
            _searchBox.TextChanged += (s, e) => UpdateFilteredItems();
            popupStack.Children.Add(_searchBox);

            _selectAllBox = new CheckBox
            {
                Content = "(Select All)",
                FontWeight = FontWeights.Bold,
                Foreground = ZeroWpfTheme.TextPrimary,
                Margin = new Thickness(6, 2, 6, 6)
            };
            _selectAllBox.Click += SelectAllBox_Click;
            popupStack.Children.Add(_selectAllBox);

            var separator = new Separator
            {
                Margin = new Thickness(0, 0, 0, 4),
                Background = ZeroWpfTheme.BorderDefault
            };
            popupStack.Children.Add(separator);

            _listBox = new ListBox
            {
                ItemsSource = _filteredItems,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                MaxHeight = 220
            };
            ScrollViewer.SetVerticalScrollBarVisibility(_listBox, ScrollBarVisibility.Auto);

            var itemTemplate = new DataTemplate(typeof(CheckedComboItem));
            var factory = new FrameworkElementFactory(typeof(CheckBox));
            factory.SetBinding(CheckBox.IsCheckedProperty, new System.Windows.Data.Binding(nameof(CheckedComboItem.IsChecked)) { Mode = System.Windows.Data.BindingMode.TwoWay });
            factory.SetBinding(CheckBox.ContentProperty, new System.Windows.Data.Binding(nameof(CheckedComboItem.Text)));
            factory.SetValue(CheckBox.ForegroundProperty, ZeroWpfTheme.TextPrimary);
            factory.SetValue(CheckBox.MarginProperty, new Thickness(4, 3, 4, 3));
            itemTemplate.VisualTree = factory;
            _listBox.ItemTemplate = itemTemplate;

            popupStack.Children.Add(_listBox);
            popupBorder.Child = popupStack;
            _popup.Child = popupBorder;

            rootGrid.Children.Add(_popup);
            AddVisualChild(rootGrid);
            AddLogicalChild(rootGrid);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (ReadOnly) return;

            if (!IsDropDownOpen)
            {
                IsDropDownOpen = true;
                _searchBox?.Focus();
            }
        }

        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CheckedComboBoxEdit cb && cb._popup != null)
            {
                cb._popup.IsOpen = (bool)e.NewValue;
            }
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CheckedComboItem.IsChecked))
            {
                UpdateDisplayText();
                UpdateSelectAllState();
                IsModified = true;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                EditValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void SelectAllBox_Click(object sender, RoutedEventArgs e)
        {
            if (ReadOnly || _isUpdatingSelectAll || _selectAllBox == null) return;

            bool target = _selectAllBox.IsChecked == true;
            foreach (var item in _items)
            {
                item.IsChecked = target;
            }
        }

        private void UpdateSelectAllState()
        {
            if (_selectAllBox == null) return;
            _isUpdatingSelectAll = true;

            int count = _items.Count(i => i.IsChecked);
            if (count == 0) _selectAllBox.IsChecked = false;
            else if (count == _items.Count) _selectAllBox.IsChecked = true;
            else _selectAllBox.IsChecked = null; // Indeterminate

            _isUpdatingSelectAll = false;
        }

        private void UpdateFilteredItems()
        {
            _filteredItems.Clear();
            string query = _searchBox?.Text.Trim() ?? string.Empty;
            foreach (var item in _items)
            {
                if (string.IsNullOrEmpty(query) || item.Text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _filteredItems.Add(item);
                }
            }
        }

        private void UpdateDisplayText()
        {
            if (_displayTextBlock == null) return;

            var checkedList = _items.Where(i => i.IsChecked).Select(i => i.Text).ToList();
            if (checkedList.Count == 0)
            {
                _displayTextBlock.Text = Placeholder;
                _displayTextBlock.Foreground = ZeroWpfTheme.TextMuted;
            }
            else if (checkedList.Count <= 2)
            {
                _displayTextBlock.Text = string.Join(", ", checkedList);
                _displayTextBlock.Foreground = ZeroWpfTheme.TextPrimary;
            }
            else
            {
                _displayTextBlock.Text = string.Format(SummaryFormat, checkedList.Count);
                _displayTextBlock.Foreground = ZeroWpfTheme.TextPrimary;
            }
        }

        public void AddItem(object value, string text, bool isChecked = false)
        {
            _items.Add(new CheckedComboItem(value, text, isChecked));
        }

        public void Reset()
        {
            foreach (var item in _items)
            {
                item.IsChecked = false;
            }
            IsModified = false;
            EditValueChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            _items.Clear();
            IsModified = false;
            EditValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Backward-compatibility alias for <see cref="CheckedComboBoxEdit"/>.
    /// </summary>
    public class ZeroCheckedComboBox : CheckedComboBoxEdit
    {
    }
}
