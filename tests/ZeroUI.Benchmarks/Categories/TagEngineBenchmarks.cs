using System;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using ZeroUI.Core.Scada;

namespace ZeroUI.Benchmarks.Categories
{
    /// <summary>
    /// Category D: TagEngine Scalability Benchmarks.
    /// Evaluates flat unboxed tag storage and inverted index dispatch across 1 tag, 1K tags, 10K tags, and 100K tags.
    /// </summary>
    [MemoryDiagnoser]
    public class TagEngineBenchmarks
    {
        private static readonly int[] TagCounts = { 1, 1_000, 10_000, 100_000 };

        [Benchmark]
        public void TagEngine_1Tag() => BenchmarkTagOperations(1);

        [Benchmark]
        public void TagEngine_1KTags() => BenchmarkTagOperations(1_000);

        [Benchmark]
        public void TagEngine_10KTags() => BenchmarkTagOperations(10_000);

        [Benchmark]
        public void TagEngine_100KTags() => BenchmarkTagOperations(100_000);

        private static void BenchmarkTagOperations(int tagCount)
        {
            var storage = new TagStorage(tagCount + 64);
            long nowMs = 1700000000000;
            var scadaVal = new ScadaValue(100.0, ScadaQuality.Good);

            const int operations = 10_000;
            for (int i = 0; i < operations; i++)
            {
                int id = i % tagCount;
                storage.Set(id, scadaVal, nowMs);
                var val = storage.GetValue(id);
            }
        }

        public static void RunProfiler()
        {
            Console.WriteLine("----------------------------------------------------------------------------------");
            Console.WriteLine("D. TagEngine Benchmarks (1, 1K, 10K, 100K Tags - Flat Array Storage & Dispatch)");
            Console.WriteLine("----------------------------------------------------------------------------------");

            foreach (var tagCount in TagCounts)
            {
                var storage = new TagStorage(tagCount + 64);
                long nowMs = 1700000000000;
                var scadaVal = new ScadaValue(42.5, ScadaQuality.Good);

                // Pre-populate tags
                for (int i = 0; i < tagCount; i++)
                {
                    storage.Set(i, scadaVal, nowMs);
                }

                // 1. Benchmark O(1) Typed Write Latency
                const int totalWrites = 1_000_000;

                // Warmup
                for (int i = 0; i < Math.Min(totalWrites, 10_000); i++)
                {
                    storage.Set(i % tagCount, scadaVal, nowMs);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                long allocBefore = GC.GetAllocatedBytesForCurrentThread();
                int gen0Before = GC.CollectionCount(0);
                var sw = Stopwatch.StartNew();

                for (int i = 0; i < totalWrites; i++)
                {
                    storage.Set(i % tagCount, scadaVal, nowMs);
                }

                sw.Stop();
                long allocAfter = GC.GetAllocatedBytesForCurrentThread();
                int gen0After = GC.CollectionCount(0);

                double elapsedMs = sw.Elapsed.TotalMilliseconds;
                double writesPerSec = (totalWrites / elapsedMs) * 1000.0;
                double nsPerWrite = (elapsedMs / totalWrites) * 1_000_000.0;
                long allocPerOp = allocAfter - allocBefore;

                // 2. Benchmark O(1) Read Latency
                var readSw = Stopwatch.StartNew();
                double checksum = 0;
                for (int i = 0; i < totalWrites; i++)
                {
                    checksum += storage.GetValue(i % tagCount).DoubleVal;
                }
                readSw.Stop();
                double readElapsedMs = readSw.Elapsed.TotalMilliseconds;
                double readsPerSec = (totalWrites / readElapsedMs) * 1000.0;
                double nsPerRead = (readElapsedMs / totalWrites) * 1_000_000.0;

                Console.WriteLine($"  • Tags: {tagCount,7:N0} | Write: {nsPerWrite,5:F1} ns ({writesPerSec,11:N0} ops/s) | Read: {nsPerRead,5:F1} ns ({readsPerSec,11:N0} ops/s) | Alloc: {allocPerOp} B | Gen0: {gen0After - gen0Before}");
            }

            Console.WriteLine();
        }
    }
}
