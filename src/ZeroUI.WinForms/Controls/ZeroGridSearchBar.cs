using System;
using System.Drawing;
using System.Windows.Forms;
using ZeroUI.Core.Data;

namespace ZeroUI.WinForms.Controls
{
    /// <summary>
    /// Integrated search and action toolbar control for ZeroGridControl with debounced filtering and export shortcuts.
    /// </summary>
    public class ZeroGridSearchBar : Panel
    {
        private ZeroGridControl? _grid;
        private readonly ZeroSearchBox _searchBox;
        private readonly Label _lblMatchCount;
        private readonly ZeroButton _btnDensity;
        private readonly ZeroButton _btnExport;

        public event EventHandler? ExportClicked;

        public ZeroGridSearchBar()
        {
            Dock = DockStyle.Top;
            Height = 48;
            BackColor = Color.FromArgb(249, 250, 251);
            Padding = new Padding(12, 7, 12, 7);

            _searchBox = new ZeroSearchBox
            {
                PlaceholderText = "🔍 Tìm kiếm mọi cột (gõ để lọc tức thì)...",
                Location = new Point(12, 7),
                Width = 320,
                DebounceIntervalMs = 150
            };
            _searchBox.DebouncedTextChanged += SearchBox_DebouncedTextChanged;

            _lblMatchCount = new Label
            {
                Text = "",
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(79, 70, 229),
                Location = new Point(345, 14)
            };

            _btnDensity = new ZeroButton
            {
                Text = "📏 Mật độ: Vừa",
                ButtonStyle = ZeroButtonStyle.Ghost,
                Size = new Size(125, 32),
                BorderRadius = 4,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(Width - 260, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnDensity.Click += (s, e) =>
            {
                if (_grid == null) return;
                var next = _grid.Density switch
                {
                    ZeroUI.Core.Common.GridDensity.Compact => ZeroUI.Core.Common.GridDensity.Middle,
                    ZeroUI.Core.Common.GridDensity.Middle => ZeroUI.Core.Common.GridDensity.Loose,
                    ZeroUI.Core.Common.GridDensity.Loose => ZeroUI.Core.Common.GridDensity.Compact,
                    _ => ZeroUI.Core.Common.GridDensity.Middle
                };
                _grid.Density = next;
                _btnDensity.Text = next switch
                {
                    ZeroUI.Core.Common.GridDensity.Compact => "📏 Mật độ: Dày",
                    ZeroUI.Core.Common.GridDensity.Middle => "📏 Mật độ: Vừa",
                    ZeroUI.Core.Common.GridDensity.Loose => "📏 Mật độ: Thoáng",
                    _ => "📏 Mật độ"
                };
            };

            _btnExport = new ZeroButton
            {
                Text = "📊 Xuất CSV",
                ButtonStyle = ZeroButtonStyle.Secondary,
                Size = new Size(110, 32),
                BorderRadius = 4,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(Width - 125, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnExport.Click += (s, e) => ExportClicked?.Invoke(this, EventArgs.Empty);

            Controls.Add(_searchBox);
            Controls.Add(_lblMatchCount);
            Controls.Add(_btnDensity);
            Controls.Add(_btnExport);
        }


        public void AttachToGrid(ZeroGridControl grid)
        {
            _grid = grid;
            UpdateCountBadge();
        }

        private void SearchBox_DebouncedTextChanged(object? sender, string query)
        {
            if (_grid == null || _grid.DataSource == null) return;

            string trimmed = query.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                _grid.ApplyFilter(null);
                _lblMatchCount.Text = "";
                return;
            }

            var src = _grid.DataSource;
            int colCount = _grid.Columns.Count;

            _grid.ApplyFilter(modelRow =>
            {
                CellValueBuffer buf = new CellValueBuffer();
                for (int c = 0; c < colCount; c++)
                {
                    if (!_grid.Columns[c].IsVisible) continue;
                    buf.Reset();
                    src.GetCellValue(modelRow, c, ref buf);
                    if (buf.Text.IndexOf(trimmed.AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
                return false;
            });


            UpdateCountBadge();
        }

        public void UpdateCountBadge()
        {
            if (_grid == null || _grid.DataSource == null)
            {
                _lblMatchCount.Text = "";
                return;
            }

            if (_grid.RowCount < _grid.DataSource.TotalRowCount)
            {
                _lblMatchCount.Text = $"Khớp: {_grid.RowCount:N0} / {_grid.DataSource.TotalRowCount:N0} dòng";
            }
            else
            {
                _lblMatchCount.Text = "";
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Bottom border line
            using var pen = new Pen(Color.FromArgb(229, 231, 235), 1f);
            e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
        }
    }
}
