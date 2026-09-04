using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ZeroUI.Core.Scada
{
    /// <summary>
    /// Thread-safe coalescing queue that throttles high-frequency telemetry updates from PLCs / edge brokers.
    /// Deduplicates and batches raw signals before delivering them to UI subscribers, preventing STA thread starvation.
    /// </summary>
    public sealed class TelemetryThrottleQueue : IDisposable
    {
        private readonly ConcurrentDictionary<string, IScadaTag> _pendingSnapshots =
            new ConcurrentDictionary<string, IScadaTag>(StringComparer.OrdinalIgnoreCase);

        private readonly Timer _flushTimer;
        private readonly Action<IReadOnlyList<IScadaTag>> _flushCallback;
        private int _isFlushing;
        private bool _disposed;

        /// <summary>
        /// Gets the flush interval in milliseconds (default: 33ms ~ 30Hz).
        /// </summary>
        public int IntervalMs { get; }

        public TelemetryThrottleQueue(Action<IReadOnlyList<IScadaTag>> flushCallback, int intervalMs = 33)
        {
            _flushCallback = flushCallback ?? throw new ArgumentNullException(nameof(flushCallback));
            IntervalMs = Math.Max(10, intervalMs);
            _flushTimer = new Timer(OnFlushTimer, null, IntervalMs, IntervalMs);
        }

        /// <summary>
        /// Enqueues a telemetry snapshot. If a tag was already pending, it is coalesced with the newest value.
        /// </summary>
        public void Enqueue(IScadaTag tag)
        {
            if (tag == null || string.IsNullOrEmpty(tag.TagPath)) return;
            _pendingSnapshots[tag.TagPath] = tag;
        }

        private void OnFlushTimer(object? state)
        {
            if (_disposed) return;
            if (Interlocked.CompareExchange(ref _isFlushing, 1, 0) != 0)
                return; // Previous flush still executing, skip frame

            try
            {
                if (_pendingSnapshots.IsEmpty) return;

                var batch = new List<IScadaTag>(_pendingSnapshots.Count);
                foreach (var kvp in _pendingSnapshots)
                {
                    if (_pendingSnapshots.TryRemove(kvp.Key, out var tag))
                    {
                        batch.Add(tag);
                    }
                }

                if (batch.Count > 0)
                {
                    _flushCallback(batch);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _isFlushing, 0);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _flushTimer.Dispose();
            _pendingSnapshots.Clear();
        }
    }
}
