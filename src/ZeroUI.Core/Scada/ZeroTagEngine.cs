using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ZeroUI.Core.Collections;
using ZeroUI.Core.Data;
using ZeroUI.Core.Historian;
using ZeroUI.Core.Runtime;

namespace ZeroUI.Core.Scada
{
    /// <summary>
    /// High-throughput, thread-safe central SCADA Tag Engine.
    /// Manages tag registry, deadband noise suppression, and UI subscription callbacks.
    /// </summary>
    public static class ZeroTagEngine
    {
        private static readonly ConcurrentDictionary<string, ScadaTagRecord> _registry =
            new ConcurrentDictionary<string, ScadaTagRecord>(StringComparer.OrdinalIgnoreCase);

        private static readonly object _subLock = new object();
        private static readonly Dictionary<string, List<Action<IScadaTag>>> _subscriptions =
            new Dictionary<string, List<Action<IScadaTag>>>(StringComparer.OrdinalIgnoreCase);

        private static readonly List<IScadaBindable> _boundControls = new List<IScadaBindable>();
        private static IHistorianEngine? _attachedHistorian;

        /// <summary>
        /// Global event fired whenever any tag in the registry is updated.
        /// </summary>
        public static event Action<IScadaTag>? TagUpdated;

        /// <summary>
        /// Registers or updates a SCADA tag with optional deadband filtering.
        /// </summary>
        /// <param name="tagPath">Tag path identifier (e.g. "Plant.Line1.TankLevel").</param>
        /// <param name="value">New telemetry value.</param>
        /// <param name="quality">Signal quality status.</param>
        /// <param name="timestamp">Timestamp (defaults to UtcNow).</param>
        /// <returns>True if value passed deadband filter and was published; false if suppressed.</returns>
        public static bool SetTagValue(string tagPath, object? value, ScadaQuality quality = ScadaQuality.Good, DateTime? timestamp = null)
        {
            if (string.IsNullOrWhiteSpace(tagPath)) return false;

            var now = timestamp ?? DateTime.UtcNow;
            var record = _registry.GetOrAdd(tagPath, path => new ScadaTagRecord(path));

            lock (record)
            {
                // Deadband noise suppression for numeric types
                if (record.Deadband > 0 && record.CurrentTag != null && value != null && record.CurrentTag.Value != null)
                {
                    if (TryGetNumeric(value, out double newNum) && TryGetNumeric(record.CurrentTag.Value, out double oldNum))
                    {
                        if (Math.Abs(newNum - oldNum) < record.Deadband && quality == record.CurrentTag.Quality)
                        {
                            return false; // Suppressed by deadband
                        }
                    }
                }

                var newTag = new ScadaTag(tagPath, value, quality, now);
                record.CurrentTag = newTag;

                if (TryGetNumeric(value, out double numVal))
                {
                    record.HistoryBuffer?.Write(new TimePoint(new DateTimeOffset(now).ToUnixTimeMilliseconds(), numVal));
                    _attachedHistorian?.LogSample(tagPath, numVal, quality, now);
                }

                // Dispatch to specific subscribers
                List<Action<IScadaTag>>? callbacks = null;
                lock (_subLock)
                {
                    if (_subscriptions.TryGetValue(tagPath, out var list))
                    {
                        callbacks = new List<Action<IScadaTag>>(list);
                    }
                }

                if (callbacks != null)
                {
                    for (int i = 0; i < callbacks.Count; i++)
                    {
                        try
                        {
                            callbacks[i](newTag);
                        }
                        catch
                        {
                            // Guard against subscriber exceptions
                        }
                    }
                }

                // Dispatch to bound visual components
                lock (_subLock)
                {
                    for (int i = 0; i < _boundControls.Count; i++)
                    {
                        var control = _boundControls[i];
                        if (string.Equals(control.BoundTagPath, tagPath, StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                if (UiDispatcher.IsInitialized && !UiDispatcher.IsOnUiDispatcherThread)
                                {
                                    UiDispatcher.Post(() => control.OnTagValueChanged(newTag));
                                }
                                else
                                {
                                    control.OnTagValueChanged(newTag);
                                }
                            }
                            catch
                            {
                                // Guard against UI rendering exceptions
                            }
                        }
                    }
                }

                TagUpdated?.Invoke(newTag);
                return true;
            }
        }

        /// <summary>
        /// Retrieves the current snapshot of a registered tag, or null if unregistered.
        /// </summary>
        public static IScadaTag? GetTag(string tagPath)
        {
            if (string.IsNullOrWhiteSpace(tagPath)) return null;
            return _registry.TryGetValue(tagPath, out var record) ? record.CurrentTag : null;
        }

        /// <summary>
        /// Sets a deadband filter for a tag to eliminate sensor jitter below the threshold.
        /// </summary>
        public static void SetDeadband(string tagPath, double deadbandThreshold)
        {
            if (string.IsNullOrWhiteSpace(tagPath)) return;
            var record = _registry.GetOrAdd(tagPath, path => new ScadaTagRecord(path));
            record.Deadband = Math.Max(0, deadbandThreshold);
        }

        /// <summary>
        /// Subscribes an action callback to updates on a specific tag.
        /// </summary>
        public static IDisposable Subscribe(string tagPath, Action<IScadaTag> callback)
        {
            if (string.IsNullOrWhiteSpace(tagPath) || callback == null)
                return EmptyDisposable.Instance;

            lock (_subLock)
            {
                if (!_subscriptions.TryGetValue(tagPath, out var list))
                {
                    list = new List<Action<IScadaTag>>();
                    _subscriptions[tagPath] = list;
                }
                list.Add(callback);
            }

            // If tag already exists, immediately invoke once with current value
            var current = GetTag(tagPath);
            if (current != null)
            {
                try
                {
                    callback(current);
                }
                catch { }
            }

            return new SubscriptionToken(tagPath, callback);
        }

        /// <summary>
        /// Registers a visual control implementing IScadaBindable into the engine.
        /// </summary>
        public static void RegisterBindable(IScadaBindable control)
        {
            if (control == null) return;
            lock (_subLock)
            {
                if (!_boundControls.Contains(control))
                {
                    _boundControls.Add(control);
                }
            }

            if (!string.IsNullOrEmpty(control.BoundTagPath))
            {
                var cur = GetTag(control.BoundTagPath!);
                if (cur != null)
                {
                    control.OnTagValueChanged(cur);
                }
            }
        }

        /// <summary>
        /// Unregisters a visual control from the engine.
        /// </summary>
        public static void UnregisterBindable(IScadaBindable control)
        {
            if (control == null) return;
            lock (_subLock)
            {
                _boundControls.Remove(control);
            }
        }

        /// <summary>
        /// Returns all currently registered tags.
        /// </summary>
        public static IReadOnlyCollection<IScadaTag> GetAllTags()
        {
            var list = new List<IScadaTag>(_registry.Count);
            foreach (var kvp in _registry)
            {
                if (kvp.Value.CurrentTag != null)
                {
                    list.Add(kvp.Value.CurrentTag);
                }
            }
            return list.AsReadOnly();
        }

        /// <summary>
        /// Attaches an industrial historian engine to automatically receive all valid tag updates.
        /// </summary>
        public static void AttachHistorian(IHistorianEngine? historian)
        {
            _attachedHistorian = historian;
        }

        /// <summary>
        /// Enables an in-memory RingBuffer for a tag to retain high-speed telemetry history for real-time trends.
        /// </summary>
        public static void EnableTagHistoryBuffer(string tagPath, int capacity = 1024)
        {
            if (string.IsNullOrWhiteSpace(tagPath)) return;
            var record = _registry.GetOrAdd(tagPath, path => new ScadaTagRecord(path));
            lock (record)
            {
                record.HistoryBuffer = new RingBuffer<TimePoint>(capacity);
            }
        }

        /// <summary>
        /// Gets the in-memory telemetry points recorded for a tag.
        /// </summary>
        public static IReadOnlyList<TimePoint> GetRecentHistory(string tagPath)
        {
            if (string.IsNullOrWhiteSpace(tagPath)) return Array.Empty<TimePoint>();
            if (_registry.TryGetValue(tagPath, out var record))
            {
                lock (record)
                {
                    if (record.HistoryBuffer != null)
                    {
                        return record.HistoryBuffer.ToArray();
                    }
                }
            }
            return Array.Empty<TimePoint>();
        }

        private static bool TryGetNumeric(object? val, out double result)
        {
            if (val == null) { result = 0; return false; }
            if (val is double d) { result = d; return true; }
            if (val is float f) { result = f; return true; }
            if (val is int i) { result = i; return true; }
            if (val is long l) { result = l; return true; }
            if (val is short s) { result = s; return true; }
            if (val is decimal dec) { result = (double)dec; return true; }
            if (val is byte b) { result = b; return true; }
            result = 0;
            return false;
        }

        private sealed class ScadaTagRecord
        {
            public string TagPath { get; }
            public IScadaTag? CurrentTag { get; set; }
            public double Deadband { get; set; }
            public RingBuffer<TimePoint>? HistoryBuffer { get; set; }

            public ScadaTagRecord(string tagPath)
            {
                TagPath = tagPath;
            }
        }

        private sealed class SubscriptionToken : IDisposable
        {
            private readonly string _tagPath;
            private readonly Action<IScadaTag> _callback;
            private bool _disposed;

            public SubscriptionToken(string tagPath, Action<IScadaTag> callback)
            {
                _tagPath = tagPath;
                _callback = callback;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                lock (_subLock)
                {
                    if (_subscriptions.TryGetValue(_tagPath, out var list))
                    {
                        list.Remove(_callback);
                        if (list.Count == 0)
                        {
                            _subscriptions.Remove(_tagPath);
                        }
                    }
                }
            }
        }

        private sealed class EmptyDisposable : IDisposable
        {
            public static readonly EmptyDisposable Instance = new EmptyDisposable();
            public void Dispose() { }
        }
    }
}
