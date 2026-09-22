using System.Drawing;
using System.Windows.Forms;
using Xunit;
using ZeroUI.Core.Common;
using ZeroUI.Core.Data;
using ZeroUI.WinForms.Base;
using ZeroUI.WinForms.DataGrid;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.Desktop.Tests
{
    public class HighDpiScalingTests
    {
        [Fact]
        public void ZeroDpi_ScalingCalculations_ShouldBeAccurate()
        {
            Assert.Equal(26, ZeroDpi.Scale(26, 1.0f));
            Assert.Equal(39, ZeroDpi.Scale(26, 1.5f));
            Assert.Equal(52, ZeroDpi.Scale(26, 2.0f));
            Assert.Equal(56, ZeroDpi.Scale(28, 2.0f));

            var size = ZeroDpi.Scale(new Size(100, 50), 2.0f);
            Assert.Equal(200, size.Width);
            Assert.Equal(100, size.Height);

            var rect = ZeroDpi.Scale(new Rectangle(10, 20, 100, 50), 2.0f);
            Assert.Equal(20, rect.X);
            Assert.Equal(40, rect.Y);
            Assert.Equal(200, rect.Width);
            Assert.Equal(100, rect.Height);

            var pad = ZeroDpi.Scale(new Padding(5, 10, 15, 20), 2.0f);
            Assert.Equal(10, pad.Left);
            Assert.Equal(20, pad.Top);
            Assert.Equal(30, pad.Right);
            Assert.Equal(40, pad.Bottom);
        }

        [Fact]
        public void GridControl_ApplyDpiScaling_ScalesRowAndHeaderMetrics()
        {
            StaTestRunner.Run(() =>
            {
                using var grid = new GridControl();
                Assert.Equal(26, grid.RowHeight);
                Assert.Equal(28, grid.HeaderHeight);

                grid.Columns.Add(new ZeroColumn("Code", "Code", 100));

                // Scale to 200% (e.g. 192 DPI 4K monitor)
                grid.ApplyDpiScaling(2.0f);

                Assert.Equal(52, grid.RowHeight);
                Assert.Equal(56, grid.HeaderHeight);
                Assert.Equal(200, grid.Columns[0].Width);
                Assert.Equal(2.0f, grid.DpiScale);
            });
        }

        [Fact]
        public void GridControl_DensityAdjustment_RespectsActiveDpiScale()
        {
            StaTestRunner.Run(() =>
            {
                using var grid = new GridControl();
                grid.ApplyDpiScaling(2.0f);

                // Compact density: 24 base * 2.0 = 48
                grid.Density = GridDensity.Compact;
                Assert.Equal(48, grid.RowHeight);

                // Loose density: 36 base * 2.0 = 72
                grid.Density = GridDensity.Loose;
                Assert.Equal(72, grid.RowHeight);
            });
        }

        [Fact]
        public void BaseForm_And_BaseUserControl_HaveDpiAutoScaleMode()
        {
            StaTestRunner.Run(() =>
            {
                using var form = new BaseForm();
                Assert.Equal(AutoScaleMode.Dpi, form.AutoScaleMode);
                Assert.Equal(new SizeF(96F, 96F), form.AutoScaleDimensions);

                using var uc = new BaseUserControl();
                Assert.Equal(AutoScaleMode.Dpi, uc.AutoScaleMode);
                Assert.Equal(new SizeF(96F, 96F), uc.AutoScaleDimensions);
            });
        }
    }
}
