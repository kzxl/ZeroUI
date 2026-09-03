# ZeroUI Implementation Roadmap

## 1. Release Timeline & Milestones

| Phase | Milestone Name | Primary Focus | Key Deliverables |
| :---: | :--- | :--- | :--- |
| **Phase 1** | **Core Virtualization Engine** | `ZeroUI.Core` (.NET Standard 2.0) | `PrefixSumArray`, `VirtualViewport2D`, `ZeroBufferPool`, `Channel` data queues. |
| **Phase 2** | **WinForms MVP (`ZeroGrid`)** | `ZeroUI.WinForms` (net462, net8.0) | Single-HWND Control, Unmanaged Fast GDI blit, 32-bit `SetScrollInfo`, 1M-row benchmark. |
| **Phase 3** | **WPF Port (`ZeroGrid`)** | `ZeroUI.Wpf` (net462, net8.0) | `DrawingVisual` host, XAML attached properties, `D3DImage` DirectX 11 pipeline. |
| **Phase 4** | **Enterprise Grid Features** | Interactivity & Production Readiness | Floating In-place Editor, Frozen Columns/Rows, Multi-column sorting, Fast filtering. |
| **Phase 5** | **Expanded Control Suite** | Additional High-Perf Controls | `ZeroTree` (Virtualized TreeList), `ZeroPlot` (Realtime Chart), `ZeroLog` (Log viewer). |
| **Phase 6** | **Theming & Packaging** | Distribution & Design System | Dark/Light Themes (ERP/Industrial density), NuGet packages, CI/CD automated benchmarks. |

---

## 2. Detailed Phase Breakdown

### Phase 1: Core Virtualization Engine (Weeks 1–2)
* [ ] Implement `ZeroUI.Core.Memory.ZeroMemory` cross-target runtime compatibility wrapper (`NativeMemory` vs `Marshal`).
* [ ] Implement `ZeroUI.Core.Virtualization.VirtualViewport2D` with physical vs logical pixel conversion.
* [ ] Implement `ZeroUI.Core.Layout.PrefixSumArray` and Sparse Dynamic Height model with binary search tests.
* [ ] Implement `ZeroUI.Core.Data.RowIndexMap` for zero-allocation sorting and filtering redirection.
* [ ] Define `IZeroVirtualSource` and `IZeroPagedSource` contracts with `CellValueBuffer`.
* [ ] Implement `ZeroUI.Core.Memory.ZeroBufferPool` wrapping `ArrayPool<T>` and unmanaged memory.
* [ ] Create BenchmarkDotNet suite testing memory allocations (Target: 0 bytes per viewport calculation).

### Phase 2: WinForms MVP (`ZeroGrid`) (Weeks 3–4)
* [ ] Create custom control `ZeroGridControl` inheriting from `Control` (Single-HWND architecture).
* [ ] Implement Win32 P/Invoke layer (`CreateDIBSection`, `BitBlt`, `ExtTextOutW`, `SetScrollInfo`).
* [ ] Implement High-DPI (`PerMonitorV2`) scaling handler (`WM_DPICHANGED`).
* [ ] Implement `SpatialHitTester` for mouse clicks, column boundary resizing (`VSplit` cursor), and row selection.
* [ ] Implement Double-Buffered Memory DC & DIB Section text rendering pipeline.
* [ ] Verify smooth scrolling on a mock 1,000,000-row x 50-column in-memory dataset.

### Phase 3: WPF Port (`ZeroGrid`) (Weeks 5–6)
* [ ] Create `ZeroGridElement` inheriting from `FrameworkElement`.
* [ ] Implement WPF `IScrollInfo` interface for seamless native `<ScrollViewer>` integration.
* [ ] Implement `DrawingVisual` render pipeline with frozen brushes and pens.
* [ ] Implement direct DirectX 11 to Direct3D 9Ex shared texture bridge via `D3DImage` for GPU rendering.
* [ ] Verify zero visual tree overhead and smooth 60 FPS scrolling.

### Phase 4: Enterprise Grid Features (Weeks 7–8)
* [ ] Implement floating flyweight `InPlaceEditCoordinator` (TextBox / ComboBox / DatePicker) with Tab/Enter navigation.
* [ ] Implement frozen columns (left-pinned and right-pinned columns).
* [ ] Implement in-memory multi-column quick-sort via `Span<int>.Sort()` and fast column filtering.
* [ ] Implement Buffer Resize Handshake protocol and Direct2D GPU Device-Loss recovery (`D2DERR_RECREATE_TARGET`).
* [ ] Implement column auto-resizing, clipboard TSV export (Ctrl+C), and drag-and-drop column reordering.

### Phase 5: Expanded Controls Suite (Weeks 9–10)
* [ ] Develop `ZeroTree`: Hierarchical tree with index-based node collapse/expansion.
* [ ] Develop `ZeroPlot`: Real-time signal graph with automated LTTB downsampling.
* [ ] Develop `ZeroLog`: Memory-mapped log viewer with instant regex search.

### Phase 6: Theming, Testing & Distribution (Weeks 11–12)
* [ ] Build Dark Theme (Industrial/ERP Charcoal `#1e1e1e`) and Modern Windows 11 Fluent Theme.
* [ ] Add automated UI stress testing across .NET 4.6.2 and .NET 8.0.
* [ ] Author XML documentation and publish NuGet packages (`ZeroUI.Core`, `ZeroUI.WinForms`, `ZeroUI.Wpf`).

