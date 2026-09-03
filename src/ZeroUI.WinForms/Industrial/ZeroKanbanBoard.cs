using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Native;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Industrial
{
    public enum KanbanPriority
    {
        Normal,
        Urgent,
        Low
    }

    public class KanbanCard
    {
        public string OrderNo { get; set; } = "";
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public KanbanPriority Priority { get; set; } = KanbanPriority.Normal;
        public string OperatorName { get; set; } = "";

        internal Rectangle Bounds;
    }

    public class KanbanColumn
    {
        public string Title { get; set; }
        public int WipLimit { get; set; }
        public List<KanbanCard> Cards { get; } = new List<KanbanCard>();

        internal Rectangle Bounds;

        public KanbanColumn(string title, int wipLimit = 0)
        {
            Title = title;
            WipLimit = wipLimit;
        }
    }

    public class KanbanCardClickedEventArgs : EventArgs
    {
        public KanbanCard Card { get; }
        public int ColumnIndex { get; }

        public KanbanCardClickedEventArgs(KanbanCard card, int colIndex)
        {
            Card = card;
            ColumnIndex = colIndex;
        }
    }

    /// <summary>
    /// Electronic Shopfloor Kanban Dispatching Board for MES manufacturing workflows.
    /// Supports multi-stage columns, Work-In-Progress (WIP) limit enforcement,
    /// priority tags, and interactive card transitions.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Industrial & SCADA")]
    [DefaultEvent("CardClicked")]
    [Description("Electronic Shopfloor Kanban Dispatching Board with WIP limits")]
    public class ZeroKanbanBoard : Control
    {
        private readonly List<KanbanColumn> _columns = new List<KanbanColumn>();
        private KanbanCard? _hoveredCard;
        private int _hoveredColIndex = -1;

        public event EventHandler<KanbanCardClickedEventArgs>? CardClicked;

        public ZeroKanbanBoard()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);

            Size = new Size(540, 240);
            BackColor = Color.FromArgb(15, 23, 42); // Dark slate
            Font = new Font("Segoe UI", 8.5f);

            InitializeDefaultBoard();
        }

        [Browsable(false)]
        public List<KanbanColumn> Columns => _columns;

        private void InitializeDefaultBoard()
        {
            var col1 = new KanbanColumn("1. Chuẩn Bị NVL", 5);
            col1.Cards.Add(new KanbanCard { OrderNo = "WO-8101", ProductName = "Mạch vi xử lý STM32", Quantity = 200, Priority = KanbanPriority.Normal, OperatorName = "Nguyễn Văn An" });
            col1.Cards.Add(new KanbanCard { OrderNo = "WO-8104", ProductName = "Bộ nguồn Buck 24V", Quantity = 500, Priority = KanbanPriority.Urgent, OperatorName = "Trần Bích" });

            var col2 = new KanbanColumn("2. Đang Gia Công SMT", 3);
            col2.Cards.Add(new KanbanCard { OrderNo = "WO-8098", ProductName = "Cảm biến quang Keyence", Quantity = 150, Priority = KanbanPriority.Normal, OperatorName = "Lê Hoàng" });
            col2.Cards.Add(new KanbanCard { OrderNo = "WO-8099", ProductName = "Driver bước TMC2209", Quantity = 300, Priority = KanbanPriority.Urgent, OperatorName = "Phạm Dũng" });

            var col3 = new KanbanColumn("3. Kiểm Tra AOI/QC", 3);
            col3.Cards.Add(new KanbanCard { OrderNo = "WO-8092", ProductName = "Module Wi-Fi ESP32", Quantity = 400, Priority = KanbanPriority.Normal, OperatorName = "Vũ Oanh" });

            var col4 = new KanbanColumn("4. Đóng Gói / Xuất", 0);
            col4.Cards.Add(new KanbanCard { OrderNo = "WO-8085", ProductName = "Vỏ nhôm CNC IP67", Quantity = 100, Priority = KanbanPriority.Low, OperatorName = "Đỗ Cường" });

            _columns.Add(col1);
            _columns.Add(col2);
            _columns.Add(col3);
            _columns.Add(col4);
        }

        public void MoveCardNext(KanbanCard card)
        {
            for (int i = 0; i < _columns.Count; i++)
            {
                if (_columns[i].Cards.Contains(card))
                {
                    _columns[i].Cards.Remove(card);
                    if (i + 1 < _columns.Count)
                    {
                        _columns[i + 1].Cards.Add(card);
                    }
                    Invalidate();
                    break;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            KanbanCard? hitCard = null;
            int hitCol = -1;

            for (int c = 0; c < _columns.Count; c++)
            {
                var col = _columns[c];
                for (int i = 0; i < col.Cards.Count; i++)
                {
                    if (col.Cards[i].Bounds.Contains(e.Location))
                    {
                        hitCard = col.Cards[i];
                        hitCol = c;
                        break;
                    }
                }
                if (hitCard != null) break;
            }

            if (_hoveredCard != hitCard)
            {
                _hoveredCard = hitCard;
                _hoveredColIndex = hitCol;
                Cursor = hitCard != null ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoveredCard != null)
            {
                _hoveredCard = null;
                _hoveredColIndex = -1;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && _hoveredCard != null)
            {
                CardClicked?.Invoke(this, new KanbanCardClickedEventArgs(_hoveredCard, _hoveredColIndex));
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = Width;
            int h = Height;

            // Frame Background
            using (var brush = new SolidBrush(BackColor))
            {
                g.FillRectangle(brush, 0, 0, w, h);
            }
            using (var borderPen = new Pen(Color.FromArgb(51, 65, 85), 1f))
            {
                g.DrawRectangle(borderPen, 0, 0, w - 1, h - 1);
            }

            if (_columns.Count == 0) return;

            int pad = 8;
            int gap = 8;
            int availableW = w - (pad * 2) - ((_columns.Count - 1) * gap);
            int colW = availableW / _columns.Count;

            for (int c = 0; c < _columns.Count; c++)
            {
                var col = _columns[c];
                int cx = pad + (c * (colW + gap));
                int cy = pad;
                int ch = h - (pad * 2);
                col.Bounds = new Rectangle(cx, cy, colW, ch);

                DrawColumn(g, col, c);
            }
        }

        private void DrawColumn(Graphics g, KanbanColumn col, int colIndex)
        {
            var rect = col.Bounds;

            // Column container background
            using (var colBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.FillRectangle(colBrush, rect);
            }
            using (var colPen = new Pen(Color.FromArgb(51, 65, 85), 1f))
            {
                g.DrawRectangle(colPen, rect);
            }

            // Column Header
            int headerH = 28;
            var headerRect = new Rectangle(rect.X, rect.Y, rect.Width, headerH);
            using (var hBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.FillRectangle(hBrush, headerRect);
            }

            // Column Title
            using (var titleFont = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(241, 245, 249)))
            {
                g.DrawString(col.Title, titleFont, titleBrush, rect.X + 6, rect.Y + 6);
            }

            // WIP Limit Badge
            bool isOverWip = (col.WipLimit > 0 && col.Cards.Count > col.WipLimit);
            string countText = col.WipLimit > 0 ? $"{col.Cards.Count}/{col.WipLimit}" : $"{col.Cards.Count}";
            Color badgeBg = isOverWip ? Color.FromArgb(239, 68, 68) : Color.FromArgb(51, 65, 85);

            using (var badgeFont = new Font("Segoe UI", 7f, FontStyle.Bold))
            {
                var bsz = g.MeasureString(countText, badgeFont);
                int bx = rect.Right - (int)bsz.Width - 12;
                int by = rect.Y + 5;
                using var bBrush = new SolidBrush(badgeBg);
                g.FillRectangle(bBrush, bx - 2, by, (int)bsz.Width + 6, 16);
                using var tBrush = new SolidBrush(Color.White);
                g.DrawString(countText, badgeFont, tBrush, bx + 1, by + 1);
            }

            // Render Cards inside column
            int cardY = rect.Y + headerH + 6;
            int cardH = 54;
            int cardMargin = 4;

            for (int i = 0; i < col.Cards.Count; i++)
            {
                var card = col.Cards[i];
                card.Bounds = new Rectangle(rect.X + cardMargin, cardY, rect.Width - (cardMargin * 2), cardH);

                DrawCard(g, card, _hoveredCard == card);
                cardY += cardH + 6;
            }
        }

        private void DrawCard(Graphics g, KanbanCard card, bool isHovered)
        {
            var r = card.Bounds;

            // Card Background
            Color bg = isHovered ? Color.FromArgb(51, 65, 85) : Color.FromArgb(15, 23, 42);
            using (var brush = new SolidBrush(bg))
            {
                g.FillRectangle(brush, r);
            }

            // Border
            Color borderC = isHovered ? Color.FromArgb(96, 165, 250) : Color.FromArgb(71, 85, 105);
            using (var pen = new Pen(borderC, isHovered ? 1.5f : 1f))
            {
                g.DrawRectangle(pen, r);
            }

            // Priority Indicator Stripe
            Color pColor = card.Priority switch
            {
                KanbanPriority.Urgent => Color.FromArgb(239, 68, 68),
                KanbanPriority.Low => Color.FromArgb(100, 116, 139),
                _ => Color.FromArgb(59, 130, 246)
            };
            using (var pBrush = new SolidBrush(pColor))
            {
                g.FillRectangle(pBrush, r.X, r.Y, 3, r.Height);
            }

            // Order No
            using (var orderFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (var orderBrush = new SolidBrush(Color.FromArgb(96, 165, 250)))
            {
                g.DrawString(card.OrderNo, orderFont, orderBrush, r.X + 8, r.Y + 4);
            }

            // Qty
            using (var qtyFont = new Font("Segoe UI", 7f))
            using (var qtyBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
            {
                string q = $"{card.Quantity} PCS";
                var qsz = g.MeasureString(q, qtyFont);
                g.DrawString(q, qtyFont, qtyBrush, r.Right - qsz.Width - 4, r.Y + 4);
            }

            // Product Name
            using (var nameFont = new Font("Segoe UI", 7.5f))
            using (var nameBrush = new SolidBrush(Color.FromArgb(241, 245, 249)))
            {
                g.DrawString(card.ProductName, nameFont, nameBrush, r.X + 8, r.Y + 20);
            }

            // Operator
            using (var opFont = new Font("Segoe UI", 6.5f))
            using (var opBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                g.DrawString($"👤 {card.OperatorName}", opFont, opBrush, r.X + 8, r.Y + 36);
            }
        }
    }
}
