using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Core.Network;
using ZeroUI.Core.Rendering;
using ZeroUI.Wpf.Base;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Network
{
    /// <summary>
    /// High-Density Hardware Switch Front-Panel Control (1U rackmount faceplate) for WPF.
    /// Renders 24/48-port RJ45 matrices and SFP/SFP+ optical cages with synchronized
    /// Link/Speed and Activity LEDs driven by <see cref="ZeroAnimationClock"/>, VLAN/PoE badges,
    /// and diagnostic inspection strip.
    /// </summary>
    public class SwitchFaceplate : ZeroWpfVisualBase
    {
        private SwitchPortLayout _layout;
        private IDisposable? _animSub;
        private SwitchPort? _selectedPort;

        public event EventHandler? SelectedPortChanged;

        #region Properties

        public SwitchPortLayout Layout => _layout;

        public SwitchPort? SelectedPort
        {
            get => _selectedPort;
            set
            {
                if (_selectedPort != value)
                {
                    _selectedPort = value;
                    SelectedPortChanged?.Invoke(this, EventArgs.Empty);
                    InvalidateVisual();
                }
            }
        }

        #endregion

        public SwitchFaceplate()
        {
            _layout = new SwitchPortLayout(24, 4);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _animSub?.Dispose();
            _animSub = ZeroAnimationClock.Subscribe((delta, frame) =>
            {
                Dispatcher.BeginInvoke(new Action(() => InvalidateVisual()));
            });
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _animSub?.Dispose();
            _animSub = null;
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

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w < 200 || h < 80) return;

            #if NETFRAMEWORK
            double dpi = 1.0;
            #else
            double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            #endif

            // Brushed Aluminum Chassis (1U rackmount)
            var chassisBg = ZeroWpfTheme.BgCard;
            var chassisPen = ZeroWpfTheme.BorderPen;
            dc.DrawRoundedRectangle(chassisBg, chassisPen, new Rect(0.5, 0.5, w - 1, h - 1), 6, 6);

            // Rack Mounting Ears
            double earWidth = 24;
            DrawRackEar(dc, new Rect(0, 0, earWidth, h), isLeft: true);
            DrawRackEar(dc, new Rect(w - earWidth, 0, earWidth, h), isLeft: false);

            double panelLeft = earWidth + 6;
            double panelRight = w - earWidth - 6;
            double panelWidth = panelRight - panelLeft;

            // Branding Bar
            double topBarHeight = 26;
            DrawBrandingBar(dc, new Rect(panelLeft, 6, panelWidth, topBarHeight), dpi);

            // Port Matrix Area
            double portAreaTop = topBarHeight + 8;
            double portAreaHeight = Math.Max(40.0, h - portAreaTop - 34);
            DrawPortMatrix(dc, new Rect(panelLeft, portAreaTop, panelWidth, portAreaHeight), dpi);

            // Diagnostic Inspection Strip
            double statusTop = h - 28;
            DrawInspectionStrip(dc, new Rect(panelLeft, statusTop, panelWidth, 22), dpi);
        }

        private void DrawRackEar(DrawingContext dc, Rect r, bool isLeft)
        {
            var earBg = ZeroWpfTheme.BgInput;
            var earPen = ZeroWpfTheme.BorderPen;
            dc.DrawRectangle(earBg, earPen, r);

            // Mounting screw holes
            var screwPen = ZeroWpfTheme.GridLinePen;
            double holeR = 3.0;
            double cx = r.Left + r.Width / 2.0;

            dc.DrawEllipse(ZeroWpfTheme.BgPrimary, screwPen, new Point(cx, r.Top + 14), holeR, holeR);
            dc.DrawEllipse(ZeroWpfTheme.BgPrimary, screwPen, new Point(cx, r.Bottom - 14), holeR, holeR);
        }

        private void DrawBrandingBar(DrawingContext dc, Rect r, double dpi)
        {
            var brandFt = CreateFormattedText("ZeroUI L3 Managed Switch - 24G 4SFP+", ZeroWpfTheme.BoldTypeface, 11.0, ZeroWpfTheme.TextPrimary, dpi);
            dc.DrawText(brandFt, new Point(r.Left + 4, r.Top + 4));

            // System Master LEDs
            double ledX = r.Right - 160;
            DrawLedIndicator(dc, new Point(ledX, r.Top + 12), "PWR", true, ZeroWpfTheme.SuccessAccent, dpi);
            DrawLedIndicator(dc, new Point(ledX + 50, r.Top + 12), "SYS", true, ZeroWpfTheme.SuccessAccent, dpi);
            DrawLedIndicator(dc, new Point(ledX + 100, r.Top + 12), "PoE", true, ZeroWpfTheme.PrimaryAccent, dpi);
        }

        private void DrawLedIndicator(DrawingContext dc, Point p, string label, bool isOn, Brush activeBrush, double dpi)
        {
            Brush b = isOn ? activeBrush : ZeroWpfTheme.TextMuted;
            dc.DrawEllipse(b, null, p, 3.5, 3.5);

            var ft = CreateFormattedText(label, ZeroWpfTheme.RegularTypeface, 8.0, ZeroWpfTheme.TextSecondary, dpi);
            dc.DrawText(ft, new Point(p.X + 6, p.Y - ft.Height / 2.0));
        }

        private void DrawPortMatrix(DrawingContext dc, Rect r, double dpi)
        {
            int totalRj45 = _layout.Rj45Count;
            int rj45Columns = totalRj45 / 2;
            int totalSfp = _layout.SfpCount;
            int sfpColumns = totalSfp / 2;

            double slotWidth = Math.Min(28.0, (r.Width - 40.0) / (rj45Columns + sfpColumns + 1));
            double slotHeight = (r.Height - 6.0) / 2.0;

            // Draw RJ45 Ports
            for (int col = 0; col < rj45Columns; col++)
            {
                double x = r.Left + col * (slotWidth + 3);

                // Top port (odd: 1, 3, 5...)
                int topPortIdx = col * 2 + 1;
                Rect topRect = new Rect(x, r.Top, slotWidth, slotHeight);
                DrawSinglePort(dc, topPortIdx, topRect, isSfp: false, dpi);

                // Bottom port (even: 2, 4, 6...)
                int botPortIdx = col * 2 + 2;
                Rect botRect = new Rect(x, r.Top + slotHeight + 4, slotWidth, slotHeight);
                DrawSinglePort(dc, botPortIdx, botRect, isSfp: false, dpi);
            }

            // Draw SFP+ Ports
            double sfpStartX = r.Left + rj45Columns * (slotWidth + 3) + 16;
            for (int col = 0; col < sfpColumns; col++)
            {
                double x = sfpStartX + col * (slotWidth + 4);

                int topSfpIdx = totalRj45 + col * 2 + 1;
                Rect topRect = new Rect(x, r.Top, slotWidth + 2, slotHeight);
                DrawSinglePort(dc, topSfpIdx, topRect, isSfp: true, dpi);

                int botSfpIdx = totalRj45 + col * 2 + 2;
                Rect botRect = new Rect(x, r.Top + slotHeight + 4, slotWidth + 2, slotHeight);
                DrawSinglePort(dc, botSfpIdx, botRect, isSfp: true, dpi);
            }
        }

        private void DrawSinglePort(DrawingContext dc, int portNumber, Rect r, bool isSfp, double dpi)
        {
            var port = _layout.FindPort(portNumber);
            bool isSelected = port == _selectedPort;

            Color bg = isSfp ? Color.FromRgb(28, 38, 52) : Color.FromRgb(18, 22, 30);
            var fillBrush = new SolidColorBrush(bg);
            fillBrush.Freeze();

            Pen pen = isSelected
                ? new Pen(ZeroWpfTheme.PrimaryAccent, 2.0)
                : new Pen(new SolidColorBrush(Color.FromRgb(48, 56, 74)), 1.0);
            pen.Freeze();

            dc.DrawRoundedRectangle(fillBrush, pen, r, 2, 2);

            // Port Number
            var numFt = CreateFormattedText(portNumber.ToString(), ZeroWpfTheme.RegularTypeface, 7.5, ZeroWpfTheme.TextMuted, dpi);
            dc.DrawText(numFt, new Point(r.Left + (r.Width - numFt.Width) / 2.0, r.Top + (r.Height - numFt.Height) / 2.0));

            // Link LED (Left)
            bool linkUp = port?.IsLinkUp ?? false;
            Brush linkBrush = linkUp ? ZeroWpfTheme.SuccessAccent : ZeroWpfTheme.TextMuted;
            dc.DrawEllipse(linkBrush, null, new Point(r.Left + 4, r.Top + 4), 2.0, 2.0);

            // Activity LED (Right, blinking if active)
            bool actBlink = linkUp && (port?.HasTraffic ?? false);
            Brush actBrush = actBlink ? ZeroWpfTheme.WarningAccent : ZeroWpfTheme.TextMuted;
            dc.DrawEllipse(actBrush, null, new Point(r.Right - 4, r.Top + 4), 2.0, 2.0);
        }

        private void DrawInspectionStrip(DrawingContext dc, Rect r, double dpi)
        {
            var bg = ZeroWpfTheme.BgInput;
            var pen = ZeroWpfTheme.GridLinePen;
            dc.DrawRoundedRectangle(bg, pen, r, 3, 3);

            string info = _selectedPort != null
                ? $"Port {_selectedPort.PortIndex} | {_selectedPort.Speed} | VLAN {_selectedPort.VlanId} | PoE: {_selectedPort.PoeWatts:F1}W | {_selectedPort.OperationalStatus}"
                : "Select a port to inspect real-time Link, VLAN, PoE, and Cable TDR telemetry.";

            var ft = CreateFormattedText(info, ZeroWpfTheme.RegularTypeface, 9.5, ZeroWpfTheme.TextSecondary, dpi);
            dc.DrawText(ft, new Point(r.Left + 8, r.Top + (r.Height - ft.Height) / 2.0));
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            Point p = e.GetPosition(this);

            double earWidth = 24;
            double panelLeft = earWidth + 6;
            double topBarHeight = 26;
            double portAreaTop = topBarHeight + 8;
            double portAreaHeight = Math.Max(40.0, ActualHeight - portAreaTop - 34);

            int totalRj45 = _layout.Rj45Count;
            int rj45Columns = totalRj45 / 2;
            int totalSfp = _layout.SfpCount;
            int sfpColumns = totalSfp / 2;

            double slotWidth = Math.Min(28.0, (ActualWidth - (earWidth * 2) - 52.0) / (rj45Columns + sfpColumns + 1));
            double slotHeight = (portAreaHeight - 6.0) / 2.0;

            // Hit test RJ45
            for (int col = 0; col < rj45Columns; col++)
            {
                double x = panelLeft + col * (slotWidth + 3);
                Rect topRect = new Rect(x, portAreaTop, slotWidth, slotHeight);
                if (topRect.Contains(p))
                {
                    SelectedPort = _layout.FindPort(col * 2 + 1);
                    return;
                }

                Rect botRect = new Rect(x, portAreaTop + slotHeight + 4, slotWidth, slotHeight);
                if (botRect.Contains(p))
                {
                    SelectedPort = _layout.FindPort(col * 2 + 2);
                    return;
                }
            }

            // Hit test SFP+
            double sfpStartX = panelLeft + rj45Columns * (slotWidth + 3) + 16;
            for (int col = 0; col < sfpColumns; col++)
            {
                double x = sfpStartX + col * (slotWidth + 4);
                Rect topRect = new Rect(x, portAreaTop, slotWidth + 2, slotHeight);
                if (topRect.Contains(p))
                {
                    SelectedPort = _layout.FindPort(totalRj45 + col * 2 + 1);
                    return;
                }

                Rect botRect = new Rect(x, portAreaTop + slotHeight + 4, slotWidth + 2, slotHeight);
                if (botRect.Contains(p))
                {
                    SelectedPort = _layout.FindPort(totalRj45 + col * 2 + 2);
                    return;
                }
            }
        }
    }
}
