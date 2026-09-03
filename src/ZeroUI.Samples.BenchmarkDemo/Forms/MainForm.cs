using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using ZeroUI.Core.Common;
using ZeroUI.Core.Data;
using ZeroUI.Samples.BenchmarkDemo.Data;
using ZeroUI.Samples.BenchmarkDemo.Diagnostics;
using ZeroUI.WinForms.Controls;

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
        private TabPage _tabControls = null!;
        private ZeroListView _showcaseLog = null!;
        private System.Windows.Forms.Timer? _logGenTimer;


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

            // Load 100,000 rows initially
            LoadDataset(100_000);
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
            Button btn100k = CreateActionButton("100K Dòng", btnX, () => LoadDataset(100_000));
            btnX += 110;
            Button btn500k = CreateActionButton("500K Dòng", btnX, () => LoadDataset(500_000));
            btnX += 110;
            Button btn1M = CreateActionButton("1 TRIỆU Dòng", btnX, () => LoadDataset(1_000_000));
            btnX += 130;
            Button btn10M = CreateActionButton("🔥 10 TRIỆU Dòng", btnX, () => LoadDataset(10_000_000));
            btn10M.BackColor = Color.FromArgb(190, 24, 24);
            btnX += 150;

            _btnAutoScroll = new Button
            {
                Text = "🚀 Chạy Auto-Scroll Stress Test (10s)",
                Location = new Point(btnX, 10),
                Size = new Size(260, 34),
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

            _lblStatus = CreateMetricLabel("Dữ liệu: Khởi tạo...", 12);
            _lblFps = CreateMetricLabel("FPS: --", 240);
            _lblLatency = CreateMetricLabel("Độ trễ: -- ms", 400);
            _lblRam = CreateMetricLabel("RAM: -- MB", 560);
            _lblGc = CreateMetricLabel("GC Gen0: --", 730);

            _hudPanel.Controls.Add(_lblStatus);
            _hudPanel.Controls.Add(_lblFps);
            _hudPanel.Controls.Add(_lblLatency);
            _hudPanel.Controls.Add(_lblRam);
            _hudPanel.Controls.Add(_lblGc);

            // 3. Tab Control
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Padding = new Point(16, 8)
            };

            _tabZero = new TabPage("⚡ ZeroGrid (ZeroUI Core Engine)");
            _tabDgv = new TabPage("🐢 DataGridView Mặc Định (VirtualMode)");
            _tabControls = new TabPage("🎨 Components Showcase (Controls Mới)");

            InitializeZeroGrid();
            InitializeDataGridView();
            InitializeComponentsShowcase();

            _tabZero.Controls.Add(_zeroGrid);
            _tabZero.Controls.Add(_pagination);
            _tabZero.Controls.Add(_searchBar);

            _tabDgv.Controls.Add(_dgv);

            _tabControl.TabPages.Add(_tabZero);
            _tabControl.TabPages.Add(_tabDgv);
            _tabControl.TabPages.Add(_tabControls);
            _tabControl.SelectedIndexChanged += (s, e) =>
            {
                _scrollFrames = 0;
                _scrollStopwatch.Restart();
            };

            // Assembly Form Layout
            Controls.Add(_tabControl);
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
            _zeroGrid.Columns.Add(new ZeroColumn("Mã Vật Tư", 120, CellAlignment.Left));
            _zeroGrid.Columns.Add(new ZeroColumn("Tên Vật Tư / Linh Kiện", 280, CellAlignment.Left));
            _zeroGrid.Columns.Add(new ZeroColumn("Số Lượng", 90, CellAlignment.Right));
            _zeroGrid.Columns.Add(new ZeroColumn("Đơn Giá (VNĐ)", 130, CellAlignment.Right));
            _zeroGrid.Columns.Add(new ZeroColumn("Thành Tiền (VNĐ)", 150, CellAlignment.Right));
            _zeroGrid.Columns.Add(new ZeroColumn("Số Lô", 120, CellAlignment.Center));
            _zeroGrid.Columns.Add(new ZeroColumn("Trạng Thái", 130, CellAlignment.Center));

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
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã Vật Tư", Width = 120 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên Vật Tư / Linh Kiện", Width = 280 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Số Lượng", Width = 90 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Đơn Giá (VNĐ)", Width = 130 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Thành Tiền (VNĐ)", Width = 150 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Số Lô", Width = 120 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng Thái", Width = 130 });

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
            _lblStatus.Text = $"Dữ liệu: Đang tạo {count:N0} dòng...";
            Application.DoEvents();

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
                _lblStatus.Text = $"Dữ liệu: {count:N0} dòng (ZeroUI nạp trong {sw.ElapsedMilliseconds} ms - DGV quá tải)";
                _scrollFrames = 0;
                _scrollStopwatch.Restart();
                return;
            }

            _dataset = MockDataGenerator.Generate(count);
            _zeroSource = new ZeroInventorySource(_dataset);

            // Bind to ZeroGrid (Executes in <1ms)
            _zeroGrid.DataSource = _zeroSource;

            // Bind to DataGridView safely without WinForms RemoveAt freeze
            try
            {
                _dgv.SuspendLayout();
                _dgv.Rows.Clear();
                _dgv.RowCount = count;
                _dgv.ResumeLayout();
            }
            catch { }

            sw.Stop();
            Cursor = Cursors.Default;

            _pagination.TotalRows = count;
            _searchBar.UpdateCountBadge();
            _baselineGen0 = GC.CollectionCount(0);
            _lblStatus.Text = $"Dữ liệu: {count:N0} dòng (Tải trong {sw.ElapsedMilliseconds} ms)";
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
            double latency = fps > 0 ? (1000.0 / fps) : 0.0;

            _lblFps.Text = $"FPS: {fps:F1}";
            _lblLatency.Text = $"Độ trễ: {latency:F1} ms";
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
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                _lblStatus.Text = "Đang xuất CSV...";
                var sw = Stopwatch.StartNew();

                try
                {
                    int count = await ZeroUI.WinForms.Export.ZeroGridExporter.ExportToCsvAsync(_zeroGrid.DataSource, _zeroGrid, sfd.FileName);
                    sw.Stop();
                    double rowsPerSec = count / Math.Max(0.001, sw.Elapsed.TotalSeconds);
                    MessageBox.Show(
                        $"✅ Xuất thành công {count:N0} dòng ra file:\n{sfd.FileName}\n\nThời gian: {sw.ElapsedMilliseconds} ms ({rowsPerSec:N0} dòng/giây)",
                        "Xuất file thành công",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi xuất file: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    Cursor = Cursors.Default;
                    _lblStatus.Text = $"Đã xuất {sfd.FileName}";
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
            leftPanel.Controls.Add(lblBtnTitle);

            int btnY = 48;
            var btnPrimary = new ZeroButton { Text = "Primary Action", ButtonStyle = ZeroButtonStyle.Primary, Location = new Point(16, btnY), Size = new Size(130, 36) };
            var btnSuccess = new ZeroButton { Text = "Thành Công", ButtonStyle = ZeroButtonStyle.Success, Location = new Point(156, btnY), Size = new Size(120, 36) };
            var btnDanger = new ZeroButton { Text = "Xóa Dữ Liệu", ButtonStyle = ZeroButtonStyle.Danger, Location = new Point(286, btnY), Size = new Size(120, 36) };
            leftPanel.Controls.Add(btnPrimary);
            leftPanel.Controls.Add(btnSuccess);
            leftPanel.Controls.Add(btnDanger);

            btnY += 46;
            var btnSecondary = new ZeroButton { Text = "Secondary", ButtonStyle = ZeroButtonStyle.Secondary, Location = new Point(16, btnY), Size = new Size(130, 36) };
            var btnBadge = new ZeroButton { Text = "Thông Báo", ButtonStyle = ZeroButtonStyle.Primary, BadgeText = "9+", Location = new Point(156, btnY), Size = new Size(140, 36) };
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
            var lblDeterminate = new Label { Text = "Tiến độ xác định (78%):", AutoSize = true, Location = new Point(16, progY), Font = new Font("Segoe UI", 9f) };
            leftPanel.Controls.Add(lblDeterminate);
            progY += 22;
            var prog1 = new ZeroProgressBar { Location = new Point(16, progY), Size = new Size(390, 24), Value = 78, ProgressColor = Color.FromArgb(16, 185, 129) };
            leftPanel.Controls.Add(prog1);

            progY += 34;
            var lblIndeterminate = new Label { Text = "Tiến độ vô hạn (Marquee 60 FPS):", AutoSize = true, Location = new Point(16, progY), Font = new Font("Segoe UI", 9f) };
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

            searchY += 32;
            var searchDemo = new ZeroSearchBox { Location = new Point(16, searchY), Width = 390, PlaceholderText = "🔍 Nhập từ khóa thử nghiệm..." };
            leftPanel.Controls.Add(searchDemo);

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
                Text = "4. ZeroListView (Trình xem Log 100K+ dòng siêu tốc)",
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
                    batch.Add(new LogEntry(DateTime.Now, (LogSeverity)(i % 4), $"[SỰ KIỆN TỰ ĐỘNG #{i + 1}] Đồng bộ dữ liệu lô hàng vào kho thành công."));
                }
                _showcaseLog.AddLogs(batch);
            };
            topLogBar.Controls.Add(btnAdd1000);

            var btnClearLogs = new ZeroButton
            {
                Text = "Xóa Log",
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
            _showcaseLog.AddLog(LogSeverity.Info, "Hệ thống ZeroUI khởi tạo thành công.");
            _showcaseLog.AddLog(LogSeverity.Success, "Kết nối DIBSection Memory DC hoàn tất (32bpp).");
            _showcaseLog.AddLog(LogSeverity.Warning, "Bộ nhớ RAM đang nạp dữ liệu quy mô lớn.");
            _showcaseLog.AddLog(LogSeverity.Error, "Cảnh báo quá tải DataGridView WinForms mặc định.");

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
                    _showcaseLog.AddLog(sev, $"[Dịch vụ #{simCount++}] Xử lý giao dịch I/O qua ZeroUI engine độ trễ 0.02ms.");
                }
            };
            _logGenTimer.Start();
        }
    }
}

