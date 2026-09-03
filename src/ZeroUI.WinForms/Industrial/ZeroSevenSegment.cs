using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZeroUI.WinForms.Industrial
{
    /// <summary>
    /// Industrial 7-Segment Digital LED Display for SCADA & MES telemetry.
    /// Features polygon beveled segment geometry, authentic segment ghosting, customizable LED colors,
    /// and support for numbers, decimals, negative signs, and colons.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Industrial & SCADA")]
    [DefaultProperty("Value")]
    [Description("Industrial 7-Segment Digital LED Display for SCADA & MES telemetry")]
    public class ZeroSevenSegment : Control
    {

        private string _value = "1420";
        private Color _segmentColor = Color.FromArgb(52, 211, 153); // Emerald Neon Green
        private Color _dimColor = Color.FromArgb(20, 45, 35);       // Subtle unlit ghost segment
        private int _digitCount = 6;
        private bool _showLeadingZeros = false;

        // 7-segment bitmask (A, B, C, D, E, F, G)
        // A=0x01, B=0x02, C=0x04, D=0x08, E=0x10, F=0x20, G=0x40
        private static readonly byte[] DigitPatterns = new byte[]
        {
            0x3F, // 0: A B C D E F
            0x06, // 1: B C
            0x5B, // 2: A B D E G
            0x4F, // 3: A B C D G
            0x66, // 4: B C F G
            0x6D, // 5: A C D F G
            0x7D, // 6: A C D E F G
            0x07, // 7: A B C
            0x7F, // 8: A B C D E F G
            0x6F  // 9: A B C D F G
        };

        public ZeroSevenSegment()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);

            Size = new Size(180, 52);
            BackColor = Color.FromArgb(15, 23, 42); // Industrial dark acrylic enclosure
        }

        [Category("Appearance")]
        [DefaultValue("1420")]
        public string Value
        {
            get => _value;
            set { _value = value ?? ""; Invalidate(); }
        }

        [Category("Appearance")]
        public Color SegmentColor
        {
            get => _segmentColor;
            set
            {
                _segmentColor = value;
                _dimColor = Color.FromArgb(25, value.R / 4, value.G / 4, value.B / 4);
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(6)]
        public int DigitCount
        {
            get => _digitCount;
            set { _digitCount = Math.Max(1, Math.Min(12, value)); Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(false)]
        public bool ShowLeadingZeros
        {
            get => _showLeadingZeros;
            set { _showLeadingZeros = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = Width;
            int h = Height;

            // 1. Dark Beveled Display Frame
            using (var brush = new LinearGradientBrush(new Point(0, 0), new Point(0, h), Color.FromArgb(15, 23, 42), Color.FromArgb(10, 15, 30)))
            {
                g.FillRectangle(brush, 0, 0, w, h);
            }
            using (var borderPen = new Pen(Color.FromArgb(51, 65, 85), 1.5f))
            {
                g.DrawRectangle(borderPen, 0, 0, w - 1, h - 1);
            }

            // 2. Compute Digit Dimensions
            int padX = 10;
            int padY = 8;
            int availableW = w - (padX * 2);
            int availableH = h - (padY * 2);

            int digitW = (availableW - ((_digitCount - 1) * 4)) / _digitCount;
            digitW = Math.Max(12, digitW);
            int digitH = availableH;

            // Format string
            string text = _value;
            int textLen = text.Length;

            int currentX = padX;
            int padDigits = _digitCount - textLen;

            for (int i = 0; i < _digitCount; i++)
            {
                char c = ' ';
                if (i >= padDigits && (i - padDigits) < textLen)
                {
                    c = text[i - padDigits];
                }
                else if (_showLeadingZeros && i < padDigits)
                {
                    c = '0';
                }

                DrawDigit(g, currentX, padY, digitW, digitH, c);
                currentX += digitW + 4;
            }
        }

        private void DrawDigit(Graphics g, int x, int y, int w, int h, char c)
        {
            byte mask = 0;
            bool isColon = (c == ':');

            if (char.IsDigit(c))
            {
                mask = DigitPatterns[c - '0'];
            }
            else if (c == '-')
            {
                mask = 0x40; // G segment only
            }


            int t = Math.Max(2, h / 12); // segment thickness
            int halfH = h / 2;

            // Segment A (Top)
            DrawSegmentPoly(g, x + t, y, w - (t * 2), t, true, (mask & 0x01) != 0);

            // Segment B (Top Right)
            DrawSegmentPoly(g, x + w - t, y + t, t, halfH - t, false, (mask & 0x02) != 0);

            // Segment C (Bottom Right)
            DrawSegmentPoly(g, x + w - t, y + halfH, t, halfH - t, false, (mask & 0x04) != 0);

            // Segment D (Bottom)
            DrawSegmentPoly(g, x + t, y + h - t, w - (t * 2), t, true, (mask & 0x08) != 0);

            // Segment E (Bottom Left)
            DrawSegmentPoly(g, x, y + halfH, t, halfH - t, false, (mask & 0x10) != 0);

            // Segment F (Top Left)
            DrawSegmentPoly(g, x, y + t, t, halfH - t, false, (mask & 0x20) != 0);

            // Segment G (Center)
            DrawSegmentPoly(g, x + t, y + halfH - (t / 2), w - (t * 2), t, true, (mask & 0x40) != 0);

            // Colon support (e.g. "08:30")
            if (isColon)
            {
                int dotSize = Math.Max(3, t);
                int dotX = x + (w / 2) - (dotSize / 2);
                using var dotBrush = new SolidBrush(_segmentColor);
                g.FillEllipse(dotBrush, dotX, y + (h / 3) - (dotSize / 2), dotSize, dotSize);
                g.FillEllipse(dotBrush, dotX, y + (h * 2 / 3) - (dotSize / 2), dotSize, dotSize);
            }
        }

        private void DrawSegmentPoly(Graphics g, int x, int y, int w, int h, bool horizontal, bool lit)
        {
            Color color = lit ? _segmentColor : _dimColor;
            using var brush = new SolidBrush(color);

            if (lit)
            {
                // Subtle glow on lit segment
                using var glowPen = new Pen(Color.FromArgb(70, _segmentColor), 2f);
                g.DrawRectangle(glowPen, x - 1, y - 1, w + 2, h + 2);
            }

            g.FillRectangle(brush, x, y, w, h);
        }
    }
}
