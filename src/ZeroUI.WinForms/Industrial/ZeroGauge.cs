using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Icons;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Industrial
{

    /// <summary>
    /// Modern anti-aliased circular gauge/meter for OEE, Yield rate, and equipment efficiency.
    /// </summary>
    [ToolboxItem(true)]
    [ToolboxBitmap(typeof(ZeroIcons), "ZeroGauge.bmp")]
    [Category("ZeroUI - Industrial & SCADA")]
    [DefaultProperty("Value")]
    [Description("Circular gauge meter for OEE, Yield, and equipment efficiency")]
    public class ZeroGauge : Control
    {

        private float _value = 85f; // 0 to 100
        private string _title = "OEE Rate";
        private string _suffix = "%";
        private int _thickness = 8;
        private Color _gaugeColor = Color.FromArgb(16, 185, 129);  // Emerald
        private Color _trackColor = Color.FromArgb(229, 231, 235); // Gray track

        public ZeroGauge()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(110, 110);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9f);

            ZeroTheme.ThemeChanged += (s, e) => Invalidate();
        }

        [Category("Data")]
        [DefaultValue(85f)]
        public float Value
        {
            get => _value;
            set
            {
                _value = Math.Max(0f, Math.Min(100f, value));
                Invalidate();
            }
        }

        [Category("Data")]
        [DefaultValue("OEE Rate")]
        public string Title
        {
            get => _title;
            set { _title = value; Invalidate(); }
        }

        [Category("Data")]
        [DefaultValue("%")]
        public string Suffix
        {
            get => _suffix;
            set { _suffix = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(8)]
        public int Thickness
        {
            get => _thickness;
            set { _thickness = Math.Max(4, value); Invalidate(); }
        }

        [Category("Appearance")]
        public Color GaugeColor
        {
            get => _gaugeColor;
            set { _gaugeColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        public Color TrackColor
        {
            get => _trackColor;
            set { _trackColor = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int pad = _thickness + 2;
            int size = Math.Min(Width, Height) - (pad * 2);
            if (size <= 0) return;

            int x = (Width - size) / 2;
            int y = (Height - size) / 2 - 4;
            Rectangle arcRect = new Rectangle(x, y, size, size);

            // 1. Draw Background Track Ring (240 degrees arch or full 360)
            float startAngle = 135f;
            float sweepLength = 270f;
            Color effTrackColor = _trackColor != Color.FromArgb(229, 231, 235) ? _trackColor : ZeroTheme.Colors.Border;

            using (var trackPen = new Pen(effTrackColor, _thickness))
            {
                trackPen.StartCap = LineCap.Round;
                trackPen.EndCap = LineCap.Round;
                g.DrawArc(trackPen, arcRect, startAngle, sweepLength);
            }

            // 2. Draw Active Progress Arc
            float activeSweep = (_value / 100f) * sweepLength;
            if (activeSweep > 0.5f)
            {
                using var gaugePen = new Pen(_gaugeColor, _thickness);
                gaugePen.StartCap = LineCap.Round;
                gaugePen.EndCap = LineCap.Round;
                g.DrawArc(gaugePen, arcRect, startAngle, activeSweep);
            }

            // 3. Draw Center Value & Title
            string valText = $"{_value:F1}{_suffix}";
            using var valFont = new Font("Segoe UI", 12.5f, FontStyle.Bold);
            Size valSize = TextRenderer.MeasureText(g, valText, valFont);
            Rectangle valRect = new Rectangle(0, y + (size / 2) - valSize.Height / 2 - 4, Width, valSize.Height);
            TextRenderer.DrawText(g, valText, valFont, valRect, ZeroTheme.Colors.TextPrimary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            if (!string.IsNullOrEmpty(_title))
            {
                using var titleFont = new Font("Segoe UI", 7.5f, FontStyle.Regular);
                Rectangle titleRect = new Rectangle(0, valRect.Bottom - 2, Width, 16);
                TextRenderer.DrawText(g, _title, titleFont, titleRect, ZeroTheme.Colors.TextSecondary, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);
            }
        }
    }
}
