using System;
using System.Drawing;
using System.Windows.Forms;
using ZeroUI.Core.Scada;
using ZeroUI.Core.Scada.Safety;
using ZeroUI.WinForms.Base;
using ZeroUI.WinForms.Industrial;
using ZeroUI.WinForms.Navigation;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.Samples.WinformDemo.Forms
{
    public sealed partial class MainForm
    {
        private void InitializeScadaSafetyCluster(ZeroTabPage page)
        {
            var pnlContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                BackColor = Color.Transparent
            };

            // 1. Top: ISA-18.2 Alarm Banner Ticker
            var alarmBanner = new AlarmBannerControl
            {
                Dock = DockStyle.Top,
                Height = 38,
                OperatorName = "Chief Operator"
            };
            pnlContainer.Controls.Add(alarmBanner);

            var spacer = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent };
            pnlContainer.Controls.Add(spacer);

            // 2. Main Dual Split Container
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 550,
                BackColor = Color.Transparent
            };

            // ---- Left Card: Machine Safety & Light Curtain ----
            var cardSafety = new ZeroCard
            {
                Dock = DockStyle.Fill,
                Title = "SIL 3 / PLe Machine Safety & Optical Curtain",
                StepNumber = 1
            };

            var pnlSafetyContent = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(12) };

            var lblEStopHeader = new Label
            {
                Text = "EMERGENCY STOP (TWIST-TO-RESET)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38),
                AutoSize = true,
                Location = new Point(16, 12)
            };
            pnlSafetyContent.Controls.Add(lblEStopHeader);

            var eStop = new EmergencyStopControl
            {
                Location = new Point(20, 38),
                Size = new Size(150, 150),
                ResetMode = EStopResetMode.TwistToReset
            };

            var lblEStopStatus = new Label
            {
                Text = "Status: READY (Contacts 1 & 2 Closed)",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(34, 197, 94),
                AutoSize = true,
                Location = new Point(190, 60)
            };

            var btnTripEStop = new Button
            {
                Text = "🚨 Trip E-Stop",
                Location = new Point(190, 95),
                Size = new Size(130, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnTripEStop.Click += (s, e) => eStop.Trip();

            var btnResetEStop = new Button
            {
                Text = "🔄 Reset E-Stop",
                Location = new Point(190, 135),
                Size = new Size(130, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnResetEStop.Click += (s, e) => eStop.Reset();

            eStop.Tripped += (s, e) =>
            {
                lblEStopStatus.Text = "Status: TRIPPED (Interlock Open)";
                lblEStopStatus.ForeColor = Color.FromArgb(239, 68, 68);
                ScadaAlarmEngine.RaiseAlarm("ESTOP_01", "Machine.Safety.EStop", "CRITICAL: Emergency Stop Depressed on Assembly Line 1", ScadaAlarmSeverity.Critical);
            };

            eStop.ResetCompleted += (s, e) =>
            {
                lblEStopStatus.Text = "Status: READY (Contacts 1 & 2 Closed)";
                lblEStopStatus.ForeColor = Color.FromArgb(34, 197, 94);
                ScadaAlarmEngine.Acknowledge("ESTOP_01", "Operator");
                ScadaAlarmEngine.ClearAlarm("ESTOP_01");
            };

            pnlSafetyContent.Controls.Add(eStop);
            pnlSafetyContent.Controls.Add(lblEStopStatus);
            pnlSafetyContent.Controls.Add(btnTripEStop);
            pnlSafetyContent.Controls.Add(btnResetEStop);

            // Light Curtain Section
            var lblCurtainHeader = new Label
            {
                Text = "TYPE 4 SAFETY LIGHT CURTAIN (16 BEAMS)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(234, 179, 8),
                AutoSize = true,
                Location = new Point(16, 210)
            };
            pnlSafetyContent.Controls.Add(lblCurtainHeader);

            var lightCurtain = new LightCurtainBar
            {
                Location = new Point(20, 238),
                Size = new Size(200, 260),
                BeamCount = 16,
                InteractiveSimulation = true
            };

            var lblCurtainStatus = new Label
            {
                Text = "Safety Field: CLEAR (OSSD Active)",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(34, 197, 94),
                AutoSize = true,
                Location = new Point(240, 260)
            };

            var btnBreakBeam = new Button
            {
                Text = "✋ Break Beam",
                Location = new Point(240, 295),
                Size = new Size(130, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(234, 179, 8),
                ForeColor = Color.Black,
                Cursor = Cursors.Hand
            };
            btnBreakBeam.Click += (s, e) =>
            {
                lightCurtain.SimulateObstacle(7, 3);
                lblCurtainStatus.Text = "Safety Field: INTRUSION (OSSD Trip)";
                lblCurtainStatus.ForeColor = Color.FromArgb(239, 68, 68);
                ScadaAlarmEngine.RaiseAlarm("CURTAIN_01", "Safety.Curtain.Trip", "HIGH: Optical Curtain Intrusion Detected at Station 4", ScadaAlarmSeverity.High);
            };

            var btnClearBeam = new Button
            {
                Text = "✔ Clear Field",
                Location = new Point(240, 335),
                Size = new Size(130, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnClearBeam.Click += (s, e) =>
            {
                lightCurtain.ClearObstacle();
                lblCurtainStatus.Text = "Safety Field: CLEAR (OSSD Active)";
                lblCurtainStatus.ForeColor = Color.FromArgb(34, 197, 94);
                ScadaAlarmEngine.ClearAlarm("CURTAIN_01");
            };

            pnlSafetyContent.Controls.Add(lightCurtain);
            pnlSafetyContent.Controls.Add(lblCurtainStatus);
            pnlSafetyContent.Controls.Add(btnBreakBeam);
            pnlSafetyContent.Controls.Add(btnClearBeam);

            cardSafety.ContentPanel.Controls.Add(pnlSafetyContent);
            split.Panel1.Controls.Add(cardSafety);

            // ---- Right Card: Edgewise Meters & MultiState LEDs ----
            var cardInstruments = new ZeroCard
            {
                Dock = DockStyle.Fill,
                Title = "DIN 43700 Edgewise Indicators & Multi-State Pilot LEDs",
                StepNumber = 2
            };

            var pnlInstrContent = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(12) };

            var lblMeterHeader = new Label
            {
                Text = "DIN 43700 EDGEWISE METERS (VERTICAL & HORIZONTAL)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(59, 130, 246),
                AutoSize = true,
                Location = new Point(16, 12)
            };
            pnlInstrContent.Controls.Add(lblMeterHeader);

            var meterVertical = new EdgewiseMeter
            {
                Location = new Point(20, 38),
                Size = new Size(80, 220),
                Orientation = EdgewiseOrientation.Vertical,
                Title = "PRESSURE",
                Unit = "bar",
                Value = 68.5,
                Minimum = 0,
                Maximum = 100,
                HighAlarmThreshold = 85,
                LowAlarmThreshold = 15
            };

            var meterHorizontal = new EdgewiseMeter
            {
                Location = new Point(120, 48),
                Size = new Size(260, 75),
                Orientation = EdgewiseOrientation.Horizontal,
                Title = "TEMPERATURE",
                Unit = "°C",
                Value = 74.2,
                Minimum = 0,
                Maximum = 120,
                HighAlarmThreshold = 95,
                LowAlarmThreshold = 10
            };

            pnlInstrContent.Controls.Add(meterVertical);
            pnlInstrContent.Controls.Add(meterHorizontal);

            // MultiState LEDs Section
            var lblLedHeader = new Label
            {
                Text = "MULTI-STATE SCADA PILOT LIGHTS",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(168, 85, 247),
                AutoSize = true,
                Location = new Point(16, 275)
            };
            pnlInstrContent.Controls.Add(lblLedHeader);

            var led1 = new MultiStateLed { Location = new Point(20, 305), Size = new Size(64, 76), State = MultiStateLedState.Normal, Label = "RUN" };
            var led2 = new MultiStateLed { Location = new Point(95, 305), Size = new Size(64, 76), State = MultiStateLedState.Standby, Label = "IDLE" };
            var led3 = new MultiStateLed { Location = new Point(170, 305), Size = new Size(64, 76), State = MultiStateLedState.Warning, Shape = MultiStateLedShape.Square, Label = "WARN" };
            var led4 = new MultiStateLed { Location = new Point(245, 305), Size = new Size(64, 76), State = MultiStateLedState.Alarm, BlinkRate = MultiStateLedBlink.Fast, Label = "TRIP" };

            pnlInstrContent.Controls.Add(led1);
            pnlInstrContent.Controls.Add(led2);
            pnlInstrContent.Controls.Add(led3);
            pnlInstrContent.Controls.Add(led4);

            var btnCycleLed = new Button
            {
                Text = "💡 Cycle LED States",
                Location = new Point(20, 395),
                Size = new Size(140, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnCycleLed.Click += (s, e) =>
            {
                led1.State = (MultiStateLedState)(((int)led1.State + 1) % 7);
                led2.State = (MultiStateLedState)(((int)led2.State + 1) % 7);
            };

            var btnSpikeMeter = new Button
            {
                Text = "📈 High Pressure Alarm",
                Location = new Point(170, 395),
                Size = new Size(160, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnSpikeMeter.Click += (s, e) =>
            {
                meterVertical.Value = 92.4;
                ScadaAlarmEngine.RaiseAlarm("PRESS_HIGH", "Header.Line1.Pressure", "WARNING: Header pressure exceeds 85 bar threshold", ScadaAlarmSeverity.High);
            };

            pnlInstrContent.Controls.Add(btnCycleLed);
            pnlInstrContent.Controls.Add(btnSpikeMeter);

            cardInstruments.ContentPanel.Controls.Add(pnlInstrContent);
            split.Panel2.Controls.Add(cardInstruments);

            pnlContainer.Controls.Add(split);
            page.Controls.Add(pnlContainer);
        }
    }
}
