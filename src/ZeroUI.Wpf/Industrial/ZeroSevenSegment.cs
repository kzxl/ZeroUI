using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Industrial
{
    /// <summary>
    /// Digital 7-Segment LED display supporting numbers, decimal points, and colons.
    /// Rendered directly via DrawingContext with authentic LED bevels and off-segment ghosting.
    /// </summary>
    public class ZeroSevenSegment : FrameworkElement
    {
        public static readonly DependencyProperty ValueTextProperty =
            DependencyProperty.Register(
                nameof(ValueText),
                typeof(string),
                typeof(ZeroSevenSegment),
                new FrameworkPropertyMetadata("0000", FrameworkPropertyMetadataOptions.AffectsRender));

        public string ValueText
        {
            get => (string)GetValue(ValueTextProperty);
            set => SetValue(ValueTextProperty, value);
        }

        public string Title { get; set; } = "Takt Time";
        public Color LedColor { get; set; } = Color.FromRgb(166, 227, 161); // Bright Green

        public ZeroSevenSegment()
        {
            ClipToBounds = true;
            ZeroWpfTheme.ThemeChanged += () => InvalidateVisual();
        }

        #if NETFRAMEWORK
        private static FormattedText CreateFormattedText(string text, Typeface typeface, double fontSize, Brush brush, double pixelsPerDip = 1.0)
        {
            return new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize, brush);
        }
        #else
        private static FormattedText CreateFormattedText(string text, Typeface typeface, double fontSize, Brush brush, double pixelsPerDip = 1.0)
        {
            return new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize, brush, pixelsPerDip);
        }
        #endif

        // Standard 7-segment bitmasks: A, B, C, D, E, F, G
        // Bit 0: A (Top)
        // Bit 1: B (Top-Right)
        // Bit 2: C (Bottom-Right)
        // Bit 3: D (Bottom)
        // Bit 4: E (Bottom-Left)
        // Bit 5: F (Top-Left)
        // Bit 6: G (Middle)
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
            0x7F, // 8: All
            0x6F, // 9: A B C D F G
        };

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            #if NETFRAMEWORK
            double dpi = 1.0;
            #else
            double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            #endif

            // Card background & bezel
            dc.DrawRectangle(ZeroWpfTheme.BgCard, null, new Rect(0, 0, w, h));
            dc.DrawRectangle(null, ZeroWpfTheme.BorderPen, new Rect(0.5, 0.5, w - 1, h - 1));

            // Title
            var titleFt = CreateFormattedText(Title, ZeroWpfTheme.BoldTypeface, 11.0, ZeroWpfTheme.TextSecondary, dpi);
            dc.DrawText(titleFt, new Point(12, 10));

            // Bezel display area (black sunken LED screen)
            double dispX = 12;
            double dispY = 32;
            double dispW = Math.Max(20, w - 24);
            double dispH = Math.Max(20, h - 44);

            dc.DrawRoundedRectangle(Brushes.Black, ZeroWpfTheme.BorderPen, new Rect(dispX, dispY, dispW, dispH), 4, 4);

            string text = ValueText ?? "0";
            int totalChars = text.Length;
            if (totalChars == 0) return;

            double digitW = Math.Min(32, (dispW - 16) / totalChars);
            double digitH = Math.Min(dispH - 12, digitW * 1.8);
            double totalBlockW = totalChars * digitW;
            double startX = dispX + (dispW - totalBlockW) / 2.0;
            double startY = dispY + (dispH - digitH) / 2.0;

            var onBrush = new SolidColorBrush(LedColor);
            onBrush.Freeze();
            var offBrush = new SolidColorBrush(Color.FromArgb(20, LedColor.R, LedColor.G, LedColor.B));
            offBrush.Freeze();

            for (int i = 0; i < totalChars; i++)
            {
                char ch = text[i];
                double dx = startX + i * digitW;

                if (ch >= '0' && ch <= '9')
                {
                    byte mask = DigitPatterns[ch - '0'];
                    DrawDigit(dc, dx, startY, digitW - 4, digitH, mask, onBrush, offBrush);
                }
                else if (ch == ':')
                {
                    // Colon
                    double cx = dx + digitW / 2.0;
                    double dotR = 2.5;
                    dc.DrawEllipse(onBrush, null, new Point(cx, startY + digitH * 0.35), dotR, dotR);
                    dc.DrawEllipse(onBrush, null, new Point(cx, startY + digitH * 0.65), dotR, dotR);
                }
                else if (ch == '.')
                {
                    // Decimal point
                    double cx = dx + digitW / 2.0;
                    double dotR = 2.5;
                    dc.DrawEllipse(onBrush, null, new Point(cx, startY + digitH - 4), dotR, dotR);
                }
                else if (ch == '-')
                {
                    // Dash (segment G)
                    DrawDigit(dc, dx, startY, digitW - 4, digitH, 0x40, onBrush, offBrush);
                }
            }
        }

        private static void DrawDigit(DrawingContext dc, double x, double y, double w, double h, byte mask, Brush onBrush, Brush offBrush)
        {
            double t = Math.Max(2, h * 0.1); // segment thickness
            double segW = w - 2 * t;
            double segH = (h - 3 * t) / 2.0;

            // Seg A (Top)
            dc.DrawRectangle((mask & 0x01) != 0 ? onBrush : offBrush, null, new Rect(x + t, y, segW, t));
            // Seg B (Top-Right)
            dc.DrawRectangle((mask & 0x02) != 0 ? onBrush : offBrush, null, new Rect(x + w - t, y + t, t, segH));
            // Seg C (Bottom-Right)
            dc.DrawRectangle((mask & 0x04) != 0 ? onBrush : offBrush, null, new Rect(x + w - t, y + 2 * t + segH, t, segH));
            // Seg D (Bottom)
            dc.DrawRectangle((mask & 0x08) != 0 ? onBrush : offBrush, null, new Rect(x + t, y + h - t, segW, t));
            // Seg E (Bottom-Left)
            dc.DrawRectangle((mask & 0x10) != 0 ? onBrush : offBrush, null, new Rect(x, y + 2 * t + segH, t, segH));
            // Seg F (Top-Left)
            dc.DrawRectangle((mask & 0x20) != 0 ? onBrush : offBrush, null, new Rect(x, y + t, t, segH));
            // Seg G (Middle)
            dc.DrawRectangle((mask & 0x40) != 0 ? onBrush : offBrush, null, new Rect(x + t, y + t + segH, segW, t));
        }
    }
}
