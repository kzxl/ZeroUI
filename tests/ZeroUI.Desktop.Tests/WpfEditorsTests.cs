using System;
using System.Windows;
using Xunit;
using ZeroUI.Wpf.Editors;
using ZeroUI.Wpf.DataGrid;

namespace ZeroUI.Desktop.Tests
{
    public class WpfEditorsTests
    {
        [Fact]
        public void HistogramScopeControl_DefaultState_ShouldHaveValidProperties()
        {
            StaTestRunner.Run(() =>
            {
                var scope = new HistogramScopeControl();
                Assert.Equal(HistogramChannelMode.Rgb, scope.ChannelMode);
                Assert.Equal(HistogramScopeType.Histogram, scope.ScopeType);
                Assert.Null(scope.RedChannel);
                Assert.Null(scope.GreenChannel);
                Assert.Null(scope.BlueChannel);
                Assert.Null(scope.LumaChannel);
                Assert.Equal(0.0, scope.ShadowClipPercent);
                Assert.Equal(0.0, scope.HighlightClipPercent);
            });
        }

        [Fact]
        public void HistogramScopeControl_EmptyAndZeroBins_ShouldNotCrash()
        {
            StaTestRunner.Run(() =>
            {
                var scope = new HistogramScopeControl();
                scope.RedChannel = Array.Empty<int>();
                scope.GreenChannel = new int[256]; // All zeros
                scope.BlueChannel = new int[256];
                scope.LumaChannel = new int[256];

                // Measure & Arrange
                scope.Measure(new Size(300, 150));
                scope.Arrange(new Rect(0, 0, 300, 150));

                // Switch modes
                scope.ChannelMode = HistogramChannelMode.Luma;
                Assert.Equal(HistogramChannelMode.Luma, scope.ChannelMode);

                scope.ChannelMode = HistogramChannelMode.RedOnly;
                Assert.Equal(HistogramChannelMode.RedOnly, scope.ChannelMode);

                scope.ChannelMode = HistogramChannelMode.Rgb;
                Assert.Equal(HistogramChannelMode.Rgb, scope.ChannelMode);
            });
        }

        [Fact]
        public void HistogramScopeControl_BoundaryClippingSpikes_ShouldHandleWithoutCrashing()
        {
            StaTestRunner.Run(() =>
            {
                var scope = new HistogramScopeControl();

                // 21.5% shadow clipping at bin 0 (100,000 counts) and normal midtones (500 counts)
                var red = new int[256];
                red[0] = 100_000;
                for (int i = 1; i < 255; i++)
                {
                    red[i] = 500;
                }
                red[255] = 50_000; // Highlight clipping spike

                scope.RedChannel = red;
                scope.ShadowClipPercent = 21.5;
                scope.HighlightClipPercent = 8.2;

                scope.Measure(new Size(400, 200));
                scope.Arrange(new Rect(0, 0, 400, 200));

                Assert.Equal(21.5, scope.ShadowClipPercent);
                Assert.Equal(8.2, scope.HighlightClipPercent);
            });
        }

        [Fact]
        public void SegmentedControl_DefaultStateAndSelection_ShouldWork()
        {
            StaTestRunner.Run(() =>
            {
                var seg = new SegmentedControl();
                Assert.NotNull(seg.Items);
                Assert.True(seg.Items.Length > 0);
                Assert.Equal(0, seg.SelectedIndex);
                Assert.Equal(seg.Items[0], seg.SelectedItem);

                // Change selection
                seg.SelectedIndex = 2;
                Assert.Equal(2, seg.SelectedIndex);
                Assert.Equal(seg.Items[2], seg.SelectedItem);
            });
        }

        [Fact]
        public void SegmentedControl_CustomItemsAndEmptyArray_ShouldNotThrow()
        {
            StaTestRunner.Run(() =>
            {
                var seg = new SegmentedControl();

                // Custom items
                seg.Items = new[] { "Option A", "Option B", "Option C" };
                Assert.Equal(3, seg.Items.Length);

                seg.SelectedIndex = 1;
                Assert.Equal(1, seg.SelectedIndex);
                Assert.Equal("Option B", seg.SelectedItem);

                // Setting empty items should handle gracefully without crashing
                seg.Items = Array.Empty<string>();
                Assert.Empty(seg.Items);
                Assert.Equal(-1, seg.SelectedIndex);
                Assert.Null(seg.SelectedItem);
            });
        }

        [Fact]
        public void FilterControl_InitializationAndExpressionGeneration_ShouldWork()
        {
            StaTestRunner.Run(() =>
            {
                var filter = new FilterControl();
                Assert.NotNull(filter.RootGroup);
                Assert.True(filter.RootGroup.Children.Count > 0);

                string whereClause = filter.GetSqlWhere();
                Assert.False(string.IsNullOrWhiteSpace(whereClause));
                Assert.Contains("Status", whereClause);

                string display = filter.GetDisplayString();
                Assert.False(string.IsNullOrWhiteSpace(display));
                Assert.Contains("Status", display);
            });
        }
    }
}
