using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Charts
{
    /// <summary>
    /// Represents a single stage in a funnel conversion chart.
    /// </summary>
    public class FunnelStage
    {
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
        public Color Color { get; set; } = Color.FromArgb(79, 70, 229);
        public string? Description { get; set; }

        public FunnelStage(string name, double value, Color color, string? description = null)
        {
            Name = name;
            Value = value;
            Color = color;
            Description = description;
        }
    }

    /// <summary>
    /// High-performance Process Pipeline and Conversion Funnel chart control with stage drop-off metrics.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Charts")]
    public class ZeroFunnelChart : Control
    {
        private readonly List<FunnelStage> _stages = new List<FunnelStage>();

        private string _valueSuffix = " pcs";
        private bool _showConversionRates = true;
        private bool _showPercentages = true;
        private int _neckWidth = 100;
        private int _segmentGap = 4;
        private int _hoverIndex = -1;

        public ZeroFunnelChart()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            Size = new Size(500, 320);

            ZeroTheme.ThemeChanged += (s, e) => Invalidate();
        }

        [Category("Appearance")]
        [DefaultValue(" pcs")]
        public string ValueSuffix
        {
            get => _valueSuffix;
            set { _valueSuffix = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        public bool ShowConversionRates
        {
            get => _showConversionRates;
            set { _showConversionRates = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        public bool ShowPercentages
        {
            get => _showPercentages;
            set { _showPercentages = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(100)]
        public int NeckWidth
        {
            get => _neckWidth;
            set { _neckWidth = Math.Max(40, value); Invalidate(); }
        }

        [Browsable(false)]
        public List<FunnelStage> Stages => _stages;

        public void AddStage(string name, double value, Color color, string? description = null)
        {
            _stages.Add(new FunnelStage(name, value, color, description));
            Invalidate();
        }

        public void ClearStages()
        {
            _stages.Clear();
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_stages.Count == 0) return;

            int topMargin = 20;
            int bottomMargin = 20;
            int availH = Height - topMargin - bottomMargin;
            int count = _stages.Count;
            float stageH = (availH - (count - 1) * _segmentGap) / (float)count;

            int newHover = -1;
            for (int i = 0; i < count; i++)
            {
                float y1 = topMargin + i * (stageH + _segmentGap);
                float y2 = y1 + stageH;
                if (e.Y >= y1 && e.Y <= y2)
                {
                    newHover = i;
                    break;
                }
            }

            if (_hoverIndex != newHover)
            {
                _hoverIndex = newHover;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverIndex != -1)
            {
                _hoverIndex = -1;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var palette = ZeroTheme.Colors;

            if (_stages.Count == 0)
            {
                using var noteBrush = new SolidBrush(palette.TextSecondary);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("No funnel stages defined.", Font, noteBrush, ClientRectangle, sf);
                return;
            }

            int count = _stages.Count;
            int topMargin = 24;
            int bottomMargin = 20;
            int availH = Height - topMargin - bottomMargin;
            if (availH <= 30) return;

            float stageH = (availH - (count - 1) * _segmentGap) / (float)count;

            // Compute Widths: Funnel occupies center 45% of width
            float funnelCenter = Width * 0.45f;
            float maxFunnelW = Math.Min(340f, Width * 0.42f);
            float minFunnelW = Math.Max(50f, Math.Min(_neckWidth, maxFunnelW * 0.4f));

            double topValue = _stages[0].Value;
            if (topValue <= 0) topValue = 1.0;

            for (int i = 0; i < count; i++)
            {
                var stage = _stages[i];
                float yTop = topMargin + i * (stageH + _segmentGap);
                float yBot = yTop + stageH;

                float tRatio = i / (float)count;
                float bRatio = (i + 1) / (float)count;

                float wTop = maxFunnelW - (maxFunnelW - minFunnelW) * tRatio;
                float wBot = maxFunnelW - (maxFunnelW - minFunnelW) * bRatio;

                // Expand slightly on hover
                if (i == _hoverIndex)
                {
                    wTop += 8f;
                    wBot += 8f;
                }

                PointF[] trapezoid = new PointF[]
                {
                    new PointF(funnelCenter - wTop / 2f, yTop),
                    new PointF(funnelCenter + wTop / 2f, yTop),
                    new PointF(funnelCenter + wBot / 2f, yBot),
                    new PointF(funnelCenter - wBot / 2f, yBot)
                };

                // Draw Trapezoid with sleek gradient
                Color fillA = stage.Color;
                Color fillB = Color.FromArgb(
                    Math.Max(0, fillA.R - 25),
                    Math.Max(0, fillA.G - 25),
                    Math.Max(0, fillA.B - 25));

                using (var lgb = new LinearGradientBrush(new PointF(funnelCenter - wTop / 2f, yTop), new PointF(funnelCenter + wTop / 2f, yBot), fillA, fillB))
                {
                    g.FillPolygon(lgb, trapezoid);
                }

                using (var borderPen = new Pen(palette.Surface, 1.5f))
                {
                    g.DrawPolygon(borderPen, trapezoid);
                }

                // Center Value Label inside trapezoid
                double pctOfTop = (stage.Value / topValue) * 100.0;
                string centerLabel = $"{stage.Value:N0}{_valueSuffix}";
                if (_showPercentages && i > 0) centerLabel += $" ({pctOfTop:F1}%)";

                using (var centerBrush = new SolidBrush(Color.White))
                using (var centerFont = new Font(Font.FontFamily, 8.5f, FontStyle.Bold))
                {
                    var cSz = g.MeasureString(centerLabel, centerFont);
                    float cy = yTop + stageH / 2f - cSz.Height / 2f;
                    g.DrawString(centerLabel, centerFont, centerBrush, funnelCenter - cSz.Width / 2f, cy);
                }

                // Left Label: Stage Name & description
                using (var nameBrush = new SolidBrush(palette.TextPrimary))
                using (var nameFont = new Font(Font.FontFamily, 9f, FontStyle.Bold))
                using (var descBrush = new SolidBrush(palette.TextSecondary))
                using (var descFont = new Font(Font.FontFamily, 7.5f))
                {
                    float textRight = funnelCenter - wTop / 2f - 12f;
                    var nSz = g.MeasureString(stage.Name, nameFont);
                    float nx = Math.Max(8f, textRight - nSz.Width);
                    float ny = yTop + (stageH - nSz.Height) / 2f;
                    if (!string.IsNullOrEmpty(stage.Description)) ny -= 6f;

                    g.DrawString(stage.Name, nameFont, nameBrush, nx, ny);

                    if (!string.IsNullOrEmpty(stage.Description))
                    {
                        var dSz = g.MeasureString(stage.Description, descFont);
                        float dx = Math.Max(8f, textRight - dSz.Width);
                        g.DrawString(stage.Description, descFont, descBrush, dx, ny + nSz.Height);
                    }
                }

                // Right Label: Conversion Rate relative to previous stage
                if (_showConversionRates)
                {
                    float rightX = funnelCenter + Math.Max(wTop, wBot) / 2f + 16f;
                    float midY = yTop + stageH / 2f;

                    if (i == 0)
                    {
                        using var topTagBrush = new SolidBrush(palette.Success);
                        using var tagFont = new Font(Font.FontFamily, 8f, FontStyle.Bold);
                        g.DrawString("100% INWARD YIELD", tagFont, topTagBrush, rightX, midY - 6f);
                    }
                    else
                    {
                        double prevVal = _stages[i - 1].Value;
                        double convRate = prevVal > 0 ? (stage.Value / prevVal) * 100.0 : 0.0;
                        double dropOff = 100.0 - convRate;

                        string rateText = $"Yield: {convRate:F1}%  (Loss: -{dropOff:F1}%)";
                        Color rateColor = convRate >= 95 ? palette.Success : (convRate >= 80 ? palette.Warning : palette.Danger);

                        using var rateBrush = new SolidBrush(rateColor);
                        using var rateFont = new Font(Font.FontFamily, 8.5f, FontStyle.Bold);
                        g.DrawString(rateText, rateFont, rateBrush, rightX, midY - 6f);
                    }
                }
            }
        }
    }
}
