using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{
    public enum ZeroComboBoxStyle
    {
        DropDownList,
        DropDown
    }

    /// <summary>
    /// Modern anti-aliased flat ComboBox control for ZeroUI.
    /// Provides lightweight dropdown selection, keyboard navigation, enum binding,
    /// and theme synchronization.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultProperty("SelectedIndex")]
    [DefaultEvent("SelectedIndexChanged")]
    [Description("Modern anti-aliased ComboBox dropdown control")]
    public class ZeroComboBox : Control
    {
        private readonly List<object> _items = new List<object>();
        private int _selectedIndex = -1;
        private string _placeholder = "Select an option...";
        private int _maxDropDownItems = 8;
        private int _itemHeight = 32;
        private ZeroComboBoxStyle _dropDownStyle = ZeroComboBoxStyle.DropDownList;

        private bool _isHovered = false;
        private bool _isFocused = false;
        private bool _isDroppedDown = false;

        private readonly ToolStripDropDown _dropdown;
        private readonly ComboListControl _listControl;

        public event EventHandler? SelectedIndexChanged;
        public event EventHandler? DropDownOpened;
        public event EventHandler? DropDownClosed;

        public ZeroComboBox()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(220, 36);
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            BackColor = Color.Transparent;

            _listControl = new ComboListControl(this);
            var host = new ToolStripControlHost(_listControl)
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AutoSize = false
            };

            _dropdown = new ToolStripDropDown
            {
                AutoClose = true,
                DropShadowEnabled = true,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            _dropdown.Items.Add(host);
            _dropdown.Opened += (s, e) =>
            {
                _isDroppedDown = true;
                Invalidate();
                DropDownOpened?.Invoke(this, EventArgs.Empty);
            };
            _dropdown.Closed += (s, e) =>
            {
                _isDroppedDown = false;
                Invalidate();
                DropDownClosed?.Invoke(this, EventArgs.Empty);
            };

            ZeroTheme.ThemeChanged += (s, e) =>
            {
                _listControl.UpdateTheme();
                Invalidate();
            };
            ZeroUIConfig.CornerStyleChanged += (s, e) => Invalidate();
            ZeroUIConfig.FontChanged += (s, e) =>
            {
                Font = ZeroUIConfig.DefaultFont;
                _listControl.Font = ZeroUIConfig.DefaultFont;
                Invalidate();
            };
        }

        [Category("Data")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Description("The items in the combo box")]
        public IList<object> Items => _items;

        [Category("Appearance")]
        [DefaultValue("Select an option...")]
        public string Placeholder
        {
            get => _placeholder;
            set
            {
                _placeholder = value ?? "";
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(ZeroComboBoxStyle.DropDownList)]
        public ZeroComboBoxStyle DropDownStyle
        {
            get => _dropDownStyle;
            set => _dropDownStyle = value;
        }

        [Category("Behavior")]
        [DefaultValue(8)]
        public int MaxDropDownItems
        {
            get => _maxDropDownItems;
            set => _maxDropDownItems = Math.Max(1, value);
        }

        [Category("Behavior")]
        [DefaultValue(32)]
        public int ItemHeight
        {
            get => _itemHeight;
            set
            {
                _itemHeight = Math.Max(20, value);
                _listControl.ItemHeight = _itemHeight;
            }
        }

        [Category("Behavior")]
        [DefaultValue(-1)]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value < -1 || value >= _items.Count) value = -1;
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    Invalidate();
                    SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [Browsable(false)]
        public object? SelectedItem
        {
            get => (_selectedIndex >= 0 && _selectedIndex < _items.Count) ? _items[_selectedIndex] : null;
            set
            {
                if (value == null)
                {
                    SelectedIndex = -1;
                    return;
                }
                int idx = _items.IndexOf(value);
                if (idx >= 0) SelectedIndex = idx;
            }
        }

        [Browsable(false)]
        public string SelectedText => SelectedItem?.ToString() ?? "";

        [Browsable(false)]
        public bool IsDroppedDown => _isDroppedDown;

        /// <summary>
        /// Populates items from an enum type.
        /// </summary>
        public void BindEnum<T>() where T : Enum
        {
            _items.Clear();
            foreach (var val in Enum.GetValues(typeof(T)))
            {
                _items.Add(val);
            }
            _selectedIndex = _items.Count > 0 ? 0 : -1;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Populates items from an enumerable source.
        /// </summary>
        public void SetItems(IEnumerable items)
        {
            _items.Clear();
            if (items != null)
            {
                foreach (var item in items)
                {
                    _items.Add(item);
                }
            }
            if (_selectedIndex >= _items.Count)
            {
                SelectedIndex = _items.Count > 0 ? 0 : -1;
            }
            else
            {
                Invalidate();
            }
        }

        public void ShowDropDown()
        {
            if (_items.Count == 0 || !Enabled) return;

            int visibleCount = Math.Min(_items.Count, _maxDropDownItems);
            int popH = visibleCount * _itemHeight + 4;
            int popW = Math.Max(Width, 160);

            _listControl.Size = new Size(popW, popH);
            _listControl.RefreshList();

            Point screenPt = PointToScreen(new Point(0, Height + 2));
            Screen currentScreen = Screen.FromControl(this);

            if (screenPt.Y + popH > currentScreen.WorkingArea.Bottom)
            {
                screenPt = PointToScreen(new Point(0, -popH - 2));
            }

            _dropdown.Show(screenPt);
        }

        public void CloseDropDown()
        {
            if (_isDroppedDown)
            {
                _dropdown.Close();
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Focus();
            if (_isDroppedDown) CloseDropDown();
            else ShowDropDown();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Down || (e.Alt && e.KeyCode == Keys.Down))
            {
                if (!_isDroppedDown) ShowDropDown();
                else _listControl.SelectNext();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                if (_isDroppedDown) _listControl.SelectPrevious();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                if (!_isDroppedDown) ShowDropDown();
                else
                {
                    _listControl.ConfirmSelection();
                    CloseDropDown();
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                CloseDropDown();
                e.Handled = true;
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            Invalidate();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            _isFocused = true;
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            _isFocused = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var palette = ZeroTheme.Colors;
            bool isDark = ZeroTheme.IsDark;

            int effRadius = ZeroUIConfig.GetEffectiveRadius(6);
            var borderRect = new Rectangle(1, 1, Width - 2, Height - 2);

            Color borderColor;
            Color bgColor;

            if (!Enabled)
            {
                borderColor = isDark ? Color.FromArgb(55, 60, 70) : Color.FromArgb(215, 220, 228);
                bgColor = isDark ? Color.FromArgb(28, 33, 43) : Color.FromArgb(243, 246, 249);
            }
            else if (_isDroppedDown || _isFocused)
            {
                borderColor = palette.Primary;
                bgColor = palette.Surface;
            }
            else if (_isHovered)
            {
                borderColor = palette.Primary;
                bgColor = palette.Surface;
            }
            else
            {
                borderColor = palette.Border;
                bgColor = palette.Surface;
            }

            // Fill and Border
            using (var path = ZeroUIConfig.CreateRoundedRectangle(borderRect, effRadius))
            {
                using (var bgBrush = new SolidBrush(bgColor))
                {
                    g.FillPath(bgBrush, path);
                }

                float penWidth = (_isDroppedDown || _isFocused) ? 1.5f : 1.0f;
                using (var pen = new Pen(borderColor, penWidth))
                {
                    g.DrawPath(pen, path);
                }
            }

            // Text
            int textLeft = 12;
            int textRight = Width - 32;
            var textRect = new Rectangle(textLeft, 0, Math.Max(10, textRight - textLeft), Height);

            string displayText = SelectedItem != null ? SelectedItem.ToString()! : _placeholder;
            Color textColor = !Enabled
                ? (isDark ? Color.FromArgb(100, 105, 115) : Color.FromArgb(160, 165, 175))
                : (SelectedItem != null ? palette.TextPrimary : palette.TextSecondary);

            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.WordEllipsis | TextFormatFlags.NoPrefix;
            TextRenderer.DrawText(g, displayText, Font, textRect, textColor, flags);

            // Vector Chevron (▼ or ▲)
            Color chevColor = Enabled ? palette.TextSecondary : Color.FromArgb(120, 125, 135);
            using var chevBrush = new SolidBrush(chevColor);
            float cx = Width - 18;
            float cy = Height / 2f;

            if (_isDroppedDown)
            {
                // Arrow up ▲
                PointF[] pts = new[]
                {
                    new PointF(cx - 4f, cy + 2f),
                    new PointF(cx + 4f, cy + 2f),
                    new PointF(cx, cy - 3f)
                };
                g.FillPolygon(chevBrush, pts);
            }
            else
            {
                // Arrow down ▼
                PointF[] pts = new[]
                {
                    new PointF(cx - 4f, cy - 2f),
                    new PointF(cx + 4f, cy - 2f),
                    new PointF(cx, cy + 3f)
                };
                g.FillPolygon(chevBrush, pts);
            }
        }

        /// <summary>
        /// Inner scrollable popup list.
        /// </summary>
        private class ComboListControl : Control
        {
            private readonly ZeroComboBox _owner;
            private int _hoveredIndex = -1;
            private int _scrollOffset = 0;
            private int _itemHeight = 32;
            private readonly VScrollBar _vScrollBar;

            public int ItemHeight
            {
                get => _itemHeight;
                set => _itemHeight = value;
            }

            public ComboListControl(ZeroComboBox owner)
            {
                _owner = owner;
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw, true);

                Font = owner.Font;
                UpdateTheme();

                _vScrollBar = new VScrollBar
                {
                    Dock = DockStyle.Right,
                    Width = 10,
                    Visible = false
                };
                _vScrollBar.Scroll += (s, e) =>
                {
                    _scrollOffset = _vScrollBar.Value;
                    Invalidate();
                };
                Controls.Add(_vScrollBar);

                MouseWheel += (s, e) =>
                {
                    if (!_vScrollBar.Visible) return;
                    int delta = e.Delta > 0 ? -1 : 1;
                    int newVal = Math.Max(0, Math.Min(_vScrollBar.Maximum, _scrollOffset + delta));
                    if (newVal != _scrollOffset)
                    {
                        _scrollOffset = newVal;
                        _vScrollBar.Value = _scrollOffset;
                        Invalidate();
                    }
                };
            }

            public void UpdateTheme()
            {
                var palette = ZeroTheme.Colors;
                BackColor = palette.CardBackground;
                Invalidate();
            }

            public void RefreshList()
            {
                _scrollOffset = 0;
                int count = _owner.Items.Count;
                int visibleCount = Height / _itemHeight;

                if (count > visibleCount)
                {
                    _vScrollBar.Visible = true;
                    _vScrollBar.Maximum = count - visibleCount;
                    _vScrollBar.LargeChange = 1;
                    _vScrollBar.Value = 0;
                }
                else
                {
                    _vScrollBar.Visible = false;
                }

                if (_owner.SelectedIndex >= 0)
                {
                    _hoveredIndex = _owner.SelectedIndex;
                    if (_hoveredIndex >= visibleCount && _vScrollBar.Visible)
                    {
                        _scrollOffset = Math.Min(_vScrollBar.Maximum, _hoveredIndex - visibleCount + 1);
                        _vScrollBar.Value = _scrollOffset;
                    }
                }
                Invalidate();
            }

            public void SelectNext()
            {
                int count = _owner.Items.Count;
                if (count == 0) return;
                _hoveredIndex = (_hoveredIndex + 1) % count;
                EnsureVisible(_hoveredIndex);
                Invalidate();
            }

            public void SelectPrevious()
            {
                int count = _owner.Items.Count;
                if (count == 0) return;
                _hoveredIndex = (_hoveredIndex - 1 + count) % count;
                EnsureVisible(_hoveredIndex);
                Invalidate();
            }

            public void ConfirmSelection()
            {
                if (_hoveredIndex >= 0 && _hoveredIndex < _owner.Items.Count)
                {
                    _owner.SelectedIndex = _hoveredIndex;
                }
            }

            private void EnsureVisible(int index)
            {
                int visibleCount = Height / _itemHeight;
                if (index < _scrollOffset)
                {
                    _scrollOffset = index;
                    if (_vScrollBar.Visible) _vScrollBar.Value = _scrollOffset;
                }
                else if (index >= _scrollOffset + visibleCount)
                {
                    _scrollOffset = index - visibleCount + 1;
                    if (_vScrollBar.Visible) _vScrollBar.Value = Math.Min(_vScrollBar.Maximum, _scrollOffset);
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                int index = (e.Y / _itemHeight) + _scrollOffset;
                if (index >= 0 && index < _owner.Items.Count && index != _hoveredIndex)
                {
                    _hoveredIndex = index;
                    Invalidate();
                }
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                if (e.Button == MouseButtons.Left)
                {
                    int index = (e.Y / _itemHeight) + _scrollOffset;
                    if (index >= 0 && index < _owner.Items.Count)
                    {
                        _owner.SelectedIndex = index;
                        _owner.CloseDropDown();
                    }
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var palette = ZeroTheme.Colors;
                int count = _owner.Items.Count;
                int renderW = _vScrollBar.Visible ? Width - _vScrollBar.Width : Width;
                int visibleCount = (Height / _itemHeight) + 1;

                using var selBgBrush = new SolidBrush(Color.FromArgb(40, palette.Primary));
                using var hoverBgBrush = new SolidBrush(Color.FromArgb(20, palette.Primary));
                using var borderPen = new Pen(palette.Border, 1f);

                // Draw outer border
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

                for (int i = 0; i < visibleCount; i++)
                {
                    int itemIdx = _scrollOffset + i;
                    if (itemIdx >= count) break;

                    int itemY = i * _itemHeight;
                    var itemRect = new Rectangle(2, itemY, renderW - 4, _itemHeight);

                    bool isSelected = (itemIdx == _owner.SelectedIndex);
                    bool isHovered = (itemIdx == _hoveredIndex);

                    if (isSelected)
                    {
                        g.FillRectangle(selBgBrush, itemRect);
                        // Left indicator bar
                        using var barBrush = new SolidBrush(palette.Primary);
                        g.FillRectangle(barBrush, 2, itemY + 4, 3, _itemHeight - 8);
                    }
                    else if (isHovered)
                    {
                        g.FillRectangle(hoverBgBrush, itemRect);
                    }

                    string label = _owner.Items[itemIdx]?.ToString() ?? "";
                    Color txtColor = isSelected ? palette.Primary : palette.TextPrimary;

                    var textRect = new Rectangle(12, itemY, renderW - 20, _itemHeight);
                    var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.WordEllipsis | TextFormatFlags.NoPrefix;
                    TextRenderer.DrawText(g, label, Font, textRect, txtColor, flags);
                }
            }
        }
    }
}
