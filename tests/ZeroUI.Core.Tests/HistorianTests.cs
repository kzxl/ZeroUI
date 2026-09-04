using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ZeroUI.Core.Data;
using ZeroUI.Core.Historian;
using ZeroUI.Core.Scada;

namespace ZeroUI.Core.Tests
{
    public class HistorianTests : IDisposable
    {
        private readonly string _testHistorianDir;

        public HistorianTests()
        {
            _testHistorianDir = Path.Combine(Path.GetTempPath(), "ZeroUI_Test_Historian_" + Guid.NewGuid().ToString("N"));
            if (!Directory.Exists(_testHistorianDir))
            {
                Directory.CreateDirectory(_testHistorianDir);
            }
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testHistorianDir))
                {
                    Directory.Delete(_testHistorianDir, recursive: true);
                }
            }
            catch
            {
                // Best effort cleanup
            }
        }

        [Fact]
        public void ZeroTagEngine_RingBufferHistory_RecordsPointsInRam()
        {
            string tagPath = "Plant.Boiler1.Temperature";
            ZeroTagEngine.EnableTagHistoryBuffer(tagPath, capacity: 16);

            for (int i = 1; i <= 20; i++)
            {
                ZeroTagEngine.SetTagValue(tagPath, (double)(i * 5));
            }

            var history = ZeroTagEngine.GetRecentHistory(tagPath);
            Assert.NotEmpty(history);
            Assert.True(history.Count <= 16);

            // Latest point should be 100
            Assert.Equal(100.0, history[history.Count - 1].Y);
        }

        [Fact]
        public async Task SqliteHistorianEngine_BatchFlushAndLttbQuery_Succeeds()
        {
            using var engine = new SqliteHistorianEngine(
                storageDirectory: _testHistorianDir,
                batchSize: 50,
                flushIntervalMs: 1000);

            string tagPath = "Plant.Line1.Pressure";
            var now = DateTime.UtcNow;

            // Log 200 telemetry points
            for (int i = 0; i < 200; i++)
            {
                var ptTime = now.AddSeconds(-200 + i);
                double val = Math.Sin(i * 0.1) * 100.0;
                engine.LogSample(tagPath, val, ScadaQuality.Good, ptTime);
            }

            // Flush to SQLite WAL
            await engine.FlushAsync();

            // Query decimated points (request max 20 points)
            var decimated = await engine.QueryDecimatedAsync(
                tagPath,
                startTime: now.AddMinutes(-5),
                endTime: now.AddMinutes(1),
                targetPoints: 20);

            Assert.NotEmpty(decimated);
            Assert.True(decimated.Count <= 20, $"Expected decimated count <= 20, got {decimated.Count}");
        }

        [Fact]
        public async Task SqliteHistorianEngine_StoreAndForward_SyncCycle_Works()
        {
            using var engine = new SqliteHistorianEngine(
                storageDirectory: _testHistorianDir,
                batchSize: 10);

            string tagPath = "Plant.Pump.FlowRate";
            var now = DateTime.UtcNow;

            for (int i = 0; i < 25; i++)
            {
                engine.LogSample(tagPath, i * 1.5, ScadaQuality.Good, now.AddSeconds(i));
            }

            await engine.FlushAsync();

            // Read unsynced batch of 10
            var unsynced = await engine.ReadUnsyncedBatchAsync(batchSize: 10);
            Assert.Equal(10, unsynced.Count);

            // Mark synced up to last ID in batch
            long lastId = unsynced[unsynced.Count - 1].Id;
            await engine.MarkSyncedAsync(lastId, now.Date);

            // Next batch should return next 10 items
            var nextBatch = await engine.ReadUnsyncedBatchAsync(batchSize: 10);
            Assert.Equal(10, nextBatch.Count);
            Assert.True(nextBatch[0].Id > lastId);
        }

        [Fact]
        public async Task StoreAndForwardWorker_AutomatedSyncLoop_Succeeds()
        {
            using var engine = new SqliteHistorianEngine(
                storageDirectory: _testHistorianDir,
                batchSize: 10);

            string tagPath = "Plant.Heater.Temp";
            var now = DateTime.UtcNow;

            for (int i = 0; i < 15; i++)
            {
                engine.LogSample(tagPath, 50.0 + i, ScadaQuality.Good, now);
            }

            await engine.FlushAsync();

            var receivedByCentral = new List<HistorianRecord>();

            using var worker = new StoreAndForwardWorker(
                localEngine: engine,
                connectivityCheck: ct => Task.FromResult(true), // Simulate online
                forwardHandler: (records, ct) =>
                {
                    receivedByCentral.AddRange(records);
                    return Task.FromResult(true); // Successfully ingested by central DB
                },
                batchSize: 10,
                pollInterval: TimeSpan.FromMilliseconds(50));

            worker.Start();

            // Wait briefly for sync loop to run
            int timeoutMs = 2000;
            while (worker.TotalSyncedCount < 15 && timeoutMs > 0)
            {
                await Task.Delay(50);
                timeoutMs -= 50;
            }

            Assert.Equal(15, worker.TotalSyncedCount);
            Assert.Equal(15, receivedByCentral.Count);
            Assert.True(worker.IsOnline);
        }

        [Fact]
        public async Task SqliteHistorianEngine_CheckpointAndStorageMetrics_ReportsAccurately()
        {
            using var engine = new SqliteHistorianEngine(
                storageDirectory: _testHistorianDir,
                batchSize: 50);

            var now = DateTime.UtcNow;

            for (int i = 0; i < 100; i++)
            {
                engine.LogSample("Plant.Tank.Level", 10.0 + i, ScadaQuality.Good, now.AddSeconds(i));
            }

            await engine.FlushAsync();

            var metricsBefore = engine.GetStorageMetrics(now.Date);
            Assert.Equal(100, metricsBefore.TotalRecords);
            Assert.True(metricsBefore.DatabaseSizeBytes > 0);

            // Execute explicit WAL truncate checkpoint
            var checkpointDuration = await engine.CheckpointAsync(now.Date, SqliteCheckpointMode.Truncate);
            Assert.True(checkpointDuration >= TimeSpan.Zero);

            var metricsAfter = engine.GetStorageMetrics(now.Date);
            Assert.Equal(100, metricsAfter.TotalRecords);
            Assert.Equal(0, metricsAfter.WalSizeBytes); // WAL truncated to 0
        }
    }
}
