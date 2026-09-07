using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.Water;
using ZeroUI.WinForms.Base;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Water
{
    /// <summary>
    /// Precision Chemical Dosing Skid visualizer for WinForms.
    /// Visualizes bulk storage day-tank with fluid level, duty/standby diaphragm metering pumps
    /// with animated reciprocating stroke pulse, discharge piping, and flow-proportional pacing HUD.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Water & Wastewater")]
    [Description("Precision chemical dosing skid with animated metering pumps and inventory tracking")]
    public class ChemicalDosingSkid : ZeroVisualControlBase
    {
        private readonly ChemicalDosingEngine _engine = new ChemicalDosingEngine();
        private string _skidTag = "CHEM-SKID-01";

        protected override bool AutoAnimate => true;

        public ChemicalDosingSkid()
        {
            Size = new Size(720, 340);
        }

        #region Public Properties

        [Category("Water")]
        [Description("Access to the underlying pure chemical dosing computation engine")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ChemicalDosingEngine Engine => _engine;

        [Category("Water")]
        [DefaultValue("CHEM-SKID-01")]
        public string SkidTag
        {
            get => _skidTag;
            set
            {
                if (_skidTag != value)
                {
                    _skidTag = value ?? "SKID-01";
                    Invalidate();
                }
            }
        }

        [Category("Water")]
        [DefaultValue(ChemicalType.Disinfectant)]
        public ChemicalType Chemical
        {
            get => _engine.Chemical;
            set
            {
                _engine.Chemical = value;
                Invalidate();
            }
        }

        [Category("Water")]
        [DefaultValue(850.0)]
        public double WaterFlowM3H
        {
            get => _engine.WaterFlowM3H;
            set
            {
                _engine.WaterFlowM3H = Math.Max(0.0, value);
                _engine.AdjustPumpsToTargetRate();
                Invalidate();
            }
        }

        [Category("Water")]
        [DefaultValue(2.5)]
        public double TargetDoseMgL
        {
            get => _engine.TargetDoseMgL;
            set
            {
                _engine.TargetDoseMgL = Math.Max(0.0, value);
                _engine.AdjustPumpsToTargetRate();
                Invalidate();
            }
        }

        [Category("Water")]
        [DefaultValue(1750.0)]
        public double TankCurrentLevelL
        {
            get => _engine.Tank.CurrentLevelL;
            set
            {
                _engine.Tank.CurrentLevelL = Math.Max(0.0, Math.Min(_engine.Tank.CapacityL, value));
                Invalidate();
            }
        }

        #endregion

        protected override void OnAnimationTick(double delta, long frame)
        {
            _engine.AdvanceConsumption(delta);
        }

        protected override void OnDrawVisual(Graphics g, Rectangle bounds, ZeroThemePalette palette)
        {
            // Header
            DrawHeader(g, bounds, palette);

            int headerH = 48;
            int mainTop = bounds.Y + headerH;
            int mainHeight = bounds.Height - headerH - 12;

            int tankW = 140;
            int hudW = 230;
            int skidW = bounds.Width - tankW - hudW - 40;

            if (skidW < 140 || mainHeight < 120) return;

            Rectangle tankRect = new Rectangle(bounds.X + 16, mainTop, tankW, mainHeight);
            Rectangle skidRect = new Rectangle(tankRect.Right + 12, mainTop, skidW, mainHeight);
            Rectangle hudRect = new Rectangle(skidRect.Right + 12, mainTop, hudW, mainHeight);

            // 1. Chemical Storage Tank
            DrawTank(g, tankRect, palette);

            // 2. Pumps & Piping Skid Schematic
            DrawPumpsSkid(g, skidRect, palette);

            // 3. Telemetry & Pacing HUD
            DrawTelemetryHud(g, hudRect, palette);
        }

        private void DrawHeader(Graphics g, Rectangle bounds, ZeroThemePalette palette)
        {
            using (var fontTitle = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (var fontSub = new Font("Segoe UI", 8.5f))
            {
                string chemName = _engine.Chemical switch
                {
                    ChemicalType.Disinfectant => "Sodium Hypochlorite (NaOCl)",
                    ChemicalType.Coagulant => "Coagulant (Alum / PAC)",
                    ChemicalType.Polymer => "Polymer Flocculant",
                    ChemicalType.PhAdjustment => "pH Adjustment (NaOH)",
                    ChemicalType.Fluoride => "Fluoride (H2SiF6)",
                    _ => "Treatment Chemical"
                };

                TextRenderer.DrawText(g, $"{_skidTag} — {chemName} Dosing Skid", fontTitle, new Point(bounds.X + 16, bounds.Y + 12), palette.TextPrimary);

                // Target Dosing Rate Badge
                double reqLh = _engine.CalculateRequiredDosingRateLh();
                string doseStr = $"Target: {_engine.TargetDoseMgL:F1} mg/L ({reqLh:F1} L/h)";
                Rectangle badgeRect = new Rectangle(bounds.Right - 210, bounds.Y + 10, 194, 24);

                using (var brush = new SolidBrush(Color.FromArgb(30, 56, 189, 248)))
                using (var pen = new Pen(Color.FromArgb(56, 189, 248), 1.2f))
                {
                    g.FillRectangle(brush, badgeRect);
                    g.DrawRectangle(pen, badgeRect);
                }

                TextRenderer.DrawText(g, doseStr, fontSub, badgeRect, Color.FromArgb(56, 189, 248),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawTank(Graphics g, Rectangle rect, ZeroThemePalette palette)
        {
            using (var fontTitle = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var fontSmall = new Font("Segoe UI", 7.5f))
            {
                PaintHelper.DrawCardBox(g, rect, "STORAGE TANK", fontTitle, palette);

                int pad = 14;
                int tankInnerW = rect.Width - pad * 2;
                int tankInnerH = rect.Height - 64;
                Rectangle cylinderRect = new Rectangle(rect.X + pad, rect.Y + 34, tankInnerW, tankInnerH);

                // Cylinder background
                using (var bgBrush = new SolidBrush(Color.FromArgb(35, palette.Border)))
                using (var cylPen = new Pen(palette.Border, 1.5f))
                {
                    g.FillRectangle(bgBrush, cylinderRect);
                    g.DrawRectangle(cylPen, cylinderRect);
                }

                // Chemical liquid fill
                double lvlPct = Math.Max(0.0, Math.Min(100.0, _engine.Tank.LevelPct));
                int fillHeight = (int)(cylinderRect.Height * (lvlPct / 100.0));
                Rectangle fillRect = new Rectangle(cylinderRect.X + 2, cylinderRect.Bottom - fillHeight, cylinderRect.Width - 4, fillHeight);

                Color chemColor = _engine.Chemical switch
                {
                    ChemicalType.Disinfectant => Color.FromArgb(234, 179, 8),  // Amber / yellow
                    ChemicalType.Coagulant => Color.FromArgb(249, 115, 22),   // Orange / brown
                    ChemicalType.Polymer => Color.FromArgb(168, 85, 247),     // Purple
                    ChemicalType.PhAdjustment => Color.FromArgb(59, 130, 246),// Blue
                    _ => Color.FromArgb(34, 197, 94)
                };

                if (fillHeight > 0)
                {
                    using (var fillBrush = new LinearGradientBrush(
                        new Point(fillRect.X, fillRect.Y),
                        new Point(fillRect.Right, fillRect.Y),
                        Color.FromArgb(180, chemColor),
                        Color.FromArgb(240, chemColor)))
                    {
                        g.FillRectangle(fillBrush, fillRect);
                    }
                }

                // Volume label
                string lvlText = $"{_engine.Tank.CurrentLevelL:N0} L ({lvlPct:F0}%)";
                Color textCol = _engine.Tank.IsLowLevel ? Color.FromArgb(239, 68, 68) : palette.TextPrimary;
                TextRenderer.DrawText(g, lvlText, fontSmall,
                    new Rectangle(rect.X, rect.Bottom - 26, rect.Width, 20),
                    textCol, TextFormatFlags.HorizontalCenter);
            }
        }

        private void DrawPumpsSkid(Graphics g, Rectangle rect, ZeroThemePalette palette)
        {
            using (var fontTitle = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var fontLabel = new Font("Segoe UI", 7.5f))
            {
                PaintHelper.DrawCardBox(g, rect, "METERING PUMPS & PIPING SKID", fontTitle, palette);

                int midY = rect.Y + rect.Height / 2;
                int pumpW = 74;
                int pumpH = 64;

                // Suction pipe from left (tank)
                using (var pipePen = new Pen(Color.FromArgb(14, 165, 233), 3f))
                {
                    g.DrawLine(pipePen, rect.X, midY, rect.X + 30, midY);
                    // Split to Pump A (upper) and Pump B (lower)
                    g.DrawLine(pipePen, rect.X + 30, midY - 36, rect.X + 30, midY + 36);
                    g.DrawLine(pipePen, rect.X + 30, midY - 36, rect.X + 60, midY - 36);
                    g.DrawLine(pipePen, rect.X + 30, midY + 36, rect.X + 60, midY + 36);
                }

                // Pump A (Upper)
                Rectangle pumpARect = new Rectangle(rect.X + 60, midY - 36 - pumpH / 2, pumpW, pumpH);
                DrawPumpUnit(g, pumpARect, _engine.PumpA, palette);

                // Pump B (Lower)
                Rectangle pumpBRect = new Rectangle(rect.X + 60, midY + 36 - pumpH / 2, pumpW, pumpH);
                DrawPumpUnit(g, pumpBRect, _engine.PumpB, palette);

                // Discharge manifold pipe merging to the right
                using (var pipePen = new Pen(Color.FromArgb(56, 189, 248), 3f))
                {
                    int rightX = rect.X + 60 + pumpW;
                    g.DrawLine(pipePen, rightX, midY - 36, rightX + 24, midY - 36);
                    g.DrawLine(pipePen, rightX, midY + 36, rightX + 24, midY + 36);
                    g.DrawLine(pipePen, rightX + 24, midY - 36, rightX + 24, midY + 36);
                    g.DrawLine(pipePen, rightX + 24, midY, rect.Right - 16, midY);

                    // Backpressure valve symbol
                    int bprX = rightX + 46;
                    g.DrawEllipse(pipePen, bprX - 6, midY - 6, 12, 12);
                }

                // Water main injection quill
                Rectangle quillRect = new Rectangle(rect.Right - 16, midY - 14, 8, 28);
                using (var quillBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    g.FillRectangle(quillBrush, quillRect);
                }
            }
        }

        private void DrawPumpUnit(Graphics g, Rectangle rect, DosingPump pump, ZeroThemePalette palette)
        {
            using (var fontPump = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (var fontSpm = new Font("Segoe UI", 7f))
            {
                Color pumpColor = pump.Mode switch
                {
                    DosingPumpMode.Fault => Color.FromArgb(239, 68, 68),
                    DosingPumpMode.Standby => Color.FromArgb(148, 163, 184),
                    _ => pump.IsRunning ? Color.FromArgb(34, 197, 94) : Color.FromArgb(245, 158, 11)
                };

                // Pump body block
                using (var pumpBrush = new SolidBrush(Color.FromArgb(40, pumpColor)))
                using (var pumpPen = new Pen(pumpColor, 1.5f))
                {
                    g.FillRectangle(pumpBrush, rect);
                    g.DrawRectangle(pumpPen, rect);
                }

                // Diaphragm head circle on the left side
                int headDia = 20;
                Rectangle headRect = new Rectangle(rect.X + 4, rect.Y + (rect.Height - headDia) / 2, headDia, headDia);
                using (var headBrush = new SolidBrush(Color.FromArgb(180, pumpColor)))
                {
                    g.FillEllipse(headBrush, headRect);
                }

                TextRenderer.DrawText(g, pump.PumpTag, fontPump, new Point(rect.X + 26, rect.Y + 8), palette.TextPrimary);
                TextRenderer.DrawText(g, $"{pump.Mode.ToString().ToUpperInvariant()}", fontSpm, new Point(rect.X + 26, rect.Y + 24), pumpColor);

                string spmStr = pump.IsRunning ? $"{pump.StrokeRateSpm:F0} SPM" : "OFF";
                TextRenderer.DrawText(g, spmStr, fontSpm, new Point(rect.X + 26, rect.Y + 40), palette.TextSecondary);
            }
        }

        private void DrawTelemetryHud(Graphics g, Rectangle rect, ZeroThemePalette palette)
        {
            using (var fontTitle = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var fontLabel = new Font("Segoe UI", 8f))
            using (var fontValue = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                PaintHelper.DrawCardBox(g, rect, "DOSING TELEMETRY", fontTitle, palette);

                int pad = 12;
                int innerW = rect.Width - pad * 2;
                int rowY = rect.Y + 34;
                int rowH = 24;

                // Main Water Flow
                PaintHelper.DrawDataRow(g, new Rectangle(rect.X + pad, rowY, innerW, rowH),
                    "Main Flow", $"{_engine.WaterFlowM3H:N0} m³/h",
                    palette.TextSecondary, palette.TextPrimary, fontLabel, fontValue);
                rowY += rowH;

                // Target Dose Setpoint
                PaintHelper.DrawDataRow(g, new Rectangle(rect.X + pad, rowY, innerW, rowH),
                    "Target Dose", $"{_engine.TargetDoseMgL:F2} mg/L",
                    palette.TextSecondary, Color.FromArgb(56, 189, 248), fontLabel, fontValue);
                rowY += rowH;

                // Actual Delivery Rate
                double delivered = _engine.TotalDeliveredDosingRateLh;
                double req = _engine.CalculateRequiredDosingRateLh();
                Color rateCol = Math.Abs(delivered - req) < 1.0 ? Color.FromArgb(34, 197, 94) : Color.FromArgb(245, 158, 11);

                PaintHelper.DrawDataRow(g, new Rectangle(rect.X + pad, rowY, innerW, rowH),
                    "Actual Rate", $"{delivered:F1} / {req:F1} L/h",
                    palette.TextSecondary, rateCol, fontLabel, fontValue);
                rowY += rowH;

                // Active Discharge Pressure
                double press = _engine.PumpA.IsRunning ? _engine.PumpA.DischargePressureBar : _engine.PumpB.DischargePressureBar;
                PaintHelper.DrawDataRow(g, new Rectangle(rect.X + pad, rowY, innerW, rowH),
                    "Discharge Press", $"{press:F1} bar",
                    palette.TextSecondary, palette.TextPrimary, fontLabel, fontValue);
                rowY += rowH;

                // Solution Concentration
                PaintHelper.DrawDataRow(g, new Rectangle(rect.X + pad, rowY, innerW, rowH),
                    "Concentration", $"{_engine.Tank.SolutionConcentrationPct:F1}% (SG {_engine.Tank.SpecificGravity:F2})",
                    palette.TextSecondary, palette.TextPrimary, fontLabel, fontValue);
                rowY += rowH;

                // Estimated Storage Autonomy
                double hoursLeft = _engine.EstimatedRunHoursRemaining;
                string autoStr = double.IsPositiveInfinity(hoursLeft) ? "∞" : $"{hoursLeft:F1} hrs";
                Color autoCol = hoursLeft < 24.0 ? Color.FromArgb(239, 68, 68) : Color.FromArgb(34, 197, 94);

                PaintHelper.DrawDataRow(g, new Rectangle(rect.X + pad, rowY, innerW, rowH),
                    "Tank Autonomy", autoStr,
                    palette.TextSecondary, autoCol, fontLabel, fontValue);
            }
        }
    }
}
