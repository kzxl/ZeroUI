using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Navigation
{
    public enum AccordionExpandMode
    {
        MultipleGroups,
        SingleGroup
    }

    public class ZeroAccordionItem
    {
        public string Text { get; set; } = string.Empty;
        public string? Glyph { get; set; }
        public string? BadgeText { get; set; }
        public object? Tag { get; set; }

        public event EventHandler? Click;

        public ZeroAccordionItem() { }

        public ZeroAccordionItem(string text, string? glyph = null, EventHandler? onClick = null, string? badge = null)
        {
            Text = text;
            Glyph = glyph;
            BadgeText = badge;
            if (onClick != null) Click += onClick;
        }

        internal void RaiseClick() => Click?.Invoke(this, EventArgs.Empty);
    }

    public class ZeroAccordionGroup
    {
        public string Text { get; set; } = string.Empty;
        public string? Glyph { get; set; }
        public bool IsExpanded { get; set; } = true;
        public string? BadgeText { get; set; }
        public ObservableCollection<ZeroAccordionItem> Items { get; } = new ObservableCollection<ZeroAccordionItem>();
        public object? Tag { get; set; }

        public ZeroAccordionGroup() { }

        public ZeroAccordionGroup(string text, string? glyph = null, bool isExpanded = true)
        {
            Text = text;
            Glyph = glyph;
            IsExpanded = isExpanded;
        }

        public ZeroAccordionItem AddItem(string text, string? glyph = null, EventHandler? onClick = null, string? badge = null)
        {
            var item = new ZeroAccordionItem(text, glyph, onClick, badge);
            Items.Add(item);
            return item;
        }
    }

    /// <summary>
    /// Modern Enterprise Accordion navigation container for ZeroUI WPF.
    /// Supports nested collapsible groups, glyph icons, notification badges,
    /// and SingleGroup or MultipleGroups expansion modes.
    /// </summary>
    public class ZeroAccordion : Control
    {
        private readonly ObservableCollection<ZeroAccordionGroup> _groups = new ObservableCollection<ZeroAccordionGroup>();
        private StackPanel? _groupsStack;
        private AccordionExpandMode _expandMode = AccordionExpandMode.MultipleGroups;

        public ObservableCollection<ZeroAccordionGroup> Groups => _groups;

        public AccordionExpandMode ExpandMode
        {
            get => _expandMode;
            set { _expandMode = value; RebuildUI(); }
        }

        public event EventHandler<ZeroAccordionItem>? ItemClicked;
        public event EventHandler<ZeroAccordionGroup>? GroupToggled;

        static ZeroAccordion()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZeroAccordion), new FrameworkPropertyMetadata(typeof(ZeroAccordion)));
        }

        public ZeroAccordion()
        {
            Background = ZeroWpfTheme.BgCard;
            BorderBrush = ZeroWpfTheme.BorderDefault;
            BorderThickness = new Thickness(1);
            Width = 260;

            _groups.CollectionChanged += (s, e) => RebuildUI();
            BuildVisualTemplate();
        }

        private void BuildVisualTemplate()
        {
            var border = new Border
            {
                Background = Background,
                BorderBrush = BorderBrush,
                BorderThickness = BorderThickness,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(4)
            };

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            _groupsStack = new StackPanel();
            scroll.Content = _groupsStack;

            border.Child = scroll;
            AddVisualChild(border);
            AddLogicalChild(border);

            RebuildUI();
        }

        private void RebuildUI()
        {
            if (_groupsStack == null) return;
            _groupsStack.Children.Clear();

            for (int g = 0; g < _groups.Count; g++)
            {
                var group = _groups[g];

                var groupContainer = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };

                // Group Header
                var headerBorder = new Border
                {
                    Height = 36,
                    CornerRadius = new CornerRadius(4),
                    Background = ZeroWpfTheme.BgInput,
                    BorderBrush = ZeroWpfTheme.BorderDefault,
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand,
                    Padding = new Thickness(8, 0, 8, 0)
                };

                var headerGrid = new Grid();
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24, GridUnitType.Pixel) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20, GridUnitType.Pixel) });

                // Glyph
                var glyphBlock = new TextBlock
                {
                    Text = group.Glyph ?? "📁",
                    FontSize = 13.0,
                    Foreground = ZeroWpfTheme.TextPrimary,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(glyphBlock, 0);
                headerGrid.Children.Add(glyphBlock);

                // Group Title
                var titleBlock = new TextBlock
                {
                    Text = group.Text,
                    FontWeight = FontWeights.Bold,
                    FontSize = 12.5,
                    Foreground = ZeroWpfTheme.TextPrimary,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 4, 0)
                };
                Grid.SetColumn(titleBlock, 1);
                headerGrid.Children.Add(titleBlock);

                // Badge
                if (!string.IsNullOrEmpty(group.BadgeText))
                {
                    var badge = new Border
                    {
                        Background = ZeroWpfTheme.PrimaryAccent,
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(6, 1, 6, 1),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 6, 0)
                    };
                    badge.Child = new TextBlock { Text = group.BadgeText, FontSize = 10.0, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
                    Grid.SetColumn(badge, 2);
                    headerGrid.Children.Add(badge);
                }

                // Chevron
                var chevron = new TextBlock
                {
                    Text = group.IsExpanded ? "▼" : "▶",
                    FontSize = 9.0,
                    Foreground = ZeroWpfTheme.TextSecondary,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(chevron, 3);
                headerGrid.Children.Add(chevron);

                headerBorder.Child = headerGrid;

                // Items Panel
                var itemsPanel = new StackPanel
                {
                    Margin = new Thickness(12, 4, 4, 4),
                    Visibility = group.IsExpanded ? Visibility.Visible : Visibility.Collapsed
                };

                foreach (var item in group.Items)
                {
                    var itemRow = new Border
                    {
                        Height = 30,
                        CornerRadius = new CornerRadius(4),
                        Background = Brushes.Transparent,
                        Cursor = Cursors.Hand,
                        Padding = new Thickness(8, 0, 8, 0),
                        Margin = new Thickness(0, 1, 0, 1)
                    };

                    var itemGrid = new Grid();
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20, GridUnitType.Pixel) });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var iGlyph = new TextBlock
                    {
                        Text = item.Glyph ?? "•",
                        FontSize = 11.0,
                        Foreground = ZeroWpfTheme.TextSecondary,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(iGlyph, 0);
                    itemGrid.Children.Add(iGlyph);

                    var iText = new TextBlock
                    {
                        Text = item.Text,
                        FontSize = 12.0,
                        Foreground = ZeroWpfTheme.TextPrimary,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(iText, 1);
                    itemGrid.Children.Add(iText);

                    itemRow.MouseEnter += (s, e) => itemRow.Background = ZeroWpfTheme.BgHover;
                    itemRow.MouseLeave += (s, e) => itemRow.Background = Brushes.Transparent;
                    itemRow.MouseDown += (s, e) =>
                    {
                        item.RaiseClick();
                        ItemClicked?.Invoke(this, item);
                    };

                    itemRow.Child = itemGrid;
                    itemsPanel.Children.Add(itemRow);
                }

                headerBorder.MouseDown += (s, e) =>
                {
                    if (_expandMode == AccordionExpandMode.SingleGroup && !group.IsExpanded)
                    {
                        foreach (var other in _groups) other.IsExpanded = false;
                    }
                    group.IsExpanded = !group.IsExpanded;
                    chevron.Text = group.IsExpanded ? "▼" : "▶";
                    itemsPanel.Visibility = group.IsExpanded ? Visibility.Visible : Visibility.Collapsed;
                    GroupToggled?.Invoke(this, group);
                };

                groupContainer.Children.Add(headerBorder);
                groupContainer.Children.Add(itemsPanel);
                _groupsStack.Children.Add(groupContainer);
            }
        }

        public ZeroAccordionGroup AddGroup(string text, string? glyph = null, bool isExpanded = true)
        {
            var grp = new ZeroAccordionGroup(text, glyph, isExpanded);
            _groups.Add(grp);
            return grp;
        }
    }
}
