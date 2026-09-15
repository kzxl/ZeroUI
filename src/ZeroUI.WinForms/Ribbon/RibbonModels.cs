using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ZeroUI.WinForms.Ribbon
{
    public enum RibbonItemStyle
    {
        /// <summary>
        /// Prominent large button with 32x32 icon on top and text below.
        /// </summary>
        Large,

        /// <summary>
        /// Compact small button with 16x16 icon on the left and text on the right.
        /// Stacked vertically up to 3 buttons per column.
        /// </summary>
        Small,

        /// <summary>
        /// Dropdown button with arrow glyph (⯆) that opens a sub-menu or approval list.
        /// </summary>
        DropDown
    }

    /// <summary>
    /// Represents an actionable item (button, dropdown) on a ribbon group.
    /// </summary>
    public class RibbonItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; } = string.Empty;
        public string? BadgeText { get; set; }
        public Image? Icon { get; set; }
        public RibbonItemStyle Style { get; set; } = RibbonItemStyle.Large;
        public bool Enabled { get; set; } = true;
        public bool Visible { get; set; } = true;
        public string? ToolTipText { get; set; }
        public bool IsCheckable { get; set; } = false;
        public bool Checked { get; set; } = false;
        public Color? BadgeColor { get; set; }
        public object? Tag { get; set; }
        public ContextMenuStrip? DropDownMenu { get; set; }

        public event EventHandler? Click;
        public event EventHandler? CheckedChanged;

        public RibbonItem() { }

        public RibbonItem(string text, Image? icon = null, RibbonItemStyle style = RibbonItemStyle.Large, EventHandler? onClick = null)
        {
            Text = text;
            Icon = icon;
            Style = style;
            if (onClick != null) Click += onClick;
        }

        public void PerformClick()
        {
            if (Enabled && Visible)
            {
                if (IsCheckable)
                {
                    Checked = !Checked;
                    CheckedChanged?.Invoke(this, EventArgs.Empty);
                }
                Click?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Represents an approval task item inside an approval dropdown group.
    /// Example: "YCSL: 3" (red dot) or "Bàn giao: 0" (green/blue dot).
    /// </summary>
    public class RibbonApprovalItem
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; } = 0;
        public object? Tag { get; set; }
        public Action? ClickAction { get; set; }

        public RibbonApprovalItem() { }

        public RibbonApprovalItem(string key, string name, int count = 0, Action? onClick = null)
        {
            Key = key;
            Name = name;
            Count = count;
            ClickAction = onClick;
        }

        public string GetDisplayText() => $"{Name}: {Count}";
    }

    /// <summary>
    /// Specialized group container for approval queues (Chờ duyệt chung, Chờ duyệt SX, Chờ duyệt Kho).
    /// Displays a dropdown button with current total count and opens a status popup with color-coded dots.
    /// </summary>
    public class RibbonApprovalGroup
    {
        public string GroupKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool Visible { get; set; } = true;
        public List<RibbonApprovalItem> Items { get; } = new List<RibbonApprovalItem>();

        public event EventHandler? ItemsChanged;

        public RibbonApprovalGroup(string title, string groupKey)
        {
            Title = title;
            GroupKey = groupKey;
        }

        public int TotalCount
        {
            get
            {
                int sum = 0;
                foreach (var item in Items) sum += Math.Max(0, item.Count);
                return sum;
            }
        }

        public void AddItem(string key, string name, int count = 0, Action? onClick = null)
        {
            Items.Add(new RibbonApprovalItem(key, name, count, onClick));
            ItemsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateCount(string key, int count)
        {
            var found = Items.Find(i => i.Key == key);
            if (found != null)
            {
                found.Count = count;
                ItemsChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public RibbonApprovalItem? GetItem(string key) => Items.Find(i => i.Key == key);
    }

    /// <summary>
    /// Represents a functional group of ribbon items under a specific page.
    /// Example: "Thông tin", "Quản lý chung", "Quản lý nhóm", "Hệ thống".
    /// </summary>
    public class RibbonGroup
    {
        public string Text { get; set; } = string.Empty;
        public bool Visible { get; set; } = true;
        public List<RibbonItem> Items { get; } = new List<RibbonItem>();

        // Optional link to specialized approval group
        public RibbonApprovalGroup? ApprovalGroup { get; set; }

        public RibbonGroup() { }

        public RibbonGroup(string text)
        {
            Text = text;
        }

        public RibbonItem? FindItem(string idOrText)
        {
            return Items.Find(i => i.Id == idOrText || string.Equals(i.Text, idOrText, StringComparison.OrdinalIgnoreCase));
        }

        public RibbonItem AddLargeButton(string text, Image? icon = null, EventHandler? onClick = null, string? badge = null)
        {
            var item = new RibbonItem(text, icon, RibbonItemStyle.Large, onClick)
            {
                BadgeText = badge
            };
            Items.Add(item);
            return item;
        }

        public RibbonItem AddSmallButton(string text, Image? icon = null, EventHandler? onClick = null)
        {
            var item = new RibbonItem(text, icon, RibbonItemStyle.Small, onClick);
            Items.Add(item);
            return item;
        }

        public RibbonItem AddDropDownButton(string text, Image? icon = null, ContextMenuStrip? menu = null)
        {
            var item = new RibbonItem(text, icon, RibbonItemStyle.DropDown)
            {
                DropDownMenu = menu
            };
            Items.Add(item);
            return item;
        }
    }

    /// <summary>
    /// Represents a primary tab page inside the ribbon control.
    /// </summary>
    public class RibbonPage
    {
        public string Text { get; set; } = string.Empty;
        public bool Visible { get; set; } = true;
        public List<RibbonGroup> Groups { get; } = new List<RibbonGroup>();
        public List<RibbonApprovalGroup> ApprovalGroups { get; } = new List<RibbonApprovalGroup>();

        public RibbonPage() { }

        public RibbonPage(string text)
        {
            Text = text;
        }

        public RibbonItem? FindItem(string idOrText)
        {
            foreach (var grp in Groups)
            {
                var item = grp.FindItem(idOrText);
                if (item != null) return item;
            }
            return null;
        }

        public RibbonApprovalGroup? GetApprovalGroup(string groupKey)
        {
            return ApprovalGroups.Find(ag => string.Equals(ag.GroupKey, groupKey, StringComparison.OrdinalIgnoreCase));
        }

        public RibbonGroup AddGroup(string title)
        {
            var grp = new RibbonGroup(title);
            Groups.Add(grp);
            return grp;
        }

        public RibbonApprovalGroup AddApprovalGroup(string title, string groupKey)
        {
            var appGroup = new RibbonApprovalGroup(title, groupKey);
            ApprovalGroups.Add(appGroup);

            // Also create a corresponding RibbonGroup for unified layout
            var grp = new RibbonGroup(title)
            {
                ApprovalGroup = appGroup
            };
            Groups.Add(grp);
            return appGroup;
        }
    }
}
