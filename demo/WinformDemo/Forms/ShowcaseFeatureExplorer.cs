using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ZeroUI.Core.Theme;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.Samples.WinformDemo.Forms
{
    public sealed class ShowcaseFeatureItem
    {
        public string Key { get; }
        public string Title { get; }
        public string Category { get; }
        public string Icon { get; }
        public string Badge { get; }
        public Color BadgeColor { get; }
        public string Description { get; }
        public string CodeSnippet { get; }

        public ShowcaseFeatureItem(string key, string title, string category, string icon, string badge, Color badgeColor, string description, string codeSnippet)
        {
            Key = key;
            Title = title;
            Category = category;
            Icon = icon;
            Badge = badge;
            BadgeColor = badgeColor;
            Description = description;
            CodeSnippet = codeSnippet;
        }
    }

    public sealed class ShowcaseFeatureExplorer : UserControl
    {
        private readonly TextBox _txtSearch;
        private readonly Panel _searchContainer;
        private readonly Panel _itemsPanel;
        private readonly List<ShowcaseFeatureItem> _allItems = new List<ShowcaseFeatureItem>();
        private readonly List<Control> _renderedRows = new List<Control>();
        private readonly ToolTip _toolTip;
        private string _selectedKey = string.Empty;

        public event Action<ShowcaseFeatureItem>? FeatureSelected;

        public ShowcaseFeatureExplorer()
        {
            DoubleBuffered = true;
            Dock = DockStyle.Left;
            Width = 340;
            BackColor = Color.FromArgb(248, 249, 251);
            Padding = new Padding(0);

            _toolTip = new ToolTip
            {
                InitialDelay = 350,
                ReshowDelay = 100,
                AutoPopDelay = 6000
            };

            // 1. Search Container
            _searchContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(240, 242, 245),
                Padding = new Padding(10, 8, 10, 8)
            };

            _txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(40, 44, 52)
            };
            _txtSearch.Text = "Type keywords here...";
            _txtSearch.ForeColor = Color.Gray;

            _txtSearch.GotFocus += (s, e) =>
            {
                if (_txtSearch.Text == "Type keywords here...")
                {
                    _txtSearch.Text = "";
                    _txtSearch.ForeColor = ZeroTheme.Colors.TextPrimary;
                }
            };

            _txtSearch.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_txtSearch.Text))
                {
                    _txtSearch.Text = "Type keywords here...";
                    _txtSearch.ForeColor = Color.Gray;
                }
            };

            _txtSearch.TextChanged += (s, e) =>
            {
                if (_txtSearch.Text != "Type keywords here...")
                {
                    FilterItems(_txtSearch.Text.Trim());
                }
            };

            var searchBoxWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(8, 4, 8, 4)
            };
            searchBoxWrapper.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(218, 222, 229));
                e.Graphics.DrawRectangle(pen, 0, 0, searchBoxWrapper.Width - 1, searchBoxWrapper.Height - 1);
            };
            searchBoxWrapper.Controls.Add(_txtSearch);
            _searchContainer.Controls.Add(searchBoxWrapper);

            // 2. Scrollable Items Panel
            _itemsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 249, 251),
                Padding = new Padding(0, 4, 0, 8)
            };

            Controls.Add(_itemsPanel);
            Controls.Add(_searchContainer);

            PopulateDefaultFeatures();
            RenderItems(_allItems);
        }

        public string SelectedKey => _selectedKey;

        public void SelectFeature(string key)
        {
            var item = _allItems.FirstOrDefault(i => i.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                _selectedKey = item.Key;
                HighlightSelectedRow();
                FeatureSelected?.Invoke(item);
            }
        }

        public void SelectNext()
        {
            int index = _allItems.FindIndex(i => i.Key == _selectedKey);
            if (index >= 0 && index < _allItems.Count - 1)
            {
                SelectFeature(_allItems[index + 1].Key);
            }
        }

        public void SelectPrevious()
        {
            int index = _allItems.FindIndex(i => i.Key == _selectedKey);
            if (index > 0)
            {
                SelectFeature(_allItems[index - 1].Key);
            }
        }

        private void FilterItems(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                RenderItems(_allItems);
            }
            else
            {
                var filtered = _allItems.Where(i =>
                    i.Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    i.Category.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    i.Badge.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    i.Description.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                RenderItems(filtered);
            }
        }

        private void RenderItems(List<ShowcaseFeatureItem> items)
        {
            _itemsPanel.SuspendLayout();
            _itemsPanel.Controls.Clear();
            _renderedRows.Clear();

            var groups = items.GroupBy(i => i.Category).ToList();

            foreach (var group in groups)
            {
                // Category Header
                var lblHeader = new Label
                {
                    Dock = DockStyle.Top,
                    Height = 28,
                    Text = group.Key.ToUpperInvariant(),
                    Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(120, 130, 145),
                    TextAlign = ContentAlignment.BottomLeft,
                    Padding = new Padding(12, 0, 0, 4)
                };
                _itemsPanel.Controls.Add(lblHeader);

                foreach (var item in group)
                {
                    var row = CreateItemRow(item);
                    _itemsPanel.Controls.Add(row);
                    _renderedRows.Add(row);
                }
            }

            // Reverse order for WinForms DockStyle.Top stacking
            var controls = _itemsPanel.Controls.Cast<Control>().ToList();
            _itemsPanel.Controls.Clear();
            for (int i = controls.Count - 1; i >= 0; i--)
            {
                _itemsPanel.Controls.Add(controls[i]);
            }

            _itemsPanel.ResumeLayout(true);
            HighlightSelectedRow();
        }

        private Control CreateItemRow(ShowcaseFeatureItem item)
        {
            var row = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                Tag = item,
                Cursor = Cursors.Hand,
                Padding = new Padding(10, 0, 6, 0)
            };

            bool isSelected = item.Key == _selectedKey;
            row.BackColor = isSelected ? Color.FromArgb(228, 236, 252) : Color.Transparent;

            var lblTitle = new Label
            {
                Dock = DockStyle.Fill,
                Text = $"{item.Icon}  {item.Title}",
                Font = new Font("Segoe UI", 9.25f, isSelected ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isSelected ? Color.FromArgb(18, 86, 209) : Color.FromArgb(33, 37, 41),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            Label? lblBadge = null;
            if (!string.IsNullOrEmpty(item.Badge))
            {
                lblBadge = new Label
                {
                    Dock = DockStyle.Right,
                    Width = 56,
                    Text = item.Badge,
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    ForeColor = item.BadgeColor,
                    TextAlign = ContentAlignment.MiddleRight,
                    Cursor = Cursors.Hand
                };
                row.Controls.Add(lblBadge);
            }

            row.Controls.Add(lblTitle);

            Action selectAction = () =>
            {
                _selectedKey = item.Key;
                HighlightSelectedRow();
                FeatureSelected?.Invoke(item);
            };

            row.Click += (s, e) => selectAction();
            lblTitle.Click += (s, e) => selectAction();
            if (lblBadge != null) lblBadge.Click += (s, e) => selectAction();

            row.MouseEnter += (s, e) =>
            {
                if (item.Key != _selectedKey)
                    row.BackColor = Color.FromArgb(240, 243, 248);
            };
            row.MouseLeave += (s, e) =>
            {
                if (item.Key != _selectedKey)
                    row.BackColor = Color.Transparent;
            };

            string tipText = $"{item.Title}\n[{item.Category}] • {item.Badge}\n\n{item.Description}";
            _toolTip.SetToolTip(row, tipText);
            _toolTip.SetToolTip(lblTitle, tipText);
            if (lblBadge != null) _toolTip.SetToolTip(lblBadge, tipText);

            return row;
        }

        private void HighlightSelectedRow()
        {
            foreach (var row in _renderedRows)
            {
                if (row.Tag is ShowcaseFeatureItem item)
                {
                    bool isSelected = item.Key == _selectedKey;
                    row.BackColor = isSelected ? Color.FromArgb(228, 236, 252) : Color.Transparent;
                    foreach (Control child in row.Controls)
                    {
                        if (child is Label lbl && child.Dock == DockStyle.Fill)
                        {
                            lbl.Font = new Font("Segoe UI", 9.25f, isSelected ? FontStyle.Bold : FontStyle.Regular);
                            lbl.ForeColor = isSelected ? Color.FromArgb(18, 86, 209) : Color.FromArgb(33, 37, 41);
                        }
                    }
                }
            }
        }

        public void ApplyTheme(ZeroSkin skin)
        {
            bool isDark = skin.IsDark;
            BackColor = isDark ? Color.FromArgb(28, 30, 42) : Color.FromArgb(248, 249, 251);
            _searchContainer.BackColor = isDark ? Color.FromArgb(22, 24, 34) : Color.FromArgb(240, 242, 245);
            _itemsPanel.BackColor = BackColor;
            _txtSearch.BackColor = isDark ? Color.FromArgb(36, 38, 52) : Color.White;
            _txtSearch.ForeColor = isDark ? Color.FromArgb(220, 224, 235) : Color.FromArgb(40, 44, 52);
            HighlightSelectedRow();
        }

        private void PopulateDefaultFeatures()
        {
            _allItems.Clear();

            // 1. CORE BENCHMARKS & GRIDS
            _allItems.Add(new ShowcaseFeatureItem(
                "feat_virtual_10m", "Virtual Grid (10M Rows)", "Core Benchmarks", "⚡", "10M", Color.FromArgb(16, 185, 129),
                "ZeroUI GridControl achieves zero-allocation high-frequency scrolling over 10,000,000 records via Win32 DIBSection and virtual viewports.",
                @"// Initialize 10M Virtual Grid
var grid = new ZeroGridControl();
grid.VirtualMode = true;
grid.RowCount = 10_000_000;
grid.ShowAutoFilterRow = true;
grid.ShowFindPanel = true;
grid.ApplyDpiScaling(ZeroDpi.GetScaleFactor(this));"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_autofilter", "Auto Filter & Find Panel", "Core Benchmarks", "🔍", "FILTER", Color.FromArgb(59, 130, 246),
                "Incremental search highlighting and instant Excel-like column header auto-filtering without dataset copying.",
                @"// Configure Instant Find Panel
grid.ShowFindPanel = true;
grid.FindHighlightMatches = true;
grid.ShowAutoFilterRow = true;
grid.FindFilterColumns = ""*"";"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_grouping_summaries", "Grouping & Summary Totals", "Core Benchmarks", "∑", "GROUPING", Color.FromArgb(245, 158, 11),
                "Interactive column header drag-to-group with real-time aggregates (Sum, Avg, Count, Min, Max) in footer.",
                @"// Enable Grouping & Totals
grid.ShowGroupPanel = true;
grid.ShowSummaryFooter = true;
grid.Columns[""UnitPrice""].Summary = SummaryType.Average;
grid.Columns[""TotalAmount""].Summary = SummaryType.Sum;"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_banded_headers", "Banded Column Headers", "Core Benchmarks", "🏛️", "BANDED", Color.FromArgb(236, 72, 153),
                "Multi-tier hierarchical column bands for complex financial and industrial telemetry datasets.",
                @"// Multi-Tier Banded Headers
grid.AddBand(""Part Identification"", new[] { ""Category"", ""ID"", ""ItemCode"", ""ItemName"" });
grid.AddBand(""Financials & Stock"", new[] { ""Quantity"", ""UnitPrice"", ""TotalAmount"" });"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_standard_dgv", "Standard DataGridView Compare", "Core Benchmarks", "🐢", "LEGACY", Color.FromArgb(107, 114, 128),
                "Direct performance, memory, and frame-rate comparison against Microsoft standard Windows Forms DataGridView.",
                @"// Standard DataGridView comparison test
var dgv = new DataGridView { VirtualMode = true };
// Observe ~150MB higher RAM footprint and GC pressure"
            ));

            // 2. INDUSTRIAL DOMAIN VERTICALS
            _allItems.Add(new ShowcaseFeatureItem(
                "feat_vert_energy", "Energy & Smart Grid", "Industrial Verticals", "⚡", "SLD/BESS", Color.FromArgb(6, 182, 212),
                "Single-Line Diagram, 16-Cell BESS Rack telemetry, and 16-String Solar PV array monitoring.",
                @"var sld = new SingleLineDiagram { Dock = DockStyle.Fill };
var bess = new BessRackMonitor();
var solar = new SolarPvMatrix();"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_vert_petrochem", "Oil & Gas / Petrochem", "Industrial Verticals", "🛢️", "DISTILL", Color.FromArgb(249, 115, 22),
                "Fractional Distillation Column with tray temperatures, SIS Cause & Effect Matrix, and Pipeline PIG Tracker.",
                @"var column = new DistillationColumn();
var esdMatrix = new EsdMatrix();
var pigMonitor = new PipelinePigMonitor();"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_vert_pharma", "Pharma & Biotech (ISA-88)", "Industrial Verticals", "💊", "ISA-88", Color.FromArgb(168, 85, 247),
                "Sanitary Bioreactor Vessel, ISO Cleanroom Cascade HUD, ISA-88 Batch SFC Tracker, and CIP/SIP 4-TACT Matrix.",
                @"var bioreactor = new BioreactorVessel();
var cleanroom = new CleanroomEnvHud();
var sfc = new SfcBatchTracker();
var cip = new CipValidationMatrix();"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_vert_water", "Water & Wastewater", "Industrial Verticals", "💧", "SCADA", Color.FromArgb(14, 165, 233),
                "Circular Clarifier Basin, Chemical Dosing Skid with active dosing pumps, and Hydraulic Gradient Chart.",
                @"var clarifier = new ClarifierBasin();
var dosing = new ChemicalDosingSkid();
var hydraulic = new HydraulicGradientChart();"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_vert_bms", "BMS & HVAC Automation", "Industrial Verticals", "🏢", "HVAC", Color.FromArgb(34, 197, 94),
                "Variable Air Volume AHU Schematic, Chiller Plant COP performance curves, and 7-Day Zone Temperature Scheduler.",
                @"var ahu = new AhuSchematic();
var chiller = new ChillerPlant();
var scheduler = new ZoneScheduler();"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_vert_life_sciences", "Life Sciences & Lab", "Industrial Verticals", "🔬", "LAB", Color.FromArgb(139, 92, 246),
                "96/384 Microplate Reader heatmap, Centrifuge RCF Monitor, and -80°C Ultra-Low Cold Chain MKT Tracker.",
                @"var reader = new MicroplateReader();
var centrifuge = new CentrifugeMonitor();
var coldChain = new ColdChainTracker();"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_vert_robotics", "Robotics & Intralogistics", "Industrial Verticals", "🤖", "AGV/ASRS", Color.FromArgb(245, 158, 11),
                "Real-time AGV Fleet Path Canvas, ASRS High-Bay Stacker Crane Visualizer, and Conveyor Merge Sorter Matrix.",
                @"var fleet = new AgvFleetCanvas();
var crane = new AsrsCraneVisualizer();
var conveyor = new ConveyorMergeMatrix();"
            ));

            // 3. INDUSTRIAL SCADA & TELEMETRY
            _allItems.Add(new ShowcaseFeatureItem(
                "feat_scada_synoptic", "P&ID Process Mimic", "SCADA & Telemetry", "🏭", "SYNOPTIC", Color.FromArgb(239, 68, 68),
                "Animated P&ID distillation plant mimic with live tank levels, rotating pumps, flow pipes, and sensor transmitters.",
                @"var synoptic = new PlantMimicCanvas();
synoptic.AddTank(""TK-101"", level: 76.5);
synoptic.AddPump(""P-101A"", rpm: 1450);
synoptic.StartAnimation();"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_scada_pid", "Closed-Loop Reaction Process", "SCADA & Telemetry", "🔄", "CLOSED-LOOP", Color.FromArgb(245, 158, 11),
                "Real-time feedback loop controller with Setpoint (SP), Process Variable (PV), and Control Variable (CV).",
                @"var pid = new PidFaceplate();
pid.BindController(pidLoop);"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_scada_alarms", "ISA-18.2 Alarm Banner & PID", "SCADA & Telemetry", "🚨", "ISA-18.2", Color.FromArgb(239, 68, 68),
                "Strict industrial alarm lifecycle management: Unacknowledged, Acknowledged, Cleared, and Suppressed.",
                @"var alarmGrid = new AlarmGrid();
alarmGrid.BindManager(isaAlarmManager);"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_scada_tags", "OPC-UA Realtime Tag Engine", "SCADA & Telemetry", "⚡", "200 Hz", Color.FromArgb(6, 182, 212),
                "High-frequency 200 Hz telemetry monitor streaming over 1,000 tags with deadband filtering and quality flags.",
                @"var tagEngine = new TagEngineMonitor();
tagEngine.Subscribe(""ns=2;s=Line1.Speed"", 200 /* Hz */);"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_scada_gauges", "Substation SCADA & Energy Mimic", "SCADA & Telemetry", "⏱️", "SUBSTATION", Color.FromArgb(16, 185, 129),
                "110kV Substation Single-Line Mimic, circuit breakers, bus voltages, power factor and digital telemetry meters.",
                @"var substation = new SubstationMimic();
substation.SetBreakerState(""CB-101"", BreakerState.Closed);"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_scada_interlock", "Telemetry & Safety Interlock", "SCADA & Telemetry", "🛡️", "SAFETY", Color.FromArgb(225, 29, 72),
                "High-speed vibration FFT spectrum analysis, temperature telemetry, and SIL safety interlock matrix.",
                @"var interlock = new SafetyInterlockMatrix();
interlock.EvaluatePermissives();"
            ));

            // 4. NETWORK & INFRASTRUCTURE
            _allItems.Add(new ShowcaseFeatureItem(
                "feat_net_rack", "19\" 42U Server Rack & Switch", "Network & Infra", "🗄️", "42U/PoE", Color.FromArgb(59, 130, 246),
                "Interactive 42U equipment cabinet with thermal heatmaps, server blade installation, and 48-port switch faceplate.",
                @"var devRack = new DeviceRack { ShowThermalOverlay = true };
var swFaceplate = new SwitchFaceplate();"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_net_topology", "Network Topology Canvas & Flows", "Network & Infra", "🕸️", "TOPOLOGY", Color.FromArgb(99, 102, 241),
                "Dynamic network node topology with layered hierarchical and circular ring layout algorithms and animated packet flows.",
                @"var netTopo = new NetworkTopology();
netTopo.Engine.ApplyHierarchicalLayout(netTopo.Width, netTopo.Height);"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_net_chassis", "Chassis Health & Optical DDM", "Network & Infra", "🖥️", "CHASSIS", Color.FromArgb(249, 115, 22),
                "Modular device chassis with dual redundant hot-swap PSUs, tachometer fan modules, and SFP+ DDM optical diagnostics.",
                @"var devFaceplate = new DeviceFaceplate();
devFaceplate.Profile.Psu2.Status = PsuHealthStatus.Normal;"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_net_ipam", "2D IPAM Subnet Matrix", "Network & Infra", "🌐", "/24 IPAM", Color.FromArgb(16, 185, 129),
                "Complete /24 IPv4 address allocation grid with color-coded status tiles, ping latency sparklines, and DHCP leases.",
                @"var ipMatrix = new IpMatrix();
ipMatrix.ScanSubnet(""192.168.1.0/24"");"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_net_fieldbus", "Fieldbus & Break Locator", "Network & Infra", "🔌", "FIELDBUS", Color.FromArgb(239, 68, 68),
                "Industrial fieldbus daisy-chain line monitor (Profinet / EtherCAT) with real-time cable break localization.",
                @"var fbMonitor = new FieldbusMonitor();
fbMonitor.Network.SimulateBreak(stationIndex: 3);"
            ));

            // 5. MES & OPERATIONS
            _allItems.Add(new ShowcaseFeatureItem(
                "feat_mes_dashboard", "MES Operations & Telemetry", "MES Operations", "📊", "OEE", Color.FromArgb(59, 130, 246),
                "Real-time factory telemetry with live OEE gauge (88.4%), Takt time pacing, cycle counters, and PLC status.",
                @"var mesHud = new MesTelemetryHud();
mesHud.UpdateOee(availability: 0.94, performance: 0.96, quality: 0.98);"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_workflow_kanban", "Smart Factory Kanban Board", "MES Operations", "📋", "KANBAN", Color.FromArgb(245, 158, 11),
                "Interactive production card lanes with WIP constraints, order progress, and technician assignment.",
                @"var kanban = new KanbanBoard();
kanban.AddLane(""Ready"", Color.LightGray);
kanban.AddLane(""In Progress"", Color.SkyBlue);"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_workflow_gantt", "Industrial Gantt Scheduler", "MES Operations", "📅", "GANTT", Color.FromArgb(139, 92, 246),
                "Multi-stage machine scheduling, task dependencies, critical path calculation, and milestone tracking.",
                @"var gantt = new GanttControl();
gantt.AddTask(""Order Intake"", DateTime.Today, DateTime.Today.AddDays(3));"
            ));

            // 6. WAREHOUSE & LOGISTICS
            _allItems.Add(new ShowcaseFeatureItem(
                "feat_barcode_station", "Receiving & Barcode Station", "Warehouse Suite", "📦", "SCANNER", Color.FromArgb(59, 130, 246),
                "Rapid GS1-128 and 2D Datamatrix barcode ingestion station with instant verification and PO matching.",
                @"var scanner = new BarcodeScanControl();
scanner.OnBarcodeScanned += code => VerifyInboundPO(code);"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_warehouse_lot", "FEFO/FIFO Lot Traceability", "Warehouse Suite", "📋", "LOT-TRACE", Color.FromArgb(245, 158, 11),
                "Material traceability timeline, batch expiration alerts, and comprehensive stock movement audit logs.",
                @"var timeline = new StockMovementTimeline();
timeline.LoadAuditTrail(lotNumber);"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_warehouse_racks", "Visual Warehouse Rack 3D/2D", "Warehouse Suite", "🏢", "WMS", Color.FromArgb(16, 185, 129),
                "Interactive visual warehouse rack matrix with bin occupancy levels, weight limits, and hazardous flags.",
                @"var rack = new WarehouseRack();
rack.LoadZoneLayout(""ZONE-A"", bayCount: 12, tierCount: 5);"
            ));

            // 7. UI COMPONENT CATALOG
            _allItems.Add(new ShowcaseFeatureItem(
                "feat_editors_core", "Core UI Controls & Inputs", "Component Catalog", "🎛️", "INPUTS", Color.FromArgb(59, 130, 246),
                "ZeroButton, ZeroProgressBar, ZeroSearchBox, ZeroSwitch, ZeroTag, ZeroSegmented, ZeroStatistic, and ZeroInput.",
                @"var btn = new ZeroButton { ButtonStyle = ZeroButtonStyle.Primary };
var prog = new ZeroProgressBar { Value = 78 };"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_master_detail", "Master-Detail Hierarchy", "Component Catalog", "📑", "HIERARCHY", Color.FromArgb(139, 92, 246),
                "Expandable nested hierarchical rows with independent schemas, lookups, and sub-summaries.",
                @"grid.EnableMasterDetail = true;
grid.DetailRowHeight = 160;"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_nav_ribbon", "Ribbon & Navigation Controls", "Component Catalog", "🧭", "LAYOUT", Color.FromArgb(16, 185, 129),
                "DevExpress-style RibbonControl, Accordion side explorer, Breadcrumb bar, and SplitContainers.",
                @"var ribbon = new RibbonControl();
var page = ribbon.AddPage(""Home"");"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_commercial_query", "Visual Query Builder (FilterControl)", "Component Catalog", "🔍", "QUERY", Color.FromArgb(139, 92, 246),
                "Hierarchical visual AND/OR expression tree builder compiling to type-safe SQL WHERE criteria.",
                @"var filter = new ZeroFilterControl();
filter.AvailableFields.AddRange(new[] { ""PartNumber"", ""Category"", ""UnitCost"", ""StockQty"" });
string sql = filter.RootGroup.ToSqlWhere();"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_commercial_editors", "Commercial Lookups & Tokens", "Component Catalog", "🏷️", "EDITORS", Color.FromArgb(16, 185, 129),
                "GridLookupEdit with +Add New, SearchLookUpEdit multi-select, CheckedComboBoxEdit, and TokenEdit with auto-complete.",
                @"var lookup = new GridLookupEdit();
var tokenEdit = new TokenEdit();
tokenEdit.AutocompleteSource = new[] { ""ISO-9001"", ""RoHS-3"" };"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_commercial_wizard", "Process Workflow Wizard", "Component Catalog", "📋", "WIZARD", Color.FromArgb(99, 102, 241),
                "Multi-step sequential guided dispatch sequence with step validation, status glyphs, and navigation.",
                @"var wizard = new ZeroWizard();
wizard.Pages.Add(new ZeroWizardPage(""Work Order"", ""Configure order""));"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_tree_list", "Multi-Level BOM TreeList", "Component Catalog", "🌳", "TREE", Color.FromArgb(20, 184, 166),
                "Industrial Bill of Materials (BOM) multi-column virtual tree explorer with collapsible parent-child nodes.",
                @"var tree = new ZeroTreeList();
tree.Columns.Add(""Component"");
tree.LoadHierarchy(bomNodes);"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_office_pdf", "Vector PDF & CAD Schematic Reader", "Component Catalog", "📄", "PDF", Color.FromArgb(239, 68, 68),
                "High-fidelity vector PDF document reader with continuous scrolling, bookmarks, and 25-400% zoom.",
                @"var pdf = new PdfViewerControl();
pdf.LoadDocument(""Report.pdf"");"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_office_spreadsheet", "Spreadsheet Calculation Engine", "Component Catalog", "📑", "SPREADSHEET", Color.FromArgb(16, 185, 129),
                "Full-featured spreadsheet grid with formula parser, range calculations, and multi-sheet support.",
                @"var sheet = new SpreadsheetControl();
sheet.SetCellValue(""A1"", 100);
sheet.SetCellFormula(""A2"", ""=A1 * 1.15"");"
            ));

            // 8. ANALYTICS & CHARTS
            _allItems.Add(new ShowcaseFeatureItem(
                "feat_analytics_charts", "High-Speed Waveforms & Trends", "Analytics & Charts", "📈", "100kHz", Color.FromArgb(139, 92, 246),
                "Real-time 100,000 points/second hardware-accelerated waveform scope and multi-axis trend charting.",
                @"var chart = new TrendChart();
chart.AddSeries(""Pressure"", SeriesType.FastLine);
chart.FeedRealtimeData(pressureVal);"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_analytics_spc", "SPC Quality & Six Sigma Charts", "Analytics & Charts", "📊", "SPC/SQC", Color.FromArgb(59, 130, 246),
                "Statistical Process Control charts with UCL, LCL, Center Line, and Nelson Rules evaluation.",
                @"var spc = new SpcChart();
spc.SetLimits(ucl: 15.2, cl: 14.0, lcl: 12.8);"
            ));

            _selectedKey = _allItems[0].Key;
        }
    }
}
