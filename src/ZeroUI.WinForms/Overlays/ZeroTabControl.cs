using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Overlays
{
    public enum ZeroTabStyle
    {
        Underline,
        Pill,
        Card
    }

    /// <summary>
    /// Represents an individual tab page container inside ZeroTabControl.
    /// Inherits from Panel to allow hosting child controls with zero layout constraints.
    /// </summary>
    public class ZeroTabPage : Panel
    {
        public string Title { get; set; } = "New Tab";
        public string Icon { get; set; } = "";
        public int BadgeCount { get; set; } = 0;
        public Color? BadgeColor { get; set; }
        public bool Closable { get; set; } = false;

        internal Rectangle HeaderBounds { get; set; }
        internal Rectangle CloseButtonBounds { get; set; }

        public ZeroTabPage()
        {
            Dock = DockStyle.Fill;
            Visible = false;
        }

        public ZeroTabPage(string title, string icon = "") : this()
        {
            Title = title;
            Icon = icon;
        }
    }

    /// <summary>
    /// Modern anti-aliased flat TabControl and container for ZeroUI.
    /// Eliminates Win32 3D gray border flicker, supports Underline/Pill styles,
    /// tab notification badges, icons, and 100% seamless Obsidian Dark / Clean Light theming.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Overlays & Navigation")]
    [DefaultEvent("SelectedIndexChanged")]
    [DefaultProperty("SelectedIndex")]
    [Description("Modern flat TabControl container with Underline/Pill styles and notification badges")]
    public class ZeroTabControl : Control
    {
        private readonly List<ZeroTabPage> _tabPages = new List<ZeroTabPage>();
        private readonly Panel _contentContainer;

        private int _selectedIndex = -1;
        private int _hoveredIndex = -1;
        private int _hoveredCloseIndex = -1;
        private int _tabHeight = 42;
        private ZeroTabStyle _tabStyle = ZeroTabStyle.Underline;

        public event EventHandler? SelectedIndexChanged;
        public event EventHandler<ZeroTabPage>? TabClosed;

        public ZeroTabControl()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            BackColor = Color.Transparent;

            _contentContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            Controls.Add(_contentContainer);

            Size = new Size(500, 350);

            ZeroTheme.ThemeChanged += (s, e) => Invalidate();
            ZeroUIConfig.ConfigChanged += (s, e) =>
            {
                Font = ZeroUIConfig.DefaultFont;
                Invalidate();
            };
        }

        [Browsable(false)]
        public List<ZeroTabPage> TabPages => _tabPages;

        [Category("Appearance")]
        [DefaultValue(42)]
        public int TabHeight
        {
            get => _tabHeight;
            set
            {
                if (_tabHeight != value && value >= 24)
                {
                    _tabHeight = value;
                    UpdateContainerPadding();
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue(ZeroTabStyle.Underline)]
        public ZeroTabStyle TabStyle
        {
            get => _tabStyle;
            set
            {
                if (_tabStyle != value)
                {
                    _tabStyle = value;
                    Invalidate();
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(-1)]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_tabPages.Count == 0)
                {
                    _selectedIndex = -1;
                    return;
                }

                int clamped = Math.Max(0, Math.Min(_tabPages.Count - 1, value));
                if (_selectedIndex != clamped)
                {
                    _selectedIndex = clamped;
                    UpdateActiveTabContent();
                    SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        [Browsable(false)]
        public ZeroTabPage? SelectedTab => (_selectedIndex >= 0 && _selectedIndex < _tabPages.Count) ? _tabPages[_selectedIndex] : null;

        public ZeroTabPage AddTab(string title, string icon = "", int badgeCount = 0)
        {
            var page = new ZeroTabPage(title, icon) { BadgeCount = badgeCount };
            AddTab(page);
            return page;
        }

        public void AddTab(ZeroTabPage page)
        {
            _tabPages.Add(page);
            _contentContainer.Controls.Add(page);
            if (_selectedIndex == -1)
            {
                SelectedIndex = 0;
            }
            Invalidate();
        }

        public void RemoveTab(ZeroTabPage page)
        {
            int idx = _tabPages.IndexOf(page);
            if (idx >= 0)
            {
                _tabPages.RemoveAt(idx);
                _contentContainer.Controls.Remove(page);
                page.Dispose();

                if (_selectedIndex >= _tabPages.Count)
                {
                    _selectedIndex = _tabPages.Count - 1;
                }
                UpdateActiveTabContent();
                TabClosed?.Invoke(this, page);
                Invalidate();
            }
        }

        private void UpdateContainerPadding()
        {
            Padding = new Padding(0, _tabHeight, 0, 0);
        }

        private void UpdateActiveTabContent()
        {
            _contentContainer.SuspendLayout();
            for (int i = 0; i < _tabPages.Count; i++)
            {
                _tabPages[i].Visible = (i == _selectedIndex);
            }
            _contentContainer.ResumeLayout(true);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateContainerPadding();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Y > _tabHeight) return;

            int hov = -1;
            int hovClose = -1;

            for (int i = 0; i < _tabPages.Count; i++)
            {
                if (_tabPages[i].HeaderBounds.Contains(e.Location))
                {
                    hov = i;
                    if (_tabPages[i].Closable && _tabPages[i].CloseButtonBounds.Contains(e.Location))
                    {
                        hovClose = i;
                    }
                    break;
                }
            }

            if (_hoveredIndex != hov || _hoveredCloseIndex != hovClose)
            {
                _hoveredIndex = hov;
                _hoveredCloseIndex = hovClose;
                Cursor = (hov >= 0) ? Cursors.Hand : Cursors.Default;
                Invalidate(new Rectangle(0, 0, Width, _tabHeight));
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoveredIndex = -1;
            _hoveredCloseIndex = -1;
            Cursor = Cursors.Default;
            Invalidate(new Rectangle(0, 0, Width, _tabHeight));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Y > _tabHeight) return;

            for (int i = 0; i < _tabPages.Count; i++)
            {
                if (_tabPages[i].HeaderBounds.Contains(e.Location))
                {
                    if (_tabPages[i].Closable && _tabPages[i].CloseButtonBounds.Contains(e.Location))
                    {
                        RemoveTab(_tabPages[i]);
                        return;
                    }

                    SelectedIndex = i;
                    return;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var palette = ZeroTheme.Colors;

            // 1. Tab Header Bar Background
            var headerRect = new Rectangle(0, 0, Width, _tabHeight);
            using (var brushHeader = new SolidBrush(palette.HeaderBackground))
            {
                g.FillRectangle(brushHeader, headerRect);
            }

            // Bottom border of tab strip
            using (var penBottom = new Pen(palette.Border, 1f))
            {
                g.DrawLine(penBottom, 0, _tabHeight - 1, Width, _tabHeight - 1);
            }

            if (_tabPages.Count == 0) return;

            int curX = 12;
            using var fontTab = new Font(Font.FontFamily, 9.2f, FontStyle.Regular);
            using var fontTabActive = new Font(Font.FontFamily, 9.2f, FontStyle.Bold);
            using var fontIcon = new Font("Segoe UI Emoji", 9.5f);
            using var fontBadge = new Font(Font.FontFamily, 7.5f, FontStyle.Bold);

            for (int i = 0; i < _tabPages.Count; i++)
            {
                var page = _tabPages[i];
                bool isSelected = i == _selectedIndex;
                bool isHovered = i == _hoveredIndex;

                var activeFont = isSelected ? fontTabActive : fontTab;
                var textSz = g.MeasureString(page.Title, activeFont);

                int itemW = (int)textSz.Width + 24;
                if (!string.IsNullOrEmpty(page.Icon)) itemW += 20;
                if (page.BadgeCount > 0) itemW += 24;
                if (page.Closable) itemW += 18;

                page.HeaderBounds = new Rectangle(curX, 0, itemW, _tabHeight);

                // Draw Tab Shape based on style
                if (_tabStyle == ZeroTabStyle.Pill)
                {
                    int pillH = _tabHeight - 12;
                    var pillRect = new Rectangle(curX + 2, 6, itemW - 4, pillH);
                    int effRadius = ZeroUIConfig.GetEffectiveRadius(6);
                    if (isSelected)
                    {
                        using var brushPill = new SolidBrush(palette.Primary);
                        using var pathPill = CreateRoundedRect(pillRect, effRadius);
                        g.FillPath(brushPill, pathPill);
                    }
                    else if (isHovered)
                    {
                        using var brushPillHov = new SolidBrush(Color.FromArgb(20, palette.Primary));
                        using var pathPillHov = CreateRoundedRect(pillRect, effRadius);
                        g.FillPath(brushPillHov, pathPillHov);
                    }
                }
                else if (_tabStyle == ZeroTabStyle.Card)
                {
                    if (isSelected)
                    {
                        var cardRect = new Rectangle(curX, 4, itemW, _tabHeight - 4);
                        int effRadius = ZeroUIConfig.GetEffectiveRadius(6);
                        using var brushCard = new SolidBrush(palette.Background);
                        using var pathCard = CreateTopRoundedRect(cardRect, effRadius);
                        g.FillPath(brushCard, pathCard);
                        using var penCard = new Pen(palette.Border, 1f);
                        g.DrawPath(penCard, pathCard);
                    }
                }
                else // Underline
                {
                    if (isHovered && !isSelected)
                    {
                        using var brushHov = new SolidBrush(Color.FromArgb(10, palette.Primary));
                        g.FillRectangle(brushHov, page.HeaderBounds);
                    }

                    if (isSelected)
                    {
                        int barH = 3;
                        var barRect = new Rectangle(curX + 6, _tabHeight - barH, itemW - 12, barH);
                        using var brushBar = new SolidBrush(palette.Primary);
                        using var pathBar = CreateRoundedRect(barRect, 2);
                        g.FillPath(brushBar, pathBar);
                    }
                }

                int innerX = curX + 12;

                // Draw Icon
                if (!string.IsNullOrEmpty(page.Icon))
                {
                    using var brushIcon = new SolidBrush(isSelected && _tabStyle == ZeroTabStyle.Pill ? Color.White : palette.TextPrimary);
                    g.DrawString(page.Icon, fontIcon, brushIcon, innerX, (_tabHeight - 18) / 2);
                    innerX += 20;
                }

                // Draw Tab Title
                Color textCol;
                if (_tabStyle == ZeroTabStyle.Pill && isSelected) textCol = Color.White;
                else if (isSelected) textCol = palette.Primary;
                else if (isHovered) textCol = palette.TextPrimary;
                else textCol = palette.TextSecondary;

                using (var brushText = new SolidBrush(textCol))
                {
                    g.DrawString(page.Title, activeFont, brushText, innerX, (_tabHeight - 18) / 2);
                    innerX += (int)textSz.Width + 6;
                }

                // Draw Badge
                if (page.BadgeCount > 0)
                {
                    string bStr = page.BadgeCount > 99 ? "99+" : page.BadgeCount.ToString();
                    var bSz = g.MeasureString(bStr, fontBadge);
                    int bW = Math.Max(18, (int)bSz.Width + 8);
                    int bH = 16;
                    var bRect = new Rectangle(innerX, (_tabHeight - bH) / 2, bW, bH);

                    Color bColor = page.BadgeColor ?? palette.Danger;
                    using var brushBadge = new SolidBrush(bColor);
                    using var pathBadge = CreateRoundedRect(bRect, 8);
                    g.FillPath(brushBadge, pathBadge);

                    using var brushBadgeText = new SolidBrush(Color.White);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(bStr, fontBadge, brushBadgeText, bRect, sf);
                    innerX += bW + 6;
                }

                // Draw Close Button (✕)
                if (page.Closable)
                {
                    page.CloseButtonBounds = new Rectangle(innerX, (_tabHeight - 14) / 2, 14, 14);
                    bool hovClose = _hoveredCloseIndex == i;

                    Color closeC = hovClose ? palette.Danger : palette.TextSecondary;
                    using var brushClose = new SolidBrush(closeC);
                    var sfClose = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("✕", fontBadge, brushClose, page.CloseButtonBounds, sfClose);
                }

                curX += itemW + 4;
            }
        }

        private static GraphicsPath CreateRoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static GraphicsPath CreateTopRoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddLine(r.Right, r.Bottom, r.X, r.Bottom);
            path.CloseFigure();
            return path;
        }
    }
}
