using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using ZeroUI.Core.Communication;

namespace ZeroUI.Benchmarks.Categories
{
    /// <summary>
    /// Category E: Modbus Protocol & Block Optimization Benchmarks.
    /// Evaluates address planning coalescing and zero-alloc packet encoding across 10, 100, and 1K tags.
    /// </summary>
    [MemoryDiagnoser]
    public class ModbusBenchmarks
    {
        private static readonly int[] TagScales = { 10, 100, 1_000 };

        [Benchmark]
        public void Modbus_10Tags() => BenchmarkPlanning(10);

        [Benchmark]
        public void Modbus_100Tags() => BenchmarkPlanning(100);

        [Benchmark]
        public void Modbus_1KTags() => BenchmarkPlanning(1_000);

        private static void BenchmarkPlanning(int tagCount)
        {
            var tags = GenerateTags(tagCount);
            var blocks = ModbusAddressPlanner.PlanBlocks(tags);
            EncodeBlocks(blocks);
        }

        private static List<AdapterTagDefinition> GenerateTags(int count)
        {
            var list = new List<AdapterTagDefinition>(count);
            for (int i = 0; i < count; i++)
            {
                // Clustered addresses with realistic register gaps
                ushort addr = (ushort)(1000 + (i * 2) + (i % 7 == 0 ? 4 : 0));
                list.Add(new AdapterTagDefinition($"Modbus.Tag_{i}", addr.ToString(), TagDataType.Float32));
            }
            return list;
        }

        private static void EncodeBlocks(IReadOnlyList<ModbusReadBlock> blocks)
        {
            var pool = ArrayPool<byte>.Shared;
            byte[] buffer = pool.Rent(260);
            try
            {
                for (int i = 0; i < blocks.Count; i++)
                {
                    var block = blocks[i];
                    BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(0, 2), (ushort)i);
                    BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2, 2), 0); // Protocol ID
                    BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(4, 2), 6); // Length
                    buffer[6] = 1; // Unit ID
                    buffer[7] = block.FunctionCode;
                    BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(8, 2), block.StartAddress);
                    BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(10, 2), block.RegisterCount);
                }
            }
            finally
            {
                pool.Return(buffer);
            }
        }

        public static void RunProfiler()
        {
            Console.WriteLine("----------------------------------------------------------------------------------");
            Console.WriteLine("E. Modbus Benchmarks (10, 100, 1K Tags - Address Planning & Buffer Pooling)");
            Console.WriteLine("----------------------------------------------------------------------------------");

            foreach (var count in TagScales)
            {
                var tags = GenerateTags(count);

                // Warmup
                var blocks = ModbusAddressPlanner.PlanBlocks(tags);
                EncodeBlocks(blocks);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                long allocBefore = GC.GetAllocatedBytesForCurrentThread();
                int gen0Before = GC.CollectionCount(0);
                var sw = Stopwatch.StartNew();

                const int iterations = 500;
                for (int iter = 0; iter < iterations; iter++)
                {
                    var planned = ModbusAddressPlanner.PlanBlocks(tags);
                    EncodeBlocks(planned);
                }

                sw.Stop();
                long allocAfter = GC.GetAllocatedBytesForCurrentThread();
                int gen0After = GC.CollectionCount(0);

                double avgMs = sw.Elapsed.TotalMilliseconds / iterations;
                double avgUs = avgMs * 1000.0;
                double reduction = (1.0 - ((double)blocks.Count / count)) * 100.0;
                long allocPerOp = (allocAfter - allocBefore) / iterations;

                Console.WriteLine($"  • Tags: {count,5:N0} -> Coalesced to {blocks.Count,3} blocks ({reduction,5:F1}% request reduction) | Plan + Encode: {avgUs,6:F2} μs | Alloc: {allocPerOp,4} B | Gen0: {gen0After - gen0Before}");
            }

            Console.WriteLine();
        }
    }
}
