using System;
using System.Windows;
using ZeroUI.Core.Theme;

namespace ZeroUI.Wpf.Theme
{
    /// <summary>
    /// Master Entry Point for ZeroUI Theme & Skin Engine.
    /// Provides 1-line app-wide styling and real-time DevExpress-style skin switching.
    /// </summary>
    public static class ZeroThemeEngine
    {
        public static bool IsDark => ZeroWpfTheme.IsDark;
        public static ZeroSkin CurrentSkin => ZeroSkinManager.CurrentSkin;

        public static void Initialize(Application? app = null, string defaultSkin = "obsidian_dark")
        {
            ZeroWpfStyles.ApplyStyles(app);
            ZeroSkinManager.ApplySkin(defaultSkin);
        }

        public static void ApplySkin(string skinName) => ZeroSkinManager.ApplySkin(skinName);
        public static void ApplySkin(ZeroSkin skin) => ZeroSkinManager.ApplySkin(skin);

        public static void ToggleTheme()
        {
            if (ZeroWpfTheme.IsDark)
            {
                ApplySkin("clean_light");
            }
            else
            {
                ApplySkin("obsidian_dark");
            }
        }
    }
}
