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
    /// Medium/High-Voltage Vacuum Circuit Breaker (VCB) Switchgear front faceplate visualizer.
    /// Displays operating spring charge mechanical flag, vacuum bottle contact wear gauge,
    /// trip coil continuous supervision LEDs, LOTO padlock interlock, and protection relay telemetry.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Energy & Smart Grid")]
    [Description("MV/HV Switchgear faceplate with spring charge indicator, contact wear gauge, trip coil supervision, and LOTO interlock")]
    public class SwitchgearFaceplate : Control
    {
        private readonly SwitchgearEngine _engine = new SwitchgearEngine();
        private IDisposable? _animSub;
        private string _breakerTag = "VCB-BAY-04 (110kV Incomer)";

        public SwitchgearFaceplate()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            Size = new Size(680, 360);

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

            // Header banner
            DrawHeader(g, theme);

            int contentTop = 56;
            int contentWidth = Width - 40;
            int contentHeight = Height - contentTop - 16;
            if (contentWidth < 200 || contentHeight < 100)
                return;

            // Divide into 3 columns:
            // Col 1: Mechanical Status & Spring Charge (width 32%)
            // Col 2: Vacuum Bottle Wear & Trip Coils (width 36%)
            // Col 3: Protection Relay & LOTO Interlock (remaining width)
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
            using (var fontSmall = new Font("Segoe UI", 8.5f))
            using (var fontBold = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, _breakerTag, fontTitle, new Point(20, 14), theme.TextPrimary);

                bool permit = SwitchgearEngine.ValidateClosePermit(_engine.Mechanism, out string reason);
                Color permitColor = permit ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68);

                Rectangle pillRect = new Rectangle(Width - 280, 12, 260, 26);
                using (var pillBrush = new SolidBrush(Color.FromArgb(25, permitColor)))
                using (var pillPen = new Pen(permitColor, 1f))
                {
                    g.FillRectangle(pillBrush, pillRect);
                    g.DrawRectangle(pillPen, pillRect);
                }
                TextRenderer.DrawText(g, reason, fontBold, pillRect, permitColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawMechanicalPanel(Graphics g, Rectangle rect, ZeroThemePalette theme)
        {
            DrawCardBox(g, rect, "MECHANISM & CHARGE", theme);

            var mech = _engine.Mechanism;
            using (var fontLabel = new Font("Segoe UI", 8.5f))
            using (var fontValue = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var fontFlag = new Font("Segoe UI", 11f, FontStyle.Bold))
            {
                // Spring Charge Mechanical Flag Box
                Rectangle flagRect = new Rectangle(rect.X + 16, rect.Y + 38, rect.Width - 32, 50);
                Color flagBg = mech.IsSpringCharged ? Color.FromArgb(245, 158, 11) : Color.FromArgb(75, 85, 99);
                Color flagFg = Color.White;
                string flagText = mech.IsSpringCharged ? "SPR. CHARGED" : "DISCHARGED";

                using (var flagBrush = new SolidBrush(flagBg))
                using (var flagPen = new Pen(Color.FromArgb(200, flagBg), 2f))
                {
                    g.FillRectangle(flagBrush, flagRect);
                    g.DrawRectangle(flagPen, flagRect);
                }
                TextRenderer.DrawText(g, flagText, fontFlag, flagRect, flagFg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // Motor charging status
                int rowY = flagRect.Bottom + 16;
                string motorStatus = mech.IsMotorCharging ? "Motor Running..." : "Idle";
                TextRenderer.DrawText(g, "Charge Motor:", fontLabel, new Point(rect.X + 16, rowY), theme.TextSecondary);
                TextRenderer.DrawText(g, motorStatus, fontValue, new Point(rect.Right - 100, rowY), theme.TextPrimary);

                rowY += 26;
                TextRenderer.DrawText(g, "Operations Count:", fontLabel, new Point(rect.X + 16, rowY), theme.TextSecondary);
                TextRenderer.DrawText(g, mech.TotalOperationsCount.ToString("N0"), fontValue, new Point(rect.Right - 80, rowY), theme.TextPrimary);

                rowY += 26;
                TextRenderer.DrawText(g, "Mech Temp:", fontLabel, new Point(rect.X + 16, rowY), theme.TextSecondary);
                TextRenderer.DrawText(g, $"{mech.MechanismTemperatureC:F1} °C", fontValue, new Point(rect.Right - 80, rowY), theme.TextPrimary);

                rowY += 26;
                TextRenderer.DrawText(g, "Rated Breaking:", fontLabel, new Point(rect.X + 16, rowY), theme.TextSecondary);
                TextRenderer.DrawText(g, $"{mech.RatedShortCircuitKa:F1} kA", fontValue, new Point(rect.Right - 80, rowY), theme.TextPrimary);
            }
        }

        private void DrawVacuumHealthPanel(Graphics g, Rectangle rect, ZeroThemePalette theme)
        {
            DrawCardBox(g, rect, "VACUUM BOTTLE HEALTH", theme);

            var mech = _engine.Mechanism;
            using (var fontLabel = new Font("Segoe UI", 8.5f))
            using (var fontValue = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var fontSmall = new Font("Segoe UI", 7.5f))
            {
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

                // Trip Coil 1 Supervision
                int coilY = barRect.Bottom + 20;
                TextRenderer.DrawText(g, "Trip Coil 1 (TC1):", fontLabel, new Point(rect.X + 16, coilY), theme.TextSecondary);
                DrawLedIndicator(g, rect.Right - 70, coilY + 2, mech.TripCoil1);

                // Trip Coil 2 Supervision
                coilY += 28;
                TextRenderer.DrawText(g, "Trip Coil 2 (TC2):", fontLabel, new Point(rect.X + 16, coilY), theme.TextSecondary);
                DrawLedIndicator(g, rect.Right - 70, coilY + 2, mech.TripCoil2);

                // Arc Interruption Chamber Status
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
            DrawCardBox(g, rect, "RELAY & LOTO SAFETY", theme);

            var relay = _engine.Relay;
            var mech = _engine.Mechanism;
            using (var fontLabel = new Font("Segoe UI", 8.5f))
            using (var fontValue = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var fontBold = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            {
                int rowY = rect.Y + 38;
                TextRenderer.DrawText(g, "Relay Type:", fontLabel, new Point(rect.X + 16, rowY), theme.TextSecondary);
                TextRenderer.DrawText(g, relay.RelayTag, fontValue, new Point(rect.Right - 90, rowY), theme.TextPrimary);

                rowY += 26;
                TextRenderer.DrawText(g, "ANSI Protection:", fontLabel, new Point(rect.X + 16, rowY), theme.TextSecondary);
                TextRenderer.DrawText(g, relay.ActiveAnsiCode, fontValue, new Point(rect.Right - 90, rowY), Color.FromArgb(59, 130, 246));

                rowY += 26;
                TextRenderer.DrawText(g, "Pickup / Alarm:", fontLabel, new Point(rect.X + 16, rowY), theme.TextSecondary);
                TextRenderer.DrawText(g, relay.IsPickupActive ? "PICKUP ACTIVE" : "Normal", fontValue,
                    new Point(rect.Right - 110, rowY), relay.IsPickupActive ? Color.FromArgb(239, 68, 68) : Color.FromArgb(34, 197, 94));

                // LOTO Safety Lockout status box
                rowY += 34;
                Rectangle lotoRect = new Rectangle(rect.X + 16, rowY, rect.Width - 32, 54);
                Color lotoColor = mech.LotoState == LotoLockState.LockedOut ? Color.FromArgb(239, 68, 68) :
                                  mech.LotoState == LotoLockState.PermitActive ? Color.FromArgb(245, 158, 11) :
                                  Color.FromArgb(34, 197, 94);

                using (var lotoBrush = new SolidBrush(Color.FromArgb(20, lotoColor)))
                using (var lotoPen = new Pen(lotoColor, 1.5f))
                {
                    g.FillRectangle(lotoBrush, lotoRect);
                    g.DrawRectangle(lotoPen, lotoRect);
                }

                string lotoText = mech.LotoState == LotoLockState.LockedOut ? "[LOCKED OUT] Padlock Applied" :
                                  mech.LotoState == LotoLockState.PermitActive ? "[PERMIT ACTIVE] Tagged" :
                                  "[UNLOCKED] No Interlock";
                TextRenderer.DrawText(g, lotoText, fontBold, lotoRect, lotoColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawCardBox(Graphics g, Rectangle rect, string title, ZeroThemePalette theme)
        {
            using (var boxBrush = new SolidBrush(Color.FromArgb(12, theme.TextPrimary)))
            using (var borderPen = new Pen(theme.Border, 1f))
            using (var fontTitle = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            {
                g.FillRectangle(boxBrush, rect);
                g.DrawRectangle(borderPen, rect);
                TextRenderer.DrawText(g, title, fontTitle, new Point(rect.X + 14, rect.Y + 12), theme.TextSecondary);
            }
        }

        private void DrawLedIndicator(Graphics g, int x, int y, TripCoilState state)
        {
            Color ledColor = state == TripCoilState.Healthy ? Color.FromArgb(34, 197, 94) :
                             state == TripCoilState.OpenCircuit ? Color.FromArgb(239, 68, 68) :
                             Color.FromArgb(245, 158, 11);

            using (var brush = new SolidBrush(ledColor))
            using (var font = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            {
                g.FillEllipse(brush, x, y, 10, 10);
                string text = state == TripCoilState.Healthy ? "OK" : "FAULT";
                TextRenderer.DrawText(g, text, font, new Point(x + 14, y - 2), ledColor);
            }
        }
    }
}
