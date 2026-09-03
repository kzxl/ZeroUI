using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Industrial
{
    /// <summary>
    /// Industrial Linear Level & Pressure Gauge for SCADA / MES telemetry (Tank level, temperature, pressure, flow).
    /// Features multi-zone scale thresholds (Normal, Warning, Critical), graduations with tick marks, and value badges.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Industrial & SCADA")]
    [DefaultProperty("Value")]
    [Description("Industrial Linear Level and Pressure Gauge for SCADA telemetry")]
    public class ZeroLinearGauge : Control
    {

        private float _value = 65f;
        private float _minimum = 0f;
        private float _maximum = 100f;
        private string _title = "Pressure";
        private string _unit = "Bar";
        private float _warningThreshold = 75f;
        private float _criticalThreshold = 90f;

        public ZeroLinearGauge()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(200, 70);
            BackColor = Color.Transparent;
        }

        [Category("Data")]
        [DefaultValue(65f)]
        public float Value
        {
            get => _value;
            set
            {
                _value = Math.Max(_minimum, Math.Min(_maximum, value));
                Invalidate();
            }
        }

        [Category("Data")]
        [DefaultValue(0f)]
        public float Minimum
        {
            get => _minimum;
            set { _minimum = value; Invalidate(); }
        }

        [Category("Data")]
        [DefaultValue(100f)]
        public float Maximum
        {
            get => _maximum;
            set { _maximum = Math.Max(_minimum + 1, value); Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue("Pressure")]
        public string Title
        {
            get => _title;
            set { _title = value ?? ""; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue("Bar")]
        public string Unit
        {
            get => _unit;
            set { _unit = value ?? ""; Invalidate(); }
        }

        [Category("Thresholds")]
        [DefaultValue(75f)]
        public float WarningThreshold
        {
            get => _warningThreshold;
            set { _warningThreshold = value; Invalidate(); }
        }

        [Category("Thresholds")]
        [DefaultValue(90f)]
        public float CriticalThreshold
        {
            get => _criticalThreshold;
            set { _criticalThreshold = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var theme = ZeroTheme.Colors;
            int w = Width;
            int h = Height;

            // 1. Header: Title + Current Value Readout
            using var titleFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var titleBrush = new SolidBrush(theme.TextPrimary);
            g.DrawString(_title, titleFont, titleBrush, 2, 2);

            Color statusColor = (_value >= _criticalThreshold)
                ? Color.FromArgb(239, 68, 68)   // Red
                : (_value >= _warningThreshold)
                    ? Color.FromArgb(245, 158, 11) // Amber
                    : Color.FromArgb(16, 185, 129); // Green

            string valText = $"{_value:F1} {_unit}";
            using var valFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            using var valBrush = new SolidBrush(statusColor);
            var valSize = g.MeasureString(valText, valFont);
            g.DrawString(valText, valFont, valBrush, w - valSize.Width - 4, 2);

            // 2. Main Gauge Bar Dimensions
            int barY = 26;
            int barH = 14;
            int barW = w - 8;
            int barX = 4;

            // Track Background
            var trackRect = new Rectangle(barX, barY, barW, barH);
            using (var trackPath = CreateRoundedRectangle(trackRect, 4))
            using (var trackBrush = new SolidBrush(Color.FromArgb(229, 231, 235)))
            {
                g.FillPath(trackBrush, trackPath);
            }

            // Fill Level
            float range = _maximum - _minimum;
            float ratio = range > 0 ? (_value - _minimum) / range : 0f;
            int fillW = Math.Max(4, (int)(barW * ratio));

            var fillRect = new Rectangle(barX, barY, fillW, barH);
            using (var fillPath = CreateRoundedRectangle(fillRect, 4))
            using (var fillBrush = new LinearGradientBrush(new Point(barX, barY), new Point(barX + barW, barY), Color.FromArgb(16, 185, 129), Color.FromArgb(239, 68, 68)))
            {
                g.FillPath(fillBrush, fillPath);
            }

            // Glass Sheen Highlight on Top Half
            using (var sheenBrush = new SolidBrush(Color.FromArgb(50, Color.White)))
            {
                g.FillRectangle(sheenBrush, barX, barY, fillW, barH / 2);
            }

            // 3. Graduations & Scale Ticks
            int tickY = barY + barH + 4;
            using var tickPen = new Pen(Color.FromArgb(156, 163, 175), 1f);
            using var tickFont = new Font("Segoe UI", 7.5f);
            using var tickBrush = new SolidBrush(Color.FromArgb(107, 114, 128));

            int tickSteps = 4;
            for (int i = 0; i <= tickSteps; i++)
            {
                float stepRatio = (float)i / tickSteps;
                int tx = barX + (int)(barW * stepRatio);
                g.DrawLine(tickPen, tx, tickY, tx, tickY + 4);

                float stepVal = _minimum + (range * stepRatio);
                string stepStr = $"{stepVal:F0}";
                var textSize = g.MeasureString(stepStr, tickFont);
                float textX = Math.Max(0, tx - (textSize.Width / 2));
                if (i == tickSteps) textX = Math.Min(w - textSize.Width, textX);
                g.DrawString(stepStr, tickFont, tickBrush, textX, tickY + 6);
            }
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
