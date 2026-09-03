using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;
using ZeroUI.WinForms.Warehouse.Models;

namespace ZeroUI.WinForms.Warehouse
{
    /// <summary>
    /// Industrial Stock Movement Traceability Tree for Warehouse & MES.
    /// Visualizes batch & lot lifecycle: Initial Inward ➔ Outward Allocations ➔ Current Balance.
    /// Follows the 1-Way Data Flow standard (Populate / CollectData).
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Warehouse & Logistics")]
    [Description("Industrial Stock Movement and Lot Traceability Timeline Tree")]
    public class ZeroStockMovementTimeline : Control
    {
        private StockMovementTraceModel _traceData = new StockMovementTraceModel
        {
            ProductCode = "ABC-001",
            LotNumber = "LOT260901",
            WarehouseCode = "WH01"
        };

        public ZeroStockMovementTimeline()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(380, 240);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            LoadSampleTrace();
        }

        private void LoadSampleTrace()
        {
            _traceData.Nodes.Clear();
            _traceData.Nodes.Add(new StockMovementNode
            {
                Id = "N1",
                Type = StockMovementType.Inward,
                Title = "INITIAL INWARD RECEIPT",
                ReferenceNo = "PNK-0824",
                Quantity = 500,
                Timestamp = DateTime.Today.AddDays(-10),
                DestinationOrSource = "Supplier Nippon Steel"
            });
            _traceData.Nodes.Add(new StockMovementNode
            {
                Id = "N2",
                Type = StockMovementType.OutwardProduction,
                Title = "PRODUCTION DISPATCH 1",
                ReferenceNo = "LSX-0912",
                Quantity = 100,
                Timestamp = DateTime.Today.AddDays(-6),
                DestinationOrSource = "Assembly Line 01"
            });
            _traceData.Nodes.Add(new StockMovementNode
            {
                Id = "N3",
                Type = StockMovementType.OutwardSales,
                Title = "SALES SHIPMENT 2",
                ReferenceNo = "PXK-0955",
                Quantity = 150,
                Timestamp = DateTime.Today.AddDays(-2),
                DestinationOrSource = "Customer Toyota Tsusho"
            });
            _traceData.Nodes.Add(new StockMovementNode
            {
                Id = "N4",
                Type = StockMovementType.Balance,
                Title = "CURRENT INVENTORY BALANCE",
                ReferenceNo = "WH01-A2",
                Quantity = 250,
                Timestamp = DateTime.Now,
                DestinationOrSource = "Available in Stock"
            });
        }

        #region Public Properties

        [Category("Data")]
        [DefaultValue("LOT260901")]
        public string LotNumber
        {
            get => _traceData.LotNumber;
            set { _traceData.LotNumber = value ?? ""; Invalidate(); }
        }

        [Category("Data")]
        [DefaultValue("ABC-001")]
        public string ProductCode
        {
            get => _traceData.ProductCode;
            set { _traceData.ProductCode = value ?? ""; Invalidate(); }
        }

        #endregion

        #region 1-Way Data Flow API

        /// <summary>
        /// Populates the timeline with a complete movement trace tree.
        /// Thread-safe via InvokeIfRequired marshaling.
        /// </summary>
        public void Populate(StockMovementTraceModel data)
        {
            if (data == null) return;

            if (InvokeRequired)
            {
                Invoke(new Action(() => Populate(data)));
                return;
            }

            _traceData = data;
            Invalidate();
        }

        /// <summary>
        /// Collects the current trace tree snapshot.
        /// </summary>
        public StockMovementTraceModel CollectData() => _traceData;

        public void Clear()
        {
            _traceData = new StockMovementTraceModel();
            Invalidate();
        }

        #endregion

        #region Rendering Engine

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = Width;
            int h = Height;

            // 1. Enclosure Card
            using (var cardPath = ZeroUIConfig.CreateRoundedRectangle(new Rectangle(0, 0, w - 1, h - 1), 8))
            {
                using var bgBrush = new SolidBrush(Color.White);
                g.FillPath(bgBrush, cardPath);
                using var borderPen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                g.DrawPath(borderPen, cardPath);
            }

            // 2. Header
            using (var titleFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.DrawString($"BATCH TRACEABILITY: {_traceData.LotNumber}", titleFont, titleBrush, 12, 10);
            }
            using (var subFont = new Font("Segoe UI", 8f, FontStyle.Regular))
            using (var subBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                g.DrawString($"Product: {_traceData.ProductCode} | Warehouse: {_traceData.WarehouseCode}", subFont, subBrush, 12, 28);
            }

            using (var sepPen = new Pen(Color.FromArgb(241, 245, 249), 1f))
            {
                g.DrawLine(sepPen, 12, 46, w - 12, 46);
            }

            // 3. Tree Spine and Nodes
            int spineX = 26;
            int startY = 60;
            int nodeGap = 44;
            var nodes = _traceData.Nodes;

            if (nodes.Count > 1)
            {
                int endSpineY = startY + (nodes.Count - 1) * nodeGap;
                using var spinePen = new Pen(Color.FromArgb(203, 213, 225), 1.8f)
                {
                    DashStyle = DashStyle.Solid
                };
                g.DrawLine(spinePen, spineX, startY, spineX, endSpineY);
            }

            using var nodeTitleFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var nodeDetailFont = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            using var qtyFont = new Font("Segoe UI", 9f, FontStyle.Bold);

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                int nodeY = startY + (i * nodeGap);

                // Branch horizontal connector (├── or └──)
                if (i > 0)
                {
                    using var branchPen = new Pen(Color.FromArgb(203, 213, 225), 1.8f);
                    g.DrawLine(branchPen, spineX, nodeY, spineX + 14, nodeY);
                }

                // Node Circle Glyph
                DrawNodeGlyph(g, spineX, nodeY, node.Type);

                // Node Content
                int textX = spineX + 22;
                Color titleColor;
                Color qtyColor;
                string qtyPrefix;

                switch (node.Type)
                {
                    case StockMovementType.Inward:
                        titleColor = Color.FromArgb(5, 150, 105);
                        qtyColor = Color.FromArgb(5, 150, 105);
                        qtyPrefix = "+";
                        break;
                    case StockMovementType.Balance:
                        titleColor = Color.FromArgb(67, 56, 202);
                        qtyColor = Color.FromArgb(67, 56, 202);
                        qtyPrefix = "=";
                        break;
                    default:
                        titleColor = Color.FromArgb(220, 38, 38);
                        qtyColor = Color.FromArgb(220, 38, 38);
                        qtyPrefix = "-";
                        break;
                }

                // Line 1: Title & Reference
                using (var tBrush = new SolidBrush(titleColor))
                using (var refBrush = new SolidBrush(Color.FromArgb(71, 85, 105)))
                {
                    string refText = string.IsNullOrEmpty(node.ReferenceNo) ? "" : $" [{node.ReferenceNo}]";
                    g.DrawString(node.Title + refText, nodeTitleFont, tBrush, textX, nodeY - 10);
                }

                // Line 2: Date & Destination/Source
                using (var detBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    string dateStr = node.Timestamp.ToString("dd/MM/yyyy HH:mm");
                    string srcStr = string.IsNullOrEmpty(node.DestinationOrSource) ? "" : $" • {node.DestinationOrSource}";
                    g.DrawString(dateStr + srcStr, nodeDetailFont, detBrush, textX, nodeY + 6);
                }

                // Right-aligned Quantity Badge
                string qtyText = $"{qtyPrefix}{node.Quantity:N0}";
                var qtySize = g.MeasureString(qtyText, qtyFont);
                using (var qBrush = new SolidBrush(qtyColor))
                {
                    g.DrawString(qtyText, qtyFont, qBrush, w - qtySize.Width - 14, nodeY - 6);
                }
            }
        }

        private void DrawNodeGlyph(Graphics g, int cx, int cy, StockMovementType type)
        {
            int r = 6;
            Color fillColor;
            Color borderColor;

            switch (type)
            {
                case StockMovementType.Inward:
                    fillColor = Color.FromArgb(16, 185, 129); // Emerald
                    borderColor = Color.FromArgb(209, 250, 229);
                    break;
                case StockMovementType.Balance:
                    fillColor = Color.FromArgb(79, 70, 229); // Indigo
                    borderColor = Color.FromArgb(224, 231, 255);
                    break;
                default:
                    fillColor = Color.FromArgb(239, 68, 68); // Rose/Red
                    borderColor = Color.FromArgb(254, 226, 226);
                    break;
            }

            using (var borderBrush = new SolidBrush(borderColor))
            {
                g.FillEllipse(borderBrush, cx - r - 2, cy - r - 2, (r + 2) * 2, (r + 2) * 2);
            }
            using (var fillBrush = new SolidBrush(fillColor))
            {
                g.FillEllipse(fillBrush, cx - r, cy - r, r * 2, r * 2);
            }
            using (var whiteBrush = new SolidBrush(Color.White))
            {
                g.FillEllipse(whiteBrush, cx - 2, cy - 2, 4, 4);
            }
        }

        #endregion
    }
}
