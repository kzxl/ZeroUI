using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ZeroUI.Core.Data;
using ZeroUI.Core.Scada;

namespace ZeroUI.Core.Benchmarks
{
    public static class FastProfilerRunner
    {
        public static void RunAll()
        {
            Console.WriteLine("==================================================================================");
            Console.WriteLine("⚡ ZeroUI.Core Performance & Zero-GC Profiler");
            Console.WriteLine($"Environment: .NET {Environment.Version} | OS: {Environment.OSVersion} | Cores: {Environment.ProcessorCount}");
            Console.WriteLine("==================================================================================");
            Console.WriteLine();

            ProfileLttb();
            ProfilePyramid();
            ProfileTagEngine();
            ProfileAlarmEngine();
            ProfileMultiResolutionHistorian();
            HistorianMultiDimensionalBenchmark.RunAsync().GetAwaiter().GetResult();

            Console.WriteLine();
            Console.WriteLine("==================================================================================");
            Console.WriteLine("✅ All Core Performance Profiles Completed Successfully.");
            Console.WriteLine("==================================================================================");
        }

        private static void ProfileLttb()
        {
            Console.WriteLine("----------------------------------------------------------------------------------");
            Console.WriteLine("1. LTTB Time-Series Decimation (10k .. 10M Points - Zero GC)");
            Console.WriteLine("----------------------------------------------------------------------------------");

            (int Size, int Target)[] testCases =
            {
                (10_000, 1_000),
                (50_000, 1_000),
                (100_000, 1_000),
                (500_000, 1_000),
                (1_000_000, 2_000),
                (10_000_000, 2_000)
            };

            foreach (var (size, target) in testCases)
            {
                var src = new TimePoint[size];
                for (int i = 0; i < size; i++)
                {
                    src[i] = new TimePoint(i, Math.Sin(i * 0.02) * 50.0 + (i % 5));
                }

                var destBuffer = new TimePoint[target];

                // Warmup
                LttbDecimation.Downsample(src.AsSpan(), destBuffer.AsSpan(), target);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                long allocBefore = GC.GetAllocatedBytesForCurrentThread();
                int gen0Before = GC.CollectionCount(0);
                var sw = Stopwatch.StartNew();

                int iterations = size >= 10_000_000 ? 1 : size >= 1_000_000 ? 3 : 10;
                for (int iter = 0; iter < iterations; iter++)
                {
                    LttbDecimation.Downsample(src.AsSpan(), destBuffer.AsSpan(), target);
                }

                sw.Stop();
                long allocAfter = GC.GetAllocatedBytesForCurrentThread();
                int gen0After = GC.CollectionCount(0);

                double avgMs = sw.Elapsed.TotalMilliseconds / iterations;
                long bytesPerOp = (allocAfter - allocBefore) / iterations;
                int gen0Diff = gen0After - gen0Before;

                double throughputKptsPerSec = (size / avgMs);

                Console.WriteLine($"  Input: {size,10:N0} pts -> Output: {target,5:N0} pts | Latency: {avgMs,7:F2} ms | Throughput: {throughputKptsPerSec,8:N0} kpts/s | Alloc: {bytesPerOp,3} B | Gen0: {gen0Diff}");
            }
            Console.WriteLine();
        }

        private static void ProfilePyramid()
        {
            Console.WriteLine("----------------------------------------------------------------------------------");
            Console.WriteLine("2. Multi-Resolution TimeSeriesPyramid vs. Raw Recomputation (10,000,000 pts)");
            Console.WriteLine("----------------------------------------------------------------------------------");

            const int totalPoints = 10_000_000;
            const int targetPoints = 2_000;
            var destBuffer = new TimePoint[targetPoints];

            Console.WriteLine($"  Building TimeSeriesPyramid with {totalPoints:N0} points (8x hierarchy)...");
            var pyramid = new TimeSeriesPyramid(totalPoints);
            var swBuild = Stopwatch.StartNew();

            // Append in chunks
            const int chunkSize = 100_000;
            var chunk = new TimePoint[chunkSize];
            for (int b = 0; b < totalPoints / chunkSize; b++)
            {
                int baseIdx = b * chunkSize;
                for (int i = 0; i < chunkSize; i++)
                {
                    chunk[i] = new TimePoint(baseIdx + i, Math.Sin((baseIdx + i) * 0.001) * 100.0);
                }
                pyramid.AppendBatch(chunk.AsSpan());
            }
            swBuild.Stop();
            Console.WriteLine($"  Pyramid Ingestion Completed in {swBuild.Elapsed.TotalMilliseconds:F1} ms ({totalPoints / swBuild.Elapsed.TotalSeconds:N0} pts/s, amortized O(1)).");
            Console.WriteLine();

            (string ZoomName, double MinX, double MaxX)[] zoomLevels =
            {
                ("100% Zoom (Full 10M pts)", 0, totalPoints),
                (" 10% Zoom (1,000,000 pts)", totalPoints * 0.45, totalPoints * 0.55),
                ("  1% Zoom (  100,000 pts)", totalPoints * 0.495, totalPoints * 0.505),
                ("0.1% Zoom (   10,000 pts)", totalPoints * 0.4995, totalPoints * 0.5005)
            };

            Console.WriteLine("  Interactive Chart Query & Decimation Performance:");
            foreach (var (zoomName, minX, maxX) in zoomLevels)
            {
                var sw = Stopwatch.StartNew();
                int written = pyramid.QueryRange(minX, maxX, destBuffer.AsSpan(), targetPoints);
                sw.Stop();

                Console.WriteLine($"  [{zoomName}] -> {written:N0} pts | Pyramid Query: {sw.Elapsed.TotalMilliseconds,6:F3} ms (Instantaneous / 144 Hz Ready)");
            }
            Console.WriteLine();
        }

        private static void ProfileTagEngine()
        {
            Console.WriteLine("----------------------------------------------------------------------------------");
            Console.WriteLine("3. SCADA ZeroTagEngine Throughput & Deadband Filtering");
            Console.WriteLine("----------------------------------------------------------------------------------");

            const int totalOps = 100_000;

            // Test 3.1: Single Thread Ingestion
            {
                GC.Collect();
                long allocBefore = GC.GetAllocatedBytesForCurrentThread();
                var sw = Stopwatch.StartNew();

                for (int i = 0; i < totalOps; i++)
                {
                    ZeroTagEngine.SetTagValue("Plant.Boiler.Temperature", 180.0 + (i % 50), ScadaQuality.Good);
                }

                sw.Stop();
                long allocAfter = GC.GetAllocatedBytesForCurrentThread();
                double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
                long allocPerOp = (allocAfter - allocBefore) / totalOps;

                Console.WriteLine($"  Single-Thread SetTagValue (100k updates)  | Time: {sw.Elapsed.TotalMilliseconds,6:F1} ms | Throughput: {opsPerSec,10:N0} ops/s | Alloc: {allocPerOp,3} B/op");
            }

            // Test 3.2: Deadband Suppression Rate
            {
                ZeroTagEngine.SetDeadband("Plant.NoisySensor", 5.0);
                ZeroTagEngine.SetTagValue("Plant.NoisySensor", 100.0);

                var sw = Stopwatch.StartNew();
                int published = 0;
                for (int i = 0; i < totalOps; i++)
                {
                    if (ZeroTagEngine.SetTagValue("Plant.NoisySensor", 100.0 + (i % 4) * 0.1, ScadaQuality.Good))
                    {
                        published++;
                    }
                }
                sw.Stop();
                double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;

                Console.WriteLine($"  Deadband Filter (100k jitter suppressed) | Time: {sw.Elapsed.TotalMilliseconds,6:F1} ms | Throughput: {opsPerSec,10:N0} ops/s | Passed: {published}/{totalOps}");
            }

            // Test 3.3: Multi-Threaded Concurrent Ingestion (4 Workers)
            {
                const int workers = 4;
                const int opsPerWorker = 25_000;

                var sw = Stopwatch.StartNew();
                Parallel.For(0, workers, w =>
                {
                    string tag = $"Plant.Line{w}.Pressure";
                    for (int i = 0; i < opsPerWorker; i++)
                    {
                        ZeroTagEngine.SetTagValue(tag, i * 0.5, ScadaQuality.Good);
                    }
                });
                sw.Stop();
                double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;

                Console.WriteLine($"  Multi-Threaded (4 Workers x 25k parallel) | Time: {sw.Elapsed.TotalMilliseconds,6:F1} ms | Throughput: {opsPerSec,10:N0} ops/s");
            }
            Console.WriteLine();
        }

        private static void ProfileAlarmEngine()
        {
            Console.WriteLine("----------------------------------------------------------------------------------");
            Console.WriteLine("4. ISA-18.2 ScadaAlarmEngine Stress Profiler");
            Console.WriteLine("----------------------------------------------------------------------------------");

            const int alarmStormCount = 10_000;

            // Test 4.1: Alarm Storm (10,000 Alarms raised)
            {
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < alarmStormCount; i++)
                {
                    ScadaAlarmEngine.RaiseAlarm(
                        $"ALM_STORM_{i}",
                        $"Line1.Breaker{i % 100}",
                        $"Trip condition on circuit breaker {i}",
                        (ScadaAlarmSeverity)(i % 5),
                        i * 1.5);
                }
                sw.Stop();
                double opsPerSec = alarmStormCount / sw.Elapsed.TotalSeconds;

                Console.WriteLine($"  Alarm Storm (10,000 Alarms Raised)        | Time: {sw.Elapsed.TotalMilliseconds,6:F1} ms | Rate: {opsPerSec,10:N0} alarms/s");
            }

            // Test 4.2: Severity Summary Aggregation
            {
                var sw = Stopwatch.StartNew();
                const int iters = 1000;
                AlarmSeverityCount summary = default;
                for (int i = 0; i < iters; i++)
                {
                    summary = ScadaAlarmEngine.GetAlarmSummary();
                }
                sw.Stop();
                double avgUs = (sw.Elapsed.TotalMilliseconds / iters) * 1000.0;

                Console.WriteLine($"  Alarm Summary Tally (Over 10k alarms)     | Latency: {avgUs,6:F1} μs | Total Active Alarms: {summary.TotalActive:N0}");
            }

            // Test 4.3: Mass Acknowledgment
            {
                var sw = Stopwatch.StartNew();
                int ackCount = ScadaAlarmEngine.AcknowledgeAll("ChiefSafetyOfficer");
                sw.Stop();

                Console.WriteLine($"  Mass AcknowledgeAll ({ackCount:N0} alarms)      | Time: {sw.Elapsed.TotalMilliseconds,6:F1} ms");
            }
            Console.WriteLine();
        }

        private static void ProfileMultiResolutionHistorian()
        {
            Console.WriteLine("----------------------------------------------------------------------------------");
            Console.WriteLine("5. Multi-Resolution Telemetry Storage (O(screen pixels) vs O(raw history))");
            Console.WriteLine("----------------------------------------------------------------------------------");

            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ZeroUI_MRes_Bench_" + Guid.NewGuid().ToString("N"));
            if (!System.IO.Directory.Exists(tempDir))
            {
                System.IO.Directory.CreateDirectory(tempDir);
            }

            try
            {
                using var engine = new ZeroUI.Core.Historian.SqliteHistorianEngine(
                    storageDirectory: tempDir,
                    batchSize: 1000,
                    flushIntervalMs: 1000);

                string tagPath = "Turbine.PowerOutput.MegaWatts";
                var baseTime = new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc);
                const int sampleCount = 100_000;

                // 100,000 samples across 1 hour (36ms per sample = ~28 Hz industrial rate)
                Console.WriteLine($"  Ingesting {sampleCount:N0} raw telemetry points (1 hour @ 28 Hz) with continuous rollups...");
                var swIngest = Stopwatch.StartNew();

                for (int i = 0; i < sampleCount; i++)
                {
                    var ts = baseTime.AddMilliseconds(i * 36);
                    double val = 450.0 + Math.Sin(i * 0.05) * 30.0;
                    if (i == 50000) val = 999.9; // Transient surge spike

                    engine.LogSample(tagPath, val, ScadaQuality.Good, ts);
                }

                engine.FlushAsync().GetAwaiter().GetResult();
                swIngest.Stop();
                Console.WriteLine($"  Ingestion & continuous rollup completed in {swIngest.Elapsed.TotalMilliseconds:F1} ms ({sampleCount / swIngest.Elapsed.TotalSeconds:N0} rec/s).");
                Console.WriteLine();

                var startTime = baseTime;
                var endTime = baseTime.AddHours(1);
                const int targetPoints = 1000;

                // Warmup
                _ = engine.QueryDecimatedAsync(tagPath, startTime, endTime, targetPoints, ZeroUI.Core.Historian.TelemetryResolution.Raw).GetAwaiter().GetResult();
                _ = engine.QueryDecimatedAsync(tagPath, startTime, endTime, targetPoints).GetAwaiter().GetResult();

                // Query 1: Raw scan (O(raw history) - 100k rows)
                var swRaw = Stopwatch.StartNew();
                var rawResult = engine.QueryDecimatedAsync(tagPath, startTime, endTime, targetPoints, ZeroUI.Core.Historian.TelemetryResolution.Raw).GetAwaiter().GetResult();
                swRaw.Stop();

                // Query 2: Multi-resolution rollups (O(screen pixels) - Level 2 1s: ~3,600 buckets)
                var swRollup = Stopwatch.StartNew();
                var rollupResult = engine.QueryDecimatedAsync(tagPath, startTime, endTime, targetPoints).GetAwaiter().GetResult();
                swRollup.Stop();

                double speedup = swRaw.Elapsed.TotalMilliseconds / Math.Max(0.001, swRollup.Elapsed.TotalMilliseconds);

                Console.WriteLine($"  Query Performance Comparison (Range: 1.0 hour, Target: {targetPoints} pts):");
                Console.WriteLine($"  • Raw Scan + LTTB [O(N = 100k)]:      {swRaw.Elapsed.TotalMilliseconds,6:F2} ms ({rawResult.Count} pts returned)");
                Console.WriteLine($"  • Multi-Res Rollup [O(pixels = 3.6k)]: {swRollup.Elapsed.TotalMilliseconds,6:F2} ms ({rollupResult.Count} pts returned)");
                Console.WriteLine($"  • Speedup Factor:                     {speedup:F1}x faster range query!");

                // Check peak preservation
                bool peakFound = false;
                for (int i = 0; i < rollupResult.Count; i++)
                {
                    if (rollupResult[i].Y >= 990.0) peakFound = true;
                }
                Console.WriteLine($"  • Waveform Fidelity Check:            Critical Surge Peak (999.9 MW) Preserved: {(peakFound ? "PASS" : "FAIL")}");
                Console.WriteLine();
            }
            finally
            {
                try
                {
                    if (System.IO.Directory.Exists(tempDir))
                    {
                        System.IO.Directory.Delete(tempDir, recursive: true);
                    }
                }
                catch { }
            }
        }
    }
}
