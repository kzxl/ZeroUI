# ZeroUI Implementation Roadmap

## 1. Release Timeline & Milestones

| Phase | Milestone Name | Primary Focus | Status | Key Deliverables |
| :---: | :--- | :--- | :---: | :--- |
| **Phase 1** | **Core Virtualization Engine** | `ZeroUI.Core` (.NET Standard 2.0) | **Completed** | `PrefixSumArray`, `VirtualViewport2D`, `ZeroBufferPool`, `Channel` data queues. |
| **Phase 2** | **WinForms MVP (`ZeroGrid`)** | `ZeroUI.WinForms` (net462, net8.0) | **Completed** | Single-HWND Control, Unmanaged Fast GDI blit, 32-bit `SetScrollInfo`, 1M/10M-row benchmark. |
| **Phase 3** | **WPF Port (`ZeroGrid`)** | `ZeroUI.Wpf` (net462, net8.0) | **Completed** | `DrawingVisual` host, XAML attached properties, `D3DImage` DirectX 11 pipeline. |
| **Phase 4** | **Enterprise Grid Features** | Interactivity & Production Readiness | **Completed** | Floating In-place Editor, Frozen Columns/Rows, Multi-column sorting, Fast filtering. |
| **Phase 5** | **Expanded Control Suite** | Additional High-Perf Controls | **Completed** | `ZeroTreeList`, `ZeroHeatmap`, `ZeroLookup`, `ZeroDateRangePicker`, `ZeroKanbanBoard`... |
| **Phase 6** | **Theming & Packaging** | Distribution & Design System | **Active** | Obsidian Dark / Clean Light Themes, Headless CLI benchmarks, Automated screenshots. |
| **Phase 7** | **SCADA & Deterministic Runtime** | Industrial Edge & Telemetry Engine | **Completed** | `ZeroRuntime`, 3-Tier Pipeline, `ZeroTripleBuffer`, `TagStorage`, `ZeroAnimationClock`, `ZeroScene`. |

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
* [ ] Author XML documentation and publish NuGet packages (`ZeroUI.Core`, `ZeroUI.WinForms`, `ZeroUI.Wpf`).

### Phase 7: SCADA & Deterministic Runtime (Directives 1–27)
* [x] **`ZeroRuntime`:** Deterministic 7-cycle master scheduler coordinating PLC (10ms), Logic (10ms), Telemetry (16ms), UI (16ms), Historian (100ms), Cleanup (1s), and Health (5s) cycles with drift compensation.
* [x] **3-Tier SCADA Pipeline:** `ScadaPipelineCoordinator` and `ZeroTripleBuffer<T>` decoupling fast 10 kHz field acquisition from medium 1 kHz calculations and slow 30–60 Hz UI display.
* [x] **`TagStorage` & `ZeroTagEngine` v2:** Contiguous unboxed array tag registry with atomic dirty bitmasking and inverted index listener dispatch (>48M writes/s, >244M reads/s).
* [x] **`ZeroAnimationClock` Core Primitive:** 60Hz centralized ticker with lock-free Copy-On-Write arrays and synchronized ISA-18.2 phases, eliminating scattered timers across all controls.
* [x] **`TimeSeriesPyramid` & High-Scale LTTB:** Multi-resolution continuous rollups (L0–L5) and zero-alloc 1M/10M point downsampling (~340M pts/s) enabling instantaneous $O(\text{screen pixels})$ zoom.
* [x] **Industrial Scene Graph (`ZeroScene`):** Single-HWND plant canvas with `GridSpatialIndex` spatial culling and `SceneNode` hierarchy (`TankNode`, `PumpNode`, `PipeNode`, `ValveNode`, `SensorNode`, `AlarmNode`).
* [x] **Modbus Address Optimization:** `ModbusAddressPlanner` coalescing disjoint register tags into contiguous MBAP block reads (up to 98.3% network packet reduction).
* [x] **Unified Benchmark Suite:** `ZeroUI.Benchmarks` CLI covering Categories A to F (Rendering, Virtual Grid, Telemetry, TagEngine, Modbus, Historian).

---

## 3. Future Enhancements & Proposals Catalog

For detailed architectural evaluations, design trade-offs, and implementation specifications of upcoming proposals, refer to [ZeroUI Proposals Catalog](file:///e:/15.%20Other/dotnet/libs/ZeroUI/docs/proposals.md).


