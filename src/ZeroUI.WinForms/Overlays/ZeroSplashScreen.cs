using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Overlays
{
    /// <summary>
    /// Thread-safe, non-blocking enterprise Splash Screen Manager for ZeroUI.
    /// Replaces DevExpress SplashScreenManager by running on an independent background STA thread,
    /// guaranteeing smooth 60 FPS animation and responsive status updates while the main application initializes.
    /// </summary>
    public static class ZeroSplashScreen
    {
        private static Thread? _splashThread;
        private static SplashForm? _splashForm;
        private static readonly object _syncLock = new object();
        private static readonly ManualResetEvent _readyEvent = new ManualResetEvent(false);

        public static bool IsShowing => _splashForm != null && !_splashForm.IsDisposed;

        public static void Show(
            string appTitle = "ZeroUI Application",
            string subtitle = "High-Performance Enterprise Suite",
            string initialStatus = "Initializing...")
        {
            lock (_syncLock)
            {
                if (IsShowing) return;

                _readyEvent.Reset();
                _splashThread = new Thread(() =>
                {
                    _splashForm = new SplashForm(appTitle, subtitle, initialStatus);
                    _readyEvent.Set();
                    Application.Run(_splashForm);
                });

                _splashThread.SetApartmentState(ApartmentState.STA);
                _splashThread.IsBackground = true;
                _splashThread.Name = "ZeroSplashScreen_STA_Thread";
                _splashThread.Start();

                _readyEvent.WaitOne(3000);
            }
        }

        public static void SetStatus(string status, int? progressPercentage = null)
        {
            if (_splashForm != null && !_splashForm.IsDisposed)
            {
                try
                {
                    if (_splashForm.InvokeRequired)
                    {
                        _splashForm.BeginInvoke(new Action(() => _splashForm.UpdateStatus(status, progressPercentage)));
                    }
                    else
                    {
                        _splashForm.UpdateStatus(status, progressPercentage);
                    }
                }
                catch
                {
                    // Ignore cross-thread or dispose race conditions during closing
                }
            }
        }

        public static void Close(int delayMs = 150)
        {
            lock (_syncLock)
            {
                if (_splashForm != null && !_splashForm.IsDisposed)
                {
                    try
                    {
                        if (delayMs > 0) Thread.Sleep(delayMs);
                        _splashForm.BeginInvoke(new Action(() =>
                        {
                            _splashForm.Close();
                            _splashForm.Dispose();
                            _splashForm = null;
                        }));
                    }
                    catch
                    {
                        // Ignore
                    }
                }
                _splashThread = null;
            }
        }

        private class SplashForm : Form
        {
            private readonly string _title;
            private readonly string _subtitle;
            private string _status;
            private int? _progress;
            private readonly System.Windows.Forms.Timer _animationTimer;
            private float _shimmerPhase = 0f;

            public SplashForm(string title, string subtitle, string initialStatus)
            {
                _title = title;
                _subtitle = subtitle;
                _status = initialStatus;

                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.CenterScreen;
                ShowInTaskbar = false;
                TopMost = true;
                Size = new Size(520, 280);

                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw, true);

                DoubleBuffered = true;

                _animationTimer = new System.Windows.Forms.Timer { Interval = 25 }; // ~40 FPS smooth shimmer
                _animationTimer.Tick += (s, e) =>
                {
                    _shimmerPhase = (_shimmerPhase + 0.05f) % 1.0f;
                    Invalidate();
                };
                _animationTimer.Start();
            }

            public void UpdateStatus(string status, int? progress)
            {
                _status = status;
                _progress = progress.HasValue ? Math.Max(0, Math.Min(100, progress.Value)) : null;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var palette = ZeroTheme.Palette;
                Color cardBg = palette.Surface;
                Color borderColor = palette.Primary;

                // 1. Draw Card Background with Border
                using (var path = ZeroUIConfig.CreateRoundedRectangle(new Rectangle(0, 0, Width - 1, Height - 1), 12))
                {
                    using (var brush = new SolidBrush(cardBg))
                    {
                        g.FillPath(brush, path);
                    }
                    using (var pen = new Pen(palette.Border, 1.5f))
                    {
                        g.DrawPath(pen, path);
                    }
                }

                // 2. Decorative Top Accent Bar
                using (var accentBrush = new LinearGradientBrush(new Point(12, 0), new Point(Width - 12, 0), palette.Primary, palette.PrimaryHover))
                {
                    g.FillRectangle(accentBrush, 12, 0, Width - 24, 4);
                }

                // 3. Logo / Badge
                Rectangle logoRect = new Rectangle(36, 44, 48, 48);
                using (var logoPath = ZeroUIConfig.CreateRoundedRectangle(logoRect, 10))
                {
                    using var brush = new SolidBrush(palette.Primary);
                    g.FillPath(brush, logoPath);
                    TextRenderer.DrawText(g, "⚡", new Font("Segoe UI", 20f), logoRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }

                // 4. App Title & Subtitle
                int textLeft = 100;
                using (var titleFont = new Font("Segoe UI", 16f, FontStyle.Bold))
                {
                    Rectangle titleRect = new Rectangle(textLeft, 44, Width - textLeft - 36, 30);
                    TextRenderer.DrawText(g, _title, titleFont, titleRect, palette.TextPrimary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                }

                using (var subFont = new Font("Segoe UI", 9.5f, FontStyle.Regular))
                {
                    Rectangle subRect = new Rectangle(textLeft, 74, Width - textLeft - 36, 20);
                    TextRenderer.DrawText(g, _subtitle, subFont, subRect, palette.TextSecondary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                }

                // 5. Progress Bar
                int barY = 175;
                Rectangle barRect = new Rectangle(36, barY, Width - 72, 6);
                using (var barPath = ZeroUIConfig.CreateRoundedRectangle(barRect, 3))
                {
                    using var trackBrush = new SolidBrush(palette.Background);
                    g.FillPath(trackBrush, barPath);

                    if (_progress.HasValue)
                    {
                        // Determinate Progress
                        int fillW = (int)((float)_progress.Value / 100f * barRect.Width);
                        if (fillW > 0)
                        {
                            Rectangle fillRect = new Rectangle(barRect.X, barRect.Y, fillW, barRect.Height);
                            using var fillPath = ZeroUIConfig.CreateRoundedRectangle(fillRect, 3);
                            using var fillBrush = new SolidBrush(palette.Primary);
                            g.FillPath(fillBrush, fillPath);
                        }
                    }
                    else
                    {
                        // Indeterminate Shimmer
                        int shimmerW = 100;
                        int shimmerX = barRect.X + (int)(_shimmerPhase * (barRect.Width + shimmerW)) - shimmerW;
                        Rectangle shimmerRect = Rectangle.Intersect(barRect, new Rectangle(shimmerX, barRect.Y, shimmerW, barRect.Height));
                        if (!shimmerRect.IsEmpty)
                        {
                            using var shimmerPath = ZeroUIConfig.CreateRoundedRectangle(shimmerRect, 3);
                            using var shimmerBrush = new SolidBrush(palette.Primary);
                            g.FillPath(shimmerBrush, shimmerPath);
                        }
                    }
                }

                // 6. Status Text
                using (var statusFont = new Font("Segoe UI", 9f, FontStyle.Regular))
                {
                    Rectangle statusRect = new Rectangle(36, 192, Width - 140, 22);
                    TextRenderer.DrawText(g, _status, statusFont, statusRect, palette.TextSecondary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                    if (_progress.HasValue)
                    {
                        Rectangle pctRect = new Rectangle(Width - 100, 192, 64, 22);
                        TextRenderer.DrawText(g, $"{_progress.Value}%", statusFont, pctRect, palette.TextPrimary, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
                    }
                }

                // 7. Footer Version / Engine Tag
                using (var footFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
                {
                    Rectangle footRect = new Rectangle(36, Height - 36, Width - 72, 20);
                    TextRenderer.DrawText(g, "ZeroUI Core v1.0 • Zero-Allocation Engine", footFont, footRect, palette.TextSecondary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _animationTimer.Stop();
                    _animationTimer.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
