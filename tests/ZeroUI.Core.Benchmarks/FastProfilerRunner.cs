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
            ProfileTagEngine();
            ProfileAlarmEngine();

            Console.WriteLine();
            Console.WriteLine("==================================================================================");
            Console.WriteLine("✅ All Core Performance Profiles Completed Successfully.");
            Console.WriteLine("==================================================================================");
        }

        private static void ProfileLttb()
        {
            Console.WriteLine("----------------------------------------------------------------------------------");
            Console.WriteLine("1. LTTB Time-Series Decimation (Zero-Allocation Verification)");
            Console.WriteLine("----------------------------------------------------------------------------------");

            int[] testSizes = { 10_000, 50_000, 100_000, 500_000 };
            var destBuffer = new TimePoint[1000];

            foreach (var size in testSizes)
            {
                var src = new TimePoint[size];
                for (int i = 0; i < size; i++)
                {
                    src[i] = new TimePoint(i, Math.Sin(i * 0.02) * 50.0 + (i % 5));
                }

                // Warmup
                LttbDecimation.Downsample(src, destBuffer, 1000);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                long allocBefore = GC.GetAllocatedBytesForCurrentThread();
                int gen0Before = GC.CollectionCount(0);
                var sw = Stopwatch.StartNew();

                const int iterations = 10;
                for (int iter = 0; iter < iterations; iter++)
                {
                    LttbDecimation.Downsample(src, destBuffer, 1000);
                }

                sw.Stop();
                long allocAfter = GC.GetAllocatedBytesForCurrentThread();
                int gen0After = GC.CollectionCount(0);

                double avgMs = sw.Elapsed.TotalMilliseconds / iterations;
                long bytesPerOp = (allocAfter - allocBefore) / iterations;
                int gen0Diff = gen0After - gen0Before;

                double throughputKptsPerSec = (size / avgMs);

                Console.WriteLine($"  Input: {size,8:N0} pts -> Output: 1,000 pts | Latency: {avgMs,6:F3} ms | Throughput: {throughputKptsPerSec,8:N0} kpts/s | Alloc: {bytesPerOp,3} B | Gen0: {gen0Diff}");
            }
            Console.WriteLine();
        }

        private static void ProfileTagEngine()
        {
            Console.WriteLine("----------------------------------------------------------------------------------");
            Console.WriteLine("2. SCADA ZeroTagEngine Throughput & Deadband Filtering");
            Console.WriteLine("----------------------------------------------------------------------------------");

            const int totalOps = 100_000;

            // Test 2.1: Single Thread Ingestion
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

            // Test 2.2: Deadband Suppression Rate
            {
                ZeroTagEngine.SetDeadband("Plant.NoisySensor", 5.0);
                ZeroTagEngine.SetTagValue("Plant.NoisySensor", 100.0);

                var sw = Stopwatch.StartNew();
                int published = 0;
                for (int i = 0; i < totalOps; i++)
                {
                    // Jitter 0.5 < deadband 5.0
                    if (ZeroTagEngine.SetTagValue("Plant.NoisySensor", 100.0 + (i % 4) * 0.1, ScadaQuality.Good))
                    {
                        published++;
                    }
                }
                sw.Stop();
                double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;

                Console.WriteLine($"  Deadband Filter (100k jitter suppressed) | Time: {sw.Elapsed.TotalMilliseconds,6:F1} ms | Throughput: {opsPerSec,10:N0} ops/s | Passed: {published}/{totalOps}");
            }

            // Test 2.3: Multi-Threaded Concurrent Ingestion (4 Workers)
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
            Console.WriteLine("3. ISA-18.2 ScadaAlarmEngine Stress Profiler");
            Console.WriteLine("----------------------------------------------------------------------------------");

            const int alarmStormCount = 10_000;

            // Test 3.1: Alarm Storm (10,000 Alarms raised)
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

            // Test 3.2: Severity Summary Aggregation
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

            // Test 3.3: Mass Acknowledgment
            {
                var sw = Stopwatch.StartNew();
                int ackCount = ScadaAlarmEngine.AcknowledgeAll("ChiefSafetyOfficer");
                sw.Stop();

                Console.WriteLine($"  Mass AcknowledgeAll ({ackCount:N0} alarms)      | Time: {sw.Elapsed.TotalMilliseconds,6:F1} ms");
            }
            Console.WriteLine();
        }
    }
}
