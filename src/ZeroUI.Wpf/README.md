# ZeroUI.Wpf ⚡

Ultra-high-performance WPF industrial UI suite with zero-allocation virtual big data grid, OLAP pivot matrices, timeline range selectors, form validation, and reactive Obsidian Dark/Clean Light theme engine (`net462`, `net8.0-windows`).

[![NuGet Version](https://img.shields.io/badge/nuget-v1.2.0-blue.svg)](https://github.com/kzxl/ZeroUI)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/kzxl/ZeroUI)
[![GitHub](https://img.shields.io/badge/GitHub-kzxl%2FZeroUI-blue.svg)](https://github.com/kzxl/ZeroUI)

---

## 🌟 Key Features

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
* **`ZeroPrintPreview`:** Vector document print previewer with high-DPI paper canvas and zoom.

### 🏭 Industrial Gauges & Theme Engine
* **SCADA Gauges:** Circular `ZeroGauge`, `ZeroLinearGauge`, `ZeroHeatmap`, `ZeroLedTower`, `ZeroSignalScope`.
* **Unified Theme Engine:** Obsidian Dark and Clean Light styling with dynamic resource dictionary swapping.

---

## 📦 Installation

```powershell
dotnet add package ZeroUI.Wpf --version 1.2.0
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
