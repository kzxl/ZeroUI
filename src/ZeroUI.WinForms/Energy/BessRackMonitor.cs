using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.Energy;
using ZeroUI.Core.Rendering;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Energy
{
    /// <summary>
    /// Battery Energy Storage System (BESS) high-voltage rack and module health visualizer.
    /// Features SoC/SoH gauges, 16-cell module balancing heatmaps, and early thermal runaway precursor detection.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Energy & Smart Grid")]
    [Description("BESS High-Voltage Rack telemetry with cell voltage balancing heatmap and thermal runaway precursor alarm")]
    public class BessRackMonitor : Control
    {
        private readonly BessEngine _engine = new BessEngine();
        private IDisposable? _animSub;

        public BessRackMonitor()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            Size = new Size(820, 420);

            ZeroTheme.ThemeChanged += OnThemeChanged;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _animSub ??= ZeroAnimationClock.Subscribe((delta, frame) =>
            {
                if (IsHandleCreated && !IsDisposed)
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

        [Category("BESS")]
        [Description("Access to the underlying BESS rack and module computation engine")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public BessEngine Engine => _engine;

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

            var rack = _engine.Rack;
            var worstRisk = _engine.OverallRisk;

            // Header Banner (Rack Name, SoC, SoH, Runaway risk pill)
            DrawHeader(g, rack, worstRisk, theme);

            // Telemetry KPIs Strip
            int kpiY = 56;
            int kpiHeight = 52;
            DrawKpiStrip(g, new Rectangle(20, kpiY, Width - 40, kpiHeight), rack, theme);

            // Module Balancing Heatmap List
            int modulesY = kpiY + kpiHeight + 14;
            int modulesHeight = Height - modulesY - 16;
            if (modulesHeight < 100 || Width < 300)
                return;

            DrawModuleList(g, new Rectangle(20, modulesY, Width - 40, modulesHeight), rack, theme);
        }

        private void DrawHeader(Graphics g, BessRack rack, ThermalRunawayRisk worstRisk, ZeroThemePalette theme)
        {
            using (var fontTitle = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (var fontBold = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                string title = $"{rack.RackId} — {rack.Name}";
                TextRenderer.DrawText(g, title, fontTitle, new Point(20, 14), theme.TextPrimary);

                // Runaway Risk Pill
                Color riskColor = worstRisk == ThermalRunawayRisk.Normal ? Color.FromArgb(34, 197, 94) :
                                  worstRisk == ThermalRunawayRisk.Elevated ? Color.FromArgb(59, 130, 246) :
                                  worstRisk == ThermalRunawayRisk.Warning ? Color.FromArgb(245, 158, 11) :
                                  Color.FromArgb(239, 68, 68);

                string riskText = worstRisk == ThermalRunawayRisk.Normal ? "THERMAL: STABLE" :
                                  worstRisk == ThermalRunawayRisk.Elevated ? "THERMAL: ELEVATED" :
                                  worstRisk == ThermalRunawayRisk.Warning ? "THERMAL: WARNING" :
                                  "RUNAWAY PRECURSOR ALARM";

                Rectangle pillRect = new Rectangle(Width - 240, 12, 220, 26);
                using (var pillBrush = new SolidBrush(Color.FromArgb(25, riskColor)))
                using (var pillPen = new Pen(riskColor, 1.2f))
                {
                    g.FillRectangle(pillBrush, pillRect);
                    g.DrawRectangle(pillPen, pillRect);
                }
                TextRenderer.DrawText(g, riskText, fontBold, pillRect, riskColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawKpiStrip(Graphics g, Rectangle rect, BessRack rack, ZeroThemePalette theme)
        {
            using (var cardBrush = new SolidBrush(Color.FromArgb(12, theme.TextPrimary)))
            using (var borderPen = new Pen(theme.Border, 1f))
            {
                g.FillRectangle(cardBrush, rect);
                g.DrawRectangle(borderPen, rect);
            }

            int colW = rect.Width / 5;
            using (var fontLabel = new Font("Segoe UI", 7.5f))
            using (var fontVal = new Font("Segoe UI", 11f, FontStyle.Bold))
            {
                // KPI 1: SoC
                DrawKpiCell(g, new Rectangle(rect.X, rect.Y, colW, rect.Height), "STATE OF CHARGE", $"{rack.SocPct:F1}%", Color.FromArgb(34, 197, 94), fontLabel, fontVal, theme);

                // KPI 2: SoH
                DrawKpiCell(g, new Rectangle(rect.X + colW, rect.Y, colW, rect.Height), "BATTERY HEALTH (SoH)", $"{rack.SohPct:F1}%", Color.FromArgb(59, 130, 246), fontLabel, fontVal, theme);

                // KPI 3: String Voltage
                DrawKpiCell(g, new Rectangle(rect.X + colW * 2, rect.Y, colW, rect.Height), "STRING VOLTAGE", $"{rack.StringVoltageV:F1} V", theme.TextPrimary, fontLabel, fontVal, theme);

                // KPI 4: Current / Power
                string pwrStr = $"{rack.PowerKw:F1} kW ({rack.CurrentAmps:F1} A)";
                DrawKpiCell(g, new Rectangle(rect.X + colW * 3, rect.Y, colW, rect.Height), "POWER FLOW", pwrStr, theme.TextPrimary, fontLabel, fontVal, theme);

                // KPI 5: Max Cell Delta mV
                double maxDelta = _engine.RackMaxDeltaCellMv;
                Color deltaColor = maxDelta < 25.0 ? Color.FromArgb(34, 197, 94) :
                                   maxDelta < 45.0 ? Color.FromArgb(245, 158, 11) : Color.FromArgb(239, 68, 68);
                DrawKpiCell(g, new Rectangle(rect.X + colW * 4, rect.Y, rect.Right - (rect.X + colW * 4), rect.Height), "MAX CELL DELTA", $"{maxDelta:F1} mV", deltaColor, fontLabel, fontVal, theme);
            }
        }

        private void DrawKpiCell(Graphics g, Rectangle r, string label, string val, Color valColor, Font fLabel, Font fVal, ZeroThemePalette theme)
        {
            TextRenderer.DrawText(g, label, fLabel, new Point(r.X + 12, r.Y + 6), theme.TextSecondary);
            TextRenderer.DrawText(g, val, fVal, new Point(r.X + 12, r.Y + 22), valColor);
        }

        private void DrawModuleList(Graphics g, Rectangle rect, BessRack rack, ZeroThemePalette theme)
        {
            using (var boxBrush = new SolidBrush(Color.FromArgb(8, theme.TextPrimary)))
            using (var borderPen = new Pen(theme.Border, 1f))
            using (var fontHeader = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            {
                g.FillRectangle(boxBrush, rect);
                g.DrawRectangle(borderPen, rect);
                TextRenderer.DrawText(g, "MODULE BALANCING & THERMAL SUPERVISION (16 CELLS PER MODULE)", fontHeader, new Point(rect.X + 14, rect.Y + 8), theme.TextSecondary);
            }

            int count = rack.Modules.Count;
            if (count == 0) return;

            int listTop = rect.Y + 28;
            int listHeight = rect.Height - 34;
            int rowH = listHeight / count;
            if (rowH < 24) rowH = 24;

            using (var fontText = new Font("Segoe UI", 8f))
            using (var fontMono = new Font("Consolas", 8f))
            {
                for (int i = 0; i < count; i++)
                {
                    int rowY = listTop + i * rowH;
                    if (rowY + rowH > rect.Bottom) break;

                    var mod = rack.Modules[i];
                    var risk = BessEngine.EvaluateRunawayRisk(mod);

                    // Row separator line
                    if (i > 0)
                    {
                        using (var linePen = new Pen(Color.FromArgb(20, theme.TextPrimary), 1f))
                        {
                            g.DrawLine(linePen, rect.X + 10, rowY, rect.Right - 10, rowY);
                        }
                    }

                    // Module Name
                    TextRenderer.DrawText(g, mod.Name, fontText, new Point(rect.X + 14, rowY + 6), theme.TextPrimary);

                    // Temperature & dT/dt
                    string tempStr = $"{mod.TemperatureC:F1} °C ({mod.RateOfTemperatureRiseCpm:F1} °C/m)";
                    TextRenderer.DrawText(g, tempStr, fontMono, new Point(rect.X + 85, rowY + 6),
                        mod.TemperatureC >= 50.0 ? Color.FromArgb(239, 68, 68) : theme.TextSecondary);

                    // 16-cell voltage balancing micro-heatmap
                    int cellHeatmapX = rect.X + 240;
                    int cellHeatmapW = rect.Width - 360;
                    if (cellHeatmapW > 80 && mod.CellVoltages != null && mod.CellVoltages.Length > 0)
                    {
                        DrawCellHeatmap(g, cellHeatmapX, rowY + 7, cellHeatmapW, rowH - 14, mod);
                    }

                    // Cell Delta mV
                    Color deltaColor = mod.DeltaCellMv < 25.0 ? Color.FromArgb(34, 197, 94) :
                                       mod.DeltaCellMv < 45.0 ? Color.FromArgb(245, 158, 11) : Color.FromArgb(239, 68, 68);
                    TextRenderer.DrawText(g, $"Δ {mod.DeltaCellMv:F0} mV", fontMono, new Point(rect.Right - 100, rowY + 6), deltaColor);
                }
            }
        }

        private void DrawCellHeatmap(Graphics g, int x, int y, int width, int height, BessModule mod)
        {
            int cellCount = mod.CellVoltages.Length;
            if (cellCount == 0) return;

            float cellW = (float)width / cellCount;
            double minV = 3.20;
            double maxV = 3.30;

            for (int c = 0; c < cellCount; c++)
            {
                double v = mod.CellVoltages[c];
                // Map v between 3.20V and 3.30V
                float norm = (float)Math.Max(0.0, Math.Min(1.0, (v - minV) / (maxV - minV)));

                // Color interpolation: blueish to emerald
                int r = (int)(20 + (1.0f - norm) * 40);
                int gr = (int)(150 + norm * 80);
                int b = (int)(200 - norm * 100);
                Color cellColor = Color.FromArgb(Math.Min(255, r), Math.Min(255, gr), Math.Min(255, b));

                RectangleF cRect = new RectangleF(x + c * cellW + 1f, y, Math.Max(2f, cellW - 2f), height);
                using (var cBrush = new SolidBrush(cellColor))
                {
                    g.FillRectangle(cBrush, cRect);
                }
            }
        }
    }
}
