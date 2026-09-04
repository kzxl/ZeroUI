using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ZeroUI.Core.Runtime;

namespace ZeroUI.Core.Rendering
{
    /// <summary>
    /// Listener contract for central animation frame notifications.
    /// </summary>
    public interface IAnimationFrameListener
    {
        /// <summary>
        /// Called when an animation frame advances.
        /// </summary>
        /// <param name="deltaSeconds">Elapsed time in seconds since the last frame.</param>
        /// <param name="frameCount">Total monotonic frame counter.</param>
        void OnAnimationFrame(double deltaSeconds, long frameCount);
    }

    /// <summary>
    /// High-resolution centralized animation clock.
    /// Eliminates per-control UI timers, drastically cutting CPU overhead and avoiding timer thread contention.
    /// Supports both worker thread ticks and UI SynchronizationContext synchronization.
    /// </summary>
    public static class ZeroAnimationClock
    {
        private static readonly object _lock = new object();
        private static readonly List<Action<double, long>> _actionListeners = new List<Action<double, long>>(64);
        private static readonly List<IAnimationFrameListener> _contractListeners = new List<IAnimationFrameListener>(64);

        private static Timer? _timer;
        private static readonly Stopwatch _stopwatch = new Stopwatch();
        private static double _lastElapsedSeconds;
        private static long _frameCount;
        private static bool _isRunning;
        private static int _targetFps = 60;
        private static SynchronizationContext? _syncContext;

        private static IDisposable? _runtimeSub;

        /// <summary>
        /// Total monotonic frames rendered since clock startup.
        /// </summary>
        public static long FrameCount => _frameCount;

        /// <summary>
        /// True if the central clock is actively ticking.
        /// </summary>
        public static bool IsRunning => _isRunning;

        /// <summary>
        /// Gets or sets the target frames per second (default is 60).
        /// </summary>
        public static int TargetFps
        {
            get => _targetFps;
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "FPS must be greater than 0.");
                _targetFps = Math.Min(120, Math.Max(10, value));
                ZeroRuntime.Shared.SetCycleInterval(RuntimeCycle.Animation, TimeSpan.FromMilliseconds(Math.Max(8, 1000.0 / _targetFps)));
                if (_isRunning)
                {
                    RestartTimer();
                }
            }
        }

        /// <summary>
        /// Configures the SynchronizationContext used to marshal frame callbacks to the UI thread (if desired).
        /// </summary>
        public static void SetSynchronizationContext(SynchronizationContext? context)
        {
            _syncContext = context;
        }

        /// <summary>
        /// Starts the centralized animation clock if not already running.
        /// </summary>
        public static void Start(int targetFps = 60)
        {
            lock (_lock)
            {
                if (_isRunning) return;
                _targetFps = targetFps;
                _stopwatch.Restart();
                _lastElapsedSeconds = 0;
                _isRunning = true;

                ZeroRuntime.Shared.SetCycleInterval(RuntimeCycle.Animation, TimeSpan.FromMilliseconds(Math.Max(8, 1000.0 / _targetFps)));
                _runtimeSub ??= ZeroRuntime.Shared.Register(RuntimeCycle.Animation, (delta, frame) => TriggerFrame(delta.TotalSeconds));
                if (!ZeroRuntime.Shared.IsRunning)
                {
                    ZeroRuntime.Shared.Start();
                }
            }
        }

        /// <summary>
        /// Stops the central animation clock.
        /// </summary>
        public static void Stop()
        {
            lock (_lock)
            {
                _isRunning = false;
                _runtimeSub?.Dispose();
                _runtimeSub = null;
                _timer?.Dispose();
                _timer = null;
                _stopwatch.Stop();
            }
        }

        /// <summary>
        /// Subscribes a delegate callback to the central animation frame tick.
        /// Returns an IDisposable token for easy unsubscription.
        /// </summary>
        public static IDisposable Subscribe(Action<double, long> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            lock (_lock)
            {
                if (!_actionListeners.Contains(callback))
                {
                    _actionListeners.Add(callback);
                }

                if (!_isRunning && (_actionListeners.Count > 0 || _contractListeners.Count > 0))
                {
                    Start(_targetFps);
                }
            }

            return new SubscriptionToken(() => Unsubscribe(callback));
        }

        /// <summary>
        /// Subscribes an IAnimationFrameListener to the central animation tick.
        /// </summary>
        public static void Subscribe(IAnimationFrameListener listener)
        {
            if (listener == null) return;
            lock (_lock)
            {
                if (!_contractListeners.Contains(listener))
                {
                    _contractListeners.Add(listener);
                }

                if (!_isRunning && (_actionListeners.Count > 0 || _contractListeners.Count > 0))
                {
                    Start(_targetFps);
                }
            }
        }

        /// <summary>
        /// Unsubscribes a delegate callback.
        /// </summary>
        public static void Unsubscribe(Action<double, long> callback)
        {
            if (callback == null) return;
            lock (_lock)
            {
                _actionListeners.Remove(callback);
                if (_actionListeners.Count == 0 && _contractListeners.Count == 0)
                {
                    Stop();
                }
            }
        }

        /// <summary>
        /// Unsubscribes an IAnimationFrameListener.
        /// </summary>
        public static void Unsubscribe(IAnimationFrameListener listener)
        {
            if (listener == null) return;
            lock (_lock)
            {
                _contractListeners.Remove(listener);
                if (_actionListeners.Count == 0 && _contractListeners.Count == 0)
                {
                    Stop();
                }
            }
        }

        /// <summary>
        /// Manually pumps a frame tick on the caller thread. Useful when hosting in custom message loops.
        /// </summary>
        public static void ManualTick(double deltaSeconds)
        {
            TriggerFrame(deltaSeconds);
        }

        private static void RestartTimer()
        {
            _timer?.Dispose();
            int intervalMs = Math.Max(8, 1000 / _targetFps);
            _timer = new Timer(OnTimerTick, null, intervalMs, intervalMs);
        }

        private static void OnTimerTick(object? state)
        {
            if (!_isRunning) return;

            double currentElapsed = _stopwatch.Elapsed.TotalSeconds;
            double delta = currentElapsed - _lastElapsedSeconds;
            _lastElapsedSeconds = currentElapsed;

            // Clamp delta to prevent time-spiral jumps if app pauses
            if (delta <= 0) delta = 1.0 / _targetFps;
            else if (delta > 0.5) delta = 0.5;

            var ctx = _syncContext;
            if (ctx != null)
            {
                ctx.Post(_ => TriggerFrame(delta), null);
            }
            else
            {
                TriggerFrame(delta);
            }
        }

        private static void TriggerFrame(double delta)
        {
            long currentFrame = Interlocked.Increment(ref _frameCount);

            Action<double, long>[]? actionsCopy = null;
            IAnimationFrameListener[]? contractsCopy = null;

            lock (_lock)
            {
                if (_actionListeners.Count > 0)
                    actionsCopy = _actionListeners.ToArray();
                if (_contractListeners.Count > 0)
                    contractsCopy = _contractListeners.ToArray();
            }

            if (actionsCopy != null)
            {
                for (int i = 0; i < actionsCopy.Length; i++)
                {
                    try { actionsCopy[i](delta, currentFrame); }
                    catch { /* Swallow exception to prevent one broken control from halting the clock */ }
                }
            }

            if (contractsCopy != null)
            {
                for (int i = 0; i < contractsCopy.Length; i++)
                {
                    try { contractsCopy[i].OnAnimationFrame(delta, currentFrame); }
                    catch { /* Swallow exception */ }
                }
            }
        }

        private sealed class SubscriptionToken : IDisposable
        {
            private readonly Action _unsubscribe;
            private bool _disposed;

            public SubscriptionToken(Action unsubscribe)
            {
                _unsubscribe = unsubscribe;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _unsubscribe();
                }
            }
        }
    }
}
