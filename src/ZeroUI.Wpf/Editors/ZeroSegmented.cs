using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Core.Input;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Editors
{
    /// <summary>
    /// Modern segmented pill switcher for WPF providing clean, compact view and filter switching.
    /// Backed by headless SelectionModel from ZeroUI.Core with keyboard navigation and zero-allocation index math.
    /// </summary>
    public class ZeroSegmented : FrameworkElement
    {
        private string[] _items = new[] { "All", "Daily", "Weekly", "Monthly" };
        private readonly SelectionModel<string> _selection = new SelectionModel<string> { WrapAround = false };
        private int _hoveredIndex = -1;

        #region Dependency Properties

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(ZeroSegmented),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender, OnSelectedIndexChanged));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(double), typeof(ZeroSegmented),
                new FrameworkPropertyMetadata(6.0, FrameworkPropertyMetadataOptions.AffectsRender));

        #endregion

        #region Properties & Events

        public SelectionModel<string> Selection => _selection;

        public string[] Items
        {
            get => _items;
            set
            {
                _items = value ?? Array.Empty<string>();
                _selection.SetSource(() => _items.Length, idx => _items[idx]);
                if (_selection.SelectedIndex >= _items.Length)
                {
                    _selection.SelectIndex(Math.Max(0, _items.Length - 1));
                }
                InvalidateMeasure();
                InvalidateVisual();
            }
        }

        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        public string? SelectedItem => _selection.SelectedItem;

        public double CornerRadius
        {
            get => (double)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public event EventHandler<int>? SelectedIndexChanged;

        #endregion

        public ZeroSegmented()
        {
            Cursor = Cursors.Hand;
            Focusable = true;
            Height = 32;

            _selection.SetSource(() => _items.Length, idx => _items[idx]);
            _selection.SelectIndex(0);
            _selection.SelectionChanged += (s, e) =>
            {
                if (SelectedIndex != _selection.SelectedIndex)
                {
                    SelectedIndex = _selection.SelectedIndex;
                }
                SelectedIndexChanged?.Invoke(this, _selection.SelectedIndex);
                InvalidateVisual();
            };

            ZeroWpfTheme.ThemeChanged += () => InvalidateVisual();
        }

        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZeroSegmented seg)
            {
                int newIdx = (int)e.NewValue;
                if (seg._selection.SelectedIndex != newIdx)
                {
                    seg._selection.SelectIndex(newIdx);
                }
            }
        }

        #region Input Handling

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int idx = GetIndexAt(e.GetPosition(this).X);
            if (_hoveredIndex != idx)
            {
                _hoveredIndex = idx;
                InvalidateVisual();
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            _hoveredIndex = -1;
            InvalidateVisual();
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.LeftButton == MouseButtonState.Pressed && IsEnabled)
            {
                Focus();
                int idx = GetIndexAt(e.GetPosition(this).X);
                if (idx >= 0 && idx < _items.Length)
                {
                    _selection.SelectIndex(idx);
                }
                e.Handled = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!IsEnabled) return;

            switch (e.Key)
            {
                case Key.Left:
                    _selection.MovePrevious();
                    e.Handled = true;
                    break;
                case Key.Right:
                    _selection.MoveNext();
                    e.Handled = true;
                    break;
                case Key.Home:
                    _selection.MoveFirst();
                    e.Handled = true;
                    break;
                case Key.End:
                    _selection.MoveLast();
                    e.Handled = true;
                    break;
            }
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            InvalidateVisual();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            InvalidateVisual();
        }

        private int GetIndexAt(double x)
        {
            if (_items.Length == 0 || ActualWidth <= 0) return -1;
            double segWidth = ActualWidth / _items.Length;
            int idx = (int)(x / segWidth);
            return Math.Max(0, Math.Min(idx, _items.Length - 1));
        }

        #endregion

        #region Measure & Render

        protected override Size MeasureOverride(Size availableSize)
        {
            double minWidth = Math.Max(200, _items.Length * 65.0);
            double height = Math.Max(30, Height);
            return new Size(
                double.IsPositiveInfinity(availableSize.Width) ? minWidth : Math.Min(minWidth, availableSize.Width),
                double.IsPositiveInfinity(availableSize.Height) ? height : Math.Min(height, availableSize.Height));
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0 || _items.Length == 0) return;

            Rect totalRect = new Rect(0, 0, w, h);
            double radius = CornerRadius;

            // 1. Focus Ring
            if (IsFocused)
            {
                Rect focusRect = new Rect(-2, -2, w + 4, h + 4);
                dc.DrawRoundedRectangle(null, new Pen(ZeroWpfTheme.BorderFocus, 1.5), focusRect, radius + 2, radius + 2);
            }

            // 2. Base Container
            dc.DrawRoundedRectangle(ZeroWpfTheme.BgInput, ZeroWpfTheme.BorderPen, totalRect, radius, radius);

            double segWidth = w / _items.Length;
            int selIdx = _selection.SelectedIndex;

            // 3. Render Segments
            for (int i = 0; i < _items.Length; i++)
            {
                double segX = Math.Round(i * segWidth);
                double curSegW = (i == _items.Length - 1) ? (w - segX) : Math.Round(segWidth);

                bool isSelected = (i == selIdx);
                bool isHovered = (i == _hoveredIndex && !isSelected && IsEnabled);

                // Segment Background
                if (isSelected)
                {
                    Rect pillRect = new Rect(segX + 2, 2, curSegW - 4, h - 4);
                    dc.DrawRoundedRectangle(ZeroWpfTheme.PrimaryAccent, null, pillRect, Math.Max(2, radius - 2), Math.Max(2, radius - 2));
                }
                else if (isHovered)
                {
                    Rect hoverRect = new Rect(segX + 2, 2, curSegW - 4, h - 4);
                    dc.DrawRoundedRectangle(ZeroWpfTheme.BgHover, null, hoverRect, Math.Max(2, radius - 2), Math.Max(2, radius - 2));
                }

                // Vertical Divider between adjacent unselected items
                if (i > 0 && i != selIdx && (i - 1) != selIdx)
                {
                    dc.DrawLine(ZeroWpfTheme.GridLinePen, new Point(segX, 6), new Point(segX, h - 6));
                }

                // Text
                Brush textBrush;
                if (!IsEnabled)
                {
                    textBrush = ZeroWpfTheme.TextMuted;
                }
                else if (isSelected)
                {
                    textBrush = Brushes.White;
                }
                else if (isHovered)
                {
                    textBrush = ZeroWpfTheme.TextPrimary;
                }
                else
                {
                    textBrush = ZeroWpfTheme.TextSecondary;
                }

                var formattedText = new FormattedText(
                    _items[i],
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    isSelected ? ZeroWpfTheme.BoldTypeface : ZeroWpfTheme.RegularTypeface,
                    12.0,
                    textBrush,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                double textX = Math.Round(segX + (curSegW - formattedText.Width) / 2.0);
                double textY = Math.Round((h - formattedText.Height) / 2.0);
                dc.DrawText(formattedText, new Point(textX, textY));
            }
        }

        #endregion
    }
}
