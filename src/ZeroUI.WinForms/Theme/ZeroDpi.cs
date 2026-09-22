using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ZeroUI.WinForms.Theme
{
    /// <summary>
    /// High-DPI Per-Monitor V2 dynamic scaling engine for ZeroUI WinForms controls.
    /// Provides unified scale factor resolution, coordinate scaling, and Per-Monitor V2 awareness.
    /// </summary>
    public static class ZeroDpi
    {
        /// <summary>
        /// Baseline display DPI standard for 100% scale (96 DPI).
        /// </summary>
        public const float BaselineDpi = 96.0f;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetDpiForWindow(IntPtr hWnd);

        /// <summary>
        /// Resolves the effective device DPI for the specified control.
        /// Consumes native Win32 Per-Monitor V2 metrics when handle is created,
        /// falling back to GDI display device context or 96 DPI baseline.
        /// </summary>
        public static int GetDeviceDpi(Control? control)
        {
            if (control != null && control.IsHandleCreated)
            {
#if NET8_0_OR_GREATER
                return control.DeviceDpi;
#else
                try
                {
                    var prop = typeof(Control).GetProperty("DeviceDpi");
                    if (prop != null)
                    {
                        var val = prop.GetValue(control, null);
                        if (val is int dpi && dpi > 0) return dpi;
                    }
                }
                catch { }

                try
                {
                    if (Environment.OSVersion.Version.Major >= 10)
                    {
                        int winDpi = GetDpiForWindow(control.Handle);
                        if (winDpi > 0) return winDpi;
                    }
                }
                catch { }

                try
                {
                    using (var g = control.CreateGraphics())
                    {
                        return (int)Math.Round(g.DpiX);
                    }
                }
                catch { }
#endif
            }

            return (int)BaselineDpi;
        }

        /// <summary>
        /// Computes the relative DPI scale factor (e.g. 1.0 = 100%, 1.25 = 125%, 1.5 = 150%, 2.0 = 200%).
        /// </summary>
        public static float GetScaleFactor(Control? control)
        {
            int dpi = GetDeviceDpi(control);
            return dpi / BaselineDpi;
        }

        /// <summary>
        /// Scales an integer pixel measurement by the given DPI scale factor.
        /// </summary>
        public static int Scale(int value, float factor)
        {
            if (Math.Abs(factor - 1.0f) < 0.001f || value == 0) return value;
            return (int)Math.Round(value * factor);
        }

        /// <summary>
        /// Scales an integer pixel measurement according to the control's effective DPI.
        /// </summary>
        public static int Scale(int value, Control? control)
        {
            return Scale(value, GetScaleFactor(control));
        }

        /// <summary>
        /// Scales a Size structure by the specified DPI factor.
        /// </summary>
        public static Size Scale(Size size, float factor)
        {
            return new Size(Scale(size.Width, factor), Scale(size.Height, factor));
        }

        /// <summary>
        /// Scales a SizeF structure by the specified DPI factor.
        /// </summary>
        public static SizeF Scale(SizeF size, float factor)
        {
            return new SizeF(size.Width * factor, size.Height * factor);
        }

        /// <summary>
        /// Scales a Point structure by the specified DPI factor.
        /// </summary>
        public static Point Scale(Point pt, float factor)
        {
            return new Point(Scale(pt.X, factor), Scale(pt.Y, factor));
        }

        /// <summary>
        /// Scales a Rectangle structure by the specified DPI factor.
        /// </summary>
        public static Rectangle Scale(Rectangle rect, float factor)
        {
            return new Rectangle(
                Scale(rect.X, factor),
                Scale(rect.Y, factor),
                Scale(rect.Width, factor),
                Scale(rect.Height, factor));
        }

        /// <summary>
        /// Scales a Padding structure by the specified DPI factor.
        /// </summary>
        public static Padding Scale(Padding padding, float factor)
        {
            return new Padding(
                Scale(padding.Left, factor),
                Scale(padding.Top, factor),
                Scale(padding.Right, factor),
                Scale(padding.Bottom, factor));
        }
    }
}
