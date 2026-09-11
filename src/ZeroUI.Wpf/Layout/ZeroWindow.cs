using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shell;
using ZeroUI.Core.Theme;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Layout
{
    /// <summary>
    /// Modern frameless enterprise window utilizing WindowChrome with customizable title bar,
    /// dynamic dark/light skin binding, and zero-distortion client area.
    /// </summary>
    public class ZeroWindow : Window, IZeroSkinnable
    {
        public static readonly DependencyProperty UseDefaultSkinProperty =
            DependencyProperty.Register(
                nameof(UseDefaultSkin),
                typeof(bool),
                typeof(ZeroWindow),
                new PropertyMetadata(true, OnSkinPropertyChanged));

        public static readonly DependencyProperty CustomSkinProperty =
            DependencyProperty.Register(
                nameof(CustomSkin),
                typeof(ZeroSkin),
                typeof(ZeroWindow),
                new PropertyMetadata(null, OnSkinPropertyChanged));

        public static readonly DependencyProperty TitleBarProperty =
            DependencyProperty.Register(
                nameof(TitleBar),
                typeof(ZeroTitleBar),
                typeof(ZeroWindow),
                new PropertyMetadata(null));

        public bool UseDefaultSkin
        {
            get => (bool)GetValue(UseDefaultSkinProperty);
            set => SetValue(UseDefaultSkinProperty, value);
        }

        public ZeroSkin? CustomSkin
        {
            get => (ZeroSkin?)GetValue(CustomSkinProperty);
            set => SetValue(CustomSkinProperty, value);
        }

        public ZeroSkin EffectiveSkin => ZeroSkinManager.ResolveSkin(this);

        public ZeroTitleBar? TitleBar
        {
            get => (ZeroTitleBar?)GetValue(TitleBarProperty);
            set => SetValue(TitleBarProperty, value);
        }

        static ZeroWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZeroWindow), new FrameworkPropertyMetadata(typeof(ZeroWindow)));
        }

        public ZeroWindow()
        {
            SetResourceReference(BackgroundProperty, "ZeroUI.BgPrimary");
            SetResourceReference(ForegroundProperty, "ZeroUI.TextPrimary");
            SetResourceReference(BorderBrushProperty, "ZeroUI.BorderDefault");
            BorderThickness = new Thickness(1);
            WindowStyle = WindowStyle.None;

            var chrome = new WindowChrome
            {
                CaptionHeight = 36,
                CornerRadius = new CornerRadius(0),
                GlassFrameThickness = new Thickness(0),
                ResizeBorderThickness = new Thickness(6),
                UseAeroCaptionButtons = false
            };
            WindowChrome.SetWindowChrome(this, chrome);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ZeroWpfTheme.ThemeChanged += OnThemeChangedInternal;
            OnThemeChanged();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ZeroWpfTheme.ThemeChanged -= OnThemeChangedInternal;
        }

        private void OnThemeChangedInternal()
        {
            if (UseDefaultSkin)
            {
                OnThemeChanged();
            }
        }

        public virtual void ApplySkin(ZeroSkin skin)
        {
            if (skin == null) throw new ArgumentNullException(nameof(skin));
            CustomSkin = skin;
            UseDefaultSkin = false;
            OnThemeChanged();
        }

        protected virtual void OnThemeChanged()
        {
            // Dynamic resources update automatically
        }

        private static void OnSkinPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZeroWindow win)
            {
                win.OnThemeChanged();
            }
        }
    }
}
