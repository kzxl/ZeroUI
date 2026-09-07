using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.Petrochem;
using ZeroUI.WinForms.Base;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Petrochem
{
    /// <summary>
    /// Safety Instrumented System (SIS) Cause &amp; Effect Matrix visualizer for WinForms per IEC 61508 / IEC 61511.
    /// Visualizes process trip initiators (Causes), final safety elements (Effects), SIL ratings,
    /// Maintenance Override Switch (MOS) bypass states, and live trip interlock propagation.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Oil & Gas")]
    [Description("SIS / ESD Cause & Effect safety matrix with SIL 1-4 ratings and MOS bypass tracking")]
    public class EsdMatrix : ZeroVisualControlBase
    {
        private readonly EsdEngine _engine = new EsdEngine();
        private double _pulsePhase;

        protected override bool AutoAnimate => true;

        public EsdMatrix()
        {
            Size = new Size(860, 480);
        }

        #region Public Properties

        [Category("Safety")]
        [Description("Access to the underlying pure ESD Cause & Effect engine")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public EsdEngine Engine => _engine;

        [Category("Safety")]
        [DefaultValue("SIS-ESD-01")]
        public string SisTag
        {
            get => _engine.SisTag;
            set
            {
                _engine.SisTag = value ?? "SIS-01";
                Invalidate();
            }
        }

        [Category("Safety")]
        [DefaultValue("Crude Fractionation Unit Safety Matrix")]
        public string AreaDescription
        {
            get => _engine.AreaDescription;
            set
            {
                _engine.AreaDescription = value ?? string.Empty;
                Invalidate();
            }
        }

        #endregion

        protected override void OnAnimationTick(double delta, long frame)
        {
            _pulsePhase = (_pulsePhase + delta * 3.5) % (Math.PI * 2.0);
        }

        protected override void OnDrawVisual(Graphics g, Rectangle bounds, ZeroThemePalette palette)
        {
            // 1. Safety Header
            DrawHeader(g, bounds, palette, out int headerH);

            var matrixRect = new Rectangle(bounds.X + 16, bounds.Y + headerH, bounds.Width - 32, bounds.Height - headerH - 16);

            // 2. Cause & Effect Crossbar Grid
            DrawMatrixGrid(g, matrixRect, palette);
        }

        private void DrawHeader(Graphics g, Rectangle bounds, ZeroThemePalette palette, out int headerH)
        {
            string title = $"{_engine.SisTag} — {_engine.AreaDescription.ToUpperInvariant()}";
            using (var font = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                Size titleSize = TextRenderer.MeasureText(g, title, font);

                // Global SIS Status Badge
                string statusText;
                Color statusBg, statusFg;

                if (_engine.ActiveTripCount > 0)
                {
                    statusText = $"TRIP ACTIVE ({_engine.ActiveTripCount})";
                    int alpha = 200 + (int)(Math.Sin(_pulsePhase) * 55.0);
                    statusBg = Color.FromArgb(alpha, 239, 68, 68);
                    statusFg = Color.White;
                }
                else if (_engine.ActiveBypassCount > 0)
                {
                    statusText = $"DEGRADED ({_engine.ActiveBypassCount})";
                    statusBg = palette.Warning;
                    statusFg = Color.Black;
                }
                else
                {
                    statusText = "ARMED & SAFE (IEC 61511)";
                    statusBg = palette.Success;
                    statusFg = Color.White;
                }

                int badgeW = 200;
                int badgeH = 26;

                if (bounds.Width - badgeW - 20 >= 20 + titleSize.Width + 16)
                {
                    TextRenderer.DrawText(g, title, font, new Point(bounds.X + 16, bounds.Y + 14), palette.TextPrimary);
                    PaintHelper.DrawStatusBadge(g, new Rectangle(bounds.Right - badgeW - 16, bounds.Y + 12, badgeW, badgeH), statusText, subFont, statusBg, statusFg);
                    headerH = 48;
                }
                else
                {
                    Rectangle titleRect = new Rectangle(bounds.X + 16, bounds.Y + 10, bounds.Width - 32, 20);
                    TextRenderer.DrawText(g, title, font, titleRect, palette.TextPrimary, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);
                    PaintHelper.DrawStatusBadge(g, new Rectangle(bounds.X + 16, bounds.Y + 34, Math.Min(bounds.Width - 32, badgeW), badgeH), statusText, subFont, statusBg, statusFg);
                    headerH = 68;
                }
            }
        }

        private void DrawMatrixGrid(Graphics g, Rectangle rect, ZeroThemePalette palette)
        {
            using (var titleFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                PaintHelper.DrawCardBox(g, rect, "CAUSE & EFFECT MATRIX (IEC 61508)", titleFont, palette);
            }

            int causeCount = _engine.Causes.Count;
            int effectCount = _engine.Effects.Count;
            if (causeCount == 0 || effectCount == 0) return;

            int leftHeaderW = Math.Min(320, rect.Width / 3 + 40);
            int topHeaderH = 68;
            int gridW = rect.Width - leftHeaderW - 16;
            int gridH = rect.Height - topHeaderH - 16;

            float cellW = (float)gridW / effectCount;
            float cellH = (float)gridH / causeCount;

            int gridX = rect.X + leftHeaderW;
            int gridY = rect.Y + topHeaderH;

            // 1. Column Headers (Effects)
            using (var tagFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (var descFont = new Font("Segoe UI", 6.5f, FontStyle.Regular))
            using (var borderPen = new Pen(palette.Border, 1f))
            {
                for (int e = 0; e < effectCount; e++)
                {
                    var effect = _engine.Effects[e];
                    float colX = gridX + (e * cellW);
                    var colRect = new RectangleF(colX, rect.Y + 28, cellW, topHeaderH - 30);

                    // Effect status background
                    Color effBg = effect.Status == EffectStatus.DeEnergized
                        ? Color.FromArgb(60, 239, 68, 68)
                        : Color.FromArgb(20, palette.Border);
                    using (var b = new SolidBrush(effBg))
                    {
                        g.FillRectangle(b, colRect);
                    }
                    g.DrawRectangle(borderPen, colRect.X, colRect.Y, colRect.Width, colRect.Height);

                    // Effect Tag
                    Color tagCol = effect.Status == EffectStatus.DeEnergized ? palette.Danger : palette.TextPrimary;
                    using (var b = new SolidBrush(tagCol))
                    {
                        g.DrawString(effect.Tag, tagFont, b, colX + 4, colRect.Y + 4);
                    }

                    // SIL Badge
                    DrawMiniSilBadge(g, colX + 4, colRect.Y + 20, effect.SilRating);

                    // Status Indicator
                    string stStr = effect.Status == EffectStatus.DeEnergized ? "TRIPPED" : "ENERGIZED";
                    Color stCol = effect.Status == EffectStatus.DeEnergized ? palette.Danger : palette.Success;
                    using (var b = new SolidBrush(stCol))
                    {
                        g.DrawString(stStr, descFont, b, colX + 4, colRect.Y + 34);
                    }
                }
            }

            // 2. Row Headers (Causes) & Grid Cells
            using (var causeTagFont = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (var causeDescFont = new Font("Segoe UI", 7f, FontStyle.Regular))
            using (var gridPen = new Pen(Color.FromArgb(40, palette.Border), 1f))
            {
                for (int c = 0; c < causeCount; c++)
                {
                    var cause = _engine.Causes[c];
                    float rowY = gridY + (c * cellH);
                    var rowRect = new RectangleF(rect.X + 8, rowY, leftHeaderW - 12, cellH - 2);

                    // Cause background
                    Color rowBg = cause.IsTripped
                        ? Color.FromArgb(50, 239, 68, 68)
                        : (cause.IsBypassed ? Color.FromArgb(40, 245, 158, 11) : Color.Transparent);

                    if (rowBg != Color.Transparent)
                    {
                        using (var b = new SolidBrush(rowBg))
                        {
                            g.FillRectangle(b, rowRect);
                        }
                    }

                    // Cause Status Dot
                    Color dotColor = cause.IsTripped
                        ? palette.Danger
                        : (cause.IsBypassed ? palette.Warning : palette.Success);
                    using (var b = new SolidBrush(dotColor))
                    {
                        g.FillEllipse(b, rowRect.X + 4, rowRect.Y + (cellH - 10) / 2, 8, 8);
                    }

                    // Cause Tag & SIL
                    using (var b = new SolidBrush(palette.TextPrimary))
                    {
                        g.DrawString(cause.Tag, causeTagFont, b, rowRect.X + 18, rowRect.Y + 2);
                    }
                    DrawMiniSilBadge(g, rowRect.X + 105, rowRect.Y + 2, cause.SilRating);

                    // Description & Value
                    string valStr = $"{cause.ProcessValue:F1} / {cause.TripSetpoint:F1} {cause.Unit}";
                    if (cause.IsBypassed) valStr += " [MOS]";
                    using (var b = new SolidBrush(palette.TextSecondary))
                    {
                        g.DrawString(cause.Description, causeDescFont, b, rowRect.X + 18, rowRect.Y + 18);
                        g.DrawString(valStr, causeDescFont, b, rowRect.Right - 90, rowRect.Y + 2);
                    }

                    // Grid Cells for row
                    for (int e = 0; e < effectCount; e++)
                    {
                        float colX = gridX + (e * cellW);
                        var cellRect = new RectangleF(colX, rowY, cellW, cellH);
                        g.DrawRectangle(gridPen, cellRect.X, cellRect.Y, cellRect.Width, cellRect.Height);

                        // Find Interlock
                        var link = FindInterlock(c, e);
                        if (link != null)
                        {
                            DrawInterlockNode(g, cellRect, link, cause, palette);
                        }
                    }
                }
            }
        }

        private EsdInterlock? FindInterlock(int causeIdx, int effectIdx)
        {
            for (int i = 0; i < _engine.Interlocks.Count; i++)
            {
                if (_engine.Interlocks[i].CauseIndex == causeIdx && _engine.Interlocks[i].EffectIndex == effectIdx)
                {
                    return _engine.Interlocks[i];
                }
            }
            return null;
        }

        private void DrawInterlockNode(Graphics g, RectangleF cell, EsdInterlock link, EsdCause cause, ZeroThemePalette palette)
        {
            float cx = cell.X + cell.Width / 2f;
            float cy = cell.Y + cell.Height / 2f;

            if (link.IsActive)
            {
                // Active trip token: Red solid circle with aura
                int r = 6;
                using (var auraBrush = new SolidBrush(Color.FromArgb(80, 239, 68, 68)))
                {
                    g.FillEllipse(auraBrush, cx - r - 3, cy - r - 3, (r + 3) * 2, (r + 3) * 2);
                }
                using (var redBrush = new SolidBrush(palette.Danger))
                {
                    g.FillEllipse(redBrush, cx - r, cy - r, r * 2, r * 2);
                }
                using (var whitePen = new Pen(Color.White, 1.5f))
                {
                    g.DrawLine(whitePen, cx - 2.5f, cy, cx + 2.5f, cy);
                }
            }
            else if (cause.IsBypassed)
            {
                // Bypassed interlock: Amber diamond
                PointF[] diamond = {
                    new PointF(cx, cy - 5),
                    new PointF(cx + 5, cy),
                    new PointF(cx, cy + 5),
                    new PointF(cx - 5, cy)
                };
                using (var warnBrush = new SolidBrush(palette.Warning))
                {
                    g.FillPolygon(warnBrush, diamond);
                }
            }
            else
            {
                // Armed healthy interlock node: Emerald green ring
                using (var pen = new Pen(palette.Success, 1.2f))
                {
                    g.DrawEllipse(pen, cx - 3.5f, cy - 3.5f, 7, 7);
                }
                using (var dotBrush = new SolidBrush(Color.FromArgb(60, palette.Success)))
                {
                    g.FillEllipse(dotBrush, cx - 2, cy - 2, 4, 4);
                }
            }
        }

        private static void DrawMiniSilBadge(Graphics g, float x, float y, SafetyIntegrityLevel sil)
        {
            Color silColor;
            switch (sil)
            {
                case SafetyIntegrityLevel.SIL4: silColor = Color.FromArgb(220, 38, 38); break;
                case SafetyIntegrityLevel.SIL3: silColor = Color.FromArgb(234, 88, 12); break;
                case SafetyIntegrityLevel.SIL2: silColor = Color.FromArgb(202, 138, 4); break;
                case SafetyIntegrityLevel.SIL1: silColor = Color.FromArgb(13, 148, 136); break;
                default: silColor = Color.FromArgb(100, 116, 139); break;
            }

            var badgeRect = new RectangleF(x, y, 30, 13);
            using (var b = new SolidBrush(silColor))
            {
                g.FillRectangle(b, badgeRect);
            }
            using (var font = new Font(FontFamily.GenericSansSerif, 6.5f, FontStyle.Bold))
            using (var textBrush = new SolidBrush(Color.White))
            {
                g.DrawString(sil.ToString(), font, textBrush, x + 2, y + 0.5f);
            }
        }
    }
}
