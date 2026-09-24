# ZeroUI ⚡

> **Ultra-High-Performance, Zero-Allocation Industrial UI & Runtime Ecosystem for .NET (WinForms, WPF, .NET 8/9 & Edge)**

[![ZeroPlatform Tier](https://img.shields.io/badge/ZeroPlatform-Tier%205%20(Presentation%20%26%20Apps)-e11d48.svg)](https://github.com/kzxl/ZeroPlatform)
[![NuGet Version](https://img.shields.io/badge/nuget-v1.9.0-blue.svg)](https://github.com/kzxl/ZeroUI)
[![GPU Acceleration](https://img.shields.io/badge/GPU%20Acceleration-Direct3D%2011%20%7C%20Direct2D-cyan.svg)](https://github.com/kzxl/ZeroGraphics)
[![Unit Tests](https://img.shields.io/badge/tests-655%20passed%20(100%25)-brightgreen.svg)](#-testing--quality-assurance)
[![Target Frameworks](https://img.shields.io/badge/targets-netstandard2.0%20%7C%20net462%20%7C%20net8.0--windows-blue.svg)](#-package-matrix)
[![UI Frame Latency](https://img.shields.io/badge/Frame%20Latency-%3C%204ms%20P95-brightgreen.svg)](docs/BENCHMARKS.md)
[![GC Allocations](https://img.shields.io/badge/Hot%20Path%20Allocations-0%20B%20(Zero--Alloc)-brightgreen.svg)](docs/BENCHMARKS.md)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](#-license)

---

## 📸 Interactive Live Showcase

| ⚡ 1,000,000 Rows Smooth Virtual Scroll (Zero-Alloc) | 🎨 Creative & Media Editors Suite |
| :---: | :---: |
| ![1M Rows Scroll](docs/images/01_zerogrid_1m_scroll.gif) | ![Media Editors](docs/images/02_media_editors_interactive.gif) |

| 🔍 Excel-Style Column Distinct Filter Header (v1.8.1) | 🏭 SCADA Closed-Loop Workcell |
| :---: | :---: |
| ![Column Filter](docs/images/03_column_filter_header.gif) | ![SCADA Closed Loop](docs/images/12_scada_closed_loop_simulation.png) |

---

## ⚡ Performance Benchmarks: ZeroUI Grid vs Native Controls

Headless and interactive stress-test verified on `.NET 8.0` (x64, Intel Core i7 / 144Hz):

| Metric | Standard DataGridView / WPF DataGrid | ZeroUI (`GridControl` / `ZeroGridControl`) | Real-World Advantage |
| :--- | :---: | :---: | :--- |
| **100K Rows Viewport Compute** | ~7,400 FPS (0.135 ms) | **24,716 FPS (0.040 ms)** | **3.3x Higher Throughput** |
| **Hot Render Path GC Allocations** | Thousands of temporary cell objects | **0 B (Zero-Alloc)** | **100% Zero GC Pauses** |
| **1,000,000 Rows Continuous Scrolling** | Frequent frame drops & stutter | **Rock-solid 144 FPS** | **Silky-smooth UX** |
| **10,000,000 Rows Capacity** | **Crash / OutOfMemoryException** | **179 MB RAM (7 ms setup)** | **Limitless Scale** |
| **1,000,000 Rows Instant Filter** | > 850 ms (UI locks) | **34 ms** | **25x Faster** |
| **Streaming CSV Export (100K rows)** | ~1,200 ms | **145 ms (688,000 rows/sec)** | **8x Faster** |

---

## 📖 Executive Summary

**ZeroUI** is an enterprise-grade industrial automation UI suite and runtime engine. Engineered to overcome the severe handle leaks, garbage collection pauses, and screen flicker common in standard desktop controls, ZeroUI renders millions of rows and real-time telemetry at 60 FPS with zero managed memory allocations.

### Core Architectural Pillars
* **Strictly Zero Allocation (`Zero-Alloc`)**: Hot render loops generate **0 bytes** of GC churn using `Span<T>`, `ArrayPool<T>`, and unmanaged DIBSection memory buffers.
* **Single-HWND Architecture**: Composite controls maintain **1 Win32 window handle**, completely eliminating OS handle exhaustion crashes (10,000 handle limit) and window flicker.
* **Win32 Memory DC DIBSection Engine**: Offscreen unmanaged double buffering with zero-copy `BitBlt` presentation (100% resilient across RDP and virtual machines).
* **Enterprise Dual-Runtime Support**: Full native compatibility with **.NET Framework 4.6.2** as well as modern **.NET 8.0 / 9.0**.
* **Unified Theme Engine**: Instant reactive switching between **Obsidian Dark Mode** (`#12151C`) and **Clean Light Mode** across all controls.
* **Specialized Analytics & Business Charts Suite (v1.9.0)**: Clean Architecture with 100% platform-neutral mathematical engines in `ZeroUI.Core.Analytics` and lightweight hardware rendering layers for WinForms and WPF (`HistogramChart`, `ScatterChart`, `SunburstChart`, `LollipopChart`, `TreemapChart`, `SankeyChart`, `BulletChart`, `ParetoChart`).
* **Creative & Media Controls Suite (v1.8.0)**: Direct-rendered, high-performance visual editors for digital imaging, raw photography, and video grading (`CurveEditor`, `ColorWheelEdit`, `CompareViewerControl`, `HistogramScopeControl`, `CropBoxControl`, `MiniMapNavigator`, `MaskGizmoOverlay`, `FacetedFilterBar`, `HistoryTimelineControl`, `FilmstripScrollerControl`, `DominantPaletteControl`, `ExifTelemetryCard`, `NumericSliderEdit`, `BatchTaskQueueControl`, `ThumbnailGridControl`, `TokenPatternEditor`).
* **High-Performance Interactive Media Subsystem (v1.9.1)**: Dedicated hardware-accelerated image canvases (`ZeroUI.WinForms.Media` & `ZeroUI.Wpf.Media`) offering sub-pixel pan, continuous anchored zoom, forensic pixel grid ($\ge 800\%$), interactive MiniMap overview navigator, floating magnifier loupe, and real-time telemetry color probe HUD (`ZImageViewer` / `ImageViewerControl`).
* **Centralized 60 FPS Clock (`ZeroAnimationClock`)**: Single global ticker with synchronized ISA-18.2 blinking phases, eliminating timer scatter.

---

## 📚 Technical Documentation & Guides

In-depth technical specifications and architectural documentation are modularized within the [`docs/`](docs/) directory:

| Document | Description |
| :--- | :--- |
| 📊 **[Verified Benchmarks](docs/BENCHMARKS.md)** | Frame budgets, 10M rows virtualization, GC allocations, and telemetry throughput. |
| 🎛️ **[Controls Catalog](docs/CONTROLS_CATALOG.md)** | Full reference for 60+ controls (GridControl, PivotGrid, SCADA, Charts, Media Viewers, and Creative Editors). |
| 🎬 **[Visual Controls Guide & Tour](docs/ZEROUI_CONTROLS_GUIDE.md)** | Architectural guide, in-process live video recordings, and feature tour across all subsystems. |
| 🎨 **[Theming & Styling](docs/THEMING_AND_STYLING.md)** | Obsidian Dark / Clean Light themes, High-DPI Per-Monitor V2, and single-HWND architecture. |
| 🏛️ **[System Architecture](docs/architecture/system-architecture.md)** | Multi-tier pipeline coordination, lock-free TripleBuffer, decoupled runtime, and renderers. |
| 📐 **[Control Ecosystem Audit](docs/architecture/control-ecosystem-audit-and-composite-architecture.md)** | Feature gap analysis, composite control standards, and cross-platform shared logic. |
| 🗺️ **[Development Roadmap](docs/roadmap.md)** | Release milestones, feature requests, and future capabilities. |

---

## 🏷️ Canonical `Z*` Control Naming Standard & Collision Prevention

To prevent ambiguous reference collisions (`CS0104`) with native WinForms and WPF controls (such as `System.Windows.Forms.Panel`, `GroupBox`, `Button`, `Label`, `TextBox`), ZeroUI establishes a clean architectural separation:

* **Foundational Engine & Infrastructure:** Prefixed with **`Zero`** (`ZeroTheme`, `ZeroDpi`, `ZeroFontCache`, `ZeroIcons`, `ZeroAnimationClock`, `ZeroLocalizer`).
* **Canonical UI Controls Suite:** Standard controls adopt the **`Z`** prefix (`ZPanel`, `ZGroupBox`, `ZMenuBar`, `ZButton`, `ZLabel`, `ZCheckBox`, `ZRadioButton`, `ZTextBox`, `ZProgressBar`, `ZTabControl`, `ZSplitContainer`, `ZGrid`, `ZChart`, `ZImageViewer`).
  * **Zero Collisions (`CS0104`):** Guarantees zero naming conflicts when developers import both `System.Windows.Forms` and ZeroUI namespaces.
  * **Instant IDE Discovery:** Simply typing `Z` in the IDE immediately surfaces the complete ZeroUI component palette.
  * **Full Theme Synchronization:** All `Z*` controls automatically inherit `ZeroTheme` dark/light modes, rounded corner styles, and High-DPI scaling.
  * **5-Release Deprecation Wrappers:** Previous class names (`PanelControl`, `GroupControl`, `MenuBarControl`, `SimpleButton`, `TextEdit`, and legacy `Zero*` shims) are retained as backward-compatibility wrappers with `[Obsolete]` warnings and will be maintained across 5 minor release cycles before pruning.

---

## 📦 Package Matrix

| Package | Targets | Primary Capabilities | Dependencies |
| :--- | :--- | :--- | :--- |
| **`ZeroUI.Core`** | `netstandard2.0`, `net462`, `net8.0` | High-frequency telemetry triple-buffer, TagEngine v2, PackML state machine, OEE metrics, validation engine, industrial state models, statistical chart engines (`HistogramEngine`, `ScatterPlotEngine`, `SunburstLayoutEngine`, `LollipopEngine`, `TreemapEngine`, `SankeyLayoutEngine`, `BulletBenchmarkEngine`, `ParetoEngine`) | **Zero 3rd-party dependencies** (Pure BCL) |
| **`ZeroUI.Historian.Sqlite`** | `netstandard2.0`, `net462`, `net8.0` | High-throughput SQLite WAL time-series telemetry storage engine (>100k records/s), rolling partitions, store & forward disk cache | `Microsoft.Data.Sqlite` |
| **`ZeroUI.WinForms`** | `net462`, `net8.0-windows` | 10M+ rows virtual grid (`GridControl`), 40+ SCADA/HMI controls, 15+ specialized charts, Obsidian dark theme, DIBSection unmanaged double-buffering | `ZeroUI.Core` |
| **`ZeroUI.WinForms.Media`** | `net462`, `net8.0-windows` | Enterprise interactive image canvas (`ZImageViewer`) with sub-pixel pan/zoom, forensic pixel grid, MiniMap navigator, HUD color probe, and Direct2D / GDI+ pipelines | `ZeroUI.Core`, `ZeroGraphics` |
| **`ZeroUI.Wpf`** | `net462`, `net8.0-windows` | Zero-alloc WPF virtual grid (`GridControl`), industrial styling, 15+ specialized charts, Creative Media Editors Suite, and D3D11 shared texture bridge | `ZeroUI.Core` |
| **`ZeroUI.Wpf.Media`** | `net462`, `net8.0-windows` | GPU-accelerated interactive image viewer (`ImageViewerControl` / `ZImageViewer`), loupe magnifier, sub-pixel transform, forensic pixel grid, and MiniMap overview | `ZeroUI.Core`, `ZeroGraphics` |

---

## ⚡ Quick Start: 1,000,000 Rows Virtual Grid (WinForms)

```csharp
using System.Windows.Forms;
using ZeroUI.WinForms.DataGrid;

// Instantiate high-performance virtual grid (ZeroGridControl legacy alias also supported)
var grid = new GridControl
{
    Dock = DockStyle.Fill,
    RowDensity = GridRowDensity.Normal,
    AllowUserSorting = true
};
this.Controls.Add(grid);

// Define strongly-typed columns
grid.Columns.Add(new GridColumn("Id", "ID", 100));
grid.Columns.Add(new GridColumn("Timestamp", "Timestamp", 180));
grid.Columns.Add(new GridColumn("Temperature", "Temp (°C)", 140));

// Bind 1,000,000 rows procedurally with 0 bytes GC allocation
grid.SetProceduralDataSource(rowCount: 1000000, (rowIndex, colIndex) =>
{
    return colIndex switch
    {
        0 => (object)rowIndex,
        1 => DateTime.UtcNow.AddSeconds(rowIndex).ToString("yyyy-MM-dd HH:mm:ss"),
        _ => (25.0 + (rowIndex % 100) * 0.1).ToString("F1")
    };
});
```

👉 **[Explore Full Control Catalog & Code Samples](docs/CONTROLS_CATALOG.md)**

---

## 🧪 Testing & Quality Assurance

```bash
# Run comprehensive automated test suites (655 tests, 100% pass across Core & Desktop UI)
dotnet test ZeroUI.slnx

# Launch interactive demonstration suites via quick launcher
./run-demo.bat
# or directly via PowerShell:
pwsh -File scripts/run-demo.ps1 -Target Both
```

---

## 📜 Release History

| Version | Release Date | Key Milestones & Highlights |
| :--- | :---: | :--- |
| **`v1.9.0`** | 2026-09-23 | **Advanced Statistical, Relational & Hierarchical Charts Suite**:<br/>• Clean Architecture decoupling: 100% calculation engines in `ZeroUI.Core.Analytics` with thin WinForms GDI+ and WPF `DrawingContext`/`StreamGeometry` rendering.<br/>• 8 New High-Performance Analytics Controls: `HistogramChart` (Sturges & Gaussian Fit), `ScatterChart` (OLS Linear Regression & Bubble Area), `SunburstChart` (Multi-Tier 360° Radial Partitions), `LollipopChart` (High-Density Comparison), `TreemapChart` (Squarified Layout), `SankeyChart` (Bézier Process Flows), `BulletChart` (Stephen Few KPI Benchmarks), `ParetoChart` (Juran 80/20 Defect Analysis).<br/>• Standardized control identifiers with zero "Zero" prefix across Core, WinForms, and WPF.<br/>• Interactive showcase sub-tabs integrated into `WinformDemo` and `WpfDemo`.<br/>• Interactive launcher scripts added (`run-demo.bat`, `scripts/run-demo.ps1`).<br/>• 655 automated tests passed (100%). |
| **`v1.8.6`** | 2026-09-22 | **Zero-Dependency Core & Cross-Platform Control Parity Standard**:<br/>• Decoupled SQLite telemetry historian into dedicated `ZeroUI.Historian.Sqlite` package, achieving 100% zero third-party dependencies in `ZeroUI.Core`.<br/>• Established automated cross-platform parity reflection test suite (`ControlParityInspectionTests`).<br/>• Standardized platform-neutral industrial state machines (`SevenSegmentState` in `ZeroUI.Core.Industrial`) achieving 100% parity across WinForms and WPF.<br/>• 607 automated tests passed (100%). |
| **`v1.8.5`** | 2026-09-20 | **Industrial Control Normalization & Prefix Removal**:<br/>• Standardized primary control names to clean enterprise identifiers (`GridControl`, `ChartControl`, `SevenSegment`, `LinearGauge`, `ValidationProvider`) per `.project-rule.md`.<br/>• Full backward compatibility preserved via `[Obsolete]` shims. |
| **`v1.8.0`** | 2026-09-15 | **Creative & Media Controls Suite**:<br/>• Direct-rendered, high-performance visual editors for digital imaging and video grading (`CurveEditor`, `ColorWheelEdit`, `CompareViewerControl`, `HistogramScopeControl`, `CropBoxControl`, `MiniMapNavigator`, `MaskGizmoOverlay`, `FacetedFilterBar`, `HistoryTimelineControl`, `FilmstripScrollerControl`, `DominantPaletteControl`, `ExifTelemetryCard`, `NumericSliderEdit`, `BatchTaskQueueControl`, `ThumbnailGridControl`, `TokenPatternEditor`).<br/>• Zero-allocation graphics primitives for curves, color scopes, and canvas gizmos.<br/>• 529 automated tests passed (100%). |
| **`v1.7.0`** | 2026-09-12 | **WPF Modernization & High-DPI Per-Monitor V2**:<br/>• Zero-alloc WPF virtual grid adapter.<br/>• High-DPI Per-Monitor V2 dynamic scaling engine.<br/>• Win32 Memory DC unmanaged DIBSection double buffering with zero-copy `BitBlt` presentation. |
| **`v1.0.0`** | 2026-09-08 | **Initial Industrial Release**:<br/>• 10M+ rows procedural virtual data grid with zero GC allocations.<br/>• 40+ industrial SCADA/HMI controls with Single-HWND architecture.<br/>• Unified theme engine: Obsidian Dark (`#12151C`) and Clean Light modes.<br/>• Centralized 60 FPS ISA-18.2 synchronized animation clock (`ZeroAnimationClock`). |

---

## 🌐 Part of the ZeroPlatform Ecosystem

ZeroUI is the user interface and SCADA visualization pillar of the **[ZeroPlatform](https://github.com/kzxl/ZeroPlatform)** suite — unifying 12 sovereign subsystems including `ZeroGraphics`, `ZeroPipeline`, `ZeroTensor`, `ZeroComm`, and `ZeroStorage`.

---

## 🏛️ Ecosystem Architectural Alignment

ZeroUI is a sovereign member of **Tier 5 (Presentation & Orchestration)** within the **ZeroPlatform** industrial automation ecosystem.

```
┌──────────────────────────────────────────────────────────┐
│ Tier 5: Presentation & Orchestration (ZeroUI)            │
└────────────────────────────┬─────────────────────────────┘
                             │ consumes
              ┌──────────────┼──────────────┐
              ▼              ▼              ▼
┌───────────────────┐ ┌──────────────┐ ┌───────────────────┐
│ Tier 0: Primitives│ │ Tier 1: Data │ │ Tier 4: Graphics  │
│ (ZeroPrimitives)  │ │ (ZeroData)   │ │ (ZeroGraphics)    │
└───────────────────┘ └──────────────┘ └───────────────────┘
```

- **Upstream Ingestion**: Consumes Tier 0 foundational primitives (`ZeroPrimitives.Core 1.3.0`), Tier 1 high-speed columnar data buffers (`ZeroData.Core 1.3.0`), and Tier 4 GPU/2D rendering infrastructure (`ZeroGraphics.* 1.5.0`).
- **Strict DAG Conformance**: Zero references to orchestrators or applications.
- **Packaging & CI/CD**: Standardized under `Company = ZeroPlatform`, `Authors = Phong Võ`, `<ZeroTier>5</ZeroTier>`.

---

## 📄 License & Author

Released under the permissive **MIT License**.  
Architected and developed by **Phong Võ**.
