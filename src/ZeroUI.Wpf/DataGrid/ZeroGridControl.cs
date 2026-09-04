using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Core.Common;
using ZeroUI.Core.Data;
using ZeroUI.Core.Virtualization;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.DataGrid
{
    /// <summary>
    /// Ultra high-performance Single-Visual Virtual DataGrid for WPF.
    /// Eliminates WPF Visual Tree overhead by rendering cells directly via DrawingContext,
    /// powered by ZeroUI.Core virtualization algorithms and RowIndexMap.
    /// </summary>
    public class ZeroGridControl : FrameworkElement
    {
        private readonly ObservableCollection<ZeroColumn> _columns = new ObservableCollection<ZeroColumn>();
        private RowIndexMap _rowIndexMap = new RowIndexMap(10000);
        private IZeroVirtualSource? _dataSource;

        private int _headerHeight = 32;
        private int _rowHeight = 28;
        private int _scrollX = 0;
        private int _scrollY = 0;

        // Selection & Interaction
        private int _selectedVisualRow = -1;
        private int _hoveredVisualRow = -1;
        private bool _isResizingColumn = false;
        private int _resizingColIndex = -1;
        private double _resizeStartX = 0;
        private int _resizeStartWidth = 0;

        // Slim ScrollBar Interaction
        private const int ScrollBarThickness = 8;
        private bool _isDraggingVThumb = false;
        private double _dragThumbStartY = 0;
        private int _dragScrollStartY = 0;
        private bool _isVThumbHovered = false;

        // Asynchronous Sorting
        private bool _isSorting = false;
        private int _sortingColumnIndex = -1;
        private System.Threading.CancellationTokenSource? _sortCts;

        public bool IsSorting => _isSorting;
        public int SortingColumnIndex => _sortingColumnIndex;

        public event EventHandler? SelectionChanged;
        public event EventHandler<int>? ColumnHeaderClicked;
        public event EventHandler? SortingStarted;
        public event EventHandler<TimeSpan>? SortingCompleted;

        public ObservableCollection<ZeroColumn> Columns => _columns;

        public static readonly DependencyProperty DensityProperty =
            DependencyProperty.Register(
                nameof(Density),
                typeof(GridDensity),
                typeof(ZeroGridControl),
                new FrameworkPropertyMetadata(GridDensity.Middle, FrameworkPropertyMetadataOptions.AffectsRender, OnDensityChanged));

        public GridDensity Density
        {
            get => (GridDensity)GetValue(DensityProperty);
            set => SetValue(DensityProperty, value);
        }

        private static void OnDensityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZeroGridControl grid)
            {
                grid._rowHeight = (GridDensity)e.NewValue switch
                {
                    GridDensity.Compact => 24,
                    GridDensity.Loose => 36,
                    _ => 28
                };
                grid.InvalidateVisual();
            }
        }

        public IZeroVirtualSource? DataSource
        {
            get => _dataSource;
            set
            {
                _dataSource = value;
                if (_dataSource != null)
                {
                    _rowIndexMap.ResetIdentity(_dataSource.TotalRowCount);
                }
                else
                {
                    _rowIndexMap.ActiveCount = 0;
                }
                _scrollY = 0;
                _selectedVisualRow = -1;
                InvalidateVisual();
            }
        }

        public int SelectedIndex
        {
            get
            {
                if (_selectedVisualRow >= 0 && _selectedVisualRow < _rowIndexMap.ActiveCount)
                {
                    return _rowIndexMap[_selectedVisualRow];
                }
                return -1;
            }
            set
            {
                if (value < 0 || _dataSource == null || value >= _dataSource.TotalRowCount)
                {
                    _selectedVisualRow = -1;
                }
                else
                {
                    // Find visual row matching model index
                    for (int i = 0; i < _rowIndexMap.ActiveCount; i++)
                    {
                        if (_rowIndexMap[i] == value)
                        {
                            _selectedVisualRow = i;
                            break;
                        }
                    }
                }
                InvalidateVisual();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public RowIndexMap IndexMap => _rowIndexMap;

        public ZeroGridControl()
        {
            ClipToBounds = true;
            Focusable = true;
            _columns.CollectionChanged += (s, e) => InvalidateVisual();
            ZeroWpfTheme.ThemeChanged += () => InvalidateVisual();
        }

        public void SortByColumn(int colIndex, Comparison<int> comparison)
        {
            _ = SortByColumnAsync(colIndex, comparison);
        }

        public async Task SortByColumnAsync(int colIndex)
        {
            if (_dataSource is IZeroSortableSource sortable)
            {
                await SortByColumnAsync(colIndex, (a, b) => sortable.CompareRows(a, b, colIndex));
            }
            else if (_dataSource != null)
            {
                await SortByColumnAsync(colIndex, (a, b) =>
                {
                    CellValueBuffer bufA = new CellValueBuffer();
                    CellValueBuffer bufB = new CellValueBuffer();
                    _dataSource.GetCellValue(a, colIndex, ref bufA);
                    _dataSource.GetCellValue(b, colIndex, ref bufB);
                    return bufA.Text.CompareTo(bufB.Text, StringComparison.Ordinal);
                });
            }
        }

        public async Task SortByColumnAsync(int colIndex, Comparison<int> comparison)
        {
            if (_dataSource == null || colIndex < 0 || colIndex >= _columns.Count) return;
            if (_isSorting) return;

            var col = _columns[colIndex];
            SortDirection newDirection = (col.SortOrder == SortDirection.Ascending) ? SortDirection.Descending : SortDirection.Ascending;
            col.SortOrder = newDirection;

            // Reset other columns
            for (int i = 0; i < _columns.Count; i++)
            {
                if (i != colIndex) _columns[i].SortOrder = SortDirection.None;
            }

            int count = _rowIndexMap.ActiveCount;
            if (count <= 1)
            {
                InvalidateVisual();
                return;
            }

            _isSorting = true;
            _sortingColumnIndex = colIndex;
            SortingStarted?.Invoke(this, EventArgs.Empty);
            InvalidateVisual();

            _sortCts?.Cancel();
            _sortCts = new System.Threading.CancellationTokenSource();
            var token = _sortCts.Token;

            // Fast copy active indices into background working buffer
            int[] working = new int[count];
            for (int i = 0; i < count; i++)
            {
                working[i] = _rowIndexMap[i];
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var ds = _dataSource;
                await Task.Run(() =>
                {
                    if (ds is IZeroSortableSource sortable)
                    {
                        var comparer = new SortableSourceComparer(sortable, colIndex, newDirection);
                        Array.Sort(working, 0, count, comparer);
                    }
                    else
                    {
                        var comparer = new FastComparisonComparer(newDirection == SortDirection.Descending ? (a, b) => comparison(b, a) : comparison);
                        Array.Sort(working, 0, count, comparer);
                    }
                }, token);

                sw.Stop();

                if (!token.IsCancellationRequested)
                {
                    for (int i = 0; i < count; i++)
                    {
                        _rowIndexMap[i] = working[i];
                    }

                    _scrollY = 0;
                    SortingCompleted?.Invoke(this, sw.Elapsed);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sort error: {ex}");
            }
            finally
            {
                _isSorting = false;
                _sortingColumnIndex = -1;
                InvalidateVisual();
            }
        }

        public int GetTotalColumnsWidth()
        {
            int total = 0;
            for (int i = 0; i < _columns.Count; i++)
            {
                if (_columns[i].IsVisible) total += _columns[i].Width;
            }
            return total;
        }

        private int[] GetVisibleColumnWidths()
        {
            int[] widths = new int[_columns.Count];
            for (int i = 0; i < _columns.Count; i++)
            {
                widths[i] = _columns[i].IsVisible ? _columns[i].Width : 0;
            }
            return widths;
        }

        private int GetMaxScrollY()
        {
            int totalRows = _rowIndexMap.ActiveCount;
            int totalH = totalRows * _rowHeight;
            int clientH = (int)Math.Max(0, ActualHeight - _headerHeight);
            return Math.Max(0, totalH - clientH);
        }

        private int GetMaxScrollX()
        {
            int totalW = GetTotalColumnsWidth();
            return Math.Max(0, totalW - (int)ActualWidth);
        }

        #if NETFRAMEWORK
        private static FormattedText CreateFormattedText(string text, Typeface typeface, double fontSize, Brush brush, double pixelsPerDip = 1.0)
        {
            return new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize, brush);
        }
        #else
        private static FormattedText CreateFormattedText(string text, Typeface typeface, double fontSize, Brush brush, double pixelsPerDip = 1.0)
        {
            return new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize, brush, pixelsPerDip);
        }
        #endif

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double width = ActualWidth;
            double height = ActualHeight;
            if (width <= 0 || height <= 0) return;

            #if NETFRAMEWORK
            double dpi = 1.0;
            #else
            double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            #endif

            // 1. Render Background
            dc.DrawRectangle(ZeroWpfTheme.BgPrimary, null, new Rect(0, 0, width, height));

            int totalCols = _columns.Count;
            int totalRows = _rowIndexMap.ActiveCount;
            int[] colWidths = GetVisibleColumnWidths();

            // 2. Render Virtual Cells
            if (_dataSource != null && totalRows > 0 && totalCols > 0)
            {
                int clientDataH = (int)Math.Max(0, height - _headerHeight);
                var range = VirtualViewport2D.ComputeUniform(
                    _scrollX,
                    _scrollY,
                    (int)width - ScrollBarThickness,
                    clientDataH,
                    _rowHeight,
                    totalRows,
                    colWidths,
                    totalCols);

                CellValueBuffer cellBuffer = new CellValueBuffer();
                double currentY = _headerHeight + range.FirstRowY;

                for (int r = range.StartRow; r <= range.EndRow && r < totalRows; r++)
                {
                    int modelRow = _rowIndexMap[r];
                    bool isSelected = (r == _selectedVisualRow);
                    bool isHovered = (r == _hoveredVisualRow && !isSelected);

                    Brush rowBrush = isSelected ? ZeroWpfTheme.SelectionBackground :
                                     isHovered ? ZeroWpfTheme.BgHover :
                                     ((r % 2 == 1) ? ZeroWpfTheme.BgInput : ZeroWpfTheme.BgCard);

                    // Row background
                    dc.DrawRectangle(rowBrush, null, new Rect(0, currentY, width - ScrollBarThickness, _rowHeight));

                    if (isSelected)
                    {
                        // Left active indicator strip
                        dc.DrawRectangle(ZeroWpfTheme.PrimaryAccent, null, new Rect(0, currentY, 3.5, _rowHeight));
                    }

                    double currentX = range.FirstColX;
                    for (int c = range.StartCol; c <= range.EndCol && c < totalCols; c++)
                    {
                        int colW = colWidths[c];
                        if (colW <= 0) continue;

                        cellBuffer.Reset();
                        cellBuffer.Alignment = _columns[c].Alignment;
                        _dataSource.GetCellValue(modelRow, c, ref cellBuffer);

                        // Cell Custom Background
                        if (cellBuffer.HasCustomBackground && !isSelected)
                        {
                            byte a = (byte)((cellBuffer.BackColor >> 24) & 0xFF);
                            byte b = (byte)((cellBuffer.BackColor >> 16) & 0xFF);
                            byte g = (byte)((cellBuffer.BackColor >> 8) & 0xFF);
                            byte rCol = (byte)(cellBuffer.BackColor & 0xFF);
                            if (a == 0) a = 255;
                            var customBrush = new SolidColorBrush(Color.FromArgb(a, rCol, g, b));
                            dc.DrawRectangle(customBrush, null, new Rect(currentX, currentY, colW, _rowHeight));
                        }

                        // Cell Text with high contrast
                        string text = cellBuffer.Text.ToString();
                        if (!string.IsNullOrEmpty(text))
                        {
                            Brush textBrush = isSelected ? ZeroWpfTheme.SelectionForeground : ZeroWpfTheme.TextPrimary;
                            Typeface tf = isSelected ? ZeroWpfTheme.BoldTypeface : ZeroWpfTheme.RegularTypeface;

                            if (!isSelected && cellBuffer.TextColor != 0)
                            {
                                byte tr = (byte)(cellBuffer.TextColor & 0xFF);
                                byte tg = (byte)((cellBuffer.TextColor >> 8) & 0xFF);
                                byte tb = (byte)((cellBuffer.TextColor >> 16) & 0xFF);
                                var customTextBrush = new SolidColorBrush(Color.FromRgb(tr, tg, tb));
                                customTextBrush.Freeze();
                                textBrush = customTextBrush;
                            }

                            var ft = CreateFormattedText(text, tf, 12.0, textBrush, dpi);

                            double textX = currentX + 8;
                            if (cellBuffer.Alignment == CellAlignment.Right)
                            {
                                textX = currentX + colW - ft.Width - 8;
                            }
                            else if (cellBuffer.Alignment == CellAlignment.Center)
                            {
                                textX = currentX + (colW - ft.Width) / 2.0;
                            }

                            double textY = currentY + (_rowHeight - ft.Height) / 2.0;
                            dc.DrawText(ft, new Point(textX, textY));
                        }

                        // Cell Vertical Border
                        dc.DrawLine(ZeroWpfTheme.GridLinePen, new Point(currentX + colW - 0.5, currentY), new Point(currentX + colW - 0.5, currentY + _rowHeight));

                        currentX += colW;
                    }

                    // Row Horizontal Border
                    dc.DrawLine(ZeroWpfTheme.GridLinePen, new Point(0, currentY + _rowHeight - 0.5), new Point(width - ScrollBarThickness, currentY + _rowHeight - 0.5));

                    currentY += _rowHeight;
                }
            }
            else
            {
                // Empty state
                var emptyTitle = CreateFormattedText("No records to display", ZeroWpfTheme.BoldTypeface, 14.0, ZeroWpfTheme.TextSecondary, dpi);
                var emptySub = CreateFormattedText("Try adjusting your filters or loading sample data", ZeroWpfTheme.RegularTypeface, 12.0, ZeroWpfTheme.TextMuted, dpi);

                double midY = (_headerHeight + height) / 2.0 - 20;
                dc.DrawText(emptyTitle, new Point((width - emptyTitle.Width) / 2.0, midY));
                dc.DrawText(emptySub, new Point((width - emptySub.Width) / 2.0, midY + 24));
            }

            // 3. Render Header Row (Always pinned on top)
            dc.DrawRectangle(ZeroWpfTheme.BgCard, null, new Rect(0, 0, width, _headerHeight));
            dc.DrawLine(ZeroWpfTheme.BorderPen, new Point(0, _headerHeight - 0.5), new Point(width, _headerHeight - 0.5));

            double headerX = -_scrollX;
            for (int c = 0; c < totalCols; c++)
            {
                int colW = colWidths[c];
                if (colW <= 0) continue;

                string headerText = _columns[c].HeaderText;
                if (_isSorting && _sortingColumnIndex == c) headerText += " ⏳";
                else if (_columns[c].SortOrder == SortDirection.Ascending) headerText += " ▲";
                else if (_columns[c].SortOrder == SortDirection.Descending) headerText += " ▼";

                var hft = CreateFormattedText(headerText, ZeroWpfTheme.BoldTypeface, 12.0, ZeroWpfTheme.TextPrimary, dpi);

                double hTextX = headerX + 8;
                if (_columns[c].Alignment == CellAlignment.Right)
                {
                    hTextX = headerX + colW - hft.Width - 8;
                }
                else if (_columns[c].Alignment == CellAlignment.Center)
                {
                    hTextX = headerX + (colW - hft.Width) / 2.0;
                }

                double hTextY = (_headerHeight - hft.Height) / 2.0;
                dc.DrawText(hft, new Point(hTextX, hTextY));

                // Column divider
                dc.DrawLine(ZeroWpfTheme.GridLinePen, new Point(headerX + colW - 0.5, 4), new Point(headerX + colW - 0.5, _headerHeight - 4));

                headerX += colW;
            }

            // 4. Render Slim Modern ScrollBar
            RenderSlimScrollBar(dc, width, height);
        }

        private void RenderSlimScrollBar(DrawingContext dc, double width, double height)
        {
            int totalRows = _rowIndexMap.ActiveCount;
            int totalH = totalRows * _rowHeight;
            double clientH = Math.Max(0, height - _headerHeight);

            if (totalH <= clientH || clientH <= 0) return;

            double trackX = width - ScrollBarThickness;
            double trackY = _headerHeight;
            double trackH = clientH;

            // Track background
            dc.DrawRectangle(ZeroWpfTheme.BgInput, null, new Rect(trackX, trackY, ScrollBarThickness, trackH));

            // Thumb
            double thumbH = Math.Max(20, (clientH / totalH) * trackH);
            double maxScroll = totalH - clientH;
            double thumbRatio = Math.Min(1.0, Math.Max(0.0, (double)_scrollY / maxScroll));
            double thumbY = trackY + thumbRatio * (trackH - thumbH);

            Brush thumbBrush = (_isDraggingVThumb || _isVThumbHovered) ? ZeroWpfTheme.PrimaryAccent : ZeroWpfTheme.TextMuted;
            dc.DrawRoundedRectangle(thumbBrush, null, new Rect(trackX + 1, thumbY, ScrollBarThickness - 2, thumbH), 3, 3);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Point pt = e.GetPosition(this);

            if (_isDraggingVThumb)
            {
                double clientH = Math.Max(0, ActualHeight - _headerHeight);
                int totalRows = _rowIndexMap.ActiveCount;
                int totalH = totalRows * _rowHeight;
                double maxScroll = totalH - clientH;
                double trackH = clientH;
                double thumbH = Math.Max(20, (clientH / totalH) * trackH);

                double deltaY = pt.Y - _dragThumbStartY;
                double availableTrack = trackH - thumbH;
                if (availableTrack > 0)
                {
                    double scrollDelta = (deltaY / availableTrack) * maxScroll;
                    _scrollY = (int)Math.Max(0, Math.Min(maxScroll, _dragScrollStartY + scrollDelta));
                    InvalidateVisual();
                }
                return;
            }

            if (_isResizingColumn)
            {
                double delta = pt.X - _resizeStartX;
                int newW = (int)Math.Max(30, _resizeStartWidth + delta);
                if (_resizingColIndex >= 0 && _resizingColIndex < _columns.Count)
                {
                    _columns[_resizingColIndex].Width = newW;
                    InvalidateVisual();
                }
                return;
            }

            // Check scrollbar hover
            bool prevVThumbHover = _isVThumbHovered;
            _isVThumbHovered = (pt.X >= ActualWidth - ScrollBarThickness && pt.Y >= _headerHeight);
            if (prevVThumbHover != _isVThumbHovered) InvalidateVisual();

            // Check header column resize handle
            if (pt.Y <= _headerHeight)
            {
                int colIdx = HitTestColumnDivider(pt.X);
                if (colIdx >= 0)
                {
                    Cursor = Cursors.SizeWE;
                    return;
                }
                Cursor = Cursors.Arrow;
                return;
            }

            Cursor = Cursors.Arrow;

            // Row hover
            int visualRow = (int)((pt.Y - _headerHeight + _scrollY) / _rowHeight);
            if (visualRow >= 0 && visualRow < _rowIndexMap.ActiveCount)
            {
                if (_hoveredVisualRow != visualRow)
                {
                    _hoveredVisualRow = visualRow;
                    InvalidateVisual();
                }
            }
            else if (_hoveredVisualRow != -1)
            {
                _hoveredVisualRow = -1;
                InvalidateVisual();
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoveredVisualRow != -1 || _isVThumbHovered)
            {
                _hoveredVisualRow = -1;
                _isVThumbHovered = false;
                InvalidateVisual();
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            Point pt = e.GetPosition(this);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Check ScrollBar thumb click
                if (pt.X >= ActualWidth - ScrollBarThickness && pt.Y >= _headerHeight)
                {
                    _isDraggingVThumb = true;
                    _dragThumbStartY = pt.Y;
                    _dragScrollStartY = _scrollY;
                    CaptureMouse();
                    return;
                }

                // Check Column Header click or resize
                if (pt.Y <= _headerHeight)
                {
                    int dividerCol = HitTestColumnDivider(pt.X);
                    if (dividerCol >= 0)
                    {
                        _isResizingColumn = true;
                        _resizingColIndex = dividerCol;
                        _resizeStartX = pt.X;
                        _resizeStartWidth = _columns[dividerCol].Width;
                        CaptureMouse();
                        return;
                    }

                    // Column header click for sort
                    int col = HitTestColumn(pt.X);
                    if (col >= 0)
                    {
                        ColumnHeaderClicked?.Invoke(this, col);
                    }
                    return;
                }

                // Row click selection
                int visualRow = (int)((pt.Y - _headerHeight + _scrollY) / _rowHeight);
                if (visualRow >= 0 && visualRow < _rowIndexMap.ActiveCount)
                {
                    _selectedVisualRow = visualRow;
                    InvalidateVisual();
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if (_isDraggingVThumb)
            {
                _isDraggingVThumb = false;
                ReleaseMouseCapture();
                InvalidateVisual();
            }
            if (_isResizingColumn)
            {
                _isResizingColumn = false;
                _resizingColIndex = -1;
                ReleaseMouseCapture();
                InvalidateVisual();
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            int scrollAmount = (e.Delta / 120) * _rowHeight * 3;
            _scrollY = Math.Max(0, Math.Min(GetMaxScrollY(), _scrollY - scrollAmount));
            InvalidateVisual();
        }

        private int HitTestColumnDivider(double mouseX)
        {
            double curX = -_scrollX;
            for (int i = 0; i < _columns.Count; i++)
            {
                if (!_columns[i].IsVisible) continue;
                curX += _columns[i].Width;
                if (Math.Abs(mouseX - curX) <= 4)
                {
                    return i;
                }
            }
            return -1;
        }

        private int HitTestColumn(double mouseX)
        {
            double curX = -_scrollX;
            for (int i = 0; i < _columns.Count; i++)
            {
                if (!_columns[i].IsVisible) continue;
                if (mouseX >= curX && mouseX < curX + _columns[i].Width)
                {
                    return i;
                }
                curX += _columns[i].Width;
            }
            return -1;
        }

        private sealed class FastComparisonComparer : System.Collections.Generic.IComparer<int>
        {
            private readonly Comparison<int> _comparison;
            public FastComparisonComparer(Comparison<int> comparison) => _comparison = comparison;
            public int Compare(int x, int y) => _comparison(x, y);
        }

        private sealed class SortableSourceComparer : System.Collections.Generic.IComparer<int>
        {
            private readonly IZeroSortableSource _source;
            private readonly int _columnIndex;
            private readonly SortDirection _direction;

            public SortableSourceComparer(IZeroSortableSource source, int columnIndex, SortDirection direction)
            {
                _source = source;
                _columnIndex = columnIndex;
                _direction = direction;
            }

            public int Compare(int x, int y)
            {
                int cmp = _source.CompareRows(x, y, _columnIndex);
                return _direction == SortDirection.Ascending ? cmp : -cmp;
            }
        }
    }
}
