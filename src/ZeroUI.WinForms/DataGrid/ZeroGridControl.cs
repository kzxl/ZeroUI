using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

using ZeroUI.Core.Common;
using ZeroUI.Core.Data;
using ZeroUI.Core.Input;
using ZeroUI.Core.Layout;
using ZeroUI.Core.Virtualization;
using ZeroUI.WinForms.Native;
using ZeroUI.WinForms.Rendering;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.DataGrid
{
    [ToolboxItem(true)]
    [Category("ZeroUI - DataGrid")]
    [DefaultProperty("DataSource")]
    [Description("High-performance virtual DataGrid with direct Win32 DIBSection rendering")]
    public class ZeroGridControl : Control
    {
        private readonly List<ZeroColumn> _columns = new List<ZeroColumn>();

        private readonly RowIndexMap _rowIndexMap = new RowIndexMap(10000);
        private readonly MemoryDIBSection _dibSection = new MemoryDIBSection();

        private IntPtr _hFont = IntPtr.Zero;
        private IntPtr _hHeaderFont = IntPtr.Zero;
        private Font? _cachedFont;

        private IZeroVirtualSource? _dataSource;
        private int _headerHeight = 28;
        private int _rowHeight = 26;
        private int _scrollX = 0;
        private int _scrollY = 0;

        // Selection & Interaction
        private int _selectedVisualRow = -1;
        private readonly HashSet<int> _selectedVisualRows = new HashSet<int>();
        private ZeroGridSelectionMode _selectionMode = ZeroGridSelectionMode.SingleRow;
        private bool _isResizingColumn = false;
        private int _resizingColIndex = -1;
        private int _resizeStartX = 0;
        private int _resizeStartWidth = 0;

        // In-Place Floating Editor
        private readonly TextBox _inPlaceEditor;
        private bool _isEditing = false;
        private int _editingVisualRow = -1;
        private int _editingColIndex = -1;

        // Summary Footer
        private bool _showFooter = false;
        private int _footerHeight = 28;

        // Asynchronous Sorting
        private bool _isSorting = false;
        private int _sortingColumnIndex = -1;
        private System.Threading.CancellationTokenSource? _sortCts;

        [Browsable(false)]
        public bool IsSorting => _isSorting;

        public event EventHandler? SortingStarted;
        public event EventHandler<TimeSpan>? SortingCompleted;
        public event EventHandler<CellValueChangedEventArgs>? CellValueChanged;
        public event EventHandler? CellBeginEdit;
        public event EventHandler? CellEndEdit;
        public event EventHandler? SelectionChanged;

        // Color Palettes (Win32 0x00BBGGRR format)
        private uint _headerBgColor = 0x00F0F0F0;
        private uint _headerTextColor = 0x00202020;
        private uint _rowBgColor = 0x00FFFFFF;
        private uint _altRowBgColor = 0x00FAFAFA;
        private uint _selectedBgColor = 0x00E0D0B0;
        private uint _gridLineColor = 0x00E5E5E5;
        private uint _cellTextColor = 0x00101010;
        private uint _footerBgColor = 0x00F4F4F5;
        private uint _pinnedBorderColor = 0x006366F1; // Indigo accent border

        public ZeroGridControl()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.Opaque |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable, true);

            DoubleBuffered = false; // We use our own zero-copy DIB Section
            TabStop = true;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // In-place floating editor setup
            _inPlaceEditor = new TextBox
            {
                Visible = false,
                BorderStyle = BorderStyle.None,
                Font = Font
            };
            _inPlaceEditor.KeyDown += InPlaceEditor_KeyDown;
            _inPlaceEditor.LostFocus += (s, e) => CommitEdit();
            Controls.Add(_inPlaceEditor);

            ZeroTheme.ThemeChanged += OnThemeChanged;
            UpdateTheme();
        }

        private static uint ToBgr(Color c) => (uint)(c.R | (c.G << 8) | (c.B << 16));

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            UpdateTheme();
        }

        public void UpdateTheme()
        {
            var p = ZeroTheme.Colors;
            _headerBgColor = ToBgr(p.HeaderBackground);
            _headerTextColor = ToBgr(p.TextPrimary);
            _rowBgColor = ToBgr(p.Surface);
            _altRowBgColor = ToBgr(ZeroTheme.IsDark
                ? Color.FromArgb(Math.Min(255, p.Surface.R + 6), Math.Min(255, p.Surface.G + 6), Math.Min(255, p.Surface.B + 10))
                : Color.FromArgb(249, 250, 251));
            _selectedBgColor = ToBgr(ZeroTheme.IsDark
                ? Color.FromArgb(45, 55, 90)
                : Color.FromArgb(224, 208, 176));
            _gridLineColor = ToBgr(p.Border);
            _cellTextColor = ToBgr(p.TextPrimary);
            _footerBgColor = ToBgr(p.HeaderBackground);
            _pinnedBorderColor = ToBgr(p.Primary);

            _inPlaceEditor.BackColor = p.Surface;
            _inPlaceEditor.ForeColor = p.TextPrimary;

            Invalidate();
        }

        private void EnsureFonts()
        {
            if (_cachedFont != Font || _hFont == IntPtr.Zero)
            {
                if (_hFont != IntPtr.Zero)
                {
                    NativeMethods.DeleteObject(_hFont);
                    _hFont = IntPtr.Zero;
                }
                if (_hHeaderFont != IntPtr.Zero)
                {
                    NativeMethods.DeleteObject(_hHeaderFont);
                    _hHeaderFont = IntPtr.Zero;
                }

                _cachedFont = Font;
                _hFont = Font.ToHfont();
                using var boldFont = new Font(Font, FontStyle.Bold);
                _hHeaderFont = boldFont.ToHfont();
            }
        }


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public List<ZeroColumn> Columns => _columns;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
                    _rowIndexMap.ResetIdentity(0);
                }
                _scrollY = 0;
                _selectedVisualRow = -1;
                UpdateScrollBars();
                Invalidate();
            }
        }

        public int HeaderHeight
        {
            get => _headerHeight;
            set { _headerHeight = Math.Max(20, value); Invalidate(); }
        }

        private GridDensity _density = GridDensity.Middle;

        [Category("Appearance")]
        [DefaultValue(GridDensity.Middle)]
        public GridDensity Density
        {
            get => _density;
            set
            {
                _density = value;
                _rowHeight = (int)value;
                UpdateScrollBars();
                Invalidate();
            }
        }

        public int RowHeight
        {
            get => _rowHeight;
            set { _rowHeight = Math.Max(16, value); UpdateScrollBars(); Invalidate(); }
        }


        public int ScrollY
        {
            get => _scrollY;
            set
            {
                int footerH = ShowFooter ? _footerHeight : 0;
                int maxScroll = Math.Max(0, (_dataSource?.TotalRowCount ?? 0) * _rowHeight - (ClientSize.Height - _headerHeight - footerH));
                int clamped = Math.Max(0, Math.Min(maxScroll, value));
                if (_scrollY != clamped)
                {
                    _scrollY = clamped;
                    UpdateScrollBars();
                    if (_isEditing) UpdateInPlaceEditorBounds();
                    Invalidate();
                }
            }
        }

        public void ScrollToRow(int visualRowIndex)
        {
            if (visualRowIndex < 0) visualRowIndex = 0;
            ScrollY = visualRowIndex * _rowHeight;
        }

        public int SelectedVisualRow
        {
            get => _selectedVisualRow;
            set
            {
                if (_selectedVisualRow != value)
                {
                    _selectedVisualRow = value;
                    _selectedVisualRows.Clear();
                    if (value >= 0) _selectedVisualRows.Add(value);
                    Invalidate();
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(ZeroGridSelectionMode.SingleRow)]
        public ZeroGridSelectionMode SelectionMode
        {
            get => _selectionMode;
            set { _selectionMode = value; Invalidate(); }
        }

        [Browsable(false)]
        public IReadOnlyCollection<int> SelectedVisualRows => _selectedVisualRows;

        [Category("Appearance")]
        [DefaultValue(false)]
        public bool ShowFooter
        {
            get => _showFooter || HasAnySummaryColumns();
            set { _showFooter = value; UpdateScrollBars(); Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(28)]
        public int FooterHeight
        {
            get => _footerHeight;
            set { _footerHeight = Math.Max(20, value); UpdateScrollBars(); Invalidate(); }
        }

        [Browsable(false)]
        public bool IsEditing => _isEditing;

        [Browsable(false)]
        public int EditingVisualRow => _editingVisualRow;

        [Browsable(false)]
        public int EditingColumnIndex => _editingColIndex;

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
                _columns.AddRange(src.GenerateColumns());
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

        public int SelectedDataRowIndex
        {
            get
            {
                if (_selectedVisualRow >= 0 && _selectedVisualRow < _rowIndexMap.ActiveCount)
                {
                    return _rowIndexMap[_selectedVisualRow];
                }
                return -1;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= NativeMethods.WS_VSCROLL | NativeMethods.WS_HSCROLL;
                return cp;
            }
        }

        public void SortByColumn(int columnIndex, Comparison<int> comparison)
        {
            if (_dataSource == null || columnIndex < 0 || columnIndex >= _columns.Count) return;

            var col = _columns[columnIndex];
            col.SortOrder = col.SortOrder == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;

            // Reset other columns
            for (int i = 0; i < _columns.Count; i++)
            {
                if (i != columnIndex) _columns[i].SortOrder = SortDirection.None;
            }

            if (col.SortOrder == SortDirection.Descending)
            {
                _rowIndexMap.Sort((a, b) => comparison(b, a));
            }
            else
            {
                _rowIndexMap.Sort(comparison);
            }

            Invalidate();
        }

        public int GetTotalColumnsWidth()
        {
            int total = 0;
            for (int i = 0; i < _columns.Count; i++)
            {
                if (_columns[i].IsVisible)
                {
                    total += _columns[i].Width;
                }
            }
            return total;
        }

        public int GetPinnedColumnsWidth()
        {
            int total = 0;
            for (int i = 0; i < _columns.Count; i++)
            {
                if (_columns[i].IsVisible && _columns[i].IsPinned)
                {
                    total += _columns[i].Width;
                }
            }
            return total;
        }

        public int GetUnpinnedColumnsWidth()
        {
            int total = 0;
            for (int i = 0; i < _columns.Count; i++)
            {
                if (_columns[i].IsVisible && !_columns[i].IsPinned)
                {
                    total += _columns[i].Width;
                }
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

        private int[] GetVisibleColumnWidths()
        {
            int[] widths = new int[_columns.Count];
            for (int i = 0; i < _columns.Count; i++)
            {
                widths[i] = _columns[i].IsVisible ? _columns[i].Width : 0;
            }
            return widths;
        }

        public void UpdateScrollBars()
        {
            if (!IsHandleCreated) return;

            int footerH = ShowFooter ? _footerHeight : 0;
            int clientH = ClientSize.Height - _headerHeight - footerH;
            int clientW = ClientSize.Width;
            int totalRows = _rowIndexMap.ActiveCount;
            int totalH = totalRows * _rowHeight;
            int pinnedW = GetPinnedColumnsWidth();
            int unpinnedW = GetUnpinnedColumnsWidth();
            int scrollableW = Math.Max(0, clientW - pinnedW);

            // Vertical Scroll
            SCROLLINFO siV = new SCROLLINFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(SCROLLINFO)),
                fMask = NativeMethods.SIF_RANGE | NativeMethods.SIF_PAGE | NativeMethods.SIF_POS,
                nMin = 0,
                nMax = Math.Max(0, totalH),
                nPage = (uint)Math.Max(0, clientH),
                nPos = _scrollY
            };
            NativeMethods.SetScrollInfo(Handle, NativeMethods.SB_VERT, ref siV, true);

            // Horizontal Scroll
            SCROLLINFO siH = new SCROLLINFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(SCROLLINFO)),
                fMask = NativeMethods.SIF_RANGE | NativeMethods.SIF_PAGE | NativeMethods.SIF_POS,
                nMin = 0,
                nMax = Math.Max(0, unpinnedW),
                nPage = (uint)Math.Max(0, scrollableW),
                nPos = _scrollX
            };
            NativeMethods.SetScrollInfo(Handle, NativeMethods.SB_HORZ, ref siH, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            int width = ClientSize.Width;
            int height = ClientSize.Height;
            if (width <= 0 || height <= 0) return;

            IntPtr hdc = e.Graphics.GetHdc();
            try
            {
                EnsureFonts();
                int textHeight = Font.Height;

                _dibSection.EnsureSize(width, height, hdc);
                _dibSection.Clear(_rowBgColor);

                int totalCols = _columns.Count;
                int totalRows = _rowIndexMap.ActiveCount;
                int[] colWidths = GetVisibleColumnWidths();
                int pinnedW = GetPinnedColumnsWidth();
                int footerH = ShowFooter ? _footerHeight : 0;
                int clientDataHeight = Math.Max(0, height - _headerHeight - footerH);

                // 1. Render Cells
                if (_dataSource != null && totalRows > 0 && totalCols > 0)
                {
                    _dibSection.SelectFont(_hFont);

                    int startRow = Math.Max(0, _scrollY / _rowHeight);
                    int visibleRowCount = (clientDataHeight / _rowHeight) + 2;
                    int endRow = Math.Min(totalRows - 1, startRow + visibleRowCount);

                    CellValueBuffer cellBuffer = new CellValueBuffer();
                    int firstRowY = (startRow * _rowHeight) - _scrollY;
                    int currentY = _headerHeight + firstRowY;

                    for (int r = startRow; r <= endRow && r < totalRows; r++)
                    {
                        if (currentY >= _headerHeight + clientDataHeight) break;

                        int modelRow = _rowIndexMap[r];
                        bool isSelected = (_selectionMode == ZeroGridSelectionMode.MultiRow)
                            ? _selectedVisualRows.Contains(r)
                            : (r == _selectedVisualRow);

                        uint rowBg = isSelected ? _selectedBgColor : ((r % 2 == 1) ? _altRowBgColor : _rowBgColor);

                        // Row background
                        _dibSection.FillRectangle(0, currentY, width, _rowHeight, rowBg);

                        // (A) Draw Unpinned Cells (shifted by -_scrollX)
                        int unpinnedX = pinnedW - _scrollX;
                        for (int c = 0; c < totalCols; c++)
                        {
                            if (!_columns[c].IsVisible || _columns[c].IsPinned) continue;
                            int colW = colWidths[c];
                            if (colW <= 0) continue;

                            if (unpinnedX + colW > pinnedW && unpinnedX < width)
                            {
                                cellBuffer.Reset();
                                cellBuffer.TextColor = _cellTextColor;
                                cellBuffer.BackColor = rowBg;
                                cellBuffer.Alignment = _columns[c].Alignment;

                                _dataSource.GetCellValue(modelRow, c, ref cellBuffer);

                                RECT cellRect = new RECT(Math.Max(pinnedW, unpinnedX), currentY, Math.Min(width, unpinnedX + colW), currentY + _rowHeight);

                                if (cellBuffer.HasCustomBackground)
                                {
                                    _dibSection.FillRectangle(cellRect.Left, cellRect.Top, cellRect.Right - cellRect.Left, _rowHeight, cellBuffer.BackColor);
                                }

                                RECT textRect = new RECT(unpinnedX + 4, currentY, unpinnedX + colW - 4, currentY + _rowHeight);
                                _dibSection.DrawText(cellBuffer.Text, ref textRect, cellBuffer.TextColor, cellBuffer.Alignment, textHeight);

                                // Vertical Gridline
                                _dibSection.FillRectangle(unpinnedX + colW - 1, currentY, 1, _rowHeight, _gridLineColor);
                            }

                            unpinnedX += colW;
                        }

                        // (B) Draw Pinned Cells on top (fixed at 0..pinnedW)
                        int pinnedX = 0;
                        for (int c = 0; c < totalCols; c++)
                        {
                            if (!_columns[c].IsVisible || !_columns[c].IsPinned) continue;
                            int colW = colWidths[c];
                            if (colW <= 0) continue;

                            cellBuffer.Reset();
                            cellBuffer.TextColor = _cellTextColor;
                            cellBuffer.BackColor = rowBg;
                            cellBuffer.Alignment = _columns[c].Alignment;

                            _dataSource.GetCellValue(modelRow, c, ref cellBuffer);

                            RECT cellRect = new RECT(pinnedX, currentY, pinnedX + colW, currentY + _rowHeight);

                            if (cellBuffer.HasCustomBackground)
                            {
                                _dibSection.FillRectangle(cellRect.Left, cellRect.Top, colW, _rowHeight, cellBuffer.BackColor);
                            }

                            RECT textRect = new RECT(pinnedX + 4, currentY, pinnedX + colW - 4, currentY + _rowHeight);
                            _dibSection.DrawText(cellBuffer.Text, ref textRect, cellBuffer.TextColor, cellBuffer.Alignment, textHeight);

                            // Vertical Gridline
                            _dibSection.FillRectangle(cellRect.Right - 1, currentY, 1, _rowHeight, _gridLineColor);

                            pinnedX += colW;
                        }

                        // Horizontal Gridline
                        _dibSection.FillRectangle(0, currentY + _rowHeight - 1, width, 1, _gridLineColor);

                        currentY += _rowHeight;
                    }

                    // Pinned columns vertical accent border
                    if (pinnedW > 0)
                    {
                        _dibSection.FillRectangle(pinnedW - 2, 0, 2, height - footerH, _pinnedBorderColor);
                    }
                }
                else
                {
                    // Empty State
                    int emptyCenterY = (_headerHeight + height - footerH) / 2 - 20;

                    _dibSection.SelectFont(_hHeaderFont);
                    RECT emptyTitleRect = new RECT(20, emptyCenterY, width - 20, emptyCenterY + 24);
                    _dibSection.DrawText("No matching data found".AsSpan(), ref emptyTitleRect, _headerTextColor, CellAlignment.Center, Font.Height);

                    _dibSection.SelectFont(_hFont);
                    RECT emptySubRect = new RECT(20, emptyCenterY + 26, width - 20, emptyCenterY + 50);
                    _dibSection.DrawText("Try adjusting your search keywords or clearing active filters".AsSpan(), ref emptySubRect, ToBgr(ZeroTheme.Colors.TextSecondary), CellAlignment.Center, Font.Height);
                }

                // 2. Render Header Row (Always pinned on top)
                _dibSection.SelectFont(_hHeaderFont);
                _dibSection.FillRectangle(0, 0, width, _headerHeight, _headerBgColor);

                // (A) Draw Unpinned Headers
                int unpinnedHdrX = pinnedW - _scrollX;
                for (int c = 0; c < totalCols; c++)
                {
                    if (!_columns[c].IsVisible || _columns[c].IsPinned) continue;
                    int colW = colWidths[c];
                    if (colW <= 0) continue;

                    if (unpinnedHdrX + colW > pinnedW && unpinnedHdrX < width)
                    {
                        RECT colRect = new RECT(unpinnedHdrX, 0, unpinnedHdrX + colW, _headerHeight);
                        string text = _columns[c].HeaderText;

                        if (_isSorting && _sortingColumnIndex == c) text += " ⏳";
                        else if (_columns[c].SortOrder == SortDirection.Ascending) text += " ▲";
                        else if (_columns[c].SortOrder == SortDirection.Descending) text += " ▼";

                        _dibSection.DrawText(text.AsSpan(), ref colRect, _headerTextColor, _columns[c].Alignment, textHeight);
                        _dibSection.FillRectangle(unpinnedHdrX + colW - 1, 4, 1, _headerHeight - 8, 0x00CCCCCC);
                    }
                    unpinnedHdrX += colW;
                }

                // (B) Draw Pinned Headers
                int pinnedHdrX = 0;
                for (int c = 0; c < totalCols; c++)
                {
                    if (!_columns[c].IsVisible || !_columns[c].IsPinned) continue;
                    int colW = colWidths[c];
                    if (colW <= 0) continue;

                    RECT colRect = new RECT(pinnedHdrX, 0, pinnedHdrX + colW, _headerHeight);
                    string text = _columns[c].HeaderText;

                    if (_isSorting && _sortingColumnIndex == c) text += " ⏳";
                    else if (_columns[c].SortOrder == SortDirection.Ascending) text += " ▲";
                    else if (_columns[c].SortOrder == SortDirection.Descending) text += " ▼";

                    _dibSection.DrawText(text.AsSpan(), ref colRect, _headerTextColor, _columns[c].Alignment, textHeight);
                    _dibSection.FillRectangle(pinnedHdrX + colW - 1, 4, 1, _headerHeight - 8, 0x00CCCCCC);

                    pinnedHdrX += colW;
                }

                _dibSection.FillRectangle(0, _headerHeight - 1, width, 1, 0x00CCCCCC);
                if (pinnedW > 0)
                {
                    _dibSection.FillRectangle(pinnedW - 2, 0, 2, _headerHeight, _pinnedBorderColor);
                }

                // 3. Render Footer Summary Bar (if enabled)
                if (ShowFooter && footerH > 0)
                {
                    int footerY = height - footerH;
                    _dibSection.FillRectangle(0, footerY, width, footerH, _footerBgColor);
                    _dibSection.FillRectangle(0, footerY, width, 1, 0x00D4D4D8);
                    _dibSection.SelectFont(_hHeaderFont);

                    // (A) Unpinned Footer summaries
                    int unpinnedFootX = pinnedW - _scrollX;
                    for (int c = 0; c < totalCols; c++)
                    {
                        if (!_columns[c].IsVisible || _columns[c].IsPinned) continue;
                        int colW = colWidths[c];
                        if (colW <= 0) continue;

                        if (unpinnedFootX + colW > pinnedW && unpinnedFootX < width)
                        {
                            string summaryText = GetColumnSummaryText(c);
                            if (!string.IsNullOrEmpty(summaryText))
                            {
                                RECT fRect = new RECT(unpinnedFootX + 6, footerY, unpinnedFootX + colW - 6, footerY + footerH);
                                _dibSection.DrawText(summaryText.AsSpan(), ref fRect, 0x00333333, _columns[c].Alignment, textHeight);
                            }
                            _dibSection.FillRectangle(unpinnedFootX + colW - 1, footerY + 3, 1, footerH - 6, 0x00D4D4D8);
                        }
                        unpinnedFootX += colW;
                    }

                    // (B) Pinned Footer summaries
                    int pinnedFootX = 0;
                    for (int c = 0; c < totalCols; c++)
                    {
                        if (!_columns[c].IsVisible || !_columns[c].IsPinned) continue;
                        int colW = colWidths[c];
                        if (colW <= 0) continue;

                        string summaryText = GetColumnSummaryText(c);
                        if (!string.IsNullOrEmpty(summaryText))
                        {
                            RECT fRect = new RECT(pinnedFootX + 6, footerY, pinnedFootX + colW - 6, footerY + footerH);
                            _dibSection.DrawText(summaryText.AsSpan(), ref fRect, 0x00333333, _columns[c].Alignment, textHeight);
                        }
                        _dibSection.FillRectangle(pinnedFootX + colW - 1, footerY + 3, 1, footerH - 6, 0x00D4D4D8);
                        pinnedFootX += colW;
                    }

                    if (pinnedW > 0)
                    {
                        _dibSection.FillRectangle(pinnedW - 2, footerY, 2, footerH, _pinnedBorderColor);
                    }
                }

                // 4. BitBlt to Screen in <0.5ms
                _dibSection.BitBltTo(hdc, 0, 0, width, height);
            }
            finally
            {
                e.Graphics.ReleaseHdc(hdc);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Do nothing: zero flicker
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case NativeMethods.WM_ERASEBKGND:
                    m.Result = (IntPtr)1; // Prevent background erasing
                    return;

                case NativeMethods.WM_VSCROLL:
                    HandleVScroll(m.WParam);
                    return;

                case NativeMethods.WM_HSCROLL:
                    HandleHScroll(m.WParam);
                    return;

                case NativeMethods.WM_MOUSEWHEEL:
                    HandleMouseWheel(m.WParam);
                    return;

                case NativeMethods.WM_SIZE:
                    base.WndProc(ref m);
                    UpdateScrollBars();
                    Invalidate();
                    return;

                case NativeMethods.WM_SETCURSOR:
                    if (_isResizingColumn)
                    {
                        Cursor.Current = Cursors.VSplit;
                        m.Result = (IntPtr)1;
                        return;
                    }
                    break;
            }

            base.WndProc(ref m);
        }

        private void HandleVScroll(IntPtr wParam)
        {
            int action = unchecked((short)(long)wParam);
            int footerH = ShowFooter ? _footerHeight : 0;
            int totalH = (_dataSource?.TotalRowCount ?? 0) * _rowHeight;
            int maxScroll = Math.Max(0, totalH - (ClientSize.Height - _headerHeight - footerH));

            switch (action)
            {
                case NativeMethods.SB_LINEUP:
                    _scrollY = Math.Max(0, _scrollY - _rowHeight);
                    break;
                case NativeMethods.SB_LINEDOWN:
                    _scrollY = Math.Min(maxScroll, _scrollY + _rowHeight);
                    break;
                case NativeMethods.SB_PAGEUP:
                    _scrollY = Math.Max(0, _scrollY - (ClientSize.Height - _headerHeight - footerH));
                    break;
                case NativeMethods.SB_PAGEDOWN:
                    _scrollY = Math.Min(maxScroll, _scrollY + (ClientSize.Height - _headerHeight - footerH));
                    break;
                case NativeMethods.SB_THUMBTRACK:
                case NativeMethods.SB_THUMBPOSITION:
                    SCROLLINFO si = new SCROLLINFO
                    {
                        cbSize = (uint)Marshal.SizeOf(typeof(SCROLLINFO)),
                        fMask = NativeMethods.SIF_TRACKPOS
                    };
                    if (NativeMethods.GetScrollInfo(Handle, NativeMethods.SB_VERT, ref si))
                    {
                        _scrollY = Math.Max(0, Math.Min(maxScroll, si.nTrackPos));
                    }
                    break;
            }

            UpdateScrollBars();
            if (_isEditing) UpdateInPlaceEditorBounds();
            Invalidate();
        }

        private void HandleHScroll(IntPtr wParam)
        {
            int action = unchecked((short)(long)wParam);
            int unpinnedW = GetUnpinnedColumnsWidth();
            int pinnedW = GetPinnedColumnsWidth();
            int scrollableW = Math.Max(0, ClientSize.Width - pinnedW);
            int maxScroll = Math.Max(0, unpinnedW - scrollableW);

            switch (action)
            {
                case NativeMethods.SB_LINEUP:
                    _scrollX = Math.Max(0, _scrollX - 20);
                    break;
                case NativeMethods.SB_LINEDOWN:
                    _scrollX = Math.Min(maxScroll, _scrollX + 20);
                    break;
                case NativeMethods.SB_PAGEUP:
                    _scrollX = Math.Max(0, _scrollX - scrollableW);
                    break;
                case NativeMethods.SB_PAGEDOWN:
                    _scrollX = Math.Min(maxScroll, _scrollX + scrollableW);
                    break;
                case NativeMethods.SB_THUMBTRACK:
                case NativeMethods.SB_THUMBPOSITION:
                    SCROLLINFO si = new SCROLLINFO
                    {
                        cbSize = (uint)Marshal.SizeOf(typeof(SCROLLINFO)),
                        fMask = NativeMethods.SIF_TRACKPOS
                    };
                    if (NativeMethods.GetScrollInfo(Handle, NativeMethods.SB_HORZ, ref si))
                    {
                        _scrollX = Math.Max(0, Math.Min(maxScroll, si.nTrackPos));
                    }
                    break;
            }

            UpdateScrollBars();
            if (_isEditing) UpdateInPlaceEditorBounds();
            Invalidate();
        }

        private void HandleMouseWheel(IntPtr wParam)
        {
            int delta = unchecked((short)((long)wParam >> 16));
            int scrollDelta = (delta / 120) * (_rowHeight * 3);

            int footerH = ShowFooter ? _footerHeight : 0;
            int totalH = (_dataSource?.TotalRowCount ?? 0) * _rowHeight;
            int maxScroll = Math.Max(0, totalH - (ClientSize.Height - _headerHeight - footerH));

            _scrollY = Math.Max(0, Math.Min(maxScroll, _scrollY - scrollDelta));
            UpdateScrollBars();
            if (_isEditing) UpdateInPlaceEditorBounds();
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            int footerH = ShowFooter ? _footerHeight : 0;
            if (e.Button == MouseButtons.Left)
            {
                var hit = SpatialHitTester.HitTest(
                    e.X,
                    e.Y,
                    _headerHeight,
                    _rowHeight,
                    _scrollX,
                    _scrollY,
                    GetVisibleColumnWidths(),
                    GetColumnPinnedFlags(),
                    _columns.Count,
                    _dataSource?.TotalRowCount ?? 0,
                    footerH,
                    ClientSize.Height);

                if (hit.Region == HitRegion.ColumnResizeGrip)
                {
                    _isResizingColumn = true;
                    _resizingColIndex = hit.ResizeColumnIndex;
                    _resizeStartX = e.X;
                    _resizeStartWidth = _columns[_resizingColIndex].Width;
                    Capture = true;
                    Cursor = Cursors.VSplit;
                }
                else if (hit.Region == HitRegion.Header)
                {
                    if (_isEditing) CommitEdit();
                    OnHeaderClicked(hit.ColumnIndex);
                }
                else if (hit.Region == HitRegion.Cell)
                {
                    if (_isEditing && (_editingVisualRow != hit.RowIndex || _editingColIndex != hit.ColumnIndex))
                    {
                        CommitEdit();
                    }

                    if (_selectionMode == ZeroGridSelectionMode.MultiRow)
                    {
                        if ((ModifierKeys & Keys.Control) != 0)
                        {
                            if (_selectedVisualRows.Contains(hit.RowIndex))
                                _selectedVisualRows.Remove(hit.RowIndex);
                            else
                                _selectedVisualRows.Add(hit.RowIndex);
                            _selectedVisualRow = hit.RowIndex;
                        }
                        else if ((ModifierKeys & Keys.Shift) != 0 && _selectedVisualRow >= 0)
                        {
                            _selectedVisualRows.Clear();
                            int minR = Math.Min(_selectedVisualRow, hit.RowIndex);
                            int maxR = Math.Max(_selectedVisualRow, hit.RowIndex);
                            for (int r = minR; r <= maxR; r++) _selectedVisualRows.Add(r);
                        }
                        else
                        {
                            _selectedVisualRows.Clear();
                            _selectedVisualRows.Add(hit.RowIndex);
                            _selectedVisualRow = hit.RowIndex;
                        }
                    }
                    else
                    {
                        _selectedVisualRows.Clear();
                        _selectedVisualRows.Add(hit.RowIndex);
                        SelectedVisualRow = hit.RowIndex;
                    }
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                var hit = SpatialHitTester.HitTest(
                    e.X,
                    e.Y,
                    _headerHeight,
                    _rowHeight,
                    _scrollX,
                    _scrollY,
                    GetVisibleColumnWidths(),
                    GetColumnPinnedFlags(),
                    _columns.Count,
                    _dataSource?.TotalRowCount ?? 0,
                    footerH,
                    ClientSize.Height);

                if (hit.Region == HitRegion.Header)
                {
                    ShowHeaderContextMenu(hit.ColumnIndex, e.Location);
                }
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Button == MouseButtons.Left)
            {
                int footerH = ShowFooter ? _footerHeight : 0;
                var hit = SpatialHitTester.HitTest(
                    e.X,
                    e.Y,
                    _headerHeight,
                    _rowHeight,
                    _scrollX,
                    _scrollY,
                    GetVisibleColumnWidths(),
                    GetColumnPinnedFlags(),
                    _columns.Count,
                    _dataSource?.TotalRowCount ?? 0,
                    footerH,
                    ClientSize.Height);

                if (hit.Region == HitRegion.Cell)
                {
                    StartEdit(hit.RowIndex, hit.ColumnIndex);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isResizingColumn && _resizingColIndex >= 0 && _resizingColIndex < _columns.Count)
            {
                int delta = e.X - _resizeStartX;
                int newWidth = Math.Max(_columns[_resizingColIndex].MinWidth, _resizeStartWidth + delta);
                _columns[_resizingColIndex].Width = newWidth;
                UpdateScrollBars();
                if (_isEditing) UpdateInPlaceEditorBounds();
                Invalidate();
                return;
            }

            int footerH = ShowFooter ? _footerHeight : 0;
            var hit = SpatialHitTester.HitTest(
                e.X,
                e.Y,
                _headerHeight,
                _rowHeight,
                _scrollX,
                _scrollY,
                GetVisibleColumnWidths(),
                GetColumnPinnedFlags(),
                _columns.Count,
                _dataSource?.TotalRowCount ?? 0,
                footerH,
                ClientSize.Height);

            if (hit.Region == HitRegion.ColumnResizeGrip)
            {
                Cursor = Cursors.VSplit;
            }
            else
            {
                Cursor = Cursors.Default;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_isResizingColumn)
            {
                _isResizingColumn = false;
                _resizingColIndex = -1;
                Capture = false;
                Cursor = Cursors.Default;
            }
        }

        [Browsable(false)]
        public int RowCount => _rowIndexMap.ActiveCount;

        public int GetModelRowIndex(int visualRowIndex)
        {
            if (visualRowIndex >= 0 && visualRowIndex < _rowIndexMap.ActiveCount)
            {
                return _rowIndexMap[visualRowIndex];
            }
            return -1;
        }

        public void ApplyFilter(Func<int, bool>? predicate)
        {
            if (_dataSource == null) return;
            if (predicate == null)
            {
                _rowIndexMap.ResetIdentity(_dataSource.TotalRowCount);
            }
            else
            {
                _rowIndexMap.Filter(predicate, _dataSource.TotalRowCount);
            }
            _scrollY = 0;
            _selectedVisualRow = -1;
            UpdateScrollBars();
            Invalidate();
        }

        protected virtual void OnHeaderClicked(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= _columns.Count || _dataSource == null || _isSorting) return;

            var col = _columns[columnIndex];
            SortDirection newDirection = col.SortOrder switch
            {
                SortDirection.None => SortDirection.Ascending,
                SortDirection.Ascending => SortDirection.Descending,
                SortDirection.Descending => SortDirection.None,
                _ => SortDirection.Ascending
            };

            _ = SortColumnAsync(columnIndex, newDirection);
        }

        public async Task SortColumnAsync(int columnIndex, SortDirection newDirection)
        {
            if (columnIndex < 0 || columnIndex >= _columns.Count || _dataSource == null || _isSorting) return;

            var col = _columns[columnIndex];

            // Reset other columns
            for (int i = 0; i < _columns.Count; i++)
            {
                if (i != columnIndex) _columns[i].SortOrder = SortDirection.None;
            }
            col.SortOrder = newDirection;

            if (newDirection == SortDirection.None)
            {
                _rowIndexMap.ResetIdentity(_dataSource.TotalRowCount);
                _scrollY = 0;
                UpdateScrollBars();
                Invalidate();
                return;
            }

            int count = _rowIndexMap.ActiveCount;
            if (count <= 0) return;

            _isSorting = true;
            _sortingColumnIndex = columnIndex;
            Cursor = Cursors.WaitCursor;
            SortingStarted?.Invoke(this, EventArgs.Empty);
            Invalidate();

            var source = _dataSource;
            _sortCts?.Cancel();
            _sortCts = new System.Threading.CancellationTokenSource();
            var token = _sortCts.Token;

            // Copy active indices into a background working buffer
            int[] working = new int[count];
            for (int i = 0; i < count; i++)
            {
                working[i] = _rowIndexMap[i];
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    if (source is IZeroSortableSource sortable)
                    {
                        var comparer = new FastSortableComparer(sortable, columnIndex, newDirection);
                        Array.Sort(working, 0, count, comparer);
                    }
                    else
                    {
                        var comparer = new GridColumnComparer(source, columnIndex, newDirection);
                        Array.Sort(working, 0, count, comparer);
                    }
                }, token);

                sw.Stop();

                if (!token.IsCancellationRequested && !IsDisposed)
                {
                    for (int i = 0; i < count; i++)
                    {
                        _rowIndexMap[i] = working[i];
                    }

                    _scrollY = 0;
                    UpdateScrollBars();
                    SortingCompleted?.Invoke(this, sw.Elapsed);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sorting error: {ex}");
            }
            finally
            {
                _isSorting = false;
                _sortingColumnIndex = -1;
                Cursor = Cursors.Default;
                Invalidate();
            }
        }

        public void AutoFitColumnWidth(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= _columns.Count) return;
            var col = _columns[columnIndex];
            int maxW = TextRenderer.MeasureText(col.HeaderText, Font).Width + 32;

            if (_dataSource != null)
            {
                int sampleCount = Math.Min(200, _rowIndexMap.ActiveCount);
                CellValueBuffer buf = new CellValueBuffer();
                for (int i = 0; i < sampleCount; i++)
                {
                    int modelRow = _rowIndexMap[i];
                    _dataSource.GetCellValue(modelRow, columnIndex, ref buf);
                    if (buf.Text.Length > 0)
                    {
                        int w = TextRenderer.MeasureText(buf.Text.ToString(), Font).Width + 24;
                        if (w > maxW) maxW = w;
                    }
                }
            }

            col.Width = Math.Max(col.MinWidth, Math.Min(col.MaxWidth, maxW));
            UpdateScrollBars();
            Invalidate();
        }

        public void AutoFitAllColumns()
        {
            for (int i = 0; i < _columns.Count; i++)
            {
                if (_columns[i].IsVisible)
                {
                    AutoFitColumnWidth(i);
                }
            }
        }

        protected virtual void ShowHeaderContextMenu(int columnIndex, Point location)
        {
            if (columnIndex < 0 || columnIndex >= _columns.Count || _dataSource == null) return;
            var col = _columns[columnIndex];

            var menu = new ContextMenuStrip
            {
                Renderer = new ZeroMenuRenderer(),
                ShowImageMargin = false,
                Font = new Font("Segoe UI", 9.5f)
            };

            // 1. Sort Ascending
            var itemAsc = new ToolStripMenuItem("▲  Sort Ascending", null, (s, e) =>
            {
                _ = SortColumnAsync(columnIndex, SortDirection.Ascending);
            })
            {
                Checked = col.SortOrder == SortDirection.Ascending
            };

            // 2. Sort Descending
            var itemDesc = new ToolStripMenuItem("▼  Sort Descending", null, (s, e) =>
            {
                _ = SortColumnAsync(columnIndex, SortDirection.Descending);
            })
            {
                Checked = col.SortOrder == SortDirection.Descending
            };

            // 3. Clear Sort
            var itemClear = new ToolStripMenuItem("✕  Clear Sorting", null, (s, e) =>
            {
                _ = SortColumnAsync(columnIndex, SortDirection.None);
            })
            {
                Enabled = col.SortOrder != SortDirection.None
            };

            menu.Items.Add(itemAsc);
            menu.Items.Add(itemDesc);
            menu.Items.Add(itemClear);
            menu.Items.Add(new ToolStripSeparator());

            // 4. Auto-fit Width
            var itemFit = new ToolStripMenuItem("↔  Best Fit Column", null, (s, e) =>
            {
                AutoFitColumnWidth(columnIndex);
            });
            var itemFitAll = new ToolStripMenuItem("⇹  Best Fit All Columns", null, (s, e) =>
            {
                AutoFitAllColumns();
            });
            menu.Items.Add(itemFit);
            menu.Items.Add(itemFitAll);

            // 5. Alignment Submenu
            var itemAlign = new ToolStripMenuItem("⬌  Alignment");
            var alignLeft = new ToolStripMenuItem("⬅  Left", null, (s, e) => { col.Alignment = CellAlignment.Left; Invalidate(); })
            {
                Checked = col.Alignment == CellAlignment.Left
            };
            var alignCenter = new ToolStripMenuItem("⬌  Center", null, (s, e) => { col.Alignment = CellAlignment.Center; Invalidate(); })
            {
                Checked = col.Alignment == CellAlignment.Center
            };
            var alignRight = new ToolStripMenuItem("➡  Right", null, (s, e) => { col.Alignment = CellAlignment.Right; Invalidate(); })
            {
                Checked = col.Alignment == CellAlignment.Right
            };
            itemAlign.DropDownItems.Add(alignLeft);
            itemAlign.DropDownItems.Add(alignCenter);
            itemAlign.DropDownItems.Add(alignRight);
            menu.Items.Add(itemAlign);

            menu.Items.Add(new ToolStripSeparator());

            // 6. Hide Column
            var itemHide = new ToolStripMenuItem($"👁  Hide '{col.HeaderText}'", null, (s, e) =>
            {
                col.IsVisible = false;
                UpdateScrollBars();
                Invalidate();
            });
            menu.Items.Add(itemHide);

            // 7. Show All Columns (if any is hidden)
            bool hasHidden = false;
            for (int i = 0; i < _columns.Count; i++)
            {
                if (!_columns[i].IsVisible) { hasHidden = true; break; }
            }
            if (hasHidden)
            {
                var itemShowAll = new ToolStripMenuItem("📋  Show All Columns", null, (s, e) =>
                {
                    for (int i = 0; i < _columns.Count; i++) _columns[i].IsVisible = true;
                    UpdateScrollBars();
                    Invalidate();
                });
                menu.Items.Add(itemShowAll);
            }

            menu.Show(this, location);
        }


        private sealed class FastSortableComparer : System.Collections.Generic.IComparer<int>
        {
            private readonly IZeroSortableSource _source;
            private readonly int _columnIndex;
            private readonly SortDirection _direction;

            public FastSortableComparer(IZeroSortableSource source, int columnIndex, SortDirection direction)
            {
                _source = source;
                _columnIndex = columnIndex;
                _direction = direction;
            }

            public int Compare(int rowA, int rowB)
            {
                int cmp = _source.CompareRows(rowA, rowB, _columnIndex);
                return _direction == SortDirection.Ascending ? cmp : -cmp;
            }
        }

        private sealed class GridColumnComparer : System.Collections.Generic.IComparer<int>
        {
            private readonly IZeroVirtualSource _source;
            private readonly int _columnIndex;
            private readonly SortDirection _direction;

            public GridColumnComparer(IZeroVirtualSource source, int columnIndex, SortDirection direction)
            {
                _source = source;
                _columnIndex = columnIndex;
                _direction = direction;
            }

            public int Compare(int rowA, int rowB)
            {
                CellValueBuffer bufA = new CellValueBuffer();
                CellValueBuffer bufB = new CellValueBuffer();
                _source.GetCellValue(rowA, _columnIndex, ref bufA);
                _source.GetCellValue(rowB, _columnIndex, ref bufB);

                int cmp = bufA.Text.CompareTo(bufB.Text, StringComparison.OrdinalIgnoreCase);
                return _direction == SortDirection.Ascending ? cmp : -cmp;
            }
        }



        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopySelectionToClipboard();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F2 || (e.KeyCode == Keys.Enter && !_isEditing))
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
            else if (e.KeyCode == Keys.Up && _selectedVisualRow > 0)
            {
                if (_isEditing) CommitEdit();
                SelectedVisualRow--;
                EnsureRowVisible(_selectedVisualRow);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down && _selectedVisualRow < _rowIndexMap.ActiveCount - 1)
            {
                if (_isEditing) CommitEdit();
                SelectedVisualRow++;
                EnsureRowVisible(_selectedVisualRow);
                e.Handled = true;
            }
        }

        public void EnsureRowVisible(int visualRowIndex)
        {
            if (visualRowIndex < 0 || visualRowIndex >= _rowIndexMap.ActiveCount) return;
            int rowTop = visualRowIndex * _rowHeight;
            int rowBottom = rowTop + _rowHeight;
            int footerH = ShowFooter ? _footerHeight : 0;
            int viewH = ClientSize.Height - _headerHeight - footerH;

            if (rowTop < _scrollY)
            {
                ScrollY = rowTop;
            }
            else if (rowBottom > _scrollY + viewH)
            {
                ScrollY = rowBottom - viewH;
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

        public Rectangle GetCellRectangle(int visualRow, int colIndex)
        {
            if (visualRow < 0 || colIndex < 0 || colIndex >= _columns.Count) return Rectangle.Empty;

            int cellY = _headerHeight + (visualRow * _rowHeight) - _scrollY;
            int pinnedW = GetPinnedColumnsWidth();

            int cellX;
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

            return new Rectangle(cellX, cellY, _columns[colIndex].Width, _rowHeight);
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

            if (_isEditing)
            {
                CommitEdit();
            }

            EnsureRowVisible(visualRow);

            var rect = GetCellRectangle(visualRow, colIndex);
            if (rect.Width <= 0 || rect.Height <= 0) return;

            CellValueBuffer buf = new CellValueBuffer();
            _dataSource.GetCellValue(modelRow, colIndex, ref buf);
            string val = buf.Text.ToString();

            _isEditing = true;
            _editingVisualRow = visualRow;
            _editingColIndex = colIndex;

            _inPlaceEditor.Font = Font;
            _inPlaceEditor.SetBounds(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2);
            _inPlaceEditor.Text = val;
            _inPlaceEditor.Visible = true;
            _inPlaceEditor.BringToFront();
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

            _inPlaceEditor.Visible = false;
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
                    Invalidate();
                }
            }

            CellEndEdit?.Invoke(this, EventArgs.Empty);
        }

        public void CancelEdit()
        {
            if (!_isEditing) return;

            _inPlaceEditor.Visible = false;
            _isEditing = false;
            _editingVisualRow = -1;
            _editingColIndex = -1;

            CellEndEdit?.Invoke(this, EventArgs.Empty);
            Invalidate();
            Focus();
        }

        private void InPlaceEditor_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CommitEdit();
                if (_selectedVisualRow < _rowIndexMap.ActiveCount - 1)
                {
                    SelectedVisualRow++;
                    EnsureRowVisible(_selectedVisualRow);
                }
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                CancelEdit();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Tab)
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
                e.SuppressKeyPress = true;
            }
        }

        private void UpdateInPlaceEditorBounds()
        {
            if (!_isEditing || _editingVisualRow < 0 || _editingColIndex < 0) return;
            var rect = GetCellRectangle(_editingVisualRow, _editingColIndex);
            int footerH = ShowFooter ? _footerHeight : 0;
            if (rect.Y < _headerHeight || rect.Bottom > ClientSize.Height - footerH)
            {
                CommitEdit();
            }
            else
            {
                _inPlaceEditor.SetBounds(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2);
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ZeroTheme.ThemeChanged -= OnThemeChanged;
                _inPlaceEditor.Dispose();
                _dibSection.Dispose();
                if (_hFont != IntPtr.Zero)
                {
                    NativeMethods.DeleteObject(_hFont);
                    _hFont = IntPtr.Zero;
                }
                if (_hHeaderFont != IntPtr.Zero)
                {
                    NativeMethods.DeleteObject(_hHeaderFont);
                    _hHeaderFont = IntPtr.Zero;
                }
            }
            base.Dispose(disposing);
        }
    }
}

