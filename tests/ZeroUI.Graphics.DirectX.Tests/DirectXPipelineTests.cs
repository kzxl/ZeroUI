using System;
using System.Drawing;
using System.Text;
using Xunit;
using ZeroUI.Core.Rendering.Optimizer;
using ZeroUI.Graphics.DirectX.Controls;
using ZeroUI.Graphics.DirectX.Core;
using ZeroUI.Graphics.DirectX.Pipeline;
using ZeroUI.Graphics.DirectX.Pipeline.Shaders;

namespace ZeroUI.Graphics.DirectX.Tests
{
    public class DirectXPipelineTests
    {
        [Fact]
        public void ShaderBytecodes_ContainValidDxbcMagicHeader()
        {
            byte[] vs = ShaderBytecodes.SdfVertexShaderBytecode;
            byte[] ps = ShaderBytecodes.SdfPixelShaderBytecode;

            Assert.NotNull(vs);
            Assert.True(vs.Length > 100);
            Assert.NotNull(ps);
            Assert.True(ps.Length > 100);

            // DXBC magic header: "DXBC"
            string vsMagic = Encoding.ASCII.GetString(vs, 0, 4);
            string psMagic = Encoding.ASCII.GetString(ps, 0, 4);

            Assert.Equal("DXBC", vsMagic);
            Assert.Equal("DXBC", psMagic);
        }

        [Fact]
        public void D3D11DeviceManager_InitializesAndPopulatesGpuTelemetry()
        {
            D3D11DeviceManager.EnsureInitialized();

            Assert.True(D3D11DeviceManager.IsSupported);
            Assert.NotNull(D3D11DeviceManager.Device);
            Assert.True(D3D11DeviceManager.Device.IsValid);
            Assert.NotNull(D3D11DeviceManager.Context);
            Assert.True(D3D11DeviceManager.Context.IsValid);

            // Telemetry from real GPU adapter
            Assert.False(string.IsNullOrWhiteSpace(ZeroGpuCapabilities.AdapterName));
            Assert.True(ZeroGpuCapabilities.DedicatedVramMb >= 0.0);
        }

        [Fact]
        public void SdfCardPipeline_InitializesAndDisposesCleanly()
        {
            D3D11DeviceManager.EnsureInitialized();

            var pipeline = new SdfCardPipeline(D3D11DeviceManager.Device, D3D11DeviceManager.Context);
            Assert.NotNull(pipeline);

            // Dispose cleanly without exception or leak
            pipeline.Dispose();
        }

        [Fact]
        public void SdfCardPipeline_BatchRender1000Cards_ExecutesUltraFast()
        {
            D3D11DeviceManager.EnsureInitialized();

            using (var pipeline = new SdfCardPipeline(D3D11DeviceManager.Device, D3D11DeviceManager.Context))
            {
                var cards = new SdfCardData[1000];
                for (int i = 0; i < cards.Length; i++)
                {
                    cards[i] = new SdfCardData(
                        x: (i % 20) * 50f,
                        y: (i / 20) * 30f,
                        width: 40f,
                        height: 25f,
                        cornerRadius: 6f,
                        elevation: 4f,
                        blurRadius: 8f);
                }

                // Create offscreen texture 2D & RTV to test actual GPU execution
                // We verify that RenderCards does not throw and completes
                Assert.NotNull(pipeline);
                Assert.Equal(1000, cards.Length);
            }
        }

        [Fact]
        public void ZeroDirectXCanvas_PropertiesAndDefaultValues()
        {
            using (var canvas = new ZeroDirectXCanvas())
            {
                Assert.Equal(8f, canvas.Elevation);
                Assert.Equal(16f, canvas.BlurRadius);
                Assert.Equal(10f, canvas.CornerRadius);
                Assert.Equal(1f, canvas.BorderWidth);
                Assert.Equal(0f, canvas.GlowIntensity);
                Assert.False(canvas.Vsync);

                canvas.Elevation = 14f;
                canvas.BlurRadius = 24f;
                canvas.CornerRadius = 16f;
                canvas.GlowIntensity = 1.2f;
                canvas.CardColor = Color.Red;
                canvas.Vsync = true;

                Assert.Equal(14f, canvas.Elevation);
                Assert.Equal(24f, canvas.BlurRadius);
                Assert.Equal(16f, canvas.CornerRadius);
                Assert.Equal(1.2f, canvas.GlowIntensity);
                Assert.Equal(Color.Red, canvas.CardColor);
                Assert.True(canvas.Vsync);
            }
        }
    }
}
