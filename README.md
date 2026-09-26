# ZeroUI ⚡

> **Ultra-High-Performance, Zero-Allocation Industrial UI & Runtime Ecosystem for .NET (WinForms, WPF, .NET 8/9 & Edge)**

[![ZeroPlatform Tier](https://img.shields.io/badge/ZeroPlatform-Tier%205%20(Presentation%20%26%20Apps)-e11d48.svg)](https://github.com/kzxl/ZeroPlatform)
[![NuGet Version](https://img.shields.io/badge/nuget-v1.11.0-blue.svg)](https://github.com/kzxl/ZeroUI)
[![GPU Acceleration](https://img.shields.io/badge/GPU%20Acceleration-Direct3D%2011%20%7C%20Direct2D-cyan.svg)](https://github.com/kzxl/ZeroGraphics)
[![Unit Tests](https://img.shields.io/badge/tests-836%20passed%20(100%25)-brightgreen.svg)](#-testing--quality-assurance)
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
* **Industrial Security & Identity Management Suite (v1.11.0)**: Decoupled enterprise authentication, role-based authorization (RBAC), multi-modal login (Username/Password, quick PIN, RFID/NFC hardware badge dongle), on-screen touch virtual keyboard (`ZVirtualKeyboard`), idle timeout heartbeat monitor with auto-lock screen overlay (`ZLockScreenOverlay`), user management console (`ZUserManager`), visual permission matrix (`ZRoleMatrix`), declarative RBAC extender (`ZAuthorizeExtender` / `ZAuthorize`), and 21 CFR Part 11 audit log viewer (`ZOperationLogViewer`).
* **Enterprise Extended Controls Suite (v1.10.1)**: 21 comprehensive production controls covering async autocomplete, command palettes, notification drawers, ISA-18.2 alarm summaries, calendars, cron builders, gauge & bubble charts, signature pads, JSON/code/markdown editors, tree views, and manufacturing timelines.
* **Interactive Guided Tour & Onboarding Engine (v1.10.0)**: Native spotlight aperture cutout masking (`Region.Exclude` / `CombinedGeometry.Exclude`), semi-transparent glass backdrop dimming, collision-free adaptive popovers, and full keyboard navigation across WinForms and WPF (`ZTour`).
* **Specialized Creative & Forensic Inspection Media Suite (v1.10.0)**: High-performance vector defect tagging with direct bitmap burning (`ZAnnotationCanvas`), optical precision metrology caliper (`ZMeasurementRuler`), dynamic screen-capture security stamps (`ZWatermarkOverlay`), GPU-accelerated video playback (`ZVideoPlayer`), symmetrical audio waveform scrubbers (`ZAudioWaveform`), and document straightening/binarization (`ZDocumentDeskew`).
* **Specialized Analytics & Business Charts Suite (v1.9.0)**: Clean Architecture with 100% platform-neutral mathematical engines in `ZeroUI.Core.Analytics` and lightweight hardware rendering layers for WinForms and WPF (`HistogramChart`, `ScatterChart`, `SunburstChart`, `LollipopChart`, `TreemapChart`, `SankeyChart`, `BulletChart`, `ParetoChart`).
* **Creative & Media Controls Suite (v1.8.0 / v1.9.1)**: Direct-rendered, high-performance visual editors for digital imaging, raw photography, inspection, and video grading cleanly segregated in `ZeroUI.Wpf.Media` and `ZeroUI.WinForms.Media` (`ZImageViewer`, `ZPictureEdit`, `ZCurveEditor`, `ZColorWheel`, `ZCompareViewer`, `ZHistogramScope`, `ZCropBox`, `ZMiniMapNavigator`, `ZMaskGizmoOverlay`, `ZHistoryTimeline`, `ZFilmstripScroller`, `ZDominantPalette`, `ZExifTelemetryCard`, `ZBatchTaskQueue`, `ZThumbnailGrid`, `ZTokenPatternEditor`).
* **Centralized 60 FPS Clock (`ZeroAnimationClock`)**: Single global ticker with synchronized ISA-18.2 blinking phases, eliminating timer scatter.

---

## 🎛️ Enterprise Extended Controls Suite (v1.10.1)

ZeroUI delivers 21 fully implemented, production-ready enterprise controls across **Windows Forms** and **WPF**, adhering to strict single-HWND architecture, zero-allocation theming, and comprehensive test coverage:

| Category | Control | WinForms | WPF | Core Capabilities |
| :--- | :--- | :---: | :---: | :--- |
| **Input & Search** | **`ZAutoComplete`** | ✅ | ✅ | Async debounce filtering, keyboard navigation (Up/Down/Enter/Esc), theme-aware suggestion popup dropdown. |
| **Overlay & Navigation** | **`ZCommandPalette`** | ✅ | ✅ | Quick-launcher overlay (Ctrl+K), fuzzy search filtering, action execution triggers, and backdrop dimming. |
| **Feedback & Alerts** | **`ZNotificationCenter`** | ✅ | ✅ | Severity-grouped notification drawer (Info, Success, Warning, Error), unread badge counts, action links, and bell icon (`ZNotificationBell`). |
| **Industrial & SCADA** | **`ZAlarmSummary`** | ✅ | ✅ | ISA-18.2 compliant alarm management, state badges (Active, Acked, Cleared), acknowledgment actions, and audio snooze toggles. |
| **Documents & Content** | **`ZRichTextEditor`** | ✅ | ✅ | WYSIWYG formatting toolbar (Bold, Italic, Underline, Bullet lists, Colors, Font size), RTF/HTML round-trip persistence. |
| **Scheduling** | **`ZScheduler`** | ✅ | ✅ | Multi-view calendar control with Day, Week, Month, and Agenda view modes, event drag creation, and navigation headers. |
| **Data & Gauges** | **`ZGaugeChart`** | ✅ | ✅ | High-precision radial gauge with customizable threshold zones (Normal, Warning, Danger), animated needle, and live value HUD. |
| **Statistical Charts** | **`ZBubbleChart`** | ✅ | ✅ | Multi-dimensional bubble dispersion visualization $(X, Y, Size, Color)$, hover hit-testing, and dynamic legends. |
| **Security & Input** | **`ZPasswordBox`** | ✅ | ✅ | Masked secure input with show/hide eye toggle, real-time password strength meter (entropy bar), and focus indicators. |
| **Virtualization** | **`ZVirtualGrid`** | ✅ | ✅ | Lightweight, high-throughput virtualization grid designed for high-frequency telemetry feeds and minimal footprint. |
| **Automation** | **`ZCronEditor`** | ✅ | ✅ | Visual cron builder (Minute, Hour, Day, Month, Weekday), human-readable summary ("At 08:00 every Monday"), and validation. |
| **Advanced Pickers** | **`ZMultiSelect`** | ✅ | ✅ | Multi-selection tag box with removable chips, inline text search filter, maximum selection limits, and clear-all action. |
| **Signatures & Ink** | **`ZSignaturePad`** | ✅ | ✅ | Vector handwriting stroke capture, pressure smoothing, custom stroke width/color, clear action, and lossless PNG export. |
| **Data Transfer** | **`ZTransferList`** | ✅ | ✅ | Dual-list transfer control with directional buttons ($>, >>, <, <<$), search filters on both lists, and count badges. |
| **Layout & Feedback** | **`ZEmptyState`** | ✅ | ✅ | Polished zero-data illustration/glyph placeholder with title, description, and primary/secondary call-to-action buttons. |
| **User Identity** | **`ZAvatar`** | ✅ | ✅ | Circular/rounded user avatar with initials fallback, image caching, and presence status dots (Online, Busy, Away, Offline). |
| **Documentation** | **`ZMarkdownViewer`** | ✅ | ✅ | Vector Markdown document previewer with headings ($H_1-H_6$), code blocks, blockquotes, bullet points, and raw source toggle. |
| **Code & Scripting** | **`ZCodeEditor`** | ✅ | ✅ | Monospace script and formula editor with gutter line numbers, tab indentation, and theme integration. |
| **JSON Management** | **`ZJsonEditor`** | ✅ | ✅ | Zero-dependency JSON editor with pretty-formatting, minification, real-time syntax validator, and status indicators. |
| **Hierarchical Trees** | **`ZTreeView`** | ✅ | ✅ | Vector tree view with chevron expand/collapse, optional node checkboxes, selection highlights, and connector lines. |
| **Audit Trails** | **`ZTimeline`** | ✅ | ✅ | Vertical manufacturing audit trail and lot tracking journal with status indicator nodes (Completed, InProgress, Error, Pending). |

---

## 🛡️ Industrial Security & Identity Management Suite (v1.11.0)

A decoupled, enterprise-grade authentication, role-based authorization (RBAC), touch HMI, and compliance audit suite engineered specifically for 24/7 industrial runtime environments and multi-operator manufacturing stations:

| Category | Control | WinForms | WPF | Core Capabilities |
| :--- | :--- | :---: | :---: | :--- |
| **Authentication** | **`ZLoginDialog`** | ✅ | ✅ | Multi-modal login modal supporting Username/Password, quick PIN, and RFID/NFC hardware badge dongle. |
| **Touch HMI** | **`ZVirtualKeyboard`** | ✅ | ✅ | Touch-screen keyboard (QWERTY alphanumeric & 10-key numeric keypad modes) with 48px+ hit targets and auto-popup provider. |
| **Session Watchdog** | **`ZIdleTimeoutMonitor`** | ✅ | ✅ | Inactivity watchdog with 64-bit monotonic clock, idle countdown, warning, auto-lock overlay, and unlock authentication. |
| **Operator Status** | **`ZUserStatusBadge`** | ✅ | ✅ | Real-time session status badge showing operator name, role badge, session duration, lockout indicator, and quick logout menu. |
| **Hardware I/O** | **`ZBadgeReaderListener`** | ✅ | ✅ | Hardware RFID/NFC badge card scanner listener (USB HID keyboard-wedge / Serial) with preamble/postamble and debounce. |
| **RBAC Matrix** | **`ZRoleMatrix`** | ✅ | ✅ | Visual permission matrix mapping roles against permissions with tri-state checkboxes and asynchronous store sync. |
| **User Management** | **`ZUserManager`** | ✅ | ✅ | Industrial operator administration console for creating, editing, locking, assigning badges, and managing credentials. |
| **Declarative Security** | **`ZAuthorizeExtender` / `ZAuthorize`** | ✅ | ✅ | Extender provider (WinForms) & attached property (WPF) dynamically disabling or hiding UI elements based on active role/permission. |
| **Compliance & Audit** | **`ZOperationLogViewer`** | ✅ | ✅ | 21 CFR Part 11 compliant operator action and electronic signature audit viewer with severity badges, actor filter, and CSV export. |

---

## 📚 Technical Documentation & Guides

In-depth technical specifications and architectural documentation are modularized within the [`docs/`](docs/) directory:

| Document | Description |
| :--- | :--- |
| 📊 **[Verified Benchmarks](docs/BENCHMARKS.md)** | Frame budgets, 10M rows virtualization, GC allocations, and telemetry throughput. |
| 🎛️ **[Controls Catalog](docs/CONTROLS_CATALOG.md)** | Full reference for 80+ controls (GridControl, PivotGrid, SCADA, Charts, Media Viewers, and Extended Editors). |
| 🎬 **[Visual Controls Guide & Tour](docs/ZEROUI_CONTROLS_GUIDE.md)** | Architectural guide, in-process live video recordings, and feature tour across all subsystems. |
| 🎨 **[Theming & Styling](docs/THEMING_AND_STYLING.md)** | Obsidian Dark / Clean Light themes, High-DPI Per-Monitor V2, and single-HWND architecture. |
| 🏛️ **[System Architecture](docs/architecture/system-architecture.md)** | Multi-tier pipeline coordination, lock-free TripleBuffer, decoupled runtime, and renderers. |
| 📐 **[Control Ecosystem Audit](docs/architecture/control-ecosystem-audit-and-composite-architecture.md)** | Feature gap analysis, composite control standards, and cross-platform shared logic. |
| 🗺️ **[Development Roadmap](docs/roadmap.md)** | Release milestones, feature requests, and future capabilities. |

---

## 🏷️ Canonical `Z*` Control Naming Standard & Collision Prevention

To prevent ambiguous reference collisions (`CS0104`) with native WinForms and WPF controls (such as `System.Windows.Forms.Panel`, `GroupBox`, `Button`, `Label`, `TextBox`), ZeroUI establishes a clean architectural separation:

* **Foundational Engine & Infrastructure:** Prefixed with **`Zero`** (`ZeroTheme`, `ZeroDpi`, `ZeroFontCache`, `ZeroIcons`, `ZeroAnimationClock`, `ZeroLocalizer`).
* **Canonical UI Controls Suite:** Standard controls adopt the **`Z`** prefix (`ZPanel`, `ZGroupBox`, `ZMenuBar`, `ZButton`, `ZLabel`, `ZCheckBox`, `ZRadioButton`, `ZTextBox`, `ZProgressBar`, `ZTabControl`, `ZSplitContainer`, `ZGrid`, `ZChart`, `ZImageViewer`, `ZJsonEditor`, `ZTimeline`).
  * **Zero Collisions (`CS0104`):** Guarantees zero naming conflicts when developers import both `System.Windows.Forms` and ZeroUI namespaces.
  * **Instant IDE Discovery:** Simply typing `Z` in the IDE immediately surfaces the complete ZeroUI component palette.
  * **Full Theme Synchronization:** All `Z*` controls automatically inherit `ZeroTheme` dark/light modes, rounded corner styles, and High-DPI scaling.
  * **5-Release Deprecation Wrappers:** Previous class names (`PanelControl`, `GroupControl`, `MenuBarControl`, `SimpleButton`, `TextEdit`, and legacy `Zero*` shims) are retained as backward-compatibility wrappers with `[Obsolete]` warnings and will be maintained across 5 minor release cycles before pruning.

---

## 📦 Package Matrix

| Package | Targets | Primary Capabilities | Dependencies |
| :--- | :--- | :--- | :--- |
| **`ZeroUI.Core`** | `netstandard2.0`, `net462`, `net8.0` | High-frequency telemetry triple-buffer, TagEngine v2, PackML state machine, OEE metrics, validation engine, industrial state models, statistical chart engines, extended models (`AiMl`, `Gis`, `Compliance`, `Workflow`, `Media`, `Scheduling`) | **Zero 3rd-party dependencies** (Pure BCL) |
| **`ZeroUI.Historian.Sqlite`** | `netstandard2.0`, `net462`, `net8.0` | High-throughput SQLite WAL time-series telemetry storage engine (>100k records/s), rolling partitions, store & forward disk cache | `Microsoft.Data.Sqlite` |
| **`ZeroUI.WinForms`** | `net462`, `net8.0-windows` | 10M+ rows virtual grid (`GridControl`), interactive onboarding engine (`ZTour`), 40+ SCADA/HMI controls, 15+ specialized charts, Obsidian dark theme, DIBSection unmanaged double-buffering | `ZeroUI.Core` |
| **`ZeroUI.WinForms.Common`** | `net462`, `net8.0-windows` | Common navigational controls, overlays, dialogs, command palette (`ZCommandPalette`), notification center (`ZNotificationCenter`), and tree view (`ZTreeView`) | `ZeroUI.Core` |
| **`ZeroUI.WinForms.Editors`** | `net462`, `net8.0-windows` | Enterprise editors: `ZAutoComplete`, `ZPasswordBox`, `ZCronEditor`, `ZMultiSelect`, `ZSignaturePad`, `ZJsonEditor`, `ZRichTextEditor` | `ZeroUI.Core`, `ZeroUI.WinForms.Common` |
| **`ZeroUI.WinForms.Documents`**| `net462`, `net8.0-windows` | Document view and editing components: `ZMarkdownViewer`, `ZCodeEditor`, `ZRichTextViewer` | `ZeroUI.Core` |
| **`ZeroUI.WinForms.Industrial`**| `net462`, `net8.0-windows`| Specialized SCADA, BMS, Energy, Network, Water, GIS, and Workflow controls (`ZTimeline`, `ZAlarmSummary`, `ZScheduler`, `ZStepBar`) | `ZeroUI.Core` |
| **`ZeroUI.WinForms.Media`** | `net462`, `net8.0-windows` | Enterprise interactive image canvas and studio controls (`ZImageViewer`, `ZPictureEdit`) with sub-pixel pan/zoom, forensic pixel grid, MiniMap navigator, HUD color probe, Lightbox preview | `ZeroUI.Core`, `ZeroGraphics` |
| **`ZeroUI.Wpf`** | `net462`, `net8.0-windows` | Zero-alloc WPF virtual grid (`GridControl`), interactive onboarding engine (`ZTour`), industrial styling, 15+ specialized charts, and D3D11 shared texture bridge | `ZeroUI.Core` |
| **`ZeroUI.Wpf.Common`** | `net462`, `net8.0-windows` | WPF navigation, overlays, command palette (`ZCommandPalette`), notification center (`ZNotificationCenter`), and tree view (`ZTreeView`) | `ZeroUI.Core` |
| **`ZeroUI.Wpf.Editors`** | `net462`, `net8.0-windows` | WPF enterprise editors: `ZAutoComplete`, `ZPasswordBox`, `ZCronEditor`, `ZMultiSelect`, `ZSignaturePad`, `ZJsonEditor` | `ZeroUI.Core`, `ZeroUI.Wpf.Common` |
| **`ZeroUI.Wpf.Documents`** | `net462`, `net8.0-windows` | WPF document components: `ZMarkdownViewer`, `ZCodeEditor`, `ZRichTextViewer` | `ZeroUI.Core` |
| **`ZeroUI.Wpf.Industrial`** | `net462`, `net8.0-windows` | WPF SCADA, BMS, Energy, Network, Water, GIS, and Workflow controls (`ZTimeline`, `ZAlarmSummary`, `ZScheduler`, `ZStepBar`) | `ZeroUI.Core` |
| **`ZeroUI.Wpf.Media`** | `net462`, `net8.0-windows` | High-performance interactive imaging & forensic inspection suite (`ZImageViewer`, `ZPictureEdit`, `ZAnnotationCanvas`, `ZMeasurementRuler`, `ZWatermarkOverlay`, `ZVideoPlayer`, `ZAudioWaveform`, `ZDocumentDeskew`, `ZCompareViewer`, `ZCropBox`, `ZHistogramScope`, `ZCurveEditor`, `ZColorWheel`, `ZThumbnailGrid`, `ZDominantPalette`, `ZHistoryTimeline`, `ZFilmstripScroller`, `ZMaskGizmoOverlay`, `ZBatchTaskQueue`, `ZTokenPatternEditor`, `ZExifTelemetryCard`, `ZMiniMapNavigator`) | `ZeroUI.Core`, `ZeroGraphics` |

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
# Run comprehensive automated test suites (804 tests, 100% pass across Core & Desktop UI)
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
| **`v1.10.1`** | 2026-09-25 | **Enterprise Extended Controls Suite & Deep Multi-Dimensional Verification**:<br/>• **21 Production Extended Controls**: Implemented full lifecycle, zero-allocation rendering, and cross-platform parity across WinForms and WPF (`ZAutoComplete`, `ZCommandPalette`, `ZNotificationCenter`, `ZAlarmSummary`, `ZRichTextEditor`, `ZScheduler`, `ZGaugeChart`, `ZBubbleChart`, `ZPasswordBox`, `ZVirtualGrid`, `ZCronEditor`, `ZMultiSelect`, `ZSignaturePad`, `ZTransferList`, `ZEmptyState`, `ZAvatar`, `ZMarkdownViewer`, `ZCodeEditor`, `ZJsonEditor`, `ZTreeView`, `ZTimeline`).<br/>• **Theme Synchronization & Thread Safety**: Zero-allocation token lookup (`ZeroTheme.Colors`, `ZeroWpfTheme`), cross-thread Dispatcher STA safety checks preventing xUnit race conditions.<br/>• **Zero-Dependency JSON Token Engine**: Integrated high-speed parser and validator in `ZJsonEditor` with zero external dependencies, compatible across .NET Framework 4.6.2 and .NET 8.<br/>• **Comprehensive Test Coverage**: Expanded automated unit test suite to **804 tests passed (100% green, 0 failures, 0 skipped)**.<br/>• Full 5-release backward compatibility deprecation policy implemented across all legacy shims. |
| **`v1.10.0`** | 2026-09-25 | **Interactive Guided Tour Engine & Canonical Z* Naming Standardization**:<br/>• Cross-Platform Interactive Onboarding Engine: Native `ZTour` for WinForms and WPF with spotlight cutout aperture masking (`Region.Exclude` / `CombinedGeometry.Exclude`), semi-transparent glass dimming backdrop, and collision-free adaptive popover positioning (`TopMost = true`).<br/>• Specialized Creative & Forensic Inspection Media Suite (`ZeroUI.Wpf.Media`): `ZAnnotationCanvas`, `ZMeasurementRuler`, `ZWatermarkOverlay`, `ZVideoPlayer`, `ZAudioWaveform`, `ZDocumentDeskew`.<br/>• Standardized canonical `Z*` naming prefix across all catalog documentation and showcase controls with 100% backward compatibility via `[Obsolete]` aliases.<br/>• Published lightweight single-file demo executables (.NET 8 framework-dependent): `WinformDemo` (~4.5 MB) and `WpfDemo` (~3.9 MB).<br/>• 720 automated tests passed (100%). |
| **`v1.9.0`** | 2026-09-23 | **Advanced Statistical, Relational & Hierarchical Charts Suite**:<br/>• Clean Architecture decoupling: 100% calculation engines in `ZeroUI.Core.Analytics` with thin WinForms GDI+ and WPF `DrawingContext`/`StreamGeometry` rendering.<br/>• 8 New High-Performance Analytics Controls: `HistogramChart`, `ScatterChart`, `SunburstChart`, `LollipopChart`, `TreemapChart`, `SankeyChart`, `BulletChart`, `ParetoChart`.<br/>• Standardized control identifiers with zero "Zero" prefix across Core, WinForms, and WPF.<br/>• Interactive showcase sub-tabs integrated into `WinformDemo` and `WpfDemo`.<br/>• Interactive launcher scripts added (`run-demo.bat`, `scripts/run-demo.ps1`).<br/>• 655 automated tests passed (100%). |
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
