using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ZeroUI.WinForms.Icons;
using ZeroUI.WinForms.Rendering;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Navigation
{
    /// <summary>
    /// Modern theme-aware application top menu bar adhering to ZeroUI design tokens.
    /// Features auto-skinning, crisp ClearType typography, dark/light theme reactivity,
    /// and fluent menu-building extension methods.
    /// Direct drop-in replacement for standard <see cref="System.Windows.Forms.MenuStrip"/>.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Navigation")]
    [Description("Modern theme-aware top menu bar with auto-skinning and fluent APIs")]
    [ToolboxBitmap(typeof(ZeroIcons), "ZeroDefaultControl.bmp")]
    public class MenuBarControl : MenuStrip
    {
        public MenuBarControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);

            Renderer = new MenuRenderer();
            GripStyle = ToolStripGripStyle.Hidden;
            ShowItemToolTips = true;
            Font = ZeroFontCache.Get("Segoe UI", 9.25f, FontStyle.Regular);

            UpdateThemeColors();

            ZeroTheme.ThemeChanged += OnThemeChanged;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateThemeColors();
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            if (IsDisposed) return;
            UpdateThemeColors();
            Invalidate();
        }

        private void UpdateThemeColors()
        {
            var palette = ZeroTheme.Colors;
            BackColor = palette.Surface;
            ForeColor = palette.TextPrimary;
        }

        #region Fluent API Helpers

        /// <summary>
        /// Adds a top-level menu category (e.g. File, Edit, View, Tools, Help).
        /// </summary>
        public ToolStripMenuItem AddMenu(string title)
        {
            var menu = new ToolStripMenuItem(title)
            {
                Font = Font,
                ForeColor = ForeColor
            };
            Items.Add(menu);
            return menu;
        }

        /// <summary>
        /// Adds a child menu action item to a parent menu.
        /// </summary>
        public ToolStripMenuItem AddMenuItem(ToolStripMenuItem parent, string text, Action? onClick = null, Keys shortcut = Keys.None, Image? icon = null)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));

            var item = new ToolStripMenuItem(text, icon, (s, e) => onClick?.Invoke())
            {
                Font = Font
            };

            if (shortcut != Keys.None)
            {
                item.ShortcutKeys = shortcut;
                item.ShowShortcutKeys = true;
            }

            parent.DropDownItems.Add(item);
            return item;
        }

        /// <summary>
        /// Adds a visual horizontal divider separator to a parent menu.
        /// </summary>
        public ToolStripSeparator AddSeparator(ToolStripMenuItem parent)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));

            var sep = new ToolStripSeparator();
            parent.DropDownItems.Add(sep);
            return sep;
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ZeroTheme.ThemeChanged -= OnThemeChanged;
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Backward-compatibility alias for <see cref="MenuBarControl"/>.
    /// </summary>
    [Obsolete("ZeroMenuBarControl is deprecated. Use MenuBarControl instead.")]
    public class ZeroMenuBarControl : MenuBarControl
    {
    }

    /// <summary>
    /// Backward-compatibility alias for <see cref="MenuBarControl"/>.
    /// </summary>
    [Obsolete("ZeroMenuStrip is deprecated. Use MenuBarControl instead.")]
    public class ZeroMenuStrip : MenuBarControl
    {
    }
}
