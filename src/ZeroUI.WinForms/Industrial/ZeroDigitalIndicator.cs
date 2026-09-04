using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.Scada;
using ZeroUI.WinForms.Native;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Industrial
{
    /// <summary>
    /// High-contrast industrial digital telemetry readout panel with configurable engineering units,
    /// 4-tier alarm threshold color transitions (LowLow, Low, High, HighHigh), and direct SCADA tag binding.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Industrial & SCADA")]
    [Description("Industrial digital telemetry readout indicator with alarm threshold coloring")]
    public class ZeroDigitalIndicator : Control, IScadaBindable
    {
        private double _value = 48.7;
        private string _unit = "bar";
        private string _tagLabel = "PT-101";
        private string _format = "0.0";
        private double _lowLowAlarm = 10.0;
        private double _lowWarning = 20.0;
        private double _highWarning = 80.0;
        private double _highHighAlarm = 90.0;
        private bool _isHovered;

        [Category("SCADA Telemetry")]
        public string? BoundTagPath { get; set; }

        [Category("Telemetry Value")]
        [DefaultValue(48.7)]
        public double Value
        {
            get => _value;
            set { _value = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue("bar")]
        public string Unit
        {
            get => _unit;
            set { _unit = value ?? ""; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue("PT-101")]
        public string TagLabel
        {
            get => _tagLabel;
            set { _tagLabel = value ?? ""; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue("0.0")]
        public string Format
        {
            get => _format;
            set { _format = value ?? "0.0"; Invalidate(); }
        }

        [Category("Alarm Limits")]
        [DefaultValue(10.0)]
        public double LowLowAlarm
        {
            get => _lowLowAlarm;
            set { _lowLowAlarm = value; Invalidate(); }
        }

        [Category("Alarm Limits")]
        [DefaultValue(20.0)]
        public double LowWarning
        {
            get => _lowWarning;
            set { _lowWarning = value; Invalidate(); }
        }

        [Category("Alarm Limits")]
        [DefaultValue(80.0)]
        public double HighWarning
        {
            get => _highWarning;
            set { _highWarning = value; Invalidate(); }
        }

        [Category("Alarm Limits")]
        [DefaultValue(90.0)]
        public double HighHighAlarm
        {
            get => _highHighAlarm;
            set { _highHighAlarm = value; Invalidate(); }
        }

        public ZeroDigitalIndicator()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(130, 65);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9f);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!ZeroDesignHelper.IsInDesignMode(this))
            {
                ZeroTagEngine.RegisterBindable(this);
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);
            ZeroTagEngine.UnregisterBindable(this);
        }

        public void OnTagValueChanged(IScadaTag tag)
        {
            if (tag == null) return;
            if (double.TryParse(tag.Value?.ToString(), out var v))
            {
                Value = v;
            }
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _isHovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _isHovered = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            bool isDark = ZeroTheme.IsDark;
            Color panelBg = isDark ? Color.FromArgb(15, 23, 42) : Color.FromArgb(248, 250, 252);
            Color borderColor = isDark ? Color.FromArgb(51, 65, 85) : Color.FromArgb(203, 213, 225);
            Color labelColor = isDark ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139);

            // Determine value color by alarm tiers
            Color valueColor;
            if (_value <= _lowLowAlarm || _value >= _highHighAlarm)
            {
                valueColor = Color.FromArgb(239, 68, 68); // Critical Red
                borderColor = valueColor;
            }
            else if (_value <= _lowWarning || _value >= _highWarning)
            {
                valueColor = Color.FromArgb(245, 158, 11); // Warning Amber
                borderColor = valueColor;
            }
            else
            {
                valueColor = isDark ? Color.FromArgb(56, 189, 248) : Color.FromArgb(2, 132, 199); // Normal Cyan/Blue
            }

            if (_isHovered)
            {
                borderColor = Color.FromArgb(59, 130, 246);
            }

            // 1. Industrial Beveled Panel
            var panelRect = new RectangleF(1f, 1f, Width - 3f, Height - 3f);
            using (var bgBrush = new SolidBrush(panelBg))
            using (var borderPen = new Pen(borderColor, _isHovered ? 2f : 1.2f))
            {
                g.FillRectangle(bgBrush, panelRect);
                g.DrawRectangle(borderPen, panelRect.X, panelRect.Y, panelRect.Width, panelRect.Height);
            }

            // 2. Tag Label (Top left)
            using (var labelFont = new Font(Font.FontFamily, 7.5f, FontStyle.Bold))
            using (var labelBrush = new SolidBrush(labelColor))
            {
                g.DrawString(_tagLabel, labelFont, labelBrush, 6f, 5f);
            }

            // 3. Digital Readout Value (Center-Left large numbers)
            string valText = _value.ToString(_format);
            using (var numFont = new Font("Segoe UI", 16f, FontStyle.Bold))
            using (var numBrush = new SolidBrush(valueColor))
            {
                g.DrawString(valText, numFont, numBrush, 5f, 22f);

                // 4. Engineering Unit (Placed next to value)
                var valSize = g.MeasureString(valText, numFont);
                using (var unitFont = new Font(Font.FontFamily, 8.5f, FontStyle.Regular))
                using (var unitBrush = new SolidBrush(labelColor))
                {
                    g.DrawString(_unit, unitFont, unitBrush, 7f + valSize.Width, 31f);
                }
            }
        }
    }
}
