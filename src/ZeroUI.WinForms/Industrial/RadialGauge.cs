using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ZeroUI.Core.Scada;
using ZeroUI.WinForms.Base;
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
    public partial class RadialGauge : ControlBase, IScadaBindable
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
        private string _valueFormat = "0.#";
        private bool _useTabularReadout = true;
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

        [Category("ZeroUI - Appearance")]
        [Description("Custom format string for the digital readout. Defaults to '0.#'. Callers can specify fixed-precision formats such as '0.0' or '0.00'.")]
        [DefaultValue("0.#")]
        public string ValueFormat
        {
            get => _valueFormat;
            set { _valueFormat = value ?? "0.#"; Invalidate(); }
        }

        [Category("ZeroUI - Appearance")]
        [Description("Enables tabular digit slot pitch to eliminate horizontal jumping/jitter.")]
        [DefaultValue(true)]
        public bool UseTabularReadout
        {
            get => _useTabularReadout;
            set { _useTabularReadout = value; Invalidate(); }
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
