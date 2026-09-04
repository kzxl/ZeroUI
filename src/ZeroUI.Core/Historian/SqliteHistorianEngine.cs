using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using ZeroUI.Core.Data;
using ZeroUI.Core.Scada;

namespace ZeroUI.Core.Historian
{
    /// <summary>
    /// High-performance embedded time-series historian leveraging SQLite with Write-Ahead Logging (WAL).
    /// Features rolling daily database partitions, asynchronous batch commits, LTTB decimation queries,
    /// and Store-and-Forward synchronization.
    /// </summary>
    public sealed class SqliteHistorianEngine : IHistorianEngine
    {
        private readonly string _storageDirectory;
        private readonly int _batchSize;
        private readonly int _flushIntervalMs;
        private readonly ConcurrentQueue<HistorianRecord> _ingestionQueue = new ConcurrentQueue<HistorianRecord>();
        private readonly Timer _flushTimer;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of SqliteHistorianEngine.
        /// </summary>
        /// <param name="storageDirectory">Folder path to store daily SQLite databases.</param>
        /// <param name="batchSize">Number of records per transactional batch flush.</param>
        /// <param name="flushIntervalMs">Maximum duration in ms before flushing pending buffer.</param>
        public SqliteHistorianEngine(
            string? storageDirectory = null,
            int batchSize = 1000,
            int flushIntervalMs = 500)
        {
            _storageDirectory = storageDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Historian");
            _batchSize = Math.Max(10, batchSize);
            _flushIntervalMs = Math.Max(50, flushIntervalMs);

            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
            }

            _flushTimer = new Timer(_ => _ = FlushAsync(_cts.Token), null, _flushIntervalMs, _flushIntervalMs);
        }

        /// <inheritdoc />
        public void LogSample(string tagPath, double value, ScadaQuality quality = ScadaQuality.Good, DateTime? timestamp = null)
        {
            if (string.IsNullOrWhiteSpace(tagPath) || _isDisposed) return;
            var time = timestamp ?? DateTime.UtcNow;
            _ingestionQueue.Enqueue(new HistorianRecord(tagPath, value, quality, time));

            if (_ingestionQueue.Count >= _batchSize)
            {
                _ = FlushAsync(_cts.Token);
            }
        }

        /// <inheritdoc />
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_ingestionQueue.IsEmpty || _isDisposed) return;

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_ingestionQueue.IsEmpty) return;

                // Group records by partition date
                var batchesByDate = new Dictionary<DateTime, List<HistorianRecord>>();
                while (_ingestionQueue.TryDequeue(out var record))
                {
                    var dateKey = record.Timestamp.Date;
                    if (!batchesByDate.TryGetValue(dateKey, out var list))
                    {
                        list = new List<HistorianRecord>(_batchSize);
                        batchesByDate[dateKey] = list;
                    }
                    list.Add(record);
                }

                foreach (var kvp in batchesByDate)
                {
                    await WriteBatchToPartitionAsync(kvp.Key, kvp.Value, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private async Task WriteBatchToPartitionAsync(DateTime date, List<HistorianRecord> records, CancellationToken ct)
        {
            var dbPath = GetPartitionPath(date);
            var connStr = $"Data Source={dbPath};";

            using var connection = new SqliteConnection(connStr);
            await connection.OpenAsync(ct).ConfigureAwait(false);
            InitializeDatabasePragmas(connection);

            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT INTO TagHistory (TagPath, Val, Quality, Timestamp, IsSynced)
                VALUES ($path, $val, $quality, $ts, 0);";

            var pPath = command.CreateParameter();
            pPath.ParameterName = "$path";
            command.Parameters.Add(pPath);

            var pVal = command.CreateParameter();
            pVal.ParameterName = "$val";
            command.Parameters.Add(pVal);

            var pQuality = command.CreateParameter();
            pQuality.ParameterName = "$quality";
            command.Parameters.Add(pQuality);

            var pTs = command.CreateParameter();
            pTs.ParameterName = "$ts";
            command.Parameters.Add(pTs);

            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                pPath.Value = r.TagPath;
                pVal.Value = r.Value;
                pQuality.Value = (int)r.Quality;
                pTs.Value = new DateTimeOffset(r.Timestamp).ToUnixTimeMilliseconds();

                await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            transaction.Commit();
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<TimePoint>> QueryDecimatedAsync(
            string tagPath,
            DateTime startTime,
            DateTime endTime,
            int targetPoints = 1000,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tagPath) || startTime >= endTime)
                return Array.Empty<TimePoint>();

            var rawPoints = new List<TimePoint>();
            long startMs = new DateTimeOffset(startTime).ToUnixTimeMilliseconds();
            long endMs = new DateTimeOffset(endTime).ToUnixTimeMilliseconds();

            // Iterate across partitions spanning the requested date range
            for (var curDate = startTime.Date; curDate <= endTime.Date; curDate = curDate.AddDays(1))
            {
                var dbPath = GetPartitionPath(curDate);
                if (!File.Exists(dbPath)) continue;

                var connStr = $"Data Source={dbPath};Mode=ReadOnly;";
                using var connection = new SqliteConnection(connStr);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT Timestamp, Val
                    FROM TagHistory
                    WHERE TagPath = $path AND Timestamp >= $start AND Timestamp <= $end
                    ORDER BY Timestamp ASC;";

                cmd.Parameters.AddWithValue("$path", tagPath);
                cmd.Parameters.AddWithValue("$start", startMs);
                cmd.Parameters.AddWithValue("$end", endMs);

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    double x = reader.GetInt64(0);
                    double y = reader.GetDouble(1);
                    rawPoints.Add(new TimePoint(x, y));
                }
            }

            if (rawPoints.Count == 0) return Array.Empty<TimePoint>();
            if (rawPoints.Count <= targetPoints) return rawPoints;

            // Decimate using LTTB algorithm for sub-millisecond downsampling
            var decimated = new TimePoint[targetPoints];
            int written = LttbDecimation.Downsample(rawPoints, decimated, targetPoints);

            var result = new TimePoint[written];
            Array.Copy(decimated, result, written);
            return result;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<HistorianRecord>> ReadUnsyncedBatchAsync(int batchSize = 1000, CancellationToken cancellationToken = default)
        {
            var results = new List<HistorianRecord>();
            var today = DateTime.UtcNow.Date;

            // Search partitions (from oldest to newest) for unsynced records
            for (var d = today.AddDays(-7); d <= today; d = d.AddDays(1))
            {
                var dbPath = GetPartitionPath(d);
                if (!File.Exists(dbPath)) continue;

                var connStr = $"Data Source={dbPath};";
                using var connection = new SqliteConnection(connStr);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, TagPath, Val, Quality, Timestamp
                    FROM TagHistory
                    WHERE IsSynced = 0
                    ORDER BY Id ASC
                    LIMIT $limit;";
                cmd.Parameters.AddWithValue("$limit", batchSize - results.Count);

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    long id = reader.GetInt64(0);
                    string path = reader.GetString(1);
                    double val = reader.GetDouble(2);
                    var quality = (ScadaQuality)reader.GetInt32(3);
                    var ts = DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(4)).UtcDateTime;

                    results.Add(new HistorianRecord(path, val, quality, ts, id));
                }

                if (results.Count >= batchSize) break;
            }

            return results;
        }

        /// <inheritdoc />
        public async Task MarkSyncedAsync(long upToId, DateTime partitionDate, CancellationToken cancellationToken = default)
        {
            var dbPath = GetPartitionPath(partitionDate);
            if (!File.Exists(dbPath)) return;

            var connStr = $"Data Source={dbPath};";
            using var connection = new SqliteConnection(connStr);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE TagHistory SET IsSynced = 1 WHERE Id <= $upToId AND IsSynced = 0;";
            cmd.Parameters.AddWithValue("$upToId", upToId);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<int> PurgeExpiredPartitionsAsync(int retentionDays, CancellationToken cancellationToken = default)
        {
            if (retentionDays <= 0) return Task.FromResult(0);
            int purgedCount = 0;
            var cutoffDate = DateTime.UtcNow.Date.AddDays(-retentionDays);

            var files = Directory.GetFiles(_storageDirectory, "Historian_*.db");
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                // Historian_yyyy_MM_dd
                if (fileName.StartsWith("Historian_", StringComparison.OrdinalIgnoreCase))
                {
                    var datePart = fileName.Substring("Historian_".Length);
                    if (DateTime.TryParseExact(datePart, "yyyy_MM_dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fileDate))
                    {
                        if (fileDate < cutoffDate)
                        {
                            try
                            {
                                File.Delete(file);
                                var walFile = file + "-wal";
                                var shmFile = file + "-shm";
                                if (File.Exists(walFile)) File.Delete(walFile);
                                if (File.Exists(shmFile)) File.Delete(shmFile);
                                purgedCount++;
                            }
                            catch
                            {
                                // In-use lock protection
                            }
                        }
                    }
                }
            }

            return Task.FromResult(purgedCount);
        }

        private string GetPartitionPath(DateTime date)
        {
            var fileName = $"Historian_{date:yyyy_MM_dd}.db";
            return Path.Combine(_storageDirectory, fileName);
        }

        private static void InitializeDatabasePragmas(SqliteConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                PRAGMA journal_mode = WAL;
                PRAGMA synchronous = NORMAL;
                PRAGMA temp_store = MEMORY;
                PRAGMA cache_size = -64000;
                PRAGMA page_size = 4096;

                CREATE TABLE IF NOT EXISTS TagHistory (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TagPath TEXT NOT NULL,
                    Val REAL NOT NULL,
                    Quality INTEGER NOT NULL,
                    Timestamp INTEGER NOT NULL,
                    IsSynced INTEGER NOT NULL DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS IX_TagHistory_Tag_Time ON TagHistory (TagPath, Timestamp);
                CREATE INDEX IF NOT EXISTS IX_TagHistory_Synced ON TagHistory (IsSynced, Id);
            ";
            cmd.ExecuteNonQuery();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _flushTimer.Dispose();
            _cts.Cancel();
            _cts.Dispose();
            _writeLock.Dispose();
        }
    }
}
