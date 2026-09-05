# ZeroUI Implementation Roadmap

## 1. Release Timeline & Milestones

| Phase | Milestone Name | Primary Focus | Status | Key Deliverables |
| :---: | :--- | :--- | :---: | :--- |
| **Phase 1** | **Core Virtualization Engine** | `ZeroUI.Core` (.NET Standard 2.0) | **Completed** | `PrefixSumArray`, `VirtualViewport2D`, `ZeroBufferPool`, `Channel` data queues. |
| **Phase 2** | **WinForms MVP (`ZeroGrid`)** | `ZeroUI.WinForms` (net462, net8.0) | **Completed** | Single-HWND Control, Unmanaged Fast GDI blit, 32-bit `SetScrollInfo`, 1M/10M-row benchmark. |
| **Phase 3** | **WPF Port (`ZeroGrid`)** | `ZeroUI.Wpf` (net462, net8.0) | **Completed** | `DrawingVisual` host, XAML attached properties, `D3DImage` DirectX 11 pipeline. |
| **Phase 4** | **Enterprise Grid Features** | Interactivity & Production Readiness | **Completed** | Floating In-place Editor, Frozen Columns/Rows, Multi-column sorting, Fast filtering. |
| **Phase 6** | **Theming & Packaging** | Distribution & Design System | **Completed** | Obsidian Dark / Clean Light Themes, Headless CLI benchmarks, Automated screenshots, NuGet packaging (v1.2.0). |
| **Phase 7** | **SCADA & Deterministic Runtime** | Industrial Edge & Telemetry Engine | **Completed** | `ZeroRuntime`, 3-Tier Pipeline, `ZeroTripleBuffer`, `TagStorage`, `ZeroAnimationClock`, `ZeroScene`. |
| **Phase 8** | **Enterprise Commercial Parity** | 8 Major Control Clusters | **Completed** | Full WinForms & WPF parity: `ZeroGridLookup`, `ZeroFilterControl`, `ZeroDockManager`, `ZeroWorkspaceSerializer`, `ZeroWizard`, `ZeroBoxPlotChart`, `ZeroGanttChart`, `ZeroPropertyGrid`, `ZeroPrintPreview`, `ZeroSkeleton`, `ZeroToast`, `ZeroModal`. |
| **Phase 9** | **Enterprise DX & Advanced Analytics** | Unified `EditValue`, Validation, OLAP, Range | **Completed** | `IZeroEditor`, `ZeroDataBinder`, `ValidationProvider`, `ZeroLocalizer`, `PivotGridControl`, `RangeControl`. |

---

## 2. Detailed Phase Breakdown

### Phase 1: Core Virtualization Engine
* [x] Implement `ZeroUI.Core.Memory.ZeroMemory` cross-target runtime compatibility wrapper (`NativeMemory` vs `Marshal`).
* [x] Implement `ZeroUI.Core.Virtualization.VirtualViewport2D` with physical vs logical pixel conversion.
* [x] Implement `ZeroUI.Core.Layout.PrefixSumArray` and Sparse Dynamic Height model with binary search tests.
* [x] Implement `ZeroUI.Core.Data.RowIndexMap` for zero-allocation sorting and filtering redirection.
* [x] Define `IZeroVirtualSource` and `IZeroPagedSource` contracts with `CellValueBuffer`.
* [x] Implement `ZeroUI.Core.Memory.ZeroBufferPool` wrapping `ArrayPool<T>` and unmanaged memory.
* [x] Create benchmark suite testing memory allocations (0 bytes per viewport calculation).

### Phase 2: WinForms MVP (`ZeroGrid`)
* [x] Create custom control `ZeroGridControl` inheriting from `Control` (Single-HWND architecture).
* [x] Implement Win32 P/Invoke layer (`CreateDIBSection`, `BitBlt`, `ExtTextOutW`, `SetScrollInfo`).
* [x] Implement High-DPI (`PerMonitorV2`) scaling handler (`WM_DPICHANGED`).
* [x] Implement `SpatialHitTester` for mouse clicks, column boundary resizing (`VSplit` cursor), and row selection.
* [x] Implement Double-Buffered Memory DC & DIB Section text rendering pipeline.
* [x] Verify smooth scrolling on 1,000,000-row and 10,000,000-row datasets.

### Phase 3: WPF Port (`ZeroGrid`)
* [x] Create `ZeroGridControl` inheriting from `FrameworkElement`.
* [x] Implement WPF `IScrollInfo` interface for seamless native `<ScrollViewer>` integration.
* [x] Implement `DrawingVisual` render pipeline with frozen brushes and pens.
* [x] Implement direct DirectX 11 to Direct3D 9Ex shared texture bridge via `D3DImage` for GPU rendering.
* [x] Verify zero visual tree overhead and smooth 60 FPS scrolling.

### Phase 4: Enterprise Grid Features
* [x] Implement floating flyweight `InPlaceEditCoordinator` (TextBox / ComboBox / DatePicker) with Tab/Enter navigation.
* [x] Implement frozen columns (left-pinned and right-pinned columns).
* [x] Implement in-memory multi-column quick-sort via `Span<int>.Sort()` and fast column filtering.
* [x] Implement Buffer Resize Handshake protocol and Direct2D GPU Device-Loss recovery (`D2DERR_RECREATE_TARGET`).
* [x] Implement column auto-resizing, clipboard TSV export (Ctrl+C), and drag-and-drop column reordering.

### Phase 5: Expanded Controls Suite
* [x] Develop `ZeroTreeList`: High-performance virtualized hierarchical Tree & Multi-Level BOM TreeList with expand/collapse chevrons, tri-state cascading checkboxes, guidelines, and search filtering.
* [x] Develop `ZeroHeatmap`: 2D Matrix Heatmap for machine throughput, line load, and thermal distribution with multi-stop color gradients (`Industrial`, `Viridis`, `CoolWarm`, `Emerald`) and hover inspection.
* [x] Develop `ZeroLookup`: Virtualized searchable autocomplete dropdown & lookup box with non-activating flyweight popup for 10,000+ items.
* [x] Develop `ZeroDateRangePicker`: Enterprise dual-date range selector (From -> To) with 1-click presets (*Today*, *Last 7 Days*, *This Month*...) and visual calendar range highlight.
* [x] Develop `ZeroNumericBox`: High-precision numeric stepper and spin box with mouse hold acceleration, unit prefixes/suffixes, and decimal formatting.
* [x] Develop `ZeroTabControl`: Modern anti-aliased flat TabControl and container with Underline/Pill styles, notification badges, and native Obsidian Dark / Clean Light theming.
* [x] Develop `ZeroTrendChart`: Real-time 60 FPS oscilloscope and multi-channel telemetry sensor chart.
* [x] Develop `ZeroWarehouseRack`: 2D Smart Warehouse Storage Rack visualizer (Bay x Level x Bin).
* [x] Develop `ZeroSpcChart`: Statistical Process Control (SPC) X-Bar Chart with control limits ($UCL$, $LCL$) and $C_{pk}$.
* [x] Develop `ZeroKanbanBoard`: Electronic Shopfloor Kanban Dispatching Board with WIP limits.
* [x] Develop `ZeroTank3D`: Industrial 3D cylindrical fluid storage tank with animated waves.

### Phase 6: Theming, Testing & Distribution
* [x] Build Obsidian Dark Theme (Charcoal `#121824`) and Clean Light Theme with reactive switching.
* [x] Build headless automated benchmark suite (`--benchmark`) and screenshot generation tool (`--capture-screenshots`).
* [x] Add automated UI stress testing across .NET 4.6.2 and .NET 8.0.
* [x] Author XML documentation and package NuGet distributions (`ZeroUI.Core`, `ZeroUI.WinForms`, `ZeroUI.Wpf` v1.2.0).

### Phase 7: SCADA & Deterministic Runtime (Directives 1–27)
* [x] **`ZeroRuntime`:** Deterministic 7-cycle master scheduler coordinating PLC (10ms), Logic (10ms), Telemetry (16ms), UI (16ms), Historian (100ms), Cleanup (1s), and Health (5s) cycles with drift compensation.
* [x] **3-Tier SCADA Pipeline:** `ScadaPipelineCoordinator` and `ZeroTripleBuffer<T>` decoupling fast 10 kHz field acquisition from medium 1 kHz calculations and slow 30–60 Hz UI display.
* [x] **`TagStorage` & `ZeroTagEngine` v2:** Contiguous unboxed array tag registry with atomic dirty bitmasking and inverted index listener dispatch (>48M writes/s, >244M reads/s).
* [x] **`ZeroAnimationClock` Core Primitive:** 60Hz centralized ticker with lock-free Copy-On-Write arrays and synchronized ISA-18.2 phases, eliminating scattered timers across all controls.
* [x] **`TimeSeriesPyramid` & High-Scale LTTB:** Multi-resolution continuous rollups (L0–L5) and zero-alloc 1M/10M point downsampling (~340M pts/s) enabling instantaneous $O(\text{screen pixels})$ zoom.
* [x] **Industrial Scene Graph (`ZeroScene`):** Single-HWND plant canvas with `GridSpatialIndex` spatial culling and `SceneNode` hierarchy (`TankNode`, `PumpNode`, `PipeNode`, `ValveNode`, `SensorNode`, `AlarmNode`).
* [x] **Modbus Address Optimization:** `ModbusAddressPlanner` coalescing disjoint register tags into contiguous MBAP block reads (up to 98.3% network packet reduction).
* [x] **Unified Benchmark Suite:** `ZeroUI.Benchmarks` CLI covering Categories A to F (Rendering, Virtual Grid, Telemetry, TagEngine, Modbus, Historian).

### Phase 8: Enterprise Commercial Parity (8 Major Clusters)
* [x] **Cluster 1 (Grid & Query):** `FilterCriteria` boolean expression tree with SQL `WHERE` generation; `ZeroFilterControl` visual query builder UI (WinForms & WPF).
* [x] **Cluster 2 (Enterprise Editors):** `ZeroGridLookup` multi-column DataGrid popup dropdown (WinForms & WPF); `ZeroCheckedComboBox` multi-select checkboxes; `ZeroTokenEdit` tag editor; `ZeroColorPicker` swatch matrix.
* [x] **Cluster 3 (Navigation & Workflows):** `ZeroSideNav` & `ZeroAccordion` (WPF parity); `ZeroWizard` multi-step guided process wizard with validation (WinForms & WPF).
* [x] **Cluster 4 (Windowing & Docking):** `ZeroDockManager` multi-zone docking layout with splitters, auto-hide, and `ZeroFloatingWindow` (WinForms & WPF); `ZeroWorkspaceSerializer` zero-dependency JSON persistence for layout and grid columns.
* [x] **Cluster 5 (Analytics & SPC):** `ZeroBoxPlotChart` statistical Box-and-Whisker quality inspection chart with USL/LSL limits (WinForms & WPF).
* [x] **Cluster 6 (Industrial & Scheduling):** `ZeroGanttChart` production scheduling timeline (WinForms & WPF); `ZeroPropertyGrid` categorized reflection property inspector (WinForms & WPF).
* [x] **Cluster 7 (Feedback & Overlays):** `ZeroSkeleton` 60 FPS shimmer loading placeholder (WinForms & WPF); `ZeroToast` & `ZeroModal` (WPF parity).
* [x] **Cluster 8 (Reporting & Print):** `ZeroPrintPreview` vector document and report print previewer with zoom and direct printer dispatch (WinForms & WPF).

### Phase 9: Enterprise DX & Advanced Analytical Engines
* [x] **Unified `IZeroEditor` Contract & `EditValue` Pipeline:** Standardized `EditValue`, `EditValueChanged`, `IsModified`, `ReadOnly`, `Reset()`, and `Clear()` across all form editor controls.
* [x] **Generic Form Data-Binding Engine (`ZeroDataBinder`):** 1-line bidirectional binding and DTO extraction (`Populate` / `Collect<T>`).
* [x] **Universal XAML XML Namespace (WPF):** Registered `[XmlnsDefinition("http://schemas.zeroui.net/winfx/xaml", ...)]` and `[XmlnsPrefix]` in `AssemblyInfo.cs`.
* [x] **Semantic Naming Normalization:** Introduced clean enterprise class names (`GridControl`, `TreeList`, `FilterControl`, `GridLookupEdit`, `TokenEdit`, `SimpleButton`, `TextEdit`, `CheckEdit`, `SpinEdit`, `DateEdit`, `WizardControl`, `DocumentPreviewControl`) with 100% backward-compatible `ZeroXXX` aliases.
* [x] **Form Validation & Visual Error Notification Engine:** `ValidationProvider`, `ZeroErrorProvider`, rules (`Required`, `Range`, `Email`, `Phone`), vector badge adorners, and hover tooltips.
* [x] **Enterprise Localization & I18N Engine:** `ZeroLocalizer` supporting hot language switching between English and Vietnamese, runtime overrides, and multi-control string catalogs.
* [x] **OLAP Multidimensional Cross-Tab Matrix:** `PivotGridControl` and `ZeroPivotGrid` with hierarchical dimension slicing and measure aggregation (Sum, Count, Average, Min, Max, Grand Totals).
* [x] **Visual Timeline & Range Selector:** `RangeControl` and `DateTimeRangeSlider` with interactive grips, span panning, focal zoom, interval snapping, and distribution histogram/area graph.

---

## 3. Future Enhancements & Proposals Catalog

For detailed architectural evaluations, design trade-offs, and implementation specifications, refer to [ZeroUI Proposals Catalog — Section 8](file:///e:/15.%20Other/dotnet/libs/ZeroUI/docs/proposals.md#8-feasible-enterprise-control-expansion-proposals-multi-subsystem-blueprint).

### Upcoming Phases Overview

#### Phase 10: High-Impact Enterprise Presentation & Productivity (Near-Term)
- **`CardView` & `TileView` Mode for `GridControl` (Proposal 8.1):** Responsive multi-column virtualized card/tile grid view.
- **`GridDataExporter` (Proposal 8.2):** Zero-dependency streaming Excel (`.xlsx`) and `.csv` exporter.
- **`SearchLookUpEdit` (Proposal 8.3):** Paginated high-capacity dropdown with persistent top search bar.
- **`BarcodeBox` & `BarcodeEdit` (Proposal 8.4):** Vector 1D/2D barcode & QR Code generator/renderer.
- **`RatingControl` (Proposal 8.5):** Inspection severity & QA score half-star selector.
- **`BreadcrumbControl` (Proposal 8.6):** Hierarchical asset path navigator with sibling dropdowns.
- **`ZeroVisualDebugger` (Proposal 8.12):** In-app runtime UI tree inspector and performance HUD.

#### Phase 11: Industrial SCADA & Advanced Operations (Mid-Term)
- **`RadialGauge` & `LinearGauge` (Proposal 8.8):** Industrial dials & thermometer gauges bound directly to `TagEngine`.
- **`FunnelChart` / `PyramidChart` (Proposal 8.9):** Production line conversion and scrap loss visualizer.
- **`FlowLayoutControl` (Proposal 8.7):** Responsive card layout container with drag-and-drop tile reordering.

#### Phase 12: Specialized Document & Office Viewers (Long-Term)
- **`SpreadsheetControl` MVP (Proposal 8.10):** Lightweight vector calculation sheet with core formulas (`SUM`, `AVERAGE`, `IF`).
- **`PdfViewerControl` (Proposal 8.11):** Embedded CAD schematic and SOP technical document reader.


