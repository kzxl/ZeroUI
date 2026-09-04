using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        private readonly HashSet<int> _selectedVisualRows = new HashSet<int>();
        private ZeroGridSelectionMode _selectionMode = ZeroGridSelectionMode.SingleRow;
        private bool _isResizingColumn = false;
        private int _resizingColIndex = -1;
        private double _resizeStartX = 0;
        private int _resizeStartWidth = 0;

        // In-Place Floating Editor
        private readonly VisualCollection _visualChildren;
        private readonly TextBox _inPlaceEditor;
        private bool _isEditing = false;
        private int _editingVisualRow = -1;
        private int _editingColIndex = -1;

        // Summary Footer
        private bool _showFooter = false;
        private int _footerHeight = 28;

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
        public event EventHandler<CellValueChangedEventArgs>? CellValueChanged;
        public event EventHandler? CellBeginEdit;
        public event EventHandler? CellEndEdit;

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
                _selectedVisualRows.Clear();
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
                    _selectedVisualRows.Clear();
                }
                else
                {
                    // Find visual row matching model index
                    for (int i = 0; i < _rowIndexMap.ActiveCount; i++)
                    {
                        if (_rowIndexMap[i] == value)
                        {
                            _selectedVisualRow = i;
                            _selectedVisualRows.Clear();
                            _selectedVisualRows.Add(i);
                            break;
                        }
                    }
                }
                InvalidateVisual();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public ZeroGridSelectionMode SelectionMode
        {
            get => _selectionMode;
            set { _selectionMode = value; InvalidateVisual(); }
        }

        public IReadOnlyCollection<int> SelectedVisualRows => _selectedVisualRows;

        public bool ShowFooter
        {
            get => _showFooter || HasAnySummaryColumns();
            set { _showFooter = value; InvalidateVisual(); }
        }

        public int FooterHeight
        {
            get => _footerHeight;
            set { _footerHeight = Math.Max(20, value); InvalidateVisual(); }
        }

        public bool IsEditing => _isEditing;

        public RowIndexMap IndexMap => _rowIndexMap;

        public ZeroGridControl()
        {
            ClipToBounds = true;
            Focusable = true;
            _visualChildren = new VisualCollection(this);

            _inPlaceEditor = new TextBox
            {
                Visibility = Visibility.Collapsed,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4, 2, 4, 2),
                FontSize = 12.0
            };
            _inPlaceEditor.KeyDown += InPlaceEditor_KeyDown;
            _inPlaceEditor.LostFocus += (s, e) => CommitEdit();
            _visualChildren.Add(_inPlaceEditor);

            _columns.CollectionChanged += (s, e) => InvalidateVisual();
            ZeroWpfTheme.ThemeChanged += () =>
            {
                UpdateEditorTheme();
                InvalidateVisual();
            };
        }

        private void UpdateEditorTheme()
        {
            _inPlaceEditor.Background = ZeroWpfTheme.BgInput;
            _inPlaceEditor.Foreground = ZeroWpfTheme.TextPrimary;
            _inPlaceEditor.BorderBrush = ZeroWpfTheme.PrimaryAccent;
            _inPlaceEditor.CaretBrush = ZeroWpfTheme.PrimaryAccent;
        }

        protected override int VisualChildrenCount => _visualChildren.Count;
        protected override Visual GetVisualChild(int index) => _visualChildren[index];

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_isEditing && _inPlaceEditor.Visibility == Visibility.Visible)
            {
                var rect = GetCellRectangle(_editingVisualRow, _editingColIndex);
                int footerH = ShowFooter ? _footerHeight : 0;
                if (rect.Y < _headerHeight || rect.Bottom > finalSize.Height - footerH)
                {
                    CommitEdit();
                }
                else
                {
                    _inPlaceEditor.Arrange(rect);
                }
            }
            return base.ArrangeOverride(finalSize);
        }

        public void SetDataSource<T>(IList<T> items, bool autoGenerateColumns = true)
        {
            if (items == null)
            {
                DataSource = null;
                return;
            }

            if (autoGenerateColumns && _columns.Count == 0)
            {
                var src = new ZeroListSource<T>(items);
                foreach (var c in src.GenerateColumns())
                {
                    _columns.Add(c);
                }
                DataSource = src;
            }
            else
            {
                DataSource = new ZeroListSource<T>(items, _columns);
            }
        }

        public bool HasAnySummaryColumns()
        {
            for (int i = 0; i < _columns.Count; i++)
            {
                if (_columns[i].IsVisible && _columns[i].Summary != SummaryType.None) return true;
            }
            return false;
        }

        public int GetPinnedColumnsWidth()
        {
            int total = 0;
            for (int i = 0; i < _columns.Count; i++)
            {
                if (_columns[i].IsVisible && _columns[i].IsPinned) total += _columns[i].Width;
            }
            return total;
        }

        public int GetUnpinnedColumnsWidth()
        {
            int total = 0;
            for (int i = 0; i < _columns.Count; i++)
            {
                if (_columns[i].IsVisible && !_columns[i].IsPinned) total += _columns[i].Width;
            }
            return total;
        }

        public bool[] GetColumnPinnedFlags()
        {
            bool[] flags = new bool[_columns.Count];
            for (int i = 0; i < _columns.Count; i++)
            {
                flags[i] = _columns[i].IsVisible && _columns[i].IsPinned;
            }
            return flags;
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
            int footerH = ShowFooter ? _footerHeight : 0;
            int totalRows = _rowIndexMap.ActiveCount;
            int totalH = totalRows * _rowHeight;
            int clientH = (int)Math.Max(0, ActualHeight - _headerHeight - footerH);
            return Math.Max(0, totalH - clientH);
        }

        private int GetMaxScrollX()
        {
            int unpinnedW = GetUnpinnedColumnsWidth();
            int pinnedW = GetPinnedColumnsWidth();
            int scrollableW = (int)Math.Max(0, ActualWidth - pinnedW - ScrollBarThickness);
            return Math.Max(0, unpinnedW - scrollableW);
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
            int pinnedW = GetPinnedColumnsWidth();
            int footerH = ShowFooter ? _footerHeight : 0;
            int clientDataH = (int)Math.Max(0, height - _headerHeight - footerH);

            // 2. Render Virtual Cells
            if (_dataSource != null && totalRows > 0 && totalCols > 0)
            {
                int startRow = Math.Max(0, _scrollY / _rowHeight);
                int visibleRowCount = (clientDataH / _rowHeight) + 2;
                int endRow = Math.Min(totalRows - 1, startRow + visibleRowCount);

                CellValueBuffer cellBuffer = new CellValueBuffer();
                double currentY = _headerHeight + (startRow * _rowHeight) - _scrollY;

                for (int r = startRow; r <= endRow && r < totalRows; r++)
                {
                    if (currentY >= _headerHeight + clientDataH) break;

                    int modelRow = _rowIndexMap[r];
                    bool isSelected = (_selectionMode == ZeroGridSelectionMode.MultiRow)
                        ? _selectedVisualRows.Contains(r)
                        : (r == _selectedVisualRow);
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

                    // (A) Draw Unpinned Cells
                    double unpinnedX = pinnedW - _scrollX;
                    for (int c = 0; c < totalCols; c++)
                    {
                        if (!_columns[c].IsVisible || _columns[c].IsPinned) continue;
                        int colW = colWidths[c];
                        if (colW <= 0) continue;

                        if (unpinnedX + colW > pinnedW && unpinnedX < width - ScrollBarThickness)
                        {
                            cellBuffer.Reset();
                            cellBuffer.Alignment = _columns[c].Alignment;
                            _dataSource.GetCellValue(modelRow, c, ref cellBuffer);

                            if (cellBuffer.HasCustomBackground && !isSelected)
                            {
                                byte a = (byte)((cellBuffer.BackColor >> 24) & 0xFF);
                                byte b = (byte)((cellBuffer.BackColor >> 16) & 0xFF);
                                byte g = (byte)((cellBuffer.BackColor >> 8) & 0xFF);
                                byte rCol = (byte)(cellBuffer.BackColor & 0xFF);
                                if (a == 0) a = 255;
                                var customBrush = new SolidColorBrush(Color.FromArgb(a, rCol, g, b));
                                dc.DrawRectangle(customBrush, null, new Rect(unpinnedX, currentY, colW, _rowHeight));
                            }

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
                                double textX = unpinnedX + 8;
                                if (cellBuffer.Alignment == CellAlignment.Right) textX = unpinnedX + colW - ft.Width - 8;
                                else if (cellBuffer.Alignment == CellAlignment.Center) textX = unpinnedX + (colW - ft.Width) / 2.0;

                                double textY = currentY + (_rowHeight - ft.Height) / 2.0;
                                dc.DrawText(ft, new Point(textX, textY));
                            }

                            dc.DrawLine(ZeroWpfTheme.GridLinePen, new Point(unpinnedX + colW - 0.5, currentY), new Point(unpinnedX + colW - 0.5, currentY + _rowHeight));
                        }

                        unpinnedX += colW;
                    }

                    // (B) Draw Pinned Cells on top
                    double pinnedX = 0;
                    for (int c = 0; c < totalCols; c++)
                    {
                        if (!_columns[c].IsVisible || !_columns[c].IsPinned) continue;
                        int colW = colWidths[c];
                        if (colW <= 0) continue;

                        cellBuffer.Reset();
                        cellBuffer.Alignment = _columns[c].Alignment;
                        _dataSource.GetCellValue(modelRow, c, ref cellBuffer);

                        if (cellBuffer.HasCustomBackground && !isSelected)
                        {
                            byte a = (byte)((cellBuffer.BackColor >> 24) & 0xFF);
                            byte b = (byte)((cellBuffer.BackColor >> 16) & 0xFF);
                            byte g = (byte)((cellBuffer.BackColor >> 8) & 0xFF);
                            byte rCol = (byte)(cellBuffer.BackColor & 0xFF);
                            if (a == 0) a = 255;
                            var customBrush = new SolidColorBrush(Color.FromArgb(a, rCol, g, b));
                            dc.DrawRectangle(customBrush, null, new Rect(pinnedX, currentY, colW, _rowHeight));
                        }

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
                            double textX = pinnedX + 8;
                            if (cellBuffer.Alignment == CellAlignment.Right) textX = pinnedX + colW - ft.Width - 8;
                            else if (cellBuffer.Alignment == CellAlignment.Center) textX = pinnedX + (colW - ft.Width) / 2.0;

                            double textY = currentY + (_rowHeight - ft.Height) / 2.0;
                            dc.DrawText(ft, new Point(textX, textY));
                        }

                        dc.DrawLine(ZeroWpfTheme.GridLinePen, new Point(pinnedX + colW - 0.5, currentY), new Point(pinnedX + colW - 0.5, currentY + _rowHeight));

                        pinnedX += colW;
                    }

                    // Row horizontal border
                    dc.DrawLine(ZeroWpfTheme.GridLinePen, new Point(0, currentY + _rowHeight - 0.5), new Point(width - ScrollBarThickness, currentY + _rowHeight - 0.5));
                    currentY += _rowHeight;
                }

                if (pinnedW > 0)
                {
                    dc.DrawLine(new Pen(ZeroWpfTheme.PrimaryAccent, 2.0), new Point(pinnedW - 1, _headerHeight), new Point(pinnedW - 1, height - footerH));
                }
            }
            else
            {
                // Empty state
                var emptyTitle = CreateFormattedText("No records to display", ZeroWpfTheme.BoldTypeface, 14.0, ZeroWpfTheme.TextSecondary, dpi);
                var emptySub = CreateFormattedText("Try adjusting your filters or loading sample data", ZeroWpfTheme.RegularTypeface, 12.0, ZeroWpfTheme.TextMuted, dpi);

                double midY = (_headerHeight + height - footerH) / 2.0 - 20;
                dc.DrawText(emptyTitle, new Point((width - emptyTitle.Width) / 2.0, midY));
                dc.DrawText(emptySub, new Point((width - emptySub.Width) / 2.0, midY + 24));
            }

            // 3. Render Header Row (Always pinned on top)
            dc.DrawRectangle(ZeroWpfTheme.BgCard, null, new Rect(0, 0, width, _headerHeight));
            dc.DrawLine(ZeroWpfTheme.BorderPen, new Point(0, _headerHeight - 0.5), new Point(width, _headerHeight - 0.5));

            // (A) Draw Unpinned Headers
            double unpinnedHdrX = pinnedW - _scrollX;
            for (int c = 0; c < totalCols; c++)
            {
                if (!_columns[c].IsVisible || _columns[c].IsPinned) continue;
                int colW = colWidths[c];
                if (colW <= 0) continue;

                if (unpinnedHdrX + colW > pinnedW && unpinnedHdrX < width - ScrollBarThickness)
                {
                    string headerText = _columns[c].HeaderText;
                    if (_isSorting && _sortingColumnIndex == c) headerText += " ⏳";
                    else if (_columns[c].SortOrder == SortDirection.Ascending) headerText += " ▲";
                    else if (_columns[c].SortOrder == SortDirection.Descending) headerText += " ▼";

                    var hft = CreateFormattedText(headerText, ZeroWpfTheme.BoldTypeface, 12.0, ZeroWpfTheme.TextPrimary, dpi);
                    double hTextX = unpinnedHdrX + 8;
                    if (_columns[c].Alignment == CellAlignment.Right) hTextX = unpinnedHdrX + colW - hft.Width - 8;
                    else if (_columns[c].Alignment == CellAlignment.Center) hTextX = unpinnedHdrX + (colW - hft.Width) / 2.0;

                    double hTextY = (_headerHeight - hft.Height) / 2.0;
                    dc.DrawText(hft, new Point(hTextX, hTextY));

                    dc.DrawLine(ZeroWpfTheme.GridLinePen, new Point(unpinnedHdrX + colW - 0.5, 4), new Point(unpinnedHdrX + colW - 0.5, _headerHeight - 4));
                }
                unpinnedHdrX += colW;
            }

            // (B) Draw Pinned Headers
            double pinnedHdrX = 0;
            for (int c = 0; c < totalCols; c++)
            {
                if (!_columns[c].IsVisible || !_columns[c].IsPinned) continue;
                int colW = colWidths[c];
                if (colW <= 0) continue;

                string headerText = _columns[c].HeaderText;
                if (_isSorting && _sortingColumnIndex == c) headerText += " ⏳";
                else if (_columns[c].SortOrder == SortDirection.Ascending) headerText += " ▲";
                else if (_columns[c].SortOrder == SortDirection.Descending) headerText += " ▼";

                var hft = CreateFormattedText(headerText, ZeroWpfTheme.BoldTypeface, 12.0, ZeroWpfTheme.TextPrimary, dpi);
                double hTextX = pinnedHdrX + 8;
                if (_columns[c].Alignment == CellAlignment.Right) hTextX = pinnedHdrX + colW - hft.Width - 8;
                else if (_columns[c].Alignment == CellAlignment.Center) hTextX = pinnedHdrX + (colW - hft.Width) / 2.0;

                double hTextY = (_headerHeight - hft.Height) / 2.0;
                dc.DrawText(hft, new Point(hTextX, hTextY));

                dc.DrawLine(ZeroWpfTheme.GridLinePen, new Point(pinnedHdrX + colW - 0.5, 4), new Point(pinnedHdrX + colW - 0.5, _headerHeight - 4));

                pinnedHdrX += colW;
            }

            if (pinnedW > 0)
            {
                dc.DrawLine(new Pen(ZeroWpfTheme.PrimaryAccent, 2.0), new Point(pinnedW - 1, 0), new Point(pinnedW - 1, _headerHeight));
            }

            // 4. Render Footer Summary Bar (if enabled)
            if (ShowFooter && footerH > 0)
            {
                double footerY = height - footerH;
                dc.DrawRectangle(ZeroWpfTheme.BgCard, null, new Rect(0, footerY, width, footerH));
                dc.DrawLine(ZeroWpfTheme.BorderPen, new Point(0, footerY), new Point(width, footerY));

                // (A) Unpinned Footers
                double unpinnedFootX = pinnedW - _scrollX;
                for (int c = 0; c < totalCols; c++)
                {
                    if (!_columns[c].IsVisible || _columns[c].IsPinned) continue;
                    int colW = colWidths[c];
                    if (colW <= 0) continue;

                    if (unpinnedFootX + colW > pinnedW && unpinnedFootX < width - ScrollBarThickness)
                    {
                        string sText = GetColumnSummaryText(c);
                        if (!string.IsNullOrEmpty(sText))
                        {
                            var sft = CreateFormattedText(sText, ZeroWpfTheme.BoldTypeface, 12.0, ZeroWpfTheme.TextPrimary, dpi);
                            double sX = unpinnedFootX + 8;
                            if (_columns[c].Alignment == CellAlignment.Right) sX = unpinnedFootX + colW - sft.Width - 8;
                            else if (_columns[c].Alignment == CellAlignment.Center) sX = unpinnedFootX + (colW - sft.Width) / 2.0;

                            double sY = footerY + (footerH - sft.Height) / 2.0;
                            dc.DrawText(sft, new Point(sX, sY));
                        }
                        dc.DrawLine(ZeroWpfTheme.GridLinePen, new Point(unpinnedFootX + colW - 0.5, footerY + 4), new Point(unpinnedFootX + colW - 0.5, footerY + footerH - 4));
                    }
                    unpinnedFootX += colW;
                }

                // (B) Pinned Footers
                double pinnedFootX = 0;
                for (int c = 0; c < totalCols; c++)
                {
                    if (!_columns[c].IsVisible || !_columns[c].IsPinned) continue;
                    int colW = colWidths[c];
                    if (colW <= 0) continue;

                    string sText = GetColumnSummaryText(c);
                    if (!string.IsNullOrEmpty(sText))
                    {
                        var sft = CreateFormattedText(sText, ZeroWpfTheme.BoldTypeface, 12.0, ZeroWpfTheme.TextPrimary, dpi);
                        double sX = pinnedFootX + 8;
                        if (_columns[c].Alignment == CellAlignment.Right) sX = pinnedFootX + colW - sft.Width - 8;
                        else if (_columns[c].Alignment == CellAlignment.Center) sX = pinnedFootX + (colW - sft.Width) / 2.0;

                        double sY = footerY + (footerH - sft.Height) / 2.0;
                        dc.DrawText(sft, new Point(sX, sY));
                    }
                    dc.DrawLine(ZeroWpfTheme.GridLinePen, new Point(pinnedFootX + colW - 0.5, footerY + 4), new Point(pinnedFootX + colW - 0.5, footerY + footerH - 4));

                    pinnedFootX += colW;
                }

                if (pinnedW > 0)
                {
                    dc.DrawLine(new Pen(ZeroWpfTheme.PrimaryAccent, 2.0), new Point(pinnedW - 1, footerY), new Point(pinnedW - 1, height));
                }
            }

            // 5. Render Slim Modern ScrollBar
            RenderSlimScrollBar(dc, width, height);
        }

        private void RenderSlimScrollBar(DrawingContext dc, double width, double height)
        {
            int footerH = ShowFooter ? _footerHeight : 0;
            int totalRows = _rowIndexMap.ActiveCount;
            int totalH = totalRows * _rowHeight;
            double clientH = Math.Max(0, height - _headerHeight - footerH);

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
            int footerH = ShowFooter ? _footerHeight : 0;

            if (_isDraggingVThumb)
            {
                double clientH = Math.Max(0, ActualHeight - _headerHeight - footerH);
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
                    if (_isEditing) InvalidateArrange();
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
                    if (_isEditing) InvalidateArrange();
                    InvalidateVisual();
                }
                return;
            }

            // Check scrollbar hover
            bool prevVThumbHover = _isVThumbHovered;
            _isVThumbHovered = (pt.X >= ActualWidth - ScrollBarThickness && pt.Y >= _headerHeight && pt.Y < ActualHeight - footerH);
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
            if (visualRow >= 0 && visualRow < _rowIndexMap.ActiveCount && pt.Y < ActualHeight - footerH)
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
            int footerH = ShowFooter ? _footerHeight : 0;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Double click cell editing
                if (e.ClickCount == 2 && pt.Y > _headerHeight && pt.Y < ActualHeight - footerH)
                {
                    int vRow = (int)((pt.Y - _headerHeight + _scrollY) / _rowHeight);
                    int col = HitTestColumn(pt.X);
                    if (vRow >= 0 && vRow < _rowIndexMap.ActiveCount && col >= 0)
                    {
                        StartEdit(vRow, col);
                        return;
                    }
                }

                // Check ScrollBar thumb click
                if (pt.X >= ActualWidth - ScrollBarThickness && pt.Y >= _headerHeight && pt.Y < ActualHeight - footerH)
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
                    if (_isEditing) CommitEdit();

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
                if (visualRow >= 0 && visualRow < _rowIndexMap.ActiveCount && pt.Y < ActualHeight - footerH)
                {
                    if (_isEditing)
                    {
                        int clickedCol = HitTestColumn(pt.X);
                        if (visualRow != _editingVisualRow || clickedCol != _editingColIndex)
                        {
                            CommitEdit();
                        }
                    }

                    if (_selectionMode == ZeroGridSelectionMode.MultiRow)
                    {
                        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                        {
                            if (_selectedVisualRows.Contains(visualRow))
                                _selectedVisualRows.Remove(visualRow);
                            else
                                _selectedVisualRows.Add(visualRow);
                            _selectedVisualRow = visualRow;
                        }
                        else if ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) && _selectedVisualRow >= 0)
                        {
                            _selectedVisualRows.Clear();
                            int minR = Math.Min(_selectedVisualRow, visualRow);
                            int maxR = Math.Max(_selectedVisualRow, visualRow);
                            for (int r = minR; r <= maxR; r++) _selectedVisualRows.Add(r);
                        }
                        else
                        {
                            _selectedVisualRows.Clear();
                            _selectedVisualRows.Add(visualRow);
                            _selectedVisualRow = visualRow;
                        }
                    }
                    else
                    {
                        _selectedVisualRows.Clear();
                        _selectedVisualRows.Add(visualRow);
                        _selectedVisualRow = visualRow;
                    }

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
            if (_isEditing) InvalidateArrange();
            InvalidateVisual();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.C)
            {
                CopySelectionToClipboard();
                e.Handled = true;
            }
            else if (e.Key == Key.F2 || (e.Key == Key.Enter && !_isEditing))
            {
                if (_selectedVisualRow >= 0 && _selectedVisualRow < _rowIndexMap.ActiveCount)
                {
                    for (int c = 0; c < _columns.Count; c++)
                    {
                        if (_columns[c].IsVisible && !_columns[c].ReadOnly)
                        {
                            StartEdit(_selectedVisualRow, c);
                            e.Handled = true;
                            return;
                        }
                    }
                }
            }
            else if (e.Key == Key.Up && _selectedVisualRow > 0)
            {
                if (_isEditing) CommitEdit();
                _selectedVisualRow--;
                _selectedVisualRows.Clear();
                _selectedVisualRows.Add(_selectedVisualRow);
                EnsureRowVisible(_selectedVisualRow);
                InvalidateVisual();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.Key == Key.Down && _selectedVisualRow < _rowIndexMap.ActiveCount - 1)
            {
                if (_isEditing) CommitEdit();
                _selectedVisualRow++;
                _selectedVisualRows.Clear();
                _selectedVisualRows.Add(_selectedVisualRow);
                EnsureRowVisible(_selectedVisualRow);
                InvalidateVisual();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        public void EnsureRowVisible(int visualRowIndex)
        {
            if (visualRowIndex < 0 || visualRowIndex >= _rowIndexMap.ActiveCount) return;
            int rowTop = visualRowIndex * _rowHeight;
            int rowBottom = rowTop + _rowHeight;
            int footerH = ShowFooter ? _footerHeight : 0;
            int viewH = (int)Math.Max(0, ActualHeight - _headerHeight - footerH);

            if (rowTop < _scrollY)
            {
                _scrollY = rowTop;
                InvalidateVisual();
            }
            else if (rowBottom > _scrollY + viewH)
            {
                _scrollY = rowBottom - viewH;
                InvalidateVisual();
            }
        }

        public void CopySelectionToClipboard()
        {
            if (_dataSource == null) return;

            var rowsToCopy = new List<int>();
            if (_selectedVisualRows.Count > 0)
            {
                rowsToCopy.AddRange(_selectedVisualRows);
                rowsToCopy.Sort();
            }
            else if (_selectedVisualRow >= 0 && _selectedVisualRow < _rowIndexMap.ActiveCount)
            {
                rowsToCopy.Add(_selectedVisualRow);
            }

            if (rowsToCopy.Count == 0) return;

            CellValueBuffer buf = new CellValueBuffer();
            var sb = new System.Text.StringBuilder();

            // Header row
            bool firstCol = true;
            for (int c = 0; c < _columns.Count; c++)
            {
                if (!_columns[c].IsVisible) continue;
                if (!firstCol) sb.Append('\t');
                sb.Append(_columns[c].HeaderText);
                firstCol = false;
            }
            sb.AppendLine();

            // Data rows
            foreach (int vRow in rowsToCopy)
            {
                if (vRow < 0 || vRow >= _rowIndexMap.ActiveCount) continue;
                int modelRow = _rowIndexMap[vRow];
                firstCol = true;
                for (int c = 0; c < _columns.Count; c++)
                {
                    if (!_columns[c].IsVisible) continue;
                    if (!firstCol) sb.Append('\t');
                    buf.Reset();
                    _dataSource.GetCellValue(modelRow, c, ref buf);
                    sb.Append(buf.Text.ToString());
                    firstCol = false;
                }
                sb.AppendLine();
            }

            try { Clipboard.SetText(sb.ToString()); } catch { }
        }

        public Rect GetCellRectangle(int visualRow, int colIndex)
        {
            if (visualRow < 0 || colIndex < 0 || colIndex >= _columns.Count) return Rect.Empty;

            double cellY = _headerHeight + (visualRow * _rowHeight) - _scrollY;
            int pinnedW = GetPinnedColumnsWidth();

            double cellX;
            if (_columns[colIndex].IsPinned)
            {
                cellX = 0;
                for (int c = 0; c < colIndex; c++)
                {
                    if (_columns[c].IsVisible && _columns[c].IsPinned) cellX += _columns[c].Width;
                }
            }
            else
            {
                cellX = pinnedW - _scrollX;
                for (int c = 0; c < colIndex; c++)
                {
                    if (_columns[c].IsVisible && !_columns[c].IsPinned) cellX += _columns[c].Width;
                }
            }

            return new Rect(cellX, cellY, _columns[colIndex].Width, _rowHeight);
        }

        public void StartEdit(int visualRow, int colIndex)
        {
            if (_dataSource == null || visualRow < 0 || visualRow >= _rowIndexMap.ActiveCount ||
                colIndex < 0 || colIndex >= _columns.Count) return;

            var col = _columns[colIndex];
            if (col.ReadOnly || !col.IsVisible) return;

            int modelRow = _rowIndexMap[visualRow];
            if (_dataSource is IZeroEditableSource editable && !editable.IsCellEditable(modelRow, colIndex))
            {
                return;
            }

            if (_isEditing) CommitEdit();

            EnsureRowVisible(visualRow);

            var rect = GetCellRectangle(visualRow, colIndex);
            if (rect.Width <= 0 || rect.Height <= 0) return;

            CellValueBuffer buf = new CellValueBuffer();
            _dataSource.GetCellValue(modelRow, colIndex, ref buf);
            string val = buf.Text.ToString();

            _isEditing = true;
            _editingVisualRow = visualRow;
            _editingColIndex = colIndex;

            UpdateEditorTheme();
            _inPlaceEditor.Text = val;
            _inPlaceEditor.Visibility = Visibility.Visible;
            _inPlaceEditor.Arrange(rect);
            _inPlaceEditor.Focus();
            _inPlaceEditor.SelectAll();

            CellBeginEdit?.Invoke(this, EventArgs.Empty);
        }

        public void CommitEdit()
        {
            if (!_isEditing || _dataSource == null) return;

            int visualRow = _editingVisualRow;
            int colIndex = _editingColIndex;
            string newText = _inPlaceEditor.Text;

            _inPlaceEditor.Visibility = Visibility.Collapsed;
            _isEditing = false;
            _editingVisualRow = -1;
            _editingColIndex = -1;

            if (visualRow >= 0 && visualRow < _rowIndexMap.ActiveCount && colIndex >= 0 && colIndex < _columns.Count)
            {
                int modelRow = _rowIndexMap[visualRow];
                CellValueBuffer buf = new CellValueBuffer();
                _dataSource.GetCellValue(modelRow, colIndex, ref buf);
                string oldText = buf.Text.ToString();

                if (oldText != newText)
                {
                    if (_dataSource is IZeroEditableSource editable)
                    {
                        editable.SetCellValue(modelRow, colIndex, newText);
                    }
                    CellValueChanged?.Invoke(this, new CellValueChangedEventArgs(visualRow, modelRow, colIndex, oldText, newText));
                    InvalidateVisual();
                }
            }

            CellEndEdit?.Invoke(this, EventArgs.Empty);
        }

        public void CancelEdit()
        {
            if (!_isEditing) return;

            _inPlaceEditor.Visibility = Visibility.Collapsed;
            _isEditing = false;
            _editingVisualRow = -1;
            _editingColIndex = -1;

            CellEndEdit?.Invoke(this, EventArgs.Empty);
            InvalidateVisual();
            Focus();
        }

        private void InPlaceEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommitEdit();
                if (_selectedVisualRow < _rowIndexMap.ActiveCount - 1)
                {
                    _selectedVisualRow++;
                    _selectedVisualRows.Clear();
                    _selectedVisualRows.Add(_selectedVisualRow);
                    EnsureRowVisible(_selectedVisualRow);
                    InvalidateVisual();
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                CancelEdit();
                e.Handled = true;
            }
            else if (e.Key == Key.Tab)
            {
                int nextCol = _editingColIndex + 1;
                int nextRow = _editingVisualRow;
                CommitEdit();
                while (nextCol < _columns.Count && (_columns[nextCol].ReadOnly || !_columns[nextCol].IsVisible))
                {
                    nextCol++;
                }
                if (nextCol < _columns.Count)
                {
                    StartEdit(nextRow, nextCol);
                }
                e.Handled = true;
            }
        }

        public string GetColumnSummaryText(int colIndex)
        {
            if (colIndex < 0 || colIndex >= _columns.Count || _dataSource == null) return string.Empty;
            var col = _columns[colIndex];
            if (col.Summary == SummaryType.None) return string.Empty;

            int count = _rowIndexMap.ActiveCount;
            if (col.Summary == SummaryType.Count)
            {
                return !string.IsNullOrEmpty(col.SummaryFormat)
                    ? string.Format(CultureInfo.InvariantCulture, col.SummaryFormat, count)
                    : $"Count: {count:N0}";
            }

            if (count == 0) return "-";

            double sum = 0;
            double min = double.MaxValue;
            double max = double.MinValue;
            int validCount = 0;

            CellValueBuffer buf = new CellValueBuffer();
            for (int i = 0; i < count; i++)
            {
                int mRow = _rowIndexMap[i];
                _dataSource.GetCellValue(mRow, colIndex, ref buf);
                string s = buf.Text.ToString();
                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double val) ||
                    double.TryParse(s.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out val))
                {
                    sum += val;
                    if (val < min) min = val;
                    if (val > max) max = val;
                    validCount++;
                }
            }

            if (validCount == 0) return "-";

            double result = col.Summary switch
            {
                SummaryType.Sum => sum,
                SummaryType.Average => sum / validCount,
                SummaryType.Min => min,
                SummaryType.Max => max,
                _ => 0
            };

            if (!string.IsNullOrEmpty(col.SummaryFormat))
            {
                return string.Format(CultureInfo.InvariantCulture, col.SummaryFormat, result);
            }

            return col.Summary switch
            {
                SummaryType.Sum => $"Σ {result:N0}",
                SummaryType.Average => $"μ {result:N1}",
                SummaryType.Min => $"Min {result:N0}",
                SummaryType.Max => $"Max {result:N0}",
                _ => result.ToString(CultureInfo.InvariantCulture)
            };
        }

        private int HitTestColumnDivider(double mouseX)
        {
            int pinnedW = GetPinnedColumnsWidth();
            if (mouseX < pinnedW)
            {
                double curX = 0;
                for (int i = 0; i < _columns.Count; i++)
                {
                    if (!_columns[i].IsVisible || !_columns[i].IsPinned) continue;
                    curX += _columns[i].Width;
                    if (Math.Abs(mouseX - curX) <= 4) return i;
                }
            }
            else
            {
                double curX = pinnedW - _scrollX;
                for (int i = 0; i < _columns.Count; i++)
                {
                    if (!_columns[i].IsVisible || _columns[i].IsPinned) continue;
                    curX += _columns[i].Width;
                    if (Math.Abs(mouseX - curX) <= 4) return i;
                }
            }
            return -1;
        }

        private int HitTestColumn(double mouseX)
        {
            int pinnedW = GetPinnedColumnsWidth();
            if (mouseX < pinnedW)
            {
                double curX = 0;
                for (int i = 0; i < _columns.Count; i++)
                {
                    if (!_columns[i].IsVisible || !_columns[i].IsPinned) continue;
                    if (mouseX >= curX && mouseX < curX + _columns[i].Width) return i;
                    curX += _columns[i].Width;
                }
            }
            else
            {
                double curX = pinnedW - _scrollX;
                for (int i = 0; i < _columns.Count; i++)
                {
                    if (!_columns[i].IsVisible || _columns[i].IsPinned) continue;
                    if (mouseX >= curX && mouseX < curX + _columns[i].Width) return i;
                    curX += _columns[i].Width;
                }
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
