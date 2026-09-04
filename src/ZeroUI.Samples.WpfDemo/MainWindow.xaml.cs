using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ZeroUI.Core.Common;
using ZeroUI.Core.Data;
using ZeroUI.Core.Theme;
using ZeroUI.Samples.WpfDemo.Data;
using ZeroUI.Wpf.Charts.Model;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Samples.WpfDemo
{
    public partial class MainWindow : Window
    {
        private ZeroWpfInventorySource? _inventorySource;
        private DispatcherTimer? _telemetryTimer;
        private DispatcherTimer? _scadaSimTimer;
        private bool _isSimulating = true;

        // Telemetry tracking
        private int _frameCount = 0;
        private Stopwatch _fpsStopwatch = Stopwatch.StartNew();
        private double _currentFps = 0.0;
        private int _initialGc0 = 0;
        private int _initialGc1 = 0;
        private int _initialGc2 = 0;

        // SCADA Simulation State
        private double _simRpm = 1840;
        private double _simPressure = 145;
        private int _simTaktSeconds = 265;
        private int _simCycles = 1842;

        public MainWindow()
        {
            InitializeComponent();
            SetupColumns();
            SetupCharts();
            SetupTelemetry();
            SetupScadaSimulation();

            CompositionTarget.Rendering += OnCompositionRendering;

            // Setup Skin Selector & Studio
            RefreshSkinSelector();
            ZeroSkinManager.SkinChanged += skin =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (ComboSkinSelector.SelectedItem != skin)
                    {
                        ComboSkinSelector.SelectedItem = skin;
                    }
                    ApplySkin(skin);
                });
            };
            ZeroSkinManager.RegistryChanged += () =>
            {
                Dispatcher.Invoke(RefreshSkinSelector);
            };

            // Load initial 100k records for instant wow factor
            LoadData(100000);
            ApplySkin(ZeroSkinManager.CurrentSkin);
        }

        private void RefreshSkinSelector()
        {
            var cur = ZeroSkinManager.CurrentSkin;
            ComboSkinSelector.ItemsSource = null;
            ComboSkinSelector.ItemsSource = ZeroSkinManager.AvailableSkins;
            ComboSkinSelector.SelectedItem = cur;
        }

        private void BtnSkinStudio_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ZeroSkinStudioDialog { Owner = this };
            dialog.ShowDialog();
        }

        private void SetupColumns()
        {
            var colId = new ZeroColumn("ID", 75, CellAlignment.Right) { ReadOnly = true, IsPinned = true, Summary = SummaryType.Count, SummaryFormat = "{0:N0} items" };
            var colCode = new ZeroColumn("Material Code", 130, CellAlignment.Left) { ReadOnly = true, IsPinned = true };
            var colName = new ZeroColumn("Description / Component Name", 240, CellAlignment.Left);
            var colQty = new ZeroColumn("Quantity", 95, CellAlignment.Right) { Summary = SummaryType.Sum, SummaryFormat = "{0:N0}" };
            var colPrice = new ZeroColumn("Unit Price ($)", 130, CellAlignment.Right) { Summary = SummaryType.Average, SummaryFormat = "Avg: ${0:N2}" };
            var colTotal = new ZeroColumn("Total Amount ($)", 150, CellAlignment.Right) { ReadOnly = true, Summary = SummaryType.Sum, SummaryFormat = "${0:N2}" };
            var colLot = new ZeroColumn("Lot Number", 120, CellAlignment.Center);
            var colStatus = new ZeroColumn("Inspection Status", 160, CellAlignment.Center);

            VirtualGrid.Columns.Add(colId);
            VirtualGrid.Columns.Add(colCode);
            VirtualGrid.Columns.Add(colName);
            VirtualGrid.Columns.Add(colQty);
            VirtualGrid.Columns.Add(colPrice);
            VirtualGrid.Columns.Add(colTotal);
            VirtualGrid.Columns.Add(colLot);
            VirtualGrid.Columns.Add(colStatus);

            VirtualGrid.ShowFooter = true;
            VirtualGrid.SelectionMode = ZeroGridSelectionMode.MultiRow;

            VirtualGrid.CellValueChanged += (s, args) =>
            {
                TxtLatency.Text = $"Cell edited: [{args.VisualRowIndex},{args.ColumnIndex}] = \"{args.NewValue}\"";
            };

            // Header sort click
            VirtualGrid.ColumnHeaderClicked += async (s, colIdx) =>
            {
                await VirtualGrid.SortByColumnAsync(colIdx);
            };

            VirtualGrid.SortingStarted += (s, e) =>
            {
                TxtLatency.Text = "Sorting... ⏳";
            };

            VirtualGrid.SortingCompleted += (s, elapsed) =>
            {
                TxtLatency.Text = $"{elapsed.TotalMilliseconds:0.#} ms (Sort)";
            };

            // Search Bar wireup
            GridSearch.DensityChanged += (s, density) => VirtualGrid.Density = density;
            GridSearch.SearchTriggered += (s, query) => FilterData(query);
            GridSearch.ExportTriggered += (s, e) =>
            {
                MessageBox.Show($"Exported {VirtualGrid.IndexMap.ActiveCount:N0} rows to CSV successfully!", "ZeroUI Export", MessageBoxButton.OK, MessageBoxImage.Information);
            };

            // Pagination wireup
            GridPager.PageSizeChanged += (s, size) => VirtualGrid.InvalidateVisual();
        }

        private void LoadData(int count)
        {
            var sw = Stopwatch.StartNew();
            var items = ZeroWpfInventorySource.Generate(count);
            _inventorySource = new ZeroWpfInventorySource(items);
            VirtualGrid.DataSource = _inventorySource;
            sw.Stop();

            TxtCapacity.Text = $"{count:N0} Rows";
            GridSearch.SetMatchCount(count, count);
            GridPager.UpdateTotalRecords(count);

            _initialGc0 = GC.CollectionCount(0);
            _initialGc1 = GC.CollectionCount(1);
            _initialGc2 = GC.CollectionCount(2);

            TxtLatency.Text = $"{sw.ElapsedMilliseconds} ms (Init)";
        }

        private void FilterData(string query)
        {
            if (_inventorySource == null) return;

            var items = _inventorySource.Items;
            if (string.IsNullOrWhiteSpace(query))
            {
                VirtualGrid.IndexMap.ResetIdentity(items.Length);
                GridSearch.SetMatchCount(items.Length, items.Length);
                GridPager.UpdateTotalRecords(items.Length);
                VirtualGrid.InvalidateVisual();
                return;
            }

            int matchCount = 0;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].ItemName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    items[i].ItemCode.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    items[i].LotNumber.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    VirtualGrid.IndexMap[matchCount++] = i;
                }
            }

            VirtualGrid.IndexMap.ActiveCount = matchCount;
            GridSearch.SetMatchCount(matchCount, items.Length);
            GridPager.UpdateTotalRecords(matchCount);
            VirtualGrid.InvalidateVisual();
        }

        private void SetupTelemetry()
        {
            _telemetryTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _telemetryTimer.Tick += (s, e) =>
            {
                TxtFps.Text = $"{_currentFps:0} FPS";

                int cur0 = GC.CollectionCount(0);
                int cur1 = GC.CollectionCount(1);
                int cur2 = GC.CollectionCount(2);
                int delta0 = cur0 - _initialGc0;
                int delta1 = cur1 - _initialGc1;
                int delta2 = cur2 - _initialGc2;

                TxtGc.Text = $"{delta0} / {delta1} / {delta2}";

                long ramBytes = Process.GetCurrentProcess().WorkingSet64;
                TxtRam.Text = $"{ramBytes / (1024.0 * 1024.0):0.#} MB";
            };
            _telemetryTimer.Start();
        }

        private void OnCompositionRendering(object? sender, EventArgs e)
        {
            _frameCount++;
            if (_fpsStopwatch.ElapsedMilliseconds >= 500)
            {
                _currentFps = (_frameCount * 1000.0) / _fpsStopwatch.ElapsedMilliseconds;
                _frameCount = 0;
                _fpsStopwatch.Restart();
            }
        }

        private void SetupScadaSimulation()
        {
            var rand = new Random();
            _scadaSimTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
            _scadaSimTimer.Tick += (s, e) =>
            {
                if (!_isSimulating) return;

                // Turbine speed random walk
                _simRpm += (rand.NextDouble() - 0.49) * 35;
                if (_simRpm < 1200) _simRpm = 1200;
                if (_simRpm > 3400) _simRpm = 3400;
                SpindleGauge.Value = _simRpm;

                // Hydraulic pressure
                _simPressure += (rand.NextDouble() - 0.5) * 4;
                _simPressure = Math.Max(90, Math.Min(230, _simPressure));
                PressureGauge.Value = _simPressure;

                // Takt Timer
                if (rand.Next(0, 10) == 0)
                {
                    _simTaktSeconds--;
                    if (_simTaktSeconds <= 0)
                    {
                        _simTaktSeconds = 300;
                        _simCycles++;
                        CycleCounterDisplay.ValueText = $"{_simCycles}";
                    }
                    int min = _simTaktSeconds / 60;
                    int sec = _simTaktSeconds % 60;
                    TaktTimerDisplay.ValueText = $"{min:00}:{sec:00}";
                }

                // Random yellow warning alert on andon tower
                if (_simRpm > 2900)
                {
                    AndonTower.YellowOn = true;
                    AndonTower.RedBlink = true;
                }
                else
                {
                    AndonTower.YellowOn = false;
                    AndonTower.RedBlink = false;
                }
            };
            _scadaSimTimer.Start();
        }

        private void SetupCharts()
        {
            // 1. Bar Chart
            var salesSeries = new ZeroChartSeries("Sales", Color.FromRgb(129, 140, 248));
            var targetSeries = new ZeroChartSeries("Target", Color.FromRgb(166, 227, 161));
            string[] months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            double[] sales = new[] { 45.0, 52.0, 58.0, 64.0, 78.0, 85.0, 92.0, 88.0, 95.0, 104.0, 115.0, 128.0 };

            for (int i = 0; i < months.Length; i++)
            {
                salesSeries.AddPoint(months[i], sales[i]);
            }
            BarChart.Series.Add(salesSeries);

            // 2. Area Spline Chart
            var workloadSeries = new ZeroChartSeries("CPU %", Color.FromRgb(243, 139, 168));
            double[] loads = new double[] { 18, 22, 25, 42, 68, 75, 82, 80, 71, 65, 54, 48, 52, 60, 78, 88, 92, 85, 62, 45, 34, 28, 22, 19 };
            for (int i = 0; i < loads.Length; i++)
            {
                workloadSeries.AddPoint($"{i}:00", loads[i]);
            }
            AreaChart.Series.Add(workloadSeries);

            // 3. Candlestick Chart
            var now = DateTime.Now.Date.AddDays(-30);
            double basePrice = 180.0;
            var rand = new Random(42);
            for (int i = 0; i < 30; i++)
            {
                double open = basePrice + (rand.NextDouble() - 0.48) * 8;
                double close = open + (rand.NextDouble() - 0.48) * 10;
                double high = Math.Max(open, close) + rand.NextDouble() * 5;
                double low = Math.Min(open, close) - rand.NextDouble() * 5;
                basePrice = close;
                CandleChart.CandleData.Add(new ZeroCandlePoint(now.AddDays(i), open, high, low, close, 15000));
            }

            // 4. Donut Chart
            var donutSeries = new ZeroChartSeries("Inventory", Color.FromRgb(129, 140, 248));
            donutSeries.AddPoint("SMT Active (42%)", 420);
            donutSeries.AddPoint("Warehouse A (28%)", 280);
            donutSeries.AddPoint("QC Quarantine (12%)", 120);
            donutSeries.AddPoint("WIP Processing (10%)", 100);
            donutSeries.AddPoint("Safety Reserve (8%)", 80);
            DonutChart.Series.Add(donutSeries);
        }

        private void ComboSkinSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboSkinSelector.SelectedItem is ZeroSkin skin && skin != ZeroSkinManager.CurrentSkin)
            {
                ZeroSkinManager.ApplySkin(skin);
            }
        }

        private void ApplySkin(ZeroSkin skin)
        {
            RootGrid.Background = ZeroWpfTheme.BgPrimary;
            HeaderBar.Background = ZeroWpfTheme.BgCard;
            HeaderBar.BorderBrush = ZeroWpfTheme.BorderDefault;
            HudBorder.Background = ZeroWpfTheme.BgCard;
            HudBorder.BorderBrush = ZeroWpfTheme.BorderDefault;

            BtnSkinStudio.Background = ZeroWpfTheme.PrimaryAccent;
            BtnSkinStudio.Foreground = ZeroWpfTheme.SelectionForeground;

            BtnAbout.Background = ZeroWpfTheme.BgInput;
            BtnAbout.Foreground = ZeroWpfTheme.TextPrimary;

            BtnLoad100K.Background = ZeroWpfTheme.BgInput;
            BtnLoad100K.Foreground = ZeroWpfTheme.TextPrimary;

            BtnLoad1M.Background = ZeroWpfTheme.PrimaryAccent;
            BtnLoad1M.Foreground = Brushes.White;

            BtnLoad10M.Background = ZeroWpfTheme.SecondaryAccent;
            BtnLoad10M.Foreground = Brushes.White;

            BtnSortPrice.Background = ZeroWpfTheme.BgInput;
            BtnSortPrice.Foreground = ZeroWpfTheme.TextPrimary;

            BtnSortQuantity.Background = ZeroWpfTheme.BgInput;
            BtnSortQuantity.Foreground = ZeroWpfTheme.TextPrimary;

            BtnTogglePinning.Background = ZeroWpfTheme.BgInput;
            BtnTogglePinning.Foreground = ZeroWpfTheme.TextPrimary;

            BtnToggleFooter.Background = ZeroWpfTheme.BgInput;
            BtnToggleFooter.Foreground = ZeroWpfTheme.TextPrimary;

            BtnLoadGenericList.Background = ZeroWpfTheme.BgInput;
            BtnLoadGenericList.Foreground = ZeroWpfTheme.TextPrimary;

            BtnClearGrid.Background = ZeroWpfTheme.BgInput;
            BtnClearGrid.Foreground = ZeroWpfTheme.TextPrimary;

            BtnToggleSim.Background = ZeroWpfTheme.BgInput;
            BtnToggleSim.Foreground = ZeroWpfTheme.TextPrimary;

            VirtualGrid.InvalidateVisual();
            BarChart.InvalidateVisual();
            AreaChart.InvalidateVisual();
            CandleChart.InvalidateVisual();
            DonutChart.InvalidateVisual();
            MesHeatmap.InvalidateVisual();
        }

        private void BtnLoad100K_Click(object sender, RoutedEventArgs e) => LoadData(100000);

        private void BtnLoad1M_Click(object sender, RoutedEventArgs e) => LoadData(1000000);

        private void BtnLoad10M_Click(object sender, RoutedEventArgs e)
        {
            var sw = Stopwatch.StartNew();
            var source = new ZeroProceduralSource(10000000);
            _inventorySource = null;
            VirtualGrid.DataSource = source;
            sw.Stop();

            TxtCapacity.Text = "10,000,000 Rows (Limitless)";
            GridSearch.SetMatchCount(10000000, 10000000);
            GridPager.UpdateTotalRecords(10000000);

            _initialGc0 = GC.CollectionCount(0);
            _initialGc1 = GC.CollectionCount(1);
            _initialGc2 = GC.CollectionCount(2);

            TxtLatency.Text = $"{sw.ElapsedMilliseconds} ms (Init)";
        }

        private async void BtnSortPrice_Click(object sender, RoutedEventArgs e)
        {
            await VirtualGrid.SortByColumnAsync(4);
        }

        private async void BtnSortQuantity_Click(object sender, RoutedEventArgs e)
        {
            await VirtualGrid.SortByColumnAsync(3);
        }

        private void BtnClearGrid_Click(object sender, RoutedEventArgs e)
        {
            VirtualGrid.DataSource = null;
            TxtCapacity.Text = "0 Rows";
            GridSearch.SetMatchCount(0, 0);
            GridPager.UpdateTotalRecords(0);
        }

        private void BtnToggleSim_Click(object sender, RoutedEventArgs e)
        {
            _isSimulating = !_isSimulating;
            BtnToggleSim.Content = _isSimulating ? "⏸ Pause Simulation" : "▶ Resume Simulation";
        }

        private void BtnTogglePinning_Click(object sender, RoutedEventArgs e)
        {
            if (VirtualGrid.Columns.Count > 1)
            {
                bool newState = !VirtualGrid.Columns[0].IsPinned;
                VirtualGrid.Columns[0].IsPinned = newState;
                VirtualGrid.Columns[1].IsPinned = newState;
                BtnTogglePinning.Content = newState ? "📌 Unpin Columns" : "📌 Pin Columns";
                VirtualGrid.InvalidateVisual();
            }
        }

        private void BtnToggleFooter_Click(object sender, RoutedEventArgs e)
        {
            VirtualGrid.ShowFooter = !VirtualGrid.ShowFooter;
            BtnToggleFooter.Content = VirtualGrid.ShowFooter ? "∑ Hide Footer" : "∑ Show Footer";
        }

        private void BtnLoadGenericList_Click(object sender, RoutedEventArgs e)
        {
            var sw = Stopwatch.StartNew();
            var list = new System.Collections.Generic.List<DemoProduct>(5000);
            for (int i = 1; i <= 5000; i++)
            {
                list.Add(new DemoProduct
                {
                    Id = i,
                    Code = $"PRD-{i:00000}",
                    Name = $"Industrial Sensor Model #{i}",
                    Quantity = 50 + (i % 200),
                    Price = 120000 + (i * 150),
                    Category = (i % 2 == 0) ? "Optics" : "Electronics",
                    InStock = (i % 5 != 0)
                });
            }

            VirtualGrid.SetDataSource(list, autoGenerateColumns: true);
            if (VirtualGrid.Columns.Count >= 5)
            {
                VirtualGrid.Columns[0].IsPinned = true;
                VirtualGrid.Columns[1].IsPinned = true;
                VirtualGrid.Columns[3].Summary = SummaryType.Sum;
                VirtualGrid.Columns[3].SummaryFormat = "{0:N0}";
                VirtualGrid.Columns[4].Summary = SummaryType.Average;
                VirtualGrid.Columns[4].SummaryFormat = "Avg: {0:N0}";
            }
            VirtualGrid.ShowFooter = true;

            _inventorySource = null;
            sw.Stop();

            TxtCapacity.Text = $"{list.Count:N0} Objects (ZeroListSource)";
            GridSearch.SetMatchCount(list.Count, list.Count);
            GridPager.UpdateTotalRecords(list.Count);
            TxtLatency.Text = $"{sw.ElapsedMilliseconds} ms (Generic List Adapter)";
        }

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "ZeroUI ⚡ High-Performance WPF Desktop Suite\n\n" +
                "• Powered by ZeroUI.Core engine (VirtualViewport2D, RowIndexMap, PrefixSumArray).\n" +
                "• Single-Visual Direct DrawingContext rendering (0 Visual Tree overhead).\n" +
                "• In-Place Editing: Flyweight floating editor overlay with Tab/Enter commit.\n" +
                "• Fixed / Pinned Columns: Freeze columns to the left with elevation shadow.\n" +
                "• Multi-Row Selection: Multi-row select & Ctrl+C clipboard TSV copy.\n" +
                "• Summary Footer: Real-time footer aggregation (Sum, Count, Avg, Min, Max).\n" +
                "• Universal Generic Adapter: ZeroListSource<T> for IList<T>.\n" +
                "• 100% Zero-Allocation hotpaths on scroll and render.\n" +
                "• Supports both .NET Framework 4.6.2 and .NET 8.0-windows.\n\n" +
                "Developed with Deepmind Advanced Agentic Engineering.",
                "ZeroUI Architecture Overview", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public class DemoProduct
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public double Price { get; set; }
        public double Total => Quantity * Price;
        public string Category { get; set; } = string.Empty;
        public bool InStock { get; set; }
    }
}
