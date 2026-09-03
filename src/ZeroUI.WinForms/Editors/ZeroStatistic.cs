using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZeroUI.WinForms.Editors
{

    public enum ZeroTrendDirection
    {
        None,
        Up,
        Down
    }

    /// <summary>
    /// Modern KPI Metric Card component for ZeroUI executive dashboards and analytical summaries.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultProperty("Value")]
    [Description("KPI Metric Card component for dashboards and analytical summaries")]
    public class ZeroStatistic : Control
    {

        private string _title = "Metric Title";
        private string _value = "0";
        private string? _prefix;
        private string? _suffix;
        private ZeroTrendDirection _trend = ZeroTrendDirection.None;
        private string? _trendText;

        private Color _valueColor = Color.FromArgb(17, 24, 39);
        private int _borderRadius = 8;

        public ZeroStatistic()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(200, 95);
            BackColor = Color.White;
        }

        [Category("Data")]
        [DefaultValue("Metric Title")]
        public string Title
        {
            get => _title;
            set { _title = value; Invalidate(); }
        }


        [Category("Data")]
        [DefaultValue("0")]
        public string Value
        {
            get => _value;
            set { _value = value; Invalidate(); }
        }

        [Category("Data")]
        [DefaultValue(null)]
        public string? Prefix
        {
            get => _prefix;
            set { _prefix = value; Invalidate(); }
        }

        [Category("Data")]
        [DefaultValue(null)]
        public string? Suffix
        {
            get => _suffix;
            set { _suffix = value; Invalidate(); }
        }

        [Category("Data")]
        [DefaultValue(ZeroTrendDirection.None)]
        public ZeroTrendDirection Trend
        {
            get => _trend;
            set { _trend = value; Invalidate(); }
        }

        [Category("Data")]
        [DefaultValue(null)]
        public string? TrendText
        {
            get => _trendText;
            set { _trendText = value; Invalidate(); }
        }

        [Category("Appearance")]
        public Color ValueColor
        {
            get => _valueColor;
            set { _valueColor = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // 1. Card Container
            using (var path = CreateRoundedRectangle(rect, _borderRadius))
            {
                using var bgBrush = new SolidBrush(BackColor);
                g.FillPath(bgBrush, path);

                using var borderPen = new Pen(Color.FromArgb(229, 231, 235), 1f);
                g.DrawPath(borderPen, path);
            }

            // 2. Title Text (Small Gray)
            Rectangle titleRect = new Rectangle(16, 12, Width - 32, 18);
            TextRenderer.DrawText(
                g,
                _title,
                new Font("Segoe UI", 8.5f, FontStyle.Regular),
                titleRect,
                Color.FromArgb(107, 114, 128),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            // 3. Value with Prefix & Suffix (Large Bold)
            string fullVal = $"{_prefix}{_value} {_suffix}".Trim();
            Rectangle valRect = new Rectangle(16, 32, Width - 32, 34);
            TextRenderer.DrawText(
                g,
                fullVal,
                new Font("Segoe UI", 18f, FontStyle.Bold),
                valRect,
                _valueColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            // 4. Trend Indicator (Optional footer note)
            if (_trend != ZeroTrendDirection.None && !string.IsNullOrEmpty(_trendText))
            {
                var (trendChar, trendColor) = _trend == ZeroTrendDirection.Up
                    ? ("▲", Color.FromArgb(56, 158, 13))   // Green
                    : ("▼", Color.FromArgb(207, 19, 34));   // Red

                string trendFull = $"{trendChar} {_trendText}";
                Rectangle trendRect = new Rectangle(16, 68, Width - 32, 18);
                TextRenderer.DrawText(
                    g,
                    trendFull,
                    new Font("Segoe UI", 8f, FontStyle.Bold),
                    trendRect,
                    trendColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(rect.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
