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

## 2. Analytics & Business Charts Subsystem (`ZeroUI.WinForms.Charts` / `ZeroUI.Wpf.Charts`)

All specialized analytics charts are built upon a decoupled Clean Architecture: **100% mathematical logic and layout calculation** reside in platform-agnostic `ZeroUI.Core.Analytics` engines, while thin presentation view layers render via GDI+ (`ZeroUI.WinForms.Charts`) and `DrawingContext`/`StreamGeometry` (`ZeroUI.Wpf.Charts`).

* **`ChartControl`** *(Legacy alias: `ZeroChart`)*:
  * Universal multi-series chart engine supporting Cartesian (Column, Bar, Line, Spline, Area) and Polar (Pie, Donut) visualizations with subpixel antialiasing, automatic human-friendly $Y$-axis rounding, interactive cursor crosshairs, halo tooltips, and clickable legends.
* **`BarChart`** *(Legacy alias: `ZeroBarChart`)*:
  * Specialized Column and Bar comparison chart with grouped and stacked modes (`IsHorizontal = true/false`, `IsStacked = true/false`), rounded column caps, and custom value formatting.
* **`LineChart`** *(Legacy alias: `ZeroLineChart`)*:
  * Specialized Line and Area trend chart featuring smooth Catmull-Rom spline curves (`IsCurved = true`), vertical translucent area gradient fills with bottom fade, point markers, and interactive hover tooltips.
* **`PieChart`** *(Legacy alias: `ZeroPieChart`)*:
  * Categorical distribution chart supporting full Pie and Donut rings (`IsDonut = true`, `DonutHoleRatio = 0.58f`), center KPI summary metrics, radial hover slice explosion, and percentage calculations.
* **`HistogramChart`** *(New in v1.9.0)*:
  * Continuous statistical distribution histogram powered by `HistogramEngine`. Supports automated binning via Sturges' rule ($k = \lceil \log_2(n) + 1 \rceil$), statistical distribution moments (Mean $\mu$, StdDev $\sigma$), and parametric Gaussian normal distribution PDF bell curve overlay with hover tooltips.
* **`ScatterChart`** *(New in v1.9.0)*:
  * Cartesian bivariate correlation and 3D Bubble plot powered by `ScatterPlotEngine`. Computes Ordinary Least Squares (OLS) linear regression trendlines ($y = mx + b$), correlation coefficient $R^2$, and magnitude bubble area scaling with alpha transparency blending.
* **`SunburstChart`** *(New in v1.9.0)*:
  * Multi-tiered hierarchical radial partition diagram powered by `SunburstLayoutEngine`. Partitions $360^\circ$ circular angular sectors across recursive tree depths with polar coordinate hit-testing and central overview breadcrumbs.
* **`LollipopChart`** *(New in v1.9.0)*:
  * High-density comparison chart powered by `LollipopEngine`. Features thin stem lines, circular dot marker heads, horizontal and vertical orientation modes, and baseline vector alignments.
* **`TreemapChart`** *(New in v1.9.0)*:
  * Squarified Treemap partition chart powered by `TreemapEngine` (Bruls-Huizing-van Wijk algorithm). Renders multi-category asset allocation and nested hierarchical proportional weights.
* **`SankeyChart`** *(New in v1.9.0)*:
  * Energy and process flow vector chart powered by `SankeyLayoutEngine`. Resolves multi-column flow stages, node heights, and smooth cubic Bézier flow ribbons with dynamic hover highlighting.
* **`BulletChart`** *(New in v1.9.0)*:
  * Stephen Few linear bullet graph powered by `BulletBenchmarkEngine`. Benchmarks dense executive KPIs against comparative targets and qualitative threshold ranges (`Poor`, `Satisfactory`, `Good`).
* **`ParetoChart`** *(New in v1.9.0)*:
  * Quality root cause distribution chart powered by `ParetoEngine`. Computes Juran's 80/20 rule, sorting defect frequencies, plotting cumulative percentage curves, and delineating the "Vital Few" from the "Useful Many".
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
* **`NumericSliderEdit`**: High-precision slider + label + direct numeric input composite control with double-click reset to default, Alt-key clipping preview, and drag-scrubbing.
* **`FacetedFilterBar`**: Multi-dimension drill-down search and faceted filter bar featuring categorized pill chips, match count badges, active indicator, and batch reset.

---

## 5. Media & Creative Image Controls Suite (`ZeroUI.WinForms.Media` / `ZeroUI.Wpf.Media`)

All creative photo studio, inspection, color grading, and media controls reside cleanly in dedicated Media subsystems (`ZeroUI.WinForms.Media` and `ZeroUI.Wpf.Media`). Complete backward compatibility across 5 release cycles is guaranteed via forwarding shims in `ZeroUI.WinForms.Editors` and `ZeroUI.Wpf.Editors`.

### Core Interactive Image Viewers & Picture Editors
* **`ZImageViewer`** *(WinForms & WPF - Legacy aliases: `ImageViewerControl`, `ZeroImageViewer`)*:
  * High-performance interactive image viewing canvas powered by Direct2D, GDI+, and WPF vector rendering.
  * Smooth sub-pixel infinite pan and cursor-anchored continuous zoom ($0.01\times$ to $64\times$).
  * **Forensic Pixel Grid**: Automatically renders aligned pixel grid overlay when zoom level reaches $\ge 800\%$ for fine sub-pixel inspection and alignment.
  * **Interactive MiniMap Navigator**: Translucent viewport box in corner overview thumbnail with drag navigation.
  * **Telemetry Color Probe HUD**: Real-time hover cursor coordinates $(X, Y)$ and exact RGBA / HEX color readout badge.
  * **Floating Loupe Magnifier (WPF)**: Dedicated magnification lens tracking cursor for instant close-up inspection.
  * **Transform Pipeline**: Real-time lossless rotation ($0^\circ, 90^\circ, 180^\circ, 270^\circ$), horizontal/vertical flip, and interpolation quality switching (`NearestNeighbor`, `Bilinear`, `HighQuality`).
  * **IZeroEditor & IZeroSkinnable**: Two-way data binding support, file-loading, and Obsidian Dark / Clean Light theme synchronization.
* **`ZPictureEdit`** *(WinForms & WPF - Legacy aliases: `PictureEdit`, `ZeroImage`, `ZeroPictureEdit`)*:
  * Anti-aliased image and avatar control with rounded corners, circular clip, initials fallback with deterministic background palettes, presence status indicator dots, drag-and-drop file loading, clipboard sync, and click-to-zoom Lightbox inspection modal.

### Specialized Creative & Inspection Media Editors (`ZeroUI.Wpf.Media`)
* **`ZAnnotationCanvas`** *(New in v2.0.0)*: Interactive canvas for defect tagging and forensic markup. Supports vector drawing of Bounding Boxes, Ellipses, Arrows, Lines, Text Callouts, and Sensitive Info Blur/Pixelation with severity levels (`Ok`, `Warning`, `Defect`, `Critical`), resize handles, and direct bitmap burning (`BurnAnnotationsToBitmap`).
* **`ZMeasurementRuler`** *(New in v2.0.0)*: Precision optical caliper for metrology and machine vision inspection. Supports pixel-to-millimeter/micrometer calibration (`CalibrationFactor`), 2-point linear distance calipers, 3-point angle gauges ($\theta^\circ$), sub-pixel ticks, and telemetry badge overlays.
* **`ZWatermarkOverlay`** *(New in v2.0.0)*: Dynamic security stamp and watermark layer. Supports token resolution (`{User}`, `{Date}`, `{Time}`, `{Machine}`), 9-point anchor placement, full diagonal matrix tiling (`DiagonalTiled`), opacity/rotation, and direct bitmap burning (`BurnWatermarkToBitmap`).
* **`ZVideoPlayer`** *(New in v2.0.0)*: GPU-accelerated video playback workstation with SMPTE timecode readout (`hh:mm:ss:ff`), frame-accurate stepping (`StepForward`, `StepBackward`), speed ratios (0.1x–8.0x), scrub bar, volume, and MVVM playback state management.
* **`ZAudioWaveform`** *(New in v2.0.0)*: Symmetrical audio amplitude waveform visualizer and scrub bar. Supports live playhead tracking, interactive time-range selection (`SelectionStart` / `SelectionEnd`), and waveform data rendering.
* **`ZDocumentDeskew`** *(New in v2.0.0)*: Document straightening, perspective keystone deskew, alignment grid guides, and high-performance pixel binarization (fixed / Otsu thresholding) for OCR and scanned document cleanup.
* **`ZCompareViewer`** *(Legacy alias: `CompareViewerControl`)*: Dual-image comparison inspection viewer featuring Split-Curtain (with draggable divider line), Side-by-Side (horizontal split), and Fade-Blend modes with synchronized panning and zoom.
* **`ZCropBox`** *(Legacy alias: `CropBoxControl`)*: Non-destructive image cropping canvas with 8-point bounding box handles, aspect ratio locking, and dynamic rule overlays (Rule of Thirds, Golden Ratio, Diagonals, Grid, None).
* **`ZHistogramScope`** *(Legacy alias: `HistogramScopeControl`)*: Real-time histogram and waveform scope supporting RGB Parade, Luminance curve, 5-zone parametric tonal range dragging (Blacks, Shadows, Midtones, Highlights, Whites), and clipping warnings.
* **`ZCurveEditor` & `CurveMath`** *(Legacy alias: `CurveEditor`)*: Monotone cubic spline curve editor supporting RGB and per-channel curves (Red, Green, Blue), 4x4 coordinate grid, interactive control point manipulation, delete, and evaluate.
* **`ZColorWheel`** *(Legacy aliases: `ColorWheelEdit`, `ColorWheel`)*: 360° Hue/Saturation color grading wheel with pre-calculated antialiased gamut bitmap, smooth dragging crosshairs, and numeric degree/distance updates.
* **`ZMiniMapNavigator`** *(Legacy alias: `MiniMapNavigator`)*: Floating / docking thumbnail canvas overview with an interactive translucent viewport box for fluid 2D pan/zoom navigation.
* **`ZMaskGizmoOverlay`** *(Legacy alias: `MaskGizmoOverlay`)*: Non-destructive linear / radial gradient and brush mask gizmo with interactive origin pin, cardinal handles, feather boundary ellipses, and rotation arcs.
* **`ZHistoryTimeline`** *(Legacy alias: `HistoryTimelineControl`)*: Visual undo/redo history and snapshot tree timeline with step indexing, relative timestamps, state jumping, and snapshot curation.
* **`ZFilmstripScroller`** *(Legacy alias: `FilmstripScrollerControl`)*: Horizontal smooth-scrolling thumbnail filmstrip with rating stars, color label flags, active badges, and multi-selection support.
* **`ZThumbnailGrid`** *(Legacy alias: `ThumbnailGridControl`)*: Lightroom-style light table photo browser grid with responsive wrap-panel card layout, Ctrl/Shift multi-selection, rating stars, color label badges, pick flags, and overlay badges.
* **`ZDominantPalette`** *(Legacy alias: `DominantPaletteControl`)*: Dominant color palette inspector with extracted swatches, percentage labels, click-to-copy hex codes, color theory harmonies, and contrast evaluation.
* **`ZExifTelemetryCard`** *(Legacy alias: `ExifTelemetryCard`)*: Camera & exposure HUD metrics deck (Shutter, Aperture, ISO, Focal Length), device banner, GPS location link, and detailed collapsible EXIF table.
* **`ZBatchTaskQueue`** *(Legacy alias: `BatchTaskQueueControl`)*: Production-grade background batch task queue controller with concurrency selection (1..8 workers), pause/resume orchestration, retry/remove item actions, and live visual progress bars.
* **`ZTokenPatternEditor`** *(Legacy alias: `TokenPatternEditor`)*: Filename and dynamic text template pattern editor with interactive token insertion chips, caret placement, and live evaluated preview.

---

## 6. Layout, Docking, Reporting & Workflows

* **`DockManager`** *(Legacy alias: `ZeroDockManager`)*: Multi-zone docking system (Left/Right/Top/Bottom/Document) with splitters, auto-hide tabs, and `FloatingWindow` for detached multi-monitor workspaces.
* **`WorkspaceSerializer`** *(Legacy alias: `ZeroWorkspaceSerializer`)*: Pure zero-dependency JSON layout persistence engine capturing and restoring DockPanel and DataGrid column configurations.
* **`WizardControl`** *(Legacy alias: `ZeroWizard`)*: Multi-step process workflow wizard with validation and step progress indicator.
* **`ProcessMap`**: Interactive business process flowchart & workflow navigation map with customizable UserControl action triggers, orthogonal routing, and JSON serialization.
* **`SpreadsheetControl`**: High-performance vector spreadsheet engine with formula evaluation (`SUM`, `AVERAGE`, `MIN`, `MAX`, `IF`), interactive formula bar, and in-place editing.
* **`PdfViewerControl`**: Vector CAD schematic and technical PDF document viewer with continuous vertical scrolling, zoom (Fit Width, 25%–400%), and full-text search highlights.
* **`ZTour`** *(WPF & WinForms - New in v2.0.0, Legacy alias: `ZeroTour`)*:
  * Enterprise interactive onboarding tour and product walkthrough engine matching Ant Design `Tour` standards.
  * Features spotlight target highlighting with dark backdrop cutout masking (`CombinedGeometry.Exclude` / GDI+ `Region.Exclude`), glowing animated bounding box, dynamic popover card with directional arrows, step progress badges (`1/4`), dot indicators, and full keyboard navigation (`Escape`, `Right`, `Left`, `Enter`).
* **`ToolbarControl` & `SideNavControl`**: Anti-aliased action toolbar with collision guard and collapsible vertical navigation bar.

---

## 7. Smart Warehouse & Logistics Subsystem (`ZeroUI.WinForms.Warehouse`)

* **`BarcodeScanControl`** *(Legacy alias: `ZeroBarcodeScanControl`)*: Industrial USB wedge barcode/QR scanner workstation control with hardware timing delta auto-detection (<35ms), duplicate scan suppression, and parsed metadata cards.
* **`WarehouseRack`** *(Legacy alias: `ZeroWarehouseRack`)*: 2D interactive storage rack visualizer (Bay x Level x Bin) with occupancy color coding and lot inspection.
* **`InventoryCard`** *(Legacy alias: `ZeroInventoryCard`)*: Industrial stock telemetry card featuring Available, Waiting, and Reserved segment distributions.
* **`LotSelector`** *(Legacy alias: `ZeroLotSelector`)*: Automated FIFO/FEFO lot allocation selector with quarantine and expiry locks.
