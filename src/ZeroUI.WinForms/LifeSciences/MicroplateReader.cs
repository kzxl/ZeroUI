using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.LifeSciences;
using ZeroUI.Core.Rendering;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.LifeSciences
{
    /// <summary>
    /// 96/384-Well ANSI/SLAS standard microplate reader assay heatmap visualizer.
    /// Features optical density (OD) absorbance heatmaps, standard curve dilution series,
    /// well selection, replicate statistics, and outlier detection.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Life Sciences")]
    [Description("ANSI/SLAS 96/384-well microplate absorbance/fluorescence reader heatmap with well selection")]
    public class MicroplateReader : Control
    {
        private readonly MicroplateEngine _engine;
        private IDisposable? _animSub;
        private MicroplateWell? _selectedWell;

        public event EventHandler? SelectedWellChanged;

        public MicroplateReader() : this(PlateFormat.Wells96)
        {
        }

        public MicroplateReader(PlateFormat format)
        {
            _engine = new MicroplateEngine(format);

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            Size = new Size(780, 440);

            ZeroTheme.ThemeChanged += OnThemeChanged;
            MouseDown += OnPlateMouseDown;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _animSub ??= ZeroAnimationClock.Subscribe((delta, frame) =>
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    Invalidate();
                }
            });
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _animSub?.Dispose();
            _animSub = null;
            base.OnHandleDestroyed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ZeroTheme.ThemeChanged -= OnThemeChanged;
                _animSub?.Dispose();
                _animSub = null;
            }
            base.Dispose(disposing);
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            if (IsHandleCreated && !IsDisposed)
            {
                if (InvokeRequired)
                    BeginInvoke(new Action(Invalidate));
                else
                    Invalidate();
            }
        }

        #region Public Properties

        [Category("Microplate")]
        [Description("Access to the underlying microplate assay computation engine")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public MicroplateEngine Engine => _engine;

        [Category("Microplate")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public MicroplateWell? SelectedWell
        {
            get => _selectedWell;
            set
            {
                if (_selectedWell != value)
                {
                    if (_selectedWell != null) _selectedWell.IsSelected = false;
                    _selectedWell = value;
                    if (_selectedWell != null) _selectedWell.IsSelected = true;
                    SelectedWellChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        #endregion

        private void OnPlateMouseDown(object? sender, MouseEventArgs e)
        {
            int gridLeft = 40;
            int gridTop = 64;
            int gridWidth = Width - gridLeft - 180;
            int gridHeight = Height - gridTop - 20;

            if (gridWidth <= 0 || gridHeight <= 0) return;

            int rows = _engine.RowCount;
            int cols = _engine.ColCount;
            float cellW = (float)gridWidth / cols;
            float cellH = (float)gridHeight / rows;

            if (e.X >= gridLeft && e.X <= gridLeft + gridWidth &&
                e.Y >= gridTop && e.Y <= gridTop + gridHeight)
            {
                int c = (int)((e.X - gridLeft) / cellW);
                int r = (int)((e.Y - gridTop) / cellH);

                if (r >= 0 && r < rows && c >= 0 && c < cols)
                {
                    SelectedWell = _engine.Wells[r, c];
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var theme = ZeroTheme.Colors;

            // Background
            using (var bgBrush = new SolidBrush(theme.Background))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            // Header Banner
            DrawHeader(g, theme);

            // Layout
            int gridLeft = 40;
            int gridTop = 64;
            int rightPanelW = 160;
            int gridWidth = Width - gridLeft - rightPanelW - 20;
            int gridHeight = Height - gridTop - 20;

            if (gridWidth < 150 || gridHeight < 100) return;

            Rectangle gridRect = new Rectangle(gridLeft, gridTop, gridWidth, gridHeight);
            Rectangle sideRect = new Rectangle(gridLeft + gridWidth + 14, gridTop, rightPanelW, gridHeight);

            DrawMicroplateGrid(g, gridRect, theme);
            DrawSidebar(g, sideRect, theme);
        }

        private void DrawHeader(Graphics g, ZeroThemePalette theme)
        {
            using (var fontTitle = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (var fontSmall = new Font("Segoe UI", 8.5f))
            using (var fontBold = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, _engine.AssayTitle, fontTitle, new Point(20, 14), theme.TextPrimary);

                string rangeStr = $"Range: {_engine.MinValue:F3} - {_engine.MaxValue:F3} OD | Replicates: Blank, Std, Ctrl, Unk";
                TextRenderer.DrawText(g, rangeStr, fontSmall, new Point(20, 36), theme.TextSecondary);

                // Format Pill
                string formatText = _engine.Format == PlateFormat.Wells96 ? "96-WELL (8x12)" : "384-WELL (16x24)";
                Rectangle pillRect = new Rectangle(Width - 140, 14, 120, 24);
                using (var pillBrush = new SolidBrush(Color.FromArgb(25, 59, 130, 246)))
                using (var pillPen = new Pen(Color.FromArgb(59, 130, 246), 1f))
                {
                    g.FillRectangle(pillBrush, pillRect);
                    g.DrawRectangle(pillPen, pillRect);
                }
                TextRenderer.DrawText(g, formatText, fontBold, pillRect, Color.FromArgb(59, 130, 246), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DrawMicroplateGrid(Graphics g, Rectangle rect, ZeroThemePalette theme)
        {
            // Plate Frame border
            using (var frameBrush = new SolidBrush(Color.FromArgb(10, theme.TextPrimary)))
            using (var framePen = new Pen(theme.Border, 1.5f))
            {
                g.FillRectangle(frameBrush, rect);
                g.DrawRectangle(framePen, rect);
            }

            int rows = _engine.RowCount;
            int cols = _engine.ColCount;
            float cellW = (float)rect.Width / cols;
            float cellH = (float)rect.Height / rows;
            float wellDiameter = Math.Min(cellW, cellH) * 0.72f;

            double minVal = _engine.MinValue;
            double maxVal = _engine.MaxValue;
            double valSpan = Math.Max(0.001, maxVal - minVal);

            using (var fontHeader = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (var fontWell = new Font("Segoe UI", 6.5f))
            {
                // Column Labels (1..12 or 1..24) along top
                for (int c = 0; c < cols; c++)
                {
                    float cx = rect.X + c * cellW + cellW / 2;
                    Rectangle colRect = new Rectangle((int)cx - 12, rect.Y - 18, 24, 14);
                    TextRenderer.DrawText(g, $"{c + 1}", fontHeader, colRect, theme.TextSecondary, TextFormatFlags.HorizontalCenter);
                }

                // Row Labels (A..H or A..P) along left
                for (int r = 0; r < rows; r++)
                {
                    float cy = rect.Y + r * cellH + cellH / 2;
                    char rChar = (char)('A' + r);
                    Rectangle rowRect = new Rectangle(rect.X - 22, (int)cy - 8, 18, 16);
                    TextRenderer.DrawText(g, rChar.ToString(), fontHeader, rowRect, theme.TextSecondary, TextFormatFlags.HorizontalCenter);
                }

                // Wells
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        var well = _engine.Wells[r, c];
                        float cx = rect.X + c * cellW + cellW / 2;
                        float cy = rect.Y + r * cellH + cellH / 2;
                        RectangleF wellRect = new RectangleF(cx - wellDiameter / 2, cy - wellDiameter / 2, wellDiameter, wellDiameter);

                        // Heatmap color interpolation
                        float norm = (float)Math.Max(0.0, Math.Min(1.0, (well.Value - minVal) / valSpan));
                        Color wellColor = InterpolateOdColor(norm);

                        using (var wBrush = new SolidBrush(wellColor))
                        {
                            g.FillEllipse(wBrush, wellRect);
                        }

                        // Outlier ring
                        if (well.IsOutlier)
                        {
                            using (var outPen = new Pen(Color.FromArgb(239, 68, 68), 2f))
                            {
                                g.DrawEllipse(outPen, wellRect);
                            }
                        }

                        // Selection indicator
                        if (well.IsSelected)
                        {
                            using (var selPen = new Pen(Color.White, 2.5f))
                            {
                                g.DrawEllipse(selPen, cx - wellDiameter / 2 - 2, cy - wellDiameter / 2 - 2, wellDiameter + 4, wellDiameter + 4);
                            }
                        }
                    }
                }
            }
        }

        private void DrawSidebar(Graphics g, Rectangle rect, ZeroThemePalette theme)
        {
            using (var boxBrush = new SolidBrush(Color.FromArgb(12, theme.TextPrimary)))
            using (var borderPen = new Pen(theme.Border, 1f))
            using (var fontTitle = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (var fontLabel = new Font("Segoe UI", 8f))
            using (var fontVal = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                g.FillRectangle(boxBrush, rect);
                g.DrawRectangle(borderPen, rect);

                TextRenderer.DrawText(g, "WELL INSPECTION", fontTitle, new Point(rect.X + 12, rect.Y + 10), theme.TextSecondary);

                int curY = rect.Y + 34;
                if (_selectedWell != null)
                {
                    TextRenderer.DrawText(g, $"Well ID: {_selectedWell.WellId}", fontVal, new Point(rect.X + 12, curY), theme.TextPrimary);
                    curY += 22;
                    TextRenderer.DrawText(g, $"Type: {_selectedWell.SampleType}", fontLabel, new Point(rect.X + 12, curY), theme.TextSecondary);
                    curY += 22;
                    TextRenderer.DrawText(g, $"OD450: {_selectedWell.Value:F3}", fontVal, new Point(rect.X + 12, curY), Color.FromArgb(59, 130, 246));
                    curY += 22;
                    TextRenderer.DrawText(g, $"Conc: {_selectedWell.Concentration:F1} ng/mL", fontLabel, new Point(rect.X + 12, curY), theme.TextPrimary);

                    if (_selectedWell.IsOutlier)
                    {
                        curY += 24;
                        TextRenderer.DrawText(g, "[FLAGGED OUTLIER]", fontVal, new Point(rect.X + 12, curY), Color.FromArgb(239, 68, 68));
                    }
                }
                else
                {
                    TextRenderer.DrawText(g, "Click a well to view", fontLabel, new Point(rect.X + 12, curY), theme.TextSecondary);
                    TextRenderer.DrawText(g, "assay telemetry & conc.", fontLabel, new Point(rect.X + 12, curY + 18), theme.TextSecondary);
                }

                // Legend at bottom of sidebar
                int legY = rect.Bottom - 110;
                TextRenderer.DrawText(g, "HEATMAP SCALE", fontTitle, new Point(rect.X + 12, legY), theme.TextSecondary);
                legY += 20;

                Rectangle legBar = new Rectangle(rect.X + 12, legY, rect.Width - 24, 12);
                using (var legBrush = new LinearGradientBrush(legBar, Color.FromArgb(30, 58, 138), Color.FromArgb(239, 68, 68), LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(legBrush, legBar);
                }
                legY += 16;
                TextRenderer.DrawText(g, "Low OD", fontLabel, new Point(rect.X + 12, legY), theme.TextSecondary);
                TextRenderer.DrawText(g, "High OD", fontLabel, new Point(rect.Right - 55, legY), theme.TextSecondary);
            }
        }

        private static Color InterpolateOdColor(float norm)
        {
            // Gradient: Deep Navy/Cyan (0.0) -> Emerald (0.35) -> Amber (0.7) -> Crimson Red (1.0)
            if (norm < 0.35f)
            {
                float t = norm / 0.35f;
                return BlendColor(Color.FromArgb(30, 58, 138), Color.FromArgb(16, 185, 129), t);
            }
            if (norm < 0.70f)
            {
                float t = (norm - 0.35f) / 0.35f;
                return BlendColor(Color.FromArgb(16, 185, 129), Color.FromArgb(245, 158, 11), t);
            }
            {
                float t = (norm - 0.70f) / 0.30f;
                return BlendColor(Color.FromArgb(245, 158, 11), Color.FromArgb(239, 68, 68), t);
            }
        }

        private static Color BlendColor(Color c1, Color c2, float t)
        {
            int r = (int)(c1.R + (c2.R - c1.R) * t);
            int g = (int)(c1.G + (c2.G - c1.G) * t);
            int b = (int)(c1.B + (c2.B - c1.B) * t);
            return Color.FromArgb(Math.Min(255, Math.Max(0, r)), Math.Min(255, Math.Max(0, g)), Math.Min(255, Math.Max(0, b)));
        }
    }
}
