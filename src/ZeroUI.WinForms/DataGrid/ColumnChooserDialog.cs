using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ZeroUI.Core.Data;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.DataGrid
{
    /// <summary>
    /// Runtime Column Customization tool window for ZeroUI WinForms GridControl.
    /// Allows users to view hidden columns and restore their visibility via double-click or button.
    /// </summary>
    public class ColumnChooserDialog : Form
    {
        private readonly GridControl _grid;
        private readonly ListBox _hiddenColumnsList;
        private readonly Button _btnShowColumn;
        private readonly Button _btnShowAll;
        private readonly Label _lblInfo;

        public ColumnChooserDialog(GridControl grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));

            Text = "Customization";
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(240, 320);
            MinimumSize = new Size(200, 220);
            TopMost = true;
            ShowInTaskbar = false;

            var colors = ZeroTheme.Colors;
            BackColor = colors.Surface;
            ForeColor = colors.TextPrimary;

            _lblInfo = new Label
            {
                Text = "Double-click column to show in grid:",
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = colors.TextSecondary,
                Padding = new Padding(6, 6, 6, 2)
            };
            Controls.Add(_lblInfo);

            _hiddenColumnsList = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f),
                BackColor = colors.Background,
                ForeColor = colors.TextPrimary,
                BorderStyle = BorderStyle.None,
                IntegralHeight = false
            };
            _hiddenColumnsList.DoubleClick += (s, e) => ShowSelectedColumn();
            Controls.Add(_hiddenColumnsList);

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = colors.Surface,
                Padding = new Padding(6)
            };

            _btnShowColumn = new Button
            {
                Text = "Add Column",
                Dock = DockStyle.Left,
                Width = 100,
                Font = new Font("Segoe UI", 8.5f),
                BackColor = colors.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnShowColumn.FlatAppearance.BorderSize = 0;
            _btnShowColumn.Click += (s, e) => ShowSelectedColumn();
            bottomPanel.Controls.Add(_btnShowColumn);

            _btnShowAll = new Button
            {
                Text = "Show All",
                Dock = DockStyle.Right,
                Width = 90,
                Font = new Font("Segoe UI", 8.5f),
                BackColor = colors.Background,
                ForeColor = colors.TextPrimary,
                FlatStyle = FlatStyle.Flat
            };
            _btnShowAll.FlatAppearance.BorderColor = colors.Border;
            _btnShowAll.Click += (s, e) => ShowAllColumns();
            bottomPanel.Controls.Add(_btnShowAll);

            Controls.Add(bottomPanel);
            _hiddenColumnsList.BringToFront();

            RefreshColumns();
        }

        public void RefreshColumns()
        {
            _hiddenColumnsList.BeginUpdate();
            _hiddenColumnsList.Items.Clear();

            for (int i = 0; i < _grid.Columns.Count; i++)
            {
                var col = _grid.Columns[i];
                if (!col.IsVisible)
                {
                    _hiddenColumnsList.Items.Add(col);
                }
            }

            _hiddenColumnsList.EndUpdate();
            _btnShowColumn.Enabled = _hiddenColumnsList.Items.Count > 0;
            _btnShowAll.Enabled = _hiddenColumnsList.Items.Count > 0;
        }

        private void ShowSelectedColumn()
        {
            if (_hiddenColumnsList.SelectedItem is ZeroColumn col)
            {
                col.IsVisible = true;
                _grid.Invalidate();
                RefreshColumns();
            }
        }

        private void ShowAllColumns()
        {
            for (int i = 0; i < _grid.Columns.Count; i++)
            {
                _grid.Columns[i].IsVisible = true;
            }
            _grid.Invalidate();
            RefreshColumns();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            base.OnFormClosing(e);
        }
    }
}
