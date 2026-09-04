using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Navigation
{
    public class ZeroSideNavItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = "Nav Item";
        public string Icon { get; set; } = "📌";
        public string Category { get; set; } = string.Empty;
        public int BadgeCount { get; set; } = 0;
        public UIElement? AssociatedView { get; set; }

        public ZeroSideNavItem() { }

        public ZeroSideNavItem(string id, string title, string icon, string category = "", int badgeCount = 0, UIElement? view = null)
        {
            Id = id;
            Title = title;
            Icon = icon;
            Category = category;
            BadgeCount = badgeCount;
            AssociatedView = view;
        }

        public override string ToString() => Title;
    }

    public class ZeroSideNavEventArgs : EventArgs
    {
        public ZeroSideNavItem Item { get; }
        public int Index { get; }

        public ZeroSideNavEventArgs(ZeroSideNavItem item, int index)
        {
            Item = item;
            Index = index;
        }
    }

    /// <summary>
    /// Modern Enterprise Sidebar Navigation control for WPF applications.
    /// Supports brand header, category section grouping, notification badges,
    /// collapsible rail mode (240px ⇄ 64px), and automatic view switching.
    /// </summary>
    public class ZeroSideNav : Control
    {
        private readonly ObservableCollection<ZeroSideNavItem> _items = new ObservableCollection<ZeroSideNavItem>();
        private StackPanel? _itemsStack;
        private Border? _rootBorder;
        private ContentControl? _contentContainer;
        private int _selectedIndex = 0;

        public static readonly DependencyProperty IsCollapsedProperty =
            DependencyProperty.Register(
                nameof(IsCollapsed),
                typeof(bool),
                typeof(ZeroSideNav),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsCollapsedChanged));

        public static readonly DependencyProperty BrandTitleProperty =
            DependencyProperty.Register(nameof(BrandTitle), typeof(string), typeof(ZeroSideNav), new PropertyMetadata("ZeroUI Suite"));

        public static readonly DependencyProperty BrandSubtitleProperty =
            DependencyProperty.Register(nameof(BrandSubtitle), typeof(string), typeof(ZeroSideNav), new PropertyMetadata("Enterprise Station"));

        public static readonly DependencyProperty BrandLogoProperty =
            DependencyProperty.Register(nameof(BrandLogo), typeof(string), typeof(ZeroSideNav), new PropertyMetadata("⚡"));

        public bool IsCollapsed
        {
            get => (bool)GetValue(IsCollapsedProperty);
            set => SetValue(IsCollapsedProperty, value);
        }

        public string BrandTitle
        {
            get => (string)GetValue(BrandTitleProperty);
            set => SetValue(BrandTitleProperty, value);
        }

        public string BrandSubtitle
        {
            get => (string)GetValue(BrandSubtitleProperty);
            set => SetValue(BrandSubtitleProperty, value);
        }

        public string BrandLogo
        {
            get => (string)GetValue(BrandLogoProperty);
            set => SetValue(BrandLogoProperty, value);
        }

        public ContentControl? ContentContainer
        {
            get => _contentContainer;
            set
            {
                _contentContainer = value;
                SwitchToSelectedItem();
            }
        }

        public ObservableCollection<ZeroSideNavItem> Items => _items;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value >= 0 && value < _items.Count && _selectedIndex != value)
                {
                    _selectedIndex = value;
                    SwitchToSelectedItem();
                    RebuildItemsUI();
                    ItemSelected?.Invoke(this, new ZeroSideNavEventArgs(_items[value], value));
                }
            }
        }

        public event EventHandler<ZeroSideNavEventArgs>? ItemSelected;
        public event EventHandler? CollapseChanged;

        static ZeroSideNav()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZeroSideNav), new FrameworkPropertyMetadata(typeof(ZeroSideNav)));
        }

        public ZeroSideNav()
        {
            Background = ZeroWpfTheme.BgCard;
            BorderBrush = ZeroWpfTheme.BorderDefault;
            BorderThickness = new Thickness(0, 0, 1, 0);
            Width = 240;
            HorizontalAlignment = HorizontalAlignment.Left;

            _items.CollectionChanged += (s, e) => RebuildItemsUI();
            BuildVisualTemplate();
        }

        private void BuildVisualTemplate()
        {
            _rootBorder = new Border
            {
                Background = Background,
                BorderBrush = BorderBrush,
                BorderThickness = BorderThickness
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60, GridUnitType.Pixel) }); // Brand Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });   // Items List
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44, GridUnitType.Pixel) }); // Collapse Footer

            // 1. Brand Header
            var brandBorder = new Border
            {
                Background = Brushes.Transparent,
                BorderBrush = ZeroWpfTheme.BorderDefault,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(14, 8, 14, 8)
            };
            var brandGrid = new Grid();
            brandGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36, GridUnitType.Pixel) });
            brandGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var logoBlock = new TextBlock
            {
                Text = BrandLogo,
                FontSize = 18.0,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(logoBlock, 0);
            brandGrid.Children.Add(logoBlock);

            var titlesStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
            var titleBlock = new TextBlock { Text = BrandTitle, FontWeight = FontWeights.Bold, FontSize = 13.0, Foreground = ZeroWpfTheme.TextPrimary };
            var subBlock = new TextBlock { Text = BrandSubtitle, FontSize = 10.5, Foreground = ZeroWpfTheme.TextMuted };
            titlesStack.Children.Add(titleBlock);
            titlesStack.Children.Add(subBlock);
            Grid.SetColumn(titlesStack, 1);
            brandGrid.Children.Add(titlesStack);

            brandBorder.Child = brandGrid;
            Grid.SetRow(brandBorder, 0);
            mainGrid.Children.Add(brandBorder);

            // 2. Items List inside ScrollViewer
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            _itemsStack = new StackPanel { Margin = new Thickness(6, 10, 6, 10) };
            scroll.Content = _itemsStack;
            Grid.SetRow(scroll, 1);
            mainGrid.Children.Add(scroll);

            // 3. Collapse/Expand Toggle Footer
            var footerBorder = new Border
            {
                BorderBrush = ZeroWpfTheme.BorderDefault,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand
            };
            var collapseBtn = new TextBlock
            {
                Text = "◀",
                FontSize = 12.0,
                Foreground = ZeroWpfTheme.TextSecondary,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            footerBorder.Child = collapseBtn;
            footerBorder.MouseDown += (s, e) =>
            {
                IsCollapsed = !IsCollapsed;
                collapseBtn.Text = IsCollapsed ? "▶" : "◀";
            };
            Grid.SetRow(footerBorder, 2);
            mainGrid.Children.Add(footerBorder);

            _rootBorder.Child = mainGrid;
            AddVisualChild(_rootBorder);
            AddLogicalChild(_rootBorder);

            RebuildItemsUI();
        }

        private static void OnIsCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZeroSideNav nav)
            {
                bool collapsed = (bool)e.NewValue;
                nav.Width = collapsed ? 64 : 240;
                nav.RebuildItemsUI();
                nav.CollapseChanged?.Invoke(nav, EventArgs.Empty);
            }
        }

        private void RebuildItemsUI()
        {
            if (_itemsStack == null) return;
            _itemsStack.Children.Clear();

            string lastCategory = string.Empty;

            for (int i = 0; i < _items.Count; i++)
            {
                int index = i;
                var item = _items[i];

                // Category header
                if (!IsCollapsed && !string.IsNullOrEmpty(item.Category) && item.Category != lastCategory)
                {
                    lastCategory = item.Category;
                    var catBlock = new TextBlock
                    {
                        Text = item.Category.ToUpperInvariant(),
                        FontSize = 10.0,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = ZeroWpfTheme.TextMuted,
                        Margin = new Thickness(12, 12, 8, 4)
                    };
                    _itemsStack.Children.Add(catBlock);
                }

                // Nav Item Row
                bool isSelected = (index == _selectedIndex);
                var rowBorder = new Border
                {
                    Height = 38,
                    Margin = new Thickness(2, 2, 2, 2),
                    CornerRadius = new CornerRadius(6),
                    Background = isSelected ? ZeroWpfTheme.PrimaryAccent : Brushes.Transparent,
                    Cursor = Cursors.Hand
                };

                var rowGrid = new Grid();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40, GridUnitType.Pixel) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32, GridUnitType.Pixel) });

                // Icon
                var iconBlock = new TextBlock
                {
                    Text = item.Icon,
                    FontSize = 15.0,
                    Foreground = isSelected ? Brushes.White : ZeroWpfTheme.TextPrimary,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(iconBlock, 0);
                rowGrid.Children.Add(iconBlock);

                if (!IsCollapsed)
                {
                    // Title
                    var titleBlock = new TextBlock
                    {
                        Text = item.Title,
                        FontSize = 12.5,
                        FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Normal,
                        Foreground = isSelected ? Brushes.White : ZeroWpfTheme.TextPrimary,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        Margin = new Thickness(2, 0, 4, 0)
                    };
                    Grid.SetColumn(titleBlock, 1);
                    rowGrid.Children.Add(titleBlock);

                    // Badge
                    if (item.BadgeCount > 0)
                    {
                        var badgeBorder = new Border
                        {
                            Background = isSelected ? Brushes.White : ZeroWpfTheme.PrimaryAccent,
                            CornerRadius = new CornerRadius(8),
                            Height = 16,
                            MinWidth = 16,
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Padding = new Thickness(4, 0, 4, 0)
                        };
                        var badgeText = new TextBlock
                        {
                            Text = item.BadgeCount > 99 ? "99+" : item.BadgeCount.ToString(),
                            FontSize = 9.5,
                            FontWeight = FontWeights.Bold,
                            Foreground = isSelected ? ZeroWpfTheme.PrimaryAccent : Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        badgeBorder.Child = badgeText;
                        Grid.SetColumn(badgeBorder, 2);
                        rowGrid.Children.Add(badgeBorder);
                    }
                }
                else
                {
                    rowBorder.ToolTip = item.Title;
                }

                // Hover effect
                rowBorder.MouseEnter += (s, e) =>
                {
                    if (index != _selectedIndex) rowBorder.Background = ZeroWpfTheme.BgHover;
                };
                rowBorder.MouseLeave += (s, e) =>
                {
                    if (index != _selectedIndex) rowBorder.Background = Brushes.Transparent;
                };

                rowBorder.MouseDown += (s, e) =>
                {
                    SelectedIndex = index;
                };

                rowBorder.Child = rowGrid;
                _itemsStack.Children.Add(rowBorder);
            }
        }

        private void SwitchToSelectedItem()
        {
            if (_contentContainer != null && _selectedIndex >= 0 && _selectedIndex < _items.Count)
            {
                var view = _items[_selectedIndex].AssociatedView;
                if (view != null)
                {
                    _contentContainer.Content = view;
                }
            }
        }

        public void AddItem(string id, string title, string icon, string category = "", int badgeCount = 0, UIElement? view = null)
        {
            _items.Add(new ZeroSideNavItem(id, title, icon, category, badgeCount, view));
            if (_items.Count == 1)
            {
                SwitchToSelectedItem();
            }
        }
    }
}
