using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Docking
{
    public enum ZeroDockPosition
    {
        Left,
        Right,
        Top,
        Bottom,
        Document
    }

    /// <summary>
    /// Represents an individual docking panel with headers, pin toggle, and hosted content.
    /// </summary>
    public class ZeroDockPanel : HeaderedContentControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(ZeroDockPanel), new PropertyMetadata("Panel"));

        public static readonly DependencyProperty DockPositionProperty =
            DependencyProperty.Register(nameof(DockPosition), typeof(ZeroDockPosition), typeof(ZeroDockPanel), new PropertyMetadata(ZeroDockPosition.Document));

        public static readonly DependencyProperty IsPinnedProperty =
            DependencyProperty.Register(nameof(IsPinned), typeof(bool), typeof(ZeroDockPanel), new PropertyMetadata(true));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public ZeroDockPosition DockPosition
        {
            get => (ZeroDockPosition)GetValue(DockPositionProperty);
            set => SetValue(DockPositionProperty, value);
        }

        public bool IsPinned
        {
            get => (bool)GetValue(IsPinnedProperty);
            set => SetValue(IsPinnedProperty, value);
        }

        public ZeroDockPanel()
        {
            Background = ZeroWpfTheme.BgCard;
            BorderBrush = ZeroWpfTheme.BorderDefault;
            BorderThickness = new Thickness(1);
        }
    }

    /// <summary>
    /// Visual Studio-style multi-region dock manager hosting Left, Right, Top, Bottom,
    /// and Document tab panels with interactive splitters and pin toggles.
    /// </summary>
    public class ZeroDockManager : Grid
    {
        private readonly ObservableCollection<ZeroDockPanel> _panels = new ObservableCollection<ZeroDockPanel>();
        private readonly TabControl _documentTabs;
        private readonly ContentControl _leftHost;
        private readonly ContentControl _rightHost;
        private readonly ContentControl _bottomHost;

        public ObservableCollection<ZeroDockPanel> Panels => _panels;

        public ZeroDockManager()
        {
            Background = ZeroWpfTheme.BgPrimary;

            // Define grid structure: Left (Col 0), Splitter (Col 1), Center (Col 2), Splitter (Col 3), Right (Col 4)
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240, GridUnitType.Pixel), MinWidth = 120 });
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Pixel) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Pixel) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260, GridUnitType.Pixel), MinWidth = 120 });

            // Center area has Document Tabs (Row 0), Splitter (Row 1), Bottom (Row 2)
            var centerGrid = new Grid();
            centerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            centerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4, GridUnitType.Pixel) });
            centerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(160, GridUnitType.Pixel), MinHeight = 80 });

            _documentTabs = new TabControl
            {
                Background = ZeroWpfTheme.BgPrimary,
                BorderBrush = ZeroWpfTheme.BorderDefault,
                BorderThickness = new Thickness(1)
            };
            Grid.SetRow(_documentTabs, 0);
            centerGrid.Children.Add(_documentTabs);

            var hSplitter = new GridSplitter
            {
                Height = 4,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Background = ZeroWpfTheme.BorderDefault
            };
            Grid.SetRow(hSplitter, 1);
            centerGrid.Children.Add(hSplitter);

            _bottomHost = new ContentControl { Background = ZeroWpfTheme.BgCard };
            Grid.SetRow(_bottomHost, 2);
            centerGrid.Children.Add(_bottomHost);

            Grid.SetColumn(centerGrid, 2);
            Children.Add(centerGrid);

            // Left Host & Splitter
            _leftHost = new ContentControl { Background = ZeroWpfTheme.BgCard };
            Grid.SetColumn(_leftHost, 0);
            Children.Add(_leftHost);

            var vSplitterLeft = new GridSplitter
            {
                Width = 4,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = ZeroWpfTheme.BorderDefault
            };
            Grid.SetColumn(vSplitterLeft, 1);
            Children.Add(vSplitterLeft);

            // Right Host & Splitter
            _rightHost = new ContentControl { Background = ZeroWpfTheme.BgCard };
            Grid.SetColumn(_rightHost, 4);
            Children.Add(_rightHost);

            var vSplitterRight = new GridSplitter
            {
                Width = 4,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = ZeroWpfTheme.BorderDefault
            };
            Grid.SetColumn(vSplitterRight, 3);
            Children.Add(vSplitterRight);

            _panels.CollectionChanged += (s, e) => RebuildLayout();
        }

        public void AddPanel(ZeroDockPanel panel)
        {
            _panels.Add(panel);
        }

        private void RebuildLayout()
        {
            _documentTabs.Items.Clear();
            _leftHost.Content = null;
            _rightHost.Content = null;
            _bottomHost.Content = null;

            for (int i = 0; i < _panels.Count; i++)
            {
                var p = _panels[i];
                switch (p.DockPosition)
                {
                    case ZeroDockPosition.Document:
                        var tabItem = new TabItem
                        {
                            Header = p.Title,
                            Content = p.Content
                        };
                        _documentTabs.Items.Add(tabItem);
                        break;

                    case ZeroDockPosition.Left:
                        _leftHost.Content = CreateDockWrapper(p);
                        break;

                    case ZeroDockPosition.Right:
                        _rightHost.Content = CreateDockWrapper(p);
                        break;

                    case ZeroDockPosition.Bottom:
                        _bottomHost.Content = CreateDockWrapper(p);
                        break;
                }
            }

            if (_documentTabs.Items.Count > 0 && _documentTabs.SelectedIndex < 0)
            {
                _documentTabs.SelectedIndex = 0;
            }
        }

        private static Border CreateDockWrapper(ZeroDockPanel panel)
        {
            var headerGrid = new Grid
            {
                Background = ZeroWpfTheme.BgCard,
                Height = 28
            };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleBlock = new TextBlock
            {
                Text = panel.Title,
                FontWeight = FontWeights.Bold,
                FontSize = 11.5,
                Foreground = ZeroWpfTheme.TextPrimary,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            Grid.SetColumn(titleBlock, 0);
            headerGrid.Children.Add(titleBlock);

            var pinBtn = new Button
            {
                Content = "📌",
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = ZeroWpfTheme.TextSecondary,
                FontSize = 10,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(pinBtn, 1);
            headerGrid.Children.Add(pinBtn);

            var mainPanel = new DockPanel();
            DockPanel.SetDock(headerGrid, Dock.Top);
            mainPanel.Children.Add(headerGrid);

            var border = new Border
            {
                BorderBrush = ZeroWpfTheme.BorderDefault,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Child = panel.Content as UIElement
            };
            mainPanel.Children.Add(border);

            return new Border
            {
                BorderBrush = ZeroWpfTheme.BorderDefault,
                BorderThickness = new Thickness(1),
                Child = mainPanel
            };
        }
    }
}
