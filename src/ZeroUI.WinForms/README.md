# ZeroUI.WinForms ⚡

Ultra-high-performance WinForms enterprise & industrial UI suite with 10M+ rows virtual grid, cross-tab OLAP pivot matrices, visual timeline range selectors, form validation engines, and 60 FPS SCADA mimics (`net462`, `net8.0-windows`).

[![NuGet Version](https://img.shields.io/badge/nuget-v1.2.0-blue.svg)](https://github.com/kzxl/ZeroUI)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/kzxl/ZeroUI)
[![GitHub](https://img.shields.io/badge/GitHub-kzxl%2FZeroUI-blue.svg)](https://github.com/kzxl/ZeroUI)

---

## 🌟 Key Features

### 🚀 ZeroGrid Big Data Virtual Engine & OLAP Matrix
* **ZeroGrid Virtual DataGrid (`ZeroGridControl`):**
  * Effortlessly renders **10,000,000+ virtual rows** with zero GC allocations on hot scrolling loops.
  * Single-HWND architecture with Win32 Memory DC DIBSection double-buffering (eliminates Win32 handle leaks and GDI object churn).
  * High-speed pointer swap sorting, debounced search filtering (150ms), density switching (`Compact`, `Normal`, `Comfortable`), and streaming CSV export (>1,100,000 rows/s).
  * Multi-level row grouping with expand/collapse states (`GroupedRowIndexMap`), summaries, and bands.
* **OLAP Multidimensional Analysis Matrix (`PivotGridControl` / `ZeroPivotGrid`):**
  * Cross-tab dynamic aggregation matrix grouping arbitrary datasets across Row and Column dimensions.
  * Summary calculations (`Sum`, `Count`, `Average`, `Min`, `Max`), interactive collapsible drill-down nodes (`▶`/`▼`), automated sub-totals, and grand totals.
* **`ZeroFilterControl` & `FilterCriteria`:** Enterprise Visual Query Builder UI with boolean tree logic (`AND`, `OR`, `NOT AND`, `NOT OR`) and SQL WHERE generation.

### ⏱️ Range Controls & Visual Timeline
* **`RangeControl` / `DateTimeRangeSlider`:** Interactive dual-thumb range selector with background sparkline / distribution histogram track, continuous and discrete range selection (numeric and DateTime intervals), auto interval tick formatting, draggable range window, and snap-to-intervals.

### 🛡️ Form Validation & Localization Engine
* **`ZeroValidationProvider` & `ZeroErrorProvider`:** Enterprise-grade form validation and visual error notification framework. Supports declarative fluent rules (`NotEmpty`, `Range`, `Regex`, `Email`, `Length`, `Custom`), animated pulsing vector error badges, warning glyphs, focus outlines, and automatic scroll-to-first-error.
* **`ZeroLocalizer` Integration:** Instant culture hot-switching (`en-US`, `vi-VN`) at runtime across all grids, date pickers, dialogs, and validation error messages without restarting the application.
* **`IZeroEditor` & `ZeroDataBinder`:** Standardized data-binding and editor protocol (`EditValue`, `IsModified`, `ReadOnly`, `ResetModified()`), enabling automated two-way property binding and validation hookup.

### ✏️ Modern Enterprise Editors (`ZeroUI.WinForms.Editors`)
* **`ZeroGridLookup`:** Multi-column dropdown editor hosting an embedded virtual DataGrid with instant live search.
* **`ZeroCheckedComboBox`:** Multi-select dropdown with checkbox items and search.
* **`ZeroTokenEdit`:** Tag & badge input with dismissible chips and keyboard navigation.
* **`ZeroColorPicker`:** Swatch matrix and HEX color editor.
* **`ZeroDatePicker` & `ZeroDateRangePicker`:** Multi-tier zoom navigation calendar (Days &rarr; Months &rarr; Years) and dual-date range presets.

### 📐 Layout, Docking & Workflows
* **`ZeroDockManager`:** Multi-zone docking system (Left/Right/Top/Bottom/Document) with splitters, auto-hide tabs, and `ZeroFloatingWindow` for detached multi-monitor workspaces.
* **`ZeroWorkspaceSerializer`:** Pure zero-dependency JSON layout persistence engine capturing and restoring DockPanel and DataGrid column configurations.
* **`ZeroWizard`:** Multi-step process workflow wizard with validation and step progress indicator.
* **`ZeroToolbar` & `ZeroSideNav`:** Anti-aliased action toolbar with collision guard and collapsible vertical navigation bar.

### 🏭 Industrial, SCADA & MES Suite (`ZeroUI.WinForms.Industrial`)
* Standard P&ID animated vector controls: `ZeroIndustrialPump`, `ZeroIndustrialMotor`, `ZeroIndustrialFan`, `ZeroIndustrialHeater`, `ZeroConveyorBelt`, `ZeroPneumaticCylinder`, `ZeroIndustrialSensor`, `ZeroIndustrialValve`, `ZeroTank3D`, `ZeroPipeFlow`.
* **`ZeroGanttChart` & `ZeroTreeList`:** Production scheduling timeline and virtual multi-level BOM tree with cascading tri-state checkboxes.
* **ISA-18.2 Alarm Grid (`ZeroAlarmGrid`):** Real-time alarm monitoring, state badges, and operator acknowledgment.
* **60 FPS Real-Time Trend Oscilloscope (`ZeroTrendChart`):** Multi-channel real-time ring buffer plotting with limit lines (USL/LSL) and cursor crosshairs.

### 📊 Modern Business & Analytics Charts (`ZeroUI.WinForms.Charts`)
* **`ZeroBoxPlotChart`:** Statistical SPC Box-and-Whisker quality inspection chart with USL/LSL limits.
* Column, Bar, Spline, Area, Pie, Donut, Candlestick, Radar, Funnel, and Waterfall charts with subpixel antialiasing and hover tooltips.

---

## 📦 Installation

```powershell
dotnet add package ZeroUI.WinForms --version 1.2.0
```

---

## 🚀 Quick Example

```csharp
using ZeroUI.WinForms.DataGrid;
using ZeroUI.WinForms.Validation;
using ZeroUI.WinForms.Theme;

// 1. Initialize Virtual Data Grid
var grid = new ZeroGridControl
{
    Dock = DockStyle.Fill,
    RowDensity = ZeroGridDensity.Normal
};

grid.Columns.Add(new ZeroColumn("ID", 70, CellAlignment.Right) { ColumnType = GridColumnType.Numeric });
grid.Columns.Add(new ZeroColumn("Item Code", 120, CellAlignment.Left));
grid.Columns.Add(new ZeroColumn("Quantity", 90, CellAlignment.Right) { ColumnType = GridColumnType.Numeric });

// 2. Attach Form Validation Provider
var validator = new ZeroValidationProvider();
validator.SetRule(grid, new ValidationRule("GridData")
         .Custom(val => grid.RowCount > 0, "Grid must contain at least one item."));

this.Controls.Add(grid);
```

Full documentation and screenshots: [github.com/kzxl/ZeroUI](https://github.com/kzxl/ZeroUI)
