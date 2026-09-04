using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{
    public class ItemCheckEventArgs : EventArgs
    {
        public int Index { get; }
        public bool NewValue { get; }
        public object Item { get; }

        public ItemCheckEventArgs(int index, bool newValue, object item)
        {
            Index = index;
            NewValue = newValue;
            Item = item;
        }
    }

    /// <summary>
    /// Modern anti-aliased Multi-Select CheckedComboBox for ZeroUI WinForms.
    /// Supports checkboxes per item, Select-All toggle, instant search filtering,
    /// and dynamic summary display formatting.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultEvent("ItemCheck")]
    [Description("Modern checked combo box allowing multi-item selection with checkboxes and filter")]
    public class ZeroCheckedComboBox : Control
    {
        public class CheckedItem
        {
            public object Value { get; set; }
            public string DisplayText { get; set; }
            public bool IsChecked { get; set; }

            public CheckedItem(object value, string displayText, bool isChecked = false)
            {
                Value = value;
                DisplayText = displayText;
                IsChecked = isChecked;
            }

            public override string ToString() => DisplayText;
        }

        private readonly List<CheckedItem> _items = new List<CheckedItem>();
        private string _placeholder = "Select items...";
        private string _summaryFormat = "{0} items selected";
        private int _itemHeight = 30;

        private bool _isHovered = false;
        private bool _isFocused = false;
        private bool _isDroppedDown = false;

        private readonly ToolStripDropDown _dropdown;
        private readonly CheckedComboPopupControl _popupControl;

        public event EventHandler<ItemCheckEventArgs>? ItemCheck;
        public event EventHandler? CheckedChanged;

        [Category("Appearance")]
        [DefaultValue("Select items...")]
        public string Placeholder
        {
            get => _placeholder;
            set { _placeholder = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue("{0} items selected")]
        public string SummaryFormat
        {
            get => _summaryFormat;
            set { _summaryFormat = value; Invalidate(); }
        }

        [Browsable(false)]
        public List<CheckedItem> Items => _items;

        [Browsable(false)]
        public List<object> CheckedValues
        {
            get
            {
                var list = new List<object>();
                for (int i = 0; i < _items.Count; i++)
                {
                    if (_items[i].IsChecked) list.Add(_items[i].Value);
                }
                return list;
            }
        }

        [Browsable(false)]
        public List<int> CheckedIndices
        {
            get
            {
                var list = new List<int>();
                for (int i = 0; i < _items.Count; i++)
                {
                    if (_items[i].IsChecked) list.Add(i);
                }
                return list;
            }
        }

        public ZeroCheckedComboBox()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(240, 36);
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            BackColor = Color.Transparent;

            _popupControl = new CheckedComboPopupControl(this);
            var host = new ToolStripControlHost(_popupControl)
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AutoSize = false
            };

            _dropdown = new ToolStripDropDown
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AutoClose = true,
                DropShadowEnabled = true
            };
            _dropdown.Items.Add(host);
            _dropdown.Closed += (s, e) =>
            {
                _isDroppedDown = false;
                Invalidate();
            };
        }

        public void AddItem(object value, string displayText, bool isChecked = false)
        {
            _items.Add(new CheckedItem(value, displayText, isChecked));
            Invalidate();
        }

        public void SetItemChecked(int index, bool isChecked)
        {
            if (index < 0 || index >= _items.Count) return;
            if (_items[index].IsChecked != isChecked)
            {
                _items[index].IsChecked = isChecked;
                ItemCheck?.Invoke(this, new ItemCheckEventArgs(index, isChecked, _items[index].Value));
                CheckedChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        public void CheckAll(bool isChecked)
        {
            bool anyChanged = false;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].IsChecked != isChecked)
                {
                    _items[i].IsChecked = isChecked;
                    ItemCheck?.Invoke(this, new ItemCheckEventArgs(i, isChecked, _items[i].Value));
                    anyChanged = true;
                }
            }
            if (anyChanged)
            {
                CheckedChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        public string GetDisplayText()
        {
            var checkedList = new List<string>();
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].IsChecked) checkedList.Add(_items[i].DisplayText);
            }

            if (checkedList.Count == 0) return _placeholder;
            if (checkedList.Count <= 2) return string.Join(", ", checkedList);
            return string.Format(_summaryFormat, checkedList.Count);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var colors = ZeroTheme.Colors;
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

            // Draw Background
            using (var path = CreateRoundedRectanglePath(bounds, 4))
            {
                using (var brush = new SolidBrush(colors.Surface))
                {
                    g.FillPath(brush, path);
                }

                Color borderColor = _isDroppedDown || _isFocused
                    ? colors.Primary
                    : (_isHovered ? colors.PrimaryHover : colors.Border);

                using (var pen = new Pen(borderColor, _isDroppedDown || _isFocused ? 1.5f : 1.0f))
                {
                    g.DrawPath(pen, path);
                }
            }

            // Draw Chevron Arrow
            int arrowX = Width - 24;
            int arrowY = Height / 2;
            using (var pen = new Pen(_isHovered || _isDroppedDown ? colors.Primary : colors.TextSecondary, 1.8f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                if (_isDroppedDown)
                {
                    g.DrawLine(pen, arrowX, arrowY + 2, arrowX + 4, arrowY - 2);
                    g.DrawLine(pen, arrowX + 4, arrowY - 2, arrowX + 8, arrowY + 2);
                }
                else
                {
                    g.DrawLine(pen, arrowX, arrowY - 2, arrowX + 4, arrowY + 2);
                    g.DrawLine(pen, arrowX + 4, arrowY + 2, arrowX + 8, arrowY - 2);
                }
            }

            // Draw Display Text
            string text = GetDisplayText();
            bool isMuted = text == _placeholder;
            using (var brush = new SolidBrush(isMuted ? colors.TextSecondary : colors.TextPrimary))
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter
                };
                var textRect = new Rectangle(12, 0, Width - 42, Height);
                g.DrawString(text, Font, brush, textRect, sf);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            ToggleDropDown();
        }

        public void ToggleDropDown()
        {
            if (_isDroppedDown)
            {
                _dropdown.Close();
            }
            else
            {
                OpenDropDown();
            }
        }

        public void OpenDropDown()
        {
            if (_dropdown.Visible) return;

            _isDroppedDown = true;
            _popupControl.RefreshItems();

            int popupH = Math.Min(320, Math.Max(120, _items.Count * _itemHeight + 70));
            _popupControl.Size = new Size(Math.Max(Width, 240), popupH);
            _dropdown.Size = _popupControl.Size;

            _dropdown.Show(this, new Point(0, Height + 2));
            Invalidate();
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // Internal Popup Control hosting search bar, Select-All toggle, and virtual items list
        private class CheckedComboPopupControl : Control
        {
            private readonly ZeroCheckedComboBox _owner;
            private readonly TextBox _searchBox;
            private readonly List<CheckedItem> _filteredItems = new List<CheckedItem>();
            private int _hoveredIndex = -1;
            private int _scrollY = 0;

            public CheckedComboPopupControl(ZeroCheckedComboBox owner)
            {
                _owner = owner;
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw, true);
                DoubleBuffered = true;

                _searchBox = new TextBox
                {
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Segoe UI", 9f),
                    Location = new Point(8, 8),
                    Width = 220
                };
                _searchBox.TextChanged += (s, e) => FilterItems();
                Controls.Add(_searchBox);
            }

            public void RefreshItems()
            {
                _searchBox.Text = string.Empty;
                FilterItems();
            }

            private void FilterItems()
            {
                _filteredItems.Clear();
                string query = _searchBox.Text.Trim();
                for (int i = 0; i < _owner._items.Count; i++)
                {
                    if (string.IsNullOrEmpty(query) || _owner._items[i].DisplayText.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _filteredItems.Add(_owner._items[i]);
                    }
                }
                Invalidate();
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                _searchBox.Width = Width - 16;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var colors = ZeroTheme.Colors;
                g.Clear(colors.Surface);

                // Draw border around popup
                using (var pen = new Pen(colors.Border))
                {
                    g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }

                // Select-All row
                int selectAllY = 36;
                int itemH = _owner._itemHeight;

                bool allChecked = _owner._items.Count > 0;
                for (int i = 0; i < _owner._items.Count; i++)
                {
                    if (!_owner._items[i].IsChecked) { allChecked = false; break; }
                }

                DrawCheckboxItem(g, 0, selectAllY, Width, itemH, "(Select All)", allChecked, _hoveredIndex == -2);

                // Separator line
                using (var pen = new Pen(colors.Border))
                {
                    g.DrawLine(pen, 8, selectAllY + itemH, Width - 8, selectAllY + itemH);
                }

                // Items list
                int startY = selectAllY + itemH + 2;
                for (int i = 0; i < _filteredItems.Count; i++)
                {
                    int itemY = startY + i * itemH - _scrollY;
                    if (itemY + itemH < startY || itemY > Height) continue;
                    DrawCheckboxItem(g, 0, itemY, Width, itemH, _filteredItems[i].DisplayText, _filteredItems[i].IsChecked, _hoveredIndex == i);
                }
            }

            private void DrawCheckboxItem(Graphics g, int x, int y, int w, int h, string text, bool isChecked, bool isHovered)
            {
                var colors = ZeroTheme.Colors;
                if (isHovered)
                {
                    using (var brush = new SolidBrush(colors.Hover))
                    {
                        g.FillRectangle(brush, x + 2, y, w - 4, h);
                    }
                }

                // Draw Checkbox Box
                int cbSize = 16;
                int cbX = x + 12;
                int cbY = y + (h - cbSize) / 2;
                var cbRect = new Rectangle(cbX, cbY, cbSize, cbSize);

                if (isChecked)
                {
                    using (var brush = new SolidBrush(colors.Primary))
                    {
                        g.FillRectangle(brush, cbRect);
                    }
                    using (var pen = new Pen(Color.White, 1.8f))
                    {
                        g.DrawLine(pen, cbX + 3, cbY + 8, cbX + 6, cbY + 11);
                        g.DrawLine(pen, cbX + 6, cbY + 11, cbX + 12, cbY + 4);
                    }
                }
                else
                {
                    using (var pen = new Pen(colors.Border, 1.2f))
                    {
                        g.DrawRectangle(pen, cbRect);
                    }
                }

                // Draw text
                using (var brush = new SolidBrush(colors.TextPrimary))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Center,
                        Trimming = StringTrimming.EllipsisCharacter
                    };
                    var textRect = new Rectangle(cbX + cbSize + 8, y, w - (cbX + cbSize + 16), h);
                    g.DrawString(text, Font, brush, textRect, sf);
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                int selectAllY = 36;
                int itemH = _owner._itemHeight;

                if (e.Y >= selectAllY && e.Y < selectAllY + itemH)
                {
                    _hoveredIndex = -2;
                }
                else if (e.Y >= selectAllY + itemH + 2)
                {
                    int index = (e.Y - (selectAllY + itemH + 2) + _scrollY) / itemH;
                    _hoveredIndex = (index >= 0 && index < _filteredItems.Count) ? index : -1;
                }
                else
                {
                    _hoveredIndex = -1;
                }
                Invalidate();
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                int selectAllY = 36;
                int itemH = _owner._itemHeight;

                if (e.Y >= selectAllY && e.Y < selectAllY + itemH)
                {
                    bool allChecked = true;
                    for (int i = 0; i < _owner._items.Count; i++)
                    {
                        if (!_owner._items[i].IsChecked) { allChecked = false; break; }
                    }
                    _owner.CheckAll(!allChecked);
                    Invalidate();
                }
                else if (e.Y >= selectAllY + itemH + 2)
                {
                    int index = (e.Y - (selectAllY + itemH + 2) + _scrollY) / itemH;
                    if (index >= 0 && index < _filteredItems.Count)
                    {
                        var item = _filteredItems[index];
                        int originalIdx = _owner._items.IndexOf(item);
                        _owner.SetItemChecked(originalIdx, !item.IsChecked);
                        Invalidate();
                    }
                }
            }
        }
    }
}
