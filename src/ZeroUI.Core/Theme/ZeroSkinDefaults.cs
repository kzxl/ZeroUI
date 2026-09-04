using System.Collections.Generic;

namespace ZeroUI.Core.Theme
{
    /// <summary>
    /// Built-in skins catalog modeled after DevExpress Skin Gallery.
    /// Provides curated Obsidian, Clean Slate, Arctic, Cyberpunk, and Emerald palettes.
    /// </summary>
    public static class ZeroSkinDefaults
    {
        public static readonly ZeroSkin ObsidianDark = new ZeroSkin(
            "obsidian_dark",
            "Obsidian Dark",
            isDark: true,
            new ZeroPaletteTokens
            {
                BgPrimary = "#11131F",
                BgCard = "#181A28",
                BgInput = "#1D2034",
                BgHover = "#262A44",
                BgActive = "#32385C",
                BgDisabled = "#151824",
                BorderDefault = "#2E344E",
                BorderSubtle = "#23273C",
                BorderFocus = "#818CF8",
                TextPrimary = "#F1F5F9",
                TextSecondary = "#94A3B8",
                TextMuted = "#64748B",
                PrimaryAccent = "#818CF8",
                PrimaryAccentDark = "#6366F1",
                SecondaryAccent = "#A78BFA",
                Success = "#A6E3A1",
                Warning = "#F9E2AF",
                Danger = "#F38BA8",
                Info = "#89DCEB",
                SelectionBackground = "#1E3A8A",
                SelectionForeground = "#FFFFFF",
                ScrollThumb = "#334155",
                ScrollThumbHover = "#64748B",
                ScrollTrack = "Transparent"
            });

        public static readonly ZeroSkin CleanLight = new ZeroSkin(
            "clean_light",
            "Clean Light",
            isDark: false,
            new ZeroPaletteTokens
            {
                BgPrimary = "#F8F9FC",
                BgCard = "#FFFFFF",
                BgInput = "#F1F3F9",
                BgHover = "#E8EDF8",
                BgActive = "#D9E2F5",
                BgDisabled = "#E2E8F0",
                BorderDefault = "#DCE1EE",
                BorderSubtle = "#EAEFF8",
                BorderFocus = "#4F46E5",
                TextPrimary = "#0F172A",
                TextSecondary = "#475569",
                TextMuted = "#94A3B8",
                PrimaryAccent = "#4F46E5",
                PrimaryAccentDark = "#4338CA",
                SecondaryAccent = "#7C3AED",
                Success = "#16A34A",
                Warning = "#D97706",
                Danger = "#DC2626",
                Info = "#0284C7",
                SelectionBackground = "#DBEAFE",
                SelectionForeground = "#1E3A8A",
                ScrollThumb = "#CBD5E1",
                ScrollThumbHover = "#94A3B8",
                ScrollTrack = "Transparent"
            });

        public static readonly ZeroSkin NordicFrost = new ZeroSkin(
            "nordic_frost",
            "Nordic Frost",
            isDark: true,
            new ZeroPaletteTokens
            {
                BgPrimary = "#0F172A",
                BgCard = "#1E293B",
                BgInput = "#243044",
                BgHover = "#334155",
                BgActive = "#3B4A63",
                BgDisabled = "#151F30",
                BorderDefault = "#334155",
                BorderSubtle = "#1E293B",
                BorderFocus = "#38BDF8",
                TextPrimary = "#F8FAFC",
                TextSecondary = "#94A3B8",
                TextMuted = "#64748B",
                PrimaryAccent = "#38BDF8",
                PrimaryAccentDark = "#0284C7",
                SecondaryAccent = "#2DD4BF",
                Success = "#34D399",
                Warning = "#FBBF24",
                Danger = "#F87171",
                Info = "#38BDF8",
                SelectionBackground = "#0369A1",
                SelectionForeground = "#FFFFFF",
                ScrollThumb = "#475569",
                ScrollThumbHover = "#64748B",
                ScrollTrack = "Transparent"
            });

        public static readonly ZeroSkin CyberpunkNeon = new ZeroSkin(
            "cyberpunk_neon",
            "Cyberpunk Neon",
            isDark: true,
            new ZeroPaletteTokens
            {
                BgPrimary = "#0E0A1F",
                BgCard = "#181236",
                BgInput = "#211847",
                BgHover = "#2D215E",
                BgActive = "#3A2B78",
                BgDisabled = "#130E29",
                BorderDefault = "#3A2A6E",
                BorderSubtle = "#241849",
                BorderFocus = "#F43F5E",
                TextPrimary = "#F8FAFC",
                TextSecondary = "#C084FC",
                TextMuted = "#7E22CE",
                PrimaryAccent = "#F43F5E",
                PrimaryAccentDark = "#E11D48",
                SecondaryAccent = "#06B6D4",
                Success = "#10B981",
                Warning = "#F59E0B",
                Danger = "#EF4444",
                Info = "#06B6D4",
                SelectionBackground = "#701A75",
                SelectionForeground = "#FFFFFF",
                ScrollThumb = "#581C87",
                ScrollThumbHover = "#7E22CE",
                ScrollTrack = "Transparent"
            });

        public static readonly ZeroSkin EmeraldEnterprise = new ZeroSkin(
            "emerald_enterprise",
            "Emerald Enterprise",
            isDark: true,
            new ZeroPaletteTokens
            {
                BgPrimary = "#06140E",
                BgCard = "#0D241B",
                BgInput = "#133327",
                BgHover = "#1A4233",
                BgActive = "#215441",
                BgDisabled = "#091C14",
                BorderDefault = "#1F4D3C",
                BorderSubtle = "#122E23",
                BorderFocus = "#10B981",
                TextPrimary = "#ECFDF5",
                TextSecondary = "#6EE7B7",
                TextMuted = "#059669",
                PrimaryAccent = "#10B981",
                PrimaryAccentDark = "#059669",
                SecondaryAccent = "#34D399",
                Success = "#34D399",
                Warning = "#FBBF24",
                Danger = "#F87171",
                Info = "#38BDF8",
                SelectionBackground = "#065F46",
                SelectionForeground = "#FFFFFF",
                ScrollThumb = "#047857",
                ScrollThumbHover = "#059669",
                ScrollTrack = "Transparent"
            });

        public static IEnumerable<ZeroSkin> GetAllDefaults()
        {
            yield return ObsidianDark;
            yield return CleanLight;
            yield return NordicFrost;
            yield return CyberpunkNeon;
            yield return EmeraldEnterprise;
        }
    }
}
