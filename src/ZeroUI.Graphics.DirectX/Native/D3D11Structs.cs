using System;
using System.Runtime.InteropServices;

namespace ZeroUI.Graphics.DirectX.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct D3D11_VIEWPORT
    {
        public float TopLeftX;
        public float TopLeftY;
        public float Width;
        public float Height;
        public float MinDepth;
        public float MaxDepth;

        public D3D11_VIEWPORT(float x, float y, float width, float height, float minDepth = 0.0f, float maxDepth = 1.0f)
        {
            TopLeftX = x;
            TopLeftY = y;
            Width = width;
            Height = height;
            MinDepth = minDepth;
            MaxDepth = maxDepth;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D3D11_BUFFER_DESC
    {
        public uint ByteWidth;
        public D3D11_USAGE Usage;
        public D3D11_BIND_FLAG BindFlags;
        public D3D11_CPU_ACCESS_FLAG CPUAccessFlags;
        public uint MiscFlags;
        public uint StructureByteStride;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D3D11_SUBRESOURCE_DATA
    {
        public IntPtr pSysMem;
        public uint SysMemPitch;
        public uint SysMemSlicePitch;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D3D11_INPUT_ELEMENT_DESC
    {
        public IntPtr SemanticName;
        public uint SemanticIndex;
        public DXGI_FORMAT Format;
        public uint InputSlot;
        public uint AlignedByteOffset;
        public int InputSlotClass;
        public uint InstanceDataStepRate;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D3D11_MAPPED_SUBRESOURCE
    {
        public IntPtr pData;
        public uint RowPitch;
        public uint DepthPitch;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D3D11_RENDER_TARGET_BLEND_DESC
    {
        public int BlendEnable; // BOOL
        public D3D11_BLEND SrcBlend;
        public D3D11_BLEND DestBlend;
        public D3D11_BLEND_OP BlendOp;
        public D3D11_BLEND SrcBlendAlpha;
        public D3D11_BLEND DestBlendAlpha;
        public D3D11_BLEND_OP BlendOpAlpha;
        public byte RenderTargetWriteMask;
        private byte _pad0;
        private byte _pad1;
        private byte _pad2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D3D11_BLEND_DESC
    {
        public int AlphaToCoverageEnable;   // BOOL
        public int IndependentBlendEnable; // BOOL
        public D3D11_RENDER_TARGET_BLEND_DESC RenderTarget0;
        public D3D11_RENDER_TARGET_BLEND_DESC RenderTarget1;
        public D3D11_RENDER_TARGET_BLEND_DESC RenderTarget2;
        public D3D11_RENDER_TARGET_BLEND_DESC RenderTarget3;
        public D3D11_RENDER_TARGET_BLEND_DESC RenderTarget4;
        public D3D11_RENDER_TARGET_BLEND_DESC RenderTarget5;
        public D3D11_RENDER_TARGET_BLEND_DESC RenderTarget6;
        public D3D11_RENDER_TARGET_BLEND_DESC RenderTarget7;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D3D11_RASTERIZER_DESC
    {
        public D3D11_FILL_MODE FillMode;
        public D3D11_CULL_MODE CullMode;
        public int FrontCounterClockwise;  // BOOL
        public int DepthBias;
        public float DepthBiasClamp;
        public float SlopeScaledDepthBias;
        public int DepthClipEnable;        // BOOL
        public int ScissorEnable;          // BOOL
        public int MultisampleEnable;      // BOOL
        public int AntialiasedLineEnable;  // BOOL
    }
}
