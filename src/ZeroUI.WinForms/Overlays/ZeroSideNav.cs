using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Overlays
{
    public class ZeroSideNavItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "Nav Item";
        public string Icon { get; set; } = "📌";
        public string Category { get; set; } = "";
        public int BadgeCount { get; set; } = 0;
        public Color? BadgeColor { get; set; }
        public Control? AssociatedView { get; set; }

        internal Rectangle Bounds { get; set; }

        public ZeroSideNavItem() { }

        public ZeroSideNavItem(string id, string title, string icon, string category = "", int badgeCount = 0, Control? view = null)
        {
            Id = id;
            Title = title;
            Icon = icon;
            Category = category;
            BadgeCount = badgeCount;
            AssociatedView = view;
        }
    }

    public class ZeroSideNavEventArgs : EventArgs
    {
        public ZeroSideNavItem Item { get; }
        public int Index { get; }

        public ZeroSideNavEventArgs(ZeroSideNavItem item, int index)
        {
            Item = item;
            Index = index;
        }
    }

    /// <summary>
    /// Modern Enterprise Sidebar Navigation control for WinForms applications.
    /// Supports brand header, category section grouping, badges, collapsible rail (230px ⇄ 64px),
    /// and automated view switching for seamless dashboard modularity.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Overlays & Navigation")]
    [DefaultEvent("ItemSelected")]
    [Description("Enterprise Sidebar Navigation with brand header, categorized items, and collapsible rail")]
    public class ZeroSideNav : Control
    {
        private readonly List<ZeroSideNavItem> _items = new List<ZeroSideNavItem>();
        private int _selectedIndex = 0;
        private int _hoveredIndex = -1;
        private bool _isCollapsed = false;
        private int _expandedWidth = 230;
        private int _collapsedWidth = 64;

        private string _brandLogo = "⚡";
        private string _brandTitle = "ZeroUI Suite";
        private string _brandSubtitle = "Enterprise Workstation";

        private Panel? _contentContainer;
        private readonly ToolTip _toolTip = new ToolTip();
        private int _lastTooltipIndex = -1;

        public event EventHandler<ZeroSideNavEventArgs>? ItemSelected;
        public event EventHandler? CollapseChanged;

        public ZeroSideNav()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            Dock = DockStyle.Left;
            Width = _expandedWidth;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            BackColor = Color.Transparent;

            ZeroTheme.ThemeChanged += (s, e) => Invalidate();
            ZeroUIConfig.CornerStyleChanged += (s, e) => Invalidate();
        }

        #region Public Properties

        [Category("Data")]
        [Browsable(false)]
        public List<ZeroSideNavItem> Items => _items;

        [Category("Appearance")]
        [DefaultValue("⚡")]
        public string BrandLogo
        {
            get => _brandLogo;
            set { _brandLogo = value ?? ""; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue("ZeroUI Suite")]
        public string BrandTitle
        {
            get => _brandTitle;
            set { _brandTitle = value ?? ""; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue("Enterprise Workstation")]
        public string BrandSubtitle
        {
            get => _brandSubtitle;
            set { _brandSubtitle = value ?? ""; Invalidate(); }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool IsCollapsed
        {
            get => _isCollapsed;
            set
            {
                if (_isCollapsed != value)
                {
                    _isCollapsed = value;
                    Width = _isCollapsed ? _collapsedWidth : _expandedWidth;
                    CollapseChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue(230)]
        public int ExpandedWidth
        {
            get => _expandedWidth;
            set
            {
                if (_expandedWidth != value && value >= 160)
                {
                    _expandedWidth = value;
                    if (!_isCollapsed) Width = _expandedWidth;
                    Invalidate();
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(0)]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_items.Count == 0)
                {
                    _selectedIndex = -1;
                    return;
                }

                int clamped = Math.Max(0, Math.Min(_items.Count - 1, value));
                if (_selectedIndex != clamped)
                {
                    _selectedIndex = clamped;
                    SyncAssociatedView();
                    ItemSelected?.Invoke(this, new ZeroSideNavEventArgs(_items[_selectedIndex], _selectedIndex));
                    Invalidate();
                }
            }
        }

        [Browsable(false)]
        public ZeroSideNavItem? SelectedItem => (_selectedIndex >= 0 && _selectedIndex < _items.Count) ? _items[_selectedIndex] : null;

        #endregion

        #region Public API

        public ZeroSideNavItem AddItem(string id, string title, string icon, string category = "", int badgeCount = 0, Control? view = null)
        {
            var item = new ZeroSideNavItem(id, title, icon, category, badgeCount, view);
            _items.Add(item);
            if (_contentContainer != null && view != null)
            {
                view.Dock = DockStyle.Fill;
                view.Visible = (_items.Count - 1 == _selectedIndex);
                if (!_contentContainer.Controls.Contains(view))
                {
                    _contentContainer.Controls.Add(view);
                }
            }
            Invalidate();
            return item;
        }

        public void BindContentContainer(Panel container)
        {
            _contentContainer = container;
            _contentContainer.SuspendLayout();
            for (int i = 0; i < _items.Count; i++)
            {
                var view = _items[i].AssociatedView;
                if (view != null)
                {
                    view.Dock = DockStyle.Fill;
                    view.Visible = (i == _selectedIndex);
                    if (!_contentContainer.Controls.Contains(view))
                    {
                        _contentContainer.Controls.Add(view);
                    }
                }
            }
            _contentContainer.ResumeLayout(true);
        }

        public void ToggleCollapse()
        {
            IsCollapsed = !IsCollapsed;
        }

        #endregion

        private void SyncAssociatedView()
        {
            if (_contentContainer == null) return;
            _contentContainer.SuspendLayout();
            for (int i = 0; i < _items.Count; i++)
            {
                var view = _items[i].AssociatedView;
                if (view != null)
                {
                    view.Visible = (i == _selectedIndex);
                }
            }
            _contentContainer.ResumeLayout(true);
        }

        #region Mouse Interaction

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int hov = -1;

            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Bounds.Contains(e.Location))
                {
                    hov = i;
                    break;
                }
            }

            if (_hoveredIndex != hov)
            {
                _hoveredIndex = hov;
                Cursor = (hov >= 0 || e.Y < 56) ? Cursors.Hand : Cursors.Default;

                if (_isCollapsed && hov >= 0 && hov != _lastTooltipIndex)
                {
                    _lastTooltipIndex = hov;
                    _toolTip.Show(_items[hov].Title, this, Width + 8, _items[hov].Bounds.Y + 8, 2000);
                }
                else if (hov < 0)
                {
                    _toolTip.Hide(this);
                    _lastTooltipIndex = -1;
                }

                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoveredIndex = -1;
            _lastTooltipIndex = -1;
            _toolTip.Hide(this);
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            // Toggle collapse when clicking brand header area
            if (e.Y <= 56)
            {
                ToggleCollapse();
                return;
            }

            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Bounds.Contains(e.Location))
                {
                    SelectedIndex = i;
                    return;
                }
            }
        }

        #endregion

        #region Rendering Engine

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var palette = ZeroTheme.Colors;
            int w = Width;
            int h = Height;

            // 1. Sidebar Background & Right Dividing Border
            using (var bgBrush = new SolidBrush(palette.HeaderBackground))
            {
                g.FillRectangle(bgBrush, 0, 0, w, h);
            }
            using (var borderPen = new Pen(palette.Border, 1f))
            {
                g.DrawLine(borderPen, w - 1, 0, w - 1, h);
            }

            // 2. Brand Header (Top 56px)
            Rectangle headerRect = new Rectangle(0, 0, w, 56);
            using (var logoFont = new Font("Segoe UI Emoji", 15f))
            using (var titleFont = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            using (var logoBrush = new SolidBrush(palette.Primary))
            using (var titleBrush = new SolidBrush(palette.TextPrimary))
            using (var subBrush = new SolidBrush(palette.TextSecondary))
            {
                if (!_isCollapsed)
                {
                    g.DrawString(_brandLogo, logoFont, logoBrush, 14, 12);
                    g.DrawString(_brandTitle, titleFont, titleBrush, 44, 10);
                    g.DrawString(_brandSubtitle, subFont, subBrush, 45, 30);
                }
                else
                {
                    var logoSz = g.MeasureString(_brandLogo, logoFont);
                    g.DrawString(_brandLogo, logoFont, logoBrush, (w - logoSz.Width) / 2f, 14);
                }
            }

            using (var sepPen = new Pen(palette.Border, 1f))
            {
                g.DrawLine(sepPen, 8, 56, w - 8, 56);
            }

            // 3. Render Items & Category Sections
            int curY = 66;
            string lastCategory = "";
            int itemH = 40;
            int paddingX = _isCollapsed ? 6 : 10;
            int itemW = w - (paddingX * 2);

            using var catFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            using var itemFont = new Font("Segoe UI", 9.2f, FontStyle.Regular);
            using var itemBold = new Font("Segoe UI", 9.2f, FontStyle.Bold);
            using var iconFont = new Font("Segoe UI Emoji", 11f);
            using var badgeFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];

                // Category Section Header (only in expanded mode)
                if (!_isCollapsed && !string.IsNullOrEmpty(item.Category) && item.Category != lastCategory)
                {
                    lastCategory = item.Category;
                    using var catBrush = new SolidBrush(Color.FromArgb(148, 163, 184));
                    g.DrawString(item.Category.ToUpperInvariant(), catFont, catBrush, 16, curY + 4);
                    curY += 24;
                }

                item.Bounds = new Rectangle(paddingX, curY, itemW, itemH);
                bool isSelected = (i == _selectedIndex);
                bool isHovered = (i == _hoveredIndex);

                int effRadius = ZeroUIConfig.GetEffectiveRadius(6);

                // Item Shape Background
                if (isSelected)
                {
                    using var selBrush = new SolidBrush(Color.FromArgb(24, palette.Primary));
                    using var selPath = ZeroUIConfig.CreateRoundedRectangle(item.Bounds, effRadius);
                    g.FillPath(selBrush, selPath);

                    // Left active indicator pill
                    using var indBrush = new SolidBrush(palette.Primary);
                    using var indPath = ZeroUIConfig.CreateRoundedRectangle(new Rectangle(paddingX + 2, curY + 6, 3, itemH - 12), 2);
                    g.FillPath(indBrush, indPath);
                }
                else if (isHovered)
                {
                    using var hovBrush = new SolidBrush(Color.FromArgb(12, palette.Primary));
                    using var hovPath = ZeroUIConfig.CreateRoundedRectangle(item.Bounds, effRadius);
                    g.FillPath(hovBrush, hovPath);
                }

                // Render Icon
                int iconX = _isCollapsed ? (w - 20) / 2 : paddingX + 14;
                Color iconColor = isSelected ? palette.Primary : (isHovered ? palette.TextPrimary : palette.TextSecondary);
                using (var iconBrush = new SolidBrush(iconColor))
                {
                    g.DrawString(item.Icon, iconFont, iconBrush, iconX, curY + (itemH - 20) / 2);
                }

                // Render Text & Badge (only in expanded mode)
                if (!_isCollapsed)
                {
                    int textX = paddingX + 42;
                    Color textColor = isSelected ? palette.Primary : (isHovered ? palette.TextPrimary : palette.TextSecondary);
                    var curFont = isSelected ? itemBold : itemFont;

                    using (var textBrush = new SolidBrush(textColor))
                    using (var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, LineAlignment = StringAlignment.Center })
                    {
                        int textMaxW = item.Bounds.Right - textX - (item.BadgeCount > 0 ? 36 : 8);
                        RectangleF textRect = new RectangleF(textX, curY, textMaxW, itemH);
                        g.DrawString(item.Title, curFont, textBrush, textRect, sf);
                    }

                    // Badge Pill
                    if (item.BadgeCount > 0)
                    {
                        string bStr = item.BadgeCount > 99 ? "99+" : item.BadgeCount.ToString();
                        var bSz = g.MeasureString(bStr, badgeFont);
                        int bW = Math.Max(18, (int)bSz.Width + 8);
                        int bH = 16;
                        int bX = item.Bounds.Right - bW - 8;
                        var bRect = new Rectangle(bX, curY + (itemH - bH) / 2, bW, bH);

                        Color bColor = item.BadgeColor ?? palette.Danger;
                        using var bBrush = new SolidBrush(bColor);
                        using var bPath = ZeroUIConfig.CreateRoundedRectangle(bRect, 8);
                        g.FillPath(bBrush, bPath);

                        using var bTextBrush = new SolidBrush(Color.White);
                        var sfBadge = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(bStr, badgeFont, bTextBrush, bRect, sfBadge);
                    }
                }

                curY += itemH + 4;
            }

            // 4. Bottom Rail Footer: Collapse hint or status
            int footerH = 36;
            int footerY = h - footerH;
            using (var sepPen = new Pen(palette.Border, 1f))
            {
                g.DrawLine(sepPen, 8, footerY, w - 8, footerY);
            }

            using (var footFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            using (var footBrush = new SolidBrush(palette.TextSecondary))
            {
                if (!_isCollapsed)
                {
                    g.DrawString("v2.4 Enterprise • Ready", footFont, footBrush, 14, footerY + 10);
                }
                else
                {
                    g.DrawString("⚡", footFont, footBrush, (w - 12) / 2f, footerY + 10);
                }
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _toolTip.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
