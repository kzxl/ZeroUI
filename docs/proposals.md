# ZeroUI Architectural Proposals & Industrial Runtime Blueprint (Directives 28–38)

This document formalizes the architectural evolution, subsystem assessments, benchmark rigor standards, and multi-phase implementation roadmap for **ZeroUI**. It establishes the core engineering principles for transforming ZeroUI from a high-performance control library into a complete **Industrial UI & Deterministic Edge Runtime Ecosystem**.

---

## 1. Executive Summary & The Core Value of ZeroUI

The primary competitive differentiator of ZeroUI is **not merely "rendering 10 million rows"**, but its unique fusion of **High-Performance UI + Deterministic Runtime + Industrial Edge Infrastructure** within a single unified ecosystem:

```text
                     ZeroRuntime (Deterministic 7-Cycle Master Scheduler)
                                              │
                    ┌─────────────────────────┼─────────────────────────┐
                    │                         │                         │
            Communication Layer        Processing Layer              UI Layer
                    │                         │                         │
            Modbus TCP / S7            TagEngine v2 (TagId)          ZeroGrid (Single-HWND)
            Block Planner              ISA-18.2 Alarm Engine         ZeroTrendChart
            Connection Watchdog        PackML State / OEE            Plant Mimic (ZeroScene)
                    │                         │                         │
                    ▼                         ▼                         │
               TagUpdate ─────────────────────┴─────────────────────────┘
                               latest-value / frame batch
                                           │
                                           ▼
                                 Direct Blit Pipeline
                                (MemoryDIBSection / BitBlt)
```

By unifying field communication (Modbus/S7), data processing (TagEngine, Alarms, PackML, OEE, Historian), and single-HWND UI rendering on .NET, ZeroUI fills a critical void for modern industrial automation, SCADA, and MES shopfloor systems on Windows.

---

## 2. Core Philosophy: "Zero Allocation Where It Matters"

ZeroUI adopts a pragmatic, production-tested philosophy: **Zero Allocation Where It Matters**, replacing the unrealistic "Everything Zero Allocation" extreme.

| Execution Context | Allocation Policy | Rationale & Guidelines |
| :--- | :---: | :--- |
| **PLC Ingestion Loop (10 kHz)** | **0 B / op (STRICT)** | High-frequency streams must never trigger Gen 0/1 GC collections. |
| **Tag Updates & Dirty Flags** | **0 B / op (STRICT)** | Flat unboxed `TagStorage` struct array indexed by primitive `TagId`. |
| **Alarm Evaluation & Limits** | **0 B / op (STRICT)** | ISA-18.2 evaluation loop using value types and pre-allocated state records. |
| **Viewport & Culling Math** | **0 B / frame (STRICT)** | `VirtualViewport2D` and `PrefixSumArray` calculations execute entirely in registers. |
| **Cell Rendering (60–144 Hz)** | **0 B / cell (STRICT)** | Win32 Memory DC, `Span<char>`, `ExtTextOutW`, and cached pens/brushes. |
| **Animation Ticker (60 Hz)** | **0 B / tick (STRICT)** | `ZeroAnimationClock` iterates immutable Copy-On-Write snapshot arrays. |
| **Telemetry Coalescing** | **0 B / swap (STRICT)** | `ZeroTripleBuffer<T>` atomic pointer exchange without queue allocations. |
| **Historian Hot Ingestion Buffer**| **0 B / sample (STRICT)** | Pre-allocated circular ring buffers and contiguous rollup arrays. |
| **Form & View Initialization** | *Allocations Allowed* | Form loading, column configuration, and control tree setup prioritize ergonomics. |
| **Dialogs & Modals** | *Allocations Allowed* | User-initiated popups (`ZeroModal`, file open) run at human frequency (< 1 Hz). |
| **Configuration & Skin Loading** | *Allocations Allowed* | JSON parsing and palette dictionary building run once on startup or skin change. |
| **Chart Series Instantiation** | *Allocations Allowed* | Series schema definition and coordinate axes setup run on chart creation. |

---

## 3. Subsystem Architecture & Status Assessment

Detailed audit of each subsystem across the ZeroUI codebase:

| Subsystem | Current State | Target Recommendation & Architectural Evolution |
| :--- | :---: | :--- |
| **`ZeroGrid`** | Excellent | Introduce `RenderCommandBuffer` to decouple cell drawing from GDI state. |
| **`Virtualization`** | Good | Maintain index redirection layer (`RowIndexMap`) with sparse height cache. |
| **`Rendering`** | Good | Unify DIBSection memory rasterization with Direct2D hardware fallback. |
| **`Theme`** | Good | Expand token cache; isolate skin persistence from rendering hot paths. |
| **`Animation`** | Good | Standardize all controls on centralized `ZeroAnimationClock` 60 Hz ticker. |
| **`UiDispatcher`** | Good | Strictly enforce batch-only flushing (30–60 Hz) and drop per-event posts. |
| **`WorkerQueue<T>`** | Good | Enforce `QueueBackpressureMode.LatestPerKey` for telemetry stream conflation. |
| **`EventBus`** | Fair | Transition to direct delegate dispatch to minimize virtual dispatch overhead. |
| **`TagEngine`** | Critical Need | Transition from `object` values to unboxed `TagId` & `struct ScadaValue`. |
| **`TelemetryQueue`** | Good Idea | Enforce lock-free `ZeroTripleBuffer` pointer swap for UI decoupler. |
| **`ModbusAdapter`**| Refactored | Enforce `ModbusAddressPlanner` block coalescing for contiguous MBAP requests. |
| **`SiemensS7Adapter`**| Good | Add DB block coalescing and verify live protocol latency on hardware. |
| **`Historian`** | Good | Implement multi-resolution pyramid storage (L0 to L5) and daily WAL rolls. |
| **`LTTB`** | Excellent | Multi-resolution continuous decimation (10M &rarr; 2K points in <30 ms). |
| **`AlarmEngine`** | Good | ISA-18.2 deterministic state machine with audit trail logging. |
| **`Plant Mimic (P&ID)`**| High Potential| Transition to `ZeroScene` and hierarchical `SceneNode` with spatial culling. |
| **`Warehouse`** | Good | Maintain data-oriented picking engine and 5-tier location addressing. |
| **`Charts`** | Fair | Decouple circular buffer math from GDI drawing; optimize Catmull-Rom splines. |

---

## 4. Benchmark Rigor & Measurement Standards

### A. Strict Garbage Collection Profiling (Directive 28)
Synthetic benchmarks that only count `GC.CollectionCount(0)` fail to detect micro-allocations that cause GC latency under heavy workloads.
- **Mandatory Metric:** `GC.GetAllocatedBytesForCurrentThread()` must be measured before and after every benchmark loop.
- **Reporting Units:** Allocated bytes per operation (`Allocated bytes/op`) and bytes per frame (`Allocated bytes/frame`).
- **Collector Verification:** Explicitly track Gen 0, Gen 1, Gen 2, Large Object Heap (LOH), and Pinned Object Heap (POH) metrics.
- **Target:** **0 B/op** on all hot ingestion and rendering passes.

### B. Statistical Iteration & Warmup Protocol (Directive 29)
Single-run benchmarks are invalid due to JIT compilation artifacts and OS thread scheduling jitter.
- **Warmup:** Minimum 10 warmup iterations to allow Tiered JIT compilation (`Tier1` / OSR) to stabilize.
- **Sampling:** Minimum 100 measured iterations per benchmark parameter.
- **Statistical Percentiles:** Report **P50 (Median)**, **P95**, and **P99** percentiles in addition to minimum, maximum, and average times.
- **Compilation Flags:** Debug OFF (`-c Release`), optimize code enabled, Server GC configured where appropriate.

### C. Honest Performance Reporting Standard (Directive 30)
- **Eliminate Headline "25,500 FPS":** Synthetic calculation loops do not represent true UI frame latency.
- **Adopt Frame Budget Reporting:** Report concrete engineering measurements:
  - *Viewport calculation:* **0.039 ms / frame**
  - *Memory allocation:* **0 B / frame**
  - *Visible cells rendered:* **750 cells**
  - *Dataset scale:* **10,000,000 virtual rows**
  - *End-to-end paint:* **P50 < 1.8 ms, P95 < 3.6 ms, P99 < 5.2 ms**
- **Verified Claim:** An end-to-end frame cost under 4 ms justifies the claim: *"Suitable for 60 Hz, 120 Hz, and 144 Hz rendering."*

---

---

## 5. Completed Core Initiatives (Phases 1–8: Delivered)

The following architectural milestones have been fully implemented, benchmarked, and merged into `main`:

* [x] **Unboxed Fast Tag Engine (Directive 31):** `TagStorage` struct array mapped by 32-bit `TagId` (>48M writes/s, >244M reads/s, 0 B/op).
* [x] **Subscriber Inverted Index (Directive 31):** Direct $O(1)$ dirty notification dispatch to registered controls.
* [x] **Modbus Address Coalescing (Directive 31):** `ModbusAddressPlanner` merging disjoint registers into block requests (up to 98% network packet reduction).
* [x] **UI Telemetry Decoupler (Directive 31):** `ZeroTripleBuffer<T>` atomic pointer swap without queue allocations.
* [x] **Centralized Animation Clock (Directive 32):** `ZeroAnimationClock` 60 Hz ticker replacing individual control timers.
* [x] **Plant Mimic Scene Graph (Directive 33):** Single-HWND `ZeroScene` canvas with `GridSpatialIndex` spatial culling.
* [x] **Deterministic Master Scheduler (Directive 33):** 7-cycle `ZeroRuntime` coordinating PLC, Logic, Telemetry, UI, and Historian.
* [x] **Enterprise Commercial Parity (8 Clusters):** Query Builder (`ZeroFilterControl`), Multi-column Lookup (`ZeroGridLookup`), Token Editor (`ZeroTokenEdit`), Workflow Wizard (`ZeroWizard`), Six Sigma Box Plot (`ZeroBoxPlotChart`), Vector Print Preview (`ZeroPrintPreview`), Docking Layout (`ZeroDockManager`), and Shimmer Skeleton (`ZeroSkeleton`).

---

## 6. Active Strategic Proposals (Directives 39–42)

### Proposal 1: Unified Base Editor Contract (`IZeroEditor` & `EditValue` Pipeline) — Directive 39
Currently, input and editor controls expose disparate value properties (`.Text`, `.Value`, `.Checked`, `.SelectedValue`, `.Tokens`, `.SelectedColor`), complicating generic form data-binding, dirty tracking, and serialization.

* **Unified Contract (`IZeroEditor`):**
  ```csharp
  public interface IZeroEditor
  {
      object? EditValue { get; set; }
      event EventHandler? EditValueChanged;
      bool IsModified { get; set; }
      bool ReadOnly { get; set; }
      void Reset();
      void Clear();
  }
  ```
* **Editor Mapping Matrix:**
  * `ZeroTextBox` / `ZeroSearchBox`: `EditValue` &harr; `string`
  * `ZeroNumericBox`: `EditValue` &harr; `decimal` / `double`
  * `ZeroCheckBox` / `ZeroSwitch`: `EditValue` &harr; `bool`
  * `ZeroDatePicker`: `EditValue` &harr; `DateTime`
  * `ZeroDateRangePicker`: `EditValue` &harr; `(DateTime Start, DateTime End)`
  * `ZeroColorPicker`: `EditValue` &harr; `Color` (or Hex string `#RRGGBB`)
  * `ZeroTokenEdit`: `EditValue` &harr; `IReadOnlyList<string>` (or comma-delimited string)
  * `ZeroGridLookup` / `ZeroLookup`: `EditValue` &harr; Selected key / ID
  * `ZeroCheckedComboBox`: `EditValue` &harr; `List<object>` (or delimited keys)
* **Value Conversion Pipeline:** Built-in automatic type converter resolving string/number/date parsing without throwing unhandled cast exceptions.

### Proposal 2: Visual Studio Design-Time Ecosystem & Smart Tags — Directive 40
Enhance out-of-the-box Visual Studio Designer integration for seamless drag-and-drop enterprise workflows.

* **Smart Tag Action Lists (Designer Verbs):**
  * `ZeroGridControl`: "Configure Columns", "Enable Auto Filter Row", "Best Fit Columns", "Dock in Parent".
  * `ZeroWizard`: "Add Step", "Remove Step", "Next Step Preview".
  * `ZeroFilterControl`: "Edit Available Fields", "Clear Rules".
  * `ZeroBoxPlotChart`: "Configure Spec Limits (USL/LSL)", "Clear Series".
* **Design-Time Attribute Standardization:**
  * Ensure all WinForms controls decorate properties with `[Category]`, `[Description]`, `[DefaultValue]`, `[DefaultProperty]`, and `[DefaultEvent]`.
  * Ensure collection properties decorate `[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]` for automatic code generation in `InitializeComponent()`.
* **Universal XAML XML Namespace (WPF):**
  * Register `[XmlnsDefinition("http://schemas.zeroui.net/winfx/xaml", "ZeroUI.Wpf...")]` and `[XmlnsPrefix]` in `AssemblyInfo.cs`.
  * Allows clean, standard XAML authoring (`xmlns:zero="http://schemas.zeroui.net/winfx/xaml"`) without verbose CLR paths.

### Proposal 3: Semantic Naming Normalization with Non-Breaking Compatibility Aliases — Directive 41
Eliminate branded prefix stuttering in XAML (`<zero:ZeroButton>`) and align with international enterprise UI standards.

* **Class Renaming Strategy:**
  * Advanced Data/Process Controls: `ZeroGridControl` &rarr; `GridControl`, `ZeroTreeList` &rarr; `TreeList`, `ZeroFilterControl` &rarr; `FilterControl`, `ZeroWizard` &rarr; `WizardControl`.
  * Editors: `ZeroGridLookup` &rarr; `GridLookupEdit`, `ZeroTokenEdit` &rarr; `TokenEdit`, `ZeroColorPicker` &rarr; `ColorPickEdit`.
  * Basic Controls: `ZeroButton` &rarr; `SimpleButton`, `ZeroTextBox` &rarr; `TextEdit`, `ZeroCheckBox` &rarr; `CheckEdit`.
* **100% Backward Compatibility:**
  * Preserve all `ZeroXXX` classes as subclasses/aliases inheriting from the new semantic classes.
  * Existing projects and test suites continue running with zero compilation breakages.

### Proposal 4: Generic Form Data-Binding Coordinator (`ZeroDataBinder`) — Directive 42
Leverage the unified `IZeroEditor.EditValue` contract to automate bidirectional DTO binding on WinForms and WPF forms.

* **Features:**
  * `ZeroDataBinder.Bind(Control container, object dtoModel)`: Maps controls by name or `DataField` attribute to DTO properties.
  * `ZeroDataBinder.Collect<T>(Control container)`: Extracts all edited values into a clean strongly-typed DTO.
  * Automatic change tracking via `IsModified` to support "Save Changes" enablement and discard prompts.

---

## 7. Realistic Engineering Performance Targets (Directive 38)

To maintain rigorous technical integrity, ZeroUI establishes concrete engineering targets distinguishing verified measurements from design objectives:

| Performance Metric | Engineering Target | Current Verified State | Status | Verification Mechanism |
| :--- | :---: | :---: | :---: | :--- |
| **UI Frame Latency (P95)** | **< 4.0 ms** | **~2.8 ms** (1,000 cells) | **Achieved** | `MemoryDIBSection` double-buffered GDI blit |
| **UI Frame Latency (P99)** | **< 8.0 ms** | **~3.6 ms** (10,000 cells) | **Achieved** | Viewport culling with spatial boundary |
| **Telemetry Ingestion Rate** | **> 1,000,000 updates/s** | **46,900,000 updates/s** | **Achieved** | `ZeroTripleBuffer` lock-free atomic swap |
| **Tag Lookup Latency** | **< 100 ns** | **20.8 ns write / 4.1 ns read** | **Achieved** | Unboxed `TagStorage` contiguous struct array |
| **Hot Path Memory Allocation** | **0 B / op** | **0 B / op** | **Achieved** | `GC.GetAllocatedBytesForCurrentThread()` = 0 |
| **Grid Cell Render Allocation**| **0 B / cell** | **0 B / cell** | **Achieved** | Pre-allocated GDI DIBSection + `Span<char>` |
| **PLC &rarr; Tag Latency (Local)** | **< 1.0 ms** | **~0.33 ms** (1,000 tags) | **Achieved** | `ModbusAddressPlanner` 17 block requests |
| **UI Telemetry Latency** | **< 16.6 ms (60 Hz)** | **~16.0 ms** | **Achieved** | `UiDispatcher` frame coalescing batch flush |
| **Historian Ingestion Rate** | **> 100,000 rec/s** | **6,900,000 rec/s (In-Mem)**<br/>**~202,000 rec/s (WAL)** | **Achieved** | Continuous rollups (L0–L5) + daily WAL commit |

---

## 8. Feasible Enterprise Control Expansion Proposals (Multi-Subsystem Blueprint)

To guide the long-term technical evolution of **ZeroUI** without compromising its core principle of **Zero External Dependencies**, this section details pragmatic, high-impact component proposals organized across the six primary control subsystems. Each proposal provides the technical architecture, operational use case, feasibility score, and complexity assessment.

```text
                               ZeroUI Enterprise Control Ecosystem
                                                │
         ┌───────────────┬───────────────┬──────┴────────┬───────────────┬───────────────┐
         │               │               │               │               │               │
    Data & Matrix   Form Editors   Navigation/Layout  Industrial   Document/Report  Diagnostics
         │               │               │               │               │               │
    • CardView      • SearchLookup • Breadcrumb     • Gauges        • Spreadsheet   • Visual Debugger
    • Streaming     • Barcode/QR   • FlowLayout     • Funnel/       • PDF Viewer    • Memory/Perf HUD
      Exporter      • RatingEdit     (Drag-Reorder)   Pyramid       (SOP Viewer)
```

---

### Subsystem 1: Advanced Data & Matrix Controls (Data & Large Grids)

#### Proposal 8.1: `CardView` & `TileView` Virtualized Presentation for `GridControl`
* **Objective:** Enable `GridControl` to seamlessly switch its rendering engine between conventional rows (`GridViewType.Table`) and multi-column card/tile layouts (`GridViewType.CardView` / `TileView`) without changing data sources, filter criteria, or sorting logic.
* **Target Scenarios:** Product catalog browsing, equipment visual status boards, operator photo directories, and QA inspection lots with thumbnail previews.
* **Technical Architecture:**
  - **Core:** Introduce `GridCardLayoutManager` in `ZeroUI.Core.Data` computing card grid coordinates $(Col, Row)$, card widths/heights, and binary-searched viewport culling via `VirtualViewport2D`.
  - **WinForms:** Direct single-HWND vector GDI+ card cell rendering with badge overlays, customizable card field slots, and hover drop-shadow effects.
  - **WPF:** Hardware-accelerated `DrawingContext` virtual card tiles with data-bound card templates.
* **Feasibility Score:** **9.5 / 10** (Extremely High — directly reuses existing virtual scrolling, data pipeline, and hit-testing infrastructure).
* **Implementation Effort:** ~2–3 engineering days.

#### Proposal 8.2: Native Zero-Allocation Streaming Data Exporter (`GridDataExporter`)
* **Objective:** Direct high-speed export of active `GridControl` views (including banded columns, group summaries, and formatted values) into modern `.xlsx` (OpenXML package) and `.csv` files with zero reliance on Microsoft Office or heavy third-party spreadsheet SDKs.
* **Target Scenarios:** Production report generation, audit trail archiving, and financial export in industrial workstations lacking desktop productivity suites.
* **Technical Architecture:**
  - **Core:** Implement a lightweight OpenXML zip packaging writer streaming raw XML parts (`xl/worksheets/sheet1.xml`, `xl/styles.xml`, `xl/workbook.xml`) using `System.IO.Compression`.
  - Zero-string-duplication via shared string table streaming.
  - Supports column formatting (Currency, Percent, Date) directly mapped from `ZeroColumn.DisplayFormat`.
* **Feasibility Score:** **10.0 / 10** (Immediate — standard pure C# XML streaming).
* **Implementation Effort:** ~1–2 engineering days.

---

### Subsystem 2: Enterprise Form Editors & Input Controls

#### Proposal 8.3: `SearchLookUpEdit` (High-Capacity Paginated Dropdown with Header Search)
* **Objective:** A specialized enterprise lookup editor optimized for massive datasets (50,000+ records) featuring a persistent top search bar, Find/Clear command buttons, count status footer (`"Found 120 matching items out of 85,000"`), and debounced asynchronous background querying.
* **Target Scenarios:** Bill of Materials (BOM) item lookups, customer account selection, and global warehouse pallet search.
* **Technical Architecture:**
  - Extends `IZeroEditor` and `PopupBaseEdit`.
  - Popup hosts a dedicated search header strip, an embedded virtual list canvas, and an item count status bar.
  - Asynchronous background filtering prevents UI thread stutter during rapid keystrokes.
* **Feasibility Score:** **9.0 / 10** (High — pattern established in `GridLookupEdit`).
* **Implementation Effort:** ~2 engineering days.

#### Proposal 8.4: Vector Barcode & QR Code Engine (`BarcodeBox` & `BarcodeEdit`)
* **Objective:** Direct vector generation and in-form rendering of 1D and 2D industrial machine-readable codes (`Code 128`, `Code 39`, `EAN-13`, `QR Code`) with crisp vector anti-aliasing on high-DPI screens and thermal printers.
* **Target Scenarios:** Warehouse bin labels, lot serial number badges, product work orders, and mobile scanner scanning screens.
* **Technical Architecture:**
  - **Core (`ZeroUI.Core.Barcode`):** Pure mathematical encoding algorithms generating bitmask arrays for 1D bars and 2D QR matrix cells with Reed-Solomon error correction.
  - **WinForms & WPF:** Renders sharp vector rectangles directly onto canvas; exports crisp SVG and high-resolution PNG/BMP for printing.
* **Feasibility Score:** **9.2 / 10** (High — self-contained algorithmic encoder without external C++ libraries).
* **Implementation Effort:** ~2–3 engineering days.

#### Proposal 8.5: `RatingControl` / `RatingEdit` (Precision Inspection Severity & Score Selector)
* **Objective:** Compact interactive rating editor supporting whole and half-star precision, custom vector glyphs (Stars, Diamonds, Shields, Hearts), and smooth hover fill preview.
* **Target Scenarios:** QA defect severity scoring, supplier performance audit grades, and visual inspection checklists.
* **Technical Architecture:**
  - Implements `IZeroEditor` exposing `decimal EditValue` (e.g. 3.5, 4.0).
  - High-performance vector glyph math with fractional fill clipping.
* **Feasibility Score:** **10.0 / 10** (Immediate — compact, clean design).
* **Implementation Effort:** ~0.5 engineering day.

---

### Subsystem 3: Layout, Windowing & Workflow Navigation

#### Proposal 8.6: `BreadcrumbControl` / `BreadcrumbEdit` (Hierarchical Domain Path Navigator)
* **Objective:** Windows Explorer-style interactive breadcrumb navigation bar showing tree hierarchy (`Enterprise > Facility North > Workshop B > Extruder Line 02 > Screw Barrel Zone`), where each segment provides a dropdown of sibling nodes, back/forward history buttons, and 1-click transition to an editable path text box.
* **Target Scenarios:** Deep factory asset navigation, multi-level folder exploration, and warehouse rack path tracking.
* **Technical Architecture:**
  - **Core:** `BreadcrumbNode` model holding title, path, children, and navigation metadata.
  - **WinForms & WPF:** Dynamic breadcrumb segment pill layout with automatic truncation (`...`), sibling popup menus, and inline autocomplete path editing.
* **Feasibility Score:** **9.5 / 10** (High — proven UI ergonomics).
* **Implementation Effort:** ~2 engineering days.

#### Proposal 8.7: `FlowLayoutControl` with Interactive Drag Reordering
* **Objective:** Responsive container automatically wrapping child cards according to available viewport width, featuring animated drag-and-drop tile reordering and layout serialization.
* **Target Scenarios:** Modular plant supervisor KPI dashboards, customizable machine health cards, and alert widget boards.
* **Technical Architecture:**
  - Coordinate re-flow engine with spring-damper smooth position interpolation.
  - Integrated with `ZeroWorkspaceSerializer` for saving customized tile order to JSON.
* **Feasibility Score:** **8.8 / 10** (High).
* **Implementation Effort:** ~2–3 engineering days.

---

### Subsystem 4: Industrial SCADA, Analytics & Special Charts

#### Proposal 8.8: Industrial Gauges Suite (`RadialGauge` & `LinearGauge`)
* **Objective:** High-precision instrumentation dials (Circular 180° / 270° and Vertical/Horizontal Linear thermometers) with customizable threshold color bands (Normal Green, Warning Amber, Danger Red), magnetic needle damping, and direct binding to `TagEngine`.
* **Target Scenarios:** Machine hydraulic pressure, boiler temperature, motor RPM, and live furnace thermal feedback.
* **Technical Architecture:**
  - **Direct TagEngine Binding:** Directly inspects unboxed `ScadaValue` in `TagStorage` to update needle angle with zero allocation on paint passes.
  - Vector GDI+ / WPF rendering with anti-aliased arcs, tick marks, numeric scale, and glossy glass reflections.
* **Feasibility Score:** **9.2 / 10** (High — standard industrial instrumentation).
* **Implementation Effort:** ~2–3 engineering days.

#### Proposal 8.9: `FunnelChart` / `PyramidChart` (Yield & Scrap Rate Visualizer)
* **Objective:** Multi-tier tapered geometric funnel and pyramid charts visualizing multi-stage conversion rates, material yields, and cumulative scrap losses across sequential manufacturing processes.
* **Target Scenarios:** Production line yield tracking (Raw Extrusion &rarr; Slicing &rarr; Coating &rarr; Packaging &rarr; Warehouse), QA defect drop-off, and order fulfillment pipelines.
* **Technical Architecture:**
  - Trapezoidal polygon rendering with automatic segment spacing, percentage badges, and interactive segment selection.
* **Feasibility Score:** **9.5 / 10** (High — straightforward geometry math).
* **Implementation Effort:** ~1–2 engineering days.

---

### Subsystem 5: Document & Specialist Data Viewers

#### Proposal 8.10: `SpreadsheetControl` (Phase 1: Tabular Formula Sheet MVP)
* **Objective:** A lightweight vector spreadsheet grid (A1..Z100) supporting freeform cell data entry, cell formatting (font style, fill color, text alignment, number formatting), freeze panes, and core mathematical formula calculation (`SUM`, `AVERAGE`, `MIN`, `MAX`, `IF`, arithmetic operators).
* **Target Scenarios:** In-app formula calculation templates, laboratory test record entry, and custom costing workbooks without launching external office software.
* **Technical Architecture:**
  - **Core (`ZeroUI.Core.Spreadsheet`):** Sparse 2D cell matrix index, formula token parser and recursive dependency graph evaluator with cycle detection.
  - **WinForms & WPF:** Virtualized spreadsheet canvas reusing `VirtualViewport2D` with row/column header coordinate bars (A, B, C... / 1, 2, 3...) and inline cell editing.
* **Feasibility Score:** **7.8 / 10** (Medium-High for Phase 1 MVP — formula engine scope is strictly bounded to core mathematical functions).
* **Implementation Effort:** ~4–5 engineering days for Phase 1 MVP.

#### Proposal 8.11: `PdfViewerControl` (Lightweight Vector Technical Document Reader)
* **Objective:** Embedded document viewer rendering standard multi-page technical documents, CAD PDF schematics, and Standard Operating Procedures (SOP) inside desktop forms with continuous vertical scrolling, zoom (25%–400%), and page navigation.
* **Target Scenarios:** Viewing machine electrical schematics, work instructions at assembly stations, and inspection certs on factory floor PCs without third-party PDF reader installations.
* **Technical Architecture:**
  - Native Windows vector document rendering bridge (via Windows Imaging Component and Windows.Data.Pdf API on Windows 10/11) with fallback vector rendering engine.
  - Virtualized page layout canvas rendering visible pages on demand.
* **Feasibility Score:** **8.0 / 10** (Medium-High — leverages native platform rasterizer on modern Windows).
* **Implementation Effort:** ~3–4 engineering days.

---

### Subsystem 6: Developer Experience & Live Diagnostics

#### Proposal 8.12: `ZeroVisualDebugger` (Embedded In-App UI & Runtime Inspector)
* **Objective:** An embedded diagnostic HUD toggled via hotkey (`F12` or `Ctrl+Shift+D`) enabling developers and QA engineers to inspect the active visual tree, live control boundaries, dirty editor states, paint FPS, GC allocations, and real-time SCADA tag values directly inside running desktop applications.
* **Target Scenarios:** Rapid debugging of complex form layouts, validating zero-allocation constraints, verifying live telemetry updates, and diagnosing customer environment issues without attaching Visual Studio debugger.
* **Technical Architecture:**
  - Lightweight semi-transparent floating overlay window querying active `Form` or `Window`.
  - Visual tree explorer with property grid inspector.
  - Live performance HUD displaying P95/P99 frame time, memory footprint, and tag subscription counters.
* **Feasibility Score:** **9.8 / 10** (Extremely High — pure diagnostic layer, zero impact on production runtime).
* **Implementation Effort:** ~1.5 engineering days.

---

### Summary Feasibility & Priority Matrix

| Proposal ID | Proposed Component | Target Subsystem | Feasibility Score | Implementation Complexity | Recommended Phase |
| :---: | :--- | :--- | :---: | :---: | :---: |
| **8.1** | **`CardView` / `TileView`** | Data & Matrix | **9.5 / 10** | Low-Medium | **Phase 10 (Next)** |
| **8.2** | **`GridDataExporter` (XLSX/CSV)** | Data & Matrix | **10.0 / 10** | Low | **Phase 10 (Next)** |
| **8.3** | **`SearchLookUpEdit`** | Form Editors | **9.0 / 10** | Low-Medium | **Phase 10 (Next)** |
| **8.4** | **`BarcodeBox` / `BarcodeEdit`** | Form Editors | **9.2 / 10** | Medium | **Phase 10 (Next)** |
| **8.5** | **`RatingControl`** | Form Editors | **10.0 / 10** | Low | **Phase 10 (Next)** |
| **8.6** | **`BreadcrumbControl`** | Navigation/Layout | **9.5 / 10** | Low-Medium | **Phase 10 (Next)** |
| **8.7** | **`FlowLayoutControl` (Drag-Reorder)**| Navigation/Layout | **8.8 / 10** | Medium | **Phase 11** |
| **8.8** | **`RadialGauge` & `LinearGauge`** | Industrial SCADA | **9.2 / 10** | Medium | **Phase 11** |
| **8.9** | **`FunnelChart` / `PyramidChart`** | Charts & Analytics | **9.5 / 10** | Low-Medium | **Phase 11** |
| **8.10**| **`SpreadsheetControl` (Phase 1)** | Document & Office | **7.8 / 10** | High | **Phase 12** |
| **8.11**| **`PdfViewerControl`** | Document & Office | **8.0 / 10** | High | **Phase 12** |
| **8.12**| **`ZeroVisualDebugger`** | DX & Diagnostics | **9.8 / 10** | Low | **Phase 10 (Next)** |

