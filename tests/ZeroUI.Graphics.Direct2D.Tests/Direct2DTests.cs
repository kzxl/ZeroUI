using System;
using Xunit;
using ZeroUI.Graphics.Direct2D.Controls;
using ZeroUI.Graphics.Direct2D.Core;
using ZeroUI.Graphics.Direct2D.Native;

namespace ZeroUI.Graphics.Direct2D.Tests
{
    public class Direct2DTests
    {
        [Fact]
        public void D2DFactory_InitializesSingletonSuccessfully()
        {
            var factory = D2DFactory.Default;
            Assert.NotNull(factory);
            Assert.True(factory.IsValid);
        }

        [Fact]
        public void DWriteFactory_CreatesTextFormatSuccessfully()
        {
            var factory = DWriteFactory.Default;
            Assert.NotNull(factory);
            Assert.True(factory.IsValid);

            using (var format = factory.CreateTextFormat("Segoe UI", 16f, DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_BOLD))
            {
                Assert.NotNull(format);
                Assert.True(format.IsValid);
                Assert.Equal("Segoe UI", format.FontFamilyName);
                Assert.Equal(16f, format.FontSize);
            }
        }

        [Fact]
        public void ZeroDirect2DCanvas_DefaultPropertiesAndConfiguration()
        {
            using (var canvas = new ZeroDirect2DCanvas())
            {
                Assert.Equal("Segoe UI", canvas.TextFontFamily);
                Assert.Equal(14f, canvas.TextSize);
                Assert.Contains("DirectWrite", canvas.SampleText);

                canvas.TextFontFamily = "Consolas";
                canvas.TextSize = 20f;
                canvas.SampleText = "High-DPI Benchmark";

                Assert.Equal("Consolas", canvas.TextFontFamily);
                Assert.Equal(20f, canvas.TextSize);
                Assert.Equal("High-DPI Benchmark", canvas.SampleText);
            }
        }
    }
}
