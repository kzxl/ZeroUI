using System;
using System.Runtime.InteropServices;
using ZeroUI.Graphics.DirectX.Native;

namespace ZeroUI.Graphics.DirectX.Core
{
    public abstract class ComObjectWrapper : IDisposable
    {
        private IntPtr _handle;
        private bool _disposed;

        public IntPtr Handle => _handle;
        public bool IsValid => _handle != IntPtr.Zero && !_disposed;

        protected ComObjectWrapper(IntPtr handle)
        {
            _handle = handle;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_handle != IntPtr.Zero)
                {
                    ComVTableHelper.Release(_handle);
                    _handle = IntPtr.Zero;
                }
                _disposed = true;
            }
        }

        ~ComObjectWrapper()
        {
            Dispose(false);
        }
    }

    public sealed class D3D11Device : ComObjectWrapper
    {
        public D3D_FEATURE_LEVEL FeatureLevel { get; }

        public D3D11Device(IntPtr handle, D3D_FEATURE_LEVEL featureLevel) : base(handle)
        {
            FeatureLevel = featureLevel;
        }

        public unsafe D3D11Buffer CreateBuffer(ref D3D11_BUFFER_DESC desc, D3D11_SUBRESOURCE_DATA* pInitialData = null)
        {
            int hr = ComVTableHelper.CreateBuffer(Handle, ref desc, (IntPtr)pInitialData, out IntPtr ppBuffer);
            if (hr < 0 || ppBuffer == IntPtr.Zero)
                throw new COMException("Failed to create D3D11Buffer.", hr);

            return new D3D11Buffer(ppBuffer, desc);
        }

        public D3D11RenderTargetView CreateRenderTargetView(IntPtr resource, IntPtr desc = default)
        {
            int hr = ComVTableHelper.CreateRenderTargetView(Handle, resource, desc, out IntPtr ppRtv);
            if (hr < 0 || ppRtv == IntPtr.Zero)
                throw new COMException("Failed to create D3D11RenderTargetView.", hr);

            return new D3D11RenderTargetView(ppRtv);
        }

        public unsafe D3D11VertexShader CreateVertexShader(byte[] bytecode)
        {
            if (bytecode == null || bytecode.Length == 0)
                throw new ArgumentNullException(nameof(bytecode));

            fixed (byte* pBytecode = bytecode)
            {
                int hr = ComVTableHelper.CreateVertexShader(Handle, (IntPtr)pBytecode, (UIntPtr)bytecode.Length, IntPtr.Zero, out IntPtr ppShader);
                if (hr < 0 || ppShader == IntPtr.Zero)
                    throw new COMException("Failed to create D3D11VertexShader.", hr);

                return new D3D11VertexShader(ppShader);
            }
        }

        public unsafe D3D11PixelShader CreatePixelShader(byte[] bytecode)
        {
            if (bytecode == null || bytecode.Length == 0)
                throw new ArgumentNullException(nameof(bytecode));

            fixed (byte* pBytecode = bytecode)
            {
                int hr = ComVTableHelper.CreatePixelShader(Handle, (IntPtr)pBytecode, (UIntPtr)bytecode.Length, IntPtr.Zero, out IntPtr ppShader);
                if (hr < 0 || ppShader == IntPtr.Zero)
                    throw new COMException("Failed to create D3D11PixelShader.", hr);

                return new D3D11PixelShader(ppShader);
            }
        }

        public unsafe D3D11InputLayout CreateInputLayout(D3D11_INPUT_ELEMENT_DESC[] descs, byte[] shaderBytecode)
        {
            if (descs == null) throw new ArgumentNullException(nameof(descs));
            if (shaderBytecode == null) throw new ArgumentNullException(nameof(shaderBytecode));

            fixed (byte* pBytecode = shaderBytecode)
            {
                int hr = ComVTableHelper.CreateInputLayout(
                    Handle, descs, (uint)descs.Length, (IntPtr)pBytecode, (UIntPtr)shaderBytecode.Length, out IntPtr ppLayout);
                if (hr < 0 || ppLayout == IntPtr.Zero)
                    throw new COMException("Failed to create D3D11InputLayout.", hr);

                return new D3D11InputLayout(ppLayout);
            }
        }

        public D3D11BlendState CreateBlendState(ref D3D11_BLEND_DESC desc)
        {
            int hr = ComVTableHelper.CreateBlendState(Handle, ref desc, out IntPtr ppBlend);
            if (hr < 0 || ppBlend == IntPtr.Zero)
                throw new COMException("Failed to create D3D11BlendState.", hr);

            return new D3D11BlendState(ppBlend);
        }

        public D3D11RasterizerState CreateRasterizerState(ref D3D11_RASTERIZER_DESC desc)
        {
            int hr = ComVTableHelper.CreateRasterizerState(Handle, ref desc, out IntPtr ppRaster);
            if (hr < 0 || ppRaster == IntPtr.Zero)
                throw new COMException("Failed to create D3D11RasterizerState.", hr);

            return new D3D11RasterizerState(ppRaster);
        }

        public int GetDeviceRemovedReason()
        {
            return ComVTableHelper.GetDeviceRemovedReason(Handle);
        }
    }

    public sealed class D3D11DeviceContext : ComObjectWrapper
    {
        public D3D11DeviceContext(IntPtr handle) : base(handle) { }

        public void ClearRenderTargetView(D3D11RenderTargetView rtv, float[] colorRGBA)
        {
            if (rtv == null || !rtv.IsValid) return;
            ComVTableHelper.ClearRenderTargetView(Handle, rtv.Handle, colorRGBA);
        }

        public void OMSetRenderTargets(D3D11RenderTargetView rtv, IntPtr dsv = default)
        {
            if (rtv == null || !rtv.IsValid)
            {
                ComVTableHelper.OMSetRenderTargets(Handle, 0, Array.Empty<IntPtr>(), dsv);
                return;
            }
            ComVTableHelper.OMSetRenderTargets(Handle, 1, new[] { rtv.Handle }, dsv);
        }

        public void OMSetBlendState(D3D11BlendState? blendState, float[]? blendFactor = null, uint sampleMask = 0xFFFFFFFF)
        {
            ComVTableHelper.OMSetBlendState(Handle, blendState?.Handle ?? IntPtr.Zero, blendFactor, sampleMask);
        }

        public void RSSetState(D3D11RasterizerState? state)
        {
            ComVTableHelper.RSSetState(Handle, state?.Handle ?? IntPtr.Zero);
        }

        public void RSSetViewports(params D3D11_VIEWPORT[] viewports)
        {
            if (viewports == null || viewports.Length == 0) return;
            ComVTableHelper.RSSetViewports(Handle, (uint)viewports.Length, viewports);
        }

        public void IASetInputLayout(D3D11InputLayout layout)
        {
            ComVTableHelper.IASetInputLayout(Handle, layout?.Handle ?? IntPtr.Zero);
        }

        public void IASetVertexBuffers(uint startSlot, D3D11Buffer buffer, uint stride, uint offset)
        {
            if (buffer == null || !buffer.IsValid) return;
            ComVTableHelper.IASetVertexBuffers(Handle, startSlot, 1, new[] { buffer.Handle }, new[] { stride }, new[] { offset });
        }

        public void IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY topology)
        {
            ComVTableHelper.IASetPrimitiveTopology(Handle, topology);
        }

        public void VSSetShader(D3D11VertexShader shader)
        {
            ComVTableHelper.VSSetShader(Handle, shader?.Handle ?? IntPtr.Zero);
        }

        public void VSSetConstantBuffers(uint startSlot, D3D11Buffer buffer)
        {
            ComVTableHelper.VSSetConstantBuffers(Handle, startSlot, 1, new[] { buffer?.Handle ?? IntPtr.Zero });
        }

        public void PSSetShader(D3D11PixelShader shader)
        {
            ComVTableHelper.PSSetShader(Handle, shader?.Handle ?? IntPtr.Zero);
        }

        public void PSSetConstantBuffers(uint startSlot, D3D11Buffer buffer)
        {
            ComVTableHelper.PSSetConstantBuffers(Handle, startSlot, 1, new[] { buffer?.Handle ?? IntPtr.Zero });
        }

        public unsafe void UpdateSubresource<T>(D3D11Buffer dstBuffer, ref T data) where T : unmanaged
        {
            if (dstBuffer == null || !dstBuffer.IsValid) return;
            fixed (T* pData = &data)
            {
                ComVTableHelper.UpdateSubresource(Handle, dstBuffer.Handle, 0, IntPtr.Zero, (IntPtr)pData, 0, 0);
            }
        }

        public void Draw(uint vertexCount, uint startVertexLocation = 0)
        {
            ComVTableHelper.Draw(Handle, vertexCount, startVertexLocation);
        }
    }

    public sealed class DxgiSwapChain : ComObjectWrapper
    {
        public DxgiSwapChain(IntPtr handle) : base(handle) { }

        public int Present(uint syncInterval = 0, uint flags = 0)
        {
            return ComVTableHelper.Present(Handle, syncInterval, flags);
        }

        public IntPtr GetBuffer(uint bufferIndex, ref Guid riid)
        {
            int hr = ComVTableHelper.GetBuffer(Handle, bufferIndex, ref riid, out IntPtr ppSurface);
            if (hr < 0 || ppSurface == IntPtr.Zero)
                throw new COMException("Failed to get swapchain backbuffer.", hr);
            return ppSurface;
        }

        public int ResizeBuffers(uint bufferCount, uint width, uint height, DXGI_FORMAT format, uint flags = 0)
        {
            return ComVTableHelper.ResizeBuffers(Handle, bufferCount, width, height, format, flags);
        }
    }

    public sealed class D3D11RenderTargetView : ComObjectWrapper
    {
        public D3D11RenderTargetView(IntPtr handle) : base(handle) { }
    }

    public sealed class D3D11Buffer : ComObjectWrapper
    {
        public D3D11_BUFFER_DESC Description { get; }
        public D3D11Buffer(IntPtr handle, D3D11_BUFFER_DESC desc) : base(handle)
        {
            Description = desc;
        }
    }

    public sealed class D3D11VertexShader : ComObjectWrapper
    {
        public D3D11VertexShader(IntPtr handle) : base(handle) { }
    }

    public sealed class D3D11PixelShader : ComObjectWrapper
    {
        public D3D11PixelShader(IntPtr handle) : base(handle) { }
    }

    public sealed class D3D11InputLayout : ComObjectWrapper
    {
        public D3D11InputLayout(IntPtr handle) : base(handle) { }
    }

    public sealed class D3D11BlendState : ComObjectWrapper
    {
        public D3D11BlendState(IntPtr handle) : base(handle) { }
    }

    public sealed class D3D11RasterizerState : ComObjectWrapper
    {
        public D3D11RasterizerState(IntPtr handle) : base(handle) { }
    }
}
