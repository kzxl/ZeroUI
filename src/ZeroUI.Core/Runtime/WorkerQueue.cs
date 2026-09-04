using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ZeroUI.Core.Runtime
{
    /// <summary>
    /// Backpressure mode when the WorkerQueue reaches its maximum capacity.
    /// </summary>
    public enum QueueBackpressureMode
    {
        /// <summary>
        /// Drops the oldest item in the queue to make room for the new item.
        /// Ideal for realtime telemetry where recent data is more relevant than stale data.
        /// </summary>
        DropOldest,

        /// <summary>
        /// Discards the incoming item when full.
        /// </summary>
        DropWrite,

        /// <summary>
        /// Asynchronously waits until capacity becomes available.
        /// </summary>
        Wait
    }

    /// <summary>
    /// High-throughput, bounded asynchronous background processing queue.
    /// Utilizes System.Threading.Channels with configurable backpressure, concurrency, and automatic retries.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    public sealed class WorkerQueue<T> : IDisposable
    {
        private readonly Channel<T> _channel;
        private readonly Func<T, CancellationToken, Task> _handler;
        private readonly int _concurrency;
        private readonly int _maxRetries;
        private readonly Action<T, Exception>? _onError;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Task[] _workerTasks;

        private long _processedCount;
        private long _droppedCount;
        private long _failedCount;
        private bool _isDisposed;

        /// <summary>
        /// Gets the total number of items successfully processed.
        /// </summary>
        public long ProcessedCount => Interlocked.Read(ref _processedCount);

        /// <summary>
        /// Gets the total number of items dropped due to backpressure.
        /// </summary>
        public long DroppedCount => Interlocked.Read(ref _droppedCount);

        /// <summary>
        /// Gets the total number of items that failed after exceeding max retries.
        /// </summary>
        public long FailedCount => Interlocked.Read(ref _failedCount);

        /// <summary>
        /// Initializes a new instance of the WorkerQueue.
        /// </summary>
        /// <param name="handler">Asynchronous processor for each item.</param>
        /// <param name="capacity">Maximum bounded queue capacity.</param>
        /// <param name="backpressure">Strategy when capacity is reached.</param>
        /// <param name="concurrency">Number of concurrent worker tasks.</param>
        /// <param name="maxRetries">Maximum retry attempts upon processing exception.</param>
        /// <param name="onError">Callback invoked when an item fails permanently.</param>
        public WorkerQueue(
            Func<T, CancellationToken, Task> handler,
            int capacity = 10000,
            QueueBackpressureMode backpressure = QueueBackpressureMode.DropOldest,
            int concurrency = 1,
            int maxRetries = 0,
            Action<T, Exception>? onError = null)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
            if (concurrency <= 0) throw new ArgumentOutOfRangeException(nameof(concurrency), "Concurrency must be positive.");

            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _concurrency = concurrency;
            _maxRetries = maxRetries;
            _onError = onError;

            var channelMode = backpressure switch
            {
                QueueBackpressureMode.DropOldest => BoundedChannelFullMode.DropOldest,
                QueueBackpressureMode.DropWrite => BoundedChannelFullMode.DropWrite,
                _ => BoundedChannelFullMode.Wait
            };

            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = channelMode,
                SingleReader = concurrency == 1,
                SingleWriter = false
            };

            _channel = Channel.CreateBounded<T>(options, item =>
            {
                // Item dropped by channel backpressure
                Interlocked.Increment(ref _droppedCount);
            });

            _workerTasks = new Task[_concurrency];
            for (int i = 0; i < _concurrency; i++)
            {
                _workerTasks[i] = Task.Run(WorkerLoopAsync);
            }
        }

        /// <summary>
        /// Enqueues an item asynchronously. If backpressure mode is Wait, awaits buffer space.
        /// </summary>
        public ValueTask EnqueueAsync(T item, CancellationToken cancellationToken = default)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(WorkerQueue<T>));
            return _channel.Writer.WriteAsync(item, cancellationToken);
        }

        /// <summary>
        /// Tries to enqueue an item synchronously without blocking.
        /// Returns true if accepted; false if rejected/dropped.
        /// </summary>
        public bool TryEnqueue(T item)
        {
            if (_isDisposed) return false;
            return _channel.Writer.TryWrite(item);
        }

        private async Task WorkerLoopAsync()
        {
            var reader = _channel.Reader;
            var token = _cts.Token;

            try
            {
                while (await reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    while (reader.TryRead(out var item))
                    {
                        await ProcessItemWithRetryAsync(item, token).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Clean exit on cancellation
            }
        }

        private async Task ProcessItemWithRetryAsync(T item, CancellationToken token)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    await _handler(item, token).ConfigureAwait(false);
                    Interlocked.Increment(ref _processedCount);
                    return;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    attempt++;
                    if (attempt > _maxRetries)
                    {
                        Interlocked.Increment(ref _failedCount);
                        try
                        {
                            _onError?.Invoke(item, ex);
                        }
                        catch
                        {
                            // Guard against error handler exceptions
                        }
                        return;
                    }

                    // Short backoff before retry (10ms, 20ms...)
                    await Task.Delay(attempt * 10, token).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Signals that no more items will be written and awaits processing of remaining items.
        /// </summary>
        public async Task CompleteAsync()
        {
            _channel.Writer.TryComplete();
            await Task.WhenAll(_workerTasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Disposes the queue, canceling running workers.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _channel.Writer.TryComplete();
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
