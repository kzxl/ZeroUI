using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ZeroUI.WinForms.Editors;

namespace ZeroUI.WinForms.DataGrid
{

    /// <summary>
    /// High-performance pagination toolbar control designed for virtual grids and large datasets.
    /// </summary>
    public class ZeroGridPagination : Panel
    {
        private int _totalRows = 0;
        private int _pageSize = 1000;
        private int _currentPage = 1;

        private readonly Label _lblInfo;
        private readonly ZeroButton _btnFirst;
        private readonly ZeroButton _btnPrev;
        private readonly Label _lblPageInfo;
        private readonly ZeroButton _btnNext;
        private readonly ZeroButton _btnLast;
        private readonly ComboBox _cbPageSize;

        public event EventHandler? PageChanged;

        public ZeroGridPagination()
        {
            Dock = DockStyle.Bottom;
            Height = 46;
            BackColor = Color.FromArgb(249, 250, 251);
            Padding = new Padding(12, 6, 12, 6);

            _lblInfo = new Label
            {
                Text = "Rows: 0 / 0",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(75, 85, 99),
                Location = new Point(14, 14)
            };


            _btnFirst = new ZeroButton
            {
                Text = "⏮",
                Size = new Size(36, 30),
                ButtonStyle = ZeroButtonStyle.Secondary,
                BorderRadius = 4,
                Location = new Point(220, 8)
            };
            _btnFirst.Click += (s, e) => NavigateToPage(1);

            _btnPrev = new ZeroButton
            {
                Text = "◀",
                Size = new Size(36, 30),
                ButtonStyle = ZeroButtonStyle.Secondary,
                BorderRadius = 4,
                Location = new Point(262, 8)
            };
            _btnPrev.Click += (s, e) => NavigateToPage(_currentPage - 1);

            _lblPageInfo = new Label
            {
                Text = "Page 1 / 1",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                Location = new Point(310, 14)
            };

            _btnNext = new ZeroButton
            {
                Text = "▶",
                Size = new Size(36, 30),
                ButtonStyle = ZeroButtonStyle.Secondary,
                BorderRadius = 4,
                Location = new Point(410, 8)
            };
            _btnNext.Click += (s, e) => NavigateToPage(_currentPage + 1);

            _btnLast = new ZeroButton
            {
                Text = "⏭",
                Size = new Size(36, 30),
                ButtonStyle = ZeroButtonStyle.Secondary,
                BorderRadius = 4,
                Location = new Point(452, 8)
            };
            _btnLast.Click += (s, e) => NavigateToPage(TotalPages);

            Label lblPageSizeTitle = new Label
            {
                Text = "Page size:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(75, 85, 99),
                Location = new Point(510, 14)
            };

            _cbPageSize = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                Location = new Point(585, 10),
                Width = 90
            };
            _cbPageSize.Items.AddRange(new object[] { "100", "500", "1,000", "5,000", "All" });
            _cbPageSize.SelectedIndex = 2; // 1,000

            _cbPageSize.SelectedIndexChanged += CbPageSize_SelectedIndexChanged;

            Controls.Add(_lblInfo);
            Controls.Add(_btnFirst);
            Controls.Add(_btnPrev);
            Controls.Add(_lblPageInfo);
            Controls.Add(_btnNext);
            Controls.Add(_btnLast);
            Controls.Add(lblPageSizeTitle);
            Controls.Add(_cbPageSize);

            UpdateState();
        }

        [Category("Data")]
        public int TotalRows
        {
            get => _totalRows;
            set
            {
                _totalRows = Math.Max(0, value);
                if (_currentPage > TotalPages) _currentPage = Math.Max(1, TotalPages);
                UpdateState();
            }
        }

        [Category("Data")]
        public int PageSize
        {
            get => _pageSize;
            set
            {
                _pageSize = value <= 0 ? int.MaxValue : value;
                UpdateState();
            }
        }

        [Category("Data")]
        public int CurrentPage
        {
            get => _currentPage;
            set => NavigateToPage(value);
        }

        public int TotalPages
        {
            get
            {
                if (_pageSize >= _totalRows || _pageSize <= 0) return 1;
                return (int)Math.Ceiling((double)_totalRows / _pageSize);
            }
        }

        public int PageStartRow => (_currentPage - 1) * _pageSize;
        public int PageEndRow => Math.Min(_totalRows, _currentPage * _pageSize);

        public void NavigateToPage(int page)
        {
            int clamped = Math.Max(1, Math.Min(TotalPages, page));
            if (_currentPage != clamped)
            {
                _currentPage = clamped;
                UpdateState();
                PageChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void CbPageSize_SelectedIndexChanged(object? sender, EventArgs e)
        {
            int newSize = _cbPageSize.SelectedIndex switch
            {
                0 => 100,
                1 => 500,
                2 => 1000,
                3 => 5000,
                4 => int.MaxValue,
                _ => 1000
            };
            PageSize = newSize;
            _currentPage = 1;
            PageChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateState()
        {
            int start = _totalRows == 0 ? 0 : PageStartRow + 1;
            int end = PageEndRow;
            _lblInfo.Text = $"Showing {start:N0} - {end:N0} of {TotalRows:N0} rows";
            _lblPageInfo.Text = $"Page {_currentPage:N0} of {TotalPages:N0}";


            _btnFirst.Enabled = _currentPage > 1;
            _btnPrev.Enabled = _currentPage > 1;
            _btnNext.Enabled = _currentPage < TotalPages;
            _btnLast.Enabled = _currentPage < TotalPages;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Top border line
            using var pen = new Pen(Color.FromArgb(229, 231, 235), 1f);
            e.Graphics.DrawLine(pen, 0, 0, Width, 0);
        }
    }
}
