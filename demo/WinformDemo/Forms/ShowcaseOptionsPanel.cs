using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.Common;
using ZeroUI.Core.Theme;
using ZeroUI.WinForms.DataGrid;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.Samples.WinformDemo.Forms
{
    public sealed class ShowcaseOptionsPanel : UserControl
    {
        private readonly Panel _container;
        private ZeroGridControl? _grid;

        // Behavior controls
        private CheckBox _chkFindPanel = null!;
        private CheckBox _chkHighlightMatches = null!;
        private CheckBox _chkAutoFilter = null!;
        private CheckBox _chkCheckBoxSelector = null!;
        private CheckBox _chkInPlaceEditing = null!;

        // Appearance controls
        private RadioButton _rbDensityCompact = null!;
        private RadioButton _rbDensityNormal = null!;
        private RadioButton _rbDensityTouch = null!;
        private CheckBox _chkAlternatingRows = null!;
        private CheckBox _chkGridlines = null!;
        private CheckBox _chkGroupPanel = null!;
        private CheckBox _chkSummaryFooter = null!;

        // Performance & Data controls
        private Button _btn100k = null!;
        private Button _btn500k = null!;
        private Button _btn1M = null!;
        private Button _btn10M = null!;
        private CheckBox _chkLiveSim = null!;
        private Button _btnStressTest = null!;

        // Action controls
        private Button _btnBestFit = null!;
        private Button _btnExportCsv = null!;
        private Button _btnSaveLayout = null!;
        private Button _btnRestoreLayout = null!;

        // Callbacks
        public event Action<int>? DatasetLoadRequested;
        public event Action? StressTestToggled;
        public event Action<bool>? LiveSimulationToggled;
        public event Action? ExportCsvRequested;

        public ShowcaseOptionsPanel()
        {
            DoubleBuffered = true;
            Dock = DockStyle.Right;
            Width = 270;
            BackColor = Color.FromArgb(248, 249, 251);
            Padding = new Padding(0);

            // Header Banner
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(240, 242, 245),
                Padding = new Padding(12, 10, 12, 10)
            };
            var lblTitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = "⚙️ Options & Properties",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                TextAlign = ContentAlignment.MiddleLeft
            };
            header.Controls.Add(lblTitle);

            _container = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 249, 251),
                Padding = new Padding(12, 8, 12, 16)
            };

            Controls.Add(_container);
            Controls.Add(header);

            BuildOptionsUI();
        }

        public void BindGrid(ZeroGridControl grid)
        {
            _grid = grid;
            SyncFromGrid();
        }

        public void SyncFromGrid()
        {
            if (_grid == null) return;

            _chkAutoFilter.Checked = _grid.ShowAutoFilterRow;
            _chkCheckBoxSelector.Checked = _grid.ShowCheckBoxSelectorColumn;

            if (_grid.Density == GridDensity.Compact) _rbDensityCompact.Checked = true;
            else if (_grid.Density == GridDensity.Loose) _rbDensityTouch.Checked = true;
            else _rbDensityNormal.Checked = true;
        }

        public void ApplyTheme(ZeroSkin skin)
        {
            bool isDark = skin.IsDark;
            BackColor = isDark ? Color.FromArgb(28, 30, 42) : Color.FromArgb(248, 249, 251);
            _container.BackColor = BackColor;

            foreach (Control c in _container.Controls)
            {
                if (c is GroupBox gb)
                {
                    gb.ForeColor = isDark ? Color.FromArgb(200, 210, 230) : Color.FromArgb(70, 80, 95);
                    foreach (Control child in gb.Controls)
                    {
                        if (child is CheckBox cb)
                            cb.ForeColor = isDark ? Color.FromArgb(220, 225, 235) : Color.FromArgb(40, 44, 52);
                        else if (child is RadioButton rb)
                            rb.ForeColor = isDark ? Color.FromArgb(220, 225, 235) : Color.FromArgb(40, 44, 52);
                    }
                }
            }
        }

        private void BuildOptionsUI()
        {
            _container.SuspendLayout();

            int currentY = 8;

            // 1. Group: Behavior
            var gbBehavior = CreateSectionGroup("BEHAVIOR", ref currentY, 175);
            _chkFindPanel = CreateCheckBox("Allow Find Panel", 12, 22, true, val =>
            {
                // Toggle find panel bar visibility
            });
            _chkHighlightMatches = CreateCheckBox("Highlight Find Results", 12, 50, true, val =>
            {
                _grid?.Invalidate();
            });
            _chkAutoFilter = CreateCheckBox("Auto Filter Row", 12, 78, true, val =>
            {
                if (_grid != null) _grid.ShowAutoFilterRow = val;
            });
            _chkCheckBoxSelector = CreateCheckBox("Checkbox Selector Col", 12, 106, false, val =>
            {
                if (_grid != null) _grid.ShowCheckBoxSelectorColumn = val;
            });
            _chkInPlaceEditing = CreateCheckBox("In-Place Cell Editing", 12, 134, true, val =>
            {
                _grid?.Invalidate();
            });
            gbBehavior.Controls.AddRange(new Control[] { _chkFindPanel, _chkHighlightMatches, _chkAutoFilter, _chkCheckBoxSelector, _chkInPlaceEditing });
            _container.Controls.Add(gbBehavior);

            // 2. Group: Appearance & Density
            var gbAppearance = CreateSectionGroup("APPEARANCE & DENSITY", ref currentY, 195);
            _rbDensityCompact = CreateRadioButton("Compact (22px)", 12, 22, false, () =>
            {
                if (_grid != null) _grid.Density = GridDensity.Compact;
            });
            _rbDensityNormal = CreateRadioButton("Normal (26px)", 95, 22, true, () =>
            {
                if (_grid != null) _grid.Density = GridDensity.Middle;
            });
            _rbDensityTouch = CreateRadioButton("Touch (34px)", 175, 22, false, () =>
            {
                if (_grid != null) _grid.Density = GridDensity.Loose;
            });

            _chkAlternatingRows = CreateCheckBox("Alternating Row Colors", 12, 54, true, val =>
            {
                _grid?.Invalidate();
            });
            _chkGridlines = CreateCheckBox("Show Gridlines", 12, 82, true, val =>
            {
                _grid?.Invalidate();
            });
            _chkGroupPanel = CreateCheckBox("Show Group Panel", 12, 110, true, val =>
            {
                // Group panel visibility
            });
            _chkSummaryFooter = CreateCheckBox("Show Summary Footer", 12, 138, true, val =>
            {
                if (_grid != null) _grid.ShowFooter = val;
            });

            gbAppearance.Controls.AddRange(new Control[] {
                _rbDensityCompact, _rbDensityNormal, _rbDensityTouch,
                _chkAlternatingRows, _chkGridlines, _chkGroupPanel, _chkSummaryFooter
            });
            _container.Controls.Add(gbAppearance);

            // 3. Group: Performance & Dataset
            var gbPerformance = CreateSectionGroup("DATASET & STRESS TEST", ref currentY, 155);

            int btnW = 54;
            int btnH = 28;
            _btn100k = CreateButton("100K", 12, 24, btnW, btnH, () => DatasetLoadRequested?.Invoke(100_000));
            _btn500k = CreateButton("500K", 72, 24, btnW, btnH, () => DatasetLoadRequested?.Invoke(500_000));
            _btn1M = CreateButton("1M", 132, 24, btnW, btnH, () => DatasetLoadRequested?.Invoke(1_000_000));
            _btn10M = CreateButton("🔥10M", 192, 24, btnW, btnH, () => DatasetLoadRequested?.Invoke(10_000_000));
            _btn10M.BackColor = Color.FromArgb(239, 68, 68);
            _btn10M.ForeColor = Color.White;

            _chkLiveSim = CreateCheckBox("Simulate Live Updates (100Hz)", 12, 62, false, val =>
            {
                LiveSimulationToggled?.Invoke(val);
            });

            _btnStressTest = CreateButton("🚀 Run Auto-Scroll Stress Test", 12, 94, 234, 32, () =>
            {
                StressTestToggled?.Invoke();
            });
            _btnStressTest.BackColor = Color.FromArgb(18, 86, 209);
            _btnStressTest.ForeColor = Color.White;

            gbPerformance.Controls.AddRange(new Control[] { _btn100k, _btn500k, _btn1M, _btn10M, _chkLiveSim, _btnStressTest });
            _container.Controls.Add(gbPerformance);

            // 4. Group: Actions & Tools
            var gbActions = CreateSectionGroup("ACTIONS & EXPORT", ref currentY, 130);
            _btnBestFit = CreateButton("📐 Best Fit Columns", 12, 24, 234, 28, () =>
            {
                _grid?.BestFitColumns();
            });
            _btnExportCsv = CreateButton("📊 Export to CSV", 12, 58, 234, 28, () =>
            {
                ExportCsvRequested?.Invoke();
            });
            _btnSaveLayout = CreateButton("💾 Save Layout", 12, 92, 114, 26, () =>
            {
                if (_grid != null)
                {
                    string json = _grid.SaveLayoutToJson();
                    MessageBox.Show("Saved column layout successfully!", "Layout Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });
            _btnRestoreLayout = CreateButton("🔄 Restore", 132, 92, 114, 26, () =>
            {
                // Restore
            });

            gbActions.Controls.AddRange(new Control[] { _btnBestFit, _btnExportCsv, _btnSaveLayout, _btnRestoreLayout });
            _container.Controls.Add(gbActions);

            _container.ResumeLayout(true);
        }

        private GroupBox CreateSectionGroup(string title, ref int y, int height)
        {
            var gb = new GroupBox
            {
                Text = title,
                Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 110, 125),
                Location = new Point(4, y),
                Size = new Size(244, height)
            };
            y += height + 10;
            return gb;
        }

        private CheckBox CreateCheckBox(string text, int x, int y, bool isChecked, Action<bool> onChanged)
        {
            var cb = new CheckBox
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Checked = isChecked,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(40, 44, 52),
                Cursor = Cursors.Hand
            };
            cb.CheckedChanged += (s, e) => onChanged(cb.Checked);
            return cb;
        }

        private RadioButton CreateRadioButton(string text, int x, int y, bool isChecked, Action onChecked)
        {
            var rb = new RadioButton
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Checked = isChecked,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(40, 44, 52),
                Cursor = Cursors.Hand
            };
            rb.CheckedChanged += (s, e) =>
            {
                if (rb.Checked) onChecked();
            };
            return rb;
        }

        private Button CreateButton(string text, int x, int y, int width, int height, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
                BackColor = Color.FromArgb(240, 243, 248),
                ForeColor = Color.FromArgb(33, 37, 41),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 225);
            btn.Click += (s, e) => onClick();
            return btn;
        }
    }
}
