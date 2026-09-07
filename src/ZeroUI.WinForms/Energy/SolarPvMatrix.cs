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
    /// Solar Photovoltaic (PV) utility/commercial generation plant string matrix visualizer.
    /// Monitors real-time string currents, voltages, MPPT performance, and string mismatch/shading detection.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Energy & Smart Grid")]
    [Description("Solar PV string matrix array with MPPT string mismatch, shading, and blown fuse diagnostics")]
    public class SolarPvMatrix : Control
    {
        private readonly SolarPvEngine _engine = new SolarPvEngine();
        private IDisposable? _animSub;

        public SolarPvMatrix()
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

        [Category("Solar PV")]
        [Description("Access to the underlying Solar PV plant and mismatch engine")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SolarPvEngine Engine => _engine;

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

            var plant = _engine.Plant;

            // Header Banner
            DrawHeader(g, plant, theme);

            // KPI Telemetry Strip
            int kpiY = 56;
            int kpiHeight = 52;
            DrawKpiStrip(g, new Rectangle(20, kpiY, Width - 40, kpiHeight), plant, theme);

            // 16-String Grid Canvas
            int gridY = kpiY + kpiHeight + 14;
            int gridHeight = Height - gridY - 16;
            if (gridHeight < 120 || Width < 300)
                return;

            DrawStringGrid(g, new Rectangle(20, gridY, Width - 40, gridHeight), plant, theme);
        }

        private void DrawHeader(Graphics g, SolarPvPlant plant, ZeroThemePalette theme)
        {
            using (var fontTitle = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (var fontBold = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                string title = $"{plant.PlantId} — {plant.Name}";
                TextRenderer.DrawText(g, title, fontTitle, new Point(20, 14), theme.TextPrimary);

                // Generation Status Pill
                Rectangle pillRect = new Rectangle(Width - 190, 12, 170, 26);
                using (var pillBrush = new SolidBrush(Color.FromArgb(25, 34, 197, 94)))
                using (var pillPen = new Pen(Color.FromArgb(34, 197, 94), 1f))
                {
                    g.FillRectangle(pillBrush, pillRect);
                    g.DrawRectangle(pillPen, pillRect);
                }
                TextRenderer.DrawText(g, "GENERATING (MPPT OK)", fontBold, pillRect, Color.FromArgb(34, 197, 94), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawKpiStrip(Graphics g, Rectangle rect, SolarPvPlant plant, ZeroThemePalette theme)
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
                // KPI 1: Fleet Power
                DrawKpiCell(g, new Rectangle(rect.X, rect.Y, colW, rect.Height), "FLEET POWER", $"{plant.TotalPowerKw:F1} kW", Color.FromArgb(34, 197, 94), fontLabel, fontVal, theme);

                // KPI 2: Irradiance
                DrawKpiCell(g, new Rectangle(rect.X + colW, rect.Y, colW, rect.Height), "SOLAR IRRADIANCE", $"{plant.SolarIrradianceWm2:F0} W/m²", Color.FromArgb(245, 158, 11), fontLabel, fontVal, theme);

                // KPI 3: PR %
                DrawKpiCell(g, new Rectangle(rect.X + colW * 2, rect.Y, colW, rect.Height), "PERFORMANCE RATIO", $"{plant.PerformanceRatioPct:F1}%", Color.FromArgb(59, 130, 246), fontLabel, fontVal, theme);

                // KPI 4: Cell Temp
                DrawKpiCell(g, new Rectangle(rect.X + colW * 3, rect.Y, colW, rect.Height), "CELL TEMPERATURE", $"{plant.CellTempC:F1} °C", theme.TextPrimary, fontLabel, fontVal, theme);

                // KPI 5: Strings Active
                int activeCount = 0;
                for (int i = 0; i < plant.Strings.Count; i++)
                    if (plant.Strings[i].Status == PvStringStatus.Normal) activeCount++;

                string strRatio = $"{activeCount}/{plant.Strings.Count} Healthy";
                Color statColor = activeCount == plant.Strings.Count ? Color.FromArgb(34, 197, 94) : Color.FromArgb(245, 158, 11);
                DrawKpiCell(g, new Rectangle(rect.X + colW * 4, rect.Y, rect.Right - (rect.X + colW * 4), rect.Height), "FLEET STRINGS", strRatio, statColor, fontLabel, fontVal, theme);
            }
        }

        private void DrawKpiCell(Graphics g, Rectangle r, string label, string val, Color valColor, Font fLabel, Font fVal, ZeroThemePalette theme)
        {
            TextRenderer.DrawText(g, label, fLabel, new Point(r.X + 12, r.Y + 6), theme.TextSecondary);
            TextRenderer.DrawText(g, val, fVal, new Point(r.X + 12, r.Y + 22), valColor);
        }

        private void DrawStringGrid(Graphics g, Rectangle rect, SolarPvPlant plant, ZeroThemePalette theme)
        {
            int cols = 4;
            int rows = 4;
            int gap = 8;
            int cellW = (rect.Width - (cols - 1) * gap) / cols;
            int cellH = (rect.Height - (rows - 1) * gap) / rows;

            using (var fontTag = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (var fontVal = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var fontSmall = new Font("Segoe UI", 7.5f))
            {
                for (int i = 0; i < plant.Strings.Count && i < 16; i++)
                {
                    int r = i / cols;
                    int c = i % cols;
                    int cellX = rect.X + c * (cellW + gap);
                    int cellY = rect.Y + r * (cellH + gap);
                    Rectangle cellRect = new Rectangle(cellX, cellY, cellW, cellH);

                    var pvStr = plant.Strings[i];
                    DrawStringCell(g, cellRect, pvStr, fontTag, fontVal, fontSmall, theme);
                }
            }
        }

        private void DrawStringCell(Graphics g, Rectangle rect, PvString pvStr, Font fTag, Font fVal, Font fSmall, ZeroThemePalette theme)
        {
            Color statusColor = pvStr.Status == PvStringStatus.Normal ? Color.FromArgb(34, 197, 94) :
                                pvStr.Status == PvStringStatus.Shaded ? Color.FromArgb(245, 158, 11) :
                                pvStr.Status == PvStringStatus.BlownFuse ? Color.FromArgb(239, 68, 68) :
                                Color.FromArgb(168, 85, 247);

            // Card background & left status border
            using (var bgBrush = new SolidBrush(Color.FromArgb(14, theme.TextPrimary)))
            using (var borderPen = new Pen(theme.Border, 1f))
            using (var statusBrush = new SolidBrush(statusColor))
            {
                g.FillRectangle(bgBrush, rect);
                g.DrawRectangle(borderPen, rect);
                g.FillRectangle(statusBrush, rect.X, rect.Y, 4, rect.Height);
            }

            // String & Inverter Label
            string title = $"STR-{pvStr.StringId:00} (INV-{pvStr.InverterId})";
            TextRenderer.DrawText(g, title, fTag, new Point(rect.X + 10, rect.Y + 6), theme.TextPrimary);

            // Status badge text
            string statText = pvStr.Status.ToString().ToUpperInvariant();
            TextRenderer.DrawText(g, statText, fSmall, new Point(rect.Right - 70, rect.Y + 7), statusColor);

            // Values: Power kW
            int valY = rect.Y + 26;
            TextRenderer.DrawText(g, $"{pvStr.PowerKw:F2} kW", fVal, new Point(rect.X + 10, valY), theme.TextPrimary);

            // Telemetry: Voltage & Current
            int teleY = valY + 20;
            string teleStr = $"{pvStr.VoltageV:F0} V | {pvStr.CurrentA:F1} A";
            TextRenderer.DrawText(g, teleStr, fSmall, new Point(rect.X + 10, teleY), theme.TextSecondary);
        }
    }
}
