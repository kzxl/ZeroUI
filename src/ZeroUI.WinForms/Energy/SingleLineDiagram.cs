using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.Energy;
using ZeroUI.Core.Rendering;
using ZeroUI.WinForms.Base;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Energy
{
    /// <summary>
    /// IEC 61850 Substation Single-Line Diagram (SLD) vector canvas.
    /// Visualizes voltage busbars with standard coloring (500kV, 220kV, 110kV, 22kV),
    /// transformers, circuit breakers, and animated active/reactive power flows.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Energy & Smart Grid")]
    [Description("IEC 61850 Substation Single-Line Diagram (SLD) canvas with dynamic busbar coloring and power flows")]
    public class SingleLineDiagram : ZeroVisualControlBase
    {
        private readonly SldEngine _engine = new SldEngine();
        private string _substationName = "110kV / 22kV Primary Substation";

        protected override bool AutoAnimate => true;

        public SingleLineDiagram()
        {
            Size = new Size(760, 360);
        }

        #region Public Properties

        [Category("SLD")]
        [Description("Access to the underlying SLD computation engine")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SldEngine Engine => _engine;

        [Category("SLD")]
        [DefaultValue("110kV / 22kV Primary Substation")]
        public string SubstationName
        {
            get => _substationName;
            set
            {
                _substationName = value;
                Invalidate();
            }
        }

        #endregion

        protected override void OnDrawVisual(Graphics g, Rectangle bounds, ZeroThemePalette theme)
        {
            // Header: Substation title, Frequency, Grid Stats
            DrawHeader(g, theme);

            int sldLeft = 30;
            int sldTop = 60;
            int sldWidth = bounds.Width - 60;
            int sldHeight = bounds.Height - sldTop - 20;

            if (sldWidth < 200 || sldHeight < 120)
                return;

            Rectangle canvasRect = new Rectangle(sldLeft, sldTop, sldWidth, sldHeight);

            // 1. High-Voltage 110kV Busbar (Top)
            float bus110Y = canvasRect.Top + 30;
            Color color110 = ColorTranslator.FromHtml(SldEngine.GetVoltageColorHex(VoltageLevel.HV_110kV));
            DrawBusbar(g, canvasRect.Left + 20, bus110Y, canvasRect.Width - 40, color110, "BUS 110kV-A (111.8 kV)", theme);

            // 2. Medium-Voltage 22kV Busbar (Bottom)
            float bus22Y = canvasRect.Bottom - 35;
            Color color22 = ColorTranslator.FromHtml(SldEngine.GetVoltageColorHex(VoltageLevel.MV_22kV));
            DrawBusbar(g, canvasRect.Left + 20, bus22Y, canvasRect.Width - 40, color22, "BUS 22kV-A (22.4 kV)", theme);

            // 3. Central Bay: 110kV Breaker -> Transformer T1 -> 22kV Breaker
            float bayX = canvasRect.Left + canvasRect.Width * 0.40f;
            DrawTransformerBay(g, bayX, bus110Y, bus22Y, color110, color22, theme);

            // 4. Feeder Bay 2
            float feederX = canvasRect.Left + canvasRect.Width * 0.75f;
            DrawOutgoerBay(g, feederX, bus22Y, color22, theme);
        }

        private void DrawHeader(Graphics g, ZeroThemePalette theme)
        {
            using (var fontTitle = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (var fontSmall = new Font("Segoe UI", 8.5f))
            using (var fontBold = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, _substationName, fontTitle, new Point(24, 14), theme.TextPrimary);

                int statsX = Width - 380;
                if (statsX > 200)
                {
                    string freqStr = "Freq: 50.02 Hz | Power: 42.0 MW (8.5 MVAr)";
                    TextRenderer.DrawText(g, freqStr, fontSmall, new Point(statsX, 16), theme.TextSecondary);

                    // Operational Status Badge
                    Rectangle pillRect = new Rectangle(Width - 110, 12, 86, 24);
                    DrawStatusBadge(g, pillRect, "ENERGIZED", fontBold, Color.FromArgb(34, 197, 94), Color.FromArgb(34, 197, 94), 4);
                }
            }
        }

        private void DrawBusbar(Graphics g, float x, float y, float width, Color color, string label, ZeroThemePalette theme)
        {
            using (var busPen = new Pen(color, 5f))
            using (var font = new Font("Segoe UI", 8f, FontStyle.Bold))
            {
                g.DrawLine(busPen, x, y, x + width, y);
                TextRenderer.DrawText(g, label, font, new Point((int)x + 8, (int)y - 18), color);
            }
        }

        private void DrawTransformerBay(Graphics g, float x, float topBusY, float botBusY, Color c110, Color c22, ZeroThemePalette theme)
        {
            using (var p110 = new Pen(c110, 2f))
            using (var p22 = new Pen(c22, 2f))
            using (var font = new Font("Segoe UI", 7.5f))
            {
                // Line from 110kV bus down to Breaker
                float cb110Y = topBusY + 35;
                g.DrawLine(p110, x, topBusY, x, cb110Y);

                // 110kV Breaker symbol
                DrawBreakerSymbol(g, x, cb110Y, BreakerState.Closed, c110);
                TextRenderer.DrawText(g, "CB-110", font, new Point((int)x + 16, (int)cb110Y - 8), theme.TextSecondary);

                // Line to Transformer
                float tTopY = cb110Y + 35;
                g.DrawLine(p110, x, cb110Y + 16, x, tTopY);

                // Transformer circles (2 Intersecting circles)
                float tRadius = 18f;
                float tCenter1Y = tTopY + tRadius;
                float tCenter2Y = tCenter1Y + tRadius * 1.1f;

                using (var tPen1 = new Pen(c110, 2.5f))
                using (var tPen2 = new Pen(c22, 2.5f))
                {
                    g.DrawEllipse(tPen1, x - tRadius, tCenter1Y - tRadius, tRadius * 2, tRadius * 2);
                    g.DrawEllipse(tPen2, x - tRadius, tCenter2Y - tRadius, tRadius * 2, tRadius * 2);
                }

                TextRenderer.DrawText(g, "T1 63 MVA (67%)", font, new Point((int)x + 24, (int)tCenter1Y), theme.TextPrimary);

                // Line from Transformer to 22kV Breaker
                float cb22Y = tCenter2Y + tRadius + 20;
                g.DrawLine(p22, x, tCenter2Y + tRadius, x, cb22Y);

                // 22kV Breaker symbol
                DrawBreakerSymbol(g, x, cb22Y, BreakerState.Closed, c22);
                TextRenderer.DrawText(g, "CB-22", font, new Point((int)x + 16, (int)cb22Y - 8), theme.TextSecondary);

                // Line to 22kV bus
                g.DrawLine(p22, x, cb22Y + 16, x, botBusY);

                // Animated power flow dots flowing top to bottom
                float phase = ZeroAnimationClock.FluidPhase;
                float flowY = topBusY + (botBusY - topBusY) * phase;
                g.FillEllipse(Brushes.White, x - 3, flowY - 3, 6, 6);
            }
        }

        private void DrawOutgoerBay(Graphics g, float x, float botBusY, Color color, ZeroThemePalette theme)
        {
            using (var pen = new Pen(color, 2f))
            using (var font = new Font("Segoe UI", 7.5f))
            {
                float cbY = botBusY - 45;
                g.DrawLine(pen, x, botBusY, x, cbY + 16);

                DrawBreakerSymbol(g, x, cbY, BreakerState.Closed, color);
                TextRenderer.DrawText(g, "FEEDER-01", font, new Point((int)x + 16, (int)cbY - 8), theme.TextSecondary);

                // Cable termination line going downwards
                g.DrawLine(pen, x, cbY, x, cbY - 30);
                g.DrawLine(pen, x - 6, cbY - 30, x + 6, cbY - 30);
            }
        }

        private void DrawBreakerSymbol(Graphics g, float cx, float cy, BreakerState state, Color color)
        {
            int size = 14;
            Rectangle rect = new Rectangle((int)cx - size / 2, (int)cy - size / 2, size, size);

            if (state == BreakerState.Closed)
            {
                using (var fillBrush = new SolidBrush(color))
                {
                    g.FillRectangle(fillBrush, rect);
                }
            }
            else
            {
                using (var borderPen = new Pen(color, 2f))
                using (var bgBrush = new SolidBrush(Color.FromArgb(20, color)))
                {
                    g.FillRectangle(bgBrush, rect);
                    g.DrawRectangle(borderPen, rect);
                }
            }
        }
    }
}
