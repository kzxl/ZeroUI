using System;
using System.Drawing;

namespace ZeroUI.WinForms.Theme
{
    public enum ZeroThemeMode
    {
        Light,
        Dark
    }

    public class ZeroThemePalette
    {
        public Color Background { get; set; }
        public Color Surface { get; set; }
        public Color CardBackground { get; set; }
        public Color TextPrimary { get; set; }
        public Color TextSecondary { get; set; }
        public Color Border { get; set; }
        public Color Primary { get; set; }
        public Color PrimaryHover { get; set; }
        public Color Success { get; set; }
        public Color Warning { get; set; }
        public Color Danger { get; set; }
        public Color Info { get; set; }
        public Color Hover { get; set; }
        public Color HeaderBackground { get; set; }
    }

    /// <summary>
    /// Unified Design Token & Theme Engine for ZeroUI supporting Light and Obsidian Dark modes.
    /// </summary>
    public static class ZeroTheme
    {
        private static ZeroThemeMode _currentMode = ZeroThemeMode.Light;

        public static event EventHandler? ThemeChanged;

        public static readonly ZeroThemePalette Light = new ZeroThemePalette
        {
            Background = Color.FromArgb(245, 246, 250),
            Surface = Color.FromArgb(255, 255, 255),
            CardBackground = Color.FromArgb(255, 255, 255),
            TextPrimary = Color.FromArgb(17, 24, 39),
            TextSecondary = Color.FromArgb(107, 114, 128),
            Border = Color.FromArgb(229, 231, 235),
            Primary = Color.FromArgb(79, 70, 229),
            PrimaryHover = Color.FromArgb(99, 102, 241),
            Success = Color.FromArgb(16, 185, 129),
            Warning = Color.FromArgb(245, 158, 11),
            Danger = Color.FromArgb(239, 68, 68),
            Info = Color.FromArgb(59, 130, 246),
            Hover = Color.FromArgb(243, 244, 246),
            HeaderBackground = Color.FromArgb(249, 250, 251)
        };

        public static readonly ZeroThemePalette Dark = new ZeroThemePalette
        {
            Background = Color.FromArgb(15, 23, 42),       // Slate 900
            Surface = Color.FromArgb(30, 41, 59),         // Slate 800
            CardBackground = Color.FromArgb(30, 41, 59),
            TextPrimary = Color.FromArgb(248, 250, 252),   // Slate 50
            TextSecondary = Color.FromArgb(148, 163, 184), // Slate 400
            Border = Color.FromArgb(51, 65, 85),          // Slate 700
            Primary = Color.FromArgb(99, 102, 241),       // Indigo 500
            PrimaryHover = Color.FromArgb(129, 140, 248),
            Success = Color.FromArgb(52, 211, 153),
            Warning = Color.FromArgb(251, 191, 36),
            Danger = Color.FromArgb(248, 113, 113),
            Info = Color.FromArgb(96, 165, 250),
            Hover = Color.FromArgb(51, 65, 85),
            HeaderBackground = Color.FromArgb(24, 33, 47)
        };

        public static ZeroThemeMode CurrentMode
        {
            get => _currentMode;
            set
            {
                if (_currentMode != value)
                {
                    _currentMode = value;
                    ThemeChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        public static ZeroThemePalette Colors => _currentMode == ZeroThemeMode.Dark ? Dark : Light;

        public static bool IsDark => _currentMode == ZeroThemeMode.Dark;

        public static void ToggleTheme()
        {
            CurrentMode = (_currentMode == ZeroThemeMode.Light) ? ZeroThemeMode.Dark : ZeroThemeMode.Light;
        }
    }
}
