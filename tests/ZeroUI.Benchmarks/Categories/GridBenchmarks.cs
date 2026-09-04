using System;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using ZeroUI.Core.Common;
using ZeroUI.Core.Data;
using ZeroUI.Core.Virtualization;

namespace ZeroUI.Benchmarks.Categories
{
    /// <summary>
    /// Category B: Grid Virtualization Benchmarks.
    /// Evaluates virtual data sources across 100K, 1M, 10M, and 100M virtual rows.
    /// Measures memory overhead, O(1) random cell access latency, and viewport slice retrieval.
    /// </summary>
    [MemoryDiagnoser]
    public class GridBenchmarks
    {
        private static readonly int[] RowCounts = { 100_000, 1_000_000, 10_000_000, 100_000_000 };

        public sealed class SyntheticVirtualSource : IZeroVirtualSource
        {
            private readonly int _rowCount;
            private readonly int _colCount;

            public SyntheticVirtualSource(int rowCount, int colCount = 10)
            {
                _rowCount = rowCount;
                _colCount = colCount;
            }

            public int TotalRowCount => _rowCount;
            public int TotalColumnCount => _colCount;

            public void GetCellValue(int rowIndex, int columnIndex, ref CellValueBuffer buffer)
            {
                // Zero-allocation O(1) procedural data generation
                buffer.Text = "VirtualData".AsSpan();
                buffer.Alignment = (columnIndex & 1) == 0 ? CellAlignment.Left : CellAlignment.Right;
                buffer.TextColor = 0x00E0E0E0U;
                buffer.BackColor = (rowIndex & 1) == 0 ? 0x001E1E1EU : 0x00252525U;
            }
        }

        [Benchmark]
        public void RandomAccess_100K() => BenchmarkRandomAccess(100_000);

        [Benchmark]
        public void RandomAccess_1M() => BenchmarkRandomAccess(1_000_000);

        [Benchmark]
        public void RandomAccess_10M() => BenchmarkRandomAccess(10_000_000);

        [Benchmark]
        public void RandomAccess_100M() => BenchmarkRandomAccess(100_000_000);

        private static void BenchmarkRandomAccess(int rowCount)
        {
            var source = new SyntheticVirtualSource(rowCount, 10);
            var buffer = new CellValueBuffer();
            const int sampleCount = 10_000;

            for (int i = 0; i < sampleCount; i++)
            {
                int r = (i * 7919) % rowCount;
                int c = i % 10;
                source.GetCellValue(r, c, ref buffer);
            }
        }

        public static void RunProfiler()
        {
            Console.WriteLine("----------------------------------------------------------------------------------");
            Console.WriteLine("B. Grid Virtualization Benchmarks (100K, 1M, 10M, 100M Virtual Rows)");
            Console.WriteLine("----------------------------------------------------------------------------------");

            const int viewportRows = 50;
            const int viewportCols = 10;
            const int totalViewportCells = viewportRows * viewportCols; // 500 cells

            foreach (var rowCount in RowCounts)
            {
                var source = new SyntheticVirtualSource(rowCount, viewportCols);

                // 1. Measure Memory Footprint of Virtual Source
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                long memBefore = GC.GetTotalMemory(true);
                var testSource = new SyntheticVirtualSource(rowCount, viewportCols);
                long memAfter = GC.GetTotalMemory(true);
                long sourceBytes = Math.Max(0, memAfter - memBefore);

                // 2. Viewport Slice Extraction (500 cells at 3 scroll positions: Top, Mid, Bottom)
                var buffer = new CellValueBuffer();
                int[] scrollOffsets = { 0, rowCount / 2, Math.Max(0, rowCount - viewportRows) };

                // Warmup
                foreach (var offset in scrollOffsets)
                {
                    for (int r = 0; r < viewportRows; r++)
                    {
                        for (int c = 0; c < viewportCols; c++)
                        {
                            source.GetCellValue(offset + r, c, ref buffer);
                        }
                    }
                }

                GC.Collect();
                long allocBefore = GC.GetAllocatedBytesForCurrentThread();
                int gen0Before = GC.CollectionCount(0);
                var sw = Stopwatch.StartNew();

                const int iterations = 1_000;
                for (int iter = 0; iter < iterations; iter++)
                {
                    int offset = scrollOffsets[iter % scrollOffsets.Length];
                    for (int r = 0; r < viewportRows; r++)
                    {
                        for (int c = 0; c < viewportCols; c++)
                        {
                            source.GetCellValue(offset + r, c, ref buffer);
                        }
                    }
                }

                sw.Stop();
                long allocAfter = GC.GetAllocatedBytesForCurrentThread();
                int gen0After = GC.CollectionCount(0);

                double avgSliceMs = sw.Elapsed.TotalMilliseconds / iterations;
                double avgSliceUs = avgSliceMs * 1000.0;
                double nsPerCell = (avgSliceUs * 1000.0) / totalViewportCells;
                double fpsPotential = 1000.0 / avgSliceMs;
                long bytesPerSlice = (allocAfter - allocBefore) / iterations;

                Console.WriteLine($"  • Rows: {rowCount,11:N0} | Viewport: {totalViewportCells} cells | Slice Latency: {avgSliceUs,6:F2} μs ({nsPerCell,5:F1} ns/cell) | Max FPS: {fpsPotential,8:N0} | RAM: {sourceBytes,3} B | Alloc: {bytesPerSlice} B");
            }

            Console.WriteLine();
        }
    }
}
