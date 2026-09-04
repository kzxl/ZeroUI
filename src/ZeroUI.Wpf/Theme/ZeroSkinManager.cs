using System;
using System.Collections.Generic;
using ZeroUI.Core.Theme;

namespace ZeroUI.Wpf.Theme
{
    /// <summary>
    /// DevExpress-style Skin Manager for ZeroUI.
    /// Manages the catalog of skins, runtime skin registration, and instant application across all controls.
    /// </summary>
    public static class ZeroSkinManager
    {
        private static readonly Dictionary<string, ZeroSkin> _skins = new Dictionary<string, ZeroSkin>(StringComparer.OrdinalIgnoreCase);
        private static ZeroSkin _currentSkin = ZeroSkinDefaults.ObsidianDark;

        public static event Action<ZeroSkin>? SkinChanged;

        public static IReadOnlyCollection<ZeroSkin> AvailableSkins => _skins.Values;
        public static ZeroSkin CurrentSkin => _currentSkin;

        static ZeroSkinManager()
        {
            foreach (var skin in ZeroSkinDefaults.GetAllDefaults())
            {
                _skins[skin.Name] = skin;
            }
        }

        public static void RegisterSkin(ZeroSkin skin)
        {
            if (skin == null) throw new ArgumentNullException(nameof(skin));
            _skins[skin.Name] = skin;
        }

        public static void ApplySkin(string skinName)
        {
            if (_skins.TryGetValue(skinName, out var skin))
            {
                ApplySkin(skin);
            }
            else
            {
                throw new KeyNotFoundException($"ZeroSkin '{skinName}' is not registered in ZeroSkinManager.");
            }
        }

        public static void ApplySkin(ZeroSkin skin)
        {
            if (skin == null) throw new ArgumentNullException(nameof(skin));
            _currentSkin = skin;
            ZeroWpfTheme.ApplyPalette(skin.Tokens, skin.IsDark);
            SkinChanged?.Invoke(skin);
        }
    }
}
