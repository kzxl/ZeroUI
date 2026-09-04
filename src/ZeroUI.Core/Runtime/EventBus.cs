using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroUI.Core.Runtime
{
    /// <summary>
    /// Contract for an in-process decoupled event bus.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Subscribes a handler to events of type TEvent.
        /// Returns an IDisposable subscription token for safe unsubscription.
        /// </summary>
        IDisposable Subscribe<TEvent>(Action<TEvent> handler);

        /// <summary>
        /// Subscribes an asynchronous handler to events of type TEvent.
        /// </summary>
        IDisposable SubscribeAsync<TEvent>(Func<TEvent, Task> handler);

        /// <summary>
        /// Publishes an event to all registered subscribers synchronously.
        /// </summary>
        void Publish<TEvent>(TEvent eventData);

        /// <summary>
        /// Publishes an event to all registered subscribers asynchronously.
        /// </summary>
        Task PublishAsync<TEvent>(TEvent eventData);
    }

    /// <summary>
    /// Thread-safe in-process publish/subscribe message bus with subscription lifecycle tokens.
    /// </summary>
    public sealed class EventBus : IEventBus
    {
        private static readonly Lazy<EventBus> _defaultInstance = new Lazy<EventBus>(() => new EventBus());
        public static EventBus Default => _defaultInstance.Value;

        private readonly ConcurrentDictionary<Type, List<Delegate>> _syncHandlers =
            new ConcurrentDictionary<Type, List<Delegate>>();

        private readonly ConcurrentDictionary<Type, List<Delegate>> _asyncHandlers =
            new ConcurrentDictionary<Type, List<Delegate>>();

        private readonly object _lock = new object();

        /// <inheritdoc />
        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var type = typeof(TEvent);

            lock (_lock)
            {
                var list = _syncHandlers.GetOrAdd(type, _ => new List<Delegate>());
                list.Add(handler);
            }

            return new SubscriptionToken(() =>
            {
                lock (_lock)
                {
                    if (_syncHandlers.TryGetValue(type, out var list))
                    {
                        list.Remove(handler);
                    }
                }
            });
        }

        /// <inheritdoc />
        public IDisposable SubscribeAsync<TEvent>(Func<TEvent, Task> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var type = typeof(TEvent);

            lock (_lock)
            {
                var list = _asyncHandlers.GetOrAdd(type, _ => new List<Delegate>());
                list.Add(handler);
            }

            return new SubscriptionToken(() =>
            {
                lock (_lock)
                {
                    if (_asyncHandlers.TryGetValue(type, out var list))
                    {
                        list.Remove(handler);
                    }
                }
            });
        }

        /// <inheritdoc />
        public void Publish<TEvent>(TEvent eventData)
        {
            var type = typeof(TEvent);
            List<Delegate>? syncList = null;

            lock (_lock)
            {
                if (_syncHandlers.TryGetValue(type, out var list) && list.Count > 0)
                {
                    syncList = new List<Delegate>(list);
                }
            }

            if (syncList != null)
            {
                for (int i = 0; i < syncList.Count; i++)
                {
                    try
                    {
                        ((Action<TEvent>)syncList[i])(eventData);
                    }
                    catch
                    {
                        // Guard against individual subscriber failure
                    }
                }
            }
        }

        /// <inheritdoc />
        public async Task PublishAsync<TEvent>(TEvent eventData)
        {
            Publish(eventData); // Run synchronous handlers first

            var type = typeof(TEvent);
            List<Delegate>? asyncList = null;

            lock (_lock)
            {
                if (_asyncHandlers.TryGetValue(type, out var list) && list.Count > 0)
                {
                    asyncList = new List<Delegate>(list);
                }
            }

            if (asyncList != null)
            {
                for (int i = 0; i < asyncList.Count; i++)
                {
                    try
                    {
                        await ((Func<TEvent, Task>)asyncList[i])(eventData).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Guard against individual subscriber failure
                    }
                }
            }
        }

        /// <summary>
        /// Clears all registered subscriptions.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _syncHandlers.Clear();
                _asyncHandlers.Clear();
            }
        }

        private sealed class SubscriptionToken : IDisposable
        {
            private Action? _unsubscribe;

            public SubscriptionToken(Action unsubscribe)
            {
                _unsubscribe = unsubscribe;
            }

            public void Dispose()
            {
                var action = Interlocked.Exchange(ref _unsubscribe, null);
                action?.Invoke();
            }
        }
    }
}
