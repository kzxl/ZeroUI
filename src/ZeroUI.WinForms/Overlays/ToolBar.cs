using System;

using ZeroUI.WinForms.Icons;using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Overlays
{
    public enum ToolbarItemType

    {
        Button,
        Separator,
        Spacer,
        Dropdown
    }

    public abstract class ToolbarItem
    {
        public string Text { get; set; } = "";
        public string? Glyph { get; set; }
        public string? Tooltip { get; set; }
        public string? ShortcutText { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        public object? Tag { get; set; }

        internal Rectangle Bounds;


        public event EventHandler? Click;

        internal void OnClick() => Click?.Invoke(this, EventArgs.Empty);
    }

    public class ToolbarButton : ToolbarItem
    {
        public bool IsPrimary { get; set; }
        public bool IsDanger { get; set; }
        public int? BadgeCount { get; set; }

        public ToolbarButton() { }

        public ToolbarButton(string text, string? glyph = null, EventHandler? onClick = null, string? shortcut = null)
        {
            Text = text;
            Glyph = glyph;
            ShortcutText = shortcut;
            if (onClick != null) Click += onClick;
        }
    }

    public class ToolbarSeparator : ToolbarItem
    {
        public ToolbarSeparator()
        {
            IsEnabled = false;
        }
    }

    public class ToolbarSpacer : ToolbarItem
    {
        public ToolbarSpacer()
        {
            IsEnabled = false;
        }
    }

    public class ToolbarDropdown : ToolbarItem
    {
        public event EventHandler? DropdownOpened;

        public ToolbarDropdown() { }

        public ToolbarDropdown(string text, string? glyph = null, EventHandler? onDropdown = null)
        {
            Text = text;
            Glyph = glyph;
            if (onDropdown != null) DropdownOpened += onDropdown;
        }

        internal void OnDropdown() => DropdownOpened?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Modern single-HWND flat enterprise action toolbar for ZeroUI.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Overlays")]
    [Description("Flat enterprise action toolbar with buttons, dividers, and elastic spacers")]
    [ToolboxBitmap(typeof(ZeroIcons), "ToolbarControl.bmp")]
    public class ToolbarControl : Control
    {

        private readonly List<ToolbarItem> _items = new List<ToolbarItem>();
        private Color _borderColor = Color.FromArgb(229, 231, 235);
        private int _itemHeight = 32;
        private ToolbarItem? _hoveredItem;
        private ToolbarItem? _pressedItem;
        private readonly ToolTip _toolTip = new ToolTip();

        public ToolbarControl()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);

            Dock = DockStyle.Top;
            Height = 44;
            BackColor = ZeroTheme.Colors.Surface;
            _borderColor = ZeroTheme.Colors.Border;
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            Padding = new Padding(8, 6, 8, 6);

            ZeroTheme.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            BackColor = ZeroTheme.Colors.Surface;
            _borderColor = ZeroTheme.Colors.Border;
            Invalidate();
        }

        [Browsable(false)]
        public List<ToolbarItem> Items => _items;

        [Category("Appearance")]
        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        [Category("Layout")]
        [DefaultValue(32)]
        public int ItemHeight
        {
            get => _itemHeight;
            set { _itemHeight = Math.Max(24, value); Invalidate(); }
        }

        public ToolbarButton AddButton(string text, string? glyph = null, EventHandler? onClick = null, string? shortcut = null)
        {
            var btn = new ToolbarButton(text, glyph, onClick, shortcut);
            _items.Add(btn);
            Invalidate();
            return btn;
        }

        public void AddSeparator()
        {
            _items.Add(new ToolbarSeparator());
            Invalidate();
        }

        public void AddSpacer()
        {
            _items.Add(new ToolbarSpacer());
            Invalidate();
        }

        public ToolbarDropdown AddDropdown(string text, string? glyph = null, EventHandler? onDropdown = null)
        {
            var dd = new ToolbarDropdown(text, glyph, onDropdown);
            _items.Add(dd);
            Invalidate();
            return dd;
        }

        public void Clear()
        {
            _items.Clear();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 1. Draw Background & Bottom Border
            using (var bgBrush = new SolidBrush(BackColor))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }
            using (var borderPen = new Pen(_borderColor, 1f))
            {
                g.DrawLine(borderPen, 0, Height - 1, Width, Height - 1);
            }

            // 2. Measure & Layout Items (Handling Left and Right Groups separated by Spacer)
            LayoutItems(g);

            // 3. Draw Items
            int centerY = Height / 2;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (!item.IsVisible || item is ToolbarSpacer) continue;

                if (item is ToolbarSeparator)
                {
                    int sepX = item.Bounds.X + (item.Bounds.Width / 2);
                    using var sepPen = new Pen(_borderColor, 1f);
                    g.DrawLine(sepPen, sepX, centerY - 10, sepX, centerY + 10);
                    continue;
                }

                bool isHovered = (item == _hoveredItem && item.IsEnabled);
                bool isPressed = (item == _pressedItem && item.IsEnabled);

                // Draw Button Background
                var palette = ZeroTheme.Colors;
                if (item is ToolbarButton btn && btn.IsPrimary)
                {
                    Color primaryBg = isPressed ? palette.PrimaryHover : (isHovered ? palette.PrimaryHover : palette.Primary);
                    using var path = CreateRoundedRectangle(item.Bounds, 6);
                    using var brush = new SolidBrush(primaryBg);
                    g.FillPath(brush, path);
                }
                else if (isPressed || isHovered)
                {
                    using var path = CreateRoundedRectangle(item.Bounds, 6);
                    using var brush = new SolidBrush(palette.Hover);
                    g.FillPath(brush, path);
                }

                // Draw Content (Glyph, Text, Shortcut, Dropdown Chevron)
                int contentX = item.Bounds.Left + 10;
                Color textColor = !item.IsEnabled ? palette.TextSecondary
                    : ((item is ToolbarButton b && b.IsPrimary) ? Color.White : palette.TextPrimary);

                // Glyph
                if (!string.IsNullOrEmpty(item.Glyph))
                {
                    Rectangle glyphRect = new Rectangle(contentX, item.Bounds.Top, 20, item.Bounds.Height);
                    TextRenderer.DrawText(g, item.Glyph, new Font("Segoe UI", 10.5f, FontStyle.Regular), glyphRect, textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                    contentX += 20;
                }

                // Text
                if (!string.IsNullOrEmpty(item.Text))
                {
                    Size textSize = TextRenderer.MeasureText(g, item.Text, Font);
                    Rectangle textRect = new Rectangle(contentX, item.Bounds.Top, textSize.Width + 4, item.Bounds.Height);
                    TextRenderer.DrawText(g, item.Text, Font, textRect, textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                    contentX += textSize.Width + 4;
                }

                // Dropdown Chevron (▾)
                if (item is ToolbarDropdown)
                {
                    Rectangle chevRect = new Rectangle(contentX, item.Bounds.Top, 14, item.Bounds.Height);
                    TextRenderer.DrawText(g, "▾", new Font("Segoe UI", 9f, FontStyle.Regular), chevRect, textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                }

                // Badge Count (if present)
                if (item is ToolbarButton buttonWithBadge && buttonWithBadge.BadgeCount.HasValue && buttonWithBadge.BadgeCount.Value > 0)
                {
                    string badgeStr = buttonWithBadge.BadgeCount.Value > 99 ? "99+" : buttonWithBadge.BadgeCount.Value.ToString();
                    using var badgeFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
                    Size badgeSize = TextRenderer.MeasureText(g, badgeStr, badgeFont);
                    int badgeW = Math.Max(16, badgeSize.Width + 6);
                    int badgeH = 16;
                    Rectangle badgeRect = new Rectangle(item.Bounds.Right - badgeW - 4, item.Bounds.Top + 4, badgeW, badgeH);

                    using var bPath = CreateRoundedRectangle(badgeRect, 8);
                    using var bBrush = new SolidBrush(Color.FromArgb(239, 68, 68));
                    g.FillPath(bBrush, bPath);

                    TextRenderer.DrawText(g, badgeStr, badgeFont, badgeRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
        }

        private void LayoutItems(Graphics g)
        {
            int centerY = Height / 2;
            int top = centerY - (_itemHeight / 2);

            int leftX = Padding.Left;
            int rightX = Width - Padding.Right;

            // Find if there is a Spacer
            int spacerIndex = -1;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] is ToolbarSpacer)
                {
                    spacerIndex = i;
                    break;
                }
            }

            // Measure and place left items
            int leftLimit = (spacerIndex == -1) ? _items.Count : spacerIndex;
            for (int i = 0; i < leftLimit; i++)
            {
                var item = _items[i];
                if (!item.IsVisible) continue;

                int itemW = MeasureItemWidth(g, item);
                item.Bounds = new Rectangle(leftX, top, itemW, _itemHeight);
                leftX += itemW + 4;
            }

            // Measure and place right items (in reverse order from the right side)
            if (spacerIndex != -1)
            {
                for (int i = _items.Count - 1; i > spacerIndex; i--)
                {
                    var item = _items[i];
                    if (!item.IsVisible) continue;

                    int itemW = MeasureItemWidth(g, item);
                    if (rightX - itemW < leftX + 8)
                    {
                        // Prevent overlap with left toolbar items on narrow screens
                        item.Bounds = Rectangle.Empty;
                        continue;
                    }
                    rightX -= itemW;
                    item.Bounds = new Rectangle(rightX, top, itemW, _itemHeight);
                    rightX -= 4;
                }
            }
        }

        private int MeasureItemWidth(Graphics g, ToolbarItem item)
        {
            if (item is ToolbarSeparator) return 12;
            if (item is ToolbarSpacer) return 0;

            int w = 20; // base padding left + right
            if (!string.IsNullOrEmpty(item.Glyph)) w += 22;
            if (!string.IsNullOrEmpty(item.Text))
            {
                Size s = TextRenderer.MeasureText(g, item.Text, Font);
                w += s.Width;
            }
            if (item is ToolbarDropdown) w += 16;
            if (item is ToolbarButton btn && btn.BadgeCount.HasValue && btn.BadgeCount.Value > 0) w += 20;

            return Math.Max(32, w);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            ToolbarItem? found = null;
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item.IsVisible && item.IsEnabled && !(item is ToolbarSeparator) && !(item is ToolbarSpacer))
                {
                    if (item.Bounds.Contains(e.Location))
                    {
                        found = item;
                        break;
                    }
                }
            }

            if (_hoveredItem != found)
            {
                _hoveredItem = found;
                Cursor = found != null ? Cursors.Hand : Cursors.Default;
                Invalidate();

                if (found != null && !string.IsNullOrEmpty(found.Tooltip))
                {
                    _toolTip.SetToolTip(this, found.Tooltip);
                }
                else
                {
                    _toolTip.SetToolTip(this, null);
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoveredItem != null)
            {
                _hoveredItem = null;
                _toolTip.SetToolTip(this, null);
                Cursor = Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && _hoveredItem != null)
            {
                _pressedItem = _hoveredItem;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && _pressedItem != null)
            {
                var clicked = _pressedItem;
                _pressedItem = null;
                Invalidate();

                if (clicked.Bounds.Contains(e.Location))
                {
                    if (clicked is ToolbarDropdown dd)
                    {
                        dd.OnDropdown();
                    }
                    clicked.OnClick();
                }
            }
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius) =>
            ZeroUIConfig.CreateRoundedRectangle(rect, radius);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ZeroTheme.ThemeChanged -= OnThemeChanged;
                _toolTip.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    [Obsolete("Use ToolbarItemType instead.")]
    public enum ZeroToolbarItemType
    {
        Button = ToolbarItemType.Button,
        Separator = ToolbarItemType.Separator,
        Spacer = ToolbarItemType.Spacer,
        Dropdown = ToolbarItemType.Dropdown
    }

    [Obsolete("Use ToolbarItem instead.")]
    public abstract class ZeroToolbarItem : ToolbarItem
    {
    }

    [Obsolete("Use ToolbarButton instead.")]
    public class ZeroToolbarButton : ToolbarButton
    {
        public ZeroToolbarButton() : base() { }
        public ZeroToolbarButton(string text, string? glyph = null, EventHandler? onClick = null, string? shortcut = null)
            : base(text, glyph, onClick, shortcut) { }
    }

    [Obsolete("Use ToolbarSeparator instead.")]
    public class ZeroToolbarSeparator : ToolbarSeparator
    {
    }

    [Obsolete("Use ToolbarSpacer instead.")]
    public class ZeroToolbarSpacer : ToolbarSpacer
    {
    }

    [Obsolete("Use ToolbarDropdown instead.")]
    public class ZeroToolbarDropdown : ToolbarDropdown
    {
        public ZeroToolbarDropdown() : base() { }
        public ZeroToolbarDropdown(string text, string? glyph = null, EventHandler? onDropdown = null)
            : base(text, glyph, onDropdown) { }
    }

    [Obsolete("Use ToolbarControl instead.")]
    [ToolboxItem(false)]
    public class ZeroToolbar : ToolbarControl
    {
        public new ZeroToolbarButton AddButton(string text, string? glyph = null, EventHandler? onClick = null, string? shortcut = null)
        {
            var btn = new ZeroToolbarButton(text, glyph, onClick, shortcut);
            Items.Add(btn);
            Invalidate();
            return btn;
        }

        public new ZeroToolbarDropdown AddDropdown(string text, string? glyph = null, EventHandler? onDropdown = null)
        {
            var dd = new ZeroToolbarDropdown(text, glyph, onDropdown);
            Items.Add(dd);
            Invalidate();
            return dd;
        }
    }
}
