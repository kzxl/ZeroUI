using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Base;
using ZeroUI.WinForms.Icons;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Containers
{
    /// <summary>
    /// Style of the border outline for <see cref="PanelControl"/>.
    /// </summary>
    public enum ZeroPanelBorderStyle
    {
        None = 0,
        Solid = 1,
        Dotted = 2,
        Dashed = 3
    }

    /// <summary>
    /// Semantic theme background mode for <see cref="PanelControl"/>.
    /// </summary>
    public enum ZeroPanelType
    {
        Surface = 0,
        Card = 1,
        Header = 2,
        Transparent = 3,
        Custom = 4
    }

    /// <summary>
    /// Modern theme-aware container panel with customizable border styles, rounded corners,
    /// High-DPI Per-Monitor V2 scaling, and automatic Dark/Light theme reactivity.
    /// Direct drop-in replacement for standard <see cref="System.Windows.Forms.Panel"/>.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Containers")]
    [Description("Modern theme-aware container panel with border, radius, and DPI scaling")]
    [ToolboxBitmap(typeof(ZeroIcons), "ZeroDefaultControl.bmp")]
    public class PanelControl : Panel, IZeroDpiScalable
    {
        private ZeroPanelBorderStyle _borderStyle = ZeroPanelBorderStyle.Solid;
        private ZeroPanelType _panelType = ZeroPanelType.Surface;
        private int _baseBorderRadius = 0;
        private int _borderRadius = 0;
        private float _borderThickness = 1f;
        private Color _customBorderColor = Color.Empty;
        private Color _customBackColor = Color.Empty;
        private float _currentDpiScale = 1.0f;

        [Category("Appearance")]
        [DefaultValue(ZeroPanelBorderStyle.Solid)]
        [Description("Visual style of the panel border.")]
        public new ZeroPanelBorderStyle BorderStyle
        {
            get => _borderStyle;
            set
            {
                if (_borderStyle != value)
                {
                    _borderStyle = value;
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue(ZeroPanelType.Surface)]
        [Description("Semantic theme role defining the background token.")]
        public ZeroPanelType PanelType
        {
            get => _panelType;
            set
            {
                if (_panelType != value)
                {
                    _panelType = value;
                    UpdateThemeColors();
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue(0)]
        [Description("Corner radius for smooth rounded borders. 0 produces sharp corners.")]
        public int BorderRadius
        {
            get => _baseBorderRadius;
            set
            {
                int clamped = Math.Max(0, value);
                if (_baseBorderRadius != clamped)
                {
                    _baseBorderRadius = clamped;
                    _borderRadius = (int)Math.Round(_baseBorderRadius * _currentDpiScale);
                    UpdateRegion();
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue(1f)]
        [Description("Thickness of the border outline.")]
        public float BorderThickness
        {
            get => _borderThickness;
            set
            {
                float clamped = Math.Max(0.5f, value);
                if (Math.Abs(_borderThickness - clamped) > 0.01f)
                {
                    _borderThickness = clamped;
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue(typeof(Color), "Empty")]
        [Description("Custom border color override. If empty, the active theme border color is used.")]
        public Color BorderColor
        {
            get => _customBorderColor;
            set
            {
                if (_customBorderColor != value)
                {
                    _customBorderColor = value;
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue(typeof(Color), "Empty")]
        [Description("Custom background color override. If empty, the active theme background color is used.")]
        public Color CustomBackColor
        {
            get => _customBackColor;
            set
            {
                if (_customBackColor != value)
                {
                    _customBackColor = value;
                    UpdateThemeColors();
                    Invalidate();
                }
            }
        }

        [Browsable(false)]
        public float DpiScale => _currentDpiScale;

        public PanelControl()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            UpdateThemeColors();

            ZeroTheme.ThemeChanged += OnThemeChanged;
            ZeroUIConfig.CornerStyleChanged += OnCornerStyleChanged;
        }

        public void ApplyDpiScaling(float scaleFactor)
        {
            if (scaleFactor <= 0f) scaleFactor = 1.0f;
            _currentDpiScale = scaleFactor;
            _borderRadius = (int)Math.Round(_baseBorderRadius * _currentDpiScale);
            UpdateRegion();
            Invalidate();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            float factor = ZeroDpi.GetScaleFactor(this);
            if (Math.Abs(factor - _currentDpiScale) > 0.001f)
            {
                ApplyDpiScaling(factor);
            }
            UpdateThemeColors();
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            UpdateRegion();
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            if (IsDisposed) return;
            UpdateThemeColors();
            Invalidate();
        }

        private void OnCornerStyleChanged(object? sender, EventArgs e)
        {
            if (IsDisposed) return;
            UpdateRegion();
            Invalidate();
        }

        private void UpdateThemeColors()
        {
            if (_customBackColor != Color.Empty)
            {
                BackColor = _customBackColor;
                return;
            }

            var palette = ZeroTheme.Colors;
            BackColor = _panelType switch
            {
                ZeroPanelType.Surface => palette.Surface,
                ZeroPanelType.Card => palette.CardBackground,
                ZeroPanelType.Header => palette.HeaderBackground,
                ZeroPanelType.Transparent => Color.Transparent,
                _ => palette.Surface
            };
        }

        private void UpdateRegion()
        {
            int effRadius = ZeroUIConfig.GetEffectiveRadius(_borderRadius);
            if (effRadius > 0 && Width > effRadius * 2 && Height > effRadius * 2)
            {
                using var path = ZeroUIConfig.CreateRoundedRectangle(new Rectangle(0, 0, Width, Height), effRadius);
                Region = new Region(path);
            }
            else
            {
                Region = null;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var palette = ZeroTheme.Colors;
            Color effectiveBorderColor = _customBorderColor != Color.Empty ? _customBorderColor : palette.Border;
            Color effectiveBgColor = BackColor;

            int effRadius = ZeroUIConfig.GetEffectiveRadius(_borderRadius);
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // 1. Draw Background
            if (effectiveBgColor != Color.Transparent)
            {
                using var bgBrush = new SolidBrush(effectiveBgColor);
                if (effRadius > 0)
                {
                    using var path = ZeroUIConfig.CreateRoundedRectangle(rect, effRadius);
                    g.FillPath(bgBrush, path);
                }
                else
                {
                    g.FillRectangle(bgBrush, new Rectangle(0, 0, Width, Height));
                }
            }

            // 2. Draw Border
            if (_borderStyle != ZeroPanelBorderStyle.None && _borderThickness > 0)
            {
                using var pen = new Pen(effectiveBorderColor, _borderThickness)
                {
                    Alignment = PenAlignment.Inset,
                    DashStyle = _borderStyle switch
                    {
                        ZeroPanelBorderStyle.Dotted => DashStyle.Dot,
                        ZeroPanelBorderStyle.Dashed => DashStyle.Dash,
                        _ => DashStyle.Solid
                    }
                };

                if (effRadius > 0)
                {
                    using var path = ZeroUIConfig.CreateRoundedRectangle(rect, effRadius);
                    g.DrawPath(pen, path);
                }
                else
                {
                    g.DrawRectangle(pen, rect);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ZeroTheme.ThemeChanged -= OnThemeChanged;
                ZeroUIConfig.CornerStyleChanged -= OnCornerStyleChanged;
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Backward-compatibility alias for <see cref="PanelControl"/>.
    /// </summary>
    [Obsolete("ZeroPanelControl is deprecated. Use PanelControl instead.")]
    public class ZeroPanelControl : PanelControl
    {
    }
}
