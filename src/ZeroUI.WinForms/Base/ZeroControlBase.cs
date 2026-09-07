using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ZeroUI.Core.Theme;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Base
{
    /// <summary>
    /// High-performance architectural base class for all ZeroUI WinForms controls.
    /// Provides built-in double buffering, zero-flicker GDI+ rendering, and automatic
    /// skin lifecycle synchronization with local override capabilities.
    /// </summary>
    [ToolboxItem(false)]
    public abstract class ZeroControlBase : Control, IZeroSkinnable
    {
        private bool _useDefaultSkin = true;
        private ZeroSkin? _customSkin;
        private readonly Action<ZeroSkin> _skinChangedHandler;
        private readonly EventHandler _themeChangedHandler;
        private bool _isDisposed;

        #region Properties & Skinnable Contract

        [Category("Skin")]
        [DefaultValue(true)]
        [Description("Determines whether this control consumes the global ZeroSkinManager theme or a localized CustomSkin.")]
        public bool UseDefaultSkin
        {
            get => _useDefaultSkin;
            set
            {
                if (_useDefaultSkin != value)
                {
                    _useDefaultSkin = value;
                    OnThemeChanged(EffectiveSkin);
                    Invalidate();
                }
            }
        }

        [Category("Skin")]
        [DefaultValue(null)]
        [Description("Localized custom skin applied when UseDefaultSkin is false.")]
        public ZeroSkin? CustomSkin
        {
            get => _customSkin;
            set
            {
                if (_customSkin != value)
                {
                    _customSkin = value;
                    if (!_useDefaultSkin)
                    {
                        OnThemeChanged(EffectiveSkin);
                        Invalidate();
                    }
                }
            }
        }

        [Browsable(false)]
        public ZeroSkin EffectiveSkin => ZeroSkinManager.ResolveSkin(this);

        /// <summary>
        /// Gets the current palette tokens corresponding to the effective skin.
        /// </summary>
        [Browsable(false)]
        protected ZeroPaletteTokens Palette => EffectiveSkin.Tokens;

        #endregion

        protected ZeroControlBase()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            DoubleBuffered = true;

            _skinChangedHandler = skin =>
            {
                if (_useDefaultSkin && !_isDisposed && IsHandleCreated)
                {
                    if (InvokeRequired)
                    {
                        try { BeginInvoke(new Action(() => { OnThemeChanged(skin); Invalidate(); })); }
                        catch (ObjectDisposedException) { }
                    }
                    else
                    {
                        OnThemeChanged(skin);
                        Invalidate();
                    }
                }
            };

            _themeChangedHandler = (s, e) =>
            {
                if (_useDefaultSkin && !_isDisposed && IsHandleCreated)
                {
                    OnThemeChanged(EffectiveSkin);
                    Invalidate();
                }
            };

            ZeroSkinManager.SkinChanged += _skinChangedHandler;
            ZeroTheme.ThemeChanged += _themeChangedHandler;

            OnThemeChanged(EffectiveSkin);
        }

        /// <summary>
        /// Applies the specified skin to this control and invalidates the surface.
        /// </summary>
        public virtual void ApplySkin(ZeroSkin skin)
        {
            if (skin == null) throw new ArgumentNullException(nameof(skin));
            _customSkin = skin;
            _useDefaultSkin = false;
            OnThemeChanged(skin);
            Invalidate();
        }

        /// <summary>
        /// Invoked when the active theme is changed globally or locally.
        /// Override in derived controls to update internal GDI+ pens, brushes, or child controls.
        /// </summary>
        /// <param name="skin">The newly effective skin.</param>
        protected virtual void OnThemeChanged(ZeroSkin skin)
        {
            if (skin == null) return;
            BackColor = ColorTranslator.FromHtml(skin.Tokens.BgCard);
            ForeColor = ColorTranslator.FromHtml(skin.Tokens.TextPrimary);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                _isDisposed = true;
                ZeroSkinManager.SkinChanged -= _skinChangedHandler;
                ZeroTheme.ThemeChanged -= _themeChangedHandler;
            }
            base.Dispose(disposing);
        }
    }
}
