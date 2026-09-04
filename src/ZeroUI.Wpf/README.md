# ZeroUI.Wpf ⚡

Ultra-high-performance WPF industrial UI suite with zero-allocation virtual big data grid, SCADA gauges, and modern reactive theme engine (`net462`, `net8.0-windows`).

[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/kzxl/ZeroUI)
[![GitHub](https://img.shields.io/badge/GitHub-kzxl%2FZeroUI-blue.svg)](https://github.com/kzxl/ZeroUI)

---

## 🌟 Key Features

* **WPF Virtual Data Grid (`ZeroGridControl`):**
  * Hardware-accelerated virtual row virtualization handling millions of items with smooth 60 FPS scrolling.
  * Standardized pagination bar (`ZeroGridPagination`) and debounced live search bar (`ZeroGridSearchBar`).
* **SCADA & Industrial Gauges (`ZeroUI.Wpf.Industrial`):**
  * **`ZeroGauge`:** Circular dial gauge with high-contrast indicator needle and threshold warning zones.
  * **`ZeroLinearGauge`:** Vertical/Horizontal bar gauge for level and pressure monitoring.
  * **`ZeroSevenSegment` & `ZeroLedTower`:** Industrial 7-segment digital display and Andon signal tower.
  * **`ZeroHeatmap`:** 2D industrial matrix heatmap for load balancing and thermal mapping.
  * **`ZeroStatusBadge`:** Animated machine state badge.
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
