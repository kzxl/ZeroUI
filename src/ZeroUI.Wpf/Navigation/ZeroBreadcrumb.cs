using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Core.Data;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Navigation
{
    /// <summary>
    /// High-performance vector Breadcrumb navigation bar with interactive crumb items and chevrons.
    /// Supports direct path string binding or ObservableCollection of ZeroBreadcrumbItem.
    /// </summary>
    public class ZeroBreadcrumb : FrameworkElement
    {
        private readonly ObservableCollection<ZeroBreadcrumbItem> _items = new ObservableCollection<ZeroBreadcrumbItem>();
        private string _separator = "›";
        private int _hoveredIndex = -1;
        private readonly List<Rect> _crumbBounds = new List<Rect>();

        public ObservableCollection<ZeroBreadcrumbItem> Items => _items;

        public event EventHandler<ZeroBreadcrumbItem>? ItemClicked;
        public event EventHandler<string>? PathChanged;

        public string Separator
        {
            get => _separator;
            set { _separator = value ?? "›"; InvalidateVisual(); }
        }

        public string Path
        {
            get
            {
                var list = new List<string>();
                for (int i = 0; i < _items.Count; i++) list.Add(_items[i].DisplayText);
                return string.Join(" / ", list);
            }
            set
            {
                _items.Clear();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var parts = value.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < parts.Length; i++)
                    {
                        string part = parts[i].Trim();
                        _items.Add(new ZeroBreadcrumbItem(part, part));
                    }
                }
                InvalidateVisual();
                PathChanged?.Invoke(this, Path);
            }
        }

        public ZeroBreadcrumb()
        {
            Height = 32;
            Focusable = true;
            ClipToBounds = true;
            _items.CollectionChanged += OnItemsChanged;
            ZeroWpfTheme.ThemeChanged += OnThemeChanged;
        }

        private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            InvalidateVisual();
        }

        private void OnThemeChanged()
        {
            InvalidateVisual();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(Math.Min(availableSize.Width, 400), 32);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double width = ActualWidth;
            double height = ActualHeight;
            if (width <= 0 || height <= 0) return;

            var dpi = VisualTreeHelper.GetDpi(this);
            _crumbBounds.Clear();

            // Background Card
            dc.DrawRectangle(ZeroWpfTheme.BgCard, null, new Rect(0, 0, width, height));
            dc.DrawLine(ZeroWpfTheme.BorderPen, new Point(0, height - 0.5), new Point(width, height - 0.5));

            double curX = 10.0;
            double centerY = height / 2.0;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                bool isLast = (i == _items.Count - 1);
                bool isHovered = (i == _hoveredIndex);

                var tf = isLast ? ZeroWpfTheme.BoldTypeface : ZeroWpfTheme.RegularTypeface;
                var brush = isLast
                    ? ZeroWpfTheme.PrimaryAccent
                    : (isHovered ? ZeroWpfTheme.TextPrimary : ZeroWpfTheme.TextSecondary);

                var ft = CreateFormattedText(item.DisplayText, tf, 12.0, brush, dpi);

                double crumbWidth = ft.Width + 14;
                Rect crumbRect = new Rect(curX, (height - 22) / 2.0, crumbWidth, 22);
                _crumbBounds.Add(crumbRect);

                // Hover Pill
                if (isHovered && !isLast)
                {
                    dc.DrawRoundedRectangle(ZeroWpfTheme.BgHover, null, crumbRect, 4, 4);
                }

                // Crumb Text
                dc.DrawText(ft, new Point(curX + 7, (height - ft.Height) / 2.0));
                curX += crumbWidth;

                // Separator
                if (!isLast)
                {
                    var sepFt = CreateFormattedText(_separator, ZeroWpfTheme.BoldTypeface, 11.0, ZeroWpfTheme.TextMuted, dpi);
                    dc.DrawText(sepFt, new Point(curX + 3, (height - sepFt.Height) / 2.0));
                    curX += sepFt.Width + 10;
                }
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            Point pt = e.GetPosition(this);

            for (int i = 0; i < _crumbBounds.Count; i++)
            {
                if (_crumbBounds[i].Contains(pt) && i < _items.Count)
                {
                    var clickedItem = _items[i];
                    ItemClicked?.Invoke(this, clickedItem);

                    // Optional path trimming: trim items after clicked index
                    while (_items.Count > i + 1)
                    {
                        _items.RemoveAt(_items.Count - 1);
                    }

                    InvalidateVisual();
                    PathChanged?.Invoke(this, Path);
                    break;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Point pt = e.GetPosition(this);

            int newHovered = -1;
            for (int i = 0; i < _crumbBounds.Count; i++)
            {
                if (_crumbBounds[i].Contains(pt))
                {
                    newHovered = i;
                    break;
                }
            }

            if (_hoveredIndex != newHovered)
            {
                _hoveredIndex = newHovered;
                Cursor = (_hoveredIndex >= 0 && _hoveredIndex < _items.Count - 1) ? Cursors.Hand : Cursors.Arrow;
                InvalidateVisual();
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoveredIndex != -1)
            {
                _hoveredIndex = -1;
                Cursor = Cursors.Arrow;
                InvalidateVisual();
            }
        }

        private static FormattedText CreateFormattedText(string text, Typeface typeface, double fontSize, Brush brush, DpiScale dpi)
        {
            return new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                brush,
                dpi.PixelsPerDip);
        }
    }
}
