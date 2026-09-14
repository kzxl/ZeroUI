using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.Editors;
using ZeroUI.Core.Theme;
using ZeroUI.WinForms.Base;
using ZeroUI.WinForms.Icons;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// Specifies how item widths are allocated inside a <see cref="ButtonGroup"/>.
    /// </summary>
    public enum ButtonGroupSizeMode
    {
        /// <summary>Each button is sized according to its text, icon, and padding.</summary>
        AutoFit,
        /// <summary>All buttons receive an equal fraction of the available width.</summary>
        EqualWidth
    }

    /// <summary>
    /// Modern connected button group (Action Cluster / Action Strip) with single-HWND rendering,
    /// seamless border geometry, toggle grouping, and theme reactivity.
    /// </summary>
    [ToolboxItem(true)]
    [ToolboxBitmap(typeof(ZeroIcons), "ButtonGroup.bmp")]
    [Category("ZeroUI - Editors")]
    [DefaultEvent("ItemClick")]
    [DefaultProperty("Items")]
    [Description("Seamless connected button cluster with single-HWND rendering, toggle grouping, and theme reactivity")]
    public class ButtonGroup : ZeroControlBase
    {
        private readonly ButtonGroupModel _model = new ButtonGroupModel();
        private readonly List<Rectangle> _itemBounds = new List<Rectangle>();
        private readonly ToolTip _toolTip = new ToolTip();
        private int _hoveredIndex = -1;
        private int _pressedIndex = -1;
        private int _borderRadius = 6;
        private ButtonGroupSizeMode _sizeMode = ButtonGroupSizeMode.AutoFit;
        private int _itemPaddingHorizontal = 16;
        private string? _currentToolTipText;

        public event EventHandler<ButtonGroupItemEventArgs>? ItemClick;
        public event EventHandler<ButtonGroupItem>? SelectionChanged;

        public ButtonGroup()
        {
            Size = new Size(280, 36);
            Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            Cursor = Cursors.Hand;

            _model.ItemClicked += (s, e) => ItemClick?.Invoke(this, e);
            _model.SelectionChanged += (s, item) => SelectionChanged?.Invoke(this, item);
            _model.ItemsChanged += (s, e) =>
            {
                RecalculateLayout();
                Invalidate();
            };

            ZeroUIConfig.CornerStyleChanged += (s, e) =>
            {
                UpdateRegion();
                Invalidate();
            };
            ZeroUIConfig.FontChanged += (s, e) =>
            {
                Font = new Font(ZeroUIConfig.DefaultFont.FontFamily, 9f, FontStyle.Regular);
                RecalculateLayout();
                UpdateRegion();
                Invalidate();
            };
        }

        #region Properties

        [Browsable(false)]
        public ButtonGroupModel Model => _model;

        [Category("Behavior")]
        [DefaultValue(ButtonGroupSelectionMode.None)]
        public ButtonGroupSelectionMode SelectionMode
        {
            get => _model.SelectionMode;
            set
            {
                if (_model.SelectionMode != value)
                {
                    _model.SelectionMode = value;
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue(ButtonGroupSizeMode.AutoFit)]
        public ButtonGroupSizeMode SizeMode
        {
            get => _sizeMode;
            set
            {
                if (_sizeMode != value)
                {
                    _sizeMode = value;
                    RecalculateLayout();
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue(6)]
        public int BorderRadius
        {
            get => _borderRadius;
            set
            {
                _borderRadius = Math.Max(0, value);
                UpdateRegion();
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(16)]
        public int ItemPaddingHorizontal
        {
            get => _itemPaddingHorizontal;
            set
            {
                _itemPaddingHorizontal = Math.Max(4, value);
                RecalculateLayout();
                Invalidate();
            }
        }

        [Browsable(false)]
        public IReadOnlyList<ButtonGroupItem> Items => _model.Items;

        [Browsable(false)]
        public ButtonGroupItem? SelectedItem => _model.SelectedItem;

        #endregion

        #region Builder API

        public ButtonGroupItem AddButton(string id, string text, string? iconGlyph = null, ButtonGroupItemStyle style = ButtonGroupItemStyle.Secondary, Action<ButtonGroupItem>? action = null)
        {
            var item = new ButtonGroupItem(id, text, iconGlyph, ButtonGroupItemType.Push, action)
            {
                Style = style
            };
            _model.Add(item);
            return item;
        }

        public ButtonGroupItem AddToggle(string id, string text, string? iconGlyph = null, bool isChecked = false, Action<ButtonGroupItem>? action = null)
        {
            var item = new ButtonGroupItem(id, text, iconGlyph, ButtonGroupItemType.Toggle, action)
            {
                IsChecked = isChecked,
                Style = ButtonGroupItemStyle.Secondary
            };
            _model.Add(item);
            return item;
        }

        public ButtonGroupItem AddDropDown(string id, string text, string? iconGlyph = null, Action<ButtonGroupItem>? action = null)
        {
            var item = new ButtonGroupItem(id, text, iconGlyph, ButtonGroupItemType.DropDown, action);
            _model.Add(item);
            return item;
        }

        public ButtonGroupItem AddSeparator()
        {
            var item = new ButtonGroupItem(Guid.NewGuid().ToString("N"), string.Empty, null, ButtonGroupItemType.Separator);
            _model.Add(item);
            return item;
        }

        public void Clear()
        {
            _model.Clear();
        }

        #endregion

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RecalculateLayout();
            UpdateRegion();
            Invalidate();
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            int effRadius = ZeroUIConfig.GetEffectiveRadius(_borderRadius);
            if (effRadius > 0)
            {
                using var path = CreateRoundedRectangle(new Rectangle(0, 0, Width, Height), effRadius, effRadius, effRadius, effRadius);
                Region = new Region(path);
            }
            else
            {
                Region = null;
            }
        }

        protected override void OnThemeChanged(ZeroSkin skin)
        {
            base.OnThemeChanged(skin);
            Invalidate();
        }

        private void RecalculateLayout()
        {
            _itemBounds.Clear();
            if (_model.Count == 0 || Width <= 0 || Height <= 0) return;

            int visibleCount = 0;
            for (int i = 0; i < _model.Count; i++)
            {
                if (_model[i].IsVisible) visibleCount++;
            }
            if (visibleCount == 0) return;

            int currentX = 0;
            int totalAvailableWidth = Width - 1;

            if (_sizeMode == ButtonGroupSizeMode.EqualWidth)
            {
                int itemW = totalAvailableWidth / visibleCount;
                for (int i = 0; i < _model.Count; i++)
                {
                    var item = _model[i];
                    if (!item.IsVisible)
                    {
                        _itemBounds.Add(Rectangle.Empty);
                        continue;
                    }

                    int w = (i == _model.Count - 1) ? (totalAvailableWidth - currentX) : itemW;
                    _itemBounds.Add(new Rectangle(currentX, 0, Math.Max(1, w), Height - 1));
                    currentX += itemW;
                }
            }
            else
            {
                // AutoFit based on text measurement
                using var bmp = new Bitmap(1, 1);
                using var g = Graphics.FromImage(bmp);

                var measuredWidths = new int[_model.Count];
                int totalMeasuredWidth = 0;

                for (int i = 0; i < _model.Count; i++)
                {
                    var item = _model[i];
                    if (!item.IsVisible)
                    {
                        measuredWidths[i] = 0;
                        continue;
                    }

                    if (item.Type == ButtonGroupItemType.Separator)
                    {
                        measuredWidths[i] = 6;
                    }
                    else
                    {
                        string displayText = string.IsNullOrEmpty(item.IconGlyph)
                            ? item.Text
                            : $"{item.IconGlyph} {item.Text}".Trim();

                        Size size = TextRenderer.MeasureText(g, displayText, Font);
                        int w = size.Width + (_itemPaddingHorizontal * 2);

                        if (!string.IsNullOrEmpty(item.BadgeText))
                        {
                            w += 24;
                        }
                        if (item.Type == ButtonGroupItemType.DropDown)
                        {
                            w += 14;
                        }

                        measuredWidths[i] = Math.Max(32, w);
                    }
                    totalMeasuredWidth += measuredWidths[i];
                }

                // If total measured width is smaller than control Width, distribute extra space proportionally
                float scaleFactor = totalMeasuredWidth > 0 && totalMeasuredWidth < totalAvailableWidth
                    ? (float)totalAvailableWidth / totalMeasuredWidth
                    : 1.0f;

                for (int i = 0; i < _model.Count; i++)
                {
                    var item = _model[i];
                    if (!item.IsVisible)
                    {
                        _itemBounds.Add(Rectangle.Empty);
                        continue;
                    }

                    int w = (int)(measuredWidths[i] * scaleFactor);
                    if (i == _model.Count - 1)
                    {
                        w = Math.Max(1, totalAvailableWidth - currentX);
                    }

                    _itemBounds.Add(new Rectangle(currentX, 0, Math.Max(1, w), Height - 1));
                    currentX += w;
                }
            }
        }

        #region Mouse Interaction & Hit-Testing

        private int HitTest(int x, int y)
        {
            for (int i = 0; i < _itemBounds.Count; i++)
            {
                if (_itemBounds[i].Contains(x, y))
                {
                    var item = _model[i];
                    if (item.IsVisible && item.IsEnabled && item.Type != ButtonGroupItemType.Separator)
                    {
                        return i;
                    }
                    return -1;
                }
            }
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int idx = HitTest(e.X, e.Y);
            if (_hoveredIndex != idx)
            {
                _hoveredIndex = idx;
                Invalidate();

                // Handle tooltip update
                if (_hoveredIndex >= 0 && _hoveredIndex < _model.Count)
                {
                    string? tip = _model[_hoveredIndex].ToolTip;
                    if (!string.IsNullOrEmpty(tip) && tip != _currentToolTipText)
                    {
                        _currentToolTipText = tip;
                        _toolTip.SetToolTip(this, tip);
                    }
                }
                else
                {
                    _currentToolTipText = null;
                    _toolTip.SetToolTip(this, null);
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoveredIndex = -1;
            _pressedIndex = -1;
            _currentToolTipText = null;
            _toolTip.SetToolTip(this, null);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _pressedIndex = HitTest(e.X, e.Y);
                if (_pressedIndex >= 0)
                {
                    Invalidate();
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && _pressedIndex >= 0)
            {
                int releasedIndex = HitTest(e.X, e.Y);
                if (releasedIndex == _pressedIndex)
                {
                    _model.TriggerClick(releasedIndex);
                }
                _pressedIndex = -1;
                Invalidate();
            }
        }

        #endregion

        #region Paint Pipeline

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var palette = CurrentPalette;

            // 1. Fill parent background to eliminate edge clipping artifacts
            Color parentBg = ZeroUIConfig.GetParentBackground(this, palette.Background);
            using (var brushParent = new SolidBrush(parentBg))
            {
                g.FillRectangle(brushParent, ClientRectangle);
            }

            if (_model.Count == 0 || _itemBounds.Count != _model.Count) return;

            Rectangle groupRect = new Rectangle(0, 0, Width - 1, Height - 1);
            int effRadius = ZeroUIConfig.GetEffectiveRadius(_borderRadius);

            // 2. Draw Group Outer Background & Clip Path
            using (var groupPath = CreateRoundedRectangle(groupRect, effRadius, effRadius, effRadius, effRadius))
            {
                using var bgBrush = new SolidBrush(palette.Surface);
                g.FillPath(bgBrush, groupPath);

                var oldClip = g.Clip;
                g.SetClip(groupPath, CombineMode.Intersect);

                // 3. Render Each Item
                int firstVisible = -1, lastVisible = -1;
                for (int i = 0; i < _model.Count; i++)
                {
                    if (_model[i].IsVisible)
                    {
                        if (firstVisible < 0) firstVisible = i;
                        lastVisible = i;
                    }
                }

                for (int i = 0; i < _model.Count; i++)
                {
                    var item = _model[i];
                    if (!item.IsVisible) continue;
                    var bounds = _itemBounds[i];
                    if (bounds.IsEmpty) continue;

                    bool isFirst = (i == firstVisible);
                    bool isLast = (i == lastVisible);
                    bool isHovered = (_hoveredIndex == i);
                    bool isPressed = (_pressedIndex == i);

                    if (item.Type == ButtonGroupItemType.Separator)
                    {
                        // Draw vertical separator
                        int lineX = bounds.X + bounds.Width / 2;
                        using var sepPen = new Pen(palette.Border, 1f);
                        g.DrawLine(sepPen, lineX, bounds.Y + 6, lineX, bounds.Bottom - 6);
                        continue;
                    }

                    // Compute background color
                    var (itemBg, itemFg) = GetItemColors(item, isHovered, isPressed, palette);

                    if (itemBg != Color.Transparent)
                    {
                        using var itemBrush = new SolidBrush(itemBg);
                        g.FillRectangle(itemBrush, bounds);
                    }

                    // Draw Content: Glyph + Text
                    string textToDraw = string.IsNullOrEmpty(item.IconGlyph)
                        ? item.Text
                        : $"{item.IconGlyph}  {item.Text}".Trim();

                    if (item.Type == ButtonGroupItemType.DropDown)
                    {
                        textToDraw += " ▾";
                    }

                    Rectangle textRect = new Rectangle(bounds.X + 4, bounds.Y, bounds.Width - 8, bounds.Height);
                    if (!string.IsNullOrEmpty(item.BadgeText))
                    {
                        textRect.Width -= 22;
                    }

                    TextRenderer.DrawText(
                        g,
                        textToDraw,
                        item.IsChecked ? new Font(Font, FontStyle.Bold) : Font,
                        textRect,
                        itemFg,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

                    // Draw Optional Badge
                    if (!string.IsNullOrEmpty(item.BadgeText))
                    {
                        Size badgeSize = TextRenderer.MeasureText(item.BadgeText, Font);
                        int badgeW = Math.Max(18, badgeSize.Width + 6);
                        int badgeH = 16;
                        Rectangle badgeRect = new Rectangle(bounds.Right - badgeW - 6, (bounds.Height - badgeH) / 2, badgeW, badgeH);

                        using var badgePath = CreateRoundedRectangle(badgeRect, 8, 8, 8, 8);
                        using var badgeBg = new SolidBrush(Color.FromArgb(220, 38, 38));
                        g.FillPath(badgeBg, badgePath);

                        TextRenderer.DrawText(
                            g,
                            item.BadgeText,
                            new Font("Segoe UI", 7.5f, FontStyle.Bold),
                            badgeRect,
                            Color.White,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                    }

                    // Draw 1px divider between items (if not last item and next item is not a separator)
                    if (!isLast && item.Type != ButtonGroupItemType.Separator && (i + 1 < _model.Count && _model[i + 1].Type != ButtonGroupItemType.Separator))
                    {
                        using var divPen = new Pen(palette.Border, 1f);
                        g.DrawLine(divPen, bounds.Right, bounds.Y + 4, bounds.Right, bounds.Bottom - 4);
                    }
                }

                g.Clip = oldClip;

                // 4. Draw Outer Group Border cleanly over clipped items
                using var borderPen = new Pen(palette.Border, 1f);
                g.DrawPath(borderPen, groupPath);
            }
        }

        private (Color bg, Color fg) GetItemColors(ButtonGroupItem item, bool isHovered, bool isPressed, ZeroThemePalette palette)
        {
            if (!item.IsEnabled || !Enabled)
            {
                return (Color.Transparent, palette.TextSecondary);
            }

            if (item.IsChecked)
            {
                // Active / Checked Toggle
                return (palette.Primary, Color.White);
            }

            if (isPressed)
            {
                return (palette.Hover, palette.Primary);
            }

            if (isHovered)
            {
                return (palette.Hover, palette.TextPrimary);
            }

            // Normal idle state based on item.Style
            return item.Style switch
            {
                ButtonGroupItemStyle.Primary => (palette.Primary, Color.White),
                ButtonGroupItemStyle.Success => (palette.Success, Color.White),
                ButtonGroupItemStyle.Danger => (palette.Danger, Color.White),
                ButtonGroupItemStyle.Ghost => (Color.Transparent, palette.TextPrimary),
                _ => (Color.Transparent, palette.TextPrimary)
            };
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int topLeft, int topRight, int bottomRight, int bottomLeft)
        {
            var path = new GraphicsPath();
            int x = rect.X, y = rect.Y, w = rect.Width, h = rect.Height;

            if (w <= 0 || h <= 0) return path;

            // Top-left
            if (topLeft > 0)
                path.AddArc(x, y, topLeft * 2, topLeft * 2, 180, 90);
            else
                path.AddLine(x, y, x, y);

            // Top-right
            if (topRight > 0)
                path.AddArc(x + w - topRight * 2, y, topRight * 2, topRight * 2, 270, 90);
            else
                path.AddLine(x + w, y, x + w, y);

            // Bottom-right
            if (bottomRight > 0)
                path.AddArc(x + w - bottomRight * 2, y + h - bottomRight * 2, bottomRight * 2, bottomRight * 2, 0, 90);
            else
                path.AddLine(x + w, y + h, x + w, y + h);

            // Bottom-left
            if (bottomLeft > 0)
                path.AddArc(x, y + h - bottomLeft * 2, bottomLeft * 2, bottomLeft * 2, 90, 90);
            else
                path.AddLine(x, y + h, x, y + h);

            path.CloseFigure();
            return path;
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
