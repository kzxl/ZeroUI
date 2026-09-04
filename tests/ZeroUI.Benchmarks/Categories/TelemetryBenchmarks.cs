using System;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using ZeroUI.Core.Scada;

namespace ZeroUI.Benchmarks.Categories
{
    /// <summary>
    /// Category C: Telemetry Pipeline Benchmarks.
    /// Evaluates ingestion, coalescing, and lock-free triple buffer swaps at 1K, 10K, 100K, and 1M updates/sec.
    /// </summary>
    [MemoryDiagnoser]
    public class TelemetryBenchmarks
    {
        private static readonly int[] UpdateBatches = { 1_000, 10_000, 100_000, 1_000_000 };

        [Benchmark]
        public void Telemetry_1K() => BenchmarkIngestion(1_000);

        [Benchmark]
        public void Telemetry_10K() => BenchmarkIngestion(10_000);

        [Benchmark]
        public void Telemetry_100K() => BenchmarkIngestion(100_000);

        [Benchmark]
        public void Telemetry_1M() => BenchmarkIngestion(1_000_000);

        private static void BenchmarkIngestion(int count)
        {
            var buffer = new ZeroTripleBuffer(1024);
            long ts = 1700000000000;
            var scadaVal = new ScadaValue(100.0, ScadaQuality.Good);

            for (int i = 0; i < count; i++)
            {
                var writeBuf = buffer.GetWriteBuffer();
                writeBuf.Set(i % 1000, scadaVal, ts + i);
                buffer.PublishWrite();
            }
        }

        public static void RunProfiler()
        {
            Console.WriteLine("----------------------------------------------------------------------------------");
            Console.WriteLine("C. Telemetry Benchmarks (1K, 10K, 100K, 1M updates/sec - ZeroTripleBuffer & Queue)");
            Console.WriteLine("----------------------------------------------------------------------------------");

            foreach (var count in UpdateBatches)
            {
                var tripleBuffer = new ZeroTripleBuffer(1024);
                long baseTs = 1700000000000;
                var testVal = new ScadaValue(50.0, ScadaQuality.Good);

                // Warmup
                for (int i = 0; i < Math.Min(count, 10_000); i++)
                {
                    var wb = tripleBuffer.GetWriteBuffer();
                    wb.Set(i % 1000, testVal, baseTs + i);
                    tripleBuffer.PublishWrite();
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                long allocBefore = GC.GetAllocatedBytesForCurrentThread();
                int gen0Before = GC.CollectionCount(0);
                var sw = Stopwatch.StartNew();

                int iterations = count >= 1_000_000 ? 1 : count >= 100_000 ? 5 : 20;
                long totalPushed = 0;

                for (int iter = 0; iter < iterations; iter++)
                {
                    for (int i = 0; i < count; i++)
                    {
                        var writeBuf = tripleBuffer.GetWriteBuffer();
                        writeBuf.Set(i % 1000, testVal, baseTs + i);
                        tripleBuffer.PublishWrite();
                    }
                    totalPushed += count;
                }

                sw.Stop();
                long allocAfter = GC.GetAllocatedBytesForCurrentThread();
                int gen0After = GC.CollectionCount(0);

                double elapsedMs = sw.Elapsed.TotalMilliseconds;
                double updatesPerSec = (totalPushed / elapsedMs) * 1000.0;
                double nsPerUpdate = (elapsedMs / totalPushed) * 1_000_000.0;
                long allocPerOp = (allocAfter - allocBefore) / iterations;

                // Consumer read test
                var readBuf = tripleBuffer.AcquireRenderBuffer(out bool hasUpdate);
                var readVal = readBuf.GetValue(0);

                Console.WriteLine($"  • Ingest {count,9:N0} updates:  {elapsedMs / iterations,7:F2} ms | Throughput: {updatesPerSec,11:N0} up/s | Latency: {nsPerUpdate,5:F1} ns | Alloc: {allocPerOp,2} B | Gen0: {gen0After - gen0Before}");
            }

            Console.WriteLine();
        }
    }
}
