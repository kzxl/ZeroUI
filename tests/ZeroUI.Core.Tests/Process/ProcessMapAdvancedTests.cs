using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZeroUI.Core.Process;

namespace ZeroUI.Core.Tests.Process
{
    public class ProcessMapAdvancedTests
    {
        [Fact]
        public void CreateLaneFromSelection_CalculatesBoundingBoxAndAssignsLaneId()
        {
            var def = new ProcessFlowDefinition();
            var node1 = new ProcessFlowNode("n1", "Step 1", "Desc 1", 100, 100, 200, 80);
            var node2 = new ProcessFlowNode("n2", "Step 2", "Desc 2", 400, 150, 200, 80);
            def.Nodes.Add(node1);
            def.Nodes.Add(node2);

            var lane = def.CreateLaneFromSelection(new[] { node1, node2 }, "1. KINH DOANH & THIẾT KẾ", "#0EA5E9");

            Assert.NotNull(lane);
            Assert.Contains(lane, def.Lanes);
            Assert.Equal("1. KINH DOANH & THIẾT KẾ", lane.Title);
            Assert.Equal(lane.Id, node1.LaneId);
            Assert.Equal(lane.Id, node2.LaneId);

            // Bounding box: X from min(100) - 36 = 64
            // MaxX: 400 + 200 = 600, + 36 = 636 => Width = 636 - 64 = 572
            Assert.True(lane.X <= node1.X);
            Assert.True(lane.Y <= node1.Y);
            Assert.True(lane.X + lane.Width >= node2.X + node2.Width);
            Assert.True(lane.Y + lane.Height >= node2.Y + node2.Height);
            Assert.True(lane.Contains(node1));
            Assert.True(lane.Contains(node2));
        }

        [Fact]
        public void FitLanesToNodes_ExpandsLaneToFitContainedNodes()
        {
            var def = new ProcessFlowDefinition();
            var lane = new ProcessFlowLane("lane_1", "Production", 50, 50, 200, 150);
            def.Lanes.Add(lane);

            // Node is far to the right (x=450, w=200 => right=650)
            var node = new ProcessFlowNode("n1", "Assembly", "Assemble parts", 450, 80, 200, 80)
            {
                LaneId = "lane_1"
            };
            def.Nodes.Add(node);

            def.FitLanesToNodes(padding: 30);

            Assert.True(lane.X <= node.X - 30);
            Assert.True(lane.X + lane.Width >= node.X + node.Width + 30);
            Assert.True(lane.Y + lane.Height >= node.Y + node.Height + 30);
            Assert.True(lane.Contains(node));
        }

        [Fact]
        public void AutoArrangeLayout_HorizontalDAG_ArrangesNodesInSequentialLayers()
        {
            var def = new ProcessFlowDefinition();
            var n1 = new ProcessFlowNode("step1", "Start", "", 500, 300, 180, 70);
            var n2 = new ProcessFlowNode("step2", "Review", "", 100, 200, 180, 70);
            var n3 = new ProcessFlowNode("step3", "Approve", "", 50, 50, 180, 70);
            def.Nodes.Add(n1);
            def.Nodes.Add(n2);
            def.Nodes.Add(n3);

            // Flow: step1 -> step2 -> step3
            def.Connections.Add(new ProcessFlowConnection("step1", "step2"));
            def.Connections.Add(new ProcessFlowConnection("step2", "step3"));

            def.AutoArrangeLayout(horizontal: true, nodeSpacingX: 80, nodeSpacingY: 50);

            // step1 should have smaller X than step2, which has smaller X than step3
            Assert.True(n1.X < n2.X, $"Expected n1.X ({n1.X}) < n2.X ({n2.X})");
            Assert.True(n2.X < n3.X, $"Expected n2.X ({n2.X}) < n3.X ({n3.X})");
        }

        [Fact]
        public void AlignNodes_AlignLeftAndAlignTop_AlignsCoordinatesCorrectly()
        {
            var def = new ProcessFlowDefinition();
            var n1 = new ProcessFlowNode("n1", "Step 1", "", 100, 50, 200, 70);
            var n2 = new ProcessFlowNode("n2", "Step 2", "", 180, 160, 200, 70);
            var n3 = new ProcessFlowNode("n3", "Step 3", "", 250, 280, 200, 70);
            def.Nodes.Add(n1);
            def.Nodes.Add(n2);
            def.Nodes.Add(n3);

            // Align Left
            ProcessFlowDefinition.AlignNodes(new[] { n1, n2, n3 }, ProcessNodeAlignment.Left);
            Assert.Equal(100, n1.X);
            Assert.Equal(100, n2.X);
            Assert.Equal(100, n3.X);

            // Align Top
            ProcessFlowDefinition.AlignNodes(new[] { n1, n2, n3 }, ProcessNodeAlignment.Top);
            Assert.Equal(50, n1.Y);
            Assert.Equal(50, n2.Y);
            Assert.Equal(50, n3.Y);
        }

        [Fact]
        public void DistributeNodes_Horizontal_EquallySpacesNodes()
        {
            var def = new ProcessFlowDefinition();
            var n1 = new ProcessFlowNode("n1", "1", "", 100, 100, 100, 60);
            var n2 = new ProcessFlowNode("n2", "2", "", 180, 100, 100, 60);
            var n3 = new ProcessFlowNode("n3", "3", "", 700, 100, 100, 60);
            def.Nodes.Add(n1);
            def.Nodes.Add(n2);
            def.Nodes.Add(n3);

            ProcessFlowDefinition.DistributeNodes(new[] { n1, n2, n3 }, horizontally: true);

            double gap1 = n2.X - (n1.X + n1.Width);
            double gap2 = n3.X - (n2.X + n2.Width);
            Assert.True(Math.Abs(gap1 - gap2) < 0.001, $"Expected equal gaps: {gap1} vs {gap2}");
        }

        [Fact]
        public void ProcessFlowLane_ContainsAndIntersects_CalculatesAccurately()
        {
            var lane = new ProcessFlowLane("lane1", "Sales", 100, 100, 500, 300);
            var insideNode = new ProcessFlowNode("n1", "", "", 150, 150, 180, 70);
            var outsideNode = new ProcessFlowNode("n2", "", "", 700, 150, 180, 70);
            var intersectingNode = new ProcessFlowNode("n3", "", "", 50, 50, 100, 100);

            Assert.True(lane.Contains(200, 200));
            Assert.False(lane.Contains(50, 50));
            Assert.True(lane.Contains(insideNode));
            Assert.False(lane.Contains(outsideNode));
            Assert.True(lane.Intersects(intersectingNode));
        }
    }
}
