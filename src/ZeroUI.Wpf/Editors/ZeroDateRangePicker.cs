using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Core.Input.Date;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Editors
{
    public enum DateRangePreset
    {
        Custom,
        Today,
        Yesterday,
        Last7Days,
        Last30Days,
        ThisMonth,
        LastMonth,
        YearToDate
    }

    /// <summary>
    /// Enterprise dual-date range selector (From Date -> To Date) for WPF with connected range ribbon,
    /// 1-click quick preset filters, interactive hover range preview, and calendar popup powered by CalendarModel.
    /// </summary>
    public class ZeroDateRangePicker : Control
    {
        private readonly Border _containerBorder;
        private readonly TextBlock _rangeDisplayBlock;
        private readonly Button _calendarTriggerButton;
        private readonly Popup _calendarPopup;
        private readonly DateRangePopupContent _popupContent;

        #region Dependency Properties

        public static readonly DependencyProperty StartDateProperty =
            DependencyProperty.Register(nameof(StartDate), typeof(DateTime), typeof(ZeroDateRangePicker),
                new FrameworkPropertyMetadata(DateTime.Today.AddDays(-6), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDateRangeChanged));

        public static readonly DependencyProperty EndDateProperty =
            DependencyProperty.Register(nameof(EndDate), typeof(DateTime), typeof(ZeroDateRangePicker),
                new FrameworkPropertyMetadata(DateTime.Today, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDateRangeChanged));

        public static readonly DependencyProperty DateFormatProperty =
            DependencyProperty.Register(nameof(DateFormat), typeof(string), typeof(ZeroDateRangePicker),
                new FrameworkPropertyMetadata("yyyy-MM-dd", OnDateFormatChanged));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(ZeroDateRangePicker),
                new FrameworkPropertyMetadata(new CornerRadius(6)));

        public static readonly DependencyProperty ShowPresetsProperty =
            DependencyProperty.Register(nameof(ShowPresets), typeof(bool), typeof(ZeroDateRangePicker),
                new FrameworkPropertyMetadata(true, (d, e) => ((ZeroDateRangePicker)d)._popupContent.Refresh()));

        #endregion

        #region Properties & Events

        public DateTime StartDate
        {
            get => (DateTime)GetValue(StartDateProperty);
            set => SetValue(StartDateProperty, value);
        }

        public DateTime EndDate
        {
            get => (DateTime)GetValue(EndDateProperty);
            set => SetValue(EndDateProperty, value);
        }

        public string DateFormat
        {
            get => (string)GetValue(DateFormatProperty);
            set => SetValue(DateFormatProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public bool ShowPresets
        {
            get => (bool)GetValue(ShowPresetsProperty);
            set => SetValue(ShowPresetsProperty, value);
        }

        public event EventHandler<(DateTime Start, DateTime End)>? DateRangeChanged;

        #endregion

        public ZeroDateRangePicker()
        {
            Height = 32;
            Focusable = true;
            Cursor = Cursors.Hand;

            _containerBorder = new Border
            {
                CornerRadius = CornerRadius,
                BorderThickness = new Thickness(1),
                SnapsToDevicePixels = true,
                Padding = new Thickness(10, 0, 6, 0)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });

            _rangeDisplayBlock = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12.5,
                FontWeight = FontWeights.Medium
            };
            UpdateDisplayText();

            Grid.SetColumn(_rangeDisplayBlock, 0);
            grid.Children.Add(_rangeDisplayBlock);

            _calendarTriggerButton = new Button
            {
                Content = "📅",
                FontSize = 12,
                Width = 22,
                Height = 22,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _calendarTriggerButton.Click += (s, e) => TogglePopup();
            Grid.SetColumn(_calendarTriggerButton, 1);
            grid.Children.Add(_calendarTriggerButton);

            _containerBorder.Child = grid;
            AddVisualChild(_containerBorder);

            _popupContent = new DateRangePopupContent(this);
            _calendarPopup = new Popup
            {
                PlacementTarget = _containerBorder,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true,
                Child = _popupContent
            };

            ZeroWpfTheme.ThemeChanged += () => UpdateThemeColors(isFocused: IsFocused);
            UpdateThemeColors(isFocused: false);
        }

        private static void OnDateRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZeroDateRangePicker picker)
            {
                picker.UpdateDisplayText();
                picker.DateRangeChanged?.Invoke(picker, (picker.StartDate, picker.EndDate));
            }
        }

        private static void OnDateFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZeroDateRangePicker picker)
            {
                picker.UpdateDisplayText();
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (IsEnabled && e.LeftButton == MouseButtonState.Pressed)
            {
                Focus();
                TogglePopup();
                e.Handled = true;
            }
        }

        public void TogglePopup()
        {
            _calendarPopup.IsOpen = !_calendarPopup.IsOpen;
            if (_calendarPopup.IsOpen)
            {
                _popupContent.Refresh();
            }
        }

        public void ClosePopup()
        {
            _calendarPopup.IsOpen = false;
        }

        public void SetRange(DateTime start, DateTime end)
        {
            if (start > end)
            {
                (start, end) = (end, start);
            }

            StartDate = start.Date;
            EndDate = end.Date;
            UpdateDisplayText();
        }

        private void UpdateDisplayText()
        {
            _rangeDisplayBlock.Text = $"{StartDate.ToString(DateFormat)}   ➜   {EndDate.ToString(DateFormat)}";
        }

        private void UpdateThemeColors(bool isFocused)
        {
            _containerBorder.Background = ZeroWpfTheme.BgInput;
            _containerBorder.BorderBrush = isFocused ? ZeroWpfTheme.BorderFocus : ZeroWpfTheme.BorderDefault;
            _rangeDisplayBlock.Foreground = ZeroWpfTheme.TextPrimary;
            _calendarTriggerButton.Foreground = ZeroWpfTheme.TextSecondary;
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            UpdateThemeColors(true);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            UpdateThemeColors(false);
        }

        #region Visual Tree Overrides

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            return _containerBorder;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            _containerBorder.Measure(constraint);
            return _containerBorder.DesiredSize;
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            _containerBorder.Arrange(new Rect(arrangeBounds));
            return arrangeBounds;
        }

        #endregion

        #region Date Range Popup Content

        private class DateRangePopupContent : FrameworkElement
        {
            private readonly ZeroDateRangePicker _owner;
            private readonly CalendarModel _calendarModel;
            private readonly CalendarDayCell[] _cells = new CalendarDayCell[CalendarModel.TotalCells];
            private DateTime? _pendingStartDate;
            private int _hoveredCellIndex = -1;
            private int _hoveredPresetIndex = -1;
            private bool _hoveredPrevMonth = false;
            private bool _hoveredNextMonth = false;

            private static readonly (string Label, DateRangePreset Preset)[] Presets = new[]
            {
                ("Today", DateRangePreset.Today),
                ("Yesterday", DateRangePreset.Yesterday),
                ("Last 7 Days", DateRangePreset.Last7Days),
                ("Last 30 Days", DateRangePreset.Last30Days),
                ("This Month", DateRangePreset.ThisMonth),
                ("Last Month", DateRangePreset.LastMonth),
                ("Year to Date", DateRangePreset.YearToDate)
            };

            public DateRangePopupContent(ZeroDateRangePicker owner)
            {
                _owner = owner;
                _calendarModel = new CalendarModel(owner.StartDate);
                Width = 380;
                Height = 310;
                Cursor = Cursors.Hand;
            }

            public void Refresh()
            {
                _pendingStartDate = null;
                _calendarModel.ViewDate = new DateTime(_owner.StartDate.Year, _owner.StartDate.Month, 1);
                _calendarModel.FillDaysGrid(_cells);
                InvalidateVisual();
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                Point pt = e.GetPosition(this);

                // Sidebar presets: X <= 100
                int hovPreset = -1;
                if (pt.X >= 8 && pt.X <= 96 && pt.Y >= 36)
                {
                    int pIdx = (int)((pt.Y - 36) / 28);
                    if (pIdx >= 0 && pIdx < Presets.Length) hovPreset = pIdx;
                }

                // Calendar navigation chevrons
                bool hovPrev = pt.X >= 115 && pt.X <= 135 && pt.Y >= 10 && pt.Y <= 30;
                bool hovNext = pt.X >= (ActualWidth - 30) && pt.X <= (ActualWidth - 10) && pt.Y >= 10 && pt.Y <= 30;

                // Calendar 42-day matrix: X >= 106
                int hovCell = -1;
                double calLeft = 106;
                double calTop = 64;
                double cellW = (ActualWidth - calLeft - 10) / 7.0;
                double cellH = 28;

                if (pt.X >= calLeft && pt.X <= ActualWidth - 10 && pt.Y >= calTop && pt.Y < calTop + (6 * cellH))
                {
                    int col = (int)((pt.X - calLeft) / cellW);
                    int row = (int)((pt.Y - calTop) / cellH);
                    if (col >= 0 && col < 7 && row >= 0 && row < 6)
                    {
                        hovCell = (row * 7) + col;
                    }
                }

                if (_hoveredPresetIndex != hovPreset || _hoveredPrevMonth != hovPrev ||
                    _hoveredNextMonth != hovNext || _hoveredCellIndex != hovCell)
                {
                    _hoveredPresetIndex = hovPreset;
                    _hoveredPrevMonth = hovPrev;
                    _hoveredNextMonth = hovNext;
                    _hoveredCellIndex = hovCell;
                    InvalidateVisual();
                }
            }

            protected override void OnMouseLeave(MouseEventArgs e)
            {
                base.OnMouseLeave(e);
                _hoveredPresetIndex = -1;
                _hoveredPrevMonth = false;
                _hoveredNextMonth = false;
                _hoveredCellIndex = -1;
                InvalidateVisual();
            }

            protected override void OnMouseDown(MouseButtonEventArgs e)
            {
                base.OnMouseDown(e);
                Point pt = e.GetPosition(this);

                if (_hoveredPrevMonth)
                {
                    _calendarModel.NavigatePreviousMonth();
                    _calendarModel.FillDaysGrid(_cells);
                    InvalidateVisual();
                    e.Handled = true;
                    return;
                }

                if (_hoveredNextMonth)
                {
                    _calendarModel.NavigateNextMonth();
                    _calendarModel.FillDaysGrid(_cells);
                    InvalidateVisual();
                    e.Handled = true;
                    return;
                }

                if (_hoveredPresetIndex >= 0)
                {
                    ApplyPreset(Presets[_hoveredPresetIndex].Preset);
                    _owner.ClosePopup();
                    e.Handled = true;
                    return;
                }

                if (_hoveredCellIndex >= 0 && _hoveredCellIndex < _cells.Length)
                {
                    DateTime clickedDate = _cells[_hoveredCellIndex].Date;
                    if (_pendingStartDate == null)
                    {
                        _pendingStartDate = clickedDate;
                        InvalidateVisual();
                    }
                    else
                    {
                        DateTime start = _pendingStartDate.Value;
                        DateTime end = clickedDate;
                        _pendingStartDate = null;
                        _owner.SetRange(start, end);
                        _owner.ClosePopup();
                    }
                    e.Handled = true;
                }
            }

            private void ApplyPreset(DateRangePreset preset)
            {
                DateTime today = DateTime.Today;
                switch (preset)
                {
                    case DateRangePreset.Today:
                        _owner.SetRange(today, today);
                        break;
                    case DateRangePreset.Yesterday:
                        _owner.SetRange(today.AddDays(-1), today.AddDays(-1));
                        break;
                    case DateRangePreset.Last7Days:
                        _owner.SetRange(today.AddDays(-6), today);
                        break;
                    case DateRangePreset.Last30Days:
                        _owner.SetRange(today.AddDays(-29), today);
                        break;
                    case DateRangePreset.ThisMonth:
                        _owner.SetRange(new DateTime(today.Year, today.Month, 1), today);
                        break;
                    case DateRangePreset.LastMonth:
                        var lastMonth = today.AddMonths(-1);
                        int daysInLastMonth = DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month);
                        _owner.SetRange(new DateTime(lastMonth.Year, lastMonth.Month, 1), new DateTime(lastMonth.Year, lastMonth.Month, daysInLastMonth));
                        break;
                    case DateRangePreset.YearToDate:
                        _owner.SetRange(new DateTime(today.Year, 1, 1), today);
                        break;
                }
            }

            protected override void OnRender(DrawingContext dc)
            {
                base.OnRender(dc);

                double w = ActualWidth;
                double h = ActualHeight;
                if (w <= 0 || h <= 0) return;

                _calendarModel.FillDaysGrid(_cells);
                double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

                // 1. Popup Background & Border
                Rect bgRect = new Rect(0.5, 0.5, w - 1.0, h - 1.0);
                dc.DrawRoundedRectangle(ZeroWpfTheme.BgCard, ZeroWpfTheme.BorderPen, bgRect, 8, 8);

                // 2. Sidebar for Quick Presets (X: 8 to 98)
                dc.DrawLine(ZeroWpfTheme.GridLinePen, new Point(100, 8), new Point(100, h - 8));

                var sidebarTitle = new FormattedText("Quick Filters", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, ZeroWpfTheme.BoldTypeface, 11.0, ZeroWpfTheme.TextMuted, dpi);
                dc.DrawText(sidebarTitle, new Point(12, 14));

                for (int i = 0; i < Presets.Length; i++)
                {
                    double py = 36 + (i * 28);
                    Rect pRect = new Rect(8, py, 88, 24);
                    bool hov = (_hoveredPresetIndex == i);

                    if (hov)
                    {
                        dc.DrawRoundedRectangle(ZeroWpfTheme.BgHover, null, pRect, 4, 4);
                    }

                    var pFt = new FormattedText(
                        Presets[i].Label,
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        ZeroWpfTheme.MediumTypeface,
                        10.5,
                        hov ? ZeroWpfTheme.PrimaryAccent : ZeroWpfTheme.TextSecondary,
                        dpi);

                    dc.DrawText(pFt, new Point(14, py + 4));
                }

                // 3. Right Calendar Panel Header [< Month YYYY >]
                double calLeft = 106;
                double calRight = w - 10;
                double calWidth = calRight - calLeft;

                var headerFt = new FormattedText(
                    _calendarModel.ViewDate.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    ZeroWpfTheme.BoldTypeface,
                    13.0,
                    ZeroWpfTheme.TextPrimary,
                    dpi);

                dc.DrawText(headerFt, new Point(calLeft + (calWidth - headerFt.Width) / 2.0, 10));

                Brush prevBrush = _hoveredPrevMonth ? ZeroWpfTheme.PrimaryAccent : ZeroWpfTheme.TextSecondary;
                Brush nextBrush = _hoveredNextMonth ? ZeroWpfTheme.PrimaryAccent : ZeroWpfTheme.TextSecondary;
                var prevFt = new FormattedText("◀", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, ZeroWpfTheme.RegularTypeface, 11.0, prevBrush, dpi);
                var nextFt = new FormattedText("▶", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, ZeroWpfTheme.RegularTypeface, 11.0, nextBrush, dpi);
                dc.DrawText(prevFt, new Point(calLeft + 10, 11));
                dc.DrawText(nextFt, new Point(calRight - 18, 11));

                // 4. Day of Week Headers
                string[] dows = { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" };
                double cellW = calWidth / 7.0;
                double cellH = 28;
                for (int i = 0; i < 7; i++)
                {
                    var dowFt = new FormattedText(dows[i], CultureInfo.InvariantCulture, FlowDirection.LeftToRight, ZeroWpfTheme.MediumTypeface, 10.5, ZeroWpfTheme.TextMuted, dpi);
                    dc.DrawText(dowFt, new Point(calLeft + (i * cellW) + (cellW - dowFt.Width) / 2.0, 42));
                }

                dc.DrawLine(ZeroWpfTheme.GridLinePen, new Point(calLeft, 60), new Point(calRight, 60));

                // 5. 42-Day Range Rendering
                double gridTop = 64;
                DateTime startRange = _pendingStartDate ?? _owner.StartDate;
                DateTime endRange = _pendingStartDate != null
                    ? (_hoveredCellIndex >= 0 ? _cells[_hoveredCellIndex].Date : startRange)
                    : _owner.EndDate;

                if (startRange > endRange) (startRange, endRange) = (endRange, startRange);

                Brush rangeBandBrush = new SolidColorBrush(ZeroWpfTheme.PrimaryAccent.Color) { Opacity = 0.22 };
                rangeBandBrush.Freeze();

                for (int i = 0; i < CalendarModel.TotalCells; i++)
                {
                    var cell = _cells[i];
                    int row = i / 7;
                    int col = i % 7;
                    double cx = calLeft + (col * cellW);
                    double cy = gridTop + (row * cellH);
                    Rect cellRect = new Rect(cx, cy, cellW, cellH);

                    bool isStart = cell.Date.Date == startRange.Date;
                    bool isEnd = cell.Date.Date == endRange.Date;
                    bool inRange = cell.Date.Date >= startRange.Date && cell.Date.Date <= endRange.Date;

                    // Draw range connection band
                    if (inRange && startRange != endRange)
                    {
                        Rect bandRect = new Rect(
                            isStart ? (cx + (cellW / 2.0)) : cx,
                            cy + 3,
                            isStart || isEnd ? (cellW / 2.0) : cellW,
                            cellH - 6);
                        dc.DrawRectangle(rangeBandBrush, null, bandRect);
                    }

                    // Draw endpoint pills
                    Rect pillRect = new Rect(cx + 2, cy + 2, cellW - 4, cellH - 4);
                    if (isStart || isEnd)
                    {
                        dc.DrawRoundedRectangle(ZeroWpfTheme.PrimaryAccent, null, pillRect, 4, 4);
                    }
                    else if (i == _hoveredCellIndex)
                    {
                        dc.DrawRoundedRectangle(ZeroWpfTheme.BgHover, null, pillRect, 4, 4);
                    }

                    Brush textBrush;
                    if (isStart || isEnd) textBrush = Brushes.White;
                    else if (!cell.IsCurrentMonth) textBrush = ZeroWpfTheme.TextMuted;
                    else textBrush = ZeroWpfTheme.TextPrimary;

                    var dayFt = new FormattedText(
                        cell.DayNumber.ToString(),
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        (isStart || isEnd) ? ZeroWpfTheme.BoldTypeface : ZeroWpfTheme.RegularTypeface,
                        11.5,
                        textBrush,
                        dpi);

                    dc.DrawText(dayFt, new Point(cx + (cellW - dayFt.Width) / 2.0, cy + (cellH - dayFt.Height) / 2.0));
                }

                // Footer Prompt
                string prompt = _pendingStartDate == null
                    ? "Select start date..."
                    : $"Start: {_pendingStartDate:MMM dd} — Select end date";

                var promptFt = new FormattedText(prompt, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, ZeroWpfTheme.MediumTypeface, 10.0, ZeroWpfTheme.TextSecondary, dpi);
                dc.DrawText(promptFt, new Point(calLeft + 4, h - 20));
            }
        }

        #endregion
    }
}
