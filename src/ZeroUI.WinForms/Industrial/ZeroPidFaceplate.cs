using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.Scada;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Industrial
{
    public enum ZeroPidMode
    {
        Auto,
        Manual,
        Cascade
    }

    /// <summary>
    /// Industrial Single-Loop Process Controller Faceplate (PID Faceplate).
    /// Features simultaneous Process Variable (PV) and Setpoint (SP) vertical comparison bars,
    /// horizontal Manipulated Variable (MV 0-100%) output gauge, mode selectors (Auto/Man/Cas),
    /// and IScadaBindable tag engine integration.
    /// </summary>
    public class ZeroPidFaceplate : Control, IScadaBindable
    {
        private string _loopTag = "PIC-101";
        private string _loopDescription = "Boiler Steam Header Pressure";
        private string _engineeringUnit = "PSI";
        private double _processVariable = 48.2;
        private double _setPoint = 50.0;
        private double _manipulatedVariable = 62.0; // 0 - 100%
        private double _minScale = 0.0;
        private double _maxScale = 100.0;
        private ZeroPidMode _mode = ZeroPidMode.Auto;

        // Tuning parameters
        private double _kp = 1.25;
        private double _ti = 18.0;
        private double _td = 2.5;

        // Interaction bounds
        private Rectangle _btnAutoRect;
        private Rectangle _btnManRect;
        private Rectangle _btnCasRect;
        private Rectangle _btnSpPlusRect;
        private Rectangle _btnSpMinusRect;

        [Category("SCADA Telemetry")]
        public string? BoundTagPath { get; set; }

        [Category("Loop Identification")]
        [DefaultValue("PIC-101")]
        public string LoopTag
        {
            get => _loopTag;
            set { _loopTag = value ?? ""; Invalidate(); }
        }

        [Category("Loop Identification")]
        [DefaultValue("Boiler Steam Header Pressure")]
        public string LoopDescription
        {
            get => _loopDescription;
            set { _loopDescription = value ?? ""; Invalidate(); }
        }

        [Category("Loop Identification")]
        [DefaultValue("PSI")]
        public string EngineeringUnit
        {
            get => _engineeringUnit;
            set { _engineeringUnit = value ?? ""; Invalidate(); }
        }

        [Category("Process Dynamics")]
        [DefaultValue(48.2)]
        public double ProcessVariable
        {
            get => _processVariable;
            set { _processVariable = value; Invalidate(); }
        }

        [Category("Process Dynamics")]
        [DefaultValue(50.0)]
        public double SetPoint
        {
            get => _setPoint;
            set { _setPoint = Math.Max(_minScale, Math.Min(_maxScale, value)); Invalidate(); }
        }

        [Category("Process Dynamics")]
        [DefaultValue(62.0)]
        public double ManipulatedVariable
        {
            get => _manipulatedVariable;
            set { _manipulatedVariable = Math.Max(0, Math.Min(100, value)); Invalidate(); }
        }

        [Category("Control Loop")]
        [DefaultValue(ZeroPidMode.Auto)]
        public ZeroPidMode Mode
        {
            get => _mode;
            set { _mode = value; Invalidate(); }
        }

        public event EventHandler? SetPointChanged;
        public event EventHandler? ModeChanged;

        public ZeroPidFaceplate()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Size = new Size(260, 310);
            Cursor = Cursors.Default;

            ZeroTheme.ThemeChanged += OnThemeChanged;
            ZeroTagEngine.RegisterBindable(this);
        }

        private void OnThemeChanged(object? sender, EventArgs e) => Invalidate();

        public void OnTagValueChanged(IScadaTag tag)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnTagValueChanged(tag)));
                return;
            }

            if (tag.TagPath.EndsWith(".PV", StringComparison.OrdinalIgnoreCase) && tag.Value is double pv)
            {
                ProcessVariable = pv;
            }
            else if (tag.TagPath.EndsWith(".SP", StringComparison.OrdinalIgnoreCase) && tag.Value is double sp)
            {
                SetPoint = sp;
            }
            else if (tag.TagPath.EndsWith(".MV", StringComparison.OrdinalIgnoreCase) && tag.Value is double mv)
            {
                ManipulatedVariable = mv;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (_btnAutoRect.Contains(e.Location))
            {
                Mode = ZeroPidMode.Auto;
                ModeChanged?.Invoke(this, EventArgs.Empty);
            }
            else if (_btnManRect.Contains(e.Location))
            {
                Mode = ZeroPidMode.Manual;
                ModeChanged?.Invoke(this, EventArgs.Empty);
            }
            else if (_btnCasRect.Contains(e.Location))
            {
                Mode = ZeroPidMode.Cascade;
                ModeChanged?.Invoke(this, EventArgs.Empty);
            }
            else if (_btnSpPlusRect.Contains(e.Location))
            {
                SetPoint = Math.Min(_maxScale, _setPoint + 1.0);
                SimulatedPlcDriver.PidSetPoint = _setPoint;
                SetPointChanged?.Invoke(this, EventArgs.Empty);
            }
            else if (_btnSpMinusRect.Contains(e.Location))
            {
                SetPoint = Math.Max(_minScale, _setPoint - 1.0);
                SimulatedPlcDriver.PidSetPoint = _setPoint;
                SetPointChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var palette = ZeroTheme.Colors;
            bool isDark = ZeroTheme.IsDark;

            // 1. Faceplate Outer Bezel
            var borderRect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var brushBg = new SolidBrush(palette.CardBackground))
            {
                g.FillRectangle(brushBg, borderRect);
            }
            using (var penBorder = new Pen(palette.Border, 1.5f))
            {
                g.DrawRectangle(penBorder, borderRect);
            }

            // 2. Loop Header Strip
            int headerH = 34;
            var headerRect = new Rectangle(0, 0, Width, headerH);
            using (var brushHeader = new SolidBrush(palette.HeaderBackground))
            {
                g.FillRectangle(brushHeader, headerRect);
            }
            using (var penHeader = new Pen(palette.Border, 1f))
            {
                g.DrawLine(penHeader, 0, headerH, Width, headerH);
            }

            using var fontTag = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            using var fontDesc = new Font("Segoe UI", 7f, FontStyle.Regular);
            using var brushText = new SolidBrush(palette.TextPrimary);
            using var brushSecondary = new SolidBrush(palette.TextSecondary);

            g.DrawString(_loopTag, fontTag, brushText, 10, 3);
            g.DrawString(_loopDescription, fontDesc, brushSecondary, 10, 18);

            // 3. Mode Buttons (AUTO, MAN, CAS)
            int modeBtnY = headerH + 6;
            int modeBtnW = 52;
            int modeBtnH = 22;

            _btnAutoRect = new Rectangle(10, modeBtnY, modeBtnW, modeBtnH);
            _btnManRect = new Rectangle(66, modeBtnY, modeBtnW, modeBtnH);
            _btnCasRect = new Rectangle(122, modeBtnY, modeBtnW, modeBtnH);

            DrawModeButton(g, _btnAutoRect, "AUTO", _mode == ZeroPidMode.Auto, palette);
            DrawModeButton(g, _btnManRect, "MAN", _mode == ZeroPidMode.Manual, palette);
            DrawModeButton(g, _btnCasRect, "CAS", _mode == ZeroPidMode.Cascade, palette);

            // SP Adjustment Buttons (+ / -)
            _btnSpMinusRect = new Rectangle(Width - 62, modeBtnY, 24, modeBtnH);
            _btnSpPlusRect = new Rectangle(Width - 34, modeBtnY, 24, modeBtnH);
            DrawButton(g, _btnSpMinusRect, "-", palette.Surface, palette.TextPrimary);
            DrawButton(g, _btnSpPlusRect, "+", palette.Surface, palette.TextPrimary);

            // 4. Vertical Process Variable (PV) & Setpoint (SP) Dual Bargraph
            int barY = modeBtnY + modeBtnH + 10;
            int barH = 140;
            int barW = 26;

            int pvX = 40;
            int spX = 100;

            // Scale axis ticks
            DrawScaleAxis(g, 12, barY, barH, _minScale, _maxScale, palette);

            // PV Bar
            DrawVerticalBar(g, pvX, barY, barW, barH, _processVariable, _minScale, _maxScale, palette.Primary, palette);
            // SP Bar
            DrawVerticalBar(g, spX, barY, barW, barH, _setPoint, _minScale, _maxScale, palette.Success, palette);

            // Readouts below bars
            using var fontVal = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var fontLbl = new Font("Segoe UI", 7f, FontStyle.Bold);

            var sf = new StringFormat { Alignment = StringAlignment.Center };
            g.DrawString("PV", fontLbl, new SolidBrush(palette.Primary), pvX + barW / 2, barY + barH + 4, sf);
            g.DrawString($"{_processVariable:0.0}", fontVal, brushText, pvX + barW / 2, barY + barH + 16, sf);

            g.DrawString("SP", fontLbl, new SolidBrush(palette.Success), spX + barW / 2, barY + barH + 4, sf);
            g.DrawString($"{_setPoint:0.0}", fontVal, brushText, spX + barW / 2, barY + barH + 16, sf);

            // Right Info Box (Tuning & Dev)
            int infoX = 150;
            int infoY = barY + 4;
            using var fontParam = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            double dev = _processVariable - _setPoint;
            Color devCol = Math.Abs(dev) > 3.0 ? palette.Danger : palette.Success;
            g.DrawString($"Dev: {dev:+0.0;-0.0;0.0} {_engineeringUnit}", fontParam, new SolidBrush(devCol), infoX, infoY);
            g.DrawString($"Kp: {_kp:0.00}", fontParam, brushSecondary, infoX, infoY + 18);
            g.DrawString($"Ti: {_ti:0.0} s", fontParam, brushSecondary, infoX, infoY + 34);
            g.DrawString($"Td: {_td:0.0} s", fontParam, brushSecondary, infoX, infoY + 50);

            // 5. Horizontal Manipulated Variable Output (MV 0-100%)
            int mvY = Height - 44;
            int mvX = 40;
            int mvW = Width - 50;
            int mvH = 16;

            using var fontMv = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            g.DrawString("MV", fontMv, brushSecondary, 12, mvY + 1);

            var mvTrack = new Rectangle(mvX, mvY, mvW, mvH);
            using (var brushTrack = new SolidBrush(palette.Surface))
            {
                g.FillRectangle(brushTrack, mvTrack);
            }
            using (var penTrack = new Pen(palette.Border, 1f))
            {
                g.DrawRectangle(penTrack, mvTrack);
            }

            int fillW = (int)(mvW * (_manipulatedVariable / 100.0));
            if (fillW > 0)
            {
                using var brushFill = new SolidBrush(palette.Warning);
                g.FillRectangle(brushFill, mvX + 1, mvY + 1, fillW, mvH - 1);
            }

            // MV percentage text
            using (var brushMvText = new SolidBrush(palette.TextPrimary))
            {
                var sfMv = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString($"{_manipulatedVariable:0.0}%", fontDesc, brushMvText, mvTrack, sfMv);
            }
        }

        private static void DrawVerticalBar(Graphics g, int x, int y, int w, int h, double val, double min, double max, Color fillCol, ZeroThemePalette palette)
        {
            var trackRect = new Rectangle(x, y, w, h);
            using (var brushTrack = new SolidBrush(palette.Surface))
            {
                g.FillRectangle(brushTrack, trackRect);
            }
            using (var penTrack = new Pen(palette.Border, 1f))
            {
                g.DrawRectangle(penTrack, trackRect);
            }

            double clamped = Math.Max(min, Math.Min(max, val));
            double ratio = (clamped - min) / (max - min);
            int barFillH = (int)(h * ratio);

            if (barFillH > 0)
            {
                int fillY = y + h - barFillH;
                using var brushFill = new SolidBrush(fillCol);
                g.FillRectangle(brushFill, x + 1, fillY, w - 1, barFillH);
            }
        }

        private static void DrawScaleAxis(Graphics g, int x, int y, int h, double min, double max, ZeroThemePalette palette)
        {
            using var fontTick = new Font("Segoe UI", 6.5f, FontStyle.Regular);
            using var brushTick = new SolidBrush(palette.TextSecondary);
            using var penTick = new Pen(palette.Border, 1f);

            var sf = new StringFormat { Alignment = StringAlignment.Far };

            // 100%, 50%, 0% ticks
            g.DrawString($"{max:0}", fontTick, brushTick, x + 20, y - 4, sf);
            g.DrawLine(penTick, x + 22, y, x + 26, y);

            g.DrawString($"{(max + min) / 2:0}", fontTick, brushTick, x + 20, y + h / 2 - 4, sf);
            g.DrawLine(penTick, x + 22, y + h / 2, x + 26, y + h / 2);

            g.DrawString($"{min:0}", fontTick, brushTick, x + 20, y + h - 8, sf);
            g.DrawLine(penTick, x + 22, y + h, x + 26, y + h);
        }

        private static void DrawModeButton(Graphics g, Rectangle r, string text, bool selected, ZeroThemePalette palette)
        {
            Color bg = selected ? palette.Primary : palette.Surface;
            Color fg = selected ? Color.White : palette.TextPrimary;

            using (var brush = new SolidBrush(bg))
            {
                g.FillRectangle(brush, r);
            }
            using (var pen = new Pen(selected ? palette.Primary : palette.Border, 1f))
            {
                g.DrawRectangle(pen, r);
            }
            using var font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            using var brushFg = new SolidBrush(fg);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(text, font, brushFg, r, sf);
        }

        private static void DrawButton(Graphics g, Rectangle r, string text, Color bg, Color fg)
        {
            using (var brush = new SolidBrush(bg))
            {
                g.FillRectangle(brush, r);
            }
            using (var pen = new Pen(Color.FromArgb(120, Color.Gray), 1f))
            {
                g.DrawRectangle(pen, r);
            }
            using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var brushFg = new SolidBrush(fg);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(text, font, brushFg, r, sf);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ZeroTheme.ThemeChanged -= OnThemeChanged;
                ZeroTagEngine.UnregisterBindable(this);
            }
            base.Dispose(disposing);
        }
    }
}
