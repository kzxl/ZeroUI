using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Layout
{
    public enum StackOrientation
    {
        Vertical,
        Horizontal
    }

    public enum StackAlignment
    {
        Start,
        Center,
        End,
        Stretch
    }

    /// <summary>
    /// High-performance, zero-flicker StackPanel for ZeroUI.
    /// Replaces legacy FlowLayoutPanel with predictable single-pass layout and zero GC allocations.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Layout")]
    [Description("Arranges child controls into a single horizontal or vertical line with smooth layout calculation.")]
    public class ZeroStackPanel : Panel
    {
        private StackOrientation _orientation = StackOrientation.Vertical;
        private StackAlignment _alignment = StackAlignment.Stretch;
        private int _spacing = 8;
        private Color _borderColor = Color.Empty;
        private int _borderWidth = 0;
        private bool _isPerformingLayout = false;

        public ZeroStackPanel()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);

            DoubleBuffered = true;
            AutoScroll = true;
            Padding = new Padding(8);
            BackColor = Color.Transparent;

            ZeroTheme.ThemeChanged += OnThemeChanged;
            ApplyCurrentTheme();
        }

        [Category("Layout")]
        [DefaultValue(StackOrientation.Vertical)]
        [Description("The direction in which child controls are stacked.")]
        public StackOrientation Orientation
        {
            get => _orientation;
            set
            {
                if (_orientation != value)
                {
                    _orientation = value;
                    PerformLayout();
                }
            }
        }

        [Category("Layout")]
        [DefaultValue(StackAlignment.Stretch)]
        [Description("Alignment of child controls along the cross-axis.")]
        public StackAlignment Alignment
        {
            get => _alignment;
            set
            {
                if (_alignment != value)
                {
                    _alignment = value;
                    PerformLayout();
                }
            }
        }

        [Category("Layout")]
        [DefaultValue(8)]
        [Description("Spacing in pixels between adjacent controls.")]
        public int Spacing
        {
            get => _spacing;
            set
            {
                if (_spacing != value)
                {
                    _spacing = Math.Max(0, value);
                    PerformLayout();
                }
            }
        }

        [Category("Appearance")]
        [Description("Optional border color.")]
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                _borderColor = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(0)]
        [Description("Optional border thickness in pixels.")]
        public int BorderWidth
        {
            get => _borderWidth;
            set
            {
                _borderWidth = Math.Max(0, value);
                Invalidate();
            }
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            ApplyCurrentTheme();
            Invalidate();
        }

        private void ApplyCurrentTheme()
        {
            var palette = ZeroTheme.Palette;
            if (BackColor == Color.Transparent || BackColor == palette.Surface || BackColor == palette.CardBackground)
            {
                // Sync subtle borders if configured
                if (_borderWidth > 0 && _borderColor == Color.Empty)
                {
                    _borderColor = palette.Border;
                }
            }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            if (_isPerformingLayout) return;

            _isPerformingLayout = true;
            try
            {
                base.OnLayout(levent);
                ArrangeChildren();
            }
            finally
            {
                _isPerformingLayout = false;
            }
        }

        private void ArrangeChildren()
        {
            if (Controls.Count == 0) return;

            int clientLeft = Padding.Left + AutoScrollPosition.X;
            int clientTop = Padding.Top + AutoScrollPosition.Y;
            int availableWidth = Math.Max(0, DisplayRectangle.Width - Padding.Horizontal);
            int availableHeight = Math.Max(0, DisplayRectangle.Height - Padding.Vertical);

            int currentX = clientLeft;
            int currentY = clientTop;

            for (int i = 0; i < Controls.Count; i++)
            {
                Control child = Controls[i];
                if (!child.Visible) continue;

                int childW = child.Width;
                int childH = child.Height;

                if (_orientation == StackOrientation.Vertical)
                {
                    int x = currentX;
                    int w = childW;

                    switch (_alignment)
                    {
                        case StackAlignment.Stretch:
                            x = clientLeft;
                            w = availableWidth;
                            break;
                        case StackAlignment.Center:
                            x = clientLeft + Math.Max(0, (availableWidth - childW) / 2);
                            break;
                        case StackAlignment.End:
                            x = clientLeft + Math.Max(0, availableWidth - childW);
                            break;
                        case StackAlignment.Start:
                        default:
                            x = clientLeft;
                            break;
                    }

                    child.SetBounds(x, currentY, w, childH);
                    currentY += childH + _spacing;
                }
                else // Horizontal
                {
                    int y = currentY;
                    int h = childH;

                    switch (_alignment)
                    {
                        case StackAlignment.Stretch:
                            y = clientTop;
                            h = availableHeight;
                            break;
                        case StackAlignment.Center:
                            y = clientTop + Math.Max(0, (availableHeight - childH) / 2);
                            break;
                        case StackAlignment.End:
                            y = clientTop + Math.Max(0, availableHeight - childH);
                            break;
                        case StackAlignment.Start:
                        default:
                            y = clientTop;
                            break;
                    }

                    child.SetBounds(currentX, y, childW, h);
                    currentX += childW + _spacing;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_borderWidth > 0 && _borderColor != Color.Empty && _borderColor != Color.Transparent)
            {
                using var pen = new Pen(_borderColor, _borderWidth);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ZeroTheme.ThemeChanged -= OnThemeChanged;
            }
            base.Dispose(disposing);
        }
    }
}
