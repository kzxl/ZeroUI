# ZeroUI ⚡

> **High-Performance, Zero-Allocation UI Engine & Control Suite for .NET WinForms and WPF**

[![Target Frameworks](https://img.shields.io/badge/targets-netstandard2.0%20%7C%20net462%20%7C%20net8.0--windows-blue.svg)](#architecture)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](#license)
[![Performance](https://img.shields.io/badge/FPS-60--120%20Hz-orange.svg)](#benchmarks)

---

## 1. Vision & Core Philosophy

Standard Windows desktop control suites (such as built-in `DataGridView` or legacy commercial vendor suites) suffer from critical performance bottlenecks:
- **WinForms:** Single-threaded GDI/GDI+ CPU rasterization, excessive Win32 Handle (`HWND`) allocations (triggering the 10,000 OS handle limit), and tearing/flickering.
- **WPF:** Visual Tree bloat (thousands of `DependencyObject` and `UIElement` instances per page), memory pressure from boxing/bindings, and CPU dispatcher saturation during fast scrolls.

**ZeroUI** solves these fundamental issues from the ground up:
* **Zero Allocation (`Zero-Alloc`):** Hot execution loops generate 0 bytes of Garbage Collection (`GC Gen 0/1/2`) heap allocations using `Span<T>`, `ReadOnlySpan<T>`, `ArrayPool<T>`, and unmanaged back-buffers.
* **Decoupled Thread Architecture:** UI thread handles only user input and frame presentation. Sorting, filtering, spatial layouts, and dirty-rect calculations run in background threads.
* **Dual Rendering Pipeline:**
  * **GPU Mode:** Hardware-accelerated Direct2D / DirectX 11 via low-overhead interop and D3D11-to-D3D9Ex shared handle bridge.
  * **Fast GDI Mode:** High-quality ClearType text rasterization via Win32 Memory DC & DIB sections with zero-copy `BitBlt` (100% resilient on Remote Desktop / Citrix / low-spec virtual machines).
* **Single HWND & High-DPI Resilient:** 1 top-level HWND per composite view eliminating Win32 handle bloat, with automatic `PerMonitorV2` High-DPI coordinate scaling.
* **Enterprise Dual-Runtime Support:** Seamlessly runs on legacy **.NET Framework 4.6.2** enterprise stacks as well as modern **.NET 8.0 / 9.0**.


---

## 2. Solution Structure

```text
E:\15. Other\dotnet\libs\ZeroUI\
├── .project-rule.md                 # AgentOption project metadata & rules
├── README.md                        # Project entry point & overview
├── docs/
│   ├── architecture/
│   │   ├── system-architecture.md   # 3-tier decoupled architecture
│   │   ├── rendering-pipeline.md    # GPU Direct2D & Unmanaged GDI pipelines
│   │   └── virtualization-engine.md # Viewport culling & O(log N) layout indexing
│   ├── standards/
│   │   ├── high-perf-guidelines.md  # SIMD, Memory pooling, and C# guidelines
│   │   └── threading-model.md       # STA marshaling & thread decoupling
│   └── roadmap.md                   # Phased delivery plan & milestones
└── src/ (Planned)
    ├── ZeroUI.Core/                 # .NET Standard 2.0 (Platform-agnostic engine)
    ├── ZeroUI.WinForms/             # net462; net8.0-windows (Single-HWND controls)
    ├── ZeroUI.Wpf/                  # net462; net8.0-windows (DrawingVisual & D3DImage)
    ├── ZeroUI.Benchmarks/           # BenchmarkDotNet performance test suite
    └── ZeroUI.Samples.Demo/         # Million-row enterprise stress test showcase
```

---

## 3. Target Specifications & Benchmarks

| Metric | Legacy WinForms / Standard WPF | ZeroUI Target |
| :--- | :---: | :---: |
| **Max Virtual Rows** | 50,000 (Noticeable stutter) | **1,000,000+** |
| **Scroll Framerate** | 12 – 25 FPS | **60 – 120 FPS (V-Sync capped)** |
| **Frame Render Latency** | 35ms – 80ms | **< 2.0ms** |
| **UI Thread CPU Usage** | 80% – 100% (Freezes) | **< 3%** |
| **RAM Footprint (1M Rows)** | 450 MB – 1.2 GB | **< 45 MB** (Flyweight & Paged) |
| **GC Gen 0 Collections / sec** | 150 – 400 | **0** during continuous scroll |

---

## 4. Documentation Index

- [Architecture Overview](file:///E:/15.%20Other/dotnet/libs/ZeroUI/docs/architecture/system-architecture.md) — System boundaries, components, and communication protocols.
- [Rendering Pipeline](file:///E:/15.%20Other/dotnet/libs/ZeroUI/docs/architecture/rendering-pipeline.md) — Direct2D GPU vs. Fast Unmanaged GDI engine.
- [Virtualization Engine](file:///E:/15.%20Other/dotnet/libs/ZeroUI/docs/architecture/virtualization-engine.md) — Viewport culling, prefix sums, and spatial queries.
- [High-Performance C# Standards](file:///E:/15.%20Other/dotnet/libs/ZeroUI/docs/standards/high-perf-guidelines.md) — Rules for zero-allocation memory management.
- [Threading & Concurrency Model](file:///E:/15.%20Other/dotnet/libs/ZeroUI/docs/standards/threading-model.md) — STA isolation and worker synchronization.
- [Implementation Roadmap](file:///E:/15.%20Other/dotnet/libs/ZeroUI/docs/roadmap.md) — Phased milestones from Core MVP to Production.

---

## 5. License
MIT License. Free for commercial enterprise and community use.
