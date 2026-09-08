using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace ZeroUI.Wpf.Rendering
{
    /// <summary>
    /// High-performance GPU Canvas control for WPF, powered by ZeroD3D11Bridge.
    /// Hosts a D3DImage back buffer connected via shared Direct3D 11 texture handles.
    /// Automatically manages high-DPI scaling, front-buffer loss recovery, and 60/120/144 FPS rendering loops.
    /// </summary>
    public class ZeroD3DCanvas : Image, IDisposable
    {
        private readonly D3DImage _d3dImage;
        private readonly ZeroD3D11Bridge _bridge;
        private readonly Stopwatch _fpsStopwatch = new Stopwatch();
        private int _frameCount;
        private double _animPhase = 0.0;
        private bool _isDisposed;

        public static readonly DependencyProperty IsRenderingActiveProperty =
            DependencyProperty.Register(
                nameof(IsRenderingActive),
                typeof(bool),
                typeof(ZeroD3DCanvas),
                new PropertyMetadata(true, OnIsRenderingActiveChanged));

        public static readonly DependencyProperty EnableWaveformDemoProperty =
            DependencyProperty.Register(
                nameof(EnableWaveformDemo),
                typeof(bool),
                typeof(ZeroD3DCanvas),
                new PropertyMetadata(true));

        public bool IsRenderingActive
        {
            get => (bool)GetValue(IsRenderingActiveProperty);
            set => SetValue(IsRenderingActiveProperty, value);
        }

        public bool EnableWaveformDemo
        {
            get => (bool)GetValue(EnableWaveformDemoProperty);
            set => SetValue(EnableWaveformDemoProperty, value);
        }

        public double CurrentFps { get; private set; }

        /// <summary>
        /// Hook for custom Direct3D 11 GPU rendering pass.
        /// Receives the active bridge with D3D11Device, D3D11Context, RTV, pixel width, and pixel height.
        /// </summary>
        public event Action<ZeroD3D11Bridge, int, int>? RenderFrame;

        public ZeroD3D11Bridge Bridge => _bridge;

        public ZeroD3DCanvas()
        {
            _d3dImage = new D3DImage();
            Source = _d3dImage;
            Stretch = Stretch.Fill;

            _bridge = new ZeroD3D11Bridge();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;
            _d3dImage.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;
        }

        private static void OnIsRenderingActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZeroD3DCanvas canvas && canvas.IsLoaded)
            {
                canvas.UpdateRenderingSubscription();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateRenderingSubscription();
            RecreateSurface();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;
        }

        private void UpdateRenderingSubscription()
        {
            CompositionTarget.Rendering -= OnRendering;
            if (IsRenderingActive && IsLoaded)
            {
                _fpsStopwatch.Restart();
                _frameCount = 0;
                CompositionTarget.Rendering += OnRendering;
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            RecreateSurface();
        }

        private void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_d3dImage.IsFrontBufferAvailable)
            {
                RecreateSurface();
            }
        }

        private void RecreateSurface()
        {
            if (!IsLoaded || ActualWidth <= 0 || ActualHeight <= 0) return;

            var dpi = VisualTreeHelper.GetDpi(this);
            int pixelWidth = Math.Max(1, (int)Math.Ceiling(ActualWidth * dpi.DpiScaleX));
            int pixelHeight = Math.Max(1, (int)Math.Ceiling(ActualHeight * dpi.DpiScaleY));

            try
            {
                _bridge.EnsureSurfaceSize(pixelWidth, pixelHeight);
                RenderCurrentFrame();
            }
            catch
            {
                // Graceful degradation
            }
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (!_d3dImage.IsFrontBufferAvailable || ActualWidth <= 0 || ActualHeight <= 0) return;

            RenderCurrentFrame();

            // FPS Measurement
            _frameCount++;
            if (_fpsStopwatch.ElapsedMilliseconds >= 500)
            {
                CurrentFps = _frameCount * 1000.0 / _fpsStopwatch.ElapsedMilliseconds;
                _frameCount = 0;
                _fpsStopwatch.Restart();
            }
        }

        /// <summary>
        /// Executes a single GPU frame render pass and presents it to WPF.
        /// </summary>
        public void RenderCurrentFrame()
        {
            if (!_bridge.IsInitialized) return;

            int w = _bridge.PixelWidth;
            int h = _bridge.PixelHeight;
            if (w <= 0 || h <= 0) return;

            if (RenderFrame != null)
            {
                RenderFrame.Invoke(_bridge, w, h);
            }
            else if (EnableWaveformDemo)
            {
                // Built-in GPU gradient pulse demo
                _animPhase += 0.035;
                float r = (float)(0.08 + 0.05 * Math.Sin(_animPhase));
                float g = (float)(0.12 + 0.08 * Math.Sin(_animPhase + 2.0));
                float b = (float)(0.22 + 0.12 * Math.Sin(_animPhase + 4.0));

                _bridge.Clear(r, g, b, 1.0f);
            }

            _bridge.Flush();
            _bridge.PresentTo(_d3dImage);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            CompositionTarget.Rendering -= OnRendering;
            _d3dImage.IsFrontBufferAvailableChanged -= OnIsFrontBufferAvailableChanged;
            _bridge.Dispose();
        }
    }
}
