using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.Logistics;
using ZeroUI.Core.Rendering;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Logistics
{
    /// <summary>
    /// Automated Storage and Retrieval System (ASRS) Stacker Crane Visualizer.
    /// Renders high-bay racking matrix, traveling crane mast, hoist carriage,
    /// telescopic fork extension, pallet payload status, and live PPH throughput metrics.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Logistics & Warehouse")]
    [Description("ASRS Stacker Crane 2D elevation visualizer with mast, hoist, forks, and cycle telemetry")]
    public class AsrsCraneVisualizer : Control
    {
        private readonly AsrsEngine _engine = new AsrsEngine();
        private IDisposable? _animSub;
        private string _aisleTag = "AISLE-04";

        public AsrsCraneVisualizer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            Size = new Size(760, 340);

            ZeroTheme.ThemeChanged += OnThemeChanged;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _animSub ??= ZeroAnimationClock.Subscribe((delta, frame) =>
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    // Gentle motion oscillation when active or simulating
                    Invalidate();
                }
            });
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _animSub?.Dispose();
            _animSub = null;
            base.OnHandleDestroyed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ZeroTheme.ThemeChanged -= OnThemeChanged;
                _animSub?.Dispose();
                _animSub = null;
            }
            base.Dispose(disposing);
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            if (IsHandleCreated && !IsDisposed)
            {
                if (InvokeRequired)
                    BeginInvoke(new Action(Invalidate));
                else
                    Invalidate();
            }
        }

        #region Public Properties

        [Category("ASRS")]
        [Description("Access to the underlying ASRS kinematics and calculation engine")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public AsrsEngine Engine => _engine;

        [Category("ASRS")]
        [DefaultValue("AISLE-04")]
        public string AisleTag
        {
            get => _aisleTag;
            set
            {
                _aisleTag = value;
                Invalidate();
            }
        }

        [Category("ASRS")]
        [DefaultValue(1.0)]
        [Description("Current horizontal crane bay position")]
        public double CurrentBay
        {
            get => _engine.Position.CurrentBay;
            set
            {
                _engine.Position.CurrentBay = Math.Max(1.0, Math.Min(_engine.Config.TotalBays, value));
                Invalidate();
            }
        }

        [Category("ASRS")]
        [DefaultValue(1.0)]
        [Description("Current vertical hoist tier position")]
        public double CurrentTier
        {
            get => _engine.Position.CurrentTier;
            set
            {
                _engine.Position.CurrentTier = Math.Max(1.0, Math.Min(_engine.Config.TotalTiers, value));
                Invalidate();
            }
        }

        [Category("ASRS")]
        [DefaultValue(0.0)]
        [Description("Telescopic fork stroke extension percentage (0.0 to 100.0%)")]
        public double ForkExtensionPct
        {
            get => _engine.Position.ForkExtensionPct;
            set
            {
                _engine.Position.ForkExtensionPct = Math.Max(0.0, Math.Min(100.0, value));
                Invalidate();
            }
        }

        [Category("ASRS")]
        [DefaultValue(AsrsForkDirection.Center)]
        public AsrsForkDirection ForkDirection
        {
            get => _engine.Position.Direction;
            set
            {
                _engine.Position.Direction = value;
                Invalidate();
            }
        }

        [Category("ASRS")]
        [DefaultValue(true)]
        public bool HasPallet
        {
            get => _engine.Payload.HasPallet;
            set
            {
                _engine.Payload.HasPallet = value;
                Invalidate();
            }
        }

        [Category("ASRS")]
        [DefaultValue("PLT-98421")]
        public string PalletBarcode
        {
            get => _engine.Payload.PalletBarcode;
            set
            {
                _engine.Payload.PalletBarcode = value;
                Invalidate();
            }
        }

        [Category("ASRS")]
        [DefaultValue(740.0)]
        public double WeightKg
        {
            get => _engine.Payload.WeightKg;
            set
            {
                _engine.Payload.WeightKg = Math.Max(0.0, value);
                Invalidate();
            }
        }

        #endregion

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var theme = ZeroTheme.Colors;

            // Background
            using (var bgBrush = new SolidBrush(theme.Background))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            // Top Header & Telemetry HUD
            DrawHeaderAndHud(g, theme);

            // Rack Grid Area
            int rackLeft = 24;
            int rackTop = 64;
            int rackWidth = Width - 48;
            int rackHeight = Height - rackTop - 24;

            if (rackWidth < 200 || rackHeight < 100)
                return;

            Rectangle rackRect = new Rectangle(rackLeft, rackTop, rackWidth, rackHeight);

            // Draw High-Bay Rack Grid
            DrawRackGrid(g, rackRect, theme);

            // Draw Bottom Rail & Top Guide
            DrawRails(g, rackRect, theme);

            // Draw Crane (Mast + Hoist Carriage + Forks + Pallet)
            DrawCraneAssembly(g, rackRect, theme);
        }

        private void DrawHeaderAndHud(Graphics g, ZeroThemePalette theme)
        {
            using (var fontTitle = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (var fontSmall = new Font("Segoe UI", 8.5f))
            using (var fontBold = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, $"{_aisleTag} — ASRS Stacker Crane", fontTitle, new Point(24, 14), theme.TextPrimary);

                // Right HUD: Pos (Bay/Tier), Payload, PPH
                int hudX = Width - 460;
                if (hudX > 220)
                {
                    string posStr = $"Bay: {_engine.Position.CurrentBay:F1} | Tier: {_engine.Position.CurrentTier:F1}";
                    TextRenderer.DrawText(g, posStr, fontSmall, new Point(hudX, 16), theme.TextSecondary);

                    string payloadStr = _engine.Payload.HasPallet
                        ? $"Pallet: {_engine.Payload.PalletBarcode} ({_engine.Payload.WeightKg:N0} kg)"
                        : "EMPTY CARRIAGE";
                    Color payloadColor = _engine.Payload.IsOverweight(_engine.Config.MaxPayloadWeightKg)
                        ? Color.FromArgb(239, 68, 68)
                        : (_engine.Payload.HasPallet ? Color.FromArgb(56, 189, 248) : theme.TextSecondary);

                    TextRenderer.DrawText(g, payloadStr, fontBold, new Point(hudX + 160, 16), payloadColor);

                    // PPH Badge
                    string pphStr = $"{_engine.Stats.HourlyThroughputPph:F0} PPH";
                    Rectangle pphBadge = new Rectangle(Width - 100, 12, 76, 24);
                    using (var badgeBrush = new SolidBrush(Color.FromArgb(30, 34, 197, 94)))
                    using (var badgePen = new Pen(Color.FromArgb(34, 197, 94), 1f))
                    {
                        g.FillRectangle(badgeBrush, pphBadge);
                        g.DrawRectangle(badgePen, pphBadge);
                    }
                    TextRenderer.DrawText(g, pphStr, fontBold, pphBadge, Color.FromArgb(34, 197, 94), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
        }

        private void DrawRackGrid(Graphics g, Rectangle rect, ZeroThemePalette theme)
        {
            int bays = _engine.Config.TotalBays;
            int tiers = _engine.Config.TotalTiers;

            float cellW = (float)rect.Width / bays;
            float cellH = (float)rect.Height / tiers;

            using (var cellBrush = new SolidBrush(theme.Surface))
            using (var beamPen = new Pen(theme.Border, 1f))
            {
                g.FillRectangle(cellBrush, rect);

                // Vertical uprights
                for (int b = 0; b <= bays; b++)
                {
                    float x = rect.Left + b * cellW;
                    g.DrawLine(beamPen, x, rect.Top, x, rect.Bottom);
                }

                // Horizontal shelf beams
                for (int t = 0; t <= tiers; t++)
                {
                    float y = rect.Top + t * cellH;
                    g.DrawLine(beamPen, rect.Left, y, rect.Right, y);
                }
            }
        }

        private void DrawRails(Graphics g, Rectangle rack, ZeroThemePalette theme)
        {
            using (var railPen = new Pen(Color.FromArgb(148, 163, 184), 3f))
            {
                // Top guide rail
                g.DrawLine(railPen, rack.Left - 8, rack.Top - 3, rack.Right + 8, rack.Top - 3);
                // Bottom floor travel rail
                g.DrawLine(railPen, rack.Left - 8, rack.Bottom + 3, rack.Right + 8, rack.Bottom + 3);
            }
        }

        private void DrawCraneAssembly(Graphics g, Rectangle rack, ZeroThemePalette theme)
        {
            int bays = Math.Max(1, _engine.Config.TotalBays);
            int tiers = Math.Max(1, _engine.Config.TotalTiers);

            float cellW = (float)rack.Width / bays;
            float cellH = (float)rack.Height / tiers;

            // Compute crane X center along aisle
            float craneX = rack.Left + (float)((_engine.Position.CurrentBay - 0.5) * cellW);
            // Compute carriage Y center along mast (Tier 1 is at bottom)
            float carriageY = rack.Bottom - (float)((_engine.Position.CurrentTier - 0.5) * cellH);

            // 1. Vertical Mast Structural Beam
            float mastW = Math.Max(12f, cellW * 0.45f);
            RectangleF mastRect = new RectangleF(craneX - mastW / 2, rack.Top - 4, mastW, rack.Height + 8);

            using (var mastBrush = new SolidBrush(Color.FromArgb(245, 158, 11))) // Industrial Safety Amber
            using (var mastPen = new Pen(Color.FromArgb(180, 83, 9), 1.5f))
            {
                g.FillRectangle(mastBrush, mastRect);
                g.DrawRectangle(mastPen, mastRect.X, mastRect.Y, mastRect.Width, mastRect.Height);
            }

            // Bottom bogie wheel truck
            using (var wheelBrush = new SolidBrush(Color.FromArgb(51, 65, 85)))
            {
                g.FillRectangle(wheelBrush, craneX - mastW, rack.Bottom, mastW * 2, 8);
            }

            // 2. Hoist Carriage Assembly
            float carrW = mastW * 1.6f;
            float carrH = Math.Max(16f, cellH * 0.7f);
            RectangleF carrRect = new RectangleF(craneX - carrW / 2, carriageY - carrH / 2, carrW, carrH);

            using (var carrBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (var carrPen = new Pen(Color.FromArgb(56, 189, 248), 1.5f))
            {
                g.FillRectangle(carrBrush, carrRect);
                g.DrawRectangle(carrPen, carrRect.X, carrRect.Y, carrRect.Width, carrRect.Height);
            }

            // 3. Telescopic Forks & Pallet Payload
            float forkStroke = (float)(_engine.Position.ForkExtensionPct / 100.0) * cellW * 0.9f;
            float forkX = craneX - carrW / 2;

            if (_engine.Position.Direction == AsrsForkDirection.Right)
                forkX += forkStroke;
            else if (_engine.Position.Direction == AsrsForkDirection.Left)
                forkX -= forkStroke;

            // Fork tines
            using (var tinePen = new Pen(Color.FromArgb(203, 213, 225), 2.5f))
            {
                g.DrawLine(tinePen, forkX - 8, carriageY + carrH * 0.3f, forkX + carrW + 8, carriageY + carrH * 0.3f);
            }

            // Pallet load
            if (_engine.Payload.HasPallet)
            {
                float palletW = carrW * 0.9f;
                float palletH = carrH * 0.6f;
                RectangleF palletRect = new RectangleF(forkX + (carrW - palletW) / 2, carriageY - carrH * 0.2f - palletH, palletW, palletH);

                using (var palletBrush = new SolidBrush(Color.FromArgb(56, 189, 248)))
                using (var palletPen = new Pen(Color.FromArgb(2, 132, 199), 1f))
                {
                    g.FillRectangle(palletBrush, palletRect);
                    g.DrawRectangle(palletPen, palletRect.X, palletRect.Y, palletRect.Width, palletRect.Height);
                }
            }
        }
    }
}
