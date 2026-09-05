# ZeroUI.Core ⚡

Ultra-high-performance, zero-allocation core runtime, analytical engines, and industrial automation infrastructure for .NET (`netstandard2.0`, `net462`, `net8.0`).

> [!NOTE]
> **Active Development Notice:** This project is currently in active development. Feedback, suggestions, and contributions from the community are warmly welcome!

[![NuGet Version](https://img.shields.io/badge/nuget-v1.2.0-blue.svg)](https://github.com/kzxl/ZeroUI)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/kzxl/ZeroUI)
[![GitHub](https://img.shields.io/badge/GitHub-kzxl%2FZeroUI-blue.svg)](https://github.com/kzxl/ZeroUI)

---

## 🌟 Key Features

### ⚡ Zero-Alloc Virtualization & Analytical Engines
* **Zero-Allocation Data Virtualization (`IZeroVirtualSource`, `RowIndexMap`):** High-speed pointer swap sorting, debounced search filtering (150ms), and streaming CSV export.
* **Hierarchical Row Mapping (`GroupedRowIndexMap`):** Virtualized multi-level row grouping with expand/collapse states and group summary aggregations.
* **OLAP Pivot Engine (`PivotDataEngine`):** Multidimensional cross-tab aggregation matrix engine supporting Row/Column dimensions, summary measures (`Sum`, `Count`, `Average`, `Min`, `Max`), and hierarchical rollups.
* **Visual Range & Timeline Engine (`RangeModel`, `DateTimeRangeModel`):** High-precision range math supporting continuous/discrete numeric values, DateTime intervals, and snap-to-tick rounding.

### 🛡️ Form Validation & Internationalization Engine
* **Declarative Validation Engine (`ValidationProvider`, `IControlValidationRule`):** Extensible validation framework with built-in rules (`NotEmpty`, `Range`, `Regex`, `Email`, `Length`, `CustomPredicate`).
* **Runtime Dynamic Localization (`ZeroLocalizer`):** Zero-allocation runtime string localization engine with instant culture hot-switching (`en-US`, `vi-VN`) without restarting the application, cascading fallback resolution, and built-in enterprise dictionaries.
* **Standardized Editor & Binding Contracts (`IZeroEditor`, `ZeroDataBinder`):** Unified contract (`EditValue`, `IsModified`, `ReadOnly`, `ResetModified()`) enabling fluent two-way binding, dirty state tracking, and validation integration.

### 🏭 Industrial Telemetry, SCADA & Historian
* **Real-Time Tag Engine (`ZeroTagEngine` v2, `TagStorage`):** High-throughput in-memory tag registry with deadband jitter filtering, OPC DA/UA quality codes (`Good`, `Bad`, `Uncertain`), and multi-worker concurrent updates (>48M writes/s, >244M reads/s).
* **Embedded SQLite WAL Historian (`SqliteHistorianEngine`):** Ultra-fast time-series telemetry historian leveraging SQLite in WAL mode with daily rolling DB partitions and batch commit workers (>100k records/s).
* **Store & Forward Worker (`StoreAndForwardWorker`):** Resilient edge-to-cloud disk caching during network disconnections with auto-draining.
* **LTTB Decimation (`LttbDecimation`):** Zero-alloc Largest-Triangle-Three-Buckets algorithm compressing 10,000,000 raw points to 2,000 screen pixels in ~29 ms with 0 bytes GC allocated.
* **TimeSeries Continuous Rollups (`TimeSeriesPyramid`):** Multi-resolution continuous rollups (L0: raw, L1: 100ms, L2: 1s, L3: 10s, L4: 1min, L5: 10min) powering instant $O(\text{screen pixels})$ chart zoom.
* **ISA-18.2 Alarm Engine (`ScadaAlarmEngine`):** Thread-safe alarm lifecycle management (`ActiveUnack`, `ActiveAck`, `ClearedUnack`, `Normal`, `Shelved`, `Suppressed`) with audit trails.

### 🔌 Industrial Field Communication & Protocols
* **Modbus TCP Master (`ModbusTcpAdapter`):** Native zero-allocation client supporting FC 01, 02, 03, 04, 05, 06, 15, 16.
* **Siemens S7 Adapter (`SiemensS7Adapter`):** High-speed ISO-on-TCP (RFC 1006 / COTP) and S7 PDU client for Siemens S7-300, S7-400, S7-1200, and S7-1500 PLCs.
* **Modbus Address Planner (`ModbusAddressPlanner`):** Industrial register address coalescer grouping disjoint tags into optimized contiguous block reads (up to 98.3% network packet reduction).

### ⚙️ MES & Manufacturing Engines
* **PackML State Machine (`PackMlStateMachine`):** Complete ISA-TR88.00.02 packaging machine state machine modeling all 17 standard states.
* **OEE Engine (`OeeEngine`):** Real-time Overall Equipment Effectiveness calculator (Availability, Performance, Quality, scrap defect tracking).
* **Smart Warehouse Route Optimizer (`GuidedPickingEngine`):** Warehouse picking route optimization with 5-tier spatial coordinate routing (`WarehouseLocation`) and FIFO/FEFO priority enforcement.

---

## 📦 Installation

```powershell
dotnet add package ZeroUI.Core --version 1.2.0
```

---

## 🚀 Quick Example

```csharp
using ZeroUI.Core.Localization;
using ZeroUI.Core.Validation;

// 1. Dynamic Runtime Localization
ZeroLocalizer.SetCulture("vi-VN");
string saveText = ZeroLocalizer.GetString("Common.Save"); // "Lưu"

// 2. High-Performance Form Validation
var validator = new ValidationProvider();
validator.RuleFor("Quantity")
         .NotEmpty()
         .Range(1, 1000)
         .WithMessage("Quantity must be between 1 and 1,000 units.");

var result = validator.ValidateField("Quantity", 1500);
if (!result.IsValid)
{
    Console.WriteLine($"Validation Error: {result.ErrorMessage}");
}
```

Full documentation and source code: [github.com/kzxl/ZeroUI](https://github.com/kzxl/ZeroUI)
