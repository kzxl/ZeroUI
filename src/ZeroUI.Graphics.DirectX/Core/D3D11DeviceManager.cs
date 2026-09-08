using System;
using System.Runtime.InteropServices;
using ZeroUI.Core.Rendering.Optimizer;
using ZeroUI.Graphics.DirectX.Native;

namespace ZeroUI.Graphics.DirectX.Core
{
    /// <summary>
    /// Thread-safe central manager for Direct3D 11 hardware acceleration and DXGI telemetry.
    /// Manages a single shared <see cref="D3D11Device"/> and immediate context across all UI controls,
    /// eliminating VRAM exhaustion and providing live hardware capability metrics to <see cref="ZeroGpuCapabilities"/>.
    /// </summary>
    public static class D3D11DeviceManager
    {
        private static readonly object _syncLock = new object();
        private static D3D11Device? _device;
        private static D3D11DeviceContext? _context;
        private static IntPtr _factoryHandle = IntPtr.Zero;
        private static bool _initialized;

        public static D3D11Device Device
        {
            get
            {
                EnsureInitialized();
                return _device ?? throw new InvalidOperationException("Direct3D 11 device could not be initialized.");
            }
        }

        public static D3D11DeviceContext Context
        {
            get
            {
                EnsureInitialized();
                return _context ?? throw new InvalidOperationException("Direct3D 11 device context could not be initialized.");
            }
        }

        public static IntPtr FactoryHandle
        {
            get
            {
                EnsureInitialized();
                return _factoryHandle;
            }
        }

        public static bool IsSupported
        {
            get
            {
                try
                {
                    EnsureInitialized();
                    return _device != null && _device.IsValid;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static event Action? DeviceReset;

        public static void EnsureInitialized()
        {
            if (_initialized && _device != null && _device.IsValid) return;

            lock (_syncLock)
            {
                if (_initialized && _device != null && _device.IsValid) return;

                InitializeInternal();
            }
        }

        private static void InitializeInternal()
        {
            // 1. Query DXGI Telemetry
            EnumerateHardwareTelemetry();

            // 2. Create D3D11 Device
            D3D_FEATURE_LEVEL[] featureLevels = new[]
            {
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1,
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0,
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_1,
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_0
            };

            D3D11_CREATE_DEVICE_FLAG flags = D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT;

            int hr = DirectXNative.D3D11CreateDevice(
                IntPtr.Zero,
                D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE,
                IntPtr.Zero,
                flags,
                featureLevels,
                (uint)featureLevels.Length,
                DirectXNative.D3D11_SDK_VERSION,
                out IntPtr pDevice,
                out D3D_FEATURE_LEVEL obtainedLevel,
                out IntPtr pContext);

            // Fallback to WARP software rasterizer if hardware is unavailable
            if (hr < 0 || pDevice == IntPtr.Zero)
            {
                hr = DirectXNative.D3D11CreateDevice(
                    IntPtr.Zero,
                    D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_WARP,
                    IntPtr.Zero,
                    flags,
                    featureLevels,
                    (uint)featureLevels.Length,
                    DirectXNative.D3D11_SDK_VERSION,
                    out pDevice,
                    out obtainedLevel,
                    out pContext);
            }

            if (hr < 0 || pDevice == IntPtr.Zero)
            {
                throw new COMException("Failed to create Direct3D 11 device or WARP fallback.", hr);
            }

            _device = new D3D11Device(pDevice, obtainedLevel);
            _context = new D3D11DeviceContext(pContext);
            _initialized = true;
        }

        private static void EnumerateHardwareTelemetry()
        {
            try
            {
                Guid factoryIid = DirectXNative.IID_IDXGIFactory1;
                int hr = DirectXNative.CreateDXGIFactory1(ref factoryIid, out IntPtr pFactory);
                if (hr >= 0 && pFactory != IntPtr.Zero)
                {
                    _factoryHandle = pFactory;

                    // Enumerate first primary adapter
                    hr = ComVTableHelper.EnumAdapters1(pFactory, 0, out IntPtr pAdapter);
                    if (hr >= 0 && pAdapter != IntPtr.Zero)
                    {
                        try
                        {
                            hr = ComVTableHelper.GetDesc1(pAdapter, out DXGI_ADAPTER_DESC1 desc);
                            if (hr >= 0)
                            {
                                string name = desc.GetDescription();
                                double dedicatedVramMb = (double)desc.DedicatedVideoMemory.ToUInt64() / (1024.0 * 1024.0);
                                double sharedSysMb = (double)desc.SharedSystemMemory.ToUInt64() / (1024.0 * 1024.0);

                                HardwareGpuTier tier;
                                if ((desc.Flags & DXGI_ADAPTER_FLAG.DXGI_ADAPTER_FLAG_SOFTWARE) != 0)
                                {
                                    tier = HardwareGpuTier.Tier0_Software;
                                }
                                else if (dedicatedVramMb >= 1024.0)
                                {
                                    tier = HardwareGpuTier.Tier2_Discrete;
                                }
                                else
                                {
                                    tier = HardwareGpuTier.Tier1_Integrated;
                                }

                                ZeroGpuCapabilities.Configure(name, tier, dedicatedVramMb, sharedSysMb, desc.VendorId);
                            }
                        }
                        finally
                        {
                            ComVTableHelper.Release(pAdapter);
                        }
                    }
                }
            }
            catch
            {
                // Non-fatal telemetry fallback
            }
        }

        public static void HandleDeviceLost()
        {
            lock (_syncLock)
            {
                _context?.Dispose();
                _context = null;

                _device?.Dispose();
                _device = null;

                if (_factoryHandle != IntPtr.Zero)
                {
                    ComVTableHelper.Release(_factoryHandle);
                    _factoryHandle = IntPtr.Zero;
                }

                _initialized = false;
                InitializeInternal();
                DeviceReset?.Invoke();
            }
        }
    }
}
