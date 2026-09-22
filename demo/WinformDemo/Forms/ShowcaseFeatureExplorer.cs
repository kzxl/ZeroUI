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
        private string _selectedKey = string.Empty;

        public event Action<ShowcaseFeatureItem>? FeatureSelected;

        public ShowcaseFeatureExplorer()
        {
            DoubleBuffered = true;
            Dock = DockStyle.Left;
            Width = 260;
            BackColor = Color.FromArgb(248, 249, 251);
            Padding = new Padding(0);

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
                Padding = new Padding(12, 0, 8, 0)
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

            if (!string.IsNullOrEmpty(item.Badge))
            {
                var lblBadge = new Label
                {
                    Dock = DockStyle.Right,
                    Width = 65,
                    Text = item.Badge,
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    ForeColor = item.BadgeColor,
                    TextAlign = ContentAlignment.MiddleRight
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

            // 1. HIGHLIGHTED FEATURES
            _allItems.Add(new ShowcaseFeatureItem(
                "feat_virtual_10m", "Virtual Grid (10M Rows)", "Highlighted Features", "⚡", "10M", Color.FromArgb(16, 185, 129),
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
                "feat_autofilter", "Auto Filter & Find Panel", "Highlighted Features", "🔍", "DIRECTX", Color.FromArgb(59, 130, 246),
                "Incremental search highlighting and instant Excel-like column header auto-filtering without dataset copying.",
                @"// Configure Instant Find Panel
grid.ShowFindPanel = true;
grid.FindHighlightMatches = true;
grid.ShowAutoFilterRow = true;
grid.FindFilterColumns = ""*"";"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_master_detail", "Master-Detail Hierarchy", "Highlighted Features", "📑", "HIERARCHY", Color.FromArgb(139, 92, 246),
                "Expandable nested hierarchical rows with independent schemas, lookups, and sub-summaries.",
                @"// Master-Detail Setup
grid.EnableMasterDetail = true;
grid.DetailRowHeight = 160;
grid.OnDetailExpand += (s, row) => LoadSubOrders(row.Id);"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_grouping_summaries", "Grouping & Summary Totals", "Highlighted Features", "∑", "GROUPING", Color.FromArgb(245, 158, 11),
                "Interactive column header drag-to-group with real-time aggregates (Sum, Avg, Count, Min, Max) in footer.",
                @"// Enable Grouping & Totals
grid.ShowGroupPanel = true;
grid.ShowSummaryFooter = true;
grid.Columns[""Price""].Summary = SummaryType.Average;
grid.Columns[""Total""].Summary = SummaryType.Sum;"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_banded_headers", "Banded Column Headers", "Highlighted Features", "🏛️", "BANDED", Color.FromArgb(236, 72, 153),
                "Multi-tier hierarchical column bands for complex financial and industrial telemetry datasets.",
                @"// Multi-Tier Banded Headers
grid.AddBand(""Sales Info"", new[] { ""Date"", ""Region"", ""Rep"" });
grid.AddBand(""Financials"", new[] { ""UnitPrice"", ""Quantity"", ""Total"" });"
            ));

            // 2. DATA PRESENTATION & BENCHMARK
            _allItems.Add(new ShowcaseFeatureItem(
                "feat_zerogrid_table", "ZeroGrid Virtual Table", "Data Presentation", "▦", "GDI+", Color.FromArgb(59, 130, 246),
                "Hardware-accelerated single-HWND memory rendering benchmark with ultra-low latency.",
                @"var grid = new ZeroGridControl { Dock = DockStyle.Fill };
grid.SetDataSource(my100kList);
grid.BestFitColumns();"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_standard_dgv", "Standard DataGridView Compare", "Data Presentation", "🐢", "LEGACY", Color.FromArgb(107, 114, 128),
                "Direct performance, memory, and frame-rate comparison against Microsoft standard Windows Forms DataGridView.",
                @"// Standard DataGridView comparison test
var dgv = new DataGridView { VirtualMode = true };
// Observe ~150MB higher RAM footprint and GC pressure"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_tree_list", "Multi-Column BOM TreeList", "Data Presentation", "🌳", "TREE", Color.FromArgb(16, 185, 129),
                "Industrial Bill of Materials (BOM) multi-column tree explorer with collapsible parent-child nodes.",
                @"var tree = new ZeroTreeList();
tree.Columns.Add(""Component"");
tree.Columns.Add(""Part No"");
tree.Columns.Add(""Qty"");
tree.LoadHierarchy(bomNodes);"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_property_grid", "Property Grid & Inspector", "Data Presentation", "⚙️", "INSPECT", Color.FromArgb(139, 92, 246),
                "DevExpress-style categorized property inspector with in-place editors, color pickers, and numeric spinners.",
                @"var inspector = new ZeroPropertyGrid();
inspector.SelectedObject = selectedMachineConfig;"
            ));

            // 3. DATA EDITORS & FORM INPUTS
            _allItems.Add(new ShowcaseFeatureItem(
                "feat_editors_core", "Core Input Editors", "Data Editors & Inputs", "🎛️", "EDITORS", Color.FromArgb(59, 130, 246),
                "TextEdit, ButtonEdit, CalcEdit, SpinEdit, DateEdit, TimeEdit, and ComboBox with active skins.",
                @"var edit = new TextEdit();
edit.PlaceholderText = ""Enter customer code..."";
edit.Properties.Mask = ""AAA-0000"";"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_editors_lookup", "Search & LookUpEdit", "Data Editors & Inputs", "🔎", "AUTOCOMPLETE", Color.FromArgb(16, 185, 129),
                "Incremental multi-column lookups with instant keyword highlighting and search-as-you-type.",
                @"var lookup = new LookUpEdit();
lookup.Properties.DataSource = productList;
lookup.Properties.DisplayMember = ""Name"";"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_editors_tokens", "TokenEdit & Chips", "Data Editors & Inputs", "🏷️", "TOKENS", Color.FromArgb(139, 92, 246),
                "Tokenized tags with delete chips, validation patterns, and autocomplete dropdowns.",
                @"var tokenEdit = new TokenEdit();
tokenEdit.AvailableTokens = new[] { ""PLC-01"", ""Modbus"", ""OPC-UA"" };"
            ));

            // 4. INDUSTRIAL AUTOMATION & SCADA
            _allItems.Add(new ShowcaseFeatureItem(
                "feat_scada_synoptic", "P&ID Process Mimic", "Industrial & SCADA", "🏭", "SYNOPTIC", Color.FromArgb(239, 68, 68),
                "Animated P&ID plant mimic with live tank levels, rotating pumps, flow pipes, and sensor transmitters.",
                @"var synoptic = new PlantMimicCanvas();
synoptic.AddTank(""TK-101"", level: 76.5);
synoptic.AddPump(""P-101A"", rpm: 1450);
synoptic.StartAnimation();"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_scada_pid", "Closed-Loop PID Tuning", "Industrial & SCADA", "🎛️", "CLOSED-LOOP", Color.FromArgb(245, 158, 11),
                "Real-time feedback loop controller with Setpoint (SP), Process Variable (PV), and Control Variable (CV).",
                @"var pid = new PidFaceplate();
pid.BindController(pidLoop);"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_scada_alarms", "ISA-18.2 Alarm Banner", "Industrial & SCADA", "🚨", "ISA-18.2", Color.FromArgb(239, 68, 68),
                "Strict industrial alarm lifecycle management: Unacknowledged, Acknowledged, Cleared, and Suppressed.",
                @"var alarmGrid = new AlarmGrid();
alarmGrid.BindManager(isaAlarmManager);"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_scada_gauges", "Telemetry Gauges & Meters", "Industrial & SCADA", "⏱️", "GAUGES", Color.FromArgb(16, 185, 129),
                "Radial, linear, seven-segment, and digital indicators with smooth zero-jitter rendering.",
                @"var gauge = new RadialGauge();
gauge.MinValue = 0; gauge.MaxValue = 200;
gauge.Value = 145.2;"
            ));

            // 5. WAREHOUSE & LOGISTICS
            _allItems.Add(new ShowcaseFeatureItem(
                "feat_warehouse_racks", "3D/2D Storage Racks", "Warehouse & Logistics", "🏢", "WMS", Color.FromArgb(16, 185, 129),
                "Interactive visual warehouse rack matrix with bin occupancy levels, weight limits, and hazardous flags.",
                @"var rack = new WarehouseRack();
rack.LoadZoneLayout(""ZONE-A"", bayCount: 12, tierCount: 5);"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_barcode_station", "Receiving & Barcode Station", "Warehouse & Logistics", "📦", "SCANNER", Color.FromArgb(59, 130, 246),
                "Rapid GS1-128 and 2D Datamatrix barcode ingestion station with instant verification.",
                @"var scanner = new BarcodeScanControl();
scanner.OnBarcodeScanned += code => VerifyInboundPO(code);"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_warehouse_lot", "FIFO/FEFO Lot Tracking", "Warehouse & Logistics", "📋", "LOT-TRACE", Color.FromArgb(245, 158, 11),
                "Material traceability timeline, batch expiration alerts, and stock movements.",
                @"var timeline = new StockMovementTimeline();
timeline.LoadAuditTrail(lotNumber);"
            ));

            // 6. ANALYTICS & CHARTS
            _allItems.Add(new ShowcaseFeatureItem(
                "feat_analytics_charts", "High-Speed Waveforms & Trends", "Analytics & Charts", "📈", "100kHz", Color.FromArgb(139, 92, 246),
                "Real-time 100,000 points/second hardware-accelerated waveform scope and multi-axis trend charting.",
                @"var chart = new TrendChart();
chart.AddSeries(""Pressure"", SeriesType.FastLine);
chart.FeedRealtimeData(pressureVal);"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_analytics_spc", "SPC Statistical Quality Chart", "Analytics & Charts", "📊", "SPC/SQC", Color.FromArgb(59, 130, 246),
                "Statistical Process Control charts with UCL, LCL, Center Line, and Nelson Rules evaluation.",
                @"var spc = new SpcChart();
spc.SetLimits(ucl: 15.2, cl: 14.0, lcl: 12.8);"
            ));

            // 7. NAVIGATION & WORKFLOW
            _allItems.Add(new ShowcaseFeatureItem(
                "feat_nav_ribbon", "Ribbon & Accordion Navigation", "Navigation & Workflow", "🧭", "RIBBON", Color.FromArgb(16, 185, 129),
                "DevExpress-style RibbonControl, Accordion side explorer, and responsive Breadcrumb bars.",
                @"var ribbon = new RibbonControl();
var page = ribbon.AddPage(""Home"");
page.AddGroup(""Operations"");"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_workflow_kanban", "Production Kanban Board", "Navigation & Workflow", "📋", "KANBAN", Color.FromArgb(245, 158, 11),
                "Drag-and-drop production card lanes with WIP constraints and status badges.",
                @"var kanban = new KanbanBoard();
kanban.AddLane(""Ready"", Color.LightGray);
kanban.AddLane(""In Progress"", Color.SkyBlue);"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_workflow_gantt", "Industrial Gantt Scheduler", "Navigation & Workflow", "📅", "GANTT", Color.FromArgb(139, 92, 246),
                "Multi-stage machine scheduling, task dependencies, critical paths, and milestone tracking.",
                @"var gantt = new GanttControl();
gantt.AddTask(""Setup SMT"", DateTime.Today, DateTime.Today.AddHours(4));"
            ));

            // 8. OFFICE & DOCUMENTS
            _allItems.Add(new ShowcaseFeatureItem(
                "feat_office_spreadsheet", "Spreadsheet Calculation Engine", "Office & Documents", "📑", "SPREADSHEET", Color.FromArgb(16, 185, 129),
                "Full-featured spreadsheet grid with formula parser, range calculations, and multi-sheet support.",
                @"var sheet = new SpreadsheetControl();
sheet.SetCellValue(""A1"", 100);
sheet.SetCellFormula(""A2"", ""=A1 * 1.15"");"
            ));

            _allItems.Add(new ShowcaseFeatureItem(
                "feat_office_pdf", "PDF Viewer & Print Preview", "Office & Documents", "📄", "PDF", Color.FromArgb(239, 68, 68),
                "High-fidelity vector PDF document reader with thumbnail navigation and direct printer spooling.",
                @"var pdf = new PdfViewerControl();
pdf.LoadDocument(""Report.pdf"");"
            ));

            _selectedKey = _allItems[0].Key;
        }
    }
}
