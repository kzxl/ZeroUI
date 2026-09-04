# ZeroUI.WinForms ⚡

Ultra-high-performance WinForms industrial UI suite with 10M+ rows virtual grid, 60 FPS SCADA & P&ID controls, and modern Obsidian Dark/Clean Light theme (`net462`, `net8.0-windows`).

[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/kzxl/ZeroUI)
[![GitHub](https://img.shields.io/badge/GitHub-kzxl%2FZeroUI-blue.svg)](https://github.com/kzxl/ZeroUI)

---

## 🌟 Key Features

* **ZeroGrid Virtual Big Data Grid & Query (`ZeroUI.WinForms.DataGrid`):**
  * Effortlessly renders **10,000,000+ rows** with zero GC allocations on hot scrolling loops (`ZeroGridControl`).
  * **`ZeroFilterControl`**: Enterprise Visual Query Builder UI with boolean tree logic and SQL WHERE generation.
  * Single-HWND architecture with Win32 Memory DC DIBSection double-buffering (eliminates Win32 handle leaks and GDI churn).
  * High-speed pointer swap sorting, debounced search filtering (150ms), density switching (`Compact`, `Normal`, `Comfortable`), and streaming CSV export (>1,100,000 rows/s).
* **Enterprise Editors (`ZeroUI.WinForms.Editors`):**
  * **`ZeroGridLookup`**: Multi-column dropdown editor hosting an embedded virtual DataGrid with instant live search.
  * **`ZeroCheckedComboBox`**: Multi-select dropdown with checkbox items and search.
  * **`ZeroTokenEdit`**: Tag & badge input with dismissible chips and keyboard navigation.
  * **`ZeroColorPicker`**: Swatch matrix and HEX color editor.
* **Layout, Docking & Workflows (`ZeroUI.WinForms.Docking`, `Layout`):**
  * **`ZeroDockManager`**: Multi-zone docking system (Left/Right/Top/Bottom/Document) with splitters, auto-hide, and `ZeroFloatingWindow` for detached multi-monitor workspaces.
  * **`ZeroWizard`**: Multi-step process workflow wizard with validation and step progress indicator.
* **Industrial & SCADA Process Actuators (`ZeroUI.WinForms.Industrial`):**
  * Standard P&ID animated vector controls: `ZeroIndustrialPump`, `ZeroIndustrialMotor`, `ZeroIndustrialFan`, `ZeroIndustrialHeater`, `ZeroConveyorBelt`, `ZeroPneumaticCylinder`, `ZeroIndustrialSensor`, `ZeroIndustrialValve`, `ZeroTank3D`, `ZeroPipeFlow`.
  * **`ZeroGanttChart`**: Production scheduling timeline with task hierarchies and progress bars.
  * **`ZeroPropertyGrid`**: High-speed categorized reflection property inspector.
  * **ISA-18.2 Alarm Grid (`ZeroAlarmGrid`):** Real-time alarm monitoring, state badges, and operator acknowledgment.
  * **60 FPS Real-Time Trend Oscilloscope (`ZeroTrendChart`):** Multi-channel real-time ring buffer plotting with limit lines (USL/LSL) and cursor crosshairs.
* **Modern Business & Analytics Charts (`ZeroUI.WinForms.Charts`):**
  * **`ZeroBoxPlotChart`**: Statistical SPC Box-and-Whisker quality inspection chart with USL/LSL limits.
  * Column, Bar, Spline, Area, Pie, Donut, Candlestick, Radar, Funnel, and Waterfall charts with subpixel antialiasing and hover tooltips.
* **Overlays, Feedback & Reporting (`ZeroUI.WinForms.Overlays`, `Feedback`, `Reporting`):**
  * **`ZeroPrintPreview`**: Vector document and report print previewer with zoom and direct printer dispatch.
  * **`ZeroSkeleton`**: GDI+ shimmer loading placeholder for cards, avatars, and data grids.
  * **`ZeroToast` & `ZeroModal`**: Non-blocking toast notification stack and backdrop-dimmed dialogs.
* **Smart Warehouse & Logistics (`ZeroUI.WinForms.Warehouse`):**
  * `ZeroBarcodeScanControl` (hardware USB wedge auto-timing detection), `ZeroInventoryCard`, `ZeroLotSelector` (FIFO/FEFO), `ZeroWarehouseRack` 2D rack visualizer.
* **Modern Form Controls & Theme Engine:**
  * Clean Light and Obsidian Dark modes with reactive global theme switching.
  * Rounded corners (Windows 11 Fluent) or sharp industrial styling.
  * Anti-aliased buttons, multi-tier zoom date picker (`ZeroDatePicker`), switches, progress bars, segmented controls, and non-blocking toast notifications (`ZeroToast`).

---

## 📦 Installation

```powershell
dotnet add package ZeroUI.WinForms
```

---

## 🚀 Quick Example

```csharp
using ZeroUI.WinForms.DataGrid;
using ZeroUI.WinForms.Theme;

// Apply global modern theme (Clean Light or Obsidian Dark)
ZeroTheme.SetSkin(ZeroSkin.ObsidianDark);

// Initialize Virtual Data Grid
var grid = new ZeroGridControl
{
    Dock = DockStyle.Fill,
    RowDensity = ZeroGridDensity.Normal
};

grid.AddColumn("ID", "ID", 80, ZeroGridColumnAlign.Center);
grid.AddColumn("Code", "Product Code", 140);
grid.AddColumn("Name", "Product Name", 250);
grid.AddColumn("Stock", "Current Stock", 120, ZeroGridColumnAlign.Right);

// Bind high-performance virtual data source
grid.SetDataSource(myVirtualSource);
this.Controls.Add(grid);
```

Full documentation and screenshots: [github.com/kzxl/ZeroUI](https://github.com/kzxl/ZeroUI)
