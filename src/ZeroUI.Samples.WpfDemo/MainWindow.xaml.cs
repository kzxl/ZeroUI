using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ZeroUI.Core.Common;
using ZeroUI.Core.Data;
using ZeroUI.Core.Input.Date;
using ZeroUI.Core.Scene;
using ZeroUI.Core.Theme;
using ZeroUI.Samples.WpfDemo.Data;
using ZeroUI.Wpf.Charts.Model;
using ZeroUI.Wpf.Editors;
using ZeroUI.Wpf.Industrial;
using ZeroUI.Wpf.Docking;
using ZeroUI.Wpf.Diagram;
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
            SetupEnterpriseControls();
            SetupPivotGrid();
            SetupGanttChart();
            SetupDockAndDiagram();

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

            // Populate Radio Group options
            RadioDispatchMode.Items = new[] { "Standard JIT Dispatch", "High-Priority Hotlot", "Hold for Quality Audit" };
            RadioSamplingTier.Items = new[] { "Level I (10%)", "Level II (Normal 100%)", "Level III (Tightened)" };

            // Populate Segmented options
            SegPeriod.Items = new[] { "Real-Time 1s", "Shift 8h", "Daily", "Weekly", "Quarterly" };
            SegFilter.Items = new[] { "All Units", "Running", "Degraded", "Faulted" };

            // Setup DatePickers
            DatePickerStart.SelectedDate = DateTime.Today;
            DatePickerEnd.SelectedDate = DateTime.Today.AddDays(14);

            // Setup DateRangePicker
            DemoDateRangePicker.SetRange(DateTime.Today.AddDays(-7), DateTime.Today);

            // Populate Lookup with 5,000 industrial items
            var lookupItems = new List<ZeroLookupItem>(5000);
            for (int i = 1; i <= 5000; i++)
            {
                lookupItems.Add(new ZeroLookupItem(
                    key: $"AST-{i:D5}",
                    displayText: $"Transducer Transmitter PT-{i:D4}",
                    subText: $"Building {((i % 5) + 1)} • Line {((char)('A' + (i % 6)))} • Modbus ID {i % 254 + 1}",
                    category: i % 10 == 0 ? "Calibrated" : (i % 7 == 0 ? "Inspect" : "Active")
                ));
            }
            DemoAssetLookup.SetItems(lookupItems);

            // Setup SCADA Plant Mimic Scene
            var plantScene = new ZeroScene();
            var tk1 = ZeroSceneNode.CreateTank("TK-101", "Raw Chemical TK-101", 60, 50, 95, 140);
            tk1.Value = 76.5;
            tk1.State = ScadaNodeState.Running;
            plantScene.AddNode(tk1);

            var tk2 = ZeroSceneNode.CreateTank("TK-102", "Catalyst Mix TK-102", 320, 50, 95, 140);
            tk2.Value = 44.0;
            tk2.State = ScadaNodeState.Running;
            plantScene.AddNode(tk2);

            var tk3 = ZeroSceneNode.CreateTank("TK-103", "Finished Yield TK-103", 580, 50, 95, 140);
            tk3.Value = 89.2;
            tk3.State = ScadaNodeState.Warning;
            plantScene.AddNode(tk3);

            var p1 = ZeroSceneNode.CreatePump("P-101A", "Primary Feed P-101A", 205, 105, 24);
            p1.Value = 1450;
            p1.State = ScadaNodeState.Running;
            plantScene.AddNode(p1);

            var p2 = ZeroSceneNode.CreatePump("P-102A", "Transfer Pump P-102A", 465, 105, 24);
            p2.Value = 1780;
            p2.State = ScadaNodeState.Running;
            plantScene.AddNode(p2);

            var v1 = ZeroSceneNode.CreateValve("XV-101", "Inlet Valve XV-101", 210, 65);
            v1.State = ScadaNodeState.Running;
            plantScene.AddNode(v1);

            var v2 = ZeroSceneNode.CreateValve("XV-102", "Transfer Valve XV-102", 470, 65);
            v2.State = ScadaNodeState.Running;
            plantScene.AddNode(v2);

            var tt1 = ZeroSceneNode.CreateSensor("TT-101", "Tank Temp 68.5°C", 72, 210, "°C");
            tt1.Value = 68.5;
            plantScene.AddNode(tt1);

            var pt2 = ZeroSceneNode.CreateSensor("PT-102", "Line Press 4.2 Bar", 332, 210, "Bar");
            pt2.Value = 4.2;
            plantScene.AddNode(pt2);

            PlantCanvas.Scene = plantScene;
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
            VirtualGrid.Columns.Clear();

            var colActive = new ZeroColumn("Active", 65, CellAlignment.Center)
            {
                ColumnType = GridColumnType.Boolean,
                Summary = SummaryType.Count,
                SummaryFormat = "{0:N0} total"
            };
            var colCategory = new ZeroColumn("Category", 145, CellAlignment.Left) { AllowGrouping = true };
            var colId = new ZeroColumn("ID", 75, CellAlignment.Right) { ReadOnly = true, IsPinned = true };
            var colCode = new ZeroColumn("Material Code", 125, CellAlignment.Left) { ReadOnly = true, IsPinned = true };
            var colName = new ZeroColumn("Description / Component Name", 230, CellAlignment.Left);
            var colQty = new ZeroColumn("Quantity", 90, CellAlignment.Right)
            {
                ColumnType = GridColumnType.Numeric,
                Summary = SummaryType.Sum,
                SummaryFormat = "{0:N0}",
                CustomValidator = val =>
                {
                    if (int.TryParse(val.Replace(",", ""), out int q) && q >= 0 && q <= 100000)
                        return (true, null);
                    return (false, "Quantity must be an integer between 0 and 100,000");
                }
            };
            var colPrice = new ZeroColumn("Unit Price ($)", 115, CellAlignment.Right)
            {
                ColumnType = GridColumnType.Numeric,
                Summary = SummaryType.Average,
                SummaryFormat = "Avg: ${0:N2}",
                CustomValidator = val =>
                {
                    if (double.TryParse(val.Replace(",", ""), out double p) && p >= 0)
                        return (true, null);
                    return (false, "Unit Price must be non-negative");
                }
            };
            var colTotal = new ZeroColumn("Total Amount ($)", 135, CellAlignment.Right) { ReadOnly = true, Summary = SummaryType.Sum, SummaryFormat = "${0:N2}" };
            var colYield = new ZeroColumn("Yield %", 120, CellAlignment.Center)
            {
                ColumnType = GridColumnType.Numeric,
                Summary = SummaryType.Average,
                SummaryFormat = "Avg: {0:P0}"
            };
            var colLot = new ZeroColumn("Lot Number", 120, CellAlignment.Center)
            {
                ColumnType = GridColumnType.Masked,
                Mask = "LOT-000000",
                CustomValidator = val =>
                {
                    if (string.IsNullOrWhiteSpace(val) || val.Contains("_"))
                        return (false, "Lot number must have 6 digits (e.g. LOT-202601)");
                    return (true, null);
                }
            };
            var colStatus = new ZeroColumn("Inspection Status", 135, CellAlignment.Center);

            VirtualGrid.Columns.Add(colActive);
            VirtualGrid.Columns.Add(colCategory);
            VirtualGrid.Columns.Add(colId);
            VirtualGrid.Columns.Add(colCode);
            VirtualGrid.Columns.Add(colName);
            VirtualGrid.Columns.Add(colQty);
            VirtualGrid.Columns.Add(colPrice);
            VirtualGrid.Columns.Add(colTotal);
            VirtualGrid.Columns.Add(colYield);
            VirtualGrid.Columns.Add(colLot);
            VirtualGrid.Columns.Add(colStatus);

            VirtualGrid.ShowFooter = true;
            VirtualGrid.ShowGroupPanel = true;
            VirtualGrid.SelectionMode = ZeroGridSelectionMode.MultiRow;

            // Enterprise Group Summaries
            VirtualGrid.GroupSummaries.Add(new GroupSummaryItem(-1, GroupSummaryType.Count, null, "Items"));
            VirtualGrid.GroupSummaries.Add(new GroupSummaryItem(5, GroupSummaryType.Sum, "{0:N0}", "Total Qty"));
            VirtualGrid.GroupSummaries.Add(new GroupSummaryItem(6, GroupSummaryType.Average, "${0:N2}", "Avg Price"));
            VirtualGrid.GroupSummaries.Add(new GroupSummaryItem(7, GroupSummaryType.Sum, "${0:N2}", "Total Amount"));

            // Conditional Formatting Rule: Highlight high quantity
            VirtualGrid.ConditionalRules.Add(new ConditionalFormattingRule(
                columnIndex: 5,
                op: ConditionOperator.GreaterThan,
                value1: 400,
                backColor: 0x4010B981,
                textColor: 0xFF34D399
            ));

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
                    items[i].Category.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
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

            BtnGroupCategory.Background = ZeroWpfTheme.BgInput;
            BtnGroupCategory.Foreground = ZeroWpfTheme.TextPrimary;

            BtnClearGrouping.Background = ZeroWpfTheme.BgInput;
            BtnClearGrouping.Foreground = ZeroWpfTheme.TextPrimary;

            BtnToggleExpand.Background = ZeroWpfTheme.BgInput;
            BtnToggleExpand.Foreground = ZeroWpfTheme.TextPrimary;

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

            BtnToggleGroupPanel.Background = ZeroWpfTheme.BgInput;
            BtnToggleGroupPanel.Foreground = ZeroWpfTheme.TextPrimary;

            BtnToggleSummaries.Background = ZeroWpfTheme.BgInput;
            BtnToggleSummaries.Foreground = ZeroWpfTheme.TextPrimary;

            BtnRefreshPivot.Background = ZeroWpfTheme.PrimaryAccent;
            BtnRefreshPivot.Foreground = ZeroWpfTheme.SelectionForeground;

            BtnGanttZoomIn.Background = ZeroWpfTheme.BgInput;
            BtnGanttZoomIn.Foreground = ZeroWpfTheme.TextPrimary;

            BtnGanttZoomOut.Background = ZeroWpfTheme.BgInput;
            BtnGanttZoomOut.Foreground = ZeroWpfTheme.TextPrimary;

            BtnGanttAddDemo.Background = ZeroWpfTheme.PrimaryAccent;
            BtnGanttAddDemo.Foreground = ZeroWpfTheme.SelectionForeground;

            BtnAddNode.Background = ZeroWpfTheme.PrimaryAccent;
            BtnAddNode.Foreground = ZeroWpfTheme.SelectionForeground;

            BtnResetDiagram.Background = ZeroWpfTheme.BgInput;
            BtnResetDiagram.Foreground = ZeroWpfTheme.TextPrimary;

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

        private void BtnGroupCategory_Click(object sender, RoutedEventArgs e)
        {
            VirtualGrid.GroupBy(1);
            TxtLatency.Text = $"Grouped by Category ({VirtualGrid.VisualRowCount:N0} visual rows)";
        }

        private void BtnClearGrouping_Click(object sender, RoutedEventArgs e)
        {
            VirtualGrid.ClearGrouping();
            TxtLatency.Text = $"Grouping cleared ({VirtualGrid.VisualRowCount:N0} rows)";
        }

        private void BtnToggleGroupPanel_Click(object sender, RoutedEventArgs e)
        {
            VirtualGrid.ShowGroupPanel = !VirtualGrid.ShowGroupPanel;
            TxtLatency.Text = $"Group Panel {(VirtualGrid.ShowGroupPanel ? "Shown" : "Hidden")}";
        }

        private void BtnToggleSummaries_Click(object sender, RoutedEventArgs e)
        {
            if (VirtualGrid.GroupSummaries.Count > 0)
            {
                VirtualGrid.GroupSummaries.Clear();
                TxtLatency.Text = "Group summaries cleared";
            }
            else
            {
                VirtualGrid.GroupSummaries.Add(new GroupSummaryItem(-1, GroupSummaryType.Count, null, "Items"));
                VirtualGrid.GroupSummaries.Add(new GroupSummaryItem(5, GroupSummaryType.Sum, "{0:N0}", "Total Qty"));
                VirtualGrid.GroupSummaries.Add(new GroupSummaryItem(6, GroupSummaryType.Average, "${0:N2}", "Avg Price"));
                VirtualGrid.GroupSummaries.Add(new GroupSummaryItem(7, GroupSummaryType.Sum, "${0:N2}", "Total Amount"));
                TxtLatency.Text = "Group summaries enabled";
            }
            VirtualGrid.RecalculateGroupSummaries();
            VirtualGrid.InvalidateVisual();
        }

        private bool _allGroupsExpanded = true;
        private void BtnToggleExpand_Click(object sender, RoutedEventArgs e)
        {
            if (_allGroupsExpanded)
            {
                VirtualGrid.CollapseAllGroups();
                _allGroupsExpanded = false;
                BtnToggleExpand.Content = "➕ Expand All";
            }
            else
            {
                VirtualGrid.ExpandAllGroups();
                _allGroupsExpanded = true;
                BtnToggleExpand.Content = "➖ Collapse All";
            }
        }

        private async void BtnSortPrice_Click(object sender, RoutedEventArgs e)
        {
            await VirtualGrid.SortByColumnAsync(6);
        }

        private async void BtnSortQuantity_Click(object sender, RoutedEventArgs e)
        {
            await VirtualGrid.SortByColumnAsync(5);
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
            if (VirtualGrid.Columns.Count > 3)
            {
                bool newState = !VirtualGrid.Columns[2].IsPinned;
                VirtualGrid.Columns[2].IsPinned = newState;
                VirtualGrid.Columns[3].IsPinned = newState;
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

        private bool _isBanded = false;

        private void BtnBandedHeaders_Click(object sender, RoutedEventArgs e)
        {
            _isBanded = !_isBanded;
            VirtualGrid.Bands.Clear();

            if (_isBanded && VirtualGrid.Columns.Count >= 7)
            {
                // Band 1: Master Identification (cols 0, 1)
                var bandMaster = new GridBand("📦 Product Master & Identification");
                bandMaster.AddColumn(VirtualGrid.Columns[0]);
                bandMaster.AddColumn(VirtualGrid.Columns[1]);

                // Band 2: Commercial & Inventory (cols 2, 3, 4, 5)
                var bandCommercial = new GridBand("💰 Commercial & Inventory Metrics");
                bandCommercial.AddColumn(VirtualGrid.Columns[2]);
                bandCommercial.AddColumn(VirtualGrid.Columns[3]);
                bandCommercial.AddColumn(VirtualGrid.Columns[4]);
                bandCommercial.AddColumn(VirtualGrid.Columns[5]);

                // Band 3: Categorization & Status (cols 6..)
                var bandCategory = new GridBand("🏷️ Categorization");
                for (int i = 6; i < VirtualGrid.Columns.Count; i++)
                {
                    bandCategory.AddColumn(VirtualGrid.Columns[i]);
                }

                VirtualGrid.Bands.Add(bandMaster);
                VirtualGrid.Bands.Add(bandCommercial);
                VirtualGrid.Bands.Add(bandCategory);

                if (VirtualGrid.Columns.Count > 6)
                {
                    VirtualGrid.Columns[6].AllowCellMerge = true;
                }

                BtnBandedHeaders.Content = "🏛️ Multi-Tier Bands: ON";
            }
            else
            {
                if (VirtualGrid.Columns.Count > 6)
                {
                    VirtualGrid.Columns[6].AllowCellMerge = false;
                }
                BtnBandedHeaders.Content = "🏛️ Multi-Tier Bands";
            }

            VirtualGrid.InvalidateVisual();
        }

        private void BtnBlockSelection_Click(object sender, RoutedEventArgs e)
        {
            if (VirtualGrid.SelectionMode == ZeroGridSelectionMode.Block)
            {
                VirtualGrid.SelectionMode = ZeroGridSelectionMode.SingleRow;
                BtnBlockSelection.Content = "📐 Block Marquee";
            }
            else
            {
                VirtualGrid.SelectionMode = ZeroGridSelectionMode.Block;
                BtnBlockSelection.Content = "📐 Block Marquee: ON";
            }
        }

        private void BtnExpandAllTree_Click(object sender, RoutedEventArgs e)
        {
            DemoTreeList.Model.ExpandAll();
            DemoTreeList.InvalidateVisual();
        }

        private void BtnCollapseAllTree_Click(object sender, RoutedEventArgs e)
        {
            DemoTreeList.Model.CollapseAll();
            DemoTreeList.InvalidateVisual();
        }

        private void SetupEnterpriseControls()
        {
            // Configure DemoTreeList columns
            DemoTreeList.Columns.Add(new ZeroColumn("Component / Asset", 210, CellAlignment.Left));
            DemoTreeList.Columns.Add(new ZeroColumn("Asset Tag", 85, CellAlignment.Center));
            DemoTreeList.Columns.Add(new ZeroColumn("Status", 80, CellAlignment.Center));
            DemoTreeList.Columns.Add(new ZeroColumn("OEE %", 75, CellAlignment.Right));
            DemoTreeList.Columns.Add(new ZeroColumn("Next Maintenance", 115, CellAlignment.Center));

            // Populate hierarchical model
            var model = new ZeroTreeModel();

            // Root 1: Gigafactory
            var root1 = new ZeroTreeNode("🏭 North American Gigafactory", "FAC-001", "Online", "94.2%", "2026-11-15");
            var line1 = new ZeroTreeNode("⚙️ Assembly Line 01 (Welding)", "LNE-010", "Running", "96.5%", "2026-10-01");
            line1.AddChild(new ZeroTreeNode("🦾 KUKA Titan Welder Arm", "ROB-101", "Running", "98.1%", "2026-09-20"));
            line1.AddChild(new ZeroTreeNode("🦾 Fanuc M-20iA Feed Cell", "ROB-102", "Running", "97.4%", "2026-09-28"));
            line1.AddChild(new ZeroTreeNode("📷 Cognex In-Sight 3D Vision", "CAM-105", "Running", "99.2%", "2026-12-10"));

            var line2 = new ZeroTreeNode("📦 Packaging & Case Packing", "LNE-020", "Standby", "91.8%", "2026-09-18");
            line2.AddChild(new ZeroTreeNode("🤖 Omron Delta High-Speed Robot", "ROB-201", "Standby", "93.4%", "2026-09-22"));
            line2.AddChild(new ZeroTreeNode("🌀 Automated Stretch Wrapper", "WRP-202", "Running", "95.0%", "2026-10-15"));

            root1.AddChild(line1);
            root1.AddChild(line2);
            model.AddRoot(root1);

            // Root 2: European Distribution Hub
            var root2 = new ZeroTreeNode("🌐 European Logistics Center", "FAC-002", "Online", "88.9%", "2026-10-30");
            var asrs = new ZeroTreeNode("🏗️ High-Bay ASRS Storage Bay A", "ASRS-01", "Running", "99.5%", "2026-12-01");
            asrs.AddChild(new ZeroTreeNode("🪜 Dual-Mast Crane Stacker 01", "CRN-301", "Running", "99.1%", "2026-11-10"));
            asrs.AddChild(new ZeroTreeNode("🪜 Dual-Mast Crane Stacker 02", "CRN-302", "Running", "98.7%", "2026-11-12"));

            var agvFleet = new ZeroTreeNode("🚜 Autonomous Mobile Robots (AMR)", "AMR-GRP", "Running", "86.4%", "2026-09-15");
            agvFleet.AddChild(new ZeroTreeNode("🤖 Tugger AMR Unit #04", "AMR-004", "Charging", "82.0%", "2026-09-16"));
            agvFleet.AddChild(new ZeroTreeNode("🤖 Forklift AMR Unit #09", "AMR-009", "Running", "91.5%", "2026-09-25"));

            root2.AddChild(asrs);
            root2.AddChild(agvFleet);
            model.AddRoot(root2);

            DemoTreeList.Model = model;

            // PropertyGrid initial object
            var cellConfig = new RoboticCellConfig
            {
                CellName = "KUKA Titan Heavy Welder ROB-101",
                AssetTag = "AST-ROB-101",
                ControllerIp = "192.168.1.101",
                ModbusPort = 502,
                TargetCycleTimeSec = 14.5,
                MaxPayloadKg = 1000.0,
                IsSafetyCurtainActive = true,
                AutoRestartOnClear = false,
                EmergencyStopTripped = false,
                OperatingMode = "Continuous Auto-Weld"
            };
            DemoPropertyGrid.SelectedObject = cellConfig;

            // Wire up TreeList selection to update PropertyGrid and Breadcrumb
            DemoTreeList.NodeSelected += (s, node) =>
            {
                // Build Breadcrumb path
                var pathStack = new List<string>();
                var curr = node;
                while (curr != null)
                {
                    string rawText = curr.GetValue(0);
                    int spIdx = rawText.IndexOf(' ');
                    pathStack.Insert(0, spIdx >= 0 ? rawText.Substring(spIdx + 1) : rawText);
                    curr = curr.Parent;
                }
                DemoBreadcrumb.Path = string.Join(" / ", pathStack);

                // Update PropertyGrid
                var nodeConfig = new RoboticCellConfig
                {
                    CellName = node.GetValue(0),
                    AssetTag = node.GetValue(1),
                    OperatingMode = node.GetValue(2),
                    TargetCycleTimeSec = 12.0,
                    IsSafetyCurtainActive = true
                };
                DemoPropertyGrid.SelectedObject = nodeConfig;
            };
        }

        #region Pivot Grid OLAP Setup

        private void SetupPivotGrid()
        {
            ComboPivotAgg.ItemsSource = new[] { "Sum of Revenue", "Average Revenue", "Count of Orders" };
            ComboPivotAgg.SelectedIndex = 0;
            RebuildPivotData(GroupSummaryType.Sum);
        }

        private void RebuildPivotData(GroupSummaryType summaryType)
        {
            DemoPivotGrid.Engine.RowDimensions.Clear();
            DemoPivotGrid.Engine.ColumnDimensions.Clear();
            DemoPivotGrid.Engine.Measures.Clear();

            DemoPivotGrid.Engine.RowDimensions.Add(new PivotDimension(0, "Region"));
            DemoPivotGrid.Engine.ColumnDimensions.Add(new PivotDimension(1, "Quarter"));

            string formatStr = summaryType == GroupSummaryType.Count ? "{0:N0}" : "${0:N2}";
            DemoPivotGrid.Engine.Measures.Add(new PivotMeasure(2, "Revenue", summaryType, formatStr));

            // Generate sample regional sales data
            string[] regions = { "North America", "Europe EMEA", "Asia-Pacific", "Latin America", "Middle East" };
            string[] quarters = { "Q1 2026", "Q2 2026", "Q3 2026", "Q4 2026" };

            var rawRows = new List<(string Region, string Quarter, double Amount)>();
            var rand = new Random(101);

            foreach (var reg in regions)
            {
                foreach (var qtr in quarters)
                {
                    int orders = rand.Next(10, 30);
                    for (int i = 0; i < orders; i++)
                    {
                        double amount = rand.Next(1200, 85000);
                        rawRows.Add((reg, qtr, amount));
                    }
                }
            }

            DemoPivotGrid.Engine.Compute(rawRows.Count,
                (r, field) => field == 0 ? rawRows[r].Region : (field == 1 ? rawRows[r].Quarter : rawRows[r].Amount.ToString()),
                (r, field) => field == 2 ? rawRows[r].Amount : 1.0);

            DemoPivotGrid.RefreshData();
        }

        private void ComboPivotAgg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DemoPivotGrid == null || ComboPivotAgg == null) return;
            GroupSummaryType agg = ComboPivotAgg.SelectedIndex switch
            {
                1 => GroupSummaryType.Average,
                2 => GroupSummaryType.Count,
                _ => GroupSummaryType.Sum
            };
            RebuildPivotData(agg);
        }

        private void BtnRefreshPivot_Click(object sender, RoutedEventArgs e)
        {
            GroupSummaryType agg = ComboPivotAgg.SelectedIndex switch
            {
                1 => GroupSummaryType.Average,
                2 => GroupSummaryType.Count,
                _ => GroupSummaryType.Sum
            };
            RebuildPivotData(agg);
        }

        #endregion

        #region Gantt Chart Setup

        private void SetupGanttChart()
        {
            DemoGanttChart.ProjectStart = DateTime.Today.AddDays(-3);
            DemoGanttChart.ProjectEnd = DateTime.Today.AddDays(30);
            DemoGanttChart.PixelsPerDay = 38.0;

            var baseDate = DateTime.Today;

            var t1 = new GanttTaskItem(1, "1. Process Safety & Hazard Audit", baseDate.AddDays(-2), baseDate.AddDays(3), 1.0f, false, "HSE Team") { BarColor = 0xFF10B981 };
            var t2 = new GanttTaskItem(2, "2. SMT Stencil & Tooling Setup", baseDate.AddDays(2), baseDate.AddDays(7), 0.85f, false, "Tooling Lead") { BarColor = 0xFF3B82F6 };
            t2.PredecessorIds.Add(1);

            var t3 = new GanttTaskItem(3, "3. Pilot Lot SMT Component Placement", baseDate.AddDays(7), baseDate.AddDays(14), 0.50f, false, "Line Operator A") { BarColor = 0xFF8B5CF6 };
            t3.PredecessorIds.Add(2);

            var t4 = new GanttTaskItem(4, "4. Nitrogen Reflow Oven Thermal Profiling", baseDate.AddDays(12), baseDate.AddDays(18), 0.25f, false, "Thermal Specialist") { BarColor = 0xFFF59E0B };
            t4.PredecessorIds.Add(3);

            var t5 = new GanttTaskItem(5, "★ Factory Acceptance Gate (FAT)", baseDate.AddDays(19), baseDate.AddDays(19), 0.0f, true, "Executive Committee") { BarColor = 0xFFEF4444 };
            t5.PredecessorIds.Add(4);

            var t6 = new GanttTaskItem(6, "5. Mass Production Ramp & Packaging", baseDate.AddDays(20), baseDate.AddDays(28), 0.0f, false, "Shift Supervisor") { BarColor = 0xFF06B6D4 };
            t6.PredecessorIds.Add(5);

            DemoGanttChart.Tasks.Add(t1);
            DemoGanttChart.Tasks.Add(t2);
            DemoGanttChart.Tasks.Add(t3);
            DemoGanttChart.Tasks.Add(t4);
            DemoGanttChart.Tasks.Add(t5);
            DemoGanttChart.Tasks.Add(t6);
        }

        private void BtnGanttZoomIn_Click(object sender, RoutedEventArgs e)
        {
            DemoGanttChart.PixelsPerDay = Math.Min(100.0, DemoGanttChart.PixelsPerDay * 1.25);
        }

        private void BtnGanttZoomOut_Click(object sender, RoutedEventArgs e)
        {
            DemoGanttChart.PixelsPerDay = Math.Max(12.0, DemoGanttChart.PixelsPerDay * 0.8);
        }

        private void BtnGanttAddDemo_Click(object sender, RoutedEventArgs e)
        {
            int nextId = DemoGanttChart.Tasks.Count + 1;
            var lastTask = DemoGanttChart.Tasks.Count > 0 ? DemoGanttChart.Tasks[DemoGanttChart.Tasks.Count - 1] : null;
            DateTime start = lastTask != null ? lastTask.EndDate.AddDays(1) : DateTime.Today;
            DateTime end = start.AddDays(5);

            var newTask = new GanttTaskItem(nextId, $"Task {nextId}: Quality Burn-in & Aging", start, end, 0.1f, false, "QC Team") { BarColor = 0xFF6366F1 };
            if (lastTask != null) newTask.PredecessorIds.Add(lastTask.Id);

            DemoGanttChart.Tasks.Add(newTask);
        }

        #endregion

        #region Dock Manager & P&ID Diagram Setup

        private ZeroDiagramCanvas? _diagramCanvas;

        private void SetupDockAndDiagram()
        {
            // Left Toolbox Panel
            var leftPanel = new ZeroDockPanel { Title = "Toolbox & Asset Library", DockPosition = ZeroDockPosition.Left };
            var toolboxStack = new StackPanel { Margin = new Thickness(12) };
            toolboxStack.Children.Add(new TextBlock { Text = "📐 Process Library", FontWeight = FontWeights.Bold, Foreground = ZeroWpfTheme.TextPrimary, Margin = new Thickness(0, 0, 0, 8) });
            toolboxStack.Children.Add(new TextBlock { Text = "• Primary Buffer Tank (TK-101)\n• Centrifugal Feed Pump (P-201)\n• Proportional Control Valve (XV-301)\n• Exothermic Reactor Vessel (RX-401)\n• RTD Temperature Sensor (TE-501)", Foreground = ZeroWpfTheme.TextSecondary, LineHeight = 20 });
            leftPanel.Content = toolboxStack;
            DemoDockManager.AddPanel(leftPanel);

            // Center Document: ZeroDiagramCanvas
            var centerDoc = new ZeroDockPanel { Title = "P&ID Process Diagram Loop", DockPosition = ZeroDockPosition.Document };
            _diagramCanvas = new ZeroDiagramCanvas();
            centerDoc.Content = _diagramCanvas;
            DemoDockManager.AddPanel(centerDoc);

            // Bottom Output Panel
            var bottomPanel = new ZeroDockPanel { Title = "Output & Fieldbus Telemetry", DockPosition = ZeroDockPosition.Bottom };
            var outputBox = new TextBox
            {
                IsReadOnly = true,
                Background = ZeroWpfTheme.BgInput,
                Foreground = ZeroWpfTheme.TextSecondary,
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11.5,
                Text = "[SYS] Process loop initialized. Baud: 115200. OPC-UA server listening on opc.tcp://10.0.1.50:4840\n[DIAG] Single-Visual Diagram engine ready. Drag nodes or connect port dots to route pipes."
            };
            bottomPanel.Content = outputBox;
            DemoDockManager.AddPanel(bottomPanel);

            // Right Properties Panel
            var rightPanel = new ZeroDockPanel { Title = "Node Inspector", DockPosition = ZeroDockPosition.Right };
            var rightStack = new StackPanel { Margin = new Thickness(12) };
            var lblInspector = new TextBlock { Text = "Select a diagram node to inspect parameters", TextWrapping = TextWrapping.Wrap, Foreground = ZeroWpfTheme.TextMuted };
            rightStack.Children.Add(lblInspector);
            rightPanel.Content = rightStack;
            DemoDockManager.AddPanel(rightPanel);

            // Connect diagram selection event to update inspector
            _diagramCanvas.NodeSelected += (s, node) =>
            {
                if (node != null)
                {
                    lblInspector.Text = $"Node ID: {node.Id}\nTitle: {node.Title}\nRole: {node.Subtitle}\nCoordinates: ({node.X:0}, {node.Y:0})\nPorts: {node.Ports.Count}";
                }
                else
                {
                    lblInspector.Text = "No node selected. Click a node to view properties.";
                }
            };

            ResetDiagramContent();
        }

        private void ResetDiagramContent()
        {
            if (_diagramCanvas == null) return;
            _diagramCanvas.Nodes.Clear();
            _diagramCanvas.Connections.Clear();

            var nodeTank = new DiagramNode("tank1", "Buffer Tank TK-101", "Raw Material Feed", 40, 60, Color.FromRgb(59, 130, 246), "🛢");
            var nodePump = new DiagramNode("pump1", "Feed Pump P-201", "Centrifugal 45kW", 240, 60, Color.FromRgb(16, 185, 129), "⚙");
            var nodeValve = new DiagramNode("valve1", "Pneumatic Valve XV-301", "Linear Throttle", 440, 60, Color.FromRgb(245, 158, 11), "🚰");
            var nodeReactor = new DiagramNode("reactor1", "Batch Reactor RX-401", "Exothermic Mixing", 640, 60, Color.FromRgb(239, 68, 68), "⚡");
            var nodeSensor = new DiagramNode("sensor1", "Temp Sensor TE-501", "RTD Dual Pt100", 640, 190, Color.FromRgb(139, 92, 246), "📡");

            _diagramCanvas.Nodes.Add(nodeTank);
            _diagramCanvas.Nodes.Add(nodePump);
            _diagramCanvas.Nodes.Add(nodeValve);
            _diagramCanvas.Nodes.Add(nodeReactor);
            _diagramCanvas.Nodes.Add(nodeSensor);

            _diagramCanvas.Connections.Add(new DiagramConnection("tank1", "out", "pump1", "in", "Slurry Feed"));
            _diagramCanvas.Connections.Add(new DiagramConnection("pump1", "out", "valve1", "in", "14.5 Bar"));
            _diagramCanvas.Connections.Add(new DiagramConnection("valve1", "out", "reactor1", "in", "Rate: 85 L/min"));
            _diagramCanvas.Connections.Add(new DiagramConnection("reactor1", "out", "sensor1", "in", "Thermal feedback"));
        }

        private void BtnAddNode_Click(object sender, RoutedEventArgs e)
        {
            if (_diagramCanvas == null) return;
            int count = _diagramCanvas.Nodes.Count + 1;
            var newNode = new DiagramNode($"custom{count}", $"Station #{count}", "Sub-process Unit", 180 + (count * 20), 180, Color.FromRgb(14, 165, 233), "🔄");
            _diagramCanvas.Nodes.Add(newNode);
        }

        private void BtnResetDiagram_Click(object sender, RoutedEventArgs e)
        {
            ResetDiagramContent();
        }

        #endregion
    }

    public class RoboticCellConfig
    {
        [Category("1. Identification")]
        [DisplayName("Asset Cell Name")]
        [Description("Descriptive human-readable identifier of the automated machinery or work cell.")]
        public string CellName { get; set; } = "Workstation Cell";

        [Category("1. Identification")]
        [DisplayName("Asset Tag Code")]
        [Description("Global enterprise asset barcode / ERP registration identifier.")]
        public string AssetTag { get; set; } = "AST-001";

        [Category("2. Industrial Network")]
        [DisplayName("Fieldbus IP Address")]
        [Description("IPv4 static endpoint assigned on the machine OT subnet.")]
        public string ControllerIp { get; set; } = "192.168.1.100";

        [Category("2. Industrial Network")]
        [DisplayName("Modbus TCP Port")]
        [Description("Port number for Modbus or OPC-UA fieldbus telemetry ingestion.")]
        public int ModbusPort { get; set; } = 502;

        [Category("3. Process Kinematics")]
        [DisplayName("Target Cycle (sec)")]
        [Description("Expected takt time per finished workpiece assembly in seconds.")]
        public double TargetCycleTimeSec { get; set; } = 15.0;

        [Category("3. Process Kinematics")]
        [DisplayName("Max Rated Payload (kg)")]
        [Description("Maximum allowable mechanical lifting payload on robot end effector.")]
        public double MaxPayloadKg { get; set; } = 500.0;

        [Category("3. Process Kinematics")]
        [DisplayName("Operational Mode")]
        [Description("Current supervisory state machine operation mode.")]
        public string OperatingMode { get; set; } = "Automatic";

        [Category("4. Safety & Interlocks")]
        [DisplayName("Optoelectronic Light Curtain")]
        [Description("Active optical safety curtain barrier protecting human operator envelope.")]
        public bool IsSafetyCurtainActive { get; set; } = true;

        [Category("4. Safety & Interlocks")]
        [DisplayName("Auto Restart on Clear")]
        [Description("Automatically resume production cycle after safety zone breach reset.")]
        public bool AutoRestartOnClear { get; set; } = false;

        [Category("4. Safety & Interlocks")]
        [DisplayName("Emergency Stop Latched")]
        [Description("Hardware SIL-3 E-Stop circuit trip indicator.")]
        public bool EmergencyStopTripped { get; set; } = false;
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
