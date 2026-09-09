# ZeroUI: Comprehensive Industrial & Enterprise Control Catalog

A reference catalog of all enterprise and industrial controls provided by ZeroUI across WinForms and WPF.

---

## 1. DataGrid & Query Builder Subsystem (`ZeroUI.WinForms.DataGrid`)

* **`ZeroGridControl`**: High-performance virtual data grid with Win32 Memory DC DIBSection rendering, custom column definitions, alignments, sorting, and row density switching (`Compact = 24px`, `Normal = 28px`, `Comfortable = 36px`). Easily handles 10,000,000+ virtual rows at 60 FPS.
* **`PivotGridControl` / `ZeroPivotGrid`** *(WinForms & WPF)*: High-performance OLAP multidimensional aggregation matrix. Dynamically groups tabular data across Row and Column dimensions, computing configurable summary aggregations (`Sum`, `Count`, `Average`, `Min`, `Max`), interactive collapsible drill-down tree nodes (`▶`/`▼`), automated sub-totals, and grand totals with zero-alloc matrix virtualization.
* **`ZeroFilterControl` & `FilterCriteria`** *(WinForms & WPF)*: Enterprise Visual Query Builder UI rendering condition trees with boolean operator badges (`AND`, `OR`, `NOT AND`, `NOT OR`), comparison selectors (`Equals`, `GreaterThan`, `Contains`, `Between`, `IsNull`), and automated SQL `WHERE` clause generation.
* **`ZeroGridSearchBar`**: Integrated live search bar with debounced input (150ms), live match counter, density switcher, and CSV export trigger.
* **`ZeroGridPagination`**: Enterprise pagination toolbar with page size selector (`50`, `100`, `500`, `1000`, `All`), row statistics, and navigation buttons.
* **`ZeroGridExporter`**: High-throughput streaming CSV exporter capable of outputting >1,100,000 rows/sec directly to disk with zero allocations.

---

## 2. Analytics & Business Charts Subsystem (`ZeroUI.WinForms.Charts`)

* **`ZeroChart`**: Universal chart engine supporting Cartesian (Column, Bar, Line, Spline, Area) and Polar (Pie, Donut) visualizations with subpixel antialiasing, automatic human-friendly $Y$-axis rounding, interactive cursor crosshairs, halo tooltips, and clickable legends.
* **`ZeroBarChart`**: Specialized Column and Bar comparison chart with grouped and stacked modes (`IsHorizontal = true/false`, `IsStacked = true/false`), rounded column caps, and custom value formatting.
* **`ZeroLineChart`**: Specialized Line and Area trend chart featuring smooth Catmull-Rom spline curves (`IsCurved = true`), vertical translucent area gradient fills with bottom fade, point markers, and interactive hover tooltips.
* **`ZeroPieChart`**: Categorical distribution chart supporting full Pie and Donut rings (`IsDonut = true`, `DonutHoleRatio = 0.58f`), center KPI summary metrics, radial hover slice explosion, and percentage calculations.
* **`ZeroCandlestickChart`**: High-performance OHLC candlestick chart with volume histogram, moving average (MA) curve, and interactive crosshair HUD.
* **`ZeroBoxPlotChart`** *(WinForms & WPF)*: Statistical Box-and-Whisker chart for industrial Six Sigma / SPC tolerance inspection with five-number statistical summaries (Min, Q1, Median, Q3, Max) and USL/LSL specification limits.
* **`ZeroRadarChart`**: Multi-dimensional radar and spider chart with customizable concentric web rings, radial spokes, polygonal series fills, and vertex markers.
* **`ZeroFunnelChart`**: Conversion funnel chart with sleek trapezoid stages, percentage drops, and automated inward yield rate computation.
* **`ZeroWaterfallChart`**: Financial and variance waterfall chart visualizing cumulative effects of sequential positive and negative values.

---

## 3. Smart Warehouse & Logistics Subsystem (`ZeroUI.WinForms.Warehouse`)

* **`ZeroBarcodeScanControl`**: Industrial barcode & QR code scanner workstation control with hardware USB wedge scanner timing detection (<35ms delta auto-detection), duplicate scan suppression, instant audio chime, and parsed metadata cards.
* **`ZeroInventoryCard`**: Industrial stock telemetry card featuring the Three Golden Metrics (Available, Waiting, Reserved), dynamic segment distribution bar, and warehouse location info.
* **`ZeroLotSelector`**: Automated FIFO (First-In, First-Out) and FEFO (First-Expired, First-Out) lot allocation selector with quarantine/expiry locks and 1-way data flow.
* **`ZeroStockMovementTimeline`**: Industrial batch traceability timeline tree visualizing lot lifecycle from receipt to production dispatch, sales shipment, and stock balance.

---

## 4. Industrial, SCADA & MES Subsystem (`ZeroUI.WinForms.Industrial`)

* **Pumps & Valves**: `ZeroIndustrialPump` (variable RPM speed, flow arrows), `ZeroIndustrialValve` (proportional control, angle indication).
* **Tanks & Vessels**: `ZeroTank3D` (liquid level with gradient fluid physics, low/high alarm setpoints).
* **Motors & Drives**: `ZeroIndustrialMotor` (dynamic rotation, overload trip indicators).
* **Thermal & Cooling**: `ZeroIndustrialHeater` (heat shimmer, temperature gradient), `ZeroIndustrialFan` (ventilation blades).
* **Pneumatics & Actuators**: `ZeroPneumaticCylinder` (stroke extension 0–100%, limit switches).
* **Conveyors & Packaging**: `ZeroConveyorBelt` (belt speed, item indexing), `ZeroProductionCounter` (Plan, Actual, NG counts).
* **Sensors & Gauges**: `ZeroGauge` (Bourdon tube dial with customizable warning/danger zones), `ZeroDigitalIndicator` (7-segment / LED digital readout), `ZeroInterlockIndicator` (permissive status check).
* **P&ID Synoptics**: `ZeroPipeFlow` (animated subpixel liquid pulse lines connecting equipment).
* **Alarm Annunciators**: `ScadaAlarmEngine` (ISA-18.2 state machine with `ActiveUnack`, `ActiveAck`, `ClearedUnack`, `Normal`, `Shelved`).

---

## 5. Enterprise Layout & Navigation Controls

* **`ZeroSideNav`**: Collapsible industrial side navigation with badge counts, flyout menus, and active states.
* **`ZeroRibbon`**: Modern Office/AutoCAD style ribbon bar with grouped tool buttons, gallery selectors, and contextual tabs.
* **`ZeroSteps`**: Multi-stage industrial wizard indicator with completed, active, pending, and error states.
* **`ZeroTimeline`**: Vertical activity timeline for audit logs and machine event journals.
* **`ZeroToolbar`**: High-density action bar with icon-text buttons, dropdowns, and separators.
* **`ZeroHardwareCard`**: SDF rounded panel with analytical soft drop shadows and customizable glow.
