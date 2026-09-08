using System;
using System.Drawing;
using System.Text;
using Xunit;
using ZeroUI.Graphics.DirectX.Core;
using ZeroUI.Graphics.Waveform.Controls;
using ZeroUI.Graphics.Waveform.Pipeline;
using ZeroUI.Graphics.Waveform.Pipeline.Shaders;

namespace ZeroUI.Graphics.Waveform.Tests
{
    public class WaveformTests
    {
        [Fact]
        public void WaveformBytecodes_ContainValidDxbcHeader()
        {
            byte[] vs = WaveformBytecodes.WaveformVertexShaderBytecode;
            byte[] ps = WaveformBytecodes.WaveformPixelShaderBytecode;

            Assert.NotNull(vs);
            Assert.True(vs.Length > 100);
            Assert.NotNull(ps);
            Assert.True(ps.Length > 100);

            string vsMagic = Encoding.ASCII.GetString(vs, 0, 4);
            string psMagic = Encoding.ASCII.GetString(ps, 0, 4);

            Assert.Equal("DXBC", vsMagic);
            Assert.Equal("DXBC", psMagic);
        }

        [Fact]
        public void WaveformPipeline_InitializesAndDisposesCleanly()
        {
            D3D11DeviceManager.EnsureInitialized();

            var pipeline = new WaveformPipeline(D3D11DeviceManager.Device, D3D11DeviceManager.Context);
            Assert.NotNull(pipeline);

            pipeline.Dispose();
        }

        [Fact]
        public void ZeroWaveformCanvas_SetData_ComputesAutoScaleAccurately()
        {
            using (var canvas = new ZeroWaveformCanvas())
            {
                Assert.True(canvas.AutoScale);
                Assert.Equal(Color.FromArgb(0, 255, 136), canvas.TraceColor);

                float[] data = new float[] { -25.5f, 10.0f, 0.0f, 48.2f, -12.0f };
                canvas.SetData(data);

                Assert.Equal(-25.5f, canvas.MinY);
                Assert.Equal(48.2f, canvas.MaxY);
            }
        }

        [Fact]
        public void ZeroWaveformCanvas_HandlesMassivePointSeries()
        {
            using (var canvas = new ZeroWaveformCanvas())
            {
                // 100,000 synthetic sine wave telemetry points
                float[] massiveData = new float[100000];
                for (int i = 0; i < massiveData.Length; i++)
                {
                    massiveData[i] = (float)Math.Sin(i * 0.05);
                }

                canvas.SetData(massiveData);

                Assert.True(canvas.MinY >= -1.05f && canvas.MinY <= -0.95f);
                Assert.True(canvas.MaxY >= 0.95f && canvas.MaxY <= 1.05f);
            }
        }
    }
}
