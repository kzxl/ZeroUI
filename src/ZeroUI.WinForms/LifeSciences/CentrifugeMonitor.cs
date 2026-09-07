using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.LifeSciences;
using ZeroUI.Core.Rendering;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.LifeSciences
{
    /// <summary>
    /// Laboratory refrigerated centrifuge operational telemetry visualizer.
    /// Monitors rotor spinning rotation, relative centrifugal force (RCF/g-force),
    /// dynamic bucket weight balance, chamber temperature, and vibration accelerometers.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Life Sciences")]
    [Description("Refrigerated centrifuge monitor with animated rotor, RCF g-force, bucket balance, and vibration sensors")]
    public class CentrifugeMonitor : Control
    {
        private readonly CentrifugeEngine _engine = new CentrifugeEngine();
        private IDisposable? _animSub;

        public CentrifugeMonitor()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            Size = new Size(780, 420);

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

        [Category("Centrifuge")]
        [Description("Access to the underlying centrifuge computation engine")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CentrifugeEngine Engine => _engine;

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

            var tele = _engine.Telemetry;

            // Header Banner
            DrawHeader(g, tele, theme);

            int contentTop = 56;
            int contentWidth = Width - 40;
            int contentHeight = Height - contentTop - 16;
            if (contentWidth < 200 || contentHeight < 100) return;

            // Divide into 2 columns:
            // Left: Animated Rotor & Bucket Chamber (width 48%)
            // Right: Telemetry KPIs, Vibration, and Lid Safety (width 52%)
            int rotorW = (int)(contentWidth * 0.48);
            int kpiW = contentWidth - rotorW - 16;

            Rectangle rotorRect = new Rectangle(20, contentTop, rotorW, contentHeight);
            Rectangle kpiRect = new Rectangle(20 + rotorW + 16, contentTop, kpiW, contentHeight);

            DrawRotorChamber(g, rotorRect, tele, theme);
            DrawTelemetryPanel(g, kpiRect, tele, theme);
        }

        private void DrawHeader(Graphics g, CentrifugeTelemetry tele, ZeroThemePalette theme)
        {
            using (var fontTitle = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (var fontSmall = new Font("Segoe UI", 8.5f))
            using (var fontBold = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, tele.ModelTag, fontTitle, new Point(20, 14), theme.TextPrimary);

                string subStr = $"Rotor: {tele.Rotor} (r_max = {tele.RotorRadiusMm:F0} mm) | Program: 14,500 RPM @ 4°C";
                TextRenderer.DrawText(g, subStr, fontSmall, new Point(20, 36), theme.TextSecondary);

                // Run Status Pill
                Color statusCol = tele.IsRunning ? Color.FromArgb(34, 197, 94) : Color.FromArgb(156, 163, 175);
                string statusText = tele.IsRunning ? "SPINNING (RUN)" : "STOPPED";

                Rectangle pillRect = new Rectangle(Width - 160, 14, 140, 26);
                using (var pillBrush = new SolidBrush(Color.FromArgb(25, statusCol)))
                using (var pillPen = new Pen(statusCol, 1f))
                {
                    g.FillRectangle(pillBrush, pillRect);
                    g.DrawRectangle(pillPen, pillRect);
                }
                TextRenderer.DrawText(g, statusText, fontBold, pillRect, statusCol, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawRotorChamber(Graphics g, Rectangle rect, CentrifugeTelemetry tele, ZeroThemePalette theme)
        {
            // Box card
            using (var boxBrush = new SolidBrush(Color.FromArgb(12, theme.TextPrimary)))
            using (var borderPen = new Pen(theme.Border, 1f))
            using (var fontTitle = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            {
                g.FillRectangle(boxBrush, rect);
                g.DrawRectangle(borderPen, rect);
                TextRenderer.DrawText(g, "ROTOR CHAMBER & BUCKET BALANCE", fontTitle, new Point(rect.X + 14, rect.Y + 10), theme.TextSecondary);
            }

            float cx = rect.X + rect.Width / 2f;
            float cy = rect.Y + (rect.Height + 20) / 2f;
            float chamberRadius = Math.Min(rect.Width, rect.Height - 40) * 0.38f;

            // Outer vacuum chamber circle
            using (var chBrush = new SolidBrush(Color.FromArgb(15, theme.TextPrimary)))
            using (var chPen = new Pen(theme.Border, 2f))
            {
                g.FillEllipse(chBrush, cx - chamberRadius, cy - chamberRadius, chamberRadius * 2, chamberRadius * 2);
                g.DrawEllipse(chPen, cx - chamberRadius, cy - chamberRadius, chamberRadius * 2, chamberRadius * 2);
            }

            // Central drive hub
            float hubRadius = chamberRadius * 0.22f;
            using (var hubBrush = new SolidBrush(Color.FromArgb(59, 130, 246)))
            using (var hubPen = new Pen(Color.White, 2f))
            {
                g.FillEllipse(hubBrush, cx - hubRadius, cy - hubRadius, hubRadius * 2, hubRadius * 2);
                g.DrawEllipse(hubPen, cx - hubRadius, cy - hubRadius, hubRadius * 2, hubRadius * 2);
            }

            // Rotor angle driven by ZeroAnimationClock
            float baseAngle = tele.IsRunning ? (ZeroAnimationClock.FluidPhase * 360f * 4f) : 0f;
            float bucketDist = chamberRadius * 0.65f;

            using (var armPen = new Pen(Color.FromArgb(180, 180, 180), 3f))
            using (var fontBucket = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            {
                for (int i = 0; i < 4; i++)
                {
                    double angleRad = (baseAngle + i * 90f) * Math.PI / 180.0;
                    float bx = cx + (float)(Math.Cos(angleRad) * bucketDist);
                    float by = cy + (float)(Math.Sin(angleRad) * bucketDist);

                    // Rotor arm from hub to bucket
                    g.DrawLine(armPen, cx, cy, bx, by);

                    // Bucket circle
                    float bRadius = 14f;
                    RectangleF bRect = new RectangleF(bx - bRadius, by - bRadius, bRadius * 2, bRadius * 2);

                    var bucket = _engine.Buckets[i];
                    Color bColor = Color.FromArgb(34, 197, 94); // balanced
                    using (var bBrush = new SolidBrush(bColor))
                    using (var bPen = new Pen(Color.White, 1.5f))
                    {
                        g.FillEllipse(bBrush, bRect);
                        g.DrawEllipse(bPen, bRect);
                    }

                    TextRenderer.DrawText(g, $"{i + 1}", fontBucket, new Point((int)bx - 5, (int)by - 6), Color.White);
                }
            }

            // Imbalance summary badge at bottom
            double deltaG = _engine.MaxBucketImbalanceGrams;
            bool isBal = _engine.IsRotorBalanced(1.5);
            Color balCol = isBal ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68);
            string balText = isBal ? $"Dynamic Balance: OK (Δ {deltaG:F2} g)" : $"IMBALANCE DETECTED (Δ {deltaG:F2} g)";

            using (var fontBal = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                Rectangle balRect = new Rectangle(rect.X + 16, rect.Bottom - 36, rect.Width - 32, 24);
                using (var balBrush = new SolidBrush(Color.FromArgb(25, balCol)))
                using (var balPen = new Pen(balCol, 1f))
                {
                    g.FillRectangle(balBrush, balRect);
                    g.DrawRectangle(balPen, balRect);
                }
                TextRenderer.DrawText(g, balText, fontBal, balRect, balCol, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawTelemetryPanel(Graphics g, Rectangle rect, CentrifugeTelemetry tele, ZeroThemePalette theme)
        {
            using (var boxBrush = new SolidBrush(Color.FromArgb(12, theme.TextPrimary)))
            using (var borderPen = new Pen(theme.Border, 1f))
            using (var fontTitle = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (var fontLabel = new Font("Segoe UI", 8.5f))
            using (var fontVal = new Font("Segoe UI", 12f, FontStyle.Bold))
            using (var fontBold = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                g.FillRectangle(boxBrush, rect);
                g.DrawRectangle(borderPen, rect);
                TextRenderer.DrawText(g, "OPERATIONAL TELEMETRY & SAFETY INTERLOCKS", fontTitle, new Point(rect.X + 14, rect.Y + 10), theme.TextSecondary);

                int rowY = rect.Y + 36;

                // KPI 1: Speed RPM
                TextRenderer.DrawText(g, "Rotational Speed:", fontLabel, new Point(rect.X + 16, rowY), theme.TextSecondary);
                string rpmStr = $"{tele.CurrentRpm:N0} RPM (Set: {tele.TargetRpm:N0})";
                TextRenderer.DrawText(g, rpmStr, fontVal, new Point(rect.X + 16, rowY + 18), Color.FromArgb(59, 130, 246));

                // KPI 2: Centrifugal Force RCF (g)
                rowY += 56;
                TextRenderer.DrawText(g, "Relative Centrifugal Force (RCF):", fontLabel, new Point(rect.X + 16, rowY), theme.TextSecondary);
                string rcfStr = $"{_engine.CurrentRcfG:N0} x g";
                TextRenderer.DrawText(g, rcfStr, fontVal, new Point(rect.X + 16, rowY + 18), Color.FromArgb(16, 185, 129));

                // KPI 3: Chamber Temperature
                rowY += 56;
                TextRenderer.DrawText(g, "Chamber Temperature:", fontLabel, new Point(rect.X + 16, rowY), theme.TextSecondary);
                string tempStr = $"{tele.CurrentTempC:F1} °C (Set: {tele.TargetTempC:F1} °C) [Active Cooling]";
                TextRenderer.DrawText(g, tempStr, fontBold, new Point(rect.X + 16, rowY + 20), theme.TextPrimary);

                // KPI 4: Vibration Sensor
                rowY += 50;
                TextRenderer.DrawText(g, "Vibration Accelerometer:", fontLabel, new Point(rect.X + 16, rowY), theme.TextSecondary);
                bool hasAlarm = CentrifugeEngine.EvaluateVibrationAlarm(tele.VibrationMmS, out bool isTrip);
                Color vibCol = isTrip ? Color.FromArgb(239, 68, 68) :
                               hasAlarm ? Color.FromArgb(245, 158, 11) : Color.FromArgb(34, 197, 94);

                string vibStr = $"{tele.VibrationMmS:F2} mm/s RMS {(isTrip ? "[CRITICAL TRIP]" : hasAlarm ? "[WARNING]" : "[NORMAL]")}";
                TextRenderer.DrawText(g, vibStr, fontBold, new Point(rect.X + 16, rowY + 20), vibCol);

                // Progress Bar for Vibration
                Rectangle vibBar = new Rectangle(rect.X + 16, rowY + 44, rect.Width - 32, 8);
                using (var vBg = new SolidBrush(Color.FromArgb(40, 128, 128, 128)))
                using (var vFill = new SolidBrush(vibCol))
                {
                    g.FillRectangle(vBg, vibBar);
                    int fillW = (int)((vibBar.Width * Math.Min(6.0, tele.VibrationMmS)) / 6.0);
                    if (fillW > 0) g.FillRectangle(vFill, vibBar.X, vibBar.Y, fillW, vibBar.Height);
                }

                // Lid Lock Safety Interlock Box
                rowY += 68;
                Rectangle lidRect = new Rectangle(rect.X + 16, rowY, rect.Width - 32, 44);
                Color lidColor = tele.LidState == LidLockState.SpeedInterlocked ? Color.FromArgb(34, 197, 94) :
                                 tele.LidState == LidLockState.Locked ? Color.FromArgb(59, 130, 246) :
                                 Color.FromArgb(239, 68, 68);

                using (var lidBrush = new SolidBrush(Color.FromArgb(20, lidColor)))
                using (var lidPen = new Pen(lidColor, 1.5f))
                {
                    g.FillRectangle(lidBrush, lidRect);
                    g.DrawRectangle(lidPen, lidRect);
                }

                string lidText = tele.LidState == LidLockState.SpeedInterlocked ? "LID INTERLOCK: SPEED LOCKED (SAFE)" :
                                 tele.LidState == LidLockState.Locked ? "LID LOCKED" : "LID UNLOCKED";
                TextRenderer.DrawText(g, lidText, fontBold, lidRect, lidColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }
}
