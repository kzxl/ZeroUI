using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZeroUI.WinForms.Theme
{
    public enum ZeroCornerStyle
    {
        Rounded,    // Modern rounded corners (default)
        Sharp,      // Square 90-degree flat corners (Enterprise dense ERP style)
        Pill        // Highly rounded pill styling
    }

    /// <summary>
    /// Global application-wide configuration for ZeroUI (similar to DevExpress WindowsFormsSettings).
    /// Provides centralized management for global font, corner rounding style, border radius, and theme tokens.
    /// </summary>
    public static class ZeroUIConfig
    {
        private static Font _defaultFont = new Font("Segoe UI", 9.25f, FontStyle.Regular);
        private static ZeroCornerStyle _cornerStyle = ZeroCornerStyle.Rounded;
        private static int _defaultBorderRadius = 6;
        private static bool _enableAntiAliasing = true;

        public static event EventHandler? ConfigChanged;

        /// <summary>
        /// Global switch for GDI+ anti-aliased rendering.
        /// </summary>
        public static bool EnableAntiAliasing
        {
            get => _enableAntiAliasing;
            set
            {
                if (_enableAntiAliasing != value)
                {
                    _enableAntiAliasing = value;
                    NotifyConfigChanged();
                }
            }
        }

        /// <summary>
        /// Global default font used across all ZeroUI controls (like DevExpress WindowsFormsSettings.DefaultFont).
        /// </summary>
        public static Font DefaultFont
        {
            get => _defaultFont;
            set
            {
                if (value != null && _defaultFont != value)
                {
                    _defaultFont = value;
                    NotifyConfigChanged();
                }
            }
        }

        /// <summary>
        /// Global corner styling: Rounded (default), Sharp (flat 90° corners), or Pill.
        /// </summary>
        public static ZeroCornerStyle CornerStyle
        {
            get => _cornerStyle;
            set
            {
                if (_cornerStyle != value)
                {
                    _cornerStyle = value;
                    NotifyConfigChanged();
                }
            }
        }

        /// <summary>
        /// Quick toggle whether rounded corners are enabled globally.
        /// When false, controls render with sharp 90-degree corners with zero corner clipping.
        /// </summary>
        public static bool RoundedCorners
        {
            get => _cornerStyle != ZeroCornerStyle.Sharp;
            set
            {
                var target = value ? ZeroCornerStyle.Rounded : ZeroCornerStyle.Sharp;
                if (_cornerStyle != target)
                {
                    _cornerStyle = target;
                    NotifyConfigChanged();
                }
            }
        }

        /// <summary>
        /// Default border radius when CornerStyle is Rounded.
        /// </summary>
        public static int DefaultBorderRadius
        {
            get => _defaultBorderRadius;
            set
            {
                int val = Math.Max(0, value);
                if (_defaultBorderRadius != val)
                {
                    _defaultBorderRadius = val;
                    NotifyConfigChanged();
                }
            }
        }

        /// <summary>
        /// Calculates effective radius taking global CornerStyle into account.
        /// </summary>
        public static int GetEffectiveRadius(int localRadius)
        {
            if (_cornerStyle == ZeroCornerStyle.Sharp) return 0;
            if (localRadius <= 0) return 0;
            return _defaultBorderRadius > 0 ? _defaultBorderRadius : localRadius;
        }

        /// <summary>
        /// Helper to retrieve the parent container's solid background color,
        /// completely eliminating black triangular corner clipping artifacts.
        /// </summary>
        public static Color GetParentBackground(Control control, Color fallback)
        {
            if (control == null) return fallback;
            Control? parent = control.Parent;
            while (parent != null)
            {
                if (parent.BackColor != Color.Transparent && parent.BackColor.A == 255)
                {
                    return parent.BackColor;
                }
                parent = parent.Parent;
            }
            return fallback;
        }

        public static void NotifyConfigChanged()
        {
            ConfigChanged?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Creates a high-precision rounded rectangle GraphicsPath with perfectly balanced arcs on all 4 corners.
        /// </summary>
        public static GraphicsPath CreateRoundedRectangle(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0 || r.Width <= 0 || r.Height <= 0)
            {
                path.AddRectangle(r);
                return path;
            }

            int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Creates a floating-point high-precision rounded rectangle GraphicsPath.
        /// </summary>
        public static GraphicsPath CreateRoundedRectangleF(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0 || r.Width <= 0 || r.Height <= 0)
            {
                path.AddRectangle(r);
                return path;
            }

            float d = Math.Min(radius * 2f, Math.Min(r.Width, r.Height));
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
