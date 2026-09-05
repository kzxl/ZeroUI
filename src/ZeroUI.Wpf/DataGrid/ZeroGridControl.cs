using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Core.Common;
using ZeroUI.Core.Data;
using ZeroUI.Core.Virtualization;
using ZeroUI.Wpf.Editors;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.DataGrid
{
    /// <summary>
    /// Ultra high-performance Single-Visual Virtual DataGrid for WPF.
    /// Eliminates WPF Visual Tree overhead by rendering cells directly via DrawingContext,
    /// powered by ZeroUI.Core virtualization algorithms and RowIndexMap.
    /// </summary>
    public class GridControl : FrameworkElement
    {
        private static readonly Pen WhiteCheckPen;

        static GridControl()
        {
            WhiteCheckPen = new Pen(Brushes.White, 1.8);
            WhiteCheckPen.Freeze();
        }

        private readonly ObservableCollection<ZeroColumn> _columns = new ObservableCollection<ZeroColumn>();
        private RowIndexMap _rowIndexMap = new RowIndexMap(10000);
        private readonly GroupedRowIndexMap _groupedMap = new GroupedRowIndexMap();
        private int[] _groupColumnIndices = Array.Empty<int>();
        private readonly ObservableCollection<GridBand> _bands = new ObservableCollection<GridBand>();
        private IZeroVirtualSource? _dataSource;

        private int _headerHeight = 32;
        private int _rowHeight = 28;
        private int _scrollX = 0;
        private int _scrollY = 0;

        public ObservableCollection<GridBand> Bands => _bands;

        public int EffectiveHeaderHeight
        {
            get
            {
                if (_bands.Count == 0) return _headerHeight;
                int maxDepth = 0;
                for (int i = 0; i < _bands.Count; i++)
                {
                    int d = _bands[i].GetMaxDepth();
                    if (d > maxDepth) maxDepth = d;
                }
                return (maxDepth + 1) * _headerHeight;
            }
        }

        private int GetMaxBandDepth()
        {
            int maxDepth = 0;
            for (int i = 0; i < _bands.Count; i++)
            {
                int d = _bands[i].GetMaxDepth();
                if (d > maxDepth) maxDepth = d;
            }
            return maxDepth;
        }

        // Interactive Group Panel & Summaries
        private bool _showGroupPanel = false;
        private int _groupPanelHeight = 34;
        private readonly ObservableCollection<GroupSummaryItem> _groupSummaries = new ObservableCollection<GroupSummaryItem>();
        private readonly ObservableCollection<ConditionalFormattingRule> _conditionalRules = new ObservableCollection<ConditionalFormattingRule>();
        private readonly Dictionary<int, HashSet<string>> _columnDistinctFilters = new Dictionary<int, HashSet<string>>();
        private readonly Dictionary<int, Rect> _columnFilterButtonBounds = new Dictionary<int, Rect>();

        // Master-Detail
        private bool _allowMasterDetail = false;
        private readonly HashSet<int> _expandedMasterRows = new HashSet<int>();
        public event EventHandler<int>? MasterRowExpanded;
        public event EventHandler<int>? MasterRowCollapsed;

        // Group Chip Hit Testing & Dragging
        private struct GroupChipInfo
        {
            public int ColumnIndex;
            public Rect ChipRect;
            public Rect CloseRect;
        }
        private readonly List<GroupChipInfo> _groupChipBounds = new List<GroupChipInfo>();
        private bool _isDraggingHeader = false;
        private int _draggedHeaderCol = -1;
        private Point _headerDragStart;
        private Point _currentMousePos;

        public bool ShowGroupPanel
        {
            get => _showGroupPanel;
            set
            {
                if (_showGroupPanel != value)
                {
                    _showGroupPanel = value;
                    InvalidateVisual();
                }
            }
        }

        public int GroupPanelHeight
        {
            get => _groupPanelHeight;
            set
            {
                if (_groupPanelHeight != value)
                {
                    _groupPanelHeight = value;
                    InvalidateVisual();
                }
            }
        }

        public int TotalTopOffset => (ShowGroupPanel ? _groupPanelHeight : 0) + EffectiveHeaderHeight;
        public ObservableCollection<GroupSummaryItem> GroupSummaries => _groupSummaries;
        public ObservableCollection<ConditionalFormattingRule> ConditionalRules => _conditionalRules;

        public bool AllowMasterDetail
        {
            get => _allowMasterDetail;
            set
            {
                if (_allowMasterDetail != value)
                {
                    _allowMasterDetail = value;
                    InvalidateVisual();
                }
            }
        }

        public bool IsMasterRowExpanded(int modelRowIndex) => _expandedMasterRows.Contains(modelRowIndex);

        public void ToggleMasterRow(int modelRowIndex)
        {
            if (_expandedMasterRows.Contains(modelRowIndex))
            {
                _expandedMasterRows.Remove(modelRowIndex);
                MasterRowCollapsed?.Invoke(this, modelRowIndex);
            }
            else
            {
                _expandedMasterRows.Add(modelRowIndex);
                MasterRowExpanded?.Invoke(this, modelRowIndex);
            }
            InvalidateVisual();
        }

        // Selection & Interaction
        private int _selectedVisualRow = -1;
        private int _hoveredVisualRow = -1;
        private readonly HashSet<int> _selectedVisualRows = new HashSet<int>();
        private ZeroGridSelectionMode _selectionMode = ZeroGridSelectionMode.SingleRow;
        private CellRange _selectedBlock = CellRange.Empty;
        private bool _isSelectingBlock = false;
        private bool _isResizingColumn = false;

        public CellRange SelectedBlock => _selectedBlock;
        private int _resizingColIndex = -1;
        private double _resizeStartX = 0;
        private int _resizeStartWidth = 0;

        // In-Place Floating Editors (Pluggable)
        private readonly VisualCollection _visualChildren;
        private readonly TextBox _inPlaceEditor;
        private readonly ZeroNumericBox _numericEditor;
        private readonly ZeroDatePicker _dateEditor;
        private readonly ZeroMaskedTextBox _maskedEditor;
        private FrameworkElement? _activeEditor;
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
                typeof(GridControl),
                new FrameworkPropertyMetadata(GridDensity.Middle, FrameworkPropertyMetadataOptions.AffectsRender, OnDensityChanged));

        public GridDensity Density
        {
            get => (GridDensity)GetValue(DensityProperty);
            set => SetValue(DensityProperty, value);
        }

        private static void OnDensityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GridControl grid)
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
                    if (_groupColumnIndices.Length > 0)
                    {
                        GroupBy(_groupColumnIndices);
                    }
                    else
                    {
                        _groupedMap.ResetIdentity(_dataSource.TotalRowCount);
                    }
                }
                else
                {
                    _rowIndexMap.ActiveCount = 0;
                    _groupedMap.ResetIdentity(0);
                }
                _scrollY = 0;
                _selectedVisualRow = -1;
                _selectedVisualRows.Clear();
                InvalidateVisual();
            }
        }

        public int VisualRowCount => _groupedMap.HasGrouping ? _groupedMap.ActiveCount : _rowIndexMap.ActiveCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetModelRowIndex(int visualRowIndex)
        {
            if (_groupedMap.HasGrouping)
            {
                if (visualRowIndex >= 0 && visualRowIndex < _groupedMap.ActiveCount)
                {
                    var entry = _groupedMap[visualRowIndex];
                    return entry.IsData ? entry.ModelRowIndex : -1;
                }
                return -1;
            }
            if (visualRowIndex >= 0 && visualRowIndex < _rowIndexMap.ActiveCount)
            {
                return _rowIndexMap[visualRowIndex];
            }
            return -1;
        }

        public int SelectedIndex
        {
            get => GetModelRowIndex(_selectedVisualRow);
            set
            {
                if (value < 0 || _dataSource == null || value >= _dataSource.TotalRowCount)
                {
                    _selectedVisualRow = -1;
                    _selectedVisualRows.Clear();
                }
                else
                {
                    int total = VisualRowCount;
                    for (int i = 0; i < total; i++)
                    {
                        if (GetModelRowIndex(i) == value)
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

        public bool HasGrouping => _groupedMap.HasGrouping;
        public IReadOnlyList<GroupRowInfo> RootGroups => _groupedMap.RootGroups;
        public GroupedRowIndexMap GroupedMap => _groupedMap;

        public void GroupBy(params int[] columnIndices)
        {
            if (_dataSource == null || columnIndices == null || columnIndices.Length == 0)
            {
                ClearGrouping();
                return;
            }

            _groupColumnIndices = (int[])columnIndices.Clone();
            int totalRows = _dataSource.TotalRowCount;

            _groupedMap.BuildGroups(totalRows, columnIndices, (modelRow, colIdx) =>
            {
                CellValueBuffer buf = new CellValueBuffer();
                _dataSource.GetCellValue(modelRow, colIdx, ref buf);
                return buf.Text.ToString();
            });

            if (_groupSummaries.Count > 0)
            {
                RecalculateGroupSummaries();
            }

            _scrollY = 0;
            _selectedVisualRow = -1;
            _selectedVisualRows.Clear();
            InvalidateVisual();
        }

        public void RecalculateGroupSummaries()
        {
            if (_dataSource == null || !_groupedMap.HasGrouping) return;

            if (_groupSummaries.Count == 0)
            {
                _groupedMap.CalculateSummaries(_groupSummaries, (r, c) => 0);
            }
            else
            {
                _groupedMap.CalculateSummaries(_groupSummaries, (modelRow, colIdx) =>
                {
                    CellValueBuffer buf = new CellValueBuffer();
                    _dataSource.GetCellValue(modelRow, colIdx, ref buf);
                    var s = buf.Text.ToString();
                    if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double val)) return val;
                    if (double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out double val2)) return val2;
                    return 0;
                });
            }
            InvalidateVisual();
        }

        public bool IsColumnFiltered(int columnIndex) => _columnDistinctFilters.ContainsKey(columnIndex);

        public List<string> GetDistinctColumnValues(int columnIndex)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_dataSource != null)
            {
                int total = _dataSource.TotalRowCount;
                CellValueBuffer buf = new CellValueBuffer();
                for (int r = 0; r < total; r++)
                {
                    _dataSource.GetCellValue(r, columnIndex, ref buf);
                    set.Add(buf.Text.ToString());
                }
            }
            var list = new List<string>(set);
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        public void ApplyDistinctColumnFilter(int columnIndex, HashSet<string>? selectedValues)
        {
            if (selectedValues == null || selectedValues.Count == 0)
            {
                _columnDistinctFilters.Remove(columnIndex);
            }
            else
            {
                _columnDistinctFilters[columnIndex] = selectedValues;
            }

            ReapplyAllFilters();
        }

        public void ClearAllFilters()
        {
            _columnDistinctFilters.Clear();
            ReapplyAllFilters();
        }

        public void ReapplyAllFilters()
        {
            if (_dataSource == null) return;
            int total = _dataSource.TotalRowCount;

            if (_columnDistinctFilters.Count == 0)
            {
                _rowIndexMap.ResetIdentity(total);
            }
            else
            {
                _rowIndexMap.Filter(modelRow =>
                {
                    CellValueBuffer buf = new CellValueBuffer();
                    foreach (var kvp in _columnDistinctFilters)
                    {
                        _dataSource.GetCellValue(modelRow, kvp.Key, ref buf);
                        if (!kvp.Value.Contains(buf.Text.ToString())) return false;
                    }
                    return true;
                }, total);
            }

            if (_groupedMap.HasGrouping && _groupColumnIndices.Length > 0)
            {
                GroupBy(_groupColumnIndices);
            }
            else
            {
                _scrollY = 0;
                InvalidateVisual();
            }
        }

        public void ShowColumnFilterPopup(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= _columns.Count) return;
            var col = _columns[columnIndex];
            var distinctValues = GetDistinctColumnValues(columnIndex);
            _columnDistinctFilters.TryGetValue(columnIndex, out var currentSelection);

            var popup = new ZeroColumnFilterPopup(this, columnIndex, col.HeaderText, distinctValues, currentSelection, (colIdx, selected) =>
            {
                ApplyDistinctColumnFilter(colIdx, selected);
            });
            popup.IsOpen = true;
        }

        public void ClearGrouping()
        {
            _groupColumnIndices = Array.Empty<int>();
            if (_dataSource != null)
            {
                _groupedMap.ResetIdentity(_dataSource.TotalRowCount);
                _rowIndexMap.ResetIdentity(_dataSource.TotalRowCount);
            }
            else
            {
                _groupedMap.ResetIdentity(0);
                _rowIndexMap.ActiveCount = 0;
            }
            _scrollY = 0;
            _selectedVisualRow = -1;
            _selectedVisualRows.Clear();
            InvalidateVisual();
        }

        public void ExpandAllGroups()
        {
            if (_groupedMap.HasGrouping)
            {
                _groupedMap.ExpandAll();
                InvalidateVisual();
            }
        }

        public void CollapseAllGroups()
        {
            if (_groupedMap.HasGrouping)
            {
                _groupedMap.CollapseAll();
                InvalidateVisual();
            }
        }

        public bool ToggleGroup(int visualRowIndex)
        {
            if (_groupedMap.HasGrouping && _groupedMap.ToggleGroup(visualRowIndex))
            {
                InvalidateVisual();
                return true;
            }
            return false;
        }

        public ZeroGridSelectionMode SelectionMode
        {
            get => _selectionMode;
            set
            {
                _selectionMode = value;
                _selectedBlock = CellRange.Empty;
                _selectedVisualRows.Clear();
                InvalidateVisual();
            }
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

        public GridControl()
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

            _numericEditor = new ZeroNumericBox
            {
                Visibility = Visibility.Collapsed,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4, 2, 4, 2),
                FontSize = 12.0
            };
            _numericEditor.KeyDown += InPlaceEditor_KeyDown;
            _numericEditor.LostFocus += (s, e) => CommitEdit();
            _visualChildren.Add(_numericEditor);

            _dateEditor = new ZeroDatePicker
            {
                Visibility = Visibility.Collapsed,
                BorderThickness = new Thickness(1),
                FontSize = 12.0,
                ShowPresets = false
            };
            _dateEditor.KeyDown += InPlaceEditor_KeyDown;
            _dateEditor.LostFocus += (s, e) =>
            {
                if (_dateEditor.IsDropDownOpen) return;
                CommitEdit();
            };
            _visualChildren.Add(_dateEditor);

            _maskedEditor = new ZeroMaskedTextBox
            {
                Visibility = Visibility.Collapsed,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4, 2, 4, 2),
                FontSize = 12.0
            };
            _maskedEditor.KeyDown += InPlaceEditor_KeyDown;
            _maskedEditor.LostFocus += (s, e) => CommitEdit();
            _visualChildren.Add(_maskedEditor);

            _columns.CollectionChanged += (s, e) => InvalidateVisual();
            _groupSummaries.CollectionChanged += (s, e) => RecalculateGroupSummaries();
            _conditionalRules.CollectionChanged += (s, e) => InvalidateVisual();
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

            _maskedEditor.Background = ZeroWpfTheme.BgInput;
            _maskedEditor.Foreground = ZeroWpfTheme.TextPrimary;
            _maskedEditor.BorderBrush = ZeroWpfTheme.PrimaryAccent;
            _maskedEditor.CaretBrush = ZeroWpfTheme.PrimaryAccent;
        }

        protected override int VisualChildrenCount => _visualChildren.Count;
        protected override Visual GetVisualChild(int index) => _visualChildren[index];

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_isEditing && _activeEditor != null && _activeEditor.Visibility == Visibility.Visible)
            {
                var rect = GetCellRectangle(_editingVisualRow, _editingColIndex);
                int footerH = ShowFooter ? _footerHeight : 0;
                if (rect.Y < TotalTopOffset || rect.Bottom > finalSize.Height - footerH)
                {
                    CommitEdit();
                }
                else
                {
                    _activeEditor.Arrange(rect);
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
            int totalRows = VisualRowCount;
            int totalH = totalRows * _rowHeight;
            int clientH = (int)Math.Max(0, ActualHeight - TotalTopOffset - footerH);
            return Math.Max(0, totalH - clientH);
        }

        private int GetMaxScrollX()
        {
            int unpinnedW = GetUnpinnedColumnsWidth();
            int pinnedW = GetPinnedColumnsWidth();
            int scrollableW = (int)Math.Max(0, ActualWidth - pinnedW - ScrollBarThickness);
            return Math.Max(0, unpinnedW - scrollableW);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTruthy(ReadOnlySpan<char> span)
        {
            if (span.IsEmpty) return false;
            if (span.Length == 1)
            {
                char c = span[0];
                return c == '1' || c == 't' || c == 'T' || c == 'y' || c == 'Y';
            }
            if (span.Length == 4)
            {
                return (span[0] == 't' || span[0] == 'T') &&
                       (span[1] == 'r' || span[1] == 'R') &&
                       (span[2] == 'u' || span[2] == 'U') &&
                       (span[3] == 'e' || span[3] == 'E');
            }
            if (span.Length == 3)
            {
                return (span[0] == 'y' || span[0] == 'Y') &&
                       (span[1] == 'e' || span[1] == 'E') &&
                       (span[2] == 's' || span[2] == 'S');
            }
            return false;
        }

        private void DrawVectorCheckBox(DrawingContext dc, double x, double y, double width, double height, bool isChecked, bool isSelected)
        {
            double cbSize = 16.0;
            double cbX = x + (width - cbSize) / 2.0;
            double cbY = y + (height - cbSize) / 2.0;
            var rect = new Rect(cbX, cbY, cbSize, cbSize);

            if (isChecked)
            {
                dc.DrawRoundedRectangle(ZeroWpfTheme.PrimaryAccent, null, rect, 3.0, 3.0);

                var checkGeometry = new StreamGeometry();
                using (var ctx = checkGeometry.Open())
                {
                    ctx.BeginFigure(new Point(cbX + 3.5, cbY + 8.0), false, false);
                    ctx.LineTo(new Point(cbX + 6.5, cbY + 11.5), true, false);
                    ctx.LineTo(new Point(cbX + 12.5, cbY + 4.5), true, false);
                }
                checkGeometry.Freeze();
                dc.DrawGeometry(null, WhiteCheckPen, checkGeometry);
            }
            else
            {
                Brush bg = isSelected ? ZeroWpfTheme.SelectionBackground : ZeroWpfTheme.BgInput;
                dc.DrawRoundedRectangle(bg, ZeroWpfTheme.BorderPen, rect, 3.0, 3.0);
            }
        }

        private void DrawDataBar(DrawingContext dc, double x, double y, double width, double height, float percent, Brush? barBrush = null)
        {
            float clamped = Math.Max(0.0f, Math.Min(1.0f, percent));
            double barH = Math.Max(6.0, height - 10.0);
            double barMaxW = Math.Max(0.0, width - 16.0);
            double barFillW = barMaxW * clamped;
            double barX = x + 8.0;
            double barY = y + (height - barH) / 2.0;

            // Track
            dc.DrawRoundedRectangle(ZeroWpfTheme.BgInput, null, new Rect(barX, barY, barMaxW, barH), 2.0, 2.0);
            // Fill
            if (barFillW > 0)
            {
                Brush brush = barBrush ?? ZeroWpfTheme.PrimaryAccent;
                dc.DrawRoundedRectangle(brush, null, new Rect(barX, barY, barFillW, barH), 2.0, 2.0);
            }
        }

        private void DrawSparkline(DrawingContext dc, double x, double y, double width, double height, ReadOnlySpan<float> values, SparklineType type, Brush? strokeBrush = null)
        {
            if (values.Length < 2) return;

            double padX = 6.0;
            double padY = 4.0;
            double availW = Math.Max(0.0, width - (padX * 2.0));
            double availH = Math.Max(0.0, height - (padY * 2.0));
            if (availW <= 0 || availH <= 0) return;

            float min = values[0];
            float max = values[0];
            for (int i = 1; i < values.Length; i++)
            {
                if (values[i] < min) min = values[i];
                if (values[i] > max) max = values[i];
            }
            float range = (max - min <= 0.0001f) ? 1.0f : (max - min);

            Brush brush = strokeBrush ?? ZeroWpfTheme.PrimaryAccent;
            Pen pen = new Pen(brush, 1.5);
            pen.Freeze();

            if (type == SparklineType.Bar)
            {
                double barSlotW = availW / values.Length;
                double barWidth = Math.Max(1.0, barSlotW - 2.0);
                for (int i = 0; i < values.Length; i++)
                {
                    float norm = (values[i] - min) / range;
                    double bh = Math.Max(2.0, norm * availH);
                    double bx = x + padX + (i * barSlotW) + 1.0;
                    double by = y + padY + availH - bh;
                    dc.DrawRectangle(brush, null, new Rect(bx, by, barWidth, bh));
                }
            }
            else
            {
                // Line or Area
                var geom = new StreamGeometry();
                using (var ctx = geom.Open())
                {
                    double stepX = availW / (values.Length - 1);
                    double firstY = y + padY + availH - (((values[0] - min) / range) * availH);
                    ctx.BeginFigure(new Point(x + padX, firstY), type == SparklineType.Area, type == SparklineType.Area);

                    for (int i = 1; i < values.Length; i++)
                    {
                        double px = x + padX + (i * stepX);
                        double py = y + padY + availH - (((values[i] - min) / range) * availH);
                        ctx.LineTo(new Point(px, py), true, false);
                    }

                    if (type == SparklineType.Area)
                    {
                        ctx.LineTo(new Point(x + padX + availW, y + padY + availH), true, false);
                        ctx.LineTo(new Point(x + padX, y + padY + availH), true, false);
                    }
                }
                geom.Freeze();

                if (type == SparklineType.Area)
                {
                    var areaBrush = brush.Clone();
                    areaBrush.Opacity = 0.25;
                    areaBrush.Freeze();
                    dc.DrawGeometry(areaBrush, pen, geom);
                }
                else
                {
                    dc.DrawGeometry(null, pen, geom);
                }
            }
        }

        private void ToggleBooleanCell(int visualRow, int colIndex)
        {
            if (_dataSource == null || visualRow < 0 || visualRow >= VisualRowCount || colIndex < 0 || colIndex >= _columns.Count) return;
            var col = _columns[colIndex];
            if (col.ReadOnly || !col.IsVisible) return;

            int modelRow = GetModelRowIndex(visualRow);
            if (modelRow < 0) return;
            if (_dataSource is IZeroEditableSource editable && !editable.IsCellEditable(modelRow, colIndex)) return;

            CellValueBuffer buf = new CellValueBuffer();
            _dataSource.GetCellValue(modelRow, colIndex, ref buf);
            bool isTrue = IsTruthy(buf.Text);
            string newVal = isTrue ? "false" : "true";

            if (_dataSource is IZeroEditableSource editableSrc)
            {
                editableSrc.SetCellValue(modelRow, colIndex, newVal);
                CellValueChanged?.Invoke(this, new CellValueChangedEventArgs(visualRow, modelRow, colIndex, buf.Text.ToString(), newVal));
                InvalidateVisual();
            }
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
            int totalRows = VisualRowCount;
            int[] colWidths = GetVisibleColumnWidths();
            int pinnedW = GetPinnedColumnsWidth();
            int footerH = ShowFooter ? _footerHeight : 0;
            int effHeaderH = EffectiveHeaderHeight;
            int groupPanelH = ShowGroupPanel ? _groupPanelHeight : 0;
            int totalTopOffset = groupPanelH + effHeaderH;
            int bandDepth = (_bands.Count > 0) ? GetMaxBandDepth() : 0;
            int bandH = bandDepth * _headerHeight;
            int clientDataH = (int)Math.Max(0, height - totalTopOffset - footerH);

            // 2. Render Virtual Cells
            if (_dataSource != null && totalRows > 0 && totalCols > 0)
            {
                int startRow = Math.Max(0, _scrollY / _rowHeight);
                int visibleRowCount = (clientDataH / _rowHeight) + 2;
                int endRow = Math.Min(totalRows - 1, startRow + visibleRowCount);

                CellValueBuffer cellBuffer = new CellValueBuffer();
                double currentY = totalTopOffset + (startRow * _rowHeight) - _scrollY;

                for (int r = startRow; r <= endRow && r < totalRows; r++)
                {
                    if (currentY >= totalTopOffset + clientDataH) break;

                    if (_groupedMap.HasGrouping)
                    {
                        var rowEntry = _groupedMap[r];
                        if (rowEntry.IsGroup)
                        {
                            var groupInfo = _groupedMap.GetGroupInfo(rowEntry.GroupId);
                            dc.DrawRectangle(ZeroWpfTheme.BgCard, null, new Rect(0, currentY, width - ScrollBarThickness, _rowHeight));
                            double indent = rowEntry.Level * 18.0;
                            string expandIcon = rowEntry.IsExpanded ? "▼ " : "▶ ";
                            string colHeader = (groupInfo != null && groupInfo.ColumnIndex >= 0 && groupInfo.ColumnIndex < _columns.Count)
                                ? _columns[groupInfo.ColumnIndex].HeaderText
                                : "Group";
                            string groupText = $"{expandIcon}{colHeader}: {(groupInfo?.GroupKey ?? string.Empty)} ({groupInfo?.TotalDataRowCount ?? 0} items)";

                            var ftGroup = CreateFormattedText(groupText, ZeroWpfTheme.BoldTypeface, 12.0, ZeroWpfTheme.PrimaryAccent, dpi);
                            dc.DrawText(ftGroup, new Point(8 + indent, currentY + (_rowHeight - ftGroup.Height) / 2.0));

                            if (groupInfo != null && !string.IsNullOrEmpty(groupInfo.FormattedSummaryText))
                            {
                                var ftSummary = CreateFormattedText(groupInfo.FormattedSummaryText!, ZeroWpfTheme.BoldTypeface, 11.5, ZeroWpfTheme.TextSecondary, dpi);
                                double sX = 8 + indent + ftGroup.Width + 18.0;
                                if (sX + ftSummary.Width < width - ScrollBarThickness)
                                {
                                    dc.DrawText(ftSummary, new Point(sX, currentY + (_rowHeight - ftSummary.Height) / 2.0));
                                }
                            }

                            dc.DrawLine(ZeroWpfTheme.GridLinePen, new Point(0, currentY + _rowHeight - 0.5), new Point(width - ScrollBarThickness, currentY + _rowHeight - 0.5));
                            currentY += _rowHeight;
                            continue;
                        }
                    }

                    int modelRow = GetModelRowIndex(r);
                    if (modelRow < 0)
                    {
                        currentY += _rowHeight;
                        continue;
                    }

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

                            bool isBlockSelected = (_selectionMode == ZeroGridSelectionMode.Block && !_selectedBlock.IsEmpty && _selectedBlock.Contains(r, c));
                            if (isBlockSelected && !isSelected)
                            {
                                dc.DrawRectangle(ZeroWpfTheme.SelectionBackground, null, new Rect(unpinnedX, currentY, colW, _rowHeight));
                            }

                            bool isMerged = false;
                            if (_columns[c].AllowCellMerge && r > startRow)
                            {
                                int prevModel = GetModelRowIndex(r - 1);
                                if (prevModel >= 0)
                                {
                                    CellValueBuffer prevBuf = new CellValueBuffer();
                                    _dataSource.GetCellValue(prevModel, c, ref prevBuf);
                                    if (prevBuf.Text.SequenceEqual(cellBuffer.Text))
                                    {
                                        isMerged = true;
                                    }
                                }
                            }

                            if (cellBuffer.HasCustomBackground && !isSelected && !isBlockSelected)
                            {
                                byte a = (byte)((cellBuffer.BackColor >> 24) & 0xFF);
                                byte b = (byte)((cellBuffer.BackColor >> 16) & 0xFF);
                                byte g = (byte)((cellBuffer.BackColor >> 8) & 0xFF);
                                byte rCol = (byte)(cellBuffer.BackColor & 0xFF);
                                if (a == 0) a = 255;
                                var customBrush = new SolidColorBrush(Color.FromArgb(a, rCol, g, b));
                                dc.DrawRectangle(customBrush, null, new Rect(unpinnedX, currentY, colW, _rowHeight));
                            }

                            if (_columns[c].ColumnType == GridColumnType.Boolean)
                            {
                                bool isChecked = IsTruthy(cellBuffer.Text);
                                DrawVectorCheckBox(dc, unpinnedX, currentY, colW, _rowHeight, isChecked, isSelected);
                            }
                            else if (cellBuffer.DataBarPercent >= 0.0f)
                            {
                                DrawDataBar(dc, unpinnedX, currentY, colW, _rowHeight, cellBuffer.DataBarPercent);
                                var textSpan = cellBuffer.Text;
                                if (!textSpan.IsEmpty)
                                {
                                    string text = cellBuffer.Text.ToString();
                                    Brush textBrush = (isSelected || isBlockSelected) ? ZeroWpfTheme.SelectionForeground : ZeroWpfTheme.TextPrimary;
                                    Typeface tf = (isSelected || isBlockSelected) ? ZeroWpfTheme.BoldTypeface : ZeroWpfTheme.RegularTypeface;
                                    var ft = CreateFormattedText(text, tf, 12.0, textBrush, dpi);
                                    double textX = unpinnedX + 8;
                                    if (cellBuffer.Alignment == CellAlignment.Right) textX = unpinnedX + colW - ft.Width - 8;
                                    else if (cellBuffer.Alignment == CellAlignment.Center) textX = unpinnedX + (colW - ft.Width) / 2.0;
                                    double textY = currentY + (_rowHeight - ft.Height) / 2.0;
                                    dc.DrawText(ft, new Point(textX, textY));
                                }
                            }
                            else if (!cellBuffer.SparklineValues.IsEmpty || _columns[c].Sparkline != SparklineType.None)
                            {
                                SparklineType sType = _columns[c].Sparkline != SparklineType.None ? _columns[c].Sparkline : SparklineType.Line;
                                DrawSparkline(dc, unpinnedX, currentY, colW, _rowHeight, cellBuffer.SparklineValues, sType);
                            }
                            else if (!isMerged)
                            {
                                var textSpan = cellBuffer.Text;
                                if (!textSpan.IsEmpty)
                                {
                                    string text = cellBuffer.Text.ToString();
                                    Brush textBrush = (isSelected || isBlockSelected) ? ZeroWpfTheme.SelectionForeground : ZeroWpfTheme.TextPrimary;
                                    Typeface tf = (isSelected || isBlockSelected) ? ZeroWpfTheme.BoldTypeface : ZeroWpfTheme.RegularTypeface;

                                    if (!isSelected && !isBlockSelected && cellBuffer.TextColor != 0)
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
                            }

                            if (isBlockSelected)
                            {
                                var blockPen = new Pen(ZeroWpfTheme.PrimaryAccent, 1.5);
                                if (r == _selectedBlock.TopRow)
                                    dc.DrawLine(blockPen, new Point(unpinnedX, currentY), new Point(unpinnedX + colW, currentY));
                                if (r == _selectedBlock.BottomRow)
                                    dc.DrawLine(blockPen, new Point(unpinnedX, currentY + _rowHeight), new Point(unpinnedX + colW, currentY + _rowHeight));
                                if (c == _selectedBlock.LeftColumn)
                                    dc.DrawLine(blockPen, new Point(unpinnedX, currentY), new Point(unpinnedX, currentY + _rowHeight));
                                if (c == _selectedBlock.RightColumn)
                                    dc.DrawLine(blockPen, new Point(unpinnedX + colW, currentY), new Point(unpinnedX + colW, currentY + _rowHeight));
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

                        bool isBlockSelected = (_selectionMode == ZeroGridSelectionMode.Block && !_selectedBlock.IsEmpty && _selectedBlock.Contains(r, c));
                        if (isBlockSelected && !isSelected)
                        {
                            dc.DrawRectangle(ZeroWpfTheme.SelectionBackground, null, new Rect(pinnedX, currentY, colW, _rowHeight));
                        }

                        bool isMerged = false;
                        if (_columns[c].AllowCellMerge && r > startRow)
                        {
                            int prevModel = GetModelRowIndex(r - 1);
                            if (prevModel >= 0)
                            {
                                CellValueBuffer prevBuf = new CellValueBuffer();
                                _dataSource.GetCellValue(prevModel, c, ref prevBuf);
                                if (prevBuf.Text.SequenceEqual(cellBuffer.Text))
                                {
                                    isMerged = true;
                                }
                            }
                        }

                        if (cellBuffer.HasCustomBackground && !isSelected && !isBlockSelected)
                        {
                            byte a = (byte)((cellBuffer.BackColor >> 24) & 0xFF);
                            byte b = (byte)((cellBuffer.BackColor >> 16) & 0xFF);
                            byte g = (byte)((cellBuffer.BackColor >> 8) & 0xFF);
                            byte rCol = (byte)(cellBuffer.BackColor & 0xFF);
                            if (a == 0) a = 255;
                            var customBrush = new SolidColorBrush(Color.FromArgb(a, rCol, g, b));
                            dc.DrawRectangle(customBrush, null, new Rect(pinnedX, currentY, colW, _rowHeight));
                        }

                        if (_columns[c].ColumnType == GridColumnType.Boolean)
                        {
                            bool isChecked = IsTruthy(cellBuffer.Text);
                            DrawVectorCheckBox(dc, pinnedX, currentY, colW, _rowHeight, isChecked, isSelected);
                        }
                        else if (cellBuffer.DataBarPercent >= 0.0f)
                        {
                            DrawDataBar(dc, pinnedX, currentY, colW, _rowHeight, cellBuffer.DataBarPercent);
                            var textSpan = cellBuffer.Text;
                            if (!textSpan.IsEmpty)
                            {
                                string text = cellBuffer.Text.ToString();
                                Brush textBrush = (isSelected || isBlockSelected) ? ZeroWpfTheme.SelectionForeground : ZeroWpfTheme.TextPrimary;
                                Typeface tf = (isSelected || isBlockSelected) ? ZeroWpfTheme.BoldTypeface : ZeroWpfTheme.RegularTypeface;
                                var ft = CreateFormattedText(text, tf, 12.0, textBrush, dpi);
                                double textX = pinnedX + 8;
                                if (cellBuffer.Alignment == CellAlignment.Right) textX = pinnedX + colW - ft.Width - 8;
                                else if (cellBuffer.Alignment == CellAlignment.Center) textX = pinnedX + (colW - ft.Width) / 2.0;
                                double textY = currentY + (_rowHeight - ft.Height) / 2.0;
                                dc.DrawText(ft, new Point(textX, textY));
                            }
                        }
                        else if (!cellBuffer.SparklineValues.IsEmpty || _columns[c].Sparkline != SparklineType.None)
                        {
                            SparklineType sType = _columns[c].Sparkline != SparklineType.None ? _columns[c].Sparkline : SparklineType.Line;
                            DrawSparkline(dc, pinnedX, currentY, colW, _rowHeight, cellBuffer.SparklineValues, sType);
                        }
                        else if (!isMerged)
                        {
                            var textSpan = cellBuffer.Text;
                            if (!textSpan.IsEmpty)
                            {
                                string text = cellBuffer.Text.ToString();
                                Brush textBrush = (isSelected || isBlockSelected) ? ZeroWpfTheme.SelectionForeground : ZeroWpfTheme.TextPrimary;
                                Typeface tf = (isSelected || isBlockSelected) ? ZeroWpfTheme.BoldTypeface : ZeroWpfTheme.RegularTypeface;

                                if (!isSelected && !isBlockSelected && cellBuffer.TextColor != 0)
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
                        }

                        if (isBlockSelected)
                        {
                            var blockPen = new Pen(ZeroWpfTheme.PrimaryAccent, 1.5);
                            if (r == _selectedBlock.TopRow)
                                dc.DrawLine(blockPen, new Point(pinnedX, currentY), new Point(pinnedX + colW, currentY));
                            if (r == _selectedBlock.BottomRow)
                                dc.DrawLine(blockPen, new Point(pinnedX, currentY + _rowHeight), new Point(pinnedX + colW, currentY + _rowHeight));
                            if (c == _selectedBlock.LeftColumn)
                                dc.DrawLine(blockPen, new Point(pinnedX, currentY), new Point(pinnedX, currentY + _rowHeight));
                            if (c == _selectedBlock.RightColumn)
                                dc.DrawLine(blockPen, new Point(pinnedX + colW, currentY), new Point(pinnedX + colW, currentY + _rowHeight));
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
                    dc.DrawLine(new Pen(ZeroWpfTheme.PrimaryAccent, 2.0), new Point(pinnedW - 1, totalTopOffset), new Point(pinnedW - 1, height - footerH));
                }
            }
            else
            {
                // Empty state
                var emptyTitle = CreateFormattedText("No records to display", ZeroWpfTheme.BoldTypeface, 14.0, ZeroWpfTheme.TextSecondary, dpi);
                var emptySub = CreateFormattedText("Try adjusting your filters or loading sample data", ZeroWpfTheme.RegularTypeface, 12.0, ZeroWpfTheme.TextMuted, dpi);

                double midY = (totalTopOffset + height - footerH) / 2.0 - 20;
                dc.DrawText(emptyTitle, new Point((width - emptyTitle.Width) / 2.0, midY));
                dc.DrawText(emptySub, new Point((width - emptySub.Width) / 2.0, midY + 24));
            }

            // 3. Render Header Row (Always pinned on top)
            if (ShowGroupPanel && groupPanelH > 0)
            {
                dc.DrawRectangle(ZeroWpfTheme.BgInput, null, new Rect(0, 0, width, groupPanelH));
                dc.DrawLine(ZeroWpfTheme.BorderPen, new Point(0, groupPanelH - 0.5), new Point(width, groupPanelH - 0.5));

                _groupChipBounds.Clear();

                if (_groupColumnIndices.Length == 0)
                {
                    var phText = CreateFormattedText("Drag a column header here to group by that column", ZeroWpfTheme.RegularTypeface, 11.5, ZeroWpfTheme.TextMuted, dpi);
                    dc.DrawText(phText, new Point(14, (groupPanelH - phText.Height) / 2.0));
                }
                else
                {
                    double chipX = 12;
                    for (int i = 0; i < _groupColumnIndices.Length; i++)
                    {
                        int colIdx = _groupColumnIndices[i];
                        string colName = (colIdx >= 0 && colIdx < _columns.Count) ? _columns[colIdx].HeaderText : $"Col {colIdx}";
                        var cft = CreateFormattedText(colName, ZeroWpfTheme.BoldTypeface, 11.0, ZeroWpfTheme.TextPrimary, dpi);
                        double chipW = cft.Width + 32;
                        double chipY = 4;
                        double chipH = groupPanelH - 8;
                        Rect chipRect = new Rect(chipX, chipY, chipW, chipH);
                        Rect closeRect = new Rect(chipX + chipW - 18, chipY, 16, chipH);

                        dc.DrawRoundedRectangle(ZeroWpfTheme.BgCard, new Pen(ZeroWpfTheme.PrimaryAccent, 1.2), chipRect, 4, 4);
                        dc.DrawText(cft, new Point(chipX + 8, chipY + (chipH - cft.Height) / 2.0));

                        var xft = CreateFormattedText("✕", ZeroWpfTheme.BoldTypeface, 10.5, ZeroWpfTheme.TextSecondary, dpi);
                        dc.DrawText(xft, new Point(closeRect.X + (closeRect.Width - xft.Width) / 2.0, chipY + (chipH - xft.Height) / 2.0));

                        _groupChipBounds.Add(new GroupChipInfo { ColumnIndex = colIdx, ChipRect = chipRect, CloseRect = closeRect });
                        chipX += chipW + 8;
                    }
                }
            }

            dc.DrawRectangle(ZeroWpfTheme.BgCard, null, new Rect(0, groupPanelH, width, effHeaderH));
            dc.DrawLine(ZeroWpfTheme.BorderPen, new Point(0, totalTopOffset - 0.5), new Point(width, totalTopOffset - 0.5));

            if (_bands.Count > 0)
            {
                var bandEntries = GridBand.ComputeLayout(_bands, (int)(pinnedW - _scrollX), groupPanelH, _headerHeight, bandDepth);
                for (int b = 0; b < bandEntries.Count; b++)
                {
                    var entry = bandEntries[b];
                    if (entry.X + entry.Width > 0 && entry.X < width)
                    {
                        Rect bRect = new Rect(entry.X, entry.Y, entry.Width, entry.Height);
                        dc.DrawRectangle(ZeroWpfTheme.BgInput, null, bRect);
                        var bft = CreateFormattedText(entry.Band.Title, ZeroWpfTheme.BoldTypeface, 12.0, ZeroWpfTheme.TextPrimary, dpi);
                        dc.DrawText(bft, new Point(entry.X + (entry.Width - bft.Width) / 2.0, entry.Y + (entry.Height - bft.Height) / 2.0));
                        dc.DrawLine(ZeroWpfTheme.GridLinePen, new Point(entry.X + entry.Width - 0.5, entry.Y), new Point(entry.X + entry.Width - 0.5, entry.Y + entry.Height));
                        dc.DrawLine(ZeroWpfTheme.GridLinePen, new Point(entry.X, entry.Y + entry.Height - 0.5), new Point(entry.X + entry.Width, entry.Y + entry.Height - 0.5));
                    }
                }
            }

            _columnFilterButtonBounds.Clear();

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
                    if (_columns[c].Alignment == CellAlignment.Right) hTextX = unpinnedHdrX + colW - hft.Width - 24;
                    else if (_columns[c].Alignment == CellAlignment.Center) hTextX = unpinnedHdrX + (colW - hft.Width) / 2.0;

                    double hTextY = groupPanelH + bandH + (_headerHeight - hft.Height) / 2.0;
                    dc.DrawText(hft, new Point(hTextX, hTextY));

                    if (_columns[c].AllowFiltering)
                    {
                        Rect filterBtnRect = new Rect(unpinnedHdrX + colW - 20, groupPanelH + bandH + (_headerHeight - 14) / 2.0, 16, 14);
                        _columnFilterButtonBounds[c] = filterBtnRect;

                        bool isFiltered = IsColumnFiltered(c);
                        Brush fBrush = isFiltered ? ZeroWpfTheme.PrimaryAccent : ZeroWpfTheme.TextMuted;
                        var ftFilter = CreateFormattedText("▼", ZeroWpfTheme.BoldTypeface, 8.5, fBrush, dpi);
                        dc.DrawText(ftFilter, new Point(filterBtnRect.X + (filterBtnRect.Width - ftFilter.Width) / 2.0, filterBtnRect.Y + (filterBtnRect.Height - ftFilter.Height) / 2.0));
                    }

                    dc.DrawLine(ZeroWpfTheme.GridLinePen, new Point(unpinnedHdrX + colW - 0.5, groupPanelH + bandH + 4), new Point(unpinnedHdrX + colW - 0.5, groupPanelH + bandH + _headerHeight - 4));
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
                if (_columns[c].Alignment == CellAlignment.Right) hTextX = pinnedHdrX + colW - hft.Width - 24;
                else if (_columns[c].Alignment == CellAlignment.Center) hTextX = pinnedHdrX + (colW - hft.Width) / 2.0;

                double hTextY = groupPanelH + bandH + (_headerHeight - hft.Height) / 2.0;
                dc.DrawText(hft, new Point(hTextX, hTextY));

                if (_columns[c].AllowFiltering)
                {
                    Rect filterBtnRect = new Rect(pinnedHdrX + colW - 20, groupPanelH + bandH + (_headerHeight - 14) / 2.0, 16, 14);
                    _columnFilterButtonBounds[c] = filterBtnRect;

                    bool isFiltered = IsColumnFiltered(c);
                    Brush fBrush = isFiltered ? ZeroWpfTheme.PrimaryAccent : ZeroWpfTheme.TextMuted;
                    var ftFilter = CreateFormattedText("▼", ZeroWpfTheme.BoldTypeface, 8.5, fBrush, dpi);
                    dc.DrawText(ftFilter, new Point(filterBtnRect.X + (filterBtnRect.Width - ftFilter.Width) / 2.0, filterBtnRect.Y + (filterBtnRect.Height - ftFilter.Height) / 2.0));
                }

                dc.DrawLine(ZeroWpfTheme.GridLinePen, new Point(pinnedHdrX + colW - 0.5, groupPanelH + bandH + 4), new Point(pinnedHdrX + colW - 0.5, groupPanelH + bandH + _headerHeight - 4));

                pinnedHdrX += colW;
            }

            if (pinnedW > 0)
            {
                dc.DrawLine(new Pen(ZeroWpfTheme.PrimaryAccent, 2.0), new Point(pinnedW - 1, groupPanelH), new Point(pinnedW - 1, totalTopOffset));
            }

            if (_isDraggingHeader && _draggedHeaderCol >= 0 && _draggedHeaderCol < _columns.Count)
            {
                string dragText = _columns[_draggedHeaderCol].HeaderText;
                var dft = CreateFormattedText(dragText, ZeroWpfTheme.BoldTypeface, 11.5, Brushes.White, dpi);
                double badgeW = dft.Width + 24;
                double badgeH = 26;
                Rect badgeRect = new Rect(_currentMousePos.X - badgeW / 2.0, _currentMousePos.Y - badgeH / 2.0, badgeW, badgeH);
                dc.DrawRoundedRectangle(ZeroWpfTheme.PrimaryAccent, new Pen(Brushes.White, 1.0), badgeRect, 4, 4);
                dc.DrawText(dft, new Point(badgeRect.X + 12, badgeRect.Y + (badgeH - dft.Height) / 2.0));
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
            int totalRows = VisualRowCount;
            int totalH = totalRows * _rowHeight;
            int topOffset = TotalTopOffset;
            double clientH = Math.Max(0, height - topOffset - footerH);

            if (totalH <= clientH || clientH <= 0) return;

            double trackX = width - ScrollBarThickness;
            double trackY = topOffset;
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
            _currentMousePos = pt;
            int footerH = ShowFooter ? _footerHeight : 0;
            int topOffset = TotalTopOffset;
            int groupPanelH = ShowGroupPanel ? _groupPanelHeight : 0;

            if (e.LeftButton == MouseButtonState.Pressed && _draggedHeaderCol >= 0 && !_isDraggingHeader && !_isResizingColumn)
            {
                if (Math.Abs(pt.X - _headerDragStart.X) > 6 || Math.Abs(pt.Y - _headerDragStart.Y) > 6)
                {
                    _isDraggingHeader = true;
                }
            }

            if (_isDraggingHeader)
            {
                InvalidateVisual();
                return;
            }

            if (_isDraggingVThumb)
            {
                double clientH = Math.Max(0, ActualHeight - topOffset - footerH);
                int totalRows = VisualRowCount;
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

            if (_isSelectingBlock && _selectionMode == ZeroGridSelectionMode.Block)
            {
                int clickedCol = HitTestColumn(pt.X);
                int vRow = (int)((pt.Y - topOffset + _scrollY) / _rowHeight);
                vRow = Math.Max(0, Math.Min(VisualRowCount - 1, vRow));
                clickedCol = Math.Max(0, Math.Min(_columns.Count - 1, clickedCol));
                if (vRow != _selectedBlock.EndRow || clickedCol != _selectedBlock.EndColumn)
                {
                    _selectedBlock = new CellRange(_selectedBlock.StartRow, _selectedBlock.StartColumn, vRow, clickedCol);
                    InvalidateVisual();
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
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
            _isVThumbHovered = (pt.X >= ActualWidth - ScrollBarThickness && pt.Y >= topOffset && pt.Y < ActualHeight - footerH);
            if (prevVThumbHover != _isVThumbHovered) InvalidateVisual();

            // Check header column resize handle
            if (pt.Y >= groupPanelH && pt.Y <= topOffset)
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
            int visualRow = (int)((pt.Y - topOffset + _scrollY) / _rowHeight);
            if (visualRow >= 0 && visualRow < VisualRowCount && pt.Y < ActualHeight - footerH)
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
            int topOffset = TotalTopOffset;
            int groupPanelH = ShowGroupPanel ? _groupPanelHeight : 0;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // 1. Group Panel Chip close button click
                if (ShowGroupPanel && pt.Y < groupPanelH)
                {
                    for (int i = 0; i < _groupChipBounds.Count; i++)
                    {
                        var chip = _groupChipBounds[i];
                        if (chip.CloseRect.Contains(pt))
                        {
                            var remaining = new List<int>(_groupColumnIndices);
                            remaining.Remove(chip.ColumnIndex);
                            if (remaining.Count > 0) GroupBy(remaining.ToArray());
                            else ClearGrouping();
                            return;
                        }
                    }
                    return;
                }

                // 2. Filter button click on column headers
                foreach (var kvp in _columnFilterButtonBounds)
                {
                    if (kvp.Value.Contains(pt))
                    {
                        ShowColumnFilterPopup(kvp.Key);
                        return;
                    }
                }

                // 3. Double click cell editing
                if (e.ClickCount == 2 && pt.Y > topOffset && pt.Y < ActualHeight - footerH)
                {
                    int vRow = (int)((pt.Y - topOffset + _scrollY) / _rowHeight);
                    int col = HitTestColumn(pt.X);
                    if (vRow >= 0 && vRow < VisualRowCount && col >= 0)
                    {
                        if (_groupedMap.HasGrouping && vRow < _groupedMap.ActiveCount && _groupedMap[vRow].IsGroup)
                        {
                            _groupedMap.ToggleGroup(vRow);
                            InvalidateVisual();
                            return;
                        }
                        StartEdit(vRow, col);
                        return;
                    }
                }

                // 4. Check ScrollBar thumb click
                if (pt.X >= ActualWidth - ScrollBarThickness && pt.Y >= topOffset && pt.Y < ActualHeight - footerH)
                {
                    _isDraggingVThumb = true;
                    _dragThumbStartY = pt.Y;
                    _dragScrollStartY = _scrollY;
                    CaptureMouse();
                    return;
                }

                // 5. Check Column Header click or resize / drag
                if (pt.Y >= groupPanelH && pt.Y <= topOffset)
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

                    int col = HitTestColumn(pt.X);
                    if (col >= 0)
                    {
                        _draggedHeaderCol = col;
                        _headerDragStart = pt;
                        CaptureMouse();
                    }
                    return;
                }

                // 6. Row click selection or Master-Detail toggle
                int visualRow = (int)((pt.Y - topOffset + _scrollY) / _rowHeight);
                int clickedCol = HitTestColumn(pt.X);
                if (visualRow >= 0 && visualRow < VisualRowCount && pt.Y < ActualHeight - footerH)
                {
                    if (_groupedMap.HasGrouping && visualRow < _groupedMap.ActiveCount && _groupedMap[visualRow].IsGroup)
                    {
                        _groupedMap.ToggleGroup(visualRow);
                        InvalidateVisual();
                        return;
                    }

                    int modelRow = GetModelRowIndex(visualRow);
                    if (_allowMasterDetail && pt.X < 24 && modelRow >= 0)
                    {
                        ToggleMasterRow(modelRow);
                        return;
                    }

                    if (clickedCol >= 0 && clickedCol < _columns.Count && _columns[clickedCol].ColumnType == GridColumnType.Boolean)
                    {
                        ToggleBooleanCell(visualRow, clickedCol);
                        _selectedVisualRow = visualRow;
                        _selectedVisualRows.Clear();
                        _selectedVisualRows.Add(visualRow);
                        InvalidateVisual();
                        SelectionChanged?.Invoke(this, EventArgs.Empty);
                        return;
                    }

                    if (_isEditing)
                    {
                        if (visualRow != _editingVisualRow || clickedCol != _editingColIndex)
                        {
                            CommitEdit();
                        }
                    }

                    if (_selectionMode == ZeroGridSelectionMode.Block)
                    {
                        _selectedBlock = new CellRange(visualRow, clickedCol, visualRow, clickedCol);
                        _isSelectingBlock = true;
                        CaptureMouse();
                        InvalidateVisual();
                        SelectionChanged?.Invoke(this, EventArgs.Empty);
                        return;
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
                            int start = (_selectedVisualRow >= 0) ? _selectedVisualRow : visualRow;
                            int min = Math.Min(start, visualRow);
                            int max = Math.Max(start, visualRow);
                            _selectedVisualRows.Clear();
                            for (int r = min; r <= max; r++) _selectedVisualRows.Add(r);
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
            if (_isDraggingHeader)
            {
                _isDraggingHeader = false;
                ReleaseMouseCapture();
                Point pt = e.GetPosition(this);
                int groupPanelH = ShowGroupPanel ? _groupPanelHeight : 0;
                if (ShowGroupPanel && pt.Y < groupPanelH && _draggedHeaderCol >= 0)
                {
                    if (Array.IndexOf(_groupColumnIndices, _draggedHeaderCol) < 0)
                    {
                        int[] newCols = new int[_groupColumnIndices.Length + 1];
                        Array.Copy(_groupColumnIndices, newCols, _groupColumnIndices.Length);
                        newCols[_groupColumnIndices.Length] = _draggedHeaderCol;
                        GroupBy(newCols);
                    }
                }
                _draggedHeaderCol = -1;
                InvalidateVisual();
                return;
            }
            if (_draggedHeaderCol >= 0)
            {
                int col = _draggedHeaderCol;
                _draggedHeaderCol = -1;
                ReleaseMouseCapture();
                ColumnHeaderClicked?.Invoke(this, col);
            }
            if (_isDraggingVThumb)
            {
                _isDraggingVThumb = false;
                ReleaseMouseCapture();
                InvalidateVisual();
            }
            if (_isSelectingBlock)
            {
                _isSelectingBlock = false;
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
                if (_selectedVisualRow >= 0 && _selectedVisualRow < VisualRowCount)
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
            else if (e.Key == Key.Down && _selectedVisualRow < VisualRowCount - 1)
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
            if (visualRowIndex < 0 || visualRowIndex >= VisualRowCount) return;
            int rowTop = visualRowIndex * _rowHeight;
            int rowBottom = rowTop + _rowHeight;
            int footerH = ShowFooter ? _footerHeight : 0;
            int viewH = (int)Math.Max(0, ActualHeight - TotalTopOffset - footerH);

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

            if (_selectionMode == ZeroGridSelectionMode.Block && !_selectedBlock.IsEmpty)
            {
                int topR = _selectedBlock.TopRow;
                int botR = _selectedBlock.BottomRow;
                int leftC = _selectedBlock.LeftColumn;
                int rightC = _selectedBlock.RightColumn;

                CellValueBuffer blockBuf = new CellValueBuffer();
                var blockSb = new System.Text.StringBuilder();

                bool first = true;
                for (int c = leftC; c <= rightC && c < _columns.Count; c++)
                {
                    if (!_columns[c].IsVisible) continue;
                    if (!first) blockSb.Append('\t');
                    blockSb.Append(_columns[c].HeaderText);
                    first = false;
                }
                blockSb.AppendLine();

                for (int r = topR; r <= botR && r < VisualRowCount; r++)
                {
                    int modelRow = GetModelRowIndex(r);
                    if (modelRow < 0) continue;

                    first = true;
                    for (int c = leftC; c <= rightC && c < _columns.Count; c++)
                    {
                        if (!_columns[c].IsVisible) continue;
                        if (!first) blockSb.Append('\t');
                        blockBuf.Reset();
                        _dataSource.GetCellValue(modelRow, c, ref blockBuf);
                        blockSb.Append(blockBuf.Text.ToString());
                        first = false;
                    }
                    blockSb.AppendLine();
                }

                try { Clipboard.SetText(blockSb.ToString()); } catch { }
                return;
            }

            var rowsToCopy = new List<int>();
            if (_selectedVisualRows.Count > 0)
            {
                rowsToCopy.AddRange(_selectedVisualRows);
                rowsToCopy.Sort();
            }
            else if (_selectedVisualRow >= 0 && _selectedVisualRow < VisualRowCount)
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
                if (vRow < 0 || vRow >= VisualRowCount) continue;
                int modelRow = GetModelRowIndex(vRow);
                if (modelRow < 0) continue;

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

            double cellY = TotalTopOffset + (visualRow * _rowHeight) - _scrollY;
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
            if (_dataSource == null || visualRow < 0 || visualRow >= VisualRowCount ||
                colIndex < 0 || colIndex >= _columns.Count) return;

            if (_groupedMap.HasGrouping && visualRow < _groupedMap.ActiveCount && _groupedMap[visualRow].IsGroup) return;

            var col = _columns[colIndex];
            if (col.ReadOnly || !col.IsVisible) return;

            int modelRow = GetModelRowIndex(visualRow);
            if (modelRow < 0) return;

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

            FrameworkElement editor;
            if (col.ColumnType == GridColumnType.Masked || !string.IsNullOrEmpty(col.Mask))
            {
                _maskedEditor.Mask = col.Mask ?? "";
                _maskedEditor.Text = val;
                editor = _maskedEditor;
            }
            else if (col.ColumnType == GridColumnType.Numeric)
            {
                if (decimal.TryParse(val.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var numVal))
                {
                    _numericEditor.Value = numVal;
                }
                else
                {
                    _numericEditor.Value = 0;
                }
                editor = _numericEditor;
            }
            else if (col.ColumnType == GridColumnType.DateTime)
            {
                if (DateTime.TryParse(val, out var dtVal))
                {
                    _dateEditor.SelectedDate = dtVal;
                }
                else
                {
                    _dateEditor.SelectedDate = DateTime.Today;
                }
                editor = _dateEditor;
            }
            else
            {
                _inPlaceEditor.Text = val;
                editor = _inPlaceEditor;
            }

            _activeEditor = editor;
            UpdateEditorTheme();
            editor.ToolTip = null;
            if (editor is Control ctrl) ctrl.BorderBrush = ZeroWpfTheme.PrimaryAccent;
            editor.Visibility = Visibility.Visible;
            editor.Arrange(rect);
            editor.Focus();
            if (editor is TextBox tb) tb.SelectAll();

            CellBeginEdit?.Invoke(this, EventArgs.Empty);
        }

        public void CommitEdit()
        {
            if (!_isEditing || _dataSource == null || _activeEditor == null) return;

            int visualRow = _editingVisualRow;
            int colIndex = _editingColIndex;
            if (visualRow < 0 || visualRow >= VisualRowCount || colIndex < 0 || colIndex >= _columns.Count)
            {
                CancelEdit();
                return;
            }

            int modelRow = GetModelRowIndex(visualRow);
            if (modelRow < 0)
            {
                CancelEdit();
                return;
            }

            var col = _columns[colIndex];
            string newText = string.Empty;
            if (_activeEditor is ZeroMaskedTextBox mtb)
            {
                newText = mtb.Text;
            }
            else if (_activeEditor is TextBox tb)
            {
                newText = tb.Text;
            }
            else if (_activeEditor is ZeroNumericBox nb)
            {
                newText = nb.Value.ToString(CultureInfo.InvariantCulture);
            }
            else if (_activeEditor is ZeroDatePicker dp)
            {
                newText = dp.SelectedDate.ToString(dp.DateFormat);
            }

            if (col.CustomValidator != null)
            {
                var (isValid, errMsg) = col.CustomValidator(newText);
                if (!isValid)
                {
                    if (_activeEditor is Control c)
                    {
                        c.ToolTip = errMsg ?? "Validation failed";
                        c.BorderBrush = ZeroWpfTheme.DangerAccent;
                    }
                    _activeEditor.Focus();
                    return;
                }
            }

            _activeEditor.Visibility = Visibility.Collapsed;
            _activeEditor = null;
            _isEditing = false;
            _editingVisualRow = -1;
            _editingColIndex = -1;

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

            CellEndEdit?.Invoke(this, EventArgs.Empty);
        }

        public void CancelEdit()
        {
            if (!_isEditing) return;

            if (_activeEditor != null)
            {
                _activeEditor.Visibility = Visibility.Collapsed;
                _activeEditor = null;
            }
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
                if (_selectedVisualRow < VisualRowCount - 1)
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

    /// <summary>
    /// Backward-compatibility alias for <see cref="GridControl"/>.
    /// </summary>
    public class ZeroGridControl : GridControl
    {
    }
}
