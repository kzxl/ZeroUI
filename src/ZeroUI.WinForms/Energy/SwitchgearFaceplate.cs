using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ZeroUI.Core.Energy;
using ZeroUI.WinForms.Base;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Energy
{
    /// <summary>
    /// Medium/High-Voltage Vacuum Circuit Breaker (VCB) Switchgear front faceplate visualizer.
    /// Displays operating spring charge mechanical flag, vacuum bottle contact wear gauge,
    /// trip coil continuous supervision LEDs, LOTO padlock interlock, and protection relay telemetry.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Energy & Smart Grid")]
    [Description("MV/HV Switchgear faceplate with spring charge indicator, contact wear gauge, trip coil supervision, and LOTO interlock")]
    public class SwitchgearFaceplate : ZeroVisualControlBase
    {
        private readonly SwitchgearEngine _engine = new SwitchgearEngine();
        private string _breakerTag = "VCB-BAY-04 (110kV Incomer)";

        protected override bool AutoAnimate => true;

        public SwitchgearFaceplate()
        {
            Size = new Size(680, 360);
        }

        #region Public Properties

        [Category("Switchgear")]
        [Description("Access to the underlying Switchgear computation engine")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SwitchgearEngine Engine => _engine;

        [Category("Switchgear")]
        [DefaultValue("VCB-BAY-04 (110kV Incomer)")]
        public string BreakerTag
        {
            get => _breakerTag;
            set
            {
                _breakerTag = value;
                Invalidate();
            }
        }

        #endregion

        protected override void OnDrawVisual(Graphics g, Rectangle bounds, ZeroThemePalette theme)
        {
            // Header banner
            DrawHeader(g, theme);

            int contentTop = 56;
            int contentWidth = bounds.Width - 40;
            int contentHeight = bounds.Height - contentTop - 16;
            if (contentWidth < 200 || contentHeight < 100)
                return;

            int col1W = (int)(contentWidth * 0.32);
            int col2W = (int)(contentWidth * 0.36);
            int col3W = contentWidth - col1W - col2W - 24;

            int col1X = 20;
            int col2X = col1X + col1W + 12;
            int col3X = col2X + col2W + 12;

            Rectangle col1Rect = new Rectangle(col1X, contentTop, col1W, contentHeight);
            Rectangle col2Rect = new Rectangle(col2X, contentTop, col2W, contentHeight);
            Rectangle col3Rect = new Rectangle(col3X, contentTop, col3W, contentHeight);

            DrawMechanicalPanel(g, col1Rect, theme);
            DrawVacuumHealthPanel(g, col2Rect, theme);
            DrawRelayInterlockPanel(g, col3Rect, theme);
        }

        private void DrawHeader(Graphics g, ZeroThemePalette theme)
        {
            using (var fontTitle = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (var fontBold = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, _breakerTag, fontTitle, new Point(20, 14), theme.TextPrimary);

                bool permit = SwitchgearEngine.ValidateClosePermit(_engine.Mechanism, out string reason);
                Color permitColor = permit ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68);

                Rectangle pillRect = new Rectangle(Width - 280, 12, 260, 26);
                DrawStatusBadge(g, pillRect, reason, fontBold, permitColor, permitColor, 4);
            }
        }

        private void DrawMechanicalPanel(Graphics g, Rectangle rect, ZeroThemePalette theme)
        {
            using (var fontHead = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (var fontLabel = new Font("Segoe UI", 8.5f))
            using (var fontValue = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var fontFlag = new Font("Segoe UI", 11f, FontStyle.Bold))
            {
                DrawCardBox(g, rect, "MECHANISM & CHARGE", fontHead, theme);

                var mech = _engine.Mechanism;

                // Spring Charge Mechanical Flag Box
                Rectangle flagRect = new Rectangle(rect.X + 16, rect.Y + 38, rect.Width - 32, 50);
                Color flagBg = mech.IsSpringCharged ? Color.FromArgb(245, 158, 11) : Color.FromArgb(75, 85, 99);
                string flagText = mech.IsSpringCharged ? "SPR. CHARGED" : "DISCHARGED";

                using (var flagBrush = new SolidBrush(flagBg))
                using (var flagPen = new Pen(Color.FromArgb(200, flagBg), 2f))
                {
                    g.FillRectangle(flagBrush, flagRect);
                    g.DrawRectangle(flagPen, flagRect);
                }
                TextRenderer.DrawText(g, flagText, fontFlag, flagRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // Data rows
                int rowY = flagRect.Bottom + 16;
                string motorStatus = mech.IsMotorCharging ? "Motor Running..." : "Idle";
                DrawDataRow(g, new Rectangle(rect.X + 16, rowY, rect.Width - 32, 20), "Charge Motor:", motorStatus, theme.TextSecondary, theme.TextPrimary, fontLabel, fontValue);

                rowY += 26;
                DrawDataRow(g, new Rectangle(rect.X + 16, rowY, rect.Width - 32, 20), "Operations Count:", mech.TotalOperationsCount.ToString("N0"), theme.TextSecondary, theme.TextPrimary, fontLabel, fontValue);

                rowY += 26;
                DrawDataRow(g, new Rectangle(rect.X + 16, rowY, rect.Width - 32, 20), "Mech Temp:", $"{mech.MechanismTemperatureC:F1} °C", theme.TextSecondary, theme.TextPrimary, fontLabel, fontValue);

                rowY += 26;
                DrawDataRow(g, new Rectangle(rect.X + 16, rowY, rect.Width - 32, 20), "Rated Breaking:", $"{mech.RatedShortCircuitKa:F1} kA", theme.TextSecondary, theme.TextPrimary, fontLabel, fontValue);
            }
        }

        private void DrawVacuumHealthPanel(Graphics g, Rectangle rect, ZeroThemePalette theme)
        {
            using (var fontHead = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (var fontLabel = new Font("Segoe UI", 8.5f))
            using (var fontValue = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var fontSmall = new Font("Segoe UI", 7.5f))
            {
                DrawCardBox(g, rect, "VACUUM BOTTLE HEALTH", fontHead, theme);

                var mech = _engine.Mechanism;

                // Contact Wear Bar
                int barY = rect.Y + 40;
                TextRenderer.DrawText(g, "Contact Erosion Wear:", fontLabel, new Point(rect.X + 16, barY), theme.TextSecondary);
                TextRenderer.DrawText(g, $"{mech.VacuumBottleContactWearPct:F1}%", fontValue, new Point(rect.Right - 60, barY),
                    mech.IsContactWearCritical ? Color.FromArgb(239, 68, 68) : theme.TextPrimary);

                Rectangle barRect = new Rectangle(rect.X + 16, barY + 22, rect.Width - 32, 14);
                using (var bgBrush = new SolidBrush(Color.FromArgb(40, 128, 128, 128)))
                {
                    g.FillRectangle(bgBrush, barRect);
                }

                Color barColor = mech.VacuumBottleContactWearPct < 60 ? Color.FromArgb(34, 197, 94) :
                                 mech.VacuumBottleContactWearPct < 85 ? Color.FromArgb(245, 158, 11) :
                                 Color.FromArgb(239, 68, 68);

                int fillW = (int)((barRect.Width * Math.Min(100.0, mech.VacuumBottleContactWearPct)) / 100.0);
                if (fillW > 0)
                {
                    using (var fillBrush = new SolidBrush(barColor))
                    {
                        g.FillRectangle(fillBrush, barRect.X, barRect.Y, fillW, barRect.Height);
                    }
                }

                // Trip Coils Supervision
                int coilY = barRect.Bottom + 20;
                TextRenderer.DrawText(g, "Trip Coil 1 (TC1):", fontLabel, new Point(rect.X + 16, coilY), theme.TextSecondary);
                DrawTripCoil(g, rect.Right - 70, coilY + 2, mech.TripCoil1);

                coilY += 28;
                TextRenderer.DrawText(g, "Trip Coil 2 (TC2):", fontLabel, new Point(rect.X + 16, coilY), theme.TextSecondary);
                DrawTripCoil(g, rect.Right - 70, coilY + 2, mech.TripCoil2);

                coilY += 32;
                Rectangle chamberRect = new Rectangle(rect.X + 16, coilY, rect.Width - 32, 40);
                using (var chBrush = new SolidBrush(Color.FromArgb(20, 59, 130, 246)))
                using (var chPen = new Pen(Color.FromArgb(59, 130, 246), 1f))
                {
                    g.FillRectangle(chBrush, chamberRect);
                    g.DrawRectangle(chPen, chamberRect);
                }
                TextRenderer.DrawText(g, "Vacuum Integrity: HIGH-INTEGRITY", fontSmall, chamberRect, Color.FromArgb(59, 130, 246), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawRelayInterlockPanel(Graphics g, Rectangle rect, ZeroThemePalette theme)
        {
            using (var fontHead = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (var fontLabel = new Font("Segoe UI", 8.5f))
            using (var fontValue = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var fontBold = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            {
                DrawCardBox(g, rect, "RELAY & LOTO SAFETY", fontHead, theme);

                var relay = _engine.Relay;
                var mech = _engine.Mechanism;

                int rowY = rect.Y + 38;
                DrawDataRow(g, new Rectangle(rect.X + 16, rowY, rect.Width - 32, 20), "Relay Type:", relay.RelayTag, theme.TextSecondary, theme.TextPrimary, fontLabel, fontValue);

                rowY += 26;
                DrawDataRow(g, new Rectangle(rect.X + 16, rowY, rect.Width - 32, 20), "ANSI Protection:", relay.ActiveAnsiCode, theme.TextSecondary, Color.FromArgb(59, 130, 246), fontLabel, fontValue);

                rowY += 26;
                Color pickColor = relay.IsPickupActive ? Color.FromArgb(239, 68, 68) : Color.FromArgb(34, 197, 94);
                DrawDataRow(g, new Rectangle(rect.X + 16, rowY, rect.Width - 32, 20), "Pickup / Alarm:", relay.IsPickupActive ? "PICKUP ACTIVE" : "Normal", theme.TextSecondary, pickColor, fontLabel, fontValue);

                // LOTO Safety Lockout status box
                rowY += 34;
                Rectangle lotoRect = new Rectangle(rect.X + 16, rowY, rect.Width - 32, 54);
                Color lotoColor = mech.LotoState == LotoLockState.LockedOut ? Color.FromArgb(239, 68, 68) :
                                  mech.LotoState == LotoLockState.PermitActive ? Color.FromArgb(245, 158, 11) :
                                  Color.FromArgb(34, 197, 94);

                string lotoText = mech.LotoState == LotoLockState.LockedOut ? "[LOCKED OUT] Padlock Applied" :
                                  mech.LotoState == LotoLockState.PermitActive ? "[PERMIT ACTIVE] Tagged" :
                                  "[UNLOCKED] No Interlock";
                DrawStatusBadge(g, lotoRect, lotoText, fontBold, lotoColor, lotoColor, 4);
            }
        }

        private void DrawTripCoil(Graphics g, int x, int y, TripCoilState state)
        {
            Color ledColor = state == TripCoilState.Healthy ? Color.FromArgb(34, 197, 94) :
                             state == TripCoilState.OpenCircuit ? Color.FromArgb(239, 68, 68) :
                             Color.FromArgb(245, 158, 11);
            string text = state == TripCoilState.Healthy ? "OK" : "FAULT";
            using (var font = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            {
                DrawLedIndicator(g, x, y, ledColor, text, font);
            }
        }
    }
}
