# ZeroUI.Core ⚡

Ultra-high-performance, zero-allocation core runtime and industrial automation infrastructure for .NET (`netstandard2.0`, `net8.0`).

[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/kzxl/ZeroUI)
[![GitHub](https://img.shields.io/badge/GitHub-kzxl%2FZeroUI-blue.svg)](https://github.com/kzxl/ZeroUI)

---

## 🌟 Key Features

* **Real-Time Tag Engine (`ZeroTagEngine`):** High-throughput in-memory tag registry with deadband jitter filtering, OPC DA/UA quality codes (`Good`, `Bad`, `Uncertain`), and multi-worker concurrent updates (>3.6M ops/sec).
* **Embedded SQLite WAL Historian (`SqliteHistorianEngine`):** Ultra-fast time-series telemetry historian leveraging SQLite in WAL mode with daily rolling DB partitions and batch commit workers (>100k records/s).
* **Store & Forward Worker (`StoreAndForwardWorker`):** Resilient edge-to-cloud disk caching during network disconnections with auto-draining.
* **Industrial Field Protocols (`ZeroUI.Core.Communication`):**
  * **Modbus TCP Master:** Native zero-allocation client supporting FC 01, 02, 03, 04, 05, 06, 15, 16.
  * **Siemens S7 Adapter:** High-speed ISO-on-TCP (RFC 1006 / COTP) and S7 PDU client for Siemens S7-300, S7-400, S7-1200, and S7-1500 PLCs.
* **Manufacturing & MES Engines (`ZeroUI.Core.Mes`):**
  * **PackML State Machine (`PackMlStateMachine`):** Complete ISA-TR88.00.02 packaging machine state machine modeling all 17 standard states.
  * **OEE Engine (`OeeEngine`):** Real-time Overall Equipment Effectiveness calculator (Availability, Performance, Quality, scrap defect tracking).
* **Smart Warehouse Route Optimizer (`GuidedPickingEngine`):** Warehouse picking route optimization with 5-tier spatial coordinate routing (`WarehouseLocation`) and FIFO/FEFO priority enforcement.
* **Decoupled UI Runtime (`ZeroUI.Core.Runtime`):**
  * `UiDispatcher`: Frame rate throttling (30–120 FPS) and batch coalescing to prevent UI thread starvation.
  * `WorkerQueue<T>`: Lock-free ring-buffered worker queue for background processing.
  * `EventBus` & `CommandBus`: Low-latency, zero-boxing decoupled messaging.
  * `LttbDecimation`: Zero-alloc downsampling compressing 100k+ points to 1,000 screen pixels in <3 ms.

---

## 📦 Installation

```powershell
dotnet add package ZeroUI.Core
```

---

## 🚀 Quick Example

```csharp
using ZeroUI.Core.Historian;
using ZeroUI.Core.Scada;

// Initialize in-memory tag engine
var tagEngine = new ZeroTagEngine();
tagEngine.RegisterTag("Line1.Reactor.Temperature", deadband: 0.2);

// Initialize embedded SQLite WAL Historian
using var historian = new SqliteHistorianEngine("data/historian");
await historian.InitializeAsync();

// Ingest telemetry with 0 GC allocations
await historian.AppendAsync(new HistorianRecord(
    timestamp: DateTime.UtcNow,
    tag: "Line1.Reactor.Temperature",
    value: 85.4,
    quality: ScadaQuality.Good
));
```

Full documentation and source code: [github.com/kzxl/ZeroUI](https://github.com/kzxl/ZeroUI)
