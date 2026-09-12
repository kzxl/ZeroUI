# ZeroUI ⚡

> **Ultra-High-Performance, Zero-Allocation Industrial UI & Runtime Ecosystem for .NET (WinForms, WPF, .NET 8/9 & Edge)**

[![ZeroPlatform Ecosystem](https://img.shields.io/badge/ZeroPlatform-Ecosystem-blueviolet.svg)](https://github.com/kzxl/ZeroPlatform)
[![NuGet Version](https://img.shields.io/badge/nuget-v1.5.0-blue.svg)](https://github.com/kzxl/ZeroUI)
[![GPU Acceleration](https://img.shields.io/badge/GPU%20Acceleration-Direct3D%2011%20%7C%20Direct2D-cyan.svg)](https://github.com/kzxl/ZeroGraphics)
[![Unit Tests](https://img.shields.io/badge/tests-438%20passed%20(100%25)-brightgreen.svg)](#-testing--quality-assurance)
[![Target Frameworks](https://img.shields.io/badge/targets-netstandard2.0%20%7C%20net462%20%7C%20net8.0--windows-blue.svg)](#-package-matrix)
[![UI Frame Latency](https://img.shields.io/badge/Frame%20Latency-%3C%204ms%20P95-brightgreen.svg)](docs/BENCHMARKS.md)
[![GC Allocations](https://img.shields.io/badge/Hot%20Path%20Allocations-0%20B%20(Zero--Alloc)-brightgreen.svg)](docs/BENCHMARKS.md)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](#-license)

---

## 📸 Visual Showcase

| ⚡ 10,000,000 Rows Virtual Grid | 🏭 SCADA Closed-Loop Workcell |
| :---: | :---: |
| ![ZeroGrid Benchmark](docs/images/01_zerogrid_benchmark.png) | ![SCADA Closed Loop](docs/images/12_scada_closed_loop_simulation.png) |

| 🌑 Obsidian Dark Theme (`#12151C`) | 📊 BI & Analytics Dashboard |
| :---: | :---: |
| ![Obsidian Dark](docs/images/08_dark_theme_scada.png) | ![Charts Dashboard](docs/images/10_business_charts_dashboard.png) |

---

## 📖 Executive Summary

**ZeroUI** is an enterprise-grade industrial automation UI suite and runtime engine. Engineered to overcome the severe handle leaks, garbage collection pauses, and screen flicker common in standard desktop controls, ZeroUI renders millions of rows and real-time telemetry at 60 FPS with zero managed memory allocations.

### Core Architectural Pillars
* **Strictly Zero Allocation (`Zero-Alloc`)**: Hot render loops generate **0 bytes** of GC churn using `Span<T>`, `ArrayPool<T>`, and unmanaged DIBSection memory buffers.
* **Single-HWND Architecture**: Composite controls maintain **1 Win32 window handle**, completely eliminating OS handle exhaustion crashes (10,000 handle limit) and window flicker.
* **Win32 Memory DC DIBSection Engine**: Offscreen unmanaged double buffering with zero-copy `BitBlt` presentation (100% resilient across RDP and virtual machines).
* **Enterprise Dual-Runtime Support**: Full native compatibility with **.NET Framework 4.6.2** as well as modern **.NET 8.0 / 9.0**.
* **Unified Theme Engine**: Instant reactive switching between **Obsidian Dark Mode** (`#12151C`) and **Clean Light Mode** across all controls.
* **Centralized 60 FPS Clock (`ZeroAnimationClock`)**: Single global ticker with synchronized ISA-18.2 blinking phases, eliminating timer scatter.

---

## 📚 Technical Documentation & Guides

In-depth technical specifications and architectural documentation are modularized within the [`docs/`](docs/) directory:

| Document | Description |
| :--- | :--- |
| 📊 **[Verified Benchmarks](docs/BENCHMARKS.md)** | Frame budgets, 10M rows virtualization, GC allocations, and telemetry throughput. |
| 🎛️ **[Controls Catalog](docs/CONTROLS_CATALOG.md)** | Full reference for 40+ controls (ZeroGrid, PivotGrid, Gauges, Pumps, Valves, Tanks 3D). |
| 🎨 **[Theming & Styling](docs/THEMING_AND_STYLING.md)** | Obsidian Dark / Clean Light themes, High-DPI Per-Monitor V2, and single-HWND architecture. |
| 🏛️ **[Threading Model](docs/standards/threading-model.md)** | Multi-tier pipeline coordination, lock-free TripleBuffer, and UiDispatcher. |
| 🗺️ **[Development Roadmap](docs/roadmap.md)** | Release milestones, feature requests, and future capabilities. |

---

## 📦 Package Matrix

| Package | Targets | Primary Capabilities |
| :--- | :--- | :--- |
| **`ZeroUI.Core`** | `netstandard2.0`, `net462`, `net8.0` | High-frequency telemetry triple-buffer, TagEngine v2, PackML state machine, OEE metrics |
| **`ZeroUI.WinForms`** | `net462`, `net8.0-windows` | 10M+ rows virtual grid, 40+ SCADA/HMI controls, Obsidian dark theme, DIBSection engine |
| **`ZeroUI.Wpf`** | `net462`, `net8.0-windows` | Zero-allocation WPF virtual big data grid and reactive industrial theme styling |

---

## ⚡ Quick Start: 1,000,000 Rows Virtual Grid (WinForms)

```csharp
using System.Windows.Forms;
using ZeroUI.WinForms.DataGrid;

var grid = new ZeroGridControl
{
    Dock = DockStyle.Fill,
    RowDensity = ZeroGridRowDensity.Normal,
    AllowUserSorting = true
};
this.Controls.Add(grid);

// Define strongly-typed columns
grid.Columns.Add(new ZeroGridColumn("Id", "ID", 100));
grid.Columns.Add(new ZeroGridColumn("Timestamp", "Timestamp", 180));
grid.Columns.Add(new ZeroGridColumn("Temperature", "Temp (°C)", 140));

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
# Run comprehensive automated test suite (438 tests, 100% pass)
dotnet test tests/ZeroUI.Core.Tests/ZeroUI.Core.Tests.csproj

# Launch interactive demonstration suite
dotnet run --project src/ZeroUI.Samples.BenchmarkDemo/ZeroUI.Samples.BenchmarkDemo.csproj -f net8.0-windows
```

---

## 🌐 Part of the ZeroPlatform Ecosystem

ZeroUI is the user interface and SCADA visualization pillar of the **[ZeroPlatform](https://github.com/kzxl/ZeroPlatform)** suite — unifying 12 sovereign subsystems including `ZeroGraphics`, `ZeroPipeline`, `ZeroTensor`, `ZeroComm`, and `ZeroStorage`.

---

## 📄 License & Author

Released under the permissive **MIT License**.  
Architected and developed by **Phong Võ**.
