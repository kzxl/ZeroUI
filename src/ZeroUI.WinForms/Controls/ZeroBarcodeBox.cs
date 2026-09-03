using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZeroUI.WinForms.Controls
{
    public class BarcodeScannedEventArgs : EventArgs
    {
        public string Barcode { get; }
        public DateTime Timestamp { get; }

        public BarcodeScannedEventArgs(string barcode)
        {
            Barcode = barcode;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Specialized high-speed barcode and QR code scanner input control for factory workstations.
    /// </summary>
    public class ZeroBarcodeBox : Control
    {
        private readonly TextBox _textBox;
        private string _placeholder = "Scan or type barcode...";
        private bool _clearOnSubmit = true;
        private bool _selectAllOnFocus = true;
        private bool _isFocused = false;
        private int _flashTicks = 0;
        private readonly Timer _flashTimer;

        public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

        public ZeroBarcodeBox()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);

            Size = new Size(280, 36);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            _textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = Font,
                Location = new Point(34, 9),
                Width = Width - 44,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            _textBox.KeyDown += TextBox_KeyDown;
            _textBox.Enter += (s, e) =>
            {
                _isFocused = true;
                if (_selectAllOnFocus) _textBox.SelectAll();
                Invalidate();
            };
            _textBox.Leave += (s, e) =>
            {
                _isFocused = false;
                Invalidate();
            };

            _flashTimer = new Timer { Interval = 50 };
            _flashTimer.Tick += (s, e) =>
            {
                _flashTicks--;
                if (_flashTicks <= 0)
                {
                    _flashTimer.Stop();
                }
                Invalidate();
            };

            Controls.Add(_textBox);
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool ClearOnSubmit
        {
            get => _clearOnSubmit;
            set => _clearOnSubmit = value;
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool SelectAllOnFocus
        {
            get => _selectAllOnFocus;
            set => _selectAllOnFocus = value;
        }

        [Category("Appearance")]
        [DefaultValue("Scan or type barcode...")]
        public string PlaceholderText
        {
            get => _placeholder;
            set { _placeholder = value; Invalidate(); }
        }

        public string BarcodeText
        {
            get => _textBox.Text;
            set => _textBox.Text = value;
        }

        public void FocusInput()
        {
            _textBox.Focus();
            _textBox.SelectAll();
        }

        private void TextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                string code = _textBox.Text.Trim();
                if (!string.IsNullOrEmpty(code))
                {
                    // Trigger visual green pulse
                    _flashTicks = 6;
                    _flashTimer.Start();

                    BarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs(code));

                    if (_clearOnSubmit)
                    {
                        _textBox.Clear();
                    }
                    else
                    {
                        _textBox.SelectAll();
                    }
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // 1. Determine Border & Fill Color based on focus and flash
            Color borderColor;
            Color bgColor = Color.White;

            if (_flashTicks > 0)
            {
                borderColor = Color.FromArgb(16, 185, 129); // Flash Emerald
                bgColor = Color.FromArgb(246, 255, 237);
            }
            else if (_isFocused)
            {
                borderColor = Color.FromArgb(79, 70, 229); // Indigo active
            }
            else
            {
                borderColor = Color.FromArgb(209, 213, 219); // Neutral gray
            }

            _textBox.BackColor = bgColor;

            using (var path = CreateRoundedRectangle(rect, 6))
            {
                using var bgBrush = new SolidBrush(bgColor);
                g.FillPath(bgBrush, path);

                using var pen = new Pen(borderColor, _isFocused || _flashTicks > 0 ? 1.8f : 1f);
                g.DrawPath(pen, path);
            }

            // 2. Draw Barcode Icon glyph (||||)
            Rectangle iconRect = new Rectangle(10, (Height - 16) / 2, 18, 16);
            TextRenderer.DrawText(g, "❚❙❘", new Font("Segoe UI", 10f, FontStyle.Bold), iconRect, _isFocused ? Color.FromArgb(79, 70, 229) : Color.FromArgb(156, 163, 175), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            // 3. Draw Placeholder when empty
            if (string.IsNullOrEmpty(_textBox.Text) && !_isFocused)
            {
                Rectangle phRect = new Rectangle(36, 0, Width - 44, Height);
                TextRenderer.DrawText(g, _placeholder, Font, phRect, Color.FromArgb(156, 163, 175), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _textBox.Focus();
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0 || rect.Width <= 0 || rect.Height <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

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
                _flashTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
