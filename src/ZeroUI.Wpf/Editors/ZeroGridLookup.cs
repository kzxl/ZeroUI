using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Core.Data;
using ZeroUI.Wpf.DataGrid;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Editors
{
    /// <summary>
    /// Enterprise Multi-Column GridLookup dropdown editor for ZeroUI WPF.
    /// Hosts an embedded virtual DataGrid within a Popup, enabling multi-column search,
    /// pagination, and instant selection for complex enterprise entities.
    /// </summary>
    public class ZeroGridLookup : Control
    {
        private Popup? _popup;
        private TextBox? _searchBox;
        private ZeroGridControl? _grid;
        private TextBlock? _displayTextBlock;

        private string _selectedText = string.Empty;
        private object? _selectedValue = null;
        private string _displayMember = "Name";
        private string _valueMember = "Id";

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(ZeroGridLookup), new PropertyMetadata("Click to search and select..."));

        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register(
                nameof(IsDropDownOpen),
                typeof(bool),
                typeof(ZeroGridLookup),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsDropDownOpenChanged));

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        public string DisplayMember
        {
            get => _displayMember;
            set => _displayMember = value;
        }

        public string ValueMember
        {
            get => _valueMember;
            set => _valueMember = value;
        }

        public string SelectedText => _selectedText;
        public object? SelectedValue => _selectedValue;
        public ZeroGridControl? GridControl => _grid;

        public event EventHandler? SelectionChanged;

        static ZeroGridLookup()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZeroGridLookup), new FrameworkPropertyMetadata(typeof(ZeroGridLookup)));
        }

        public ZeroGridLookup()
        {
            Background = ZeroWpfTheme.BgInput;
            BorderBrush = ZeroWpfTheme.BorderDefault;
            BorderThickness = new Thickness(1);
            Height = 36;
            Width = 260;
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
                Padding = new Thickness(8, 0, 8, 0)
            };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20, GridUnitType.Pixel) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16, GridUnitType.Pixel) });

            var iconBlock = new TextBlock
            {
                Text = "⊞",
                FontSize = 13.0,
                Foreground = ZeroWpfTheme.TextSecondary,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(iconBlock, 0);
            headerGrid.Children.Add(iconBlock);

            _displayTextBlock = new TextBlock
            {
                Text = Placeholder,
                Foreground = ZeroWpfTheme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(6, 0, 4, 0)
            };
            Grid.SetColumn(_displayTextBlock, 1);
            headerGrid.Children.Add(_displayTextBlock);

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

            // Popup construction hosting search bar & DataGrid
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
                Padding = new Thickness(8),
                Width = 540,
                Height = 340
            };

            var popupGrid = new Grid();
            popupGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36, GridUnitType.Pixel) });
            popupGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            _searchBox = new TextBox
            {
                Height = 28,
                Background = ZeroWpfTheme.BgInput,
                Foreground = ZeroWpfTheme.TextPrimary,
                BorderBrush = ZeroWpfTheme.BorderDefault,
                Padding = new Thickness(4, 2, 4, 2),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(_searchBox, 0);
            popupGrid.Children.Add(_searchBox);

            _grid = new ZeroGridControl
            {
                Margin = new Thickness(0, 6, 0, 0)
            };
            _grid.MouseDown += (s, e) => { if (e.ClickCount == 2) CommitSelection(); };
            Grid.SetRow(_grid, 1);
            popupGrid.Children.Add(_grid);

            popupBorder.Child = popupGrid;
            _popup.Child = popupBorder;

            rootGrid.Children.Add(_popup);
            AddVisualChild(rootGrid);
            AddLogicalChild(rootGrid);
        }

        public void SetDataSource<T>(IList<T> items)
        {
            _grid?.SetDataSource(items);
        }

        private void CommitSelection()
        {
            if (_grid == null || _grid.DataSource == null) return;
            int visualRow = _grid.SelectedIndex;
            if (visualRow < 0) return;

            int modelRow = _grid.GetModelRowIndex(visualRow);
            if (modelRow < 0) modelRow = visualRow;

            CellValueBuffer buf = new CellValueBuffer();
            _grid.DataSource.GetCellValue(modelRow, 0, ref buf);
            _selectedText = buf.Text.ToString();
            _selectedValue = modelRow;

            if (_displayTextBlock != null)
            {
                _displayTextBlock.Text = _selectedText;
                _displayTextBlock.Foreground = ZeroWpfTheme.TextPrimary;
            }

            IsDropDownOpen = false;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (!IsDropDownOpen)
            {
                IsDropDownOpen = true;
                _searchBox?.Focus();
            }
        }

        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZeroGridLookup gl && gl._popup != null)
            {
                gl._popup.IsOpen = (bool)e.NewValue;
            }
        }
    }
}
