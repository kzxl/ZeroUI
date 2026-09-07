using System;
using System.Windows;
using ZeroUI.Core.Theme;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Base
{
    /// <summary>
    /// Architectural base class for high-performance direct vector rendering controls (FrameworkElement / OnRender) in ZeroUI WPF.
    /// Manages theme lifecycle invalidation, memory-safe event subscription, and localized skin scoping.
    /// </summary>
    public abstract class ZeroWpfVisualBase : FrameworkElement, IZeroSkinnable
    {
        public static readonly DependencyProperty UseDefaultSkinProperty =
            DependencyProperty.Register(
                nameof(UseDefaultSkin),
                typeof(bool),
                typeof(ZeroWpfVisualBase),
                new PropertyMetadata(true, OnSkinPropertyChanged));

        public static readonly DependencyProperty CustomSkinProperty =
            DependencyProperty.Register(
                nameof(CustomSkin),
                typeof(ZeroSkin),
                typeof(ZeroWpfVisualBase),
                new PropertyMetadata(null, OnSkinPropertyChanged));

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

        protected ZeroWpfVisualBase()
        {
            ClipToBounds = true;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ZeroWpfTheme.ThemeChanged += OnThemeChangedInternal;
            OnThemeChanged();
            InvalidateVisual();
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
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Applies a localized skin directly to this visualizer and invalidates the surface.
        /// </summary>
        public virtual void ApplySkin(ZeroSkin skin)
        {
            if (skin == null) throw new ArgumentNullException(nameof(skin));
            CustomSkin = skin;
            UseDefaultSkin = false;
            OnThemeChanged();
            InvalidateVisual();
        }

        /// <summary>
        /// Invoked when the active theme is modified globally or locally. Override to update local geometry, brushes, or pens.
        /// </summary>
        protected virtual void OnThemeChanged()
        {
        }

        private static void OnSkinPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZeroWpfVisualBase visual)
            {
                visual.OnThemeChanged();
                visual.InvalidateVisual();
            }
        }
    }
}
