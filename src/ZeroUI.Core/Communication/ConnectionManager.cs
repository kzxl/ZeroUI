using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZeroUI.Core.Runtime;

namespace ZeroUI.Core.Communication
{
    /// <summary>
    /// Event payload broadcasted across EventBus when an adapter connection state changes.
    /// </summary>
    public sealed class AdapterStateChangedEvent
    {
        public string AdapterId { get; }
        public string Endpoint { get; }
        public AdapterConnectionState OldState { get; }
        public AdapterConnectionState NewState { get; }
        public TimeSpan Latency { get; }
        public DateTime Timestamp { get; }

        public AdapterStateChangedEvent(string adapterId, string endpoint, AdapterConnectionState oldState, AdapterConnectionState newState, TimeSpan latency)
        {
            AdapterId = adapterId;
            Endpoint = endpoint;
            OldState = oldState;
            NewState = newState;
            Latency = latency;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Central manager coordinating lifecycle, background polling, and automatic reconnection
    /// of multiple industrial protocol adapters.
    /// </summary>
    public sealed class ConnectionManager : IDisposable
    {
        private static readonly Lazy<ConnectionManager> _defaultInstance =
            new Lazy<ConnectionManager>(() => new ConnectionManager());
        public static ConnectionManager Default => _defaultInstance.Value;

        private readonly ConcurrentDictionary<string, ManagedAdapterContext> _adapters =
            new ConcurrentDictionary<string, ManagedAdapterContext>(StringComparer.OrdinalIgnoreCase);

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _isDisposed;

        /// <summary>
        /// Registers a protocol adapter into the connection manager with configurable poll interval and auto-reconnect.
        /// </summary>
        public void RegisterAdapter(
            IProtocolAdapter adapter,
            TimeSpan? pollInterval = null,
            bool autoReconnect = true)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            var interval = pollInterval ?? TimeSpan.FromMilliseconds(250);
            var context = new ManagedAdapterContext(adapter, interval, autoReconnect);

            if (_adapters.TryAdd(adapter.AdapterId, context))
            {
                adapter.StateChanged += OnAdapterStateChanged;
            }
        }

        /// <summary>
        /// Starts background polling and connection loops for all registered adapters.
        /// </summary>
        public async Task StartAllAsync(CancellationToken cancellationToken = default)
        {
            foreach (var kvp in _adapters)
            {
                var context = kvp.Value;
                if (context.WorkerTask == null)
                {
                    context.WorkerTask = Task.Run(() => AdapterLoopAsync(context, _cts.Token));
                }
            }

            await Task.Yield();
        }

        /// <summary>
        /// Stops all background tasks and disconnects all adapters.
        /// </summary>
        public async Task StopAllAsync(CancellationToken cancellationToken = default)
        {
            _cts.Cancel();

            var stopTasks = new List<Task>();
            foreach (var kvp in _adapters)
            {
                stopTasks.Add(kvp.Value.Adapter.DisconnectAsync(cancellationToken));
            }

            await Task.WhenAll(stopTasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets an adapter by its unique identifier.
        /// </summary>
        public IProtocolAdapter? GetAdapter(string adapterId)
        {
            return _adapters.TryGetValue(adapterId, out var ctx) ? ctx.Adapter : null;
        }

        /// <summary>
        /// Returns all currently registered adapters.
        /// </summary>
        public IReadOnlyCollection<IProtocolAdapter> GetAllAdapters()
        {
            var list = new List<IProtocolAdapter>(_adapters.Count);
            foreach (var kvp in _adapters)
            {
                list.Add(kvp.Value.Adapter);
            }
            return list.AsReadOnly();
        }

        private async Task AdapterLoopAsync(ManagedAdapterContext ctx, CancellationToken token)
        {
            var adapter = ctx.Adapter;
            int reconnectAttempts = 0;

            while (!token.IsCancellationRequested)
            {
                // Connection Management
                if (adapter.State != AdapterConnectionState.Connected)
                {
                    if (ctx.AutoReconnect)
                    {
                        try
                        {
                            reconnectAttempts++;
                            await adapter.ConnectAsync(token).ConfigureAwait(false);
                            reconnectAttempts = 0;
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch
                        {
                            // Backoff: 1s, 2s, 4s, up to 15s max
                            int delaySec = Math.Min(15, (int)Math.Pow(2, Math.Min(reconnectAttempts, 4)));
                            await Task.Delay(TimeSpan.FromSeconds(delaySec), token).ConfigureAwait(false);
                            continue;
                        }
                    }
                    else
                    {
                        await Task.Delay(500, token).ConfigureAwait(false);
                        continue;
                    }
                }

                // Polling Loop
                try
                {
                    await adapter.PollOnceAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Socket fault during poll
                }

                try
                {
                    await Task.Delay(ctx.PollInterval, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void OnAdapterStateChanged(IProtocolAdapter adapter, AdapterConnectionState newState)
        {
            // Sync with StateStore
            StateStore.Default.SetState($"Connection.{adapter.AdapterId}.Status", newState.ToString());
            StateStore.Default.SetState($"Connection.{adapter.AdapterId}.LatencyMs", adapter.Latency.TotalMilliseconds);

            // Broadcast via EventBus
            EventBus.Default.Publish(new AdapterStateChangedEvent(
                adapter.AdapterId,
                adapter.Endpoint,
                AdapterConnectionState.Disconnected, // Transition state
                newState,
                adapter.Latency));
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _cts.Cancel();
            _cts.Dispose();

            foreach (var kvp in _adapters)
            {
                kvp.Value.Adapter.StateChanged -= OnAdapterStateChanged;
                kvp.Value.Adapter.Dispose();
            }
            _adapters.Clear();
        }

        private sealed class ManagedAdapterContext
        {
            public IProtocolAdapter Adapter { get; }
            public TimeSpan PollInterval { get; }
            public bool AutoReconnect { get; }
            public Task? WorkerTask { get; set; }

            public ManagedAdapterContext(IProtocolAdapter adapter, TimeSpan pollInterval, bool autoReconnect)
            {
                Adapter = adapter;
                PollInterval = pollInterval;
                AutoReconnect = autoReconnect;
            }
        }
    }
}
