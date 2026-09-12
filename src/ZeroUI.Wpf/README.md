# ZeroUI.Wpf ⚡

Ultra-high-performance WPF industrial UI suite with zero-allocation virtual big data grid, OLAP pivot matrices, timeline range selectors, form validation, and reactive Obsidian Dark/Clean Light theme engine (`net462`, `net8.0-windows`).

> [!NOTE]
> **Active Development Notice:** This project is currently in active development. Feedback, suggestions, and contributions from the community are warmly welcome!

[![ZeroPlatform Ecosystem](https://img.shields.io/badge/ZeroPlatform-Ecosystem-blueviolet.svg)](https://github.com/kzxl/ZeroPlatform)
[![NuGet Version](https://img.shields.io/badge/nuget-v1.5.0-blue.svg)](https://github.com/kzxl/ZeroUI)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/kzxl/ZeroUI)
[![GitHub](https://img.shields.io/badge/GitHub-kzxl%2FZeroUI-blue.svg)](https://github.com/kzxl/ZeroUI)

---

## 🌟 Key Features

### ⚡ Direct3D 11 GPU Bridge & Automatic Render Optimizer
* **Native D3D11 Shared Texture Bridge (`ZeroD3D11Bridge`, `ZeroD3DCanvas`):** Direct DXGI zero-copy texture sharing with WPF `D3DImage`, independent GPU rendering loop (60/120/144 FPS), and interactive cursor wave modulation.
* **Universal Attached Properties (`opt:RenderOptimizer.*`):** Attach live GPU SDF box shadows, blurs, and neon bloom effects to ANY standard WPF control (`Button`, `TextBox`, `Border`) via `opt:RenderOptimizer.Elevation`, `BlurRadius`, `GlowIntensity`, `GlowColor`, `RoutingMode`.
* **Smart Card Container (`ZeroOptimizedCard`):** Automated cost-based rendering routing (CPU ClearType foreground + GPU SDF shadow backdrop) with dynamic adaptive throttling (recovers 60 FPS under heavy load).
* **Real-Time Diagnostic HUD (`ZeroRenderOptimizerHUD`):** Glassmorphic heads-up display overlay showing live FPS, 16.6ms budget compliance, GPU hardware tier, dedicated VRAM, and 9-slice atlas hit rates.

### 🚀 WPF Virtual Data Grid & OLAP Matrix
* **WPF Virtual Data Grid (`ZeroUI.Wpf.DataGrid`):**
  * Hardware-accelerated row virtualization handling millions of items with smooth 60 FPS scrolling (`ZeroGridControl`).
  * In-place column sorting, live filtering, column pinning, cell formatting, and selection models (Cell, Row, MultiRow, Block).
  * Standardized pagination toolbar (`ZeroGridPagination`) and debounced search bar (`ZeroGridSearchBar`).
* **`ZeroPivotGrid` / `PivotGridControl`:** Cross-tab OLAP aggregation matrix with collapsible tree headers (`▶`/`▼`), multi-dimensional rollups (`Sum`, `Count`, `Average`, `Min`, `Max`), sub-totals, and grand totals.
* **`ZeroFilterControl`:** Visual Query Builder rendering hierarchical boolean condition trees with SQL WHERE clause generation.

### ⏱️ Visual Timeline & Range Selector
* **`RangeControl` / `DateTimeRangeSlider`:** Dual-thumb interactive range selector with sparkline track, continuous/discrete selection (numeric and DateTime), and draggable selection window.

### 🛡️ Form Validation & Editors Suite
* **`ValidationProvider` & `ZeroErrorProvider`:** Declarative XAML and code-behind form validation engine with animated pulsing error badges, warning glyphs, and automated binding.
* **`ZeroGridLookup`:** Multi-column dropdown editor with embedded virtual DataGrid and cross-column search.
* **`ZeroCheckedComboBox`:** Multi-select dropdown with checkboxes, "(Select All)", and search filter.
* **`ZeroTokenEdit`:** Tag & badge input with dismissible chips and keyboard navigation.
* **`ZeroColorPicker` & `ZeroDateRangePicker`:** Enterprise swatch color picker and dual-date range presets.

### 📐 Navigation, Layout & Reporting
* **`ZeroWizard`:** Multi-step process workflow wizard with validation and step progress indicator.
* **`ZeroSideNav` & `ZeroAccordion`:** Collapsible vertical sidebar navigation with category groups and badges.
* **`ZeroDockManager`:** Multi-region docking layout system with detachable floating windows and auto-hide tabs.
* **`SpreadsheetControl` & Reporting:** Vector spreadsheet with live formula evaluation (`=SUM`, `AVERAGE`, `MIN`, `MAX`, `IF`), interactive formula bar, in-place cell editing, and PDF/Document previewers (`PdfViewerControl`, `DocumentPreviewControl`).

### 🏭 Industrial Gauges & Theme Engine
* **SCADA Gauges:** Circular `ZeroGauge`, `ZeroLinearGauge`, `ZeroHeatmap`, `ZeroLedTower`, `ZeroSignalScope`.
* **Unified Theme Engine:** Obsidian Dark and Clean Light styling with dynamic resource dictionary swapping.

---

## 📦 Installation

```powershell
dotnet add package ZeroUI.Wpf --version 1.5.0
```

---

## 🚀 Quick Example (XAML)

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:grid="clr-namespace:ZeroUI.Wpf.DataGrid;assembly=ZeroUI.Wpf"
        xmlns:pivot="clr-namespace:ZeroUI.Wpf.PivotGrid;assembly=ZeroUI.Wpf"
        xmlns:range="clr-namespace:ZeroUI.Wpf.Range;assembly=ZeroUI.Wpf"
        Title="ZeroUI WPF Showcase" Height="700" Width="1000">
    <Grid>
        <grid:ZeroGridControl x:Name="virtualGrid" />
    </Grid>
</Window>
```

Full documentation and source code: [github.com/kzxl/ZeroUI](https://github.com/kzxl/ZeroUI)
