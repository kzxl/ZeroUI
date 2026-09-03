using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Overlays
{
    /// <summary>
    /// Modern anti-aliased context menu with rounded highlight pills, shortcut keys,
    /// danger action styling, checkable items, submenus, and 100% theme reactivity.
    /// Can be assigned directly to any WinForms control's ContextMenuStrip property.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Overlays")]
    [Description("Modern anti-aliased context menu strip with pill highlights and theme support")]
    public class ZeroContextMenu : ContextMenuStrip
    {
        public ZeroContextMenu()
        {
            Renderer = new ZeroContextMenuRenderer();
            ShowImageMargin = false;
            ShowCheckMargin = false;
            DropShadowEnabled = true;
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            Padding = new Padding(4, 6, 4, 6);
            BackColor = ZeroTheme.Colors.CardBackground;

            ZeroTheme.ThemeChanged += (s, e) =>
            {
                BackColor = ZeroTheme.Colors.CardBackground;
                Invalidate();
            };
        }

        public ZeroMenuItem AddAction(string text, Action onClick, string? shortcut = null, string? icon = null)
        {
            var item = new ZeroMenuItem(text, onClick)
            {
                ShortcutHint = shortcut,
                Glyph = icon
            };
            Items.Add(item);
            return item;
        }

        public ZeroMenuItem AddDangerAction(string text, Action onClick, string? shortcut = null, string? icon = null)
        {
            var item = new ZeroMenuItem(text, onClick)
            {
                ShortcutHint = shortcut,
                Glyph = icon,
                IsDanger = true
            };
            Items.Add(item);
            return item;
        }

        public ZeroMenuItem AddCheckable(string text, bool isChecked, Action<bool> onToggle, string? icon = null)
        {
            var item = new ZeroMenuItem(text, null)
            {
                CheckOnClick = true,
                Checked = isChecked,
                Glyph = icon
            };
            item.CheckedChanged += (s, e) => onToggle(item.Checked);
            Items.Add(item);
            return item;
        }

        public ToolStripSeparator AddSeparator()
        {
            var sep = new ToolStripSeparator();
            Items.Add(sep);
            return sep;
        }

        public ZeroMenuItem AddSubMenu(string text, string? icon = null)
        {
            var item = new ZeroMenuItem(text, null)
            {
                Glyph = icon,
                DropDown = new ZeroContextMenu()
            };
            Items.Add(item);
            return item;
        }
    }

    /// <summary>
    /// Custom MenuItem supporting danger state, glyph emojis, shortcut hints, and badge tags.
    /// </summary>
    public class ZeroMenuItem : ToolStripMenuItem
    {
        public bool IsDanger { get; set; } = false;
        public string? Glyph { get; set; }
        public string? ShortcutHint { get; set; }
        public string? BadgeText { get; set; }
        public Color? BadgeColor { get; set; }

        public ZeroMenuItem() : base() { }

        public ZeroMenuItem(string text, Action? onClick) : base(text)
        {
            if (onClick != null) Click += (s, e) => onClick();
        }

        public override Size GetPreferredSize(Size constrainingSize)
        {
            var baseSize = base.GetPreferredSize(constrainingSize);
            int w = baseSize.Width + 36;
            if (!string.IsNullOrEmpty(ShortcutHint)) w += 65;
            if (!string.IsNullOrEmpty(BadgeText)) w += 40;
            if (!string.IsNullOrEmpty(Glyph)) w += 20;
            return new Size(Math.Max(180, w), Math.Max(30, baseSize.Height + 6));
        }

        public ZeroMenuItem AddSubAction(string text, Action onClick, string? shortcut = null, string? icon = null)
        {
            var item = new ZeroMenuItem(text, onClick)
            {
                ShortcutHint = shortcut,
                Glyph = icon
            };
            DropDownItems.Add(item);
            return item;
        }
    }

    /// <summary>
    /// Custom ToolStripRenderer rendering anti-aliased rounded pills, theme borders, and typography.
    /// </summary>
    public class ZeroContextMenuRenderer : ToolStripRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var palette = ZeroTheme.Colors;
            using var brushBg = new SolidBrush(palette.CardBackground);
            g.FillRectangle(brushBg, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var palette = ZeroTheme.Colors;
            using var penBorder = new Pen(palette.Border, 1f);
            Rectangle r = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            g.DrawRectangle(penBorder, r);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var palette = ZeroTheme.Colors;
            var item = e.Item;
            var zeroItem = item as ZeroMenuItem;

            if (item.Selected && item.Enabled)
            {
                Rectangle r = new Rectangle(4, 2, item.Width - 8, item.Height - 4);
                using var path = CreateRoundedRect(r, 5);

                Color hoverBg = (zeroItem != null && zeroItem.IsDanger)
                    ? Color.FromArgb(40, palette.Danger)
                    : Color.FromArgb(25, palette.Primary);

                using var brushHov = new SolidBrush(hoverBg);
                g.FillPath(brushHov, path);

                // Subtle left pill accent on hover
                Color accentBarColor = (zeroItem != null && zeroItem.IsDanger) ? palette.Danger : palette.Primary;
                using var brushBar = new SolidBrush(accentBarColor);
                g.FillRectangle(brushBar, new Rectangle(r.X, r.Y + 3, 3, r.Height - 6));
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var palette = ZeroTheme.Colors;
            var item = e.Item;
            var zeroItem = item as ZeroMenuItem;

            // Determine text color
            Color textColor;
            if (!item.Enabled)
            {
                textColor = Color.FromArgb(100, palette.TextSecondary);
            }
            else if (zeroItem != null && zeroItem.IsDanger)
            {
                textColor = item.Selected ? palette.Danger : Color.FromArgb(248, 113, 113); // Soft red
            }
            else if (item.Selected)
            {
                textColor = palette.Primary;
            }
            else
            {
                textColor = palette.TextPrimary;
            }

            int curX = 12;

            // 1. Draw Checkmark (if checked)
            if (item is ToolStripMenuItem tsmi && tsmi.Checked)
            {
                using var fontCheck = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                using var brushCheck = new SolidBrush(palette.Primary);
                g.DrawString("✔", fontCheck, brushCheck, curX, (item.Height - 16) / 2);
                curX += 18;
            }

            // 2. Draw Glyph (if specified)
            if (zeroItem != null && !string.IsNullOrEmpty(zeroItem.Glyph))
            {
                using var fontGlyph = new Font("Segoe UI Emoji", 9.5f);
                using var brushGlyph = new SolidBrush(textColor);
                g.DrawString(zeroItem.Glyph, fontGlyph, brushGlyph, curX, (item.Height - 18) / 2);
                curX += 22;
            }

            // 3. Draw Item Text
            using (var fontText = new Font(item.Font.FontFamily, 9f, FontStyle.Regular))
            using (var brushText = new SolidBrush(textColor))
            {
                g.DrawString(item.Text, fontText, brushText, curX, (item.Height - 16) / 2);
            }

            // 4. Draw Badge Tag (if any)
            if (zeroItem != null && !string.IsNullOrEmpty(zeroItem.BadgeText))
            {
                int textW = (int)g.MeasureString(item.Text, item.Font).Width;
                int badgeX = curX + textW + 8;
                int badgeW = 28;
                var badgeRect = new Rectangle(badgeX, (item.Height - 16) / 2, badgeW, 16);

                Color bColor = zeroItem.BadgeColor ?? palette.Primary;
                using var brushBadgeBg = new SolidBrush(Color.FromArgb(30, bColor));
                using var pathBadge = CreateRoundedRect(badgeRect, 3);
                g.FillPath(brushBadgeBg, pathBadge);

                using var fontBadge = new Font(item.Font.FontFamily, 7.5f, FontStyle.Bold);
                using var brushBadgeText = new SolidBrush(bColor);
                var sfB = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(zeroItem.BadgeText, fontBadge, brushBadgeText, badgeRect, sfB);
            }

            // 5. Draw Right-aligned Shortcut Hint
            string? shortcut = zeroItem?.ShortcutHint ?? (item as ToolStripMenuItem)?.ShortcutKeyDisplayString;
            if (!string.IsNullOrEmpty(shortcut))
            {
                using var fontShort = new Font(item.Font.FontFamily, 8f, FontStyle.Regular);
                using var brushShort = new SolidBrush(palette.TextSecondary);
                var sf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                Rectangle shortRect = new Rectangle(item.Width - 100, 0, 88, item.Height);
                g.DrawString(shortcut, fontShort, brushShort, shortRect, sf);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var g = e.Graphics;
            var palette = ZeroTheme.Colors;

            int y = e.Item.Height / 2;
            using var penDiv = new Pen(Color.FromArgb(40, palette.Border), 1f);
            g.DrawLine(penDiv, 12, y, e.Item.Width - 12, y);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var palette = ZeroTheme.Colors;
            Color arrowColor = (e.Item != null && e.Item.Selected) ? palette.Primary : palette.TextSecondary;

            using var fontArrow = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var brushArrow = new SolidBrush(arrowColor);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("›", fontArrow, brushArrow, e.ArrowRectangle, sf);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            // Do not render legacy XP image stripe
        }

        private static GraphicsPath CreateRoundedRect(Rectangle r, int radius) =>
            ZeroUIConfig.CreateRoundedRectangle(r, radius);
    }
}
