using System;
using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using ZeroUI.Core.Historian;
using ZeroUI.Core.Scada;

namespace ZeroUI.Benchmarks.Categories
{
    /// <summary>
    /// Category F: Historian Ingestion & Multi-Resolution Rollup Benchmarks.
    /// Evaluates continuous rollup aggregation and ingestion throughput across 1K/s, 10K/s, 100K/s, and 1M/s.
    /// </summary>
    [MemoryDiagnoser]
    public class HistorianBenchmarks
    {
        private static readonly int[] IngestScales = { 1_000, 10_000, 100_000, 1_000_000 };

        [Benchmark]
        public void Historian_1K() => BenchmarkIngestion(1_000);

        [Benchmark]
        public void Historian_10K() => BenchmarkIngestion(10_000);

        [Benchmark]
        public void Historian_100K() => BenchmarkIngestion(100_000);

        [Benchmark]
        public void Historian_1M() => BenchmarkIngestion(1_000_000);

        private static void BenchmarkIngestion(int count)
        {
            var buckets = new Dictionary<RollupKey, RollupBucket>(count / 10);
            long baseTime = 1700000000000;
            const string tagPath = "Plant.Boiler1.Pressure";

            for (int i = 0; i < count; i++)
            {
                long ts = baseTime + (i * 10); // 10ms raw ticks
                double val = Math.Sin(i * 0.05) * 50.0 + 100.0;

                // Continuous Rollup across 100ms and 1sec tiers
                long b100 = ts - (ts % 100);
                var k100 = new RollupKey(tagPath, TelemetryResolution.L1_100ms, b100);
                if (buckets.TryGetValue(k100, out var b))
                {
                    b.AddSample(val, ScadaQuality.Good);
                }
                else
                {
                    buckets[k100] = new RollupBucket(tagPath, TelemetryResolution.L1_100ms, b100, val, ScadaQuality.Good);
                }

                long b1s = ts - (ts % 1000);
                var k1s = new RollupKey(tagPath, TelemetryResolution.L2_1s, b1s);
                if (buckets.TryGetValue(k1s, out var b2))
                {
                    b2.AddSample(val, ScadaQuality.Good);
                }
                else
                {
                    buckets[k1s] = new RollupBucket(tagPath, TelemetryResolution.L2_1s, b1s, val, ScadaQuality.Good);
                }
            }
        }

        public static void RunProfiler()
        {
            Console.WriteLine("----------------------------------------------------------------------------------");
            Console.WriteLine("F. Historian Benchmarks (1K/s, 10K/s, 100K/s, 1M/s - Ingestion & Continuous Rollups)");
            Console.WriteLine("----------------------------------------------------------------------------------");

            const string tagPath = "Plant.Turbine1.Speed";
            long baseTime = 1700000000000;

            foreach (var count in IngestScales)
            {
                var buckets = new Dictionary<RollupKey, RollupBucket>(count / 10);

                // Warmup
                for (int i = 0; i < Math.Min(count, 10_000); i++)
                {
                    long ts = baseTime + (i * 10);
                    double val = 3000.0 + (i % 50);
                    long b100 = ts - (ts % 100);
                    var k = new RollupKey(tagPath, TelemetryResolution.L1_100ms, b100);
                    if (buckets.TryGetValue(k, out var b)) b.AddSample(val, ScadaQuality.Good);
                    else buckets[k] = new RollupBucket(tagPath, TelemetryResolution.L1_100ms, b100, val, ScadaQuality.Good);
                }

                buckets.Clear();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                long allocBefore = GC.GetAllocatedBytesForCurrentThread();
                int gen0Before = GC.CollectionCount(0);
                var sw = Stopwatch.StartNew();

                int iterations = count >= 1_000_000 ? 1 : count >= 100_000 ? 5 : 20;
                long totalIngested = 0;

                for (int iter = 0; iter < iterations; iter++)
                {
                    buckets.Clear();
                    for (int i = 0; i < count; i++)
                    {
                        long ts = baseTime + (i * 10);
                        double val = 3000.0 + Math.Sin(i * 0.01) * 200.0;

                        long b100 = ts - (ts % 100);
                        var k100 = new RollupKey(tagPath, TelemetryResolution.L1_100ms, b100);
                        if (buckets.TryGetValue(k100, out var b))
                        {
                            b.AddSample(val, ScadaQuality.Good);
                        }
                        else
                        {
                            buckets[k100] = new RollupBucket(tagPath, TelemetryResolution.L1_100ms, b100, val, ScadaQuality.Good);
                        }

                        long b1s = ts - (ts % 1000);
                        var k1s = new RollupKey(tagPath, TelemetryResolution.L2_1s, b1s);
                        if (buckets.TryGetValue(k1s, out var b2))
                        {
                            b2.AddSample(val, ScadaQuality.Good);
                        }
                        else
                        {
                            buckets[k1s] = new RollupBucket(tagPath, TelemetryResolution.L2_1s, b1s, val, ScadaQuality.Good);
                        }
                    }
                    totalIngested += count;
                }

                sw.Stop();
                long allocAfter = GC.GetAllocatedBytesForCurrentThread();
                int gen0After = GC.CollectionCount(0);

                double elapsedMs = sw.Elapsed.TotalMilliseconds;
                double recPerSec = (totalIngested / elapsedMs) * 1000.0;
                double usPerRec = (elapsedMs / totalIngested) * 1000.0;
                long allocPerOp = (allocAfter - allocBefore) / iterations;

                Console.WriteLine($"  • Ingest {count,9:N0} records:  {elapsedMs / iterations,7:F2} ms | Throughput: {recPerSec,11:N0} rec/s | Latency: {usPerRec,6:F3} μs | Buckets: {buckets.Count,6:N0} | Alloc: {allocPerOp,9:N0} B | Gen0: {gen0After - gen0Before}");
            }

            Console.WriteLine();
        }
    }
}
