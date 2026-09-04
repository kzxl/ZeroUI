# ZeroUI.Wpf ⚡

Ultra-high-performance WPF industrial UI suite with zero-allocation virtual big data grid, SCADA gauges, and modern reactive theme engine (`net462`, `net8.0-windows`).

[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/kzxl/ZeroUI)
[![GitHub](https://img.shields.io/badge/GitHub-kzxl%2FZeroUI-blue.svg)](https://github.com/kzxl/ZeroUI)

---

## 🌟 Key Features

* **WPF Virtual Data Grid & Query (`ZeroUI.Wpf.DataGrid`):**
  * Hardware-accelerated virtual row virtualization handling millions of items with smooth 60 FPS scrolling (`ZeroGridControl`).
  * **`ZeroFilterControl`**: Visual Query Builder rendering hierarchical boolean condition trees with SQL WHERE clause generation.
  * Standardized pagination bar (`ZeroGridPagination`) and debounced live search bar (`ZeroGridSearchBar`).
* **Enterprise Editors (`ZeroUI.Wpf.Editors`):**
  * **`ZeroGridLookup`**: Multi-column dropdown editor with embedded virtual DataGrid and cross-column search.
  * **`ZeroCheckedComboBox`**: Multi-select dropdown with checkboxes, "(Select All)", and search filter.
  * **`ZeroTokenEdit`**: Tag & badge input with dismissible chips and keyboard navigation.
  * **`ZeroColorPicker`**: Enterprise swatch matrix and HEX editor.
* **Navigation & Workflows (`ZeroUI.Wpf.Navigation`):**
  * **`ZeroWizard`**: Multi-step process workflow wizard with validation and step progress indicator.
  * **`ZeroSideNav`**: Collapsible vertical sidebar navigation with category groups and badges.
  * **`ZeroAccordion`**: Collapsible navigation accordion with animated groups.
* **Industrial, Charts & Analytics (`ZeroUI.Wpf.Charts`, `Industrial`):**
  * **`ZeroBoxPlotChart`**: Statistical SPC Box-and-Whisker quality inspection chart with USL/LSL limits.
  * **`ZeroGanttChart`**: Production scheduling timeline with task hierarchies and progress bars.
  * **`ZeroPropertyGrid`**: High-speed categorized reflection property inspector.
  * **`ZeroChart` & Gauges**: Circular `ZeroGauge`, `ZeroLinearGauge`, `ZeroHeatmap`, `ZeroLedTower`, `ZeroSignalScope`.
* **Overlays, Feedback & Reporting (`ZeroUI.Wpf.Overlays`, `Feedback`, `Reporting`):**
  * **`ZeroPrintPreview`**: Vector document print previewer with high-DPI paper canvas and zoom.
  * **`ZeroSkeleton`**: Hardware-accelerated 60 FPS shimmer loading placeholder.
  * **`ZeroToast` & `ZeroModal`**: Non-intrusive floating toast notifications and backdrop-dimmed dialogs.
* **Modern Design System & Theme Engine:**
  * Obsidian Dark and Clean Light styling with dynamic resource dictionary swapping.
  * Fluent typography and high-DPI scaling support.

---

## 📦 Installation

```powershell
dotnet add package ZeroUI.Wpf
```

---

## 🚀 Quick Example (XAML)

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:grid="clr-namespace:ZeroUI.Wpf.DataGrid;assembly=ZeroUI.Wpf"
        xmlns:scada="clr-namespace:ZeroUI.Wpf.Industrial;assembly=ZeroUI.Wpf"
        Title="ZeroUI WPF Showcase" Height="600" Width="900">
    <Grid>
        <grid:ZeroGridControl x:Name="virtualGrid" />
    </Grid>
</Window>
```

Full documentation and source code: [github.com/kzxl/ZeroUI](https://github.com/kzxl/ZeroUI)
