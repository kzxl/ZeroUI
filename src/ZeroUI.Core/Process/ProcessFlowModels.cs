using System;
using System.Collections.Generic;

namespace ZeroUI.Core.Process
{
    /// <summary>
    /// Visual shape type for a node in a business process map.
    /// </summary>
    public enum ProcessNodeShape
    {
        /// <summary>
        /// Standard rectangular process task card with header, title, and description.
        /// </summary>
        TaskCard,

        /// <summary>
        /// Diamond decision node representing conditional branch logic.
        /// </summary>
        DecisionDiamond,

        /// <summary>
        /// Pill-shaped start terminal node.
        /// </summary>
        StartTerminal,

        /// <summary>
        /// Pill-shaped end/completion terminal node.
        /// </summary>
        EndTerminal
    }

    /// <summary>
    /// Runtime operational status of a process step.
    /// </summary>
    public enum ProcessNodeStatus
    {
        Normal,
        InProgress,
        Completed,
        Warning,
        Disabled
    }

    /// <summary>
    /// Cardinal port position on a process node for wire attachments.
    /// </summary>
    public enum ProcessPortPosition
    {
        Left,
        Right,
        Top,
        Bottom,
        Center
    }

    /// <summary>
    /// Wire routing style between process nodes.
    /// </summary>
    public enum ProcessRoutingMode
    {
        /// <summary>
        /// Standard orthogonal stepped 90-degree lines with arrows.
        /// </summary>
        Orthogonal,

        /// <summary>
        /// Direct point-to-point straight line.
        /// </summary>
        Straight,

        /// <summary>
        /// Smooth cubic Bezier spline.
        /// </summary>
        Bezier
    }

    /// <summary>
    /// Represents a single actionable step or decision node in a business process diagram.
    /// </summary>
    public class ProcessFlowNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = "Step Title";
        public string Subtitle { get; set; } = string.Empty;
        public ProcessNodeShape Shape { get; set; } = ProcessNodeShape.TaskCard;
        public ProcessNodeStatus Status { get; set; } = ProcessNodeStatus.Normal;

        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
        public double Width { get; set; } = 220;
        public double Height { get; set; } = 80;

        /// <summary>
        /// Semantic or hex header background color (e.g. #3B82F6, #8B5CF6, #10B981, #0284C7).
        /// </summary>
        public string HeaderColorHex { get; set; } = "#3B82F6";

        /// <summary>
        /// Semantic or hex border color.
        /// </summary>
        public string BorderColorHex { get; set; } = "#CBD5E1";

        /// <summary>
        /// Icon symbol or glyph (e.g. 📄, 🔍, 📦, 🛒, 🏢, 📥).
        /// </summary>
        public string IconGlyph { get; set; } = "📄";

        /// <summary>
        /// Target action key registered in <see cref="ProcessActionRegistry"/> to execute when clicked.
        /// </summary>
        public string ActionKey { get; set; } = string.Empty;

        /// <summary>
        /// Optional identifier of the parent swimlane containing this node.
        /// </summary>
        public string? LaneId { get; set; }

        /// <summary>
        /// Optional runtime metadata or data transfer object.
        /// </summary>
        public object? Tag { get; set; }

        public ProcessFlowNode()
        {
        }

        public ProcessFlowNode(string id, string title, string subtitle, double x, double y, double width = 220, double height = 80)
        {
            Id = id;
            Title = title;
            Subtitle = subtitle;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// Represents a directed connector or branch between two process nodes.
    /// </summary>
    public class ProcessFlowConnection
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string SourceNodeId { get; set; } = string.Empty;
        public string TargetNodeId { get; set; } = string.Empty;

        public ProcessPortPosition SourcePort { get; set; } = ProcessPortPosition.Bottom;
        public ProcessPortPosition TargetPort { get; set; } = ProcessPortPosition.Top;

        /// <summary>
        /// Descriptive label displayed along the transition connector (e.g. "CÒN HÀNG SẴN", "HẾT HÀNG -> ĐỀ XUẤT MUA").
        /// </summary>
        public string Label { get; set; } = string.Empty;

        public string StrokeColorHex { get; set; } = "#0EA5E9";
        public double StrokeThickness { get; set; } = 2.0;
        public bool IsDashed { get; set; } = false;
        public ProcessRoutingMode RoutingMode { get; set; } = ProcessRoutingMode.Orthogonal;

        public ProcessFlowConnection()
        {
        }

        public ProcessFlowConnection(string sourceNodeId, string targetNodeId, string label = "", string strokeColorHex = "#0EA5E9")
        {
            SourceNodeId = sourceNodeId;
            TargetNodeId = targetNodeId;
            Label = label;
            StrokeColorHex = strokeColorHex;
        }
    }

    /// <summary>
    /// Represents a horizontal or vertical swimlane boundary grouping steps by operational department.
    /// </summary>
    public class ProcessFlowLane
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = "Lane";
        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
        public double Width { get; set; } = 1200;
        public double Height { get; set; } = 400;
        public string HeaderColorHex { get; set; } = "#64748B";
        public string BackgroundColorHex { get; set; } = "#F8FAFC";

        public ProcessFlowLane()
        {
        }

        public ProcessFlowLane(string id, string title, double x, double y, double width, double height)
        {
            Id = id;
            Title = title;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// Root data model encapsulating a complete customizable business process workflow graph.
    /// </summary>
    public class ProcessFlowDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Code { get; set; } = "PROC_DEFAULT";
        public string Title { get; set; } = "Business Process Flow";
        public string Description { get; set; } = string.Empty;
        public int Version { get; set; } = 1;

        public List<ProcessFlowLane> Lanes { get; } = new List<ProcessFlowLane>();
        public List<ProcessFlowNode> Nodes { get; } = new List<ProcessFlowNode>();
        public List<ProcessFlowConnection> Connections { get; } = new List<ProcessFlowConnection>();

        /// <summary>
        /// Creates a factory template matching the standard IT Inventory business workflow
        /// with requisition, stock-check decision, PO purchase approval, receipt, and warehouse inward.
        /// </summary>
        public static ProcessFlowDefinition CreateDefaultWarehouseFlow()
        {
            var def = new ProcessFlowDefinition
            {
                Code = "IT_INVENTORY_WORKFLOW",
                Title = "QUY TRÌNH NGHIỆP VỤ KHO IT",
                Description = "Nhấn vào các bước trong sơ đồ để chuyển đến chức năng tương ứng"
            };

            // 1. Swimlanes
            def.Lanes.Add(new ProcessFlowLane("lane_request", "YÊU CẦU & CUNG ỨNG", 20, 20, 960, 520)
            {
                HeaderColorHex = "#64748B",
                BackgroundColorHex = "#F8FAFC"
            });
            def.Lanes.Add(new ProcessFlowLane("lane_warehouse", "NHẬP KHO & XUẤT KHO", 20, 560, 960, 320)
            {
                HeaderColorHex = "#0EA5E9",
                BackgroundColorHex = "#F0F9FF"
            });

            // 2. Nodes
            // Step 1: IT Requisition
            def.Nodes.Add(new ProcessFlowNode("node_req", "1. PHIẾU YÊU CẦU IT", "Nhân viên lập phiếu yêu cầu cấp thiết bị", 370, 60, 220, 70)
            {
                Shape = ProcessNodeShape.TaskCard,
                HeaderColorHex = "#475569",
                IconGlyph = "📄",
                ActionKey = "it.ticket.request",
                LaneId = "lane_request"
            });

            // Step 2: Inventory Stock Check (Decision Diamond)
            def.Nodes.Add(new ProcessFlowNode("node_check", "2. KIỂM TRA TỒN KHO", "(Lọc máy còn trong kho IT)", 390, 180, 180, 80)
            {
                Shape = ProcessNodeShape.DecisionDiamond,
                HeaderColorHex = "#0284C7",
                BorderColorHex = "#38BDF8",
                IconGlyph = "🔍",
                ActionKey = "it.inventory.lookup",
                LaneId = "lane_request"
            });

            // Step 3: Purchase Requisition
            def.Nodes.Add(new ProcessFlowNode("node_pr", "3. YÊU CẦU MUA HÀNG", "• IT lập yêu cầu mua hàng (YCMH)\n• Trình duyệt theo phân cấp", 640, 290, 220, 75)
            {
                Shape = ProcessNodeShape.TaskCard,
                HeaderColorHex = "#7C3AED",
                BorderColorHex = "#DDD6FE",
                IconGlyph = "🛒",
                ActionKey = "purchase.pr.create",
                LaneId = "lane_request"
            });

            // Step 4: Purchase Order (PO)
            def.Nodes.Add(new ProcessFlowNode("node_po", "4. PHIẾU MUA HÀNG (PO)", "• Bộ phận Mua hàng lập phiếu\n• Đặt hàng nhà cung cấp", 640, 390, 220, 75)
            {
                Shape = ProcessNodeShape.TaskCard,
                HeaderColorHex = "#7C3AED",
                BorderColorHex = "#DDD6FE",
                IconGlyph = "🏢",
                ActionKey = "purchase.po.create",
                LaneId = "lane_request"
            });

            // Step 5: Goods Receipt Note
            def.Nodes.Add(new ProcessFlowNode("node_grn", "5. PHIẾU NHẬN HÀNG", "• Thủ kho IT nhận hàng thực tế\n• Xác nhận đã nhận đủ trên phiếu", 640, 490, 220, 75)
            {
                Shape = ProcessNodeShape.TaskCard,
                HeaderColorHex = "#0284C7",
                BorderColorHex = "#BAE6FD",
                IconGlyph = "📦",
                ActionKey = "inventory.receipt.note",
                LaneId = "lane_request"
            });

            // Step 6: Warehouse Stock-In
            def.Nodes.Add(new ProcessFlowNode("node_inward", "6. NHẬP KHO IT", "• Lập và duyệt phiếu nhập kho\n• Sinh mã tài sản (Asset Tag)", 640, 590, 220, 75)
            {
                Shape = ProcessNodeShape.TaskCard,
                HeaderColorHex = "#16A34A",
                BorderColorHex = "#BBF7D0",
                IconGlyph = "📥",
                ActionKey = "it.inventory.stockin",
                LaneId = "lane_warehouse"
            });

            // Target Step: Warehouse Stock Pool
            def.Nodes.Add(new ProcessFlowNode("node_stock", "TỒN KHO IT", "Thiết bị sẵn sàng cấp phát", 260, 680, 220, 70)
            {
                Shape = ProcessNodeShape.TaskCard,
                HeaderColorHex = "#0D9488",
                BorderColorHex = "#99F6E4",
                IconGlyph = "🖥️",
                ActionKey = "it.inventory.pool",
                LaneId = "lane_warehouse"
            });

            // 3. Connections
            // node_req -> node_check
            def.Connections.Add(new ProcessFlowConnection("node_req", "node_check")
            {
                SourcePort = ProcessPortPosition.Bottom,
                TargetPort = ProcessPortPosition.Top,
                StrokeColorHex = "#94A3B8"
            });

            // node_check -> node_pr (Out of Stock branch)
            def.Connections.Add(new ProcessFlowConnection("node_check", "node_pr", "HẾT HÀNG -> ĐỀ XUẤT MUA", "#EF4444")
            {
                SourcePort = ProcessPortPosition.Right,
                TargetPort = ProcessPortPosition.Top,
                StrokeColorHex = "#EF4444"
            });

            // node_check -> node_stock (In Stock branch)
            def.Connections.Add(new ProcessFlowConnection("node_check", "node_stock", "CÒN HÀNG SẴN", "#10B981")
            {
                SourcePort = ProcessPortPosition.Left,
                TargetPort = ProcessPortPosition.Top,
                StrokeColorHex = "#10B981"
            });

            // node_pr -> node_po
            def.Connections.Add(new ProcessFlowConnection("node_pr", "node_po")
            {
                SourcePort = ProcessPortPosition.Bottom,
                TargetPort = ProcessPortPosition.Top,
                StrokeColorHex = "#94A3B8"
            });

            // node_po -> node_grn
            def.Connections.Add(new ProcessFlowConnection("node_po", "node_grn")
            {
                SourcePort = ProcessPortPosition.Bottom,
                TargetPort = ProcessPortPosition.Top,
                StrokeColorHex = "#94A3B8"
            });

            // node_grn -> node_inward
            def.Connections.Add(new ProcessFlowConnection("node_grn", "node_inward")
            {
                SourcePort = ProcessPortPosition.Bottom,
                TargetPort = ProcessPortPosition.Top,
                StrokeColorHex = "#94A3B8"
            });

            // node_inward -> node_stock
            def.Connections.Add(new ProcessFlowConnection("node_inward", "node_stock")
            {
                SourcePort = ProcessPortPosition.Left,
                TargetPort = ProcessPortPosition.Right,
                StrokeColorHex = "#10B981"
            });

            return def;
        }
    }
}
