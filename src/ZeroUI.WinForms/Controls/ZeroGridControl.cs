using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ZeroUI.Core.Common;
using ZeroUI.Core.Data;
using ZeroUI.Core.Input;
using ZeroUI.Core.Layout;
using ZeroUI.Core.Virtualization;
using ZeroUI.WinForms.Native;
using ZeroUI.WinForms.Rendering;

namespace ZeroUI.WinForms.Controls
{
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
        private bool _isResizingColumn = false;
        private int _resizingColIndex = -1;
        private int _resizeStartX = 0;
        private int _resizeStartWidth = 0;

        // Color Palettes (Win32 0x00BBGGRR format)
        private uint _headerBgColor = 0x00F0F0F0;
        private uint _headerTextColor = 0x00202020;
        private uint _rowBgColor = 0x00FFFFFF;
        private uint _altRowBgColor = 0x00FAFAFA;
        private uint _selectedBgColor = 0x00E0D0B0;
        private uint _gridLineColor = 0x00E5E5E5;
        private uint _cellTextColor = 0x00101010;

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
                int maxScroll = Math.Max(0, (_dataSource?.TotalRowCount ?? 0) * _rowHeight - (ClientSize.Height - _headerHeight));
                int clamped = Math.Max(0, Math.Min(maxScroll, value));
                if (_scrollY != clamped)
                {
                    _scrollY = clamped;
                    UpdateScrollBars();
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
                    Invalidate();
                }
            }
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

            int clientH = ClientSize.Height - _headerHeight;
            int clientW = ClientSize.Width;
            int totalRows = _rowIndexMap.ActiveCount;
            int totalH = totalRows * _rowHeight;
            int totalW = GetTotalColumnsWidth();

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
                nMax = Math.Max(0, totalW),
                nPage = (uint)Math.Max(0, clientW),
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

                // 1. Render Cells
                if (_dataSource != null && totalRows > 0 && totalCols > 0)

                {
                    _dibSection.SelectFont(_hFont);

                    int clientDataHeight = height - _headerHeight;
                    var range = VirtualViewport2D.ComputeUniform(
                        _scrollX,
                        _scrollY,
                        width,
                        clientDataHeight,
                        _rowHeight,
                        totalRows,
                        colWidths,
                        totalCols);

                    CellValueBuffer cellBuffer = new CellValueBuffer();

                    int currentY = _headerHeight + range.FirstRowY;
                    for (int r = range.StartRow; r <= range.EndRow && r < totalRows; r++)
                    {
                        int modelRow = _rowIndexMap[r];
                        bool isSelected = (r == _selectedVisualRow);
                        uint rowBg = isSelected ? _selectedBgColor : ((r % 2 == 1) ? _altRowBgColor : _rowBgColor);

                        // Row background
                        _dibSection.FillRectangle(0, currentY, width, _rowHeight, rowBg);

                        int currentX = range.FirstColX;
                        for (int c = range.StartCol; c <= range.EndCol && c < totalCols; c++)
                        {
                            int colW = colWidths[c];
                            if (colW <= 0) continue;

                            cellBuffer.Reset();
                            cellBuffer.TextColor = _cellTextColor;
                            cellBuffer.BackColor = rowBg;
                            cellBuffer.Alignment = _columns[c].Alignment;

                            _dataSource.GetCellValue(modelRow, c, ref cellBuffer);

                            RECT cellRect = new RECT(currentX, currentY, currentX + colW, currentY + _rowHeight);

                            if (cellBuffer.HasCustomBackground)
                            {
                                _dibSection.FillRectangle(cellRect.Left, cellRect.Top, colW, _rowHeight, cellBuffer.BackColor);
                            }

                            // Text
                            _dibSection.DrawText(cellBuffer.Text, ref cellRect, cellBuffer.TextColor, cellBuffer.Alignment, textHeight);

                            // Vertical Gridline
                            _dibSection.FillRectangle(cellRect.Right - 1, currentY, 1, _rowHeight, _gridLineColor);

                            currentX += colW;
                        }

                        // Horizontal Gridline
                        _dibSection.FillRectangle(0, currentY + _rowHeight - 1, width, 1, _gridLineColor);

                        currentY += _rowHeight;
                    }
                }
                else
                {
                    // ZeroUI Native Empty State (Zero-Alloc Viewport)
                    int emptyCenterY = (_headerHeight + height) / 2 - 20;

                    _dibSection.SelectFont(_hHeaderFont);
                    RECT emptyTitleRect = new RECT(20, emptyCenterY, width - 20, emptyCenterY + 24);
                    _dibSection.DrawText("No matching data found".AsSpan(), ref emptyTitleRect, 0x00666666, CellAlignment.Center, Font.Height);

                    _dibSection.SelectFont(_hFont);
                    RECT emptySubRect = new RECT(20, emptyCenterY + 26, width - 20, emptyCenterY + 50);
                    _dibSection.DrawText("Try adjusting your search keywords or clearing active filters".AsSpan(), ref emptySubRect, 0x00999999, CellAlignment.Center, Font.Height);
                }



                // 2. Render Header Row (Always on top with Bold Header Font)
                _dibSection.SelectFont(_hHeaderFont);
                _dibSection.FillRectangle(0, 0, width, _headerHeight, _headerBgColor);
                _dibSection.FillRectangle(0, _headerHeight - 1, width, 1, 0x00CCCCCC);

                int headerX = -_scrollX;
                for (int c = 0; c < totalCols; c++)
                {
                    int colW = colWidths[c];
                    if (colW <= 0) continue;

                    RECT colRect = new RECT(headerX, 0, headerX + colW, _headerHeight);
                    string text = _columns[c].HeaderText;

                    if (_columns[c].SortOrder == SortDirection.Ascending)
                    {
                        text += " ▲";
                    }
                    else if (_columns[c].SortOrder == SortDirection.Descending)
                    {
                        text += " ▼";
                    }

                    _dibSection.DrawText(text.AsSpan(), ref colRect, _headerTextColor, _columns[c].Alignment, textHeight);

                    // Column separator
                    _dibSection.FillRectangle(headerX + colW - 1, 4, 1, _headerHeight - 8, 0x00CCCCCC);

                    headerX += colW;
                }

                // 3. BitBlt to Screen in <0.5ms
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
            int totalH = (_dataSource?.TotalRowCount ?? 0) * _rowHeight;
            int maxScroll = Math.Max(0, totalH - (ClientSize.Height - _headerHeight));

            switch (action)
            {
                case NativeMethods.SB_LINEUP:
                    _scrollY = Math.Max(0, _scrollY - _rowHeight);
                    break;
                case NativeMethods.SB_LINEDOWN:
                    _scrollY = Math.Min(maxScroll, _scrollY + _rowHeight);
                    break;
                case NativeMethods.SB_PAGEUP:
                    _scrollY = Math.Max(0, _scrollY - (ClientSize.Height - _headerHeight));
                    break;
                case NativeMethods.SB_PAGEDOWN:
                    _scrollY = Math.Min(maxScroll, _scrollY + (ClientSize.Height - _headerHeight));
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
            Invalidate();
        }

        private void HandleHScroll(IntPtr wParam)
        {
            int action = unchecked((short)(long)wParam);
            int totalW = GetTotalColumnsWidth();
            int maxScroll = Math.Max(0, totalW - ClientSize.Width);

            switch (action)
            {
                case NativeMethods.SB_LINEUP:
                    _scrollX = Math.Max(0, _scrollX - 20);
                    break;
                case NativeMethods.SB_LINEDOWN:
                    _scrollX = Math.Min(maxScroll, _scrollX + 20);
                    break;
                case NativeMethods.SB_PAGEUP:
                    _scrollX = Math.Max(0, _scrollX - ClientSize.Width);
                    break;
                case NativeMethods.SB_PAGEDOWN:
                    _scrollX = Math.Min(maxScroll, _scrollX + ClientSize.Width);
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
            Invalidate();
        }

        private void HandleMouseWheel(IntPtr wParam)
        {
            int delta = unchecked((short)((long)wParam >> 16));
            int scrollDelta = (delta / 120) * (_rowHeight * 3);

            int totalH = (_dataSource?.TotalRowCount ?? 0) * _rowHeight;
            int maxScroll = Math.Max(0, totalH - (ClientSize.Height - _headerHeight));

            _scrollY = Math.Max(0, Math.Min(maxScroll, _scrollY - scrollDelta));
            UpdateScrollBars();
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

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
                    _columns.Count,
                    _dataSource?.TotalRowCount ?? 0);

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
                    OnHeaderClicked(hit.ColumnIndex);
                }
                else if (hit.Region == HitRegion.Cell)
                {
                    SelectedVisualRow = hit.RowIndex;
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
                Invalidate();
                return;
            }

            var hit = SpatialHitTester.HitTest(
                e.X,
                e.Y,
                _headerHeight,
                _rowHeight,
                _scrollX,
                _scrollY,
                GetVisibleColumnWidths(),
                _columns.Count,
                _dataSource?.TotalRowCount ?? 0);

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
            if (columnIndex < 0 || columnIndex >= _columns.Count || _dataSource == null) return;

            var col = _columns[columnIndex];
            SortDirection newDirection = col.SortOrder switch
            {
                SortDirection.None => SortDirection.Ascending,
                SortDirection.Ascending => SortDirection.Descending,
                SortDirection.Descending => SortDirection.None,
                _ => SortDirection.Ascending
            };

            // Reset other columns
            for (int i = 0; i < _columns.Count; i++)
            {
                if (i != columnIndex) _columns[i].SortOrder = SortDirection.None;
            }
            col.SortOrder = newDirection;

            if (newDirection == SortDirection.None)
            {
                _rowIndexMap.ResetIdentity(_dataSource.TotalRowCount);
            }
            else
            {
                _rowIndexMap.Sort(new GridColumnComparer(_dataSource, columnIndex, newDirection));
            }

            Invalidate();
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
            else if (e.KeyCode == Keys.Up && _selectedVisualRow > 0)
            {
                SelectedVisualRow--;
                EnsureRowVisible(_selectedVisualRow);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down && _selectedVisualRow < _rowIndexMap.ActiveCount - 1)
            {
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
            int viewH = ClientSize.Height - _headerHeight;

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
            if (_selectedVisualRow < 0 || _selectedVisualRow >= _rowIndexMap.ActiveCount || _dataSource == null) return;

            int modelRow = _rowIndexMap[_selectedVisualRow];
            CellValueBuffer buf = new CellValueBuffer();
            var sb = new System.Text.StringBuilder();

            for (int c = 0; c < _columns.Count; c++)
            {
                if (!_columns[c].IsVisible) continue;
                if (c > 0) sb.Append('\t');
                buf.Reset();
                _dataSource.GetCellValue(modelRow, c, ref buf);
                sb.Append(buf.Text.ToString());
            }

            try { Clipboard.SetText(sb.ToString()); } catch { }
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
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

