using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using ZeroUI.Core.Scada;
using ZeroUI.WinForms.Icons;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Industrial
{
    /// <summary>
    /// High-precision circular industrial instrumentation dial gauge for SCADA and MES monitoring.
    /// Supports 180° / 270° sweep scales, multi-zone colored threshold bands, major/minor tick marks,
    /// inertial needle damping, and direct binding to SCADA telemetry tags.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Industrial & SCADA")]
    [DefaultProperty("Value")]
    [DefaultEvent("ValueChanged")]
    [Description("High-precision circular industrial dial gauge with needle pointer and threshold zones")]
    [ToolboxBitmap(typeof(ZeroIcons), "RadialGauge.bmp")]
    public class RadialGauge : Control, IScadaBindable
    {
        private double _minimum = 0.0;
        private double _maximum = 100.0;
        private double _value = 65.0;
        private double _targetValue = 65.0;
        private float _startAngle = 135f;
        private float _sweepAngle = 270f;
        private int _majorTicks = 10;
        private int _minorTicks = 4;
        private string _title = "Pressure";
        private string _unit = "bar";
        private bool _enableDamping = true;
        private double _dampingFactor = 0.25;
        private readonly Timer _animationTimer;
        private string? _boundTagPath;

        private readonly List<GaugeThresholdRange> _thresholds = new List<GaugeThresholdRange>();

        public event EventHandler? ValueChanged;

        #region Properties

        [Category("ZeroUI - Data")]
        [Description("Minimum scale value.")]
        [DefaultValue(0.0)]
        public double Minimum
        {
            get => _minimum;
            set
            {
                _minimum = value;
                if (_maximum <= _minimum) _maximum = _minimum + 1.0;
                Invalidate();
            }
        }

        [Category("ZeroUI - Data")]
        [Description("Maximum scale value.")]
        [DefaultValue(100.0)]
        public double Maximum
        {
            get => _maximum;
            set
            {
                _maximum = Math.Max(_minimum + 1.0, value);
                Invalidate();
            }
        }

        [Category("ZeroUI - Data")]
        [Description("Current indicated value.")]
        [DefaultValue(65.0)]
        public double Value
        {
            get => _value;
            set
            {
                double clamped = Math.Max(_minimum, Math.Min(_maximum, value));
                if (_enableDamping)
                {
                    _targetValue = clamped;
                    if (!_animationTimer.Enabled)
                        _animationTimer.Start();
                }
                else
                {
                    if (Math.Abs(_value - clamped) > 1e-4)
                    {
                        _value = clamped;
                        _targetValue = clamped;
                        Invalidate();
                        ValueChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        [Category("ZeroUI - Geometry")]
        [Description("Starting angle of the dial in degrees (0 = 3 o'clock, 90 = 6 o'clock, 135 = standard bottom-left).")]
        [DefaultValue(135f)]
        public float StartAngle
        {
            get => _startAngle;
            set { _startAngle = value; Invalidate(); }
        }

        [Category("ZeroUI - Geometry")]
        [Description("Total angular sweep of the scale (typically 180° for half-dial, 270° for industrial dial).")]
        [DefaultValue(270f)]
        public float SweepAngle
        {
            get => _sweepAngle;
            set { _sweepAngle = Math.Max(30f, Math.Min(360f, value)); Invalidate(); }
        }

        [Category("ZeroUI - Appearance")]
        [Description("Dial title or measurement name.")]
        [DefaultValue("Pressure")]
        public string Title
        {
            get => _title;
            set { _title = value ?? string.Empty; Invalidate(); }
        }

        [Category("ZeroUI - Appearance")]
        [Description("Engineering measurement unit string.")]
        [DefaultValue("bar")]
        public string Unit
        {
            get => _unit;
            set { _unit = value ?? string.Empty; Invalidate(); }
        }

        [Category("ZeroUI - Scale")]
        [Description("Number of major scale subdivisions.")]
        [DefaultValue(10)]
        public int MajorTicks
        {
            get => _majorTicks;
            set { _majorTicks = Math.Max(2, value); Invalidate(); }
        }

        [Category("ZeroUI - Scale")]
        [Description("Number of minor ticks between each major tick.")]
        [DefaultValue(4)]
        public int MinorTicks
        {
            get => _minorTicks;
            set { _minorTicks = Math.Max(0, value); Invalidate(); }
        }

        [Category("ZeroUI - Dynamics")]
        [Description("Enables smooth inertial needle motion damping.")]
        [DefaultValue(true)]
        public bool EnableDamping
        {
            get => _enableDamping;
            set => _enableDamping = value;
        }

        [Category("ZeroUI - Dynamics")]
        [Description("Inertial damping factor (0.05 to 0.5).")]
        [DefaultValue(0.25)]
        public double DampingFactor
        {
            get => _dampingFactor;
            set => _dampingFactor = Math.Max(0.01, Math.Min(1.0, value));
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<GaugeThresholdRange> Thresholds => _thresholds;

        #region IScadaBindable Implementation

        [Category("ZeroUI - SCADA")]
        [Description("Direct SCADA telemetry tag binding path (e.g. 'Line1.Boiler.Pressure').")]
        [DefaultValue(null)]
        public string? BoundTagPath
        {
            get => _boundTagPath;
            set => _boundTagPath = value;
        }

        public void OnTagValueChanged(IScadaTag tag)
        {
            if (tag != null)
            {
                Value = tag.GetValue<double>();
            }
        }

        #endregion

        #endregion

        public RadialGauge()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(180, 180);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            // Default industrial thresholds: 0-70 Normal (Green), 70-85 Warning (Amber), 85-100 Danger (Red)
            _thresholds.Add(new GaugeThresholdRange(0, 70, GaugeSeverity.Normal, 0xFF10B981u, "Normal"));
            _thresholds.Add(new GaugeThresholdRange(70, 85, GaugeSeverity.Warning, 0xFFF59E0Bu, "Warning"));
            _thresholds.Add(new GaugeThresholdRange(85, 100, GaugeSeverity.Critical, 0xFFEF4444u, "Danger"));

            _animationTimer = new Timer
            {
                Interval = 16 // 60 FPS update
            };
            _animationTimer.Tick += (s, e) =>
            {
                double next = GaugeMath.ApplyDamping(_value, _targetValue, _dampingFactor);
                if (Math.Abs(next - _value) > 1e-4)
                {
                    _value = next;
                    Invalidate();
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    _value = _targetValue;
                    _animationTimer.Stop();
                    Invalidate();
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            };

            ZeroTheme.ThemeChanged += (s, e) => Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            float w = Width;
            float h = Height;
            float cx = w / 2f;
            float cy = h / 2f;
            float radius = Math.Min(cx, cy) - 10f;
            if (radius < 20f) return;

            var colors = ZeroTheme.Colors;

            // 1. Dial Background Bezel
            using (var bezelBrush = new SolidBrush(ZeroTheme.IsDark ? Color.FromArgb(24, 26, 32) : Color.FromArgb(248, 249, 251)))
            {
                g.FillEllipse(bezelBrush, cx - radius, cy - radius, radius * 2f, radius * 2f);
            }

            using (var bezelPen = new Pen(colors.Border, 1.5f))
            {
                g.DrawEllipse(bezelPen, cx - radius, cy - radius, radius * 2f, radius * 2f);
            }

            float arcRadius = radius - 14f;
            var arcRect = new RectangleF(cx - arcRadius, cy - arcRadius, arcRadius * 2f, arcRadius * 2f);

            // 2. Base Track Arc
            using (var trackPen = new Pen(colors.Border, 6f))
            {
                trackPen.StartCap = LineCap.Round;
                trackPen.EndCap = LineCap.Round;
                g.DrawArc(trackPen, arcRect, _startAngle, _sweepAngle);
            }

            // 3. Colored Threshold Bands
            if (_thresholds.Count > 0)
            {
                for (int i = 0; i < _thresholds.Count; i++)
                {
                    var t = _thresholds[i];
                    float tStart = GaugeMath.ValueToAngle(t.From, _minimum, _maximum, _startAngle, _sweepAngle);
                    float tEnd = GaugeMath.ValueToAngle(t.To, _minimum, _maximum, _startAngle, _sweepAngle);
                    float tSweep = tEnd - tStart;

                    if (tSweep > 0.5f)
                    {
                        Color bandColor = Color.FromArgb((int)t.ArgbColor);
                        using (var bandPen = new Pen(bandColor, 6f))
                        {
                            g.DrawArc(bandPen, arcRect, tStart, tSweep);
                        }
                    }
                }
            }

            // 4. Tick Marks and Scale Numbers
            float tickOuterR = arcRadius - 8f;
            float tickMajorInnerR = tickOuterR - 8f;
            float tickMinorInnerR = tickOuterR - 4f;

            int totalSteps = _majorTicks * Math.Max(1, _minorTicks);
            using (var majorPen = new Pen(colors.TextSecondary, 1.5f))
            using (var minorPen = new Pen(Color.FromArgb(120, colors.TextSecondary), 1f))
            using (var labelFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            using (var labelBrush = new SolidBrush(colors.TextSecondary))
            {
                for (int step = 0; step <= totalSteps; step++)
                {
                    float ratio = (float)step / totalSteps;
                    float angleDeg = _startAngle + ratio * _sweepAngle;
                    double angleRad = angleDeg * Math.PI / 180.0;

                    float cos = (float)Math.Cos(angleRad);
                    float sin = (float)Math.Sin(angleRad);

                    bool isMajor = (step % Math.Max(1, _minorTicks) == 0);
                    float innerR = isMajor ? tickMajorInnerR : tickMinorInnerR;
                    Pen penToUse = isMajor ? majorPen : minorPen;

                    float x1 = cx + innerR * cos;
                    float y1 = cy + innerR * sin;
                    float x2 = cx + tickOuterR * cos;
                    float y2 = cy + tickOuterR * sin;

                    g.DrawLine(penToUse, x1, y1, x2, y2);

                    // Draw Scale Numbers on Major Ticks
                    if (isMajor && radius >= 50f)
                    {
                        double tickVal = _minimum + ratio * (_maximum - _minimum);
                        string numStr = tickVal >= 1000 ? $"{tickVal / 1000:0.#}k" : $"{tickVal:0}";
                        float numR = innerR - 10f;
                        float nx = cx + numR * cos;
                        float ny = cy + numR * sin;

                        SizeF sz = g.MeasureString(numStr, labelFont);
                        g.DrawString(numStr, labelFont, labelBrush, nx - sz.Width / 2f, ny - sz.Height / 2f);
                    }
                }
            }

            // 5. Gauge Title and Readout
            using (var titleFont = new Font("Segoe UI", 8f, FontStyle.Regular))
            using (var titleBrush = new SolidBrush(colors.TextSecondary))
            using (var valFont = new Font("Segoe UI", 11f, FontStyle.Bold))
            {
                // Title
                if (!string.IsNullOrEmpty(_title))
                {
                    SizeF tSz = g.MeasureString(_title, titleFont);
                    g.DrawString(_title, titleFont, titleBrush, cx - tSz.Width / 2f, cy + radius * 0.35f);
                }

                // Digital Value Readout
                string valStr = $"{_value:F1} {_unit}".Trim();
                GaugeSeverity sev = GaugeMath.EvaluateSeverity(_value, _thresholds);
                Color readoutColor = sev == GaugeSeverity.Critical
                    ? Color.FromArgb(239, 68, 68)
                    : (sev == GaugeSeverity.Warning ? Color.FromArgb(245, 158, 11) : colors.TextPrimary);

                using (var valBrush = new SolidBrush(readoutColor))
                {
                    SizeF vSz = g.MeasureString(valStr, valFont);
                    g.DrawString(valStr, valFont, valBrush, cx - vSz.Width / 2f, cy + radius * 0.52f);
                }
            }

            // 6. Inertial Needle Pointer
            float needleAngleDeg = GaugeMath.ValueToAngle(_value, _minimum, _maximum, _startAngle, _sweepAngle);
            double needleRad = needleAngleDeg * Math.PI / 180.0;
            float nCos = (float)Math.Cos(needleRad);
            float nSin = (float)Math.Sin(needleRad);
            float needleR = arcRadius - 4f;

            float tipX = cx + needleR * nCos;
            float tipY = cy + needleR * nSin;

            // Needle Polygon (Tapered triangle from base to tip)
            float baseHalfW = 3.5f;
            float perpCos = -nSin;
            float perpSin = nCos;

            PointF[] needlePoly = new PointF[]
            {
                new PointF(cx + perpCos * baseHalfW, cy + perpSin * baseHalfW),
                new PointF(tipX, tipY),
                new PointF(cx - perpCos * baseHalfW, cy - perpSin * baseHalfW),
                new PointF(cx - nCos * 8f, cy - nSin * 8f) // Counter-balance tail
            };

            Color needleColor = Color.FromArgb(239, 68, 68); // Signal Red needle
            using (var needleBrush = new SolidBrush(needleColor))
            {
                g.FillPolygon(needleBrush, needlePoly);
            }

            // 7. Metallic Center Pivot Cap
            float capR = 7f;
            using (var capBrush = new LinearGradientBrush(
                new RectangleF(cx - capR, cy - capR, capR * 2f, capR * 2f),
                Color.FromArgb(240, 240, 245),
                Color.FromArgb(120, 120, 130),
                45f))
            {
                g.FillEllipse(capBrush, cx - capR, cy - capR, capR * 2f, capR * 2f);
            }
            using (var capPen = new Pen(Color.FromArgb(90, 90, 100), 1f))
            {
                g.DrawEllipse(capPen, cx - capR, cy - capR, capR * 2f, capR * 2f);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Legacy alias for <see cref="RadialGauge"/>.
    /// Preserved for backward compatibility.
    /// </summary>
    [Obsolete("ZeroRadialGauge is deprecated. Please use RadialGauge instead.")]
    [ToolboxItem(false)]
    public class ZeroRadialGauge : RadialGauge
    {
    }
}
