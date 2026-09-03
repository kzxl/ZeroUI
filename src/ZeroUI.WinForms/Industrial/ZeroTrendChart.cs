using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Native;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Industrial
{
    public class TrendChannel
    {
        public string Name { get; set; } = "Channel";
        public string Unit { get; set; } = "";
        public Color Color { get; set; } = Color.FromArgb(59, 130, 246);
        public float MinValue { get; set; } = 0f;
        public float MaxValue { get; set; } = 100f;
        public float LatestValue { get; internal set; }

        internal readonly float[] Buffer;
        internal int Head = 0;
        internal int Count = 0;

        public TrendChannel(string name, string unit, Color color, float min, float max, int capacity = 180)
        {
            Name = name;
            Unit = unit;
            Color = color;
            MinValue = min;
            MaxValue = max;
            Buffer = new float[capacity];
        }

        public void Add(float value)
        {
            Buffer[Head] = value;
            Head = (Head + 1) % Buffer.Length;
            if (Count < Buffer.Length) Count++;
            LatestValue = value;
        }

        public float GetPoint(int index)
        {
            if (index < 0 || index >= Count) return 0f;
            int start = (Head - Count + Buffer.Length) % Buffer.Length;
            int actualIndex = (start + index) % Buffer.Length;
            return Buffer[actualIndex];
        }
    }

    /// <summary>
    /// High-performance real-time 60 FPS oscilloscope and trend chart for SCADA and industrial telemetry.
    /// Utilizes fixed-size circular ring buffers with zero GC allocation on continuous signal streaming.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Industrial & SCADA")]
    [Description("High-performance real-time trend and oscilloscope chart for SCADA telemetry")]
    public class ZeroTrendChart : Control
    {
        private readonly List<TrendChannel> _channels = new List<TrendChannel>();
        private float? _upperLimit = 85f;
        private float? _lowerLimit = 15f;
        private string _title = "Live Sensor Telemetry";
        private int _gridDivisionsX = 6;
        private int _gridDivisionsY = 4;
        private bool _showFill = true;

        public ZeroTrendChart()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);

            Size = new Size(380, 160);
            BackColor = Color.FromArgb(15, 23, 42); // Industrial Slate Dark
            Font = new Font("Segoe UI", 8.5f);

            // Default demo channels
            _channels.Add(new TrendChannel("Pressure", "Bar", Color.FromArgb(56, 189, 248), 0, 100));
            _channels.Add(new TrendChannel("Oven Temp", "°C", Color.FromArgb(245, 158, 11), 0, 300));
        }

        [Category("Appearance")]
        [DefaultValue("Live Sensor Telemetry")]
        public string Title
        {
            get => _title;
            set { _title = value ?? ""; Invalidate(); }
        }

        [Category("Limits")]
        [DefaultValue(85f)]
        public float? UpperLimit
        {
            get => _upperLimit;
            set { _upperLimit = value; Invalidate(); }
        }

        [Category("Limits")]
        [DefaultValue(15f)]
        public float? LowerLimit
        {
            get => _lowerLimit;
            set { _lowerLimit = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        public bool ShowFill
        {
            get => _showFill;
            set { _showFill = value; Invalidate(); }
        }

        [Browsable(false)]
        public List<TrendChannel> Channels => _channels;

        public void AddPoint(int channelIndex, float value)
        {
            if (channelIndex >= 0 && channelIndex < _channels.Count)
            {
                _channels[channelIndex].Add(value);
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = Width;
            int h = Height;

            // 1. Dark Enclosure Background
            using (var bgBrush = new SolidBrush(BackColor))
            {
                g.FillRectangle(bgBrush, 0, 0, w, h);
            }
            using (var borderPen = new Pen(Color.FromArgb(51, 65, 85), 1f))
            {
                g.DrawRectangle(borderPen, 0, 0, w - 1, h - 1);
            }

            // 2. Header Area: Title & Channel Legend
            int headerH = 26;
            using (var titleFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(241, 245, 249)))
            {
                g.DrawString(_title, titleFont, titleBrush, 8, 6);
            }

            int legendX = w - 10;
            using (var legendFont = new Font("Segoe UI", 8f, FontStyle.Bold))
            {
                for (int c = _channels.Count - 1; c >= 0; c--)
                {
                    var ch = _channels[c];
                    string txt = $"{ch.Name}: {ch.LatestValue:F1} {ch.Unit}";
                    var sz = g.MeasureString(txt, legendFont);
                    legendX -= (int)sz.Width + 14;

                    // Dot
                    using var dotBrush = new SolidBrush(ch.Color);
                    g.FillEllipse(dotBrush, legendX - 10, 10, 6, 6);

                    // Text
                    using var txtBrush = new SolidBrush(Color.FromArgb(203, 213, 225));
                    g.DrawString(txt, legendFont, txtBrush, legendX, 6);

                    legendX -= 12;
                }
            }

            // 3. Plot Area Dimensions
            int plotX = 36;
            int plotY = headerH;
            int plotW = w - plotX - 10;
            int plotH = h - plotY - 18;

            if (plotW <= 10 || plotH <= 10) return;

            // Plot Background
            using (var plotBrush = new SolidBrush(Color.FromArgb(10, 15, 30)))
            {
                g.FillRectangle(plotBrush, plotX, plotY, plotW, plotH);
            }

            // 4. Grid Lines (Horizontal & Vertical)
            using (var gridPen = new Pen(Color.FromArgb(30, 41, 59), 1f) { DashStyle = DashStyle.Dot })
            {
                for (int i = 0; i <= _gridDivisionsY; i++)
                {
                    int gy = plotY + (plotH * i / _gridDivisionsY);
                    g.DrawLine(gridPen, plotX, gy, plotX + plotW, gy);
                }
                for (int j = 0; j <= _gridDivisionsX; j++)
                {
                    int gx = plotX + (plotW * j / _gridDivisionsX);
                    g.DrawLine(gridPen, gx, plotY, gx, plotY + plotH);
                }
            }

            // 5. Upper and Lower Limit Lines (USL / LSL)
            if (_channels.Count > 0)
            {
                var primary = _channels[0];
                float range = primary.MaxValue - primary.MinValue;
                if (range > 0)
                {
                    if (_upperLimit.HasValue && _upperLimit.Value <= primary.MaxValue)
                    {
                        float yNorm = 1f - ((_upperLimit.Value - primary.MinValue) / range);
                        int uy = plotY + (int)(plotH * yNorm);
                        using var uslPen = new Pen(Color.FromArgb(239, 68, 68), 1f) { DashStyle = DashStyle.Dash };
                        g.DrawLine(uslPen, plotX, uy, plotX + plotW, uy);

                        using var lFont = new Font("Segoe UI", 7f);
                        using var lBrush = new SolidBrush(Color.FromArgb(248, 113, 113));
                        g.DrawString($"USL {_upperLimit:F0}", lFont, lBrush, plotX + 4, uy - 12);
                    }

                    if (_lowerLimit.HasValue && _lowerLimit.Value >= primary.MinValue)
                    {
                        float yNorm = 1f - ((_lowerLimit.Value - primary.MinValue) / range);
                        int ly = plotY + (int)(plotH * yNorm);
                        using var lslPen = new Pen(Color.FromArgb(245, 158, 11), 1f) { DashStyle = DashStyle.Dash };
                        g.DrawLine(lslPen, plotX, ly, plotX + plotW, ly);

                        using var lFont = new Font("Segoe UI", 7f);
                        using var lBrush = new SolidBrush(Color.FromArgb(251, 191, 36));
                        g.DrawString($"LSL {_lowerLimit:F0}", lFont, lBrush, plotX + 4, ly + 2);
                    }
                }
            }

            // 6. Draw Channel Curves (Stack or Overlay)
            var clipRect = new Rectangle(plotX, plotY, plotW, plotH);
            var prevClip = g.Clip;
            g.SetClip(clipRect);

            for (int c = 0; c < _channels.Count; c++)
            {
                var ch = _channels[c];
                if (ch.Count < 2) continue;

                float range = ch.MaxValue - ch.MinValue;
                if (range <= 0) range = 1f;

                PointF[] points = new PointF[ch.Count];
                float stepX = (float)plotW / (ch.Buffer.Length - 1);

                int startDrawX = plotX + (int)((ch.Buffer.Length - ch.Count) * stepX);

                for (int i = 0; i < ch.Count; i++)
                {
                    float val = ch.GetPoint(i);
                    float normY = 1f - ((val - ch.MinValue) / range);
                    normY = Math.Max(0f, Math.Min(1f, normY));

                    float px = startDrawX + (i * stepX);
                    float py = plotY + (normY * plotH);
                    points[i] = new PointF(px, py);
                }

                // Fill underneath primary channel
                if (_showFill && c == 0 && points.Length > 2)
                {
                    using var path = new GraphicsPath();
                    path.AddLine(points[0].X, plotY + plotH, points[0].X, points[0].Y);
                    path.AddCurve(points);
                    path.AddLine(points[points.Length - 1].X, plotY + plotH, points[0].X, plotY + plotH);

                    using var fillBrush = new LinearGradientBrush(
                        new Point(plotX, plotY),
                        new Point(plotX, plotY + plotH),
                        Color.FromArgb(60, ch.Color),
                        Color.FromArgb(5, ch.Color));
                    g.FillPath(fillBrush, path);
                }

                // Draw curve line
                using var pen = new Pen(ch.Color, 2f);
                g.DrawCurve(pen, points);
            }

            g.Clip = prevClip;

            // 7. Y-Axis Scale Labels (Primary Channel)
            if (_channels.Count > 0)
            {
                var p = _channels[0];
                using var axisFont = new Font("Segoe UI", 7.5f);
                using var axisBrush = new SolidBrush(Color.FromArgb(148, 163, 184));
                g.DrawString($"{p.MaxValue:F0}", axisFont, axisBrush, 4, plotY - 2);
                g.DrawString($"{(p.MaxValue + p.MinValue) / 2:F0}", axisFont, axisBrush, 4, plotY + (plotH / 2) - 6);
                g.DrawString($"{p.MinValue:F0}", axisFont, axisBrush, 4, plotY + plotH - 10);
            }
        }
    }
}
