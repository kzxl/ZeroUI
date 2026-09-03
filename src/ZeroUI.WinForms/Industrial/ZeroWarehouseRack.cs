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
    public enum BinOccupancyStatus
    {
        Empty,
        Available,
        Full,
        Quarantine
    }

    public class WarehouseBin
    {
        public int Bay { get; set; }
        public int Level { get; set; }
        public string BinCode { get; set; } = "";
        public string Sku { get; set; } = "";
        public string ItemName { get; set; } = "";
        public string LotNumber { get; set; } = "";
        public int CurrentQty { get; set; }
        public int MaxCapacity { get; set; } = 1000;
        public BinOccupancyStatus Status { get; set; } = BinOccupancyStatus.Available;

        internal Rectangle Bounds;
    }

    public class WarehouseBinClickedEventArgs : EventArgs
    {
        public WarehouseBin Bin { get; }

        public WarehouseBinClickedEventArgs(WarehouseBin bin)
        {
            Bin = bin;
        }
    }

    /// <summary>
    /// 2D Smart Warehouse Storage Rack visualization control for WMS and inventory management.
    /// Renders multi-tier rack shelves (Bay x Level x Bin) with occupancy indicators,
    /// quarantine locks, hover inspection, and click events.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Industrial & SCADA")]
    [DefaultProperty("RackTitle")]
    [DefaultEvent("BinClicked")]
    [Description("2D Smart Warehouse Storage Rack visualizer for WMS inventory management")]
    public class ZeroWarehouseRack : Control
    {
        private int _bays = 5;
        private int _levels = 4;
        private WarehouseBin[,] _bins = new WarehouseBin[0, 0];
        private WarehouseBin? _hoveredBin;
        private string _rackTitle = "Kệ Chứa Cuộn Linh Kiện SMT — Dãy A (Rack A-01..05)";

        public event EventHandler<WarehouseBinClickedEventArgs>? BinClicked;

        public ZeroWarehouseRack()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);

            Size = new Size(460, 240);
            BackColor = Color.FromArgb(15, 23, 42); // Dark industrial frame
            Font = new Font("Segoe UI", 8.5f);

            InitializeRack();
        }

        [Category("Rack Configuration")]
        [DefaultValue(5)]
        public int Bays
        {
            get => _bays;
            set
            {
                _bays = Math.Max(2, Math.Min(12, value));
                InitializeRack();
                Invalidate();
            }
        }

        [Category("Rack Configuration")]
        [DefaultValue(4)]
        public int Levels
        {
            get => _levels;
            set
            {
                _levels = Math.Max(2, Math.Min(8, value));
                InitializeRack();
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue("Kệ Chứa Cuộn Linh Kiện SMT — Dãy A (Rack A-01..05)")]
        public string RackTitle
        {
            get => _rackTitle;
            set { _rackTitle = value ?? ""; Invalidate(); }
        }

        private void InitializeRack()
        {
            _bins = new WarehouseBin[_levels, _bays];
            var rand = new Random(101);

            for (int lvl = 0; lvl < _levels; lvl++)
            {
                int levelNum = _levels - lvl; // Top level is highest number
                for (int bay = 0; bay < _bays; bay++)
                {
                    int bayNum = bay + 1;
                    string code = $"A-{bayNum:D2}-{levelNum:D2}";

                    int roll = rand.Next(100);
                    BinOccupancyStatus status;
                    int max = 2000;
                    int qty = 0;
                    string sku = "";
                    string name = "";
                    string lot = "";

                    if (roll < 20)
                    {
                        status = BinOccupancyStatus.Empty;
                    }
                    else if (roll < 35)
                    {
                        status = BinOccupancyStatus.Quarantine;
                        qty = 500;
                        sku = "IC-MCU-000001";
                        name = "STM32F407VGT6";
                        lot = "LOT-20260815-HOLD";
                    }
                    else if (roll < 75)
                    {
                        status = BinOccupancyStatus.Available;
                        qty = rand.Next(300, 1500);
                        sku = "SMD-CAP-0805";
                        name = "Tụ gốm SMD 10uF 25V";
                        lot = $"LOT-2026090{bayNum}-SMT1";
                    }
                    else
                    {
                        status = BinOccupancyStatus.Full;
                        qty = max;
                        sku = "MEC-STEP-N23";
                        name = "Động cơ Nema 23";
                        lot = $"LOT-2026082{bayNum}-IMP";
                    }

                    _bins[lvl, bay] = new WarehouseBin
                    {
                        Bay = bayNum,
                        Level = levelNum,
                        BinCode = code,
                        Sku = sku,
                        ItemName = name,
                        LotNumber = lot,
                        CurrentQty = qty,
                        MaxCapacity = max,
                        Status = status
                    };
                }
            }
        }

        public void SetBin(int level, int bay, BinOccupancyStatus status, string sku, string name, string lot, int qty)
        {
            int r = _levels - level;
            int c = bay - 1;
            if (r >= 0 && r < _levels && c >= 0 && c < _bays)
            {
                var b = _bins[r, c];
                b.Status = status;
                b.Sku = sku;
                b.ItemName = name;
                b.LotNumber = lot;
                b.CurrentQty = qty;
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            WarehouseBin? hit = null;

            for (int lvl = 0; lvl < _levels; lvl++)
            {
                for (int bay = 0; bay < _bays; bay++)
                {
                    if (_bins[lvl, bay].Bounds.Contains(e.Location))
                    {
                        hit = _bins[lvl, bay];
                        break;
                    }
                }
                if (hit != null) break;
            }

            if (_hoveredBin != hit)
            {
                _hoveredBin = hit;
                Cursor = hit != null ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoveredBin != null)
            {
                _hoveredBin = null;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && _hoveredBin != null)
            {
                BinClicked?.Invoke(this, new WarehouseBinClickedEventArgs(_hoveredBin));
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = Width;
            int h = Height;

            // 1. Rack Enclosure
            using (var brush = new SolidBrush(BackColor))
            {
                g.FillRectangle(brush, 0, 0, w, h);
            }
            using (var borderPen = new Pen(Color.FromArgb(51, 65, 85), 1f))
            {
                g.DrawRectangle(borderPen, 0, 0, w - 1, h - 1);
            }

            // 2. Title & Occupancy Summary
            using (var titleFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(241, 245, 249)))
            {
                g.DrawString(_rackTitle, titleFont, titleBrush, 8, 6);
            }

            int emptyCount = 0, fullCount = 0, availCount = 0, quarCount = 0;
            for (int lvl = 0; lvl < _levels; lvl++)
            {
                for (int bay = 0; bay < _bays; bay++)
                {
                    switch (_bins[lvl, bay].Status)
                    {
                        case BinOccupancyStatus.Empty: emptyCount++; break;
                        case BinOccupancyStatus.Full: fullCount++; break;
                        case BinOccupancyStatus.Quarantine: quarCount++; break;
                        default: availCount++; break;
                    }
                }
            }

            string summary = $"Sử dụng: {fullCount + availCount}/{_levels * _bays} | Khóa QC: {quarCount}";
            using (var statFont = new Font("Segoe UI", 7.5f))
            using (var statBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
            {
                var sz = g.MeasureString(summary, statFont);
                g.DrawString(summary, statFont, statBrush, w - sz.Width - 10, 8);
            }

            // 3. Rack Geometry Layout
            int startY = 28;
            int legendH = 22;
            int marginX = 14;
            int availableW = w - (marginX * 2);
            int availableH = h - startY - legendH - 6;

            int gapX = 6;
            int gapY = 8;
            int binW = Math.Max(24, (availableW - ((_bays - 1) * gapX)) / _bays);
            int binH = Math.Max(20, (availableH - ((_levels - 1) * gapY)) / _levels);

            int startX = marginX + (availableW - ((binW * _bays) + ((_bays - 1) * gapX))) / 2;

            // Draw Upright Columns (Metallic beams)
            using (var beamPen = new Pen(Color.FromArgb(71, 85, 105), 3f))
            {
                for (int b = 0; b <= _bays; b++)
                {
                    int bx = startX + (b * (binW + gapX)) - (gapX / 2);
                    g.DrawLine(beamPen, bx, startY - 2, bx, startY + (binH * _levels) + ((_levels - 1) * gapY) + 2);
                }
            }

            // Draw Shelf Beams (Horizontal)
            using (var shelfPen = new Pen(Color.FromArgb(100, 116, 139), 2f))
            {
                for (int l = 0; l <= _levels; l++)
                {
                    int ly = startY + (l * (binH + gapY)) - (gapY / 2);
                    g.DrawLine(shelfPen, startX - 3, ly, startX + (_bays * (binW + gapX)), ly);
                }
            }

            // 4. Draw Individual Bins
            for (int lvl = 0; lvl < _levels; lvl++)
            {
                for (int bay = 0; bay < _bays; bay++)
                {
                    var bin = _bins[lvl, bay];
                    int x = startX + (bay * (binW + gapX));
                    int y = startY + (lvl * (binH + gapY));
                    bin.Bounds = new Rectangle(x, y, binW, binH);

                    DrawBin(g, bin, _hoveredBin == bin);
                }
            }

            // 5. Bottom Legend
            int legY = h - 18;
            DrawLegend(g, 10, legY, Color.FromArgb(51, 65, 85), "Trống (Empty)");
            DrawLegend(g, 105, legY, Color.FromArgb(59, 130, 246), "Có hàng (Avail)");
            DrawLegend(g, 210, legY, Color.FromArgb(16, 185, 129), "Đầy (Full)");
            DrawLegend(g, 290, legY, Color.FromArgb(239, 68, 68), "Khóa QC (Lock)");
        }

        private void DrawBin(Graphics g, WarehouseBin bin, bool isHovered)
        {
            Rectangle r = bin.Bounds;
            Color fillC;
            Color borderC;

            switch (bin.Status)
            {
                case BinOccupancyStatus.Empty:
                    fillC = Color.FromArgb(30, 41, 59);
                    borderC = Color.FromArgb(51, 65, 85);
                    break;
                case BinOccupancyStatus.Full:
                    fillC = Color.FromArgb(16, 185, 129);
                    borderC = Color.FromArgb(52, 211, 153);
                    break;
                case BinOccupancyStatus.Quarantine:
                    fillC = Color.FromArgb(239, 68, 68);
                    borderC = Color.FromArgb(248, 113, 113);
                    break;
                default:
                    fillC = Color.FromArgb(59, 130, 246);
                    borderC = Color.FromArgb(96, 165, 250);
                    break;
            }

            // Fill
            using (var brush = new LinearGradientBrush(new Point(r.X, r.Y), new Point(r.X, r.Bottom), fillC, Color.FromArgb(180, fillC.R / 2, fillC.G / 2, fillC.B / 2)))
            {
                g.FillRectangle(brush, r);
            }

            // Border
            using (var pen = new Pen(isHovered ? Color.White : borderC, isHovered ? 2f : 1f))
            {
                g.DrawRectangle(pen, r);
            }

            // Bin Code Text
            using var font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            using var textBrush = new SolidBrush(Color.White);
            var sz = g.MeasureString(bin.BinCode, font);
            g.DrawString(bin.BinCode, font, textBrush, r.X + (r.Width - sz.Width) / 2, r.Y + 4);

            // Sub text: Qty or status
            string sub = bin.Status == BinOccupancyStatus.Empty ? "0" : $"{bin.CurrentQty}";
            using var subFont = new Font("Segoe UI", 6.5f);
            using var subBrush = new SolidBrush(Color.FromArgb(226, 232, 240));
            var subSz = g.MeasureString(sub, subFont);
            g.DrawString(sub, subFont, subBrush, r.X + (r.Width - subSz.Width) / 2, r.Y + r.Height - subSz.Height - 3);
        }

        private void DrawLegend(Graphics g, int x, int y, Color color, string label)
        {
            using var brush = new SolidBrush(color);
            g.FillRectangle(brush, x, y + 2, 8, 8);
            using var font = new Font("Segoe UI", 7.5f);
            using var txtBrush = new SolidBrush(Color.FromArgb(148, 163, 184));
            g.DrawString(label, font, txtBrush, x + 12, y);
        }
    }
}
