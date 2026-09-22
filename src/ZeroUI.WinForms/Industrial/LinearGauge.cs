using System;
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
    /// Industrial Linear Level and Pressure Gauge for SCADA telemetry.
    /// Features multi-zone scale thresholds (Normal, Warning, Critical), graduations with tick marks,
    /// dynamic High-DPI Per-Monitor V2 scaling, and direct binding to SCADA telemetry tags via <see cref="IScadaBindable"/>.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Industrial & SCADA")]
    [DefaultProperty("Value")]
    [Description("Industrial Linear Level and Pressure Gauge for SCADA telemetry")]
    [ToolboxBitmap(typeof(ZeroIcons), "LinearGauge.bmp")]
    public partial class LinearGauge : ControlBase, IScadaBindable
    {
        private float _value = 65f;
        private float _minimum = 0f;
        private float _maximum = 100f;
        private string _title = "Pressure";
        private string _unit = "Bar";
        private string _valueFormat = "0.#";
        private bool _useTabularReadout = true;
        private float _warningThreshold = 75f;
        private float _criticalThreshold = 90f;
        private string? _boundTagPath;

        public LinearGauge()
        {
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

        [Category("Appearance")]
        [Description("Custom format string for the telemetry value. Defaults to '0.#'. Callers can specify fixed-precision formats such as '0.0' or '0.00'.")]
        [DefaultValue("0.#")]
        public string ValueFormat
        {
            get => _valueFormat;
            set { _valueFormat = value ?? "0.#"; Invalidate(); }
        }

        [Category("Appearance")]
        [Description("Enables tabular digit slot pitch to eliminate horizontal jumping/jitter.")]
        [DefaultValue(true)]
        public bool UseTabularReadout
        {
            get => _useTabularReadout;
            set { _useTabularReadout = value; Invalidate(); }
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

        #region IScadaBindable

        [Category("SCADA")]
        public string? BoundTagPath
        {
            get => _boundTagPath;
            set => _boundTagPath = value;
        }

        public void OnTagValueChanged(IScadaTag tag)
        {
            if (tag != null)
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => OnTagValueChanged(tag)));
                    return;
                }

                Value = tag.GetValue<float>();
            }
        }

        #endregion
    }

    /// <summary>
    /// Legacy alias for LinearGauge.
    /// Preserved for backward compatibility.
    /// </summary>
    [Obsolete("ZeroLinearGauge is deprecated. Please use LinearGauge instead.")]
    [ToolboxItem(false)]
    public class ZeroLinearGauge : LinearGauge
    {
    }
}
