using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.Rendering;
using ZeroUI.Core.Bms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Bms
{
    /// <summary>
    /// Air Handling Unit (AHU) mechanical cross-section visualizer.
    /// Renders mixing dampers, differential pressure filters, hydronic heating/cooling coils,
    /// and supply/return fans with real-time rotating blades, airflow vectors, and static pressure badges.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - BMS & HVAC")]
    [Description("Air Handling Unit (AHU) mechanical schematic with animated dampers, filters, coils, and fans")]
    public class AhuSchematic : Control
    {
        private readonly AhuEngine _engine = new AhuEngine();
        private IDisposable? _animSub;
        private string _unitTag = "AHU-01";

        public AhuSchematic()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            Size = new Size(680, 260);

            ZeroTheme.ThemeChanged += OnThemeChanged;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _animSub ??= ZeroAnimationClock.Subscribe((delta, frame) =>
            {
                if (IsHandleCreated && !IsDisposed && _engine.Fans.IsSupplyFanRunning)
                {
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

        [Category("BMS")]
        [Description("Access to the underlying thermodynamic computation engine")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public AhuEngine Engine => _engine;

        [Category("BMS")]
        [DefaultValue("AHU-01")]
        [Description("Equipment identifier or tag name")]
        public string UnitTag
        {
            get => _unitTag;
            set
            {
                _unitTag = value;
                Invalidate();
            }
        }

        [Category("BMS")]
        [DefaultValue(AhuOperatingMode.Cooling)]
        public AhuOperatingMode Mode
        {
            get => _engine.Mode;
            set
            {
                _engine.Mode = value;
                Invalidate();
            }
        }

        [Category("BMS")]
        [DefaultValue(20.0)]
        [Description("Outside air damper position percentage (0–100%)")]
        public double OutsideAirPct
        {
            get => _engine.Dampers.OutsideAirPct;
            set
            {
                _engine.Dampers.OutsideAirPct = Math.Max(0, Math.Min(100, value));
                Invalidate();
            }
        }

        [Category("BMS")]
        [DefaultValue(45.0)]
        [Description("Cooling hydronic valve opening percentage (0–100%)")]
        public double CoolingValvePct
        {
            get => _engine.Coils.CoolingValvePct;
            set
            {
                _engine.Coils.CoolingValvePct = Math.Max(0, Math.Min(100, value));
                Invalidate();
            }
        }

        [Category("BMS")]
        [DefaultValue(0.0)]
        [Description("Heating hydronic valve opening percentage (0–100%)")]
        public double HeatingValvePct
        {
            get => _engine.Coils.HeatingValvePct;
            set
            {
                _engine.Coils.HeatingValvePct = Math.Max(0, Math.Min(100, value));
                Invalidate();
            }
        }

        [Category("BMS")]
        [DefaultValue(1450.0)]
        [Description("Supply fan rotation speed in RPM")]
        public double SupplyFanRpm
        {
            get => _engine.Fans.SupplyFanRpm;
            set
            {
                _engine.Fans.SupplyFanRpm = Math.Max(0, value);
                Invalidate();
            }
        }

        [Category("BMS")]
        [DefaultValue(12500.0)]
        [Description("Supply airflow rate in CFM")]
        public double AirflowCfm
        {
            get => _engine.Air.AirflowCfm;
            set
            {
                _engine.Air.AirflowCfm = Math.Max(0, value);
                Invalidate();
            }
        }

        [Category("BMS")]
        [DefaultValue(14.5)]
        [Description("Supply air discharge temperature in °C")]
        public double SupplyTempC
        {
            get => _engine.Air.SupplyTempC;
            set
            {
                _engine.Air.SupplyTempC = value;
                Invalidate();
            }
        }

        [Category("BMS")]
        [DefaultValue(24.0)]
        [Description("Return air temperature in °C")]
        public double ReturnTempC
        {
            get => _engine.Air.ReturnTempC;
            set
            {
                _engine.Air.ReturnTempC = value;
                Invalidate();
            }
        }

        [Category("BMS")]
        [DefaultValue(32.0)]
        [Description("Outside ambient temperature in °C")]
        public double OutsideTempC
        {
            get => _engine.Air.OutsideTempC;
            set
            {
                _engine.Air.OutsideTempC = value;
                Invalidate();
            }
        }

        #endregion

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var theme = ZeroTheme.Colors;

            // Canvas Background
            using (var bgBrush = new SolidBrush(theme.Background))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            // Header: Tag, Mode, Airflow
            DrawHeader(g, theme);

            // Main Mechanical Duct Tunnel (Left = Intake, Right = Discharge)
            int tunnelLeft = 20;
            int tunnelTop = 50;
            int tunnelWidth = Width - 40;
            int tunnelHeight = Height - 70;

            if (tunnelWidth < 200 || tunnelHeight < 80)
                return;

            Rectangle tunnelRect = new Rectangle(tunnelLeft, tunnelTop, tunnelWidth, tunnelHeight);
            DrawTunnelCasing(g, tunnelRect, theme);

            // Section widths proportional to casing
            float secW = tunnelWidth / 5.0f;

            // 1. Mixing Chamber & Dampers
            RectangleF damperRect = new RectangleF(tunnelLeft, tunnelTop, secW, tunnelHeight);
            DrawDamperSection(g, damperRect, theme);

            // 2. Filter Bank (Pre + Final)
            RectangleF filterRect = new RectangleF(tunnelLeft + secW, tunnelTop, secW * 0.8f, tunnelHeight);
            DrawFilterSection(g, filterRect, theme);

            // 3. Hydronic Coils (Heating + Cooling)
            RectangleF coilRect = new RectangleF(tunnelLeft + secW * 1.8f, tunnelTop, secW * 1.2f, tunnelHeight);
            DrawCoilSection(g, coilRect, theme);

            // 4. Supply Fan Scroll & Rotating Impeller
            RectangleF fanRect = new RectangleF(tunnelLeft + secW * 3.0f, tunnelTop, secW * 1.1f, tunnelHeight);
            DrawFanSection(g, fanRect, theme);

            // 5. Discharge Duct, Static Pressure, & Airflow Vectors
            RectangleF dischargeRect = new RectangleF(tunnelLeft + secW * 4.1f, tunnelTop, secW * 0.9f, tunnelHeight);
            DrawDischargeSection(g, dischargeRect, theme);

            // Flow Particles across tunnel
            DrawAirflowParticles(g, tunnelRect, theme);
        }

        private void DrawHeader(Graphics g, ZeroThemePalette theme)
        {
            using (var fontBold = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (var fontSmall = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            {
                // Title Tag
                TextRenderer.DrawText(g, $"{_unitTag} — Air Handling Unit", fontBold, new Point(20, 14), theme.TextPrimary);

                // Operating Mode Badge
                Color modeColor = _engine.Mode switch
                {
                    AhuOperatingMode.Cooling => Color.FromArgb(56, 189, 248),
                    AhuOperatingMode.Heating => Color.FromArgb(249, 115, 22),
                    AhuOperatingMode.Economizer => Color.FromArgb(34, 197, 94),
                    AhuOperatingMode.Ventilation => Color.FromArgb(168, 85, 247),
                    AhuOperatingMode.Purge => Color.FromArgb(239, 68, 68),
                    _ => theme.TextSecondary
                };

                string modeText = $"Mode: {_engine.Mode.ToString().ToUpperInvariant()}";
                var modeSize = TextRenderer.MeasureText(g, modeText, fontSmall);
                Rectangle modeBadge = new Rectangle(240, 14, modeSize.Width + 14, 22);

                using (var fillBrush = new SolidBrush(Color.FromArgb(35, modeColor)))
                using (var borderPen = new Pen(modeColor, 1f))
                {
                    g.FillRectangle(fillBrush, modeBadge);
                    g.DrawRectangle(borderPen, modeBadge);
                }
                TextRenderer.DrawText(g, modeText, fontSmall, modeBadge, modeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // Airflow Metric (Right-aligned)
                string flowText = $"{_engine.Air.AirflowCfm:N0} CFM | SP: {_engine.Fans.SupplyStaticPressureInWg:F2} in.wg";
                var flowSize = TextRenderer.MeasureText(g, flowText, fontSmall);
                TextRenderer.DrawText(g, flowText, fontSmall, new Point(Width - flowSize.Width - 20, 16), theme.TextSecondary);
            }
        }

        private void DrawTunnelCasing(Graphics g, Rectangle rect, ZeroThemePalette theme)
        {
            using (var casingBrush = new SolidBrush(theme.Surface))
            using (var borderPen = new Pen(theme.Border, 1.5f))
            {
                g.FillRectangle(casingBrush, rect);
                g.DrawRectangle(borderPen, rect);
            }

            // Outer structural flanges
            using (var flangePen = new Pen(Color.FromArgb(40, theme.TextSecondary), 1f))
            {
                g.DrawLine(flangePen, rect.Left - 4, rect.Top - 4, rect.Left - 4, rect.Bottom + 4);
                g.DrawLine(flangePen, rect.Right + 4, rect.Top - 4, rect.Right + 4, rect.Bottom + 4);
            }
        }

        private void DrawDamperSection(Graphics g, RectangleF rect, ZeroThemePalette theme)
        {
            using (var borderPen = new Pen(theme.Border, 1f) { DashStyle = DashStyle.Dot })
            using (var font = new Font("Segoe UI", 8f))
            {
                g.DrawLine(borderPen, rect.Right, rect.Top, rect.Right, rect.Bottom);

                // Label
                TextRenderer.DrawText(g, "Mixing Box", font, new Point((int)rect.Left + 8, (int)rect.Top + 6), theme.TextSecondary);

                // Outside Air Damper (Left vertical bank)
                float damperX = rect.Left + 18;
                float damperTop = rect.Top + 30;
                float damperH = rect.Height - 50;

                DrawDamperLouvers(g, damperX, damperTop, 36, damperH, (float)OutsideAirPct, theme);

                // Damper Badge
                string pctText = $"OA: {OutsideAirPct:0}%";
                TextRenderer.DrawText(g, pctText, font, new Point((int)rect.Left + 12, (int)rect.Bottom - 20),
                    OutsideAirPct > 0 ? Color.FromArgb(56, 189, 248) : theme.TextSecondary);
            }
        }

        private void DrawDamperLouvers(Graphics g, float x, float y, float w, float h, float pct, ZeroThemePalette theme)
        {
            int bladeCount = 4;
            float bladeSpacing = h / bladeCount;
            float angle = (pct / 100.0f) * 80.0f; // 0° = closed flat, 80° = wide open

            using (var bladePen = new Pen(theme.Primary, 2.5f))
            using (var framePen = new Pen(theme.Border, 1f))
            {
                g.DrawRectangle(framePen, x, y, w, h);

                for (int i = 0; i < bladeCount; i++)
                {
                    float cy = y + (i + 0.5f) * bladeSpacing;
                    float rad = angle * (float)Math.PI / 180.0f;
                    float dx = (w * 0.4f) * (float)Math.Cos(rad);
                    float dy = (w * 0.4f) * (float)Math.Sin(rad);

                    g.DrawLine(bladePen, x + w / 2 - dx, cy - dy, x + w / 2 + dx, cy + dy);
                    g.FillEllipse(Brushes.DarkGray, x + w / 2 - 2, cy - 2, 4, 4);
                }
            }
        }

        private void DrawFilterSection(Graphics g, RectangleF rect, ZeroThemePalette theme)
        {
            using (var font = new Font("Segoe UI", 8f))
            using (var borderPen = new Pen(theme.Border, 1f) { DashStyle = DashStyle.Dot })
            {
                g.DrawLine(borderPen, rect.Right, rect.Top, rect.Right, rect.Bottom);

                TextRenderer.DrawText(g, "Filters", font, new Point((int)rect.Left + 8, (int)rect.Top + 6), theme.TextSecondary);

                // Filter zigzag pleats
                float midX = rect.Left + rect.Width * 0.5f;
                float filterTop = rect.Top + 28;
                float filterH = rect.Height - 56;

                Color filterColor = _engine.Filters.IsDirty ? Color.FromArgb(239, 68, 68) : Color.FromArgb(34, 197, 94);

                using (var filterPen = new Pen(filterColor, 2f))
                {
                    int pleats = 8;
                    float pleatH = filterH / pleats;
                    for (int i = 0; i < pleats; i++)
                    {
                        float y1 = filterTop + i * pleatH;
                        float y2 = y1 + pleatH * 0.5f;
                        float y3 = y1 + pleatH;
                        g.DrawLine(filterPen, midX - 8, y1, midX + 8, y2);
                        g.DrawLine(filterPen, midX + 8, y2, midX - 8, y3);
                    }
                }

                // Delta P badge
                string dpText = $"ΔP: {_engine.Filters.FinalFilterDeltaP:F2}\"";
                TextRenderer.DrawText(g, dpText, font, new Point((int)rect.Left + 4, (int)rect.Bottom - 20), filterColor);
            }
        }

        private void DrawCoilSection(Graphics g, RectangleF rect, ZeroThemePalette theme)
        {
            using (var font = new Font("Segoe UI", 8f))
            using (var borderPen = new Pen(theme.Border, 1f) { DashStyle = DashStyle.Dot })
            {
                g.DrawLine(borderPen, rect.Right, rect.Top, rect.Right, rect.Bottom);

                // 1. Heating Coil (Left)
                float heatX = rect.Left + rect.Width * 0.28f;
                DrawSerpentineCoil(g, heatX, rect.Top + 28, 14, rect.Height - 56, Color.FromArgb(239, 68, 68));
                string heatText = $"Heat: {HeatingValvePct:0}%";
                TextRenderer.DrawText(g, heatText, font, new Point((int)rect.Left + 4, (int)rect.Bottom - 20),
                    HeatingValvePct > 0 ? Color.FromArgb(239, 68, 68) : theme.TextSecondary);

                // 2. Cooling Coil (Right)
                float coolX = rect.Left + rect.Width * 0.72f;
                DrawSerpentineCoil(g, coolX, rect.Top + 28, 14, rect.Height - 56, Color.FromArgb(56, 189, 248));
                string coolText = $"Cool: {CoolingValvePct:0}%";
                TextRenderer.DrawText(g, coolText, font, new Point((int)rect.Left + (int)(rect.Width * 0.5f), (int)rect.Bottom - 20),
                    CoolingValvePct > 0 ? Color.FromArgb(56, 189, 248) : theme.TextSecondary);
            }
        }

        private void DrawSerpentineCoil(Graphics g, float cx, float y, float w, float h, Color color)
        {
            using (var coilPen = new Pen(color, 2.5f))
            {
                int loops = 6;
                float loopH = h / loops;
                for (int i = 0; i < loops; i++)
                {
                    float topY = y + i * loopH;
                    g.DrawArc(coilPen, cx - w / 2, topY, w, loopH, -90, 180);
                    g.DrawArc(coilPen, cx - w / 2, topY + loopH * 0.5f, w, loopH, 90, 180);
                }
            }
        }

        private void DrawFanSection(Graphics g, RectangleF rect, ZeroThemePalette theme)
        {
            using (var font = new Font("Segoe UI", 8f))
            using (var borderPen = new Pen(theme.Border, 1f) { DashStyle = DashStyle.Dot })
            {
                g.DrawLine(borderPen, rect.Right, rect.Top, rect.Right, rect.Bottom);

                TextRenderer.DrawText(g, "Supply Fan", font, new Point((int)rect.Left + 6, (int)rect.Top + 6), theme.TextSecondary);

                float cx = rect.Left + rect.Width * 0.5f;
                float cy = rect.Top + rect.Height * 0.5f;
                float radius = Math.Min(rect.Width, rect.Height) * 0.32f;

                // Fan housing scroll
                using (var housingPen = new Pen(theme.TextSecondary, 2f))
                {
                    g.DrawEllipse(housingPen, cx - radius, cy - radius, radius * 2, radius * 2);
                }

                // Rotating Impeller Blades
                float baseAngle = 0f;
                if (_engine.Fans.IsSupplyFanRunning)
                {
                    baseAngle = (float)(ZeroAnimationClock.TotalElapsedTime * (_engine.Fans.SupplyFanRpm / 60.0) * 360.0) % 360f;
                }

                int blades = 6;
                Color bladeColor = _engine.Fans.IsSupplyFanRunning ? Color.FromArgb(34, 197, 94) : theme.TextSecondary;
                using (var bladePen = new Pen(bladeColor, 2.5f))
                {
                    for (int i = 0; i < blades; i++)
                    {
                        float angleDeg = baseAngle + (i * 360f / blades);
                        float rad = angleDeg * (float)Math.PI / 180f;
                        float bx = cx + (float)Math.Cos(rad) * (radius - 3);
                        float by = cy + (float)Math.Sin(rad) * (radius - 3);
                        g.DrawLine(bladePen, cx, cy, bx, by);
                    }
                }

                // Hub
                g.FillEllipse(Brushes.White, cx - 4, cy - 4, 8, 8);

                // RPM Badge
                string rpmText = $"{_engine.Fans.SupplyFanRpm:0} RPM";
                TextRenderer.DrawText(g, rpmText, font, new Point((int)rect.Left + 6, (int)rect.Bottom - 20), bladeColor);
            }
        }

        private void DrawDischargeSection(Graphics g, RectangleF rect, ZeroThemePalette theme)
        {
            using (var font = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var fontSmall = new Font("Segoe UI", 7.5f))
            {
                // Discharge Temp Card
                Rectangle tempCard = new Rectangle((int)rect.Left + 8, (int)rect.Top + 24, (int)rect.Width - 16, 42);
                using (var cardBrush = new SolidBrush(Color.FromArgb(25, 56, 189, 248)))
                using (var cardPen = new Pen(Color.FromArgb(56, 189, 248), 1f))
                {
                    g.FillRectangle(cardBrush, tempCard);
                    g.DrawRectangle(cardPen, tempCard);
                }
                TextRenderer.DrawText(g, "SUPPLY", fontSmall, new Point(tempCard.Left + 4, tempCard.Top + 3), theme.TextSecondary);
                TextRenderer.DrawText(g, $"{SupplyTempC:F1} °C", font, new Point(tempCard.Left + 4, tempCard.Top + 18), Color.FromArgb(56, 189, 248));

                // Return Temp Card
                Rectangle retCard = new Rectangle((int)rect.Left + 8, (int)rect.Top + 74, (int)rect.Width - 16, 42);
                using (var cardBrush = new SolidBrush(Color.FromArgb(20, theme.Border)))
                using (var cardPen = new Pen(theme.Border, 1f))
                {
                    g.FillRectangle(cardBrush, retCard);
                    g.DrawRectangle(cardPen, retCard);
                }
                TextRenderer.DrawText(g, "RETURN", fontSmall, new Point(retCard.Left + 4, retCard.Top + 3), theme.TextSecondary);
                TextRenderer.DrawText(g, $"{ReturnTempC:F1} °C", font, new Point(retCard.Left + 4, retCard.Top + 18), theme.TextPrimary);
            }
        }

        private void DrawAirflowParticles(Graphics g, Rectangle tunnel, ZeroThemePalette theme)
        {
            if (!_engine.Fans.IsSupplyFanRunning)
                return;

            float phase = ZeroAnimationClock.FluidPhase; // 0.0 to 1.0 cycle
            int count = 5;
            float spacing = tunnel.Width / (float)count;
            float cy = tunnel.Top + tunnel.Height * 0.5f;

            using (var arrowPen = new Pen(Color.FromArgb(90, 56, 189, 248), 1.8f))
            {
                for (int i = 0; i < count; i++)
                {
                    float x = tunnel.Left + ((i + phase) * spacing) % tunnel.Width;
                    // Draw small chevron >
                    g.DrawLine(arrowPen, x - 5, cy - 6, x, cy);
                    g.DrawLine(arrowPen, x, cy, x - 5, cy + 6);
                }
            }
        }
    }
}
