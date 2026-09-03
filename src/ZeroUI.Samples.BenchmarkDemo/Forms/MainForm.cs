using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using ZeroUI.Core.Common;
using ZeroUI.Core.Data;
using ZeroUI.Samples.BenchmarkDemo.Data;
using ZeroUI.Samples.BenchmarkDemo.Diagnostics;
using ZeroUI.WinForms.DataGrid;
using ZeroUI.WinForms.Editors;
using ZeroUI.WinForms.Industrial;
using ZeroUI.WinForms.Overlays;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.Samples.BenchmarkDemo.Forms


{
    public sealed class MainForm : Form
    {
        private readonly PerformanceMonitor _perfMonitor = new PerformanceMonitor();
        private readonly System.Windows.Forms.Timer _hudTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _autoScrollTimer = new System.Windows.Forms.Timer();

        private InventoryItem[] _dataset = Array.Empty<InventoryItem>();
        private ZeroInventorySource? _zeroSource;

        // UI Controls
        private Panel _topPanel = null!;
        private Panel _hudPanel = null!;
        private TabControl _tabControl = null!;
        private TabPage _tabZero = null!;
        private TabPage _tabDgv = null!;

        private ZeroGridControl _zeroGrid = null!;
        private DataGridView _dgv = null!;

        private ZeroGridSearchBar _searchBar = null!;
        private ZeroGridPagination _pagination = null!;
        private ZeroToolbar _mainToolbar = null!;
        private ZeroDrawer _drawer = null!;
        private TabPage _tabControls = null!;
        private TabPage _tabMes = null!;
        private TabPage _tabScada = null!;
        private TabPage _tabWms = null!;
        private TabPage _tabAdvanced = null!;
        private ZeroSteps _mesSteps = null!;

        private ZeroListView _showcaseLog = null!;
        private System.Windows.Forms.Timer? _logGenTimer;
        private System.Windows.Forms.Timer? _scadaSimTimer;






        // HUD Labels
        private Label _lblStatus = null!;
        private Label _lblFps = null!;
        private Label _lblLatency = null!;
        private Label _lblRam = null!;
        private Label _lblGc = null!;
        private Button _btnAutoScroll = null!;

        // Stress Test State
        private bool _isStressTesting = false;
        private int _stressTestElapsedTicks = 0;
        private int _stressTestDirection = 1;
        private int _baselineGen0 = 0;
        private Stopwatch _scrollStopwatch = new Stopwatch();
        private int _scrollFrames = 0;

        public MainForm()
        {
            InitializeComponents();

            _hudTimer.Interval = 200;
            _hudTimer.Tick += (s, e) => UpdateHud();
            _hudTimer.Start();

            _autoScrollTimer.Interval = 16; // ~60 Hz tick
            _autoScrollTimer.Tick += AutoScrollTick;

            // Defer initial dataset generation to Shown event for instant 0ms window popup
            Shown += (s, e) => LoadDataset(100_000);
        }


        private void InitializeComponents()
        {
            Text = "⚡ ZeroUI vs Standard DataGridView — 1,000,000 Rows Performance Benchmark";
            Size = new Size(1280, 800);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            BackColor = Color.FromArgb(245, 246, 250);

            // 1. Top Action Panel
            _topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 54,
                BackColor = Color.FromArgb(24, 26, 40),
                Padding = new Padding(12, 10, 12, 10)
            };

            Label lblTitle = new Label
            {
                Text = "⚡ ZeroUI Benchmark Suite",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(12, 14)
            };
            _topPanel.Controls.Add(lblTitle);

            int btnX = 280;
            Button btn100k = CreateActionButton("100K Rows", btnX, () => LoadDataset(100_000));
            btnX += 110;
            Button btn500k = CreateActionButton("500K Rows", btnX, () => LoadDataset(500_000));
            btnX += 110;
            Button btn1M = CreateActionButton("1M Rows", btnX, () => LoadDataset(1_000_000));
            btnX += 110;
            Button btn10M = CreateActionButton("🔥 10M Rows", btnX, () => LoadDataset(10_000_000));
            btn10M.BackColor = Color.FromArgb(190, 24, 24);
            btnX += 130;

            _btnAutoScroll = new Button
            {
                Text = "🚀 Run Auto-Scroll Stress Test (10s)",
                Location = new Point(btnX, 10),
                Size = new Size(270, 34),
                BackColor = Color.FromArgb(79, 70, 229),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnAutoScroll.FlatAppearance.BorderSize = 0;
            _btnAutoScroll.Click += (s, e) => ToggleStressTest();
            _topPanel.Controls.Add(_btnAutoScroll);


            _topPanel.Controls.Add(btn100k);
            _topPanel.Controls.Add(btn500k);
            _topPanel.Controls.Add(btn1M);
            _topPanel.Controls.Add(btn10M);


            // 2. Metric HUD Panel
            _hudPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(17, 19, 31),
                Padding = new Padding(12, 8, 12, 8)
            };

            _lblStatus = CreateMetricLabel("Data: Ready", 12);
            _lblFps = CreateMetricLabel("FPS: --", 240);
            _lblLatency = CreateMetricLabel("Latency: -- ms", 400);
            _lblRam = CreateMetricLabel("RAM: -- MB", 560);
            _lblGc = CreateMetricLabel("GC Gen0: --", 730);

            _hudPanel.Controls.Add(_lblStatus);
            _hudPanel.Controls.Add(_lblFps);
            _hudPanel.Controls.Add(_lblLatency);
            _hudPanel.Controls.Add(_lblRam);
            _hudPanel.Controls.Add(_lblGc);

            // 2.5. ZeroToolbar (Modern Enterprise Action Bar)
            _mainToolbar = new ZeroToolbar
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.White
            };

            var btnNew = _mainToolbar.AddButton("New Order", "➕", (s, e) =>
            {
                var formPanel = new Panel { Dock = DockStyle.Fill };
                var desc = new ZeroDescriptions { Dock = DockStyle.Fill, Columns = 1, RowHeight = 32 };
                desc.Add("Order Code", "ORD-2026-9081");
                desc.Add("Target Product", "B1030 MAX (Rev 2.0)");
                desc.Add("Quantity", "10,000 units");
                desc.Add("Due Date", DateTime.Today.AddDays(7).ToString("yyyy-MM-dd"));
                formPanel.Controls.Add(desc);

                ZeroModal.Show(this, "Create Production Order", formPanel, onOk: () =>
                {
                    ZeroToast.Success(this, "Work Order ORD-2026-9081 created successfully!");
                });
            });
            btnNew.IsPrimary = true;

            _mainToolbar.AddButton("Detail Drawer", "📋", (s, e) => _drawer.Toggle());

            _mainToolbar.AddButton("Export CSV", "📊", (s, e) => _searchBar.TriggerExport());
            _mainToolbar.AddButton("Refresh", "🔄", (s, e) =>
            {
                LoadDataset(100_000);
                ZeroToast.Info(this, "Dataset refreshed successfully.");
            });

            _mainToolbar.AddSeparator();

            _mainToolbar.AddDropdown("Views", "👁", (s, e) =>
            {
                ZeroToast.Info(this, "Views: Standard Grid, Pivot Analysis, Production Line View");
            });

            _mainToolbar.AddDropdown("Tools", "🛠", (s, e) =>
            {
                ZeroToast.Info(this, "Diagnostic Tools: GC Zero-Alloc Profiler, Memory DC Monitor");
            });

            var btnAlerts = _mainToolbar.AddButton("Alerts", "🔔", (s, e) =>
            {
                ZeroToast.Warning(this, "Active Alert: SMT Feeder BOA472 low stock warning.");
            });
            btnAlerts.BadgeCount = 3;

            _mainToolbar.AddSpacer(); // Elastic right spacer

            var btnTheme = _mainToolbar.AddButton("🌙 Dark Mode", null, (s, e) =>
            {
                ZeroTheme.ToggleTheme();
                (s as ZeroToolbarButton)!.Text = ZeroTheme.IsDark ? "☀️ Light Mode" : "🌙 Dark Mode";
                _tabZero.BackColor = ZeroTheme.Colors.Background;
                _tabMes.BackColor = ZeroTheme.Colors.Background;
                _tabControls.BackColor = ZeroTheme.Colors.Background;
                _tabScada.BackColor = ZeroTheme.Colors.Background;
                _tabWms.BackColor = ZeroTheme.Colors.Background;
                _tabAdvanced.BackColor = ZeroTheme.Colors.Background;
                ZeroToast.Info(this, $"Switched theme to: {(ZeroTheme.IsDark ? "Obsidian Dark" : "Clean Light")}");
            });

            _mainToolbar.AddButton("Bo Góc: Bật", "📐", (s, e) =>
            {
                ZeroUIConfig.RoundedCorners = !ZeroUIConfig.RoundedCorners;
                (s as ZeroToolbarButton)!.Text = ZeroUIConfig.RoundedCorners ? "Bo Góc: Bật" : "Bo Góc: Tắt (Vuông)";
                ZeroToast.Info(this, $"Đã đổi kiểu góc toàn cục: {(ZeroUIConfig.RoundedCorners ? "Bo Tròn (Rounded - 6px)" : "Vuông Vức (Sharp - 0px)")}");
                Invalidate(true);
            });

            _mainToolbar.AddButton("Fullscreen", "⛶", (s, e) =>
            {
                WindowState = (WindowState == FormWindowState.Maximized) ? FormWindowState.Normal : FormWindowState.Maximized;
            });

            _mainToolbar.AddButton("Settings", "⚙️", (s, e) =>
            {
                OpenGlobalSettingsDialog();
            });

            _mainToolbar.AddButton("Help", "❓", (s, e) =>
            {
                ZeroToast.Info(this, "Repository: https://github.com/kzxl/ZeroUI");
            });

            // 3. Setup Side Drawer
            _drawer = new ZeroDrawer
            {
                Title = "Material & Lot Specification",
                Subtitle = "Deep inspection for selected inventory entity",
                DrawerWidth = 400
            };
            var descDrawer = new ZeroDescriptions { Dock = DockStyle.Fill, Columns = 1, RowHeight = 30 };
            descDrawer.Add("Part Number", "BOA437-SMT-V2");
            descDrawer.Add("Category", "Microcontroller / Active");
            descDrawer.Add("Standard Cost", "$14.20");
            descDrawer.Add("Lead Time", "3 Days");
            descDrawer.Add("Safety Stock", "500 Units");
            descDrawer.Add("Supplier", "Foxconn Precision Co.");
            descDrawer.Add("Inspection Status", "Passed OQC", Color.FromArgb(16, 185, 129));
            _drawer.ContentPanel.Controls.Add(descDrawer);

            // 4. Tab Control
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Padding = new Point(16, 8)
            };

            _tabZero = new TabPage("⚡ ZeroGrid (ZeroUI Core Engine)");
            _tabDgv = new TabPage("🐢 Default DataGridView (VirtualMode)");
            _tabControls = new TabPage("🎨 Components Showcase");
            _tabMes = new TabPage("🏭 MES Production Dashboard");
            _tabScada = new TabPage("🔬 SCADA & Smart Factory Hub");
            _tabWms = new TabPage("📦 WMS & Quality Inspection Center");
            _tabAdvanced = new TabPage("🚀 Advanced Enterprise Suite");

            InitializeZeroGrid();
            InitializeDataGridView();
            InitializeComponentsShowcase();
            InitializeMesDashboard();
            InitializeScadaHub();
            InitializeWmsCenter();
            InitializeAdvancedSuite();

            _tabZero.Controls.Add(_zeroGrid);
            _tabZero.Controls.Add(_pagination);
            _tabZero.Controls.Add(_searchBar);

            _tabDgv.Controls.Add(_dgv);

            _tabControl.TabPages.Add(_tabZero);
            _tabControl.TabPages.Add(_tabDgv);
            _tabControl.TabPages.Add(_tabControls);
            _tabControl.TabPages.Add(_tabMes);
            _tabControl.TabPages.Add(_tabScada);
            _tabControl.TabPages.Add(_tabWms);
            _tabControl.TabPages.Add(_tabAdvanced);



            _tabControl.SelectedIndexChanged += (s, e) =>
            {
                _scrollFrames = 0;
                _scrollStopwatch.Restart();
                if (_tabControl.SelectedTab == _tabDgv && _dgv.RowCount != _dataset.Length && _dataset.Length > 0)
                {
                    try
                    {
                        _dgv.SuspendLayout();
                        _dgv.Rows.Clear();
                        _dgv.RowCount = _dataset.Length;
                        _dgv.ResumeLayout();
                    }
                    catch { }
                }
            };


            // Assembly Form Layout
            Controls.Add(_drawer);
            Controls.Add(_tabControl);
            Controls.Add(_mainToolbar);
            Controls.Add(_hudPanel);
            Controls.Add(_topPanel);
        }




        private Button CreateActionButton(string text, int x, Action onClick)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, 10),
                Size = new Size(100, 34),
                BackColor = Color.FromArgb(38, 42, 68),
                ForeColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private Label CreateMetricLabel(string text, int x)
        {
            return new Label
            {
                Text = text,
                ForeColor = Color.FromArgb(166, 227, 161),
                Font = new Font("Consolas", 10f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(x, 12)
            };
        }

        private void InitializeZeroGrid()
        {
            _zeroGrid = new ZeroGridControl
            {
                Dock = DockStyle.Fill,
                HeaderHeight = 32,
                RowHeight = 28
            };

            _zeroGrid.Columns.Add(new ZeroColumn("ID", 70, CellAlignment.Right));
            _zeroGrid.Columns.Add(new ZeroColumn("Item Code", 120, CellAlignment.Left));
            _zeroGrid.Columns.Add(new ZeroColumn("Item Name / Description", 280, CellAlignment.Left));
            _zeroGrid.Columns.Add(new ZeroColumn("Quantity", 90, CellAlignment.Right));
            _zeroGrid.Columns.Add(new ZeroColumn("Unit Price ($)", 130, CellAlignment.Right));
            _zeroGrid.Columns.Add(new ZeroColumn("Total Amount ($)", 150, CellAlignment.Right));
            _zeroGrid.Columns.Add(new ZeroColumn("Batch No", 120, CellAlignment.Center));
            _zeroGrid.Columns.Add(new ZeroColumn("Status", 130, CellAlignment.Center));

            _zeroGrid.SortingStarted += (s, e) =>
            {
                _lblStatus.Text = "Data: Sorting rows asynchronously (0ms UI freeze)...";
            };

            _zeroGrid.SortingCompleted += (s, elapsed) =>
            {
                _lblStatus.Text = $"Data: {_zeroGrid.RowCount:N0} rows (Sorted in {elapsed.TotalMilliseconds:F1} ms)";
            };

            _searchBar = new ZeroGridSearchBar();
            _searchBar.AttachToGrid(_zeroGrid);
            _searchBar.ExportClicked += HandleExportCsv;

            _pagination = new ZeroGridPagination();
            _pagination.PageChanged += (s, e) =>
            {
                _zeroGrid.ScrollToRow(_pagination.PageStartRow);
            };
        }


        private void InitializeDataGridView()
        {
            _dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                VirtualMode = true,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToOrderColumns = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // Enable double buffering on DataGridView via reflection
            typeof(DataGridView).InvokeMember(
                "DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null, _dgv, new object[] { true });

            _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", Width = 70 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Item Code", Width = 120 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Item Name / Description", Width = 280 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Quantity", Width = 90 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Unit Price ($)", Width = 130 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Total Amount ($)", Width = 150 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Batch No", Width = 120 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", Width = 130 });


            _dgv.CellValueNeeded += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _dataset.Length) return;
                ref readonly var item = ref _dataset[e.RowIndex];

                switch (e.ColumnIndex)
                {
                    case 0: e.Value = item.Id; break;
                    case 1: e.Value = item.ItemCode; break;
                    case 2: e.Value = item.ItemName; break;
                    case 3: e.Value = item.Quantity; break;
                    case 4: e.Value = item.UnitPrice.ToString("N0"); break;
                    case 5: e.Value = item.TotalAmount.ToString("N0"); break;
                    case 6: e.Value = item.LotNumber; break;
                    case 7: e.Value = item.Status; break;
                }
            };
        }

        private void LoadDataset(int count)
        {
            if (_isStressTesting)
            {
                ToggleStressTest();
            }

            Cursor = Cursors.WaitCursor;
            _lblStatus.Text = $"Data: Generating {count:N0} rows...";

            Stopwatch sw = Stopwatch.StartNew();

            if (count >= 10_000_000)
            {
                // Extreme Procedural Virtual Source (0 allocation overhead, <45MB RAM!)
                var procSource = new ZeroProceduralSource(count);
                _zeroGrid.DataSource = procSource;

                // For DataGridView: Protect from OutOfMemory crash at 10M rows
                try
                {
                    _dgv.SuspendLayout();
                    _dgv.Rows.Clear();
                    _dgv.RowCount = 1;
                    _dgv.ResumeLayout();
                }
                catch { }

                sw.Stop();
                Cursor = Cursors.Default;
                _pagination.TotalRows = count;
                _searchBar.UpdateCountBadge();
                _baselineGen0 = GC.CollectionCount(0);
                _lblStatus.Text = $"Data: {count:N0} rows (ZeroUI initialized in {sw.ElapsedMilliseconds} ms - DGV OOM Protected)";
                _scrollFrames = 0;
                _scrollStopwatch.Restart();
                return;
            }

            _dataset = MockDataGenerator.Generate(count);
            _zeroSource = new ZeroInventorySource(_dataset);

            // Bind to ZeroGrid (Executes in <1ms)
            _zeroGrid.DataSource = _zeroSource;

            // Bind to DataGridView only if active to avoid blocking UI thread
            if (_tabControl.SelectedTab == _tabDgv)
            {
                try
                {
                    _dgv.SuspendLayout();
                    _dgv.Rows.Clear();
                    _dgv.RowCount = count;
                    _dgv.ResumeLayout();
                }
                catch { }
            }

            sw.Stop();
            Cursor = Cursors.Default;


            _pagination.TotalRows = count;
            _searchBar.UpdateCountBadge();
            _baselineGen0 = GC.CollectionCount(0);
            _lblStatus.Text = $"Data: {count:N0} rows (Loaded in {sw.ElapsedMilliseconds} ms)";
            _scrollFrames = 0;
            _scrollStopwatch.Restart();

        }


        private void ToggleStressTest()
        {
            if (_isStressTesting)
            {
                _autoScrollTimer.Stop();
                _isStressTesting = false;
                _btnAutoScroll.Text = "🚀 Chạy Auto-Scroll Stress Test (10s)";
                _btnAutoScroll.BackColor = Color.FromArgb(79, 70, 229);
            }
            else
            {
                _stressTestElapsedTicks = 0;
                _stressTestDirection = 1;
                _scrollFrames = 0;
                _baselineGen0 = GC.CollectionCount(0);
                _scrollStopwatch.Restart();

                _isStressTesting = true;
                _btnAutoScroll.Text = "⏹️ Đang Stress Test... Bấm để dừng";
                _btnAutoScroll.BackColor = Color.FromArgb(220, 38, 38);
                _autoScrollTimer.Start();
            }
        }

        private void AutoScrollTick(object? sender, EventArgs e)
        {
            _stressTestElapsedTicks++;

            // Auto stop after 10 seconds (~600 ticks)
            if (_stressTestElapsedTicks > 600)
            {
                ToggleStressTest();
                return;
            }

            int total = _tabControl.SelectedTab == _tabZero
                ? (_zeroGrid.DataSource?.TotalRowCount ?? _dataset.Length)
                : _dgv.RowCount;

            if (total <= 0) return;

            int step = total >= 5_000_000 ? 2500 : 250;

            if (_tabControl.SelectedTab == _tabZero)
            {
                int currentY = _zeroGrid.ScrollY;
                int rowH = _zeroGrid.RowHeight;
                int maxY = Math.Max(0, total * rowH - (_zeroGrid.ClientSize.Height - _zeroGrid.HeaderHeight));
                int nextY = currentY + (step * _stressTestDirection * rowH);

                if (nextY >= maxY)
                {
                    nextY = maxY;
                    _stressTestDirection = -1;
                }
                else if (nextY <= 0)
                {
                    nextY = 0;
                    _stressTestDirection = 1;
                }

                _zeroGrid.ScrollY = nextY;
            }
            else
            {
                if (_dgv.RowCount <= 1) return;
                int current = _dgv.FirstDisplayedScrollingRowIndex;
                int next = current + (step * _stressTestDirection);

                if (next >= _dgv.RowCount)
                {
                    next = Math.Max(0, _dgv.RowCount - 1);
                    _stressTestDirection = -1;
                }
                else if (next <= 0)
                {
                    next = 0;
                    _stressTestDirection = 1;
                }

                try
                {
                    _dgv.FirstDisplayedScrollingRowIndex = next;
                }
                catch { }
            }


            _scrollFrames++;
        }

        private void UpdateHud()
        {
            double ramMb = _perfMonitor.ProcessRamMb;
            int gcGen0 = GC.CollectionCount(0) - _baselineGen0;

            double elapsedSec = _scrollStopwatch.Elapsed.TotalSeconds;
            double fps = (elapsedSec > 0 && _scrollFrames > 0) ? (_scrollFrames / elapsedSec) : 0.0;
            double latencyMs = fps > 0 ? (1000.0 / fps) : 0.0;

            _lblFps.Text = $"FPS: {fps:F0}";
            _lblLatency.Text = $"Latency: {latencyMs:F2} ms";
            _lblRam.Text = $"RAM: {ramMb:F1} MB";
            _lblGc.Text = $"GC Gen0 Delta: {gcGen0}";

            // Color coding
            if (fps >= 55)
            {
                _lblFps.ForeColor = Color.FromArgb(166, 227, 161); // Green
            }
            else if (fps >= 30)
            {
                _lblFps.ForeColor = Color.FromArgb(249, 226, 175); // Yellow
            }
            else
            {
                _lblFps.ForeColor = Color.FromArgb(243, 139, 168); // Red
            }
        }

        private async void HandleExportCsv(object? sender, EventArgs e)
        {
            if (_zeroGrid.DataSource == null || _zeroGrid.RowCount == 0)
            {
                MessageBox.Show("No data available to export!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = $"ZeroUI_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                Cursor = Cursors.WaitCursor;
                _lblStatus.Text = "Exporting CSV...";
                var sw = Stopwatch.StartNew();

                try
                {
                    int count = await ZeroGridExporter.ExportToCsvAsync(_zeroGrid.DataSource, _zeroGrid, sfd.FileName);
                    sw.Stop();

                    double rowsPerSec = count / Math.Max(0.001, sw.Elapsed.TotalSeconds);
                    ZeroToast.Success(this, $"Exported {count:N0} rows successfully ({sw.ElapsedMilliseconds} ms)");
                    MessageBox.Show(
                        $"✅ Successfully exported {count:N0} rows to file:\n{sfd.FileName}\n\nTime elapsed: {sw.ElapsedMilliseconds} ms ({rowsPerSec:N0} rows/sec)",
                        "Export Successful",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    Cursor = Cursors.Default;
                    _lblStatus.Text = $"Exported: {sfd.FileName}";
                }
            }
        }

        private void InitializeComponentsShowcase()
        {
            _tabControls.BackColor = Color.FromArgb(243, 244, 246);

            // Left Panel: Controls showcase
            var leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 460,
                Padding = new Padding(20),
                AutoScroll = true,
                BackColor = Color.White
            };

            // Section 1: Buttons
            var lblBtnTitle = new Label
            {
                Text = "1. ZeroButton (Stateful Flat Buttons & Badges)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                AutoSize = true,
                Location = new Point(16, 16)
            };
                       int btnY = 48;
            var btnPrimary = new ZeroButton { Text = "Primary Action", ButtonStyle = ZeroButtonStyle.Primary, Location = new Point(16, btnY), Size = new Size(130, 36) };
            var btnSuccess = new ZeroButton { Text = "Success", ButtonStyle = ZeroButtonStyle.Success, Location = new Point(156, btnY), Size = new Size(120, 36) };
            var btnDanger = new ZeroButton { Text = "Delete / Danger", ButtonStyle = ZeroButtonStyle.Danger, Location = new Point(286, btnY), Size = new Size(130, 36) };
            leftPanel.Controls.Add(btnPrimary);
            leftPanel.Controls.Add(btnSuccess);
            leftPanel.Controls.Add(btnDanger);

            btnY += 46;
            var btnSecondary = new ZeroButton { Text = "Secondary", ButtonStyle = ZeroButtonStyle.Secondary, Location = new Point(16, btnY), Size = new Size(130, 36) };
            var btnBadge = new ZeroButton { Text = "Notifications", ButtonStyle = ZeroButtonStyle.Primary, BadgeText = "9+", Location = new Point(156, btnY), Size = new Size(140, 36) };
            leftPanel.Controls.Add(btnSecondary);
            leftPanel.Controls.Add(btnBadge);

            // Section 2: Progress Bars
            int progY = btnY + 60;
            var lblProgTitle = new Label
            {
                Text = "2. ZeroProgressBar (Smooth Subpixel Antialiased)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                AutoSize = true,
                Location = new Point(16, progY)
            };
            leftPanel.Controls.Add(lblProgTitle);

            progY += 32;
            var lblDeterminate = new Label { Text = "Determinate Progress (78%):", AutoSize = true, Location = new Point(16, progY), Font = new Font("Segoe UI", 9f) };
            leftPanel.Controls.Add(lblDeterminate);
            progY += 22;
            var prog1 = new ZeroProgressBar { Location = new Point(16, progY), Size = new Size(390, 24), Value = 78, ProgressColor = Color.FromArgb(16, 185, 129) };
            leftPanel.Controls.Add(prog1);

            progY += 34;
            var lblIndeterminate = new Label { Text = "Indeterminate Progress (Marquee 60 FPS):", AutoSize = true, Location = new Point(16, progY), Font = new Font("Segoe UI", 9f) };
            leftPanel.Controls.Add(lblIndeterminate);
            progY += 22;
            var prog2 = new ZeroProgressBar { Location = new Point(16, progY), Size = new Size(390, 24), IsIndeterminate = true, ProgressColor = Color.FromArgb(79, 70, 229) };
            leftPanel.Controls.Add(prog2);

            // Section 3: Search Box
            int searchY = progY + 44;
            var lblSearchTitle = new Label
            {
                Text = "3. ZeroSearchBox (Placeholder & Fast Clear)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                AutoSize = true,
                Location = new Point(16, searchY)
            };
            leftPanel.Controls.Add(lblSearchTitle);

            searchY += 30;
            var searchDemo = new ZeroSearchBox { Location = new Point(16, searchY), Width = 390, PlaceholderText = "🔍 Enter keywords to test..." };
            leftPanel.Controls.Add(searchDemo);

            // Section 4: ZeroSwitch & ZeroTag
            int tagY = searchY + 44;
            var lblTagTitle = new Label
            {
                Text = "4. ZeroSwitch & ZeroTag (State & Toggle Controls)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                AutoSize = true,
                Location = new Point(16, tagY)
            };
            leftPanel.Controls.Add(lblTagTitle);

            tagY += 30;
            var swDemo = new ZeroSwitch { Location = new Point(16, tagY), Checked = true };
            var tag1 = new ZeroTag { Location = new Point(78, tagY), Size = new Size(72, 26), TagType = ZeroTagType.Success, Text = "Active" };
            var tag2 = new ZeroTag { Location = new Point(156, tagY), Size = new Size(80, 26), TagType = ZeroTagType.Processing, Text = "Processing" };
            var tag3 = new ZeroTag { Location = new Point(242, tagY), Size = new Size(72, 26), TagType = ZeroTagType.Warning, Text = "Warning" };
            var tag4 = new ZeroTag { Location = new Point(320, tagY), Size = new Size(60, 26), TagType = ZeroTagType.Error, Text = "Error" };
            swDemo.CheckedChanged += (s, e) =>
            {
                tag1.TagType = swDemo.Checked ? ZeroTagType.Success : ZeroTagType.Default;
                tag1.Text = swDemo.Checked ? "Active" : "Disabled";
            };
            leftPanel.Controls.Add(swDemo);
            leftPanel.Controls.Add(tag1);
            leftPanel.Controls.Add(tag2);
            leftPanel.Controls.Add(tag3);
            leftPanel.Controls.Add(tag4);

            // Section 5: ZeroSegmented (Pill Switcher)
            int segY = tagY + 38;
            var lblSegTitle = new Label
            {
                Text = "5. ZeroSegmented (Pill Switcher)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                AutoSize = true,
                Location = new Point(16, segY)
            };
            leftPanel.Controls.Add(lblSegTitle);

            segY += 30;
            var segDemo = new ZeroSegmented
            {
                Location = new Point(16, segY),
                Width = 390,
                Items = new[] { "All", "Daily", "Weekly", "Monthly" }
            };
            segDemo.SelectedIndexChanged += (s, e) =>
            {
                ZeroToast.Info(this, $"Selected view: {segDemo.SelectedItem}");
            };
            leftPanel.Controls.Add(segDemo);

            // Section 6: ZeroStatistic (KPI Card)
            int statY = segY + 44;
            var lblStatTitle = new Label
            {
                Text = "6. ZeroStatistic (KPI Dashboard Cards)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                AutoSize = true,
                Location = new Point(16, statY)
            };
            leftPanel.Controls.Add(lblStatTitle);

            statY += 30;
            var stat1 = new ZeroStatistic
            {
                Location = new Point(16, statY),
                Size = new Size(190, 95),
                Title = "TOTAL INVENTORY",
                Value = "1,248,500",
                Suffix = "pcs",
                Trend = ZeroTrendDirection.Up,
                TrendText = "+12.4% vs last month"
            };
            var stat2 = new ZeroStatistic
            {
                Location = new Point(216, statY),
                Size = new Size(190, 95),
                Title = "ACTUAL REVENUE",
                Value = "3.85",
                Prefix = "$",
                Suffix = "B",
                Trend = ZeroTrendDirection.Down,
                TrendText = "-2.1% cycle"
            };
            leftPanel.Controls.Add(stat1);
            leftPanel.Controls.Add(stat2);

            // Section 7: ZeroToast (Floating Toasts)
            int toastY = statY + 105;
            var lblToastTitle = new Label
            {
                Text = "7. ZeroToast (Non-blocking Floating Toasts)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                AutoSize = true,
                Location = new Point(16, toastY)
            };
            leftPanel.Controls.Add(lblToastTitle);

            toastY += 30;
            var btnToastSuccess = new ZeroButton { Text = "Toast Success", ButtonStyle = ZeroButtonStyle.Success, Location = new Point(16, toastY), Size = new Size(125, 32), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            btnToastSuccess.Click += (s, e) => ZeroToast.Success(this, "10 million rows synchronized into RAM successfully!");

            var btnToastWarn = new ZeroButton { Text = "Toast Warning", ButtonStyle = ZeroButtonStyle.Secondary, Location = new Point(148, toastY), Size = new Size(120, 32), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            btnToastWarn.Click += (s, e) => ZeroToast.Warning(this, "System detected rapid memory bandwidth consumption.");

            var btnToastError = new ZeroButton { Text = "Toast Error", ButtonStyle = ZeroButtonStyle.Danger, Location = new Point(275, toastY), Size = new Size(115, 32), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            btnToastError.Click += (s, e) => ZeroToast.Error(this, "Database server connection timeout!");

            leftPanel.Controls.Add(btnToastSuccess);
            leftPanel.Controls.Add(btnToastWarn);
            leftPanel.Controls.Add(btnToastError);

            // Right Panel: ZeroListView Log Streamer
            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                BackColor = Color.FromArgb(249, 250, 251)
            };

            var topLogBar = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.Transparent };
            var lblLogTitle = new Label
            {
                Text = "8. ZeroListView (High-Throughput Log Viewer 100K+ Rows)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                AutoSize = true,
                Location = new Point(0, 12)
            };
            topLogBar.Controls.Add(lblLogTitle);

            var btnAdd1000 = new ZeroButton
            {
                Text = "+1,000 Logs",
                ButtonStyle = ZeroButtonStyle.Secondary,
                Size = new Size(110, 30),
                BorderRadius = 4,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(410, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnAdd1000.Click += (s, e) =>
            {
                var batch = new List<LogEntry>(1000);
                for (int i = 0; i < 1000; i++)
                {
                    batch.Add(new LogEntry(DateTime.Now, (LogSeverity)(i % 4), $"[AUTOMATED EVENT #{i + 1}] Inventory batch sync completed successfully."));
                }
                _showcaseLog.AddLogs(batch);
            };
            topLogBar.Controls.Add(btnAdd1000);

            var btnClearLogs = new ZeroButton
            {
                Text = "Clear Logs",
                ButtonStyle = ZeroButtonStyle.Ghost,
                Size = new Size(80, 30),
                BorderRadius = 4,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(526, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClearLogs.Click += (s, e) => _showcaseLog.Clear();
            topLogBar.Controls.Add(btnClearLogs);

            _showcaseLog = new ZeroListView { Dock = DockStyle.Fill };
            _showcaseLog.AddLog(LogSeverity.Info, "ZeroUI system initialized successfully.");
            _showcaseLog.AddLog(LogSeverity.Success, "Connected to Win32 DIBSection Memory DC (32bpp).");
            _showcaseLog.AddLog(LogSeverity.Warning, "High-throughput streaming engine allocated.");
            _showcaseLog.AddLog(LogSeverity.Error, "Default DataGridView load threshold exceeded.");

            rightPanel.Controls.Add(_showcaseLog);
            rightPanel.Controls.Add(topLogBar);

            _tabControls.Controls.Add(rightPanel);
            _tabControls.Controls.Add(leftPanel);

            // Simulation timer: add a log entry every 600ms
            _logGenTimer = new System.Windows.Forms.Timer { Interval = 600 };
            int simCount = 1;
            _logGenTimer.Tick += (s, e) =>
            {
                if (_tabControl.SelectedTab == _tabControls && _showcaseLog.Entries.Count < 50000)
                {
                    var sev = (LogSeverity)(simCount % 4);
                    _showcaseLog.AddLog(sev, $"[Service #{simCount++}] Processed high-throughput transaction via ZeroUI engine with 0.02ms latency.");
                }
            };
            _logGenTimer.Start();
        }

        private void InitializeMesDashboard()
        {
            _tabMes.BackColor = Color.FromArgb(243, 244, 246);
            _tabMes.Padding = new Padding(16);
            _tabMes.AutoScroll = true;

            var mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            // ALERT BANNER: Line Notification
            var alertBanner = new ZeroAlertBanner
            {
                Dock = DockStyle.Top,
                Height = 44,
                Severity = ZeroAlertSeverity.Warning,
                Title = "SMT Feeder Alert",
                Message = "Reel BOA472 is running low (18 remaining). Buffer refill suggested before 14:00.",
                IsClosable = true
            };
            var alertSpacer = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };

            // TOP ACTION TOOLBAR: Scanner & Live Simulator
            var rowToolbar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, 8) };
            
            var statusBadge = new ZeroStatusBadge
            {
                Location = new Point(4, 8),
                Size = new Size(160, 24),
                Status = ZeroStatusType.Running,
                Text = "Line 01: Running"
            };
            rowToolbar.Controls.Add(statusBadge);

            var barcodeBox = new ZeroBarcodeBox
            {
                Location = new Point(175, 4),
                Width = 280,
                PlaceholderText = "Scan Barcode (e.g. SN-1030-88)..."
            };
            rowToolbar.Controls.Add(barcodeBox);

            var mesDatePicker = new ZeroDatePicker
            {
                Location = new Point(465, 4),
                Width = 145,
                Value = DateTime.Today
            };
            mesDatePicker.ValueChanged += (s, e) => ZeroToast.Info(this, $"Filter date set to: {mesDatePicker.Value:yyyy-MM-dd}");
            rowToolbar.Controls.Add(mesDatePicker);

            var btnSimulateScan = new ZeroButton
            {
                Text = "⚡ Giả lập PLC (+5 Lắp ráp, +4 QC, +3 Nhập kho)",
                ButtonStyle = ZeroButtonStyle.Primary,
                Size = new Size(360, 34),
                Location = new Point(620, 4),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            rowToolbar.Controls.Add(btnSimulateScan);


            // ROW 1: Board Info + Shell Info + OEE Gauge
            var row1 = new Panel { Dock = DockStyle.Top, Height = 210, BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, 10) };

            // Card 1A: Board Info (Left Fill)
            var cardBoard = new ZeroCard
            {
                StepNumber = 1,
                BadgeColor = Color.FromArgb(22, 119, 255),
                Title = "Thông tin board (Board Information)",
                Subtitle = "Board sử dụng theo partlist: 026MC02RP2.0",
                ActionText = "Thông tin xuất kho: Theo trạng thái",
                Dock = DockStyle.Fill
            };
            cardBoard.ActionClicked += (s, e) => ZeroToast.Info(this, "Đang mở chi tiết xuất kho theo trạng thái...");

            var gridBoard = new ZeroGridControl
            {
                Dock = DockStyle.Fill,
                Density = GridDensity.Compact,
                HeaderHeight = 26,
                Font = new Font("Segoe UI", 9f)
            };
            gridBoard.Columns.Add(new ZeroColumn("Mã NVL", 140, CellAlignment.Left));
            gridBoard.Columns.Add(new ZeroColumn("SL Partlist", 100, CellAlignment.Right));
            gridBoard.Columns.Add(new ZeroColumn("Tồn kho NVL", 120, CellAlignment.Right));
            gridBoard.Columns.Add(new ZeroColumn("Tồn kho BTP", 120, CellAlignment.Right));
            gridBoard.DataSource = new ZeroUI.Samples.BenchmarkDemo.Data.MesBoardSource();
            cardBoard.ContentPanel.Controls.Add(gridBoard);

            // Splitter 1
            var splitR1A = new Panel { Dock = DockStyle.Right, Width = 10, BackColor = Color.Transparent };

            // Card 1B: Shell Info
            var cardShell = new ZeroCard
            {
                StepNumber = 2,
                BadgeColor = Color.FromArgb(124, 58, 237),
                Title = "Thông tin vỏ (Shell Info)",
                Dock = DockStyle.Right,
                Width = 230
            };
            var descShell = new ZeroDescriptions { Dock = DockStyle.Fill, Columns = 1, RowHeight = 26 };
            descShell.Add("Phiếu YCVT", "Lịch sản xuất", Color.FromArgb(107, 114, 128));
            descShell.Add("Mã phiếu", "(Chưa tạo)", Color.FromArgb(156, 163, 175));
            descShell.Add("Trạng thái", "--", Color.FromArgb(156, 163, 175));
            cardShell.ContentPanel.Controls.Add(descShell);

            // Splitter 2
            var splitR1B = new Panel { Dock = DockStyle.Right, Width = 10, BackColor = Color.Transparent };

            // Card 1C: OEE Gauge Meter
            var cardGauge = new ZeroCard
            {
                StepNumber = null,
                Title = "Chỉ số OEE Chuyền",
                Dock = DockStyle.Right,
                Width = 145
            };
            var gaugeOee = new ZeroGauge
            {
                Dock = DockStyle.Fill,
                Value = 88.5f,
                Title = "Hiệu suất OEE",
                GaugeColor = Color.FromArgb(16, 185, 129)
            };
            cardGauge.ContentPanel.Controls.Add(gaugeOee);

            row1.Controls.Add(cardBoard);
            row1.Controls.Add(splitR1A);
            row1.Controls.Add(cardShell);
            row1.Controls.Add(splitR1B);
            row1.Controls.Add(cardGauge);

            // ROW 2: Production Line Workflow (ZeroSteps)
            var row2 = new Panel { Dock = DockStyle.Top, Height = 125, BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, 10) };


            var cardSteps = new ZeroCard
            {
                StepNumber = 3,
                BadgeColor = Color.FromArgb(22, 119, 255),
                Title = "Thông tin trên chuyền sản xuất (Production Line Workflow)",
                Dock = DockStyle.Fill
            };

            _mesSteps = new ZeroSteps { Dock = DockStyle.Fill };
            _mesSteps.SetSteps(new[]
            {
                new ZeroStepItem { Key = "ASSY", Title = "Thông tin lắp ráp", Quantity = 0, Timestamp = "--", Status = ZeroStepStatus.InProgress, Glyph = ZeroStepGlyph.Gear },
                new ZeroStepItem { Key = "QC", Title = "Thông tin QC", Quantity = 0, Timestamp = "--", Status = ZeroStepStatus.Completed, Glyph = ZeroStepGlyph.Checkmark },
                new ZeroStepItem { Key = "WH", Title = "Số lượng nhập kho", Quantity = 0, Timestamp = "--", Status = ZeroStepStatus.Waiting, Glyph = ZeroStepGlyph.Warehouse }
            });

            _mesSteps.StepClicked += (s, e) =>
            {
                ZeroToast.Info(this, $"Đã chọn công đoạn: {e.Step.Title} | Sản lượng hiện tại: {e.Step.Quantity:N0}");
            };

            cardSteps.ContentPanel.Controls.Add(_mesSteps);
            row2.Controls.Add(cardSteps);

            // ROW 3: Summary + Product Specs + Timeline Lot Tracking
            var row3 = new Panel { Dock = DockStyle.Top, Height = 180, BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, 10) };

            // Card 3A: Summary
            var cardSummary = new ZeroCard
            {
                StepNumber = 4,
                BadgeColor = Color.FromArgb(22, 119, 255),
                Title = "Thông tin tổng hợp (Summary)",
                Dock = DockStyle.Left,
                Width = 340
            };
            var descSummary = new ZeroDescriptions { Dock = DockStyle.Fill, Columns = 1, RowHeight = 28 };
            descSummary.Add("Số lượng KH / thực nhập", "100 / 0", Color.FromArgb(17, 24, 39));
            descSummary.Add("Trễ hạn", "Không", Color.FromArgb(22, 163, 74), isHighlighted: true);
            descSummary.Add("Nguyên nhân", "--", Color.FromArgb(107, 114, 128));
            cardSummary.ContentPanel.Controls.Add(descSummary);

            var splitR3A = new Panel { Dock = DockStyle.Left, Width = 10, BackColor = Color.Transparent };

            // Card 3B: Product Specifications
            var cardProduct = new ZeroCard
            {
                StepNumber = null,
                Title = "Thông tin sản phẩm (Specs)",
                Dock = DockStyle.Left,
                Width = 320
            };
            var descProduct = new ZeroDescriptions { Dock = DockStyle.Fill, Columns = 1, RowHeight = 28 };
            descProduct.Add("Mã sản phẩm", "1030MAX001", Color.FromArgb(17, 24, 39));
            descProduct.Add("Tên sản phẩm", "B1030 MAX", Color.FromArgb(17, 24, 39));
            descProduct.Add("BOM / Partlist", "026MC02RP2.0", Color.FromArgb(79, 70, 229));
            cardProduct.ContentPanel.Controls.Add(descProduct);

            var splitR3B = new Panel { Dock = DockStyle.Left, Width = 10, BackColor = Color.Transparent };

            // Card 3C: Vertical Lot Tracking Timeline
            var cardTimeline = new ZeroCard
            {
                StepNumber = null,
                Title = "Nhật ký phả hệ lô hàng (Lot Traceability)",
                Dock = DockStyle.Fill
            };
            var timeline = new ZeroTimeline { Dock = DockStyle.Fill, ItemSpacing = 40 };
            timeline.Add("Nhập kho NVL", "07:30", "Lô BOA437 & BOA541 kiểm duyệt OQC", ZeroTimelineStatus.Completed);
            timeline.Add("Cấp phát SMT", "08:15", "Gắp 420 chip lên bo mạch", ZeroTimelineStatus.Completed);
            timeline.Add("Hàn sóng & Lắp ráp", "09:40", "Đang lắp ráp chuyền 01", ZeroTimelineStatus.InProgress);
            cardTimeline.ContentPanel.Controls.Add(timeline);

            row3.Controls.Add(cardTimeline);
            row3.Controls.Add(splitR3B);
            row3.Controls.Add(cardProduct);
            row3.Controls.Add(splitR3A);
            row3.Controls.Add(cardSummary);

            // Simulation Logic
            int simAssy = 0;
            int simQc = 0;
            int simWh = 0;

            ZeroSevenSegment? segOutput = null;
            ZeroLinearGauge? gaugePressure = null;

            void ProcessScan(string barcode)
            {
                simAssy += 1;
                if (simAssy >= 2) simQc += 1;
                if (simQc >= 2) simWh += 1;

                string now = DateTime.Now.ToString("HH:mm:ss");
                _mesSteps.UpdateStep("ASSY", simAssy, now, ZeroStepStatus.Completed);
                _mesSteps.UpdateStep("QC", simQc, now, ZeroStepStatus.InProgress);
                _mesSteps.UpdateStep("WH", simWh, now, simWh > 0 ? ZeroStepStatus.InProgress : ZeroStepStatus.Waiting);

                descSummary.SetValue("Số lượng KH / thực nhập", $"100 / {simWh}", Color.FromArgb(17, 24, 39));
                timeline.Add($"Barcode {barcode}", now, "Quét mã trạm thành công", ZeroTimelineStatus.Completed);
                if (segOutput != null) segOutput.Value = (1420 + simWh).ToString("D6");
                if (gaugePressure != null) gaugePressure.Value = 72.5f + (simWh % 12);
                ZeroToast.Success(this, $"Scanned: {barcode} | Lắp ráp: {simAssy}, Nhập kho: {simWh}");
            }

            barcodeBox.BarcodeScanned += (s, e) => ProcessScan(e.Barcode);

            btnSimulateScan.Click += (s, e) =>
            {
                simAssy += 5;
                if (simAssy >= 5) simQc += 4;
                if (simQc >= 4) simWh += 3;

                string now = DateTime.Now.ToString("HH:mm:ss");
                _mesSteps.UpdateStep("ASSY", simAssy, now, ZeroStepStatus.Completed);
                _mesSteps.UpdateStep("QC", simQc, now, ZeroStepStatus.InProgress);
                _mesSteps.UpdateStep("WH", simWh, now, simWh > 0 ? ZeroStepStatus.InProgress : ZeroStepStatus.Waiting);

                descSummary.SetValue("Số lượng KH / thực nhập", $"100 / {simWh}", Color.FromArgb(17, 24, 39));
                timeline.Add("PLC Signal Batch", now, $"Đồng bộ lô sản xuất (+{simWh} sp)", ZeroTimelineStatus.Completed);
                if (segOutput != null) segOutput.Value = (1420 + simWh).ToString("D6");
                if (gaugePressure != null) gaugePressure.Value = 72.5f + (simWh % 12);
                ZeroToast.Success(this, $"PLC Signal: Lắp ráp: {simAssy}, QC: {simQc}, Nhập kho: {simWh}");
            };


            // ROW 4: SCADA Telemetry & Industrial Andon Control
            var row4 = new Panel { Dock = DockStyle.Top, Height = 175, BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, 10) };

            // Card 4A: Andon Signal Tower Light
            var cardAndon = new ZeroCard
            {
                StepNumber = null,
                Title = "Đèn Tháp Andon (SCADA Signal)",
                Dock = DockStyle.Left,
                Width = 280
            };

            var andonTower = new ZeroLedTower
            {
                Location = new Point(14, 6),
                Size = new Size(54, 120),
                RedLight = LedState.Off,
                AmberLight = LedState.Off,
                GreenLight = LedState.On,
                BlueLight = LedState.Off
            };
            cardAndon.ContentPanel.Controls.Add(andonTower);

            var btnAndonRun = new ZeroButton
            {
                Text = "▶ Running",
                ButtonStyle = ZeroButtonStyle.Success,
                Location = new Point(80, 8),
                Size = new Size(130, 26)
            };
            btnAndonRun.Click += (s, e) =>
            {
                andonTower.SetStatus(running: true, warning: false, alarm: false);
                statusBadge.Status = ZeroStatusType.Running;
                statusBadge.Text = "Line 01: Running";
                ZeroToast.Success(this, "SCADA: Chuyền chuyển RUNNING (Đèn xanh sáng)");
            };
            cardAndon.ContentPanel.Controls.Add(btnAndonRun);

            var btnAndonWarn = new ZeroButton
            {
                Text = "⚠ Warning",
                ButtonStyle = ZeroButtonStyle.Secondary,
                Location = new Point(80, 38),
                Size = new Size(130, 26)
            };
            btnAndonWarn.Click += (s, e) =>
            {
                andonTower.SetStatus(running: false, warning: true, alarm: false);
                statusBadge.Status = ZeroStatusType.Idle;
                statusBadge.Text = "Line 01: Low Feeder Alert";
                ZeroToast.Warning(this, "SCADA: Cảnh báo Feeder SMT (Đèn vàng sáng)");
            };
            cardAndon.ContentPanel.Controls.Add(btnAndonWarn);

            var btnAndonAlarm = new ZeroButton
            {
                Text = "⛔ E-Stop Alarm",
                ButtonStyle = ZeroButtonStyle.Danger,
                Location = new Point(80, 68),
                Size = new Size(130, 26)
            };
            btnAndonAlarm.Click += (s, e) =>
            {
                andonTower.SetStatus(running: false, warning: false, alarm: true);
                statusBadge.Status = ZeroStatusType.Alarm;
                statusBadge.Text = "Line 01: EMERGENCY STOP";
                ZeroToast.Error(this, "SCADA: Dừng khẩn cấp E-STOP! Đèn đỏ Andon nhấp nháy 2Hz!");
            };
            cardAndon.ContentPanel.Controls.Add(btnAndonAlarm);

            var splitR4A = new Panel { Dock = DockStyle.Left, Width = 10, BackColor = Color.Transparent };

            // Card 4B: Industrial 7-Segment Digital Readouts
            var cardLed = new ZeroCard
            {
                StepNumber = null,
                Title = "Bảng Số LED 7 Đoạn (Takt & Output)",
                Dock = DockStyle.Left,
                Width = 340
            };

            var lblTakt = new Label { Text = "Takt Time Mục Tiêu (giây):", Location = new Point(10, 4), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            var segTakt = new ZeroSevenSegment
            {
                Location = new Point(10, 20),
                Size = new Size(305, 34),
                DigitCount = 6,
                Value = "00:28",
                SegmentColor = Color.FromArgb(56, 189, 248) // Neon Cyan
            };

            var lblActual = new Label { Text = "Sản Lượng Thực Tế Lô (sp):", Location = new Point(10, 58), AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            segOutput = new ZeroSevenSegment
            {
                Location = new Point(10, 74),
                Size = new Size(305, 34),
                DigitCount = 6,
                Value = "001420",
                SegmentColor = Color.FromArgb(52, 211, 153) // Neon Emerald
            };

            cardLed.ContentPanel.Controls.Add(lblTakt);
            cardLed.ContentPanel.Controls.Add(segTakt);
            cardLed.ContentPanel.Controls.Add(lblActual);
            cardLed.ContentPanel.Controls.Add(segOutput);

            var splitR4B = new Panel { Dock = DockStyle.Left, Width = 10, BackColor = Color.Transparent };

            // Card 4C: Linear Gauges (Hydraulic Pressure & Oven Temp)
            var cardSensors = new ZeroCard
            {
                StepNumber = null,
                Title = "Giám Sát Áp Suất & Nhiệt Độ (SCADA Telemetry)",
                Dock = DockStyle.Fill
            };

            gaugePressure = new ZeroLinearGauge
            {
                Location = new Point(12, 4),
                Size = new Size(260, 50),
                Title = "Áp Suất Thủy Lực Ép",

                Unit = "Bar",
                Minimum = 0,
                Maximum = 120,
                Value = 72.5f,
                WarningThreshold = 85,
                CriticalThreshold = 105
            };

            var gaugeTemp = new ZeroLinearGauge
            {
                Location = new Point(285, 4),
                Size = new Size(260, 50),
                Title = "Nhiệt Độ Lò Hàn SMT (Zone 3)",
                Unit = "°C",
                Minimum = 50,
                Maximum = 300,
                Value = 245.0f,
                WarningThreshold = 265,
                CriticalThreshold = 285
            };

            cardSensors.ContentPanel.Controls.Add(gaugePressure);
            cardSensors.ContentPanel.Controls.Add(gaugeTemp);

            row4.Controls.Add(cardSensors);
            row4.Controls.Add(splitR4B);
            row4.Controls.Add(cardLed);
            row4.Controls.Add(splitR4A);
            row4.Controls.Add(cardAndon);

            // Assemble top-to-bottom layout
            mainContainer.Controls.Add(row4);
            mainContainer.Controls.Add(row3);
            mainContainer.Controls.Add(row2);
            mainContainer.Controls.Add(row1);
            mainContainer.Controls.Add(rowToolbar);
            mainContainer.Controls.Add(alertSpacer);
            mainContainer.Controls.Add(alertBanner);

            alertBanner.BringToFront();
            alertSpacer.BringToFront();
            rowToolbar.BringToFront();
            row1.BringToFront();
            row2.BringToFront();
            row3.BringToFront();
            row4.BringToFront();

            _tabMes.Controls.Add(mainContainer);
        }

        private void InitializeScadaHub()
        {
            var mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(16)
            };

            // 0. Alert / Status Banner
            var banner = new ZeroAlertBanner
            {
                Dock = DockStyle.Top,
                Severity = ZeroAlertSeverity.Info,
                Title = "🔬 SCADA & Smart Factory Hub — Trạm Giám Sát & Điều Hành Thời Gian Thực",
                Message = "Tích hợp biểu đồ xung sóng 60 FPS (ZeroTrendChart), nhịp chuyền Lean (ZeroTaktTimer), ma trận lỗi quang học AOI (ZeroDefectMatrix), thanh ghi I/O PLC (ZeroPlcIoMonitor) và bàn phím Andon SLA (ZeroAndonCallPad).",
                Height = 62
            };
            var bannerSpacer = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent };

            // ROW 1: Real-time Oscilloscope (TrendChart) + Lean Takt Countdown Ring (TaktTimer)
            var row1 = new Panel { Dock = DockStyle.Top, Height = 220, BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, 10) };

            // Card 1A: Trend Chart
            var cardTrend = new ZeroCard
            {
                StepNumber = null,
                Title = "Biểu Đồ Sóng Cảm Biến Thời Gian Thực (60 FPS Zero-Alloc)",
                Dock = DockStyle.Left,
                Width = 560
            };

            var trendChart = new ZeroTrendChart
            {
                Dock = DockStyle.Fill,
                Title = "Ch1: Áp Suất Buồng Ép (Bar) | Ch2: Nhiệt Độ Lò Nung (°C)",
                UpperLimit = 85f,
                LowerLimit = 15f
            };
            cardTrend.ContentPanel.Controls.Add(trendChart);

            var trendToolbar = new Panel { Dock = DockStyle.Bottom, Height = 32, BackColor = Color.FromArgb(15, 23, 42), Padding = new Padding(6, 4, 6, 4) };
            var btnSpike = new ZeroButton
            {
                Text = "⚡ Tạo Xung Quá Áp (Inject Spike)",
                ButtonStyle = ZeroButtonStyle.Danger,
                Dock = DockStyle.Left,
                Width = 200
            };
            var btnPauseTrend = new ZeroButton
            {
                Text = "⏸ Dừng/Chạy Stream",
                ButtonStyle = ZeroButtonStyle.Secondary,
                Dock = DockStyle.Left,
                Width = 140
            };
            trendToolbar.Controls.Add(btnPauseTrend);
            trendToolbar.Controls.Add(btnSpike);
            cardTrend.ContentPanel.Controls.Add(trendToolbar);

            var split1 = new Panel { Dock = DockStyle.Left, Width = 12, BackColor = Color.Transparent };

            // Card 1B: Takt Timer
            var cardTakt = new ZeroCard
            {
                StepNumber = null,
                Title = "Chu Kỳ Nhịp Chuyền Takt Time (Assembly Line)",
                Dock = DockStyle.Fill
            };

            var taktTimer = new ZeroTaktTimer
            {
                Dock = DockStyle.Left,
                Width = 180,
                TargetTaktSeconds = 25f,
                AverageCycleTime = 23.8f,
                CompletedUnits = 186
            };
            cardTakt.ContentPanel.Controls.Add(taktTimer);

            var taktControls = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 20, 10, 10) };
            var btnCompleteUnit = new ZeroButton
            {
                Text = "✅ Hoàn Tất 1 Sản Phẩm (+1 Output)",
                ButtonStyle = ZeroButtonStyle.Success,
                Dock = DockStyle.Top,
                Height = 36
            };
            var taktSpacer1 = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };
            var btnResetTakt = new ZeroButton
            {
                Text = "🔄 Đặt Lại Chu Kỳ (Reset Takt)",
                ButtonStyle = ZeroButtonStyle.Secondary,
                Dock = DockStyle.Top,
                Height = 34
            };
            taktControls.Controls.Add(btnResetTakt);
            taktControls.Controls.Add(taktSpacer1);
            taktControls.Controls.Add(btnCompleteUnit);
            cardTakt.ContentPanel.Controls.Add(taktControls);

            row1.Controls.Add(cardTakt);
            row1.Controls.Add(split1);
            row1.Controls.Add(cardTrend);

            // ROW 2: AOI Defect Matrix (DefectMatrix) + PLC I/O Registers (PlcIoMonitor)
            var row2 = new Panel { Dock = DockStyle.Top, Height = 220, BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, 10) };

            // Card 2A: Defect Matrix
            var cardDefect = new ZeroCard
            {
                StepNumber = null,
                Title = "Bản Đồ Kiểm Tra Ngoại Quan AOI SMT (Array 3x6)",
                Dock = DockStyle.Left,
                Width = 520
            };

            var defectMatrix = new ZeroDefectMatrix
            {
                Dock = DockStyle.Fill,
                Title = "SMT Carrier Panel #SN-94812 — AOI Camera Trạm 03"
            };
            cardDefect.ContentPanel.Controls.Add(defectMatrix);

            var defectToolbar = new Panel { Dock = DockStyle.Bottom, Height = 32, BackColor = Color.FromArgb(15, 23, 42), Padding = new Padding(6, 4, 6, 4) };
            var btnSimDefect = new ZeroButton
            {
                Text = "🔍 Giả Lập Lỗi Hàn (Simulate Defect)",
                ButtonStyle = ZeroButtonStyle.Danger,
                Dock = DockStyle.Left,
                Width = 200
            };
            var btnClearPass = new ZeroButton
            {
                Text = "✅ Tất Cả Đạt (All Pass)",
                ButtonStyle = ZeroButtonStyle.Success,
                Dock = DockStyle.Left,
                Width = 140
            };
            defectToolbar.Controls.Add(btnClearPass);
            defectToolbar.Controls.Add(btnSimDefect);
            cardDefect.ContentPanel.Controls.Add(defectToolbar);

            var split2 = new Panel { Dock = DockStyle.Left, Width = 12, BackColor = Color.Transparent };

            // Card 2B: PLC I/O Monitor
            var cardPlc = new ZeroCard
            {
                StepNumber = null,
                Title = "Bảng Giám Sát Bit I/O PLC (Click DO để Force)",
                Dock = DockStyle.Fill
            };

            var plcMonitor = new ZeroPlcIoMonitor
            {
                Dock = DockStyle.Fill,
                DigitalInputs = 0x00A5,
                DigitalOutputs = 0x000F,
                AllowSimulationClick = true
            };
            cardPlc.ContentPanel.Controls.Add(plcMonitor);

            row2.Controls.Add(cardPlc);
            row2.Controls.Add(split2);
            row2.Controls.Add(cardDefect);

            // ROW 3: Shopfloor Andon Call Pad + Station Event Journal
            var row3 = new Panel { Dock = DockStyle.Top, Height = 200, BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, 10) };

            // Card 3A: Andon Call Pad
            var cardAndon = new ZeroCard
            {
                StepNumber = null,
                Title = "Bàn Phím Cảm Ứng Gọi Hỗ Trợ Andon Trạm (Touch SLA Pad)",
                Dock = DockStyle.Left,
                Width = 480
            };

            var andonPad = new ZeroAndonCallPad
            {
                Dock = DockStyle.Fill
            };
            cardAndon.ContentPanel.Controls.Add(andonPad);

            var split3 = new Panel { Dock = DockStyle.Left, Width = 12, BackColor = Color.Transparent };

            // Card 3B: Station Event Journal
            var cardLog = new ZeroCard
            {
                StepNumber = null,
                Title = "Nhật Ký Sự Kiện Trạm SCADA (Real-Time Audit Log)",
                Dock = DockStyle.Fill
            };

            var scadaLog = new ZeroListView
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f)
            };
            cardLog.ContentPanel.Controls.Add(scadaLog);

            row3.Controls.Add(cardLog);
            row3.Controls.Add(split3);
            row3.Controls.Add(cardAndon);

            // Seed initial log entries
            scadaLog.AddLog(DateTime.Now.AddMinutes(-12), LogSeverity.Info, "Trạm SCADA khởi động: Kết nối PLC Siemens S7-1500 (IP 192.168.1.10) thành công.");
            scadaLog.AddLog(DateTime.Now.AddMinutes(-8), LogSeverity.Success, "AOI Inspection: Nạp mẫu kiểm tra Panel SMT 3x6 (Thư viện chuẩn IPC-A-610G).");
            scadaLog.AddLog(DateTime.Now.AddMinutes(-5), LogSeverity.Info, "Lean Takt: Chu kỳ nhịp chuyền thiết lập chuẩn 25.0s.");

            // Wire events
            taktTimer.TaktCompleted += (s, e) =>
            {
                scadaLog.AddLog(DateTime.Now, LogSeverity.Success, $"Takt Timer: Hoàn tất 1 sản phẩm. Tổng sản lượng ca: {taktTimer.CompletedUnits} PCS.");
            };

            taktTimer.TaktOverdue += (s, e) =>
            {
                scadaLog.AddLog(DateTime.Now, LogSeverity.Warning, "Takt Timer: CẢNH BÁO TRỄ NHỊP CHUYỀN (>25s)!");
                ZeroToast.Warning(this, "⚠ Cảnh báo: Chuyền sản xuất đang vượt Takt Time quy định!");
            };

            btnCompleteUnit.Click += (s, e) =>
            {
                taktTimer.CompleteUnit();
                ZeroToast.Success(this, $"Đã xác nhận hoàn thành SP #{taktTimer.CompletedUnits}!");
            };

            btnResetTakt.Click += (s, e) =>
            {
                taktTimer.Reset();
                scadaLog.AddLog(DateTime.Now, LogSeverity.Info, "Takt Timer: Đã reset chu kỳ về 0.0s.");
            };

            defectMatrix.SlotClicked += (s, e) =>
            {
                var slot = e.Slot;
                string msg = $"[AOI Drill-Down] Vị trí: {slot.Code} | Trạng thái: {slot.Status} | Chi tiết: {slot.DefectDetail}";
                var sev = slot.Status == DefectStatus.Fail ? LogSeverity.Error : (slot.Status == DefectStatus.Warning ? LogSeverity.Warning : LogSeverity.Success);
                scadaLog.AddLog(DateTime.Now, sev, msg);
                if (slot.Status == DefectStatus.Fail)
                    ZeroToast.Error(this, $"Phát hiện lỗi tại {slot.Code}: {slot.DefectDetail}");
                else
                    ZeroToast.Info(this, $"Chi tiết {slot.Code}: {slot.DefectDetail}");
            };

            btnSimDefect.Click += (s, e) =>
            {
                defectMatrix.SetSlotStatus(2, 1, DefectStatus.Fail, "Lệch chân tụ IC (Tombstone C18)");
                scadaLog.AddLog(DateTime.Now, LogSeverity.Error, "AOI Inspection: Phát hiện lỗi ngoại quan tại vị trí U14 (Tombstone C18)!");
                ZeroToast.Error(this, "AOI: Phát hiện linh kiện lệch chân tại U14!");
            };

            btnClearPass.Click += (s, e) =>
            {
                for (int r = 0; r < defectMatrix.Rows; r++)
                    for (int c = 0; c < defectMatrix.Columns; c++)
                        defectMatrix.SetSlotStatus(r, c, DefectStatus.Pass, "OK");
                scadaLog.AddLog(DateTime.Now, LogSeverity.Success, "AOI Inspection: Toàn bộ panel đã đạt chuẩn Pass 100%.");
                ZeroToast.Success(this, "Panel AOI: Đạt chuẩn 100%!");
            };

            plcMonitor.OutputCoilChanged += (s, e) =>
            {
                string state = e.NewState ? "BẬT (HIGH - 1)" : "TẮT (LOW - 0)";
                scadaLog.AddLog(DateTime.Now, LogSeverity.Info, $"PLC Coil: Ép trạng thái ngõ ra DO_{e.BitIndex:D2} sang {state}.");
                ZeroToast.Info(this, $"PLC DO_{e.BitIndex:D2} = {(e.NewState ? 1 : 0)}");
            };

            andonPad.CallTriggered += (s, e) =>
            {
                if (e.IsActive)
                {
                    scadaLog.AddLog(DateTime.Now, LogSeverity.Warning, $"ANDON CALL: Yêu cầu khẩn cấp [{e.CallType}] được kích hoạt tại trạm!");
                    ZeroToast.Warning(this, $"🚨 ANDON: Đã phát tín hiệu gọi [{e.CallType}]!");
                }
                else
                {
                    scadaLog.AddLog(DateTime.Now, LogSeverity.Success, $"ANDON CALL: Yêu cầu [{e.CallType}] đã được xử lý và giải tỏa.");
                    ZeroToast.Success(this, $"Andon: Đã đóng yêu cầu [{e.CallType}].");
                }
            };

            bool isStreaming = true;
            btnPauseTrend.Click += (s, e) =>
            {
                isStreaming = !isStreaming;
                btnPauseTrend.Text = isStreaming ? "⏸ Dừng Stream" : "▶ Tiếp tục Stream";
            };

            btnSpike.Click += (s, e) =>
            {
                trendChart.AddPoint(0, 94.2f);
                scadaLog.AddLog(DateTime.Now, LogSeverity.Error, "SCADA Sensor: Cảnh báo áp suất vượt ngưỡng an toàn USL (94.2 Bar > 85.0 Bar)!");
                ZeroToast.Error(this, "⚠ Quá áp buồng ép: 94.2 Bar!");
            };


            // Live Simulation Timer for Telemetry and PLC
            float simTime = 0f;
            var rand = new Random();

            _scadaSimTimer = new System.Windows.Forms.Timer { Interval = 80 };
            _scadaSimTimer.Tick += (s, e) =>
            {
                if (!isStreaming) return;
                simTime += 0.1f;

                // Simulated pressure: 60 + 14 * sin(t) + jitter
                float p = 60f + (14f * (float)Math.Sin(simTime)) + (float)(rand.NextDouble() * 3.0 - 1.5);
                // Simulated temp: 220 + 8 * cos(t*0.4) + jitter
                float t = 220f + (8f * (float)Math.Cos(simTime * 0.4f)) + (float)(rand.NextDouble() * 2.0 - 1.0);

                trendChart.AddPoint(0, p);
                trendChart.AddPoint(1, t);

                // Pulse DI bit 0 (Photocell) every ~2.5 seconds
                if ((int)(simTime * 10) % 25 == 0)
                {
                    bool cur = (plcMonitor.DigitalInputs & 0x0001) != 0;
                    plcMonitor.SetInputBit(0, !cur);
                }
            };
            _scadaSimTimer.Start();

            // Assemble top-to-bottom layout
            mainContainer.Controls.Add(row3);
            mainContainer.Controls.Add(row2);
            mainContainer.Controls.Add(row1);
            mainContainer.Controls.Add(bannerSpacer);
            mainContainer.Controls.Add(banner);

            banner.BringToFront();
            bannerSpacer.BringToFront();
            row1.BringToFront();
            row2.BringToFront();
            row3.BringToFront();

            _tabScada.Controls.Add(mainContainer);
        }

        private void InitializeWmsCenter()
        {
            var mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(16)
            };

            // 0. Alert Banner
            var banner = new ZeroAlertBanner
            {
                Dock = DockStyle.Top,
                Severity = ZeroAlertSeverity.Success,
                Title = "📦 WMS & Quality Inspection Center — Kho Thông Minh & Kiểm Soát Chất Lượng Six Sigma",
                Message = "Tích hợp bản đồ kệ kho linh kiện 2D (ZeroWarehouseRack), bồn chứa dung môi 3D (ZeroTank3D), biểu đồ kiểm soát thống kê SPC X-Bar (ZeroSpcChart) và bảng thẻ Kanban điện tử (ZeroKanbanBoard).",
                Height = 62
            };
            var bannerSpacer = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent };

            // ROW 1: Smart Warehouse Storage Rack (WarehouseRack) + Industrial 3D Fluid Tank (Tank3D)
            var row1 = new Panel { Dock = DockStyle.Top, Height = 280, BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, 10) };

            // Card 1A: Warehouse Rack
            var cardRack = new ZeroCard
            {
                StepNumber = null,
                Title = "Sơ Đồ Kệ Kho Linh Kiện SMT — Dãy A (Bay 01..05 x Level 01..04)",
                Dock = DockStyle.Left,
                Width = 560
            };

            var rack = new ZeroWarehouseRack
            {
                Dock = DockStyle.Fill,
                Bays = 5,
                Levels = 4,
                RackTitle = "Kệ Chứa Cuộn SMT A (A-01-01 đến A-05-04)"
            };
            cardRack.ContentPanel.Controls.Add(rack);

            var rackToolbar = new Panel { Dock = DockStyle.Bottom, Height = 32, BackColor = Color.FromArgb(15, 23, 42), Padding = new Padding(6, 4, 6, 4) };
            var btnAddReel = new ZeroButton
            {
                Text = "📦 Cấp Cuộn SMT Vào Vị Trí A-02-03",
                ButtonStyle = ZeroButtonStyle.Success,
                Dock = DockStyle.Left,
                Width = 230
            };
            var btnLockQc = new ZeroButton
            {
                Text = "🔒 Khóa Lô Cách Ly QC",
                ButtonStyle = ZeroButtonStyle.Danger,
                Dock = DockStyle.Left,
                Width = 160
            };
            rackToolbar.Controls.Add(btnLockQc);
            rackToolbar.Controls.Add(btnAddReel);
            cardRack.ContentPanel.Controls.Add(rackToolbar);

            var split1 = new Panel { Dock = DockStyle.Left, Width = 12, BackColor = Color.Transparent };

            // Card 1B: Industrial 3D Tank
            var cardTank = new ZeroCard
            {
                StepNumber = null,
                Title = "Bồn Chứa Dung Môi SMT-TK01 (Capacity 10,000L)",
                Dock = DockStyle.Fill
            };

            var tank = new ZeroTank3D
            {
                Dock = DockStyle.Left,
                Width = 190,
                CapacityLiters = 10000f,
                CurrentLevelLiters = 6850f,
                TankName = "Bồn IPA SMT-TK01",
                FluidName = "Dung môi tẩy rửa 99.7%"
            };
            cardTank.ContentPanel.Controls.Add(tank);

            var tankControls = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 30, 10, 10) };
            var btnPumpIn = new ZeroButton
            {
                Text = "🔼 Bơm Nạp (+1,000L)",
                ButtonStyle = ZeroButtonStyle.Primary,
                Dock = DockStyle.Top,
                Height = 36
            };
            var tankSpacer1 = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };
            var btnDrainOut = new ZeroButton
            {
                Text = "🔽 Xả Đáy (-1,000L)",
                ButtonStyle = ZeroButtonStyle.Secondary,
                Dock = DockStyle.Top,
                Height = 36
            };
            tankControls.Controls.Add(btnDrainOut);
            tankControls.Controls.Add(tankSpacer1);
            tankControls.Controls.Add(btnPumpIn);
            cardTank.ContentPanel.Controls.Add(tankControls);

            row1.Controls.Add(cardTank);
            row1.Controls.Add(split1);
            row1.Controls.Add(cardRack);

            // ROW 2: Statistical Process Control (SpcChart) + Electronic Kanban Board (KanbanBoard)
            var row2 = new Panel { Dock = DockStyle.Top, Height = 280, BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, 10) };

            // Card 2A: SPC Chart
            var cardSpc = new ZeroCard
            {
                StepNumber = null,
                Title = "Biểu Đồ Thống Kê Đo Kiểm SPC X-Bar (Dung Sai Phay CNC)",
                Dock = DockStyle.Left,
                Width = 560
            };

            var spcChart = new ZeroSpcChart
            {
                Dock = DockStyle.Fill,
                NominalTarget = 12.000f,
                USL = 12.020f,
                LSL = 11.980f,
                Title = "SPC X-Bar: Đường kính trục Pin định vị CNC (12.000 ± 0.020 mm)"
            };
            cardSpc.ContentPanel.Controls.Add(spcChart);

            var spcToolbar = new Panel { Dock = DockStyle.Bottom, Height = 32, BackColor = Color.FromArgb(15, 23, 42), Padding = new Padding(6, 4, 6, 4) };
            var btnAddSample = new ZeroButton
            {
                Text = "📏 Đo Mẫu Mới (Thêm Subgroup)",
                ButtonStyle = ZeroButtonStyle.Success,
                Dock = DockStyle.Left,
                Width = 200
            };
            var btnSpikeSpc = new ZeroButton
            {
                Text = "⚠ Giả Lập Lỗi Vượt 3-Sigma",
                ButtonStyle = ZeroButtonStyle.Danger,
                Dock = DockStyle.Left,
                Width = 190
            };
            spcToolbar.Controls.Add(btnSpikeSpc);
            spcToolbar.Controls.Add(btnAddSample);
            cardSpc.ContentPanel.Controls.Add(spcToolbar);

            var split2 = new Panel { Dock = DockStyle.Left, Width = 12, BackColor = Color.Transparent };

            // Card 2B: Kanban Board
            var cardKanban = new ZeroCard
            {
                StepNumber = null,
                Title = "Bảng Thẻ Kanban Điện Tử Điều Phối Chuyền SMT (WIP Limit)",
                Dock = DockStyle.Fill
            };

            var kanban = new ZeroKanbanBoard
            {
                Dock = DockStyle.Fill
            };
            cardKanban.ContentPanel.Controls.Add(kanban);

            row2.Controls.Add(cardKanban);
            row2.Controls.Add(split2);
            row2.Controls.Add(cardSpc);

            // Wire Events
            rack.BinClicked += (s, e) =>
            {
                var b = e.Bin;
                string info = $"Vị trí {b.BinCode}: {b.Status} | SKU: {(string.IsNullOrEmpty(b.Sku) ? "(Trống)" : b.Sku)} | Tồn: {b.CurrentQty} PCS | Lô: {b.LotNumber}";
                if (b.Status == BinOccupancyStatus.Quarantine)
                    ZeroToast.Error(this, $"[CẢNH BÁO KHÓA QC] {info}");
                else if (b.Status == BinOccupancyStatus.Full)
                    ZeroToast.Success(this, $"[VỊ TRÍ ĐẦY] {info}");
                else
                    ZeroToast.Info(this, $"[CHI TIẾT VỊ TRÍ] {info}");
            };

            btnAddReel.Click += (s, e) =>
            {
                rack.SetBin(3, 2, BinOccupancyStatus.Full, "IC-MCU-STM32", "STM32F407VGT6", "LOT-20260903-NEW", 2000);
                ZeroToast.Success(this, "Đã nhập thêm 2,000 PCS STM32 vào ngăn A-02-03!");
            };

            btnLockQc.Click += (s, e) =>
            {
                rack.SetBin(2, 4, BinOccupancyStatus.Quarantine, "SMD-CAP-0805", "Tụ gốm 10uF", "LOT-20260815-HOLD", 800);
                ZeroToast.Error(this, "Đã khóa cách ly vị trí A-04-02 chờ phòng QA tái kiểm định!");
            };

            btnPumpIn.Click += (s, e) =>
            {
                tank.CurrentLevelLiters = Math.Min(tank.CapacityLiters, tank.CurrentLevelLiters + 1000f);
                if (tank.AlarmState == TankAlarmState.HighOverflow)
                    ZeroToast.Error(this, $"CẢNH BÁO TRÀN: Mức bồn đạt {tank.Percentage:F1}% (>90%)!");
                else
                    ZeroToast.Success(this, $"Bơm nạp thành công: {tank.CurrentLevelLiters:N0} L ({tank.Percentage:F1}%)");
            };

            btnDrainOut.Click += (s, e) =>
            {
                tank.CurrentLevelLiters = Math.Max(0f, tank.CurrentLevelLiters - 1000f);
                if (tank.AlarmState == TankAlarmState.LowLevel)
                    ZeroToast.Warning(this, $"CẢNH BÁO CẠN BỒN: Mức bồn còn {tank.Percentage:F1}% (<15%)!");
                else
                    ZeroToast.Info(this, $"Xả bồn thành công: {tank.CurrentLevelLiters:N0} L ({tank.Percentage:F1}%)");
            };

            var rand = new Random();
            btnAddSample.Click += (s, e) =>
            {
                float val = 12.000f + (float)(rand.NextDouble() * 0.010 - 0.005);
                spcChart.AddSample(val);
                ZeroToast.Success(this, $"Đã đo mẫu mới: {val:F3} mm (Trong kiểm soát)");
            };

            btnSpikeSpc.Click += (s, e) =>
            {
                float spike = 12.028f;
                spcChart.AddSample(spike, "Lệch dao phay CNC!");
                ZeroToast.Error(this, $"CẢNH BÁO SPC: Mẫu {spike:F3} mm vượt giới hạn kiểm soát trên (UCL: {spcChart.UCL:F3} mm)!");
            };

            kanban.CardClicked += (s, e) =>
            {
                kanban.MoveCardNext(e.Card);
                ZeroToast.Info(this, $"Kanban: Lệnh sản xuất {e.Card.OrderNo} ({e.Card.ProductName}) chuyển sang công đoạn kế tiếp!");
            };

            // Assemble top-to-bottom layout
            mainContainer.Controls.Add(row2);
            mainContainer.Controls.Add(row1);
            mainContainer.Controls.Add(bannerSpacer);
            mainContainer.Controls.Add(banner);

            banner.BringToFront();
            bannerSpacer.BringToFront();
            row1.BringToFront();
            row2.BringToFront();

            _tabWms.Controls.Add(mainContainer);
        }

        private void InitializeAdvancedSuite()
        {
            var mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(12),
                BackColor = Color.Transparent
            };

            // 1. Top Banner
            var banner = new ZeroAlertBanner
            {
                Dock = DockStyle.Top,
                Severity = ZeroAlertSeverity.Info,
                Title = "🚀 BỘ SUITE COMPONENT NÂNG CAO MỚI (ADVANCED ENTERPRISE SUITE)",
                Message = "ZeroUI đã tích hợp đầy đủ 6 Component nâng cao: ZeroTreeList (Cây BOM đa cấp ảo hóa), ZeroHeatmap (Ma trận nhiệt 24h x 7 ngày), ZeroLookup (Tìm kiếm Catalog 5,000 vật tư), ZeroDateRangePicker (Khoảng ngày 1-click), ZeroNumericBox (Nhập số chính xác cao), và ZeroTabControl (Bộ chuyển tab phẳng không giật)."
            };

            var bannerSpacer = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent };

            // 2. Top Filter Bar (DateRangePicker, Lookup, NumericBox, Button)
            var topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 8)
            };

            var dtRange = new ZeroDateRangePicker
            {
                Location = new Point(0, 4),
                Width = 260,
                Preset = DateRangePreset.Last7Days
            };
            dtRange.DateRangeChanged += (s, e) =>
            {
                ZeroToast.Info(this, $"Khoảng ngày đã chọn: {dtRange.StartDate:yyyy-MM-dd} → {dtRange.EndDate:yyyy-MM-dd}");
            };

            var lookupCatalog = new ZeroLookup
            {
                Location = new Point(272, 4),
                Width = 320,
                Placeholder = "Tìm trong 5,000 vật tư linh kiện..."
            };

            // Populate 5,000 realistic electronic/industrial items
            var catalogItems = new List<ZeroLookupItem>(5000);
            catalogItems.Add(new ZeroLookupItem("IC-MCU-STM32", "STM32F407VGT6 Cortex-M4 168MHz", "Foxconn Precision • Tồn: 12,450 PCS • $8.50", "Active IC"));
            catalogItems.Add(new ZeroLookupItem("IC-RAM-ISSI", "IS42S16400J 64Mb SDRAM 166MHz", "ISSI Micro • Tồn: 8,200 PCS • $2.40", "Memory"));
            catalogItems.Add(new ZeroLookupItem("IC-ETH-LAN8720", "LAN8720A 10/100 Ethernet Transceiver", "Microchip • Tồn: 5,600 PCS • $1.15", "Interface"));
            catalogItems.Add(new ZeroLookupItem("SEN-KEY-OPTO", "Keyence PR-M51N3 Cảm Biến Quang", "Keyence Japan • Tồn: 320 PCS • $68.00", "Sensors"));
            catalogItems.Add(new ZeroLookupItem("PLC-FX5U-32M", "Mitsubishi FX5U-32MR/ES PLC Main", "Mitsubishi Electric • Tồn: 45 PCS • $285.00", "PLC"));
            catalogItems.Add(new ZeroLookupItem("DRV-STEP-TMC", "TMC2209 Ultra-Silent Stepper Driver", "Trinamic GmbH • Tồn: 2,100 PCS • $4.20", "Motion"));

            string[] catPrefixes = new[] { "RES", "CAP", "IND", "DIO", "MOS", "CONN", "RELAY", "FUSE", "OPTO", "SW" };
            string[] catNames = new[] { "Điện trở dán", "Tụ gốm nhiều lớp", "Cuộn cảm cuộn dây", "Diode Schottky", "Mosfet kênh N", "Đầu nối Header", "Rơ le trung gian", "Cầu chì tự phục hồi", "Optocoupler cách ly", "Công tắc gạt" };

            for (int i = 7; i <= 5000; i++)
            {
                int catIdx = i % catPrefixes.Length;
                string pCode = $"{catPrefixes[catIdx]}-{i:D5}";
                string pName = $"{catNames[catIdx]} SMD #{i}";
                string pSub = $"Tiêu chuẩn AEC-Q200 • Tồn: {(i * 17) % 5000 + 100:N0} PCS • ${(i % 99 + 1) * 0.05f:F2}";
                catalogItems.Add(new ZeroLookupItem(pCode, $"{pCode} • {pName}", pSub, catPrefixes[catIdx]));
            }
            lookupCatalog.SetItems(catalogItems);

            lookupCatalog.SelectedItemChanged += (s, e) =>
            {
                if (lookupCatalog.SelectedItem != null)
                {
                    ZeroToast.Success(this, $"Đã chọn vật tư: [{lookupCatalog.SelectedItem.Key}] {lookupCatalog.SelectedItem.DisplayText}");
                }
            };

            var numBatchSize = new ZeroNumericBox
            {
                Location = new Point(604, 4),
                Width = 200,
                Prefix = "Lô SX:",
                Suffix = "PCS",
                Step = 500,
                Value = 5000,
                MinValue = 100,
                MaxValue = 500000
            };
            numBatchSize.ValueChanged += (s, e) =>
            {
                ZeroToast.Info(this, $"Sản lượng kế hoạch điều chỉnh: {numBatchSize.Value:N0} PCS");
            };

            var btnFilter = new ZeroButton
            {
                Location = new Point(816, 4),
                Size = new Size(130, 36),
                Text = "⚡ Áp Dụng Lọc",
                ButtonStyle = ZeroButtonStyle.Primary
            };
            btnFilter.Click += (s, e) =>
            {
                ZeroToast.Success(this, $"Đã nạp dữ liệu kỳ {dtRange.StartDate:dd/MM} - {dtRange.EndDate:dd/MM} với quy mô {numBatchSize.Value:N0} PCS!");
            };

            topBar.Controls.Add(btnFilter);
            topBar.Controls.Add(numBatchSize);
            topBar.Controls.Add(lookupCatalog);
            topBar.Controls.Add(dtRange);

            var topBarSpacer = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };

            // 3. Body Panel (2 Columns)
            var bodyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            // Left Column: BOM TreeList (Width = 530)
            var leftCol = new Panel
            {
                Dock = DockStyle.Left,
                Width = 530,
                Padding = new Padding(0, 0, 10, 0),
                BackColor = Color.Transparent
            };

            var cardBom = new ZeroCard
            {
                Dock = DockStyle.Fill,
                Title = "Cấu Trúc Cây BOM Đa Cấp (Multi-Level BOM ZeroTreeList)",
                Subtitle = "Ảo hóa phân cấp linh kiện, Chevron mở/đóng, Checkbox 3 trạng thái"
            };

            var bomTools = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.Transparent,
                Padding = new Padding(4, 4, 4, 4)
            };

            var txtBomSearch = new ZeroSearchBox
            {
                Location = new Point(4, 4),
                Width = 200,
                PlaceholderText = "Lọc linh kiện BOM..."
            };

            var btnExpandAll = new ZeroButton
            {
                Location = new Point(210, 4),
                Size = new Size(92, 34),
                Text = "➕ Mở Rộng",
                ButtonStyle = ZeroButtonStyle.Secondary
            };

            var btnCollapseAll = new ZeroButton
            {
                Location = new Point(308, 4),
                Size = new Size(92, 34),
                Text = "➖ Thu Gọn",
                ButtonStyle = ZeroButtonStyle.Secondary
            };

            var btnCheckStats = new ZeroButton
            {
                Location = new Point(406, 4),
                Size = new Size(98, 34),
                Text = "✔ Thống Kê",
                ButtonStyle = ZeroButtonStyle.Success
            };

            bomTools.Controls.Add(btnCheckStats);
            bomTools.Controls.Add(btnCollapseAll);
            bomTools.Controls.Add(btnExpandAll);
            bomTools.Controls.Add(txtBomSearch);

            var treeBom = new ZeroTreeList
            {
                Dock = DockStyle.Fill,
                ShowCheckBoxes = true,
                ShowLines = true
            };

            // Setup modern Context Menu for BOM Tree
            var bomMenu = new ZeroContextMenu();
            bomMenu.AddAction("Sao chép mã linh kiện", () => ZeroToast.Info(this, "Đã sao chép mã linh kiện vào Clipboard!"), "Ctrl+C", "📋");
            bomMenu.AddAction("Tra cứu tồn kho ERP", () => ZeroToast.Info(this, "Tồn kho khả dụng: 1,420 PCS tại Kho SMT Alpha"), "F3", "🔍");
            bomMenu.AddAction("Thêm linh kiện con", () => ZeroModal.Prompt(this, "Thêm Linh Kiện Con", "Nhập mã định mức linh kiện:", "RES-0402-10K", val => ZeroToast.Success(this, $"Đã bổ sung mã {val} vào cụm!")), "Ins", "➕");
            bomMenu.AddSeparator();
            var subCat = bomMenu.AddSubMenu("Phân loại danh mục", "🏷️");
            subCat.AddSubAction("Linh kiện tích cực (IC / Vi xử lý)", () => ZeroToast.Info(this, "Đã gán nhóm IC"));
            subCat.AddSubAction("Linh kiện thụ động (Điện trở / Tụ điện)", () => ZeroToast.Info(this, "Đã gán nhóm R/L/C"));
            subCat.AddSubAction("Cơ khí / Vỏ hộp kim loại", () => ZeroToast.Info(this, "Đã gán nhóm Cơ khí"));
            bomMenu.AddSeparator();
            bomMenu.AddCheckable("Ghim linh kiện ưu tiên KCS", false, chk => ZeroToast.Info(this, $"Đã {(chk ? "ghim" : "bỏ ghim")} linh kiện ưu tiên!"), "⭐");
            bomMenu.AddDangerAction("Xóa khỏi định mức BOM", () => ZeroModal.Confirm(this, "Xác Nhận Xóa", "Bạn có chắc muốn gỡ linh kiện này khỏi định mức sản xuất?", () => ZeroToast.Success(this, "Đã gỡ linh kiện khỏi BOM!")), "Del", "🗑️");

            treeBom.ContextMenuStrip = bomMenu;

            // Build realistic BOM hierarchy for Industrial Smart Gateway
            var rootBom = new ZeroTreeNode("ASM-9000: Gateway Điều Khiển IoT Công Nghiệp", "⚙️", "Tổng định mức: U$ 24.80 • 28 LK")
            {
                Badge = "Cụm Chính",
                BadgeColor = ZeroTheme.Colors.Info
            };

            var pcbAssy = rootBom.AddChild("PCB-001: Bo Mạch Chủ SMT (4-Layer FR4)", "🟩", "Công đoạn: Máy dán SMT Line #1");
            pcbAssy.Badge = "SMT Assy";
            pcbAssy.BadgeColor = ZeroTheme.Colors.Success;
            pcbAssy.AddChild("MCU-STM32: STM32F407VGT6 ARM Cortex-M4 168MHz", "📦", "1 PCS • U$ 8.50").Badge = "Linh Kiện Chính";
            pcbAssy.AddChild("RAM-ISSI: 32MB SDRAM IC 133MHz High-Speed", "📦", "1 PCS • U$ 2.40");
            pcbAssy.AddChild("ETH-PHY: LAN8720A 10/100 Ethernet Controller", "📦", "1 PCS • U$ 1.15");
            pcbAssy.AddChild("FLASH-SPI: W25Q128JV 16MB SPI NOR Flash", "📦", "1 PCS • U$ 0.95");
            pcbAssy.AddChild("PWR-LDO: AMS1117-3.3V Step-down Converter", "⚡", "2 PCS • U$ 0.35");
            pcbAssy.AddChild("XTAL-8M: Thạch anh dao động 8.000MHz ±10ppm", "💎", "1 PCS • U$ 0.20");

            var pwrAssy = rootBom.AddChild("ASM-002: Cụm Cấp Nguồn Cách Ly & Chống Sét 24V", "⚡", "Công đoạn: Ghép hàn THT Line #2");
            pwrAssy.Badge = "Power Sub";
            pwrAssy.BadgeColor = ZeroTheme.Colors.Warning;
            pwrAssy.AddChild("TRF-24V: Biến Áp Xung Flyback 24V/2A Shielded", "🔋", "1 PCS • U$ 3.80");
            pwrAssy.AddChild("MOV-471: Varistor 470V Chống Sét Lan Truyền", "🛡️", "2 PCS • U$ 0.45");
            pwrAssy.AddChild("CAP-450V: Tụ Lọc Cao Áp Nichicon 100uF/450V", "📦", "2 PCS • U$ 1.20");
            pwrAssy.AddChild("FUSE-T2A: Cầu Chì Chậm 250V 2A Chống Cháy", "🔥", "1 PCS • U$ 0.25");

            var mecAssy = rootBom.AddChild("MEC-003: Khung Vỏ Nhôm Anodized IP67", "🛡️", "Công đoạn: Lắp ráp cơ khí CNC");
            mecAssy.Badge = "Cơ Khí";
            mecAssy.BadgeColor = ZeroTheme.Colors.Info;
            mecAssy.AddChild("CNC-TOP: Nắp Nhôm Phay CNC Phủ Anode Đen", "🔩", "1 PCS • U$ 6.20");
            mecAssy.AddChild("CNC-BTM: Đáy Nhôm Phay CNC Định Vị Ray DIN", "🔩", "1 PCS • U$ 4.50");
            mecAssy.AddChild("SCR-M3: Vít Inox 304 M3x8 Chống Gỉ Chịu Rung", "🔩", "8 PCS • U$ 0.08");
            mecAssy.AddChild("GSK-SIL: Gioăng Silicone Đúc Khuôn Chống Nước", "🛞", "1 PCS • U$ 0.90");

            var pkgAssy = rootBom.AddChild("PKG-004: Hộp Đóng Gói & Serial Lot Tracking", "📦", "Công đoạn: KCS & Đóng Thùng");
            pkgAssy.Badge = "Đóng Gói";
            pkgAssy.BadgeColor = ZeroTheme.Colors.Success;
            pkgAssy.AddChild("BOX-CTN: Thùng Carton 3 Lớp Chống Va Đập", "📦", "1 PCS • U$ 0.65");
            pkgAssy.AddChild("FOAM-EVA: Mút Xốp EVA Chống Tĩnh Điện ESD", "🛡️", "2 PCS • U$ 0.40");
            pkgAssy.AddChild("LBL-QR: Tem Nhãn Barcode Serial & QR Lot", "🏷️", "2 PCS • U$ 0.05");

            treeBom.AddNode(rootBom);

            txtBomSearch.DebouncedTextChanged += (s, text) =>
            {
                treeBom.FilterText = text;
            };

            btnExpandAll.Click += (s, e) => treeBom.ExpandAll();
            btnCollapseAll.Click += (s, e) => treeBom.CollapseAll();

            btnCheckStats.Click += (s, e) =>
            {
                int totalChecked = 0;
                CountCheckedNodes(rootBom, ref totalChecked);
                ZeroToast.Success(this, $"Đã chọn {totalChecked} hạng mục trong cây BOM sẵn sàng xuất lệnh lắp ráp!");
            };

            treeBom.NodeSelected += (s, node) =>
            {
                ZeroToast.Info(this, $"BOM: {node.Text} {(string.IsNullOrEmpty(node.SubText) ? "" : "• " + node.SubText)}");
            };

            cardBom.ContentPanel.Controls.Add(treeBom);
            cardBom.ContentPanel.Controls.Add(bomTools);
            bomTools.BringToFront();
            leftCol.Controls.Add(cardBom);

            // Right Column (Heatmap on Top + ZeroTabControl on Bottom)
            var rightCol = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            // Card Heatmap
            var cardHeatmap = new ZeroCard
            {
                Dock = DockStyle.Top,
                Height = 310,
                Title = "Ma Trận Nhiệt Sản Lượng Chuyền SMT 24 Giờ x 7 Ngày (ZeroHeatmap)",
                Subtitle = "Phân bố công suất theo từng khung giờ, tooltip chi tiết và chuyển đổi dải màu",
                ActionText = "🎨 Đổi Bảng Màu"
            };

            var heatmap = new ZeroHeatmap
            {
                Dock = DockStyle.Fill,
                ShowValues = true,
                ShowLegend = true,
                PaletteMode = HeatmapPaletteMode.Industrial
            };

            cardHeatmap.ActionClicked += (s, e) =>
            {
                var nextMode = heatmap.PaletteMode switch
                {
                    HeatmapPaletteMode.Industrial => HeatmapPaletteMode.Viridis,
                    HeatmapPaletteMode.Viridis => HeatmapPaletteMode.CoolWarm,
                    HeatmapPaletteMode.CoolWarm => HeatmapPaletteMode.Emerald,
                    _ => HeatmapPaletteMode.Industrial
                };
                heatmap.PaletteMode = nextMode;
                ZeroToast.Info(this, $"Đã đổi bảng màu Heatmap sang: {nextMode}");
            };

            heatmap.CellClicked += (s, e) =>
            {
                ZeroToast.Success(this, $"[SẢN LƯỢNG] {e.RowLabel} lúc {e.ColumnLabel}: {e.Value:0} PCS/giờ");
            };

            cardHeatmap.ContentPanel.Controls.Add(heatmap);

            var heatmapSpacer = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent };

            // Card ZeroTabControl
            var cardTabs = new ZeroCard
            {
                Dock = DockStyle.Fill,
                Title = "Bộ Chuyển Tab Phẳng Hiện Đại Không Giật (ZeroTabControl)",
                Subtitle = "Hỗ trợ phong cách Underline / Pill / Card, Notification Badges, và Dark Mode"
            };

            var tabTools = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.Transparent,
                Padding = new Padding(4, 2, 4, 4)
            };

            var segStyle = new ZeroSegmented
            {
                Location = new Point(4, 2),
                Size = new Size(260, 32),
                Items = new[] { "Underline", "Pill", "Card" },
                SelectedIndex = 0
            };

            var tabSuite = new ZeroTabControl
            {
                Dock = DockStyle.Fill,
                TabStyle = ZeroTabStyle.Underline
            };

            segStyle.SelectedIndexChanged += (s, e) =>
            {
                tabSuite.TabStyle = (ZeroTabStyle)segStyle.SelectedIndex;
            };

            tabTools.Controls.Add(segStyle);

            // Page 1: SMT Oven Reflow Profile
            var pageOven = tabSuite.AddTab("Thiết Lập Lò Hàn SMT", "🔥");
            pageOven.Padding = new Padding(12);

            var pnlOven = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            var numZ1 = CreateParamBox("Vùng 1 (Preheat):", 160.0m, "°C", 0.5m, 100m, 200m);
            var numZ2 = CreateParamBox("Vùng 2 (Soak):", 195.0m, "°C", 0.5m, 150m, 230m);
            var numZ3 = CreateParamBox("Vùng 3 (Peak Reflow):", 248.5m, "°C", 0.5m, 200m, 280m);
            var numSpeed = CreateParamBox("Tốc Độ Băng Tải:", 1.15m, "m/min", 0.05m, 0.5m, 3.0m, 2);
            var numN2 = CreateParamBox("Độ Tinh Khiết N2:", 99.98m, "%", 0.01m, 95m, 100m, 2);

            pnlOven.Controls.Add(numZ1);
            pnlOven.Controls.Add(numZ2);
            pnlOven.Controls.Add(numZ3);
            pnlOven.Controls.Add(numSpeed);
            pnlOven.Controls.Add(numN2);
            pageOven.Controls.Add(pnlOven);

            // Page 2: Work Order Specifications
            var pageOrder = tabSuite.AddTab("Thông Số Lệnh SX", "📋");
            pageOrder.Padding = new Padding(12);

            var descOrder = new ZeroDescriptions
            {
                Dock = DockStyle.Fill,
                Columns = 2,
                RowHeight = 32
            };
            descOrder.Add("Mã Lệnh SX", "WO-2026-GATEWAY-88");
            descOrder.Add("Sản Phẩm Đích", "B1030 IoT Smart Gateway Rev 2.0");
            descOrder.Add("Chuyền Gia Công", "Line SMT Alpha #01");
            descOrder.Add("Mục Tiêu FPY", "99.45% Yield Rate", Color.FromArgb(16, 185, 129));
            descOrder.Add("Lead Engineer", "Võ Tuấn Phong (Kỹ Sư Trưởng)");
            descOrder.Add("Tiêu Chuẩn Hàn", "IPC-A-610 Class 3 Industrial");
            pageOrder.Controls.Add(descOrder);

            // Page 3: Line Alarms
            var pageAlerts = tabSuite.AddTab("Cảnh Báo Chuyền", "🔔", 3);
            pageAlerts.Padding = new Padding(12);

            var alertBox = new ZeroAlertBanner
            {
                Dock = DockStyle.Top,
                Severity = ZeroAlertSeverity.Warning,
                Title = "CẢNH BÁO TIỄN LIỆU KHAY SMT FEEDER BOA472",
                Message = "Cuộn tụ gốm 10uF tại slot #18 sắp hết linh kiện (còn < 150 PCS). Đề nghị kỹ thuật viên nạp cuộn mới trong vòng 12 phút để tránh dừng chuyền!"
            };
            pageAlerts.Controls.Add(alertBox);

            // Page 4: ZeroImage & ZeroModal Dialogs
            var pageImageModal = tabSuite.AddTab("Ảnh & Modal Dialogs", "🖼️");
            pageImageModal.Padding = new Padding(10);

            var pnlImgModal = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            // Avatar Section
            var pnlAvatars = new Panel { Size = new Size(520, 68), BackColor = Color.Transparent };
            var lblAvatars = new Label
            {
                Text = "ZeroImage (Avatar & Trạng Thái):",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = ZeroTheme.Colors.TextSecondary,
                Location = new Point(4, 2),
                AutoSize = true
            };

            var av1 = new ZeroImage
            {
                Location = new Point(4, 20),
                Size = new Size(42, 42),
                IsCircle = true,
                FallbackText = "Võ Tuấn Phong",
                Status = AvatarStatus.Online
            };

            var av2 = new ZeroImage
            {
                Location = new Point(56, 20),
                Size = new Size(42, 42),
                IsCircle = true,
                FallbackText = "Nguyễn Văn An",
                Status = AvatarStatus.Busy
            };

            var av3 = new ZeroImage
            {
                Location = new Point(108, 20),
                Size = new Size(42, 42),
                IsCircle = true,
                FallbackText = "Trần Thị Bích",
                Status = AvatarStatus.Away
            };

            // PCB Inspection Image with Lightbox Preview
            var pcbBmp = new Bitmap(240, 150);
            using (var gPcb = Graphics.FromImage(pcbBmp))
            {
                gPcb.Clear(Color.FromArgb(15, 60, 40));
                using var penTrace = new Pen(Color.FromArgb(30, 180, 100), 2f);
                gPcb.DrawLine(penTrace, 20, 20, 100, 20);
                gPcb.DrawLine(penTrace, 100, 20, 140, 60);
                gPcb.DrawLine(penTrace, 140, 60, 220, 60);
                gPcb.DrawLine(penTrace, 30, 80, 110, 80);
                gPcb.DrawLine(penTrace, 110, 80, 150, 120);
                using var brushChip = new SolidBrush(Color.FromArgb(30, 30, 30));
                gPcb.FillRectangle(brushChip, new Rectangle(90, 40, 60, 60));
                using var brushGold = new SolidBrush(Color.FromArgb(234, 179, 8));
                for (int p = 0; p < 5; p++)
                {
                    gPcb.FillRectangle(brushGold, 82, 45 + (p * 10), 6, 4);
                    gPcb.FillRectangle(brushGold, 152, 45 + (p * 10), 6, 4);
                }
            }

            var imgPcb = new ZeroImage
            {
                Location = new Point(165, 14),
                Size = new Size(84, 50),
                BorderRadius = 6,
                Image = pcbBmp,
                ScaleMode = ImageScaleMode.Cover,
                EnableZoomPreview = true
            };

            var lblZoomHint = new Label
            {
                Text = "🔍 Click ảnh để phóng to Lightbox",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
                ForeColor = ZeroTheme.Colors.TextSecondary,
                Location = new Point(255, 30),
                AutoSize = true
            };

            pnlAvatars.Controls.Add(lblZoomHint);
            pnlAvatars.Controls.Add(imgPcb);
            pnlAvatars.Controls.Add(av3);
            pnlAvatars.Controls.Add(av2);
            pnlAvatars.Controls.Add(av1);
            pnlAvatars.Controls.Add(lblAvatars);
            pnlImgModal.Controls.Add(pnlAvatars);

            // Modal Dialog Buttons Section
            var pnlModalBtns = new Panel { Size = new Size(520, 80), BackColor = Color.Transparent };
            var lblModals = new Label
            {
                Text = "ZeroModal (Popup Thông Báo & Xác Nhận):",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = ZeroTheme.Colors.TextSecondary,
                Location = new Point(4, 4),
                AutoSize = true
            };

            var btnSuccess = new ZeroButton
            {
                Location = new Point(4, 26),
                Size = new Size(95, 34),
                Text = "✔ Success",
                ButtonStyle = ZeroButtonStyle.Success,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold)
            };
            btnSuccess.Click += (s, e) =>
            {
                ZeroModal.Success(this, "Kiểm Định Hoàn Tất", "Đã ghi nhận 2,500 sản phẩm IoT Gateway đạt tiêu chuẩn KCS!");
            };

            var btnWarning = new ZeroButton
            {
                Location = new Point(105, 26),
                Size = new Size(95, 34),
                Text = "⚠ Warning",
                ButtonStyle = ZeroButtonStyle.Secondary,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold)
            };
            btnWarning.Click += (s, e) =>
            {
                ZeroModal.Warning(this, "Cảnh Báo Giới Hạn", "Nhiệt độ vùng Reflow vượt ngưỡng 248.5 °C. Kiểm tra quạt đối lưu!");
            };

            var btnError = new ZeroButton
            {
                Location = new Point(206, 26),
                Size = new Size(95, 34),
                Text = "✕ Error",
                ButtonStyle = ZeroButtonStyle.Danger,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold)
            };
            btnError.Click += (s, e) =>
            {
                ZeroModal.Error(this, "Mất Kết Nối PLC", "Không nhận được phản hồi Modbus TCP từ trạm dán SMT sau 3 lần thử!");
            };

            var btnConfirm = new ZeroButton
            {
                Location = new Point(307, 26),
                Size = new Size(95, 34),
                Text = "? Confirm",
                ButtonStyle = ZeroButtonStyle.Primary,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold)
            };
            btnConfirm.Click += (s, e) =>
            {
                ZeroModal.Confirm(
                    this,
                    "Xác Nhận Xuất Xưởng",
                    "Bạn có chắc muốn cấp phát mã Serial cho 500 thùng hàng?",
                    onConfirm: () => ZeroToast.Success(this, "Đã cấp phát 500 tem mã vạch thành công!"),
                    confirmText: "Đồng ý",
                    cancelText: "Hủy");
            };

            var btnPrompt = new ZeroButton
            {
                Location = new Point(408, 26),
                Size = new Size(95, 34),
                Text = "✏ Prompt",
                ButtonStyle = ZeroButtonStyle.Secondary,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold)
            };
            btnPrompt.Click += (s, e) =>
            {
                ZeroModal.Prompt(
                    this,
                    "Quét Barcode / Nhập Mã",
                    "Vui lòng quét Serial Number linh kiện cần tra cứu:",
                    "SN-GW-2026-8801",
                    val => ZeroToast.Success(this, $"Đã nhận Serial: {val}"));
            };

            pnlModalBtns.Controls.Add(btnPrompt);
            pnlModalBtns.Controls.Add(btnConfirm);
            pnlModalBtns.Controls.Add(btnError);
            pnlModalBtns.Controls.Add(btnWarning);
            pnlModalBtns.Controls.Add(btnSuccess);
            pnlModalBtns.Controls.Add(lblModals);
            pnlImgModal.Controls.Add(pnlModalBtns);

            pageImageModal.Controls.Add(pnlImgModal);

            cardTabs.ContentPanel.Controls.Add(tabSuite);
            cardTabs.ContentPanel.Controls.Add(tabTools);

            rightCol.Controls.Add(cardTabs);
            rightCol.Controls.Add(heatmapSpacer);
            rightCol.Controls.Add(cardHeatmap);

            bodyPanel.Controls.Add(rightCol);
            bodyPanel.Controls.Add(leftCol);

            // Assemble main container
            mainContainer.Controls.Add(bodyPanel);
            mainContainer.Controls.Add(topBarSpacer);
            mainContainer.Controls.Add(topBar);
            mainContainer.Controls.Add(bannerSpacer);
            mainContainer.Controls.Add(banner);

            banner.BringToFront();
            bannerSpacer.BringToFront();
            topBar.BringToFront();
            topBarSpacer.BringToFront();
            bodyPanel.BringToFront();

            _tabAdvanced.Controls.Add(mainContainer);
        }

        private Panel CreateParamBox(string label, decimal initialVal, string unit, decimal step, decimal min, decimal max, int decimals = 1)
        {
            var pnl = new Panel
            {
                Size = new Size(230, 68),
                Margin = new Padding(6)
            };

            var lbl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = ZeroTheme.Colors.TextSecondary,
                Location = new Point(0, 2),
                AutoSize = true
            };

            var num = new ZeroNumericBox
            {
                Location = new Point(0, 24),
                Width = 220,
                Suffix = unit,
                Step = step,
                MinValue = min,
                MaxValue = max,
                DecimalPlaces = decimals,
                Value = initialVal
            };

            pnl.Controls.Add(lbl);
            pnl.Controls.Add(num);
            return pnl;
        }

        private void CountCheckedNodes(ZeroTreeNode node, ref int count)
        {
            if (node.CheckState == CheckState.Checked) count++;
            foreach (var child in node.Children)
            {
                CountCheckedNodes(child, ref count);
            }
        }

        private void OpenGlobalSettingsDialog()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(12) };

            int curY = 6;

            // 1. Corner Style
            var lblCorners = new Label
            {
                Text = "📐 Kiểu Bo Góc Toàn Cục (Global Corner Style - DevExpress Style):",
                Font = new Font(ZeroUIConfig.DefaultFont.FontFamily, 9.5f, FontStyle.Bold),
                ForeColor = ZeroTheme.Colors.TextPrimary,
                Location = new Point(8, curY),
                AutoSize = true
            };
            pnl.Controls.Add(lblCorners);
            curY += 26;

            var segCorners = new ZeroSegmented
            {
                Location = new Point(8, curY),
                Size = new Size(470, 36),
                Items = new[] { "Bo Tròn (Rounded - 6px)", "Vuông Vức (Sharp - 0px)", "Viên Thuốc (Pill - 12px)" },
                SelectedIndex = ZeroUIConfig.CornerStyle switch
                {
                    ZeroCornerStyle.Rounded => 0,
                    ZeroCornerStyle.Sharp => 1,
                    ZeroCornerStyle.Pill => 2,
                    _ => 0
                }
            };
            pnl.Controls.Add(segCorners);
            curY += 46;

            // 2. Global Font
            var lblFont = new Label
            {
                Text = "🔤 Font Chữ Toàn Cục (Global Default Font):",
                Font = new Font(ZeroUIConfig.DefaultFont.FontFamily, 9.5f, FontStyle.Bold),
                ForeColor = ZeroTheme.Colors.TextPrimary,
                Location = new Point(8, curY),
                AutoSize = true
            };
            pnl.Controls.Add(lblFont);
            curY += 26;

            string[] fonts = new[] { "Segoe UI", "Aptos", "Tahoma", "Consolas" };
            var segFont = new ZeroSegmented
            {
                Location = new Point(8, curY),
                Size = new Size(470, 36),
                Items = fonts,
                SelectedIndex = ZeroUIConfig.DefaultFont.FontFamily.Name switch
                {
                    "Aptos" => 1,
                    "Tahoma" => 2,
                    "Consolas" => 3,
                    _ => 0
                }
            };
            pnl.Controls.Add(segFont);
            curY += 46;

            // 3. Live Preview Card
            var pnlPreview = new ZeroCard
            {
                Location = new Point(8, curY),
                Size = new Size(470, 95),
                Title = "🔍 Xem Trước Trực Quan (Live Preview)",
                Subtitle = "Hiệu lực ngay lập tức cho toàn bộ các control"
            };
            var previewBtn = new ZeroButton { Location = new Point(10, 8), Size = new Size(125, 34), Text = "Nút Mẫu", ButtonStyle = ZeroButtonStyle.Primary };
            var previewTag = new ZeroTag { Location = new Point(145, 12), Size = new Size(95, 26), Text = "Hoạt Động", TagType = ZeroTagType.Success };
            var previewSearch = new ZeroSearchBox { Location = new Point(250, 8), Size = new Size(195, 34), PlaceholderText = "Ô nhập liệu thử..." };

            pnlPreview.ContentPanel.Controls.Add(previewBtn);
            pnlPreview.ContentPanel.Controls.Add(previewTag);
            pnlPreview.ContentPanel.Controls.Add(previewSearch);
            pnl.Controls.Add(pnlPreview);

            segCorners.SelectedIndexChanged += (s, e) =>
            {
                switch (segCorners.SelectedIndex)
                {
                    case 0:
                        ZeroUIConfig.CornerStyle = ZeroCornerStyle.Rounded;
                        ZeroUIConfig.DefaultBorderRadius = 6;
                        break;
                    case 1:
                        ZeroUIConfig.CornerStyle = ZeroCornerStyle.Sharp;
                        ZeroUIConfig.DefaultBorderRadius = 0;
                        break;
                    case 2:
                        ZeroUIConfig.CornerStyle = ZeroCornerStyle.Pill;
                        ZeroUIConfig.DefaultBorderRadius = 12;
                        break;
                }
                ZeroUIConfig.NotifyConfigChanged();
                pnlPreview.Invalidate(true);
            };

            segFont.SelectedIndexChanged += (s, e) =>
            {
                string family = segFont.SelectedIndex switch
                {
                    1 => "Aptos",
                    2 => "Tahoma",
                    3 => "Consolas",
                    _ => "Segoe UI"
                };
                ZeroUIConfig.DefaultFont = new Font(family, 9.25f, FontStyle.Regular);
                ZeroUIConfig.NotifyConfigChanged();
                lblCorners.Font = new Font(family, 9.5f, FontStyle.Bold);
                lblFont.Font = new Font(family, 9.5f, FontStyle.Bold);
                pnlPreview.Invalidate(true);
            };

            ZeroModal.Show(
                this,
                "⚙️ Cài Đặt Toàn Cục Ứng Dụng (ZeroUI Global Settings)",
                pnl,
                okText: "Đóng",
                showCancel: false,
                width: 530,
                height: 380);
        }
    }
}







