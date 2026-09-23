using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ZeroGraphics.DirectX.Controls;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Containers
{
    /// <summary>
    /// Hardware-accelerated GPU UI card for industrial dashboards, SCADA machine panels, and equipment monitors.
    /// Rendered via Direct3D 11 SDF shaders from ZeroGraphics for subpixel anti-aliased rounded corners,
    /// real-time Gaussian drop shadow, and neon glow effects with 0% CPU consumption.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Industrial & SCADA")]
    [Description("Hardware-accelerated Direct3D 11 card container with analytical drop shadows and neon bloom")]
    public class HardwareCard : ZeroDirectXCanvas
    {
        private bool _alarmState;

        [Category("ZeroUI - Industrial")]
        [DefaultValue(false)]
        [Description("Enables or disables visual alarm neon bloom glow on the card.")]
        public bool AlarmState
        {
            get => _alarmState;
            set
            {
                _alarmState = value;
                if (_alarmState)
                {
                    GlowIntensity = 1.2f;
                    GlowColor = Color.FromArgb(239, 68, 68); // Neon Red alarm
                    CardBorderColor = Color.FromArgb(220, 38, 38);
                }
                else
                {
                    GlowIntensity = 0f;
                    GlowColor = Color.FromArgb(0, 229, 255);
                    CardBorderColor = Color.FromArgb(42, 51, 71);
                }
                Invalidate();
            }
        }

        public HardwareCard()
        {
            // Default to ZeroUI Obsidian Dark palette
            BackColor = Color.FromArgb(13, 17, 23);
            CardColor = Color.FromArgb(22, 27, 38);
            CardBorderColor = Color.FromArgb(42, 51, 71);
            ShadowColor = Color.FromArgb(140, 0, 0, 0);

            CornerRadius = 12f;
            Elevation = 8f;
            BlurRadius = 16f;
            BorderWidth = 1f;
        }
    }

    /// <summary>
    /// Legacy alias for <see cref="HardwareCard"/>.
    /// Preserved for backward compatibility.
    /// </summary>
    [Obsolete("ZeroHardwareCard is deprecated. Please use HardwareCard instead.")]
    [ToolboxItem(false)]
    public class ZeroHardwareCard : HardwareCard
    {
    }
}
