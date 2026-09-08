using System;
using System.Runtime.InteropServices;

namespace ZeroUI.Graphics.DirectX.Native
{
    /// <summary>
    /// High-performance COM VTable invoker.
    /// Directly dispatches DirectX COM interface methods by slot index without heavy external COM dependencies.
    /// </summary>
    public static unsafe class ComVTableHelper
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate uint ReleaseDelegate(IntPtr thisPtr);

        public static uint Release(IntPtr comPtr)
        {
            if (comPtr == IntPtr.Zero) return 0;
            IntPtr methodPtr = (*(IntPtr**)comPtr)[2];
            return Marshal.GetDelegateForFunctionPointer<ReleaseDelegate>(methodPtr)(comPtr);
        }

        // =========================================================================
        // IDXGIFactory / IDXGIFactory1
        // =========================================================================

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CreateSwapChainDelegate(
            IntPtr thisPtr,
            IntPtr pDevice,
            ref DXGI_SWAP_CHAIN_DESC pDesc,
            out IntPtr ppSwapChain);

        public static int CreateSwapChain(IntPtr factory, IntPtr pDevice, ref DXGI_SWAP_CHAIN_DESC desc, out IntPtr ppSwapChain)
        {
            IntPtr methodPtr = (*(IntPtr**)factory)[10];
            return Marshal.GetDelegateForFunctionPointer<CreateSwapChainDelegate>(methodPtr)(factory, pDevice, ref desc, out ppSwapChain);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int EnumAdapters1Delegate(
            IntPtr thisPtr,
            uint adapterIndex,
            out IntPtr ppAdapter);

        public static int EnumAdapters1(IntPtr factory1, uint adapterIndex, out IntPtr ppAdapter)
        {
            IntPtr methodPtr = (*(IntPtr**)factory1)[12];
            return Marshal.GetDelegateForFunctionPointer<EnumAdapters1Delegate>(methodPtr)(factory1, adapterIndex, out ppAdapter);
        }

        // =========================================================================
        // IDXGIAdapter1
        // =========================================================================

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetDesc1Delegate(
            IntPtr thisPtr,
            out DXGI_ADAPTER_DESC1 pDesc);

        public static int GetDesc1(IntPtr adapter1, out DXGI_ADAPTER_DESC1 desc)
        {
            IntPtr methodPtr = (*(IntPtr**)adapter1)[10];
            return Marshal.GetDelegateForFunctionPointer<GetDesc1Delegate>(methodPtr)(adapter1, out desc);
        }

        // =========================================================================
        // IDXGISwapChain
        // =========================================================================

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int PresentDelegate(IntPtr thisPtr, uint syncInterval, uint flags);

        public static int Present(IntPtr swapChain, uint syncInterval, uint flags)
        {
            IntPtr methodPtr = (*(IntPtr**)swapChain)[8];
            return Marshal.GetDelegateForFunctionPointer<PresentDelegate>(methodPtr)(swapChain, syncInterval, flags);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetBufferDelegate(IntPtr thisPtr, uint bufferIndex, ref Guid riid, out IntPtr ppSurface);

        public static int GetBuffer(IntPtr swapChain, uint bufferIndex, ref Guid riid, out IntPtr ppSurface)
        {
            IntPtr methodPtr = (*(IntPtr**)swapChain)[9];
            return Marshal.GetDelegateForFunctionPointer<GetBufferDelegate>(methodPtr)(swapChain, bufferIndex, ref riid, out ppSurface);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int ResizeBuffersDelegate(
            IntPtr thisPtr,
            uint bufferCount,
            uint width,
            uint height,
            DXGI_FORMAT newFormat,
            uint swapChainFlags);

        public static int ResizeBuffers(IntPtr swapChain, uint bufferCount, uint width, uint height, DXGI_FORMAT newFormat, uint flags)
        {
            IntPtr methodPtr = (*(IntPtr**)swapChain)[13];
            return Marshal.GetDelegateForFunctionPointer<ResizeBuffersDelegate>(methodPtr)(swapChain, bufferCount, width, height, newFormat, flags);
        }

        // =========================================================================
        // ID3D11Device
        // =========================================================================

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CreateBufferDelegate(
            IntPtr thisPtr,
            ref D3D11_BUFFER_DESC pDesc,
            IntPtr pInitialData,
            out IntPtr ppBuffer);

        public static int CreateBuffer(IntPtr device, ref D3D11_BUFFER_DESC desc, IntPtr initialData, out IntPtr ppBuffer)
        {
            IntPtr methodPtr = (*(IntPtr**)device)[3];
            return Marshal.GetDelegateForFunctionPointer<CreateBufferDelegate>(methodPtr)(device, ref desc, initialData, out ppBuffer);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CreateRenderTargetViewDelegate(
            IntPtr thisPtr,
            IntPtr pResource,
            IntPtr pDesc,
            out IntPtr ppRTView);

        public static int CreateRenderTargetView(IntPtr device, IntPtr resource, IntPtr desc, out IntPtr ppRtv)
        {
            IntPtr methodPtr = (*(IntPtr**)device)[9];
            return Marshal.GetDelegateForFunctionPointer<CreateRenderTargetViewDelegate>(methodPtr)(device, resource, desc, out ppRtv);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CreateInputLayoutDelegate(
            IntPtr thisPtr,
            [In] D3D11_INPUT_ELEMENT_DESC[] pInputElementDescs,
            uint numElements,
            IntPtr pShaderBytecodeWithInputSignature,
            UIntPtr bytecodeLength,
            out IntPtr ppInputLayout);

        public static int CreateInputLayout(
            IntPtr device,
            D3D11_INPUT_ELEMENT_DESC[] descs,
            uint numElements,
            IntPtr bytecode,
            UIntPtr bytecodeLength,
            out IntPtr ppInputLayout)
        {
            IntPtr methodPtr = (*(IntPtr**)device)[11];
            return Marshal.GetDelegateForFunctionPointer<CreateInputLayoutDelegate>(methodPtr)(
                device, descs, numElements, bytecode, bytecodeLength, out ppInputLayout);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CreateVertexShaderDelegate(
            IntPtr thisPtr,
            IntPtr pShaderBytecode,
            UIntPtr bytecodeLength,
            IntPtr pClassLinkage,
            out IntPtr ppVertexShader);

        public static int CreateVertexShader(
            IntPtr device,
            IntPtr bytecode,
            UIntPtr bytecodeLength,
            IntPtr classLinkage,
            out IntPtr ppVertexShader)
        {
            IntPtr methodPtr = (*(IntPtr**)device)[12];
            return Marshal.GetDelegateForFunctionPointer<CreateVertexShaderDelegate>(methodPtr)(
                device, bytecode, bytecodeLength, classLinkage, out ppVertexShader);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CreatePixelShaderDelegate(
            IntPtr thisPtr,
            IntPtr pShaderBytecode,
            UIntPtr bytecodeLength,
            IntPtr pClassLinkage,
            out IntPtr ppPixelShader);

        public static int CreatePixelShader(
            IntPtr device,
            IntPtr bytecode,
            UIntPtr bytecodeLength,
            IntPtr classLinkage,
            out IntPtr ppPixelShader)
        {
            IntPtr methodPtr = (*(IntPtr**)device)[15];
            return Marshal.GetDelegateForFunctionPointer<CreatePixelShaderDelegate>(methodPtr)(
                device, bytecode, bytecodeLength, classLinkage, out ppPixelShader);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CreateBlendStateDelegate(
            IntPtr thisPtr,
            ref D3D11_BLEND_DESC pBlendStateDesc,
            out IntPtr ppBlendState);

        public static int CreateBlendState(IntPtr device, ref D3D11_BLEND_DESC desc, out IntPtr ppBlendState)
        {
            IntPtr methodPtr = (*(IntPtr**)device)[20];
            return Marshal.GetDelegateForFunctionPointer<CreateBlendStateDelegate>(methodPtr)(device, ref desc, out ppBlendState);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CreateRasterizerStateDelegate(
            IntPtr thisPtr,
            ref D3D11_RASTERIZER_DESC pRasterizerDesc,
            out IntPtr ppRasterizerState);

        public static int CreateRasterizerState(IntPtr device, ref D3D11_RASTERIZER_DESC desc, out IntPtr ppRasterizerState)
        {
            IntPtr methodPtr = (*(IntPtr**)device)[22];
            return Marshal.GetDelegateForFunctionPointer<CreateRasterizerStateDelegate>(methodPtr)(device, ref desc, out ppRasterizerState);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetDeviceRemovedReasonDelegate(IntPtr thisPtr);

        public static int GetDeviceRemovedReason(IntPtr device)
        {
            IntPtr methodPtr = (*(IntPtr**)device)[39];
            return Marshal.GetDelegateForFunctionPointer<GetDeviceRemovedReasonDelegate>(methodPtr)(device);
        }

        // =========================================================================
        // ID3D11DeviceContext
        // =========================================================================

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void VSSetConstantBuffersDelegate(
            IntPtr thisPtr,
            uint startSlot,
            uint numBuffers,
            [In] IntPtr[] ppConstantBuffers);

        public static void VSSetConstantBuffers(IntPtr context, uint startSlot, uint numBuffers, IntPtr[] buffers)
        {
            IntPtr methodPtr = (*(IntPtr**)context)[3];
            Marshal.GetDelegateForFunctionPointer<VSSetConstantBuffersDelegate>(methodPtr)(context, startSlot, numBuffers, buffers);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void PSSetShaderDelegate(
            IntPtr thisPtr,
            IntPtr pPixelShader,
            [In] IntPtr[]? ppClassInstances,
            uint numClassInstances);

        public static void PSSetShader(IntPtr context, IntPtr pixelShader)
        {
            IntPtr methodPtr = (*(IntPtr**)context)[5];
            Marshal.GetDelegateForFunctionPointer<PSSetShaderDelegate>(methodPtr)(context, pixelShader, null, 0);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void VSSetShaderDelegate(
            IntPtr thisPtr,
            IntPtr pVertexShader,
            [In] IntPtr[]? ppClassInstances,
            uint numClassInstances);

        public static void VSSetShader(IntPtr context, IntPtr vertexShader)
        {
            IntPtr methodPtr = (*(IntPtr**)context)[7];
            Marshal.GetDelegateForFunctionPointer<VSSetShaderDelegate>(methodPtr)(context, vertexShader, null, 0);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void DrawDelegate(IntPtr thisPtr, uint vertexCount, uint startVertexLocation);

        public static void Draw(IntPtr context, uint vertexCount, uint startVertexLocation)
        {
            IntPtr methodPtr = (*(IntPtr**)context)[9];
            Marshal.GetDelegateForFunctionPointer<DrawDelegate>(methodPtr)(context, vertexCount, startVertexLocation);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void PSSetConstantBuffersDelegate(
            IntPtr thisPtr,
            uint startSlot,
            uint numBuffers,
            [In] IntPtr[] ppConstantBuffers);

        public static void PSSetConstantBuffers(IntPtr context, uint startSlot, uint numBuffers, IntPtr[] buffers)
        {
            IntPtr methodPtr = (*(IntPtr**)context)[12];
            Marshal.GetDelegateForFunctionPointer<PSSetConstantBuffersDelegate>(methodPtr)(context, startSlot, numBuffers, buffers);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void IASetInputLayoutDelegate(IntPtr thisPtr, IntPtr pInputLayout);

        public static void IASetInputLayout(IntPtr context, IntPtr inputLayout)
        {
            IntPtr methodPtr = (*(IntPtr**)context)[13];
            Marshal.GetDelegateForFunctionPointer<IASetInputLayoutDelegate>(methodPtr)(context, inputLayout);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void IASetVertexBuffersDelegate(
            IntPtr thisPtr,
            uint startSlot,
            uint numBuffers,
            [In] IntPtr[] ppVertexBuffers,
            [In] uint[] pStrides,
            [In] uint[] pOffsets);

        public static void IASetVertexBuffers(IntPtr context, uint startSlot, uint numBuffers, IntPtr[] buffers, uint[] strides, uint[] offsets)
        {
            IntPtr methodPtr = (*(IntPtr**)context)[14];
            Marshal.GetDelegateForFunctionPointer<IASetVertexBuffersDelegate>(methodPtr)(context, startSlot, numBuffers, buffers, strides, offsets);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void IASetPrimitiveTopologyDelegate(IntPtr thisPtr, D3D11_PRIMITIVE_TOPOLOGY topology);

        public static void IASetPrimitiveTopology(IntPtr context, D3D11_PRIMITIVE_TOPOLOGY topology)
        {
            IntPtr methodPtr = (*(IntPtr**)context)[20];
            Marshal.GetDelegateForFunctionPointer<IASetPrimitiveTopologyDelegate>(methodPtr)(context, topology);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void OMSetRenderTargetsDelegate(
            IntPtr thisPtr,
            uint numViews,
            [In] IntPtr[] ppRenderTargetViews,
            IntPtr pDepthStencilView);

        public static void OMSetRenderTargets(IntPtr context, uint numViews, IntPtr[] rtv, IntPtr dsv)
        {
            IntPtr methodPtr = (*(IntPtr**)context)[29];
            Marshal.GetDelegateForFunctionPointer<OMSetRenderTargetsDelegate>(methodPtr)(context, numViews, rtv, dsv);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void OMSetBlendStateDelegate(
            IntPtr thisPtr,
            IntPtr pBlendState,
            [In] float[]? blendFactor,
            uint sampleMask);

        public static void OMSetBlendState(IntPtr context, IntPtr blendState, float[]? blendFactor, uint sampleMask = 0xFFFFFFFF)
        {
            IntPtr methodPtr = (*(IntPtr**)context)[31];
            Marshal.GetDelegateForFunctionPointer<OMSetBlendStateDelegate>(methodPtr)(context, blendState, blendFactor, sampleMask);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void RSSetStateDelegate(IntPtr thisPtr, IntPtr pRasterizerState);

        public static void RSSetState(IntPtr context, IntPtr rasterizerState)
        {
            IntPtr methodPtr = (*(IntPtr**)context)[39];
            Marshal.GetDelegateForFunctionPointer<RSSetStateDelegate>(methodPtr)(context, rasterizerState);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void RSSetViewportsDelegate(
            IntPtr thisPtr,
            uint numViewports,
            [In] D3D11_VIEWPORT[] pViewports);

        public static void RSSetViewports(IntPtr context, uint numViewports, D3D11_VIEWPORT[] viewports)
        {
            IntPtr methodPtr = (*(IntPtr**)context)[40];
            Marshal.GetDelegateForFunctionPointer<RSSetViewportsDelegate>(methodPtr)(context, numViewports, viewports);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void UpdateSubresourceDelegate(
            IntPtr thisPtr,
            IntPtr pDstResource,
            uint dstSubresource,
            IntPtr pDstBox,
            IntPtr pSrcData,
            uint srcRowPitch,
            uint srcDepthPitch);

        public static void UpdateSubresource(IntPtr context, IntPtr dstResource, uint dstSubresource, IntPtr pDstBox, IntPtr pSrcData, uint srcRowPitch, uint srcDepthPitch)
        {
            IntPtr methodPtr = (*(IntPtr**)context)[44];
            Marshal.GetDelegateForFunctionPointer<UpdateSubresourceDelegate>(methodPtr)(context, dstResource, dstSubresource, pDstBox, pSrcData, srcRowPitch, srcDepthPitch);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void ClearRenderTargetViewDelegate(
            IntPtr thisPtr,
            IntPtr pRenderTargetView,
            [In] float[] colorRGBA);

        public static void ClearRenderTargetView(IntPtr context, IntPtr rtv, float[] colorRGBA)
        {
            IntPtr methodPtr = (*(IntPtr**)context)[46];
            Marshal.GetDelegateForFunctionPointer<ClearRenderTargetViewDelegate>(methodPtr)(context, rtv, colorRGBA);
        }
    }
}
