using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZeroUI.WinForms.Controls
{
    public enum ZeroToastType
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Lightweight non-blocking floating toast notification component for ZeroUI that slides in and auto-dismisses.
    /// </summary>
    public sealed class ZeroToast : Form

    {
        private readonly Form _ownerForm;
        private readonly ZeroToastType _type;
        private readonly string _message;
        private readonly Timer _stayTimer;
        private readonly Timer _fadeTimer;
        private bool _isFadingOut = false;

        private ZeroToast(Form owner, string message, ZeroToastType type, int durationMs = 3000)
        {
            _ownerForm = owner;
            _message = message;
            _type = type;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            Opacity = 0.0;
            Size = new Size(340, 56);

            _fadeTimer = new Timer { Interval = 20 };
            _fadeTimer.Tick += FadeTimer_Tick;

            _stayTimer = new Timer { Interval = Math.Max(1000, durationMs) };
            _stayTimer.Tick += (s, e) =>
            {
                _stayTimer.Stop();
                _isFadingOut = true;
                _fadeTimer.Start();
            };

            Click += (s, e) => CloseToast();

        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }

        public static void Show(Form parent, string message, ZeroToastType type = ZeroToastType.Info, int durationMs = 3000)
        {
            if (parent == null || parent.IsDisposed) return;

            if (parent.InvokeRequired)
            {
                parent.BeginInvoke(new Action(() => Show(parent, message, type, durationMs)));
                return;
            }

            var toast = new ZeroToast(parent, message, type, durationMs);
            toast.PositionToast();
            toast.Show(parent);
            toast._fadeTimer.Start();
        }

        public static void Success(Form parent, string message) => Show(parent, message, ZeroToastType.Success);
        public static void Info(Form parent, string message) => Show(parent, message, ZeroToastType.Info);
        public static void Warning(Form parent, string message) => Show(parent, message, ZeroToastType.Warning);
        public static void Error(Form parent, string message) => Show(parent, message, ZeroToastType.Error);

        private void PositionToast()
        {
            Point parentLocation = _ownerForm.PointToScreen(Point.Empty);
            int targetX = parentLocation.X + _ownerForm.ClientSize.Width - Width - 24;
            int targetY = parentLocation.Y + 24;
            Location = new Point(targetX, targetY);
        }

        private void FadeTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isFadingOut)
            {
                Opacity += 0.12;
                if (Opacity >= 0.98)
                {
                    Opacity = 1.0;
                    _fadeTimer.Stop();
                    _stayTimer.Start();
                }
            }
            else
            {
                Opacity -= 0.12;
                if (Opacity <= 0.05)
                {
                    _fadeTimer.Stop();
                    Close();
                    Dispose();
                }
            }
        }

        private void CloseToast()
        {
            _stayTimer.Stop();
            _isFadingOut = true;
            _fadeTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // 1. Draw Card Background & Shadow
            using (var path = CreateRoundedRectangle(rect, 8))
            {
                using var bgBrush = new SolidBrush(Color.White);
                g.FillPath(bgBrush, path);

                using var borderPen = new Pen(Color.FromArgb(229, 231, 235), 1.2f);
                g.DrawPath(borderPen, path);
            }

            // 2. Draw Type Icon / Glyph
            var (iconChar, iconColor) = _type switch
            {
                ZeroToastType.Success => ("✔", Color.FromArgb(82, 196, 26)),    // Emerald Green
                ZeroToastType.Warning => ("⚠", Color.FromArgb(250, 173, 20)),   // Amber Gold
                ZeroToastType.Error => ("✖", Color.FromArgb(255, 77, 79)),      // Ruby Red
                _ => ("ℹ", Color.FromArgb(22, 119, 255))                        // Cobalt Blue
            };


            Rectangle iconRect = new Rectangle(14, 0, 26, Height);
            TextRenderer.DrawText(
                g,
                iconChar,
                new Font("Segoe UI", 13f, FontStyle.Bold),
                iconRect,
                iconColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            // 3. Draw Message Text
            Rectangle textRect = new Rectangle(44, 0, Width - 60, Height);
            TextRenderer.DrawText(
                g,
                _message,
                new Font("Segoe UI", 9.5f, FontStyle.Regular),
                textRect,
                Color.FromArgb(31, 41, 55),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stayTimer.Dispose();
                _fadeTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
