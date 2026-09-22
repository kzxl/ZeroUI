# ZeroUI: Comprehensive Industrial & Enterprise Control Catalog

A complete reference catalog of enterprise and industrial controls provided by ZeroUI across Windows Forms and WPF (.NET Framework 4.6.2, .NET 8.0/9.0).

> [!NOTE]
> **Enterprise Naming Standard:** All controls strictly use standard enterprise names without the `Zero` prefix (e.g., `GridControl`, `ChartControl`, `SevenSegment`, `LinearGauge`, `ValidationProvider`). Full backward compatibility is preserved across all namespaces via `[Obsolete]` class aliases and shims.

---

## 1. Data Matrix & Virtualization Subsystem (`ZeroUI.WinForms.DataGrid` / `ZeroUI.Wpf.DataGrid`)

* **`GridControl`** *(Legacy alias: `ZeroGridControl`)*:
  * High-performance virtual data grid powered by Win32 Memory DC DIBSection double-buffering (WinForms) or Direct3D 11 shared texture bridge (WPF).
  * Smoothly renders **10,000,000+ virtual rows** at 60–144 FPS with **0 bytes GC heap allocation** on hot scrolling loops.
  * Features multi-column sorting (`RowIndexMap`), frozen columns (left/right pinned), density switching (`Compact = 24px`, `Normal = 28px`, `Comfortable = 36px`), and hierarchical row grouping (`GroupedRowIndexMap`).
  * Built-in CardView and TileView presentation modes using `GridCardLayoutManager`.
* **`PivotGridControl`** *(Legacy alias: `ZeroPivotGrid`)*:
  * High-performance multidimensional OLAP aggregation matrix.
  * Dynamically pivots tabular datasets across Row and Column dimensions, calculating summary aggregations (`Sum`, `Count`, `Average`, `Min`, `Max`), interactive collapsible drill-down tree nodes (`▶`/`▼`), automated sub-totals, and grand totals with zero-alloc matrix virtualization.
* **`TreeList`** *(Legacy alias: `ZeroTreeList`)*:
  * Hierarchical virtual multi-level BOM tree with expandable nodes, tri-state cascading checkboxes (`Unchecked`, `Checked`, `Indeterminate`), and guidelines.
* **`FilterControl` & `FilterCriteria`** *(Legacy alias: `ZeroFilterControl`)*:
  * Enterprise Visual Query Builder UI rendering condition trees with boolean operator badges (`AND`, `OR`, `NOT AND`, `NOT OR`), comparison selectors (`Equals`, `GreaterThan`, `Contains`, `Between`, `IsNull`), and automated SQL `WHERE` clause generation.
* **`GridSearchBar`** *(Legacy alias: `ZeroGridSearchBar`)*:
  * Integrated search header with 150ms debounced input, live match counter, density switcher, and CSV export trigger.
* **`GridPagination`** *(Legacy alias: `ZeroGridPagination`)*:
  * Enterprise pagination toolbar with page size selector (`50`, `100`, `500`, `1000`, `All`), row statistics, and navigation buttons.
* **`GridDataExporter`** *(Legacy alias: `ZeroGridExporter`)*:
  * High-throughput streaming CSV and OpenXML (`.xlsx`) exporter streaming >1,100,000 rows/sec directly to disk without loading datasets into memory.

---

## 2. Analytics & Business Charts Subsystem (`ZeroUI.WinForms.Charts`)

* **`ChartControl`** *(Legacy alias: `ZeroChart`)*:
  * Universal multi-series chart engine supporting Cartesian (Column, Bar, Line, Spline, Area) and Polar (Pie, Donut) visualizations with subpixel antialiasing, automatic human-friendly $Y$-axis rounding, interactive cursor crosshairs, halo tooltips, and clickable legends.
* **`BarChart`** *(Legacy alias: `ZeroBarChart`)*:
  * Specialized Column and Bar comparison chart with grouped and stacked modes (`IsHorizontal = true/false`, `IsStacked = true/false`), rounded column caps, and custom value formatting.
* **`LineChart`** *(Legacy alias: `ZeroLineChart`)*:
  * Specialized Line and Area trend chart featuring smooth Catmull-Rom spline curves (`IsCurved = true`), vertical translucent area gradient fills with bottom fade, point markers, and interactive hover tooltips.
* **`PieChart`** *(Legacy alias: `ZeroPieChart`)*:
  * Categorical distribution chart supporting full Pie and Donut rings (`IsDonut = true`, `DonutHoleRatio = 0.58f`), center KPI summary metrics, radial hover slice explosion, and percentage calculations.
* **`CandlestickChart`** *(Legacy alias: `ZeroCandlestickChart`)*:
  * High-performance OHLC candlestick chart with volume histogram, moving average (MA) curve, and interactive crosshair HUD.
* **`BoxPlotChart`** *(Legacy alias: `ZeroBoxPlotChart`)*:
  * Statistical Box-and-Whisker chart for industrial Six Sigma / SPC tolerance inspection with five-number statistical summaries (Min, Q1, Median, Q3, Max) and USL/LSL specification limits.
* **`RadarChart`** *(Legacy alias: `ZeroRadarChart`)*:
  * Multi-dimensional radar and spider chart with customizable concentric web rings, radial spokes, polygonal series fills, and vertex markers.
* **`FunnelChart` & `PyramidChart`** *(Legacy aliases: `ZeroFunnelChart`, `ZeroPyramidChart`)*:
  * Conversion funnel and yield rate visualizer with dual presentation modes, interactive slice selection, drop-off metrics, and automated inward yield rate computation.
* **`WaterfallChart`** *(Legacy alias: `ZeroWaterfallChart`)*:
  * Financial and variance waterfall chart visualizing cumulative effects of sequential positive and negative values.

---

## 3. Industrial Instrumentation & SCADA Suite (`ZeroUI.WinForms.Industrial` / `ZeroUI.Wpf.Industrial`)

* **`SevenSegment`** *(Legacy alias: `ZeroSevenSegment`)*:
  * Industrial 7-segment digital LED readout display with 100% feature parity across WinForms and WPF.
  * Backed by platform-neutral `SevenSegmentState` in `ZeroUI.Core.Industrial` with hardware bitmask decoding (`0x7F` mapping).
  * Supports custom digit counts, decimal points, colons, negative signs, italic slant angles, unlit ghost segments, and standardized color presets (`Green`, `Amber`, `Red`, `Cyan`, `Blue`, `White`).
* **`LinearGauge`** *(Legacy alias: `ZeroLinearGauge`)*:
  * High-precision industrial vertical and horizontal thermometer gauge with multi-color threshold bands (`GaugeThresholdRange`: Normal, Warning, Critical) and magnetic needle damping.
* **`RadialGauge`** *(Legacy alias: `ZeroRadialGauge` / `ZeroGauge`)*:
  * Industrial Bourdon tube circular dial gauge (180° / 270° sweep) with subpixel anti-aliased needle, warning arcs, and direct telemetry binding via `IScadaBindable`.
* **`TrendChart`** *(Legacy alias: `ZeroTrendChart`)*:
  * 60 FPS real-time multi-channel oscilloscope streaming high-frequency telemetry via ring buffers (`ZeroTripleBuffer`), limit lines (USL/LSL), and LTTB peak downsampling.
* **`AlarmGrid`** *(Legacy alias: `ZeroAlarmGrid`)*:
  * ISA-18.2 compliant real-time alarm monitoring grid with state-synchronized blinking driven by `ZeroAnimationClock`, severity badges (`Critical`, `High`, `Medium`, `Low`), and operator acknowledgment.
* **`PlantMimicCanvas` & P&ID Vector Primitives**:
  * Single-HWND vector synoptic canvas supporting infinite pan/zoom and hardware nodes:
    * `IndustrialPump`: Variable RPM speed, fluid flow arrows.
    * `IndustrialValve`: Proportional control, angle indication (0–100%).
    * `Tank3D`: Cylindrical fluid vessel with real-time level gradient physics and low/high alarm setpoints.
    * `IndustrialMotor`: Dynamic rotor rotation, overload trip indicators.
    * `IndustrialHeater`: Heat shimmer and temperature gradient.
    * `IndustrialFan`: Ventilation impeller blades driven by `ZeroAnimationClock`.
    * `PneumaticCylinder`: Bidirectional stroke extension (0–100%) and magnetic limit switches.
    * `ConveyorBelt`: Roller belt speed, item indexing, moving product flow.
    * `PipeFlow`: Subpixel animated liquid/gas pulse lines connecting equipment.
    * `LedTowerControl`: 4-tier Andon signal tower (`Red`, `Amber`, `Green`, `Blue`) with `On`, `Off`, `BlinkFast`, `BlinkSlow` states.

---

## 4. Modern Enterprise Form Editors (`ZeroUI.WinForms.Editors` / `ZeroUI.Wpf.Editors`)

All editors implement `IZeroEditor` and integrate with `ZeroDataBinder` for automated two-way binding:
* **`TextEdit` & `MemoEdit`**: Single-line and multi-line themed text editors with focus glow, clear button, and character counter.
* **`SpinEdit`**: High-precision numeric stepper and spin box with mouse hold acceleration and decimal formatting.
* **`CheckEdit` & `ToggleSwitch`**: Sleek modern checkboxes and iOS-style toggle switches with custom glyphs.
* **`RatingControl`**: Inspection severity and QA rating half-star selector supporting stars, shields, hearts, and diamonds.
* **`GridLookupEdit`** *(Legacy alias: `ZeroGridLookup`)*: Multi-column dropdown editor hosting an embedded virtual DataGrid with instant live search across 100k+ records.
* **`SearchLookUpEdit`**: Paginated high-capacity dropdown with persistent top search bar for 50,000+ item master catalogs.
* **`CheckedComboBoxEdit`** *(Legacy alias: `ZeroCheckedComboBox`)*: Multi-select dropdown with checkbox items and search.
* **`TokenEdit`** *(Legacy alias: `ZeroTokenEdit`)*: Tag & badge input with dismissible chips and keyboard navigation.
* **`ColorPickEdit`** *(Legacy alias: `ZeroColorPicker`)*: Swatch matrix and HEX color editor.
* **`DateEdit` & `DateRangePicker`**: Multi-tier zoom navigation calendar (Days &rarr; Months &rarr; Years) and dual-date range presets.
* **`RangeControl` / `DateTimeRangeSlider`**: Interactive dual-thumb range selector with background sparkline / distribution histogram track.
* **`ValidationProvider`**: Declarative fluent validation engine (`NotEmpty`, `Range`, `Regex`, `Email`, `Custom`) with animated pulsing vector badges and auto scroll-to-error.

---

## 5. Creative & Media Controls Suite (`ZeroUI.Wpf.Editors`)

* **`CurveEditor` & `CurveMath`**: Monotone cubic spline curve editor supporting RGB and per-channel curves (Red, Green, Blue), 4x4 coordinate grid, interactive control point manipulation, delete, and evaluate.
* **`ColorWheelEdit` / `ColorWheel`**: 360° Hue/Saturation color grading wheel with pre-calculated antialiased gamut bitmap, smooth dragging crosshairs, and numeric degree/distance updates.
* **`CompareViewerControl`**: Dual-image comparison inspection viewer featuring Split-Curtain (with draggable divider line), Side-by-Side (horizontal split), and Fade-Blend modes with synchronized panning and zoom.
* **`CropBoxControl`**: Non-destructive image cropping canvas with 8-point bounding box handles, aspect ratio locking, and dynamic rule overlays (Rule of Thirds, Golden Ratio, Diagonals, Grid, None).
* **`HistogramScopeControl`**: Real-time histogram and waveform scope supporting RGB Parade, Luminance curve, 5-zone parametric tonal range dragging (Blacks, Shadows, Midtones, Highlights, Whites), and clipping warnings.
* **`FacetedFilterBar`**: Multi-dimension drill-down search and faceted filter bar featuring categorized pill chips, match count badges, active indicator, and batch reset.
* **`MiniMapNavigator`**: Floating / docking thumbnail canvas overview with an interactive translucent viewport box for fluid 2D pan/zoom navigation.
* **`MaskGizmoOverlay`**: Non-destructive linear / radial gradient and brush mask gizmo with interactive origin pin, cardinal handles, feather boundary ellipses, and rotation arcs.
* **`HistoryTimelineControl`**: Visual undo/redo history and snapshot tree timeline with step indexing, relative timestamps, state jumping, and snapshot curation.
* **`FilmstripScrollerControl`**: Horizontal smooth-scrolling thumbnail filmstrip with rating stars, color label flags, active badges, and multi-selection support.
* **`DominantPaletteControl`**: Dominant color palette inspector with extracted swatches, percentage labels, click-to-copy hex codes, color theory harmonies, and contrast evaluation.
* **`ExifTelemetryCard`**: Camera & exposure HUD metrics deck (Shutter, Aperture, ISO, Focal Length), device banner, GPS location link, and detailed collapsible EXIF table.
* **`NumericSliderEdit`**: High-precision slider + label + direct numeric input composite control with double-click reset to default, Alt-key clipping preview, and drag-scrubbing.
* **`BatchTaskQueueControl`**: Production-grade background batch task queue controller with concurrency selection (1..8 workers), pause/resume orchestration, retry/remove item actions, and live visual progress bars.
* **`TokenPatternEditor`**: Filename and dynamic text template pattern editor with interactive token insertion chips, caret placement, and live evaluated preview.
* **`ThumbnailGridControl`**: Lightroom-style light table photo browser grid with responsive wrap-panel card layout, Ctrl/Shift multi-selection, rating stars, color label badges, pick flags, and overlay badges.

---

## 6. Layout, Docking, Reporting & Workflows

* **`DockManager`** *(Legacy alias: `ZeroDockManager`)*: Multi-zone docking system (Left/Right/Top/Bottom/Document) with splitters, auto-hide tabs, and `FloatingWindow` for detached multi-monitor workspaces.
* **`WorkspaceSerializer`** *(Legacy alias: `ZeroWorkspaceSerializer`)*: Pure zero-dependency JSON layout persistence engine capturing and restoring DockPanel and DataGrid column configurations.
* **`WizardControl`** *(Legacy alias: `ZeroWizard`)*: Multi-step process workflow wizard with validation and step progress indicator.
* **`ProcessMap`**: Interactive business process flowchart & workflow navigation map with customizable UserControl action triggers, orthogonal routing, and JSON serialization.
* **`SpreadsheetControl`**: High-performance vector spreadsheet engine with formula evaluation (`SUM`, `AVERAGE`, `MIN`, `MAX`, `IF`), interactive formula bar, and in-place editing.
* **`PdfViewerControl`**: Vector CAD schematic and technical PDF document viewer with continuous vertical scrolling, zoom (Fit Width, 25%–400%), and full-text search highlights.
* **`ToolbarControl` & `SideNavControl`**: Anti-aliased action toolbar with collision guard and collapsible vertical navigation bar.

---

## 7. Smart Warehouse & Logistics Subsystem (`ZeroUI.WinForms.Warehouse`)

* **`BarcodeScanControl`** *(Legacy alias: `ZeroBarcodeScanControl`)*: Industrial USB wedge barcode/QR scanner workstation control with hardware timing delta auto-detection (<35ms), duplicate scan suppression, and parsed metadata cards.
* **`WarehouseRack`** *(Legacy alias: `ZeroWarehouseRack`)*: 2D interactive storage rack visualizer (Bay x Level x Bin) with occupancy color coding and lot inspection.
* **`InventoryCard`** *(Legacy alias: `ZeroInventoryCard`)*: Industrial stock telemetry card featuring Available, Waiting, and Reserved segment distributions.
* **`LotSelector`** *(Legacy alias: `ZeroLotSelector`)*: Automated FIFO/FEFO lot allocation selector with quarantine and expiry locks.
