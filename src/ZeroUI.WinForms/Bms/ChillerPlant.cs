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
    /// Central Chiller Plant schematic visualizer.
    /// Renders water chillers, cooling towers, primary/secondary pumps, and live thermodynamic
    /// efficiency meters (Coefficient of Performance COP and kW/Ton).
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - BMS & HVAC")]
    [Description("Central Chiller Plant visualizer with animated water loops and efficiency gauges")]
    public class ChillerPlant : Control
    {
        private readonly ChillerPlantEngine _engine = new ChillerPlantEngine();
        private IDisposable? _animSub;
        private string _plantTitle = "Central Utility Plant";

        public ChillerPlant()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            Size = new Size(720, 320);

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

        [Category("BMS")]
        [Description("Access to the underlying thermodynamic chiller plant computation engine")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ChillerPlantEngine Engine => _engine;

        [Category("BMS")]
        [DefaultValue("Central Utility Plant")]
        public string PlantTitle
        {
            get => _plantTitle;
            set
            {
                _plantTitle = value;
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

            // Top Header & Efficiency HUD
            DrawHeaderAndHud(g, theme);

            // Layout split: Left = Chillers & Evaporator Loop, Right = Cooling Towers & Condenser Loop
            int mainTop = 64;
            int mainHeight = Height - mainTop - 16;
            int halfWidth = (Width - 40) / 2;

            if (halfWidth < 180 || mainHeight < 120)
                return;

            Rectangle chillerArea = new Rectangle(20, mainTop, halfWidth, mainHeight);
            Rectangle towerArea = new Rectangle(20 + halfWidth + 10, mainTop, halfWidth, mainHeight);

            DrawChillersSection(g, chillerArea, theme);
            DrawTowersSection(g, towerArea, theme);

            // Water Loop Interconnect Pipes
            DrawInterconnectPipes(g, chillerArea, towerArea, theme);
        }

        private void DrawHeaderAndHud(Graphics g, ZeroThemePalette theme)
        {
            using (var fontTitle = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (var fontSmall = new Font("Segoe UI", 8.5f))
            using (var fontBold = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                // Title
                TextRenderer.DrawText(g, _plantTitle, fontTitle, new Point(20, 14), theme.TextPrimary);

                // Right HUD: Total Load, Power, kW/Ton, COP
                int hudX = Width - 420;
                if (hudX > 220)
                {
                    // Total Load
                    string loadStr = $"Load: {_engine.TotalActualTons:N0} TR / {_engine.TotalRatedTons:N0} TR";
                    TextRenderer.DrawText(g, loadStr, fontSmall, new Point(hudX, 16), theme.TextSecondary);

                    // Power
                    string pwrStr = $"{_engine.TotalPowerKw:N0} kW";
                    TextRenderer.DrawText(g, pwrStr, fontSmall, new Point(hudX + 160, 16), theme.TextSecondary);

                    // Efficiency Badge: kW/Ton & COP
                    double kwTon = _engine.PlantAverageKwPerTon;
                    double cop = _engine.PlantAverageCop;

                    Color effColor = kwTon < 0.65 ? Color.FromArgb(34, 197, 94) :
                                     kwTon < 0.80 ? Color.FromArgb(245, 158, 11) :
                                     Color.FromArgb(239, 68, 68);

                    Rectangle effBadge = new Rectangle(Width - 160, 10, 140, 26);
                    using (var badgeBrush = new SolidBrush(Color.FromArgb(30, effColor)))
                    using (var badgePen = new Pen(effColor, 1f))
                    {
                        g.FillRectangle(badgeBrush, effBadge);
                        g.DrawRectangle(badgePen, effBadge);
                    }

                    string effStr = $"{kwTon:F2} kW/TR | COP {cop:F1}";
                    TextRenderer.DrawText(g, effStr, fontBold, effBadge, effColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
        }

        private void DrawChillersSection(Graphics g, Rectangle rect, ZeroThemePalette theme)
        {
            using (var casingBrush = new SolidBrush(theme.Surface))
            using (var borderPen = new Pen(theme.Border, 1f))
            using (var fontHeader = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var fontSmall = new Font("Segoe UI", 8f))
            {
                g.FillRectangle(casingBrush, rect);
                g.DrawRectangle(borderPen, rect);

                TextRenderer.DrawText(g, "WATER CHILLERS (CHW CIRCUIT)", fontHeader, new Point(rect.Left + 12, rect.Top + 8), Color.FromArgb(56, 189, 248));

                int chillerCount = _engine.Chillers.Count;
                if (chillerCount == 0) return;

                int itemHeight = (rect.Height - 44) / chillerCount;

                for (int i = 0; i < chillerCount; i++)
                {
                    var ch = _engine.Chillers[i];
                    Rectangle itemRect = new Rectangle(rect.Left + 10, rect.Top + 32 + i * itemHeight, rect.Width - 20, itemHeight - 8);

                    // Chiller Vessel Shell
                    using (var vesselBrush = new SolidBrush(Color.FromArgb(25, 56, 189, 248)))
                    using (var vesselPen = new Pen(ch.Status == ChillerStatus.Running ? Color.FromArgb(56, 189, 248) : theme.Border, 1.5f))
                    {
                        g.FillRectangle(vesselBrush, itemRect);
                        g.DrawRectangle(vesselPen, itemRect);
                    }

                    // Compressor Icon circle
                    int compRadius = 14;
                    int compX = itemRect.Left + 24;
                    int compY = itemRect.Top + itemRect.Height / 2;

                    using (var compBrush = new SolidBrush(ch.Status == ChillerStatus.Running ? Color.FromArgb(34, 197, 94) : Color.FromArgb(100, 116, 139)))
                    {
                        g.FillEllipse(compBrush, compX - compRadius, compY - compRadius, compRadius * 2, compRadius * 2);
                    }

                    // Chiller Name & Status
                    TextRenderer.DrawText(g, ch.Name, fontHeader, new Point(itemRect.Left + 48, itemRect.Top + 6), theme.TextPrimary);

                    string statusStr = ch.Status == ChillerStatus.Running
                        ? $"{ch.ActualTons:N0} TR ({ch.PowerKw:N0} kW) | {ch.EfficiencyKwPerTon:F2} kW/TR"
                        : "OFFLINE / STANDBY";
                    TextRenderer.DrawText(g, statusStr, fontSmall, new Point(itemRect.Left + 48, itemRect.Top + 24),
                        ch.Status == ChillerStatus.Running ? theme.TextSecondary : Color.FromArgb(148, 163, 184));

                    // Temperatures CHWST / CHWRT
                    string tempStr = $"CHW: {ch.ChwSupplyTempC:F1}°C / {ch.ChwReturnTempC:F1}°C (ΔT {ch.ChwDeltaTempC:F1}°C)";
                    TextRenderer.DrawText(g, tempStr, fontSmall, new Point(itemRect.Left + 48, itemRect.Top + 40), Color.FromArgb(56, 189, 248));
                }
            }
        }

        private void DrawTowersSection(Graphics g, Rectangle rect, ZeroThemePalette theme)
        {
            using (var casingBrush = new SolidBrush(theme.Surface))
            using (var borderPen = new Pen(theme.Border, 1f))
            using (var fontHeader = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var fontSmall = new Font("Segoe UI", 8f))
            {
                g.FillRectangle(casingBrush, rect);
                g.DrawRectangle(borderPen, rect);

                TextRenderer.DrawText(g, "COOLING TOWERS (CW CIRCUIT)", fontHeader, new Point(rect.Left + 12, rect.Top + 8), Color.FromArgb(34, 197, 94));

                int towerCount = _engine.Towers.Count;
                if (towerCount == 0) return;

                int itemHeight = (rect.Height - 44) / towerCount;

                for (int i = 0; i < towerCount; i++)
                {
                    var tw = _engine.Towers[i];
                    Rectangle itemRect = new Rectangle(rect.Left + 10, rect.Top + 32 + i * itemHeight, rect.Width - 20, itemHeight - 8);

                    using (var towerBrush = new SolidBrush(Color.FromArgb(25, 34, 197, 94)))
                    using (var towerPen = new Pen(tw.IsFanRunning ? Color.FromArgb(34, 197, 94) : theme.Border, 1.5f))
                    {
                        g.FillRectangle(towerBrush, itemRect);
                        g.DrawRectangle(towerPen, itemRect);
                    }

                    // Rotating Fan on Cooling Tower
                    int fanRadius = 14;
                    int fanX = itemRect.Left + 24;
                    int fanY = itemRect.Top + itemRect.Height / 2;

                    float fanAngle = tw.IsFanRunning
                        ? (float)(ZeroAnimationClock.TotalElapsedTime * (tw.FanRpm / 60.0) * 360.0) % 360f
                        : 0f;

                    using (var fanPen = new Pen(tw.IsFanRunning ? Color.FromArgb(34, 197, 94) : theme.TextSecondary, 2f))
                    {
                        g.DrawEllipse(fanPen, fanX - fanRadius, fanY - fanRadius, fanRadius * 2, fanRadius * 2);
                        for (int b = 0; b < 4; b++)
                        {
                            float rad = (fanAngle + b * 90f) * (float)Math.PI / 180f;
                            g.DrawLine(fanPen, fanX, fanY, fanX + (float)Math.Cos(rad) * fanRadius, fanY + (float)Math.Sin(rad) * fanRadius);
                        }
                    }

                    // Tower Name & Status
                    TextRenderer.DrawText(g, tw.Name, fontHeader, new Point(itemRect.Left + 48, itemRect.Top + 6), theme.TextPrimary);

                    string rpmStr = tw.IsFanRunning ? $"{tw.FanRpm:N0} RPM (Running)" : "FAN STOPPED";
                    TextRenderer.DrawText(g, rpmStr, fontSmall, new Point(itemRect.Left + 48, itemRect.Top + 24),
                        tw.IsFanRunning ? theme.TextSecondary : Color.FromArgb(148, 163, 184));

                    string approachStr = $"Approach: {tw.ApproachTempC:F1}°C (Entering: {tw.WaterTempInC:F1}°C / Leaving: {tw.WaterTempOutC:F1}°C)";
                    TextRenderer.DrawText(g, approachStr, fontSmall, new Point(itemRect.Left + 48, itemRect.Top + 40), Color.FromArgb(34, 197, 94));
                }
            }
        }

        private void DrawInterconnectPipes(Graphics g, Rectangle chillers, Rectangle towers, ZeroThemePalette theme)
        {
            float phase = ZeroAnimationClock.FluidPhase;

            // Pipe 1: Condenser Water Loop between Chillers and Towers (Green)
            float pipeY = chillers.Top + chillers.Height * 0.5f;
            using (var pipePen = new Pen(Color.FromArgb(120, 34, 197, 94), 2f) { DashStyle = DashStyle.Dash })
            {
                g.DrawLine(pipePen, chillers.Right, pipeY, towers.Left, pipeY);
            }

            // Pulse dot on pipe
            float dotX = chillers.Right + (towers.Left - chillers.Right) * phase;
            g.FillEllipse(Brushes.LimeGreen, dotX - 3, pipeY - 3, 6, 6);
        }
    }
}
