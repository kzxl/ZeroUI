using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.Editors;
using ZeroUI.Core.Localization;
using ZeroUI.Core.Theme;
using ZeroUI.WinForms.Base;
using ZeroUI.WinForms.Icons;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{
    public class TokenEventArgs : EventArgs
    {
        public string Token { get; }
        public int Index { get; }

        public TokenEventArgs(string token, int index)
        {
            Token = token;
            Index = index;
        }
    }

    /// <summary>
    /// Modern anti-aliased Tag/Token/Chip input editor for ZeroUI WinForms.
    /// Supports discrete tag badges with dismiss icons, inline text entry, backspace removal,
    /// and auto-wrapping or horizontal scroll.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultEvent("TokenAdded")]
    [Description("Modern token/chip input editor rendering discrete tag badges with dismiss buttons")]
    [ToolboxBitmap(typeof(ZeroIcons), "TokenEdit.bmp")]
    public class TokenEdit : ZeroControlBase, IZeroEditor
    {
        private readonly List<string> _tokens = new List<string>();
        private readonly List<Rectangle> _tokenBounds = new List<Rectangle>();
        private readonly List<Rectangle> _closeBounds = new List<Rectangle>();

        private readonly TextBox _inputBox;
        private string? _placeholder;
        private int _tokenHeight = 24;
        private int _tokenSpacing = 6;
        private int _hoveredCloseIndex = -1;

        private IEnumerable<string>? _autocompleteSource;
        private readonly List<string> _cachedSource = new List<string>();
        private readonly ZeroDropDownHost _dropdown;
        private readonly TokenSuggestionPopupControl _popupControl;

        public event EventHandler<TokenEventArgs>? TokenAdded;
        public event EventHandler<TokenEventArgs>? TokenRemoved;
        public event EventHandler? TokensChanged;
        public event EventHandler? EditValueChanged;

        [Category("ZeroUI - Data")]
        [Description("Optional collection of predefined suggestion strings for token autocomplete.")]
        [DefaultValue(null)]
        public IEnumerable<string>? AutocompleteSource
        {
            get => _autocompleteSource;
            set
            {
                _autocompleteSource = value;
                _cachedSource.Clear();
                if (value != null)
                {
                    foreach (var item in value)
                    {
                        if (!string.IsNullOrWhiteSpace(item))
                            _cachedSource.Add(item.Trim());
                    }
                }
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object? EditValue
        {
            get => _tokens.ToArray();
            set
            {
                _tokens.Clear();
                if (value != null)
                {
                    if (value is System.Collections.IEnumerable enumerable && !(value is string))
                    {
                        foreach (var item in enumerable)
                        {
                            if (item != null)
                            {
                                string s = item.ToString()?.Trim() ?? "";
                                if (!string.IsNullOrEmpty(s) && !_tokens.Contains(s))
                                    _tokens.Add(s);
                            }
                        }
                    }
                    else
                    {
                        string str = value.ToString() ?? "";
                        var parts = str.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var part in parts)
                        {
                            string s = part.Trim();
                            if (!string.IsNullOrEmpty(s) && !_tokens.Contains(s))
                                _tokens.Add(s);
                        }
                    }
                }
                TokensChanged?.Invoke(this, EventArgs.Empty);
                Relayout();
            }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool IsModified { get; set; } = false;

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool ReadOnly
        {
            get => _inputBox?.ReadOnly ?? false;
            set
            {
                if (_inputBox != null)
                {
                    _inputBox.ReadOnly = value;
                    Invalidate();
                }
            }
        }

        public void Reset()
        {
            ClearTokens();
            IsModified = false;
        }

        public void Clear() => Reset();

        [Category("Appearance")]
        [DefaultValue(null)]
        public string Placeholder
        {
            get => _placeholder ?? ZeroLocalizer.GetString(ZeroStringId.TokenEditPlaceholder);
            set { _placeholder = value; Invalidate(); }
        }

        [Browsable(false)]
        public IReadOnlyList<string> Tokens => _tokens;

        public TokenEdit()
        {
            SetStyle(ControlStyles.Selectable, true);

            ZeroLocalizer.CultureChanged += (s, e) => Invalidate();

            Cursor = Cursors.IBeam;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            BackColor = Color.Transparent;

            _inputBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = Font,
                BackColor = ZeroTheme.Colors.Surface,
                ForeColor = ZeroTheme.Colors.TextPrimary
            };
            _inputBox.KeyDown += InputBox_KeyDown;
            _inputBox.TextChanged += InputBox_TextChanged;
            Controls.Add(_inputBox);

            _popupControl = new TokenSuggestionPopupControl(this);
            _dropdown = new ZeroDropDownHost
            {
                Content = _popupControl
            };

            Size = new Size(300, 40);
        }

        protected override void OnThemeChanged(ZeroSkin skin)
        {
            base.OnThemeChanged(skin);
            if (_inputBox == null) return;
            _inputBox.BackColor = CurrentPalette.Surface;
            _inputBox.ForeColor = CurrentPalette.TextPrimary;
            Invalidate();
        }

        public void AddToken(string token)
        {
            if (ReadOnly || !Enabled) return;
            if (string.IsNullOrWhiteSpace(token)) return;
            string clean = token.Trim();
            if (!_tokens.Contains(clean))
            {
                _tokens.Add(clean);
                int idx = _tokens.Count - 1;
                IsModified = true;
                TokenAdded?.Invoke(this, new TokenEventArgs(clean, idx));
                TokensChanged?.Invoke(this, EventArgs.Empty);
                EditValueChanged?.Invoke(this, EventArgs.Empty);
                Relayout();
            }
        }

        public void RemoveToken(int index)
        {
            if (ReadOnly || !Enabled) return;
            if (index < 0 || index >= _tokens.Count) return;
            string token = _tokens[index];
            _tokens.RemoveAt(index);
            IsModified = true;
            TokenRemoved?.Invoke(this, new TokenEventArgs(token, index));
            TokensChanged?.Invoke(this, EventArgs.Empty);
            EditValueChanged?.Invoke(this, EventArgs.Empty);
            Relayout();
        }

        public void ClearTokens()
        {
            if (ReadOnly || !Enabled) return;
            if (_tokens.Count > 0)
            {
                _tokens.Clear();
                IsModified = true;
                TokensChanged?.Invoke(this, EventArgs.Empty);
                EditValueChanged?.Invoke(this, EventArgs.Empty);
                Relayout();
            }
        }

        private void InputBox_TextChanged(object? sender, EventArgs e)
        {
            Relayout();
            if (_cachedSource.Count > 0)
            {
                string query = _inputBox.Text.Trim();
                if (!string.IsNullOrEmpty(query))
                {
                    var matches = new List<string>();
                    for (int i = 0; i < _cachedSource.Count; i++)
                    {
                        string s = _cachedSource[i];
                        bool alreadyAdded = false;
                        for (int t = 0; t < _tokens.Count; t++)
                        {
                            if (string.Equals(_tokens[t], s, StringComparison.OrdinalIgnoreCase))
                            {
                                alreadyAdded = true;
                                break;
                            }
                        }
                        if (!alreadyAdded && s.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            matches.Add(s);
                            if (matches.Count >= 20) break;
                        }
                    }

                    if (matches.Count > 0)
                    {
                        _popupControl.SetSuggestions(matches);
                        ShowSuggestions();
                    }
                    else
                    {
                        _dropdown.Close();
                    }
                }
                else
                {
                    _dropdown.Close();
                }
            }
        }

        private void ShowSuggestions()
        {
            if (ReadOnly || !Enabled) return;
            int popW = Math.Max(Width, 220);
            int popH = _popupControl.PreferredHeight;
            _popupControl.Size = new Size(popW, popH);
            _dropdown.ShowDropDown(this, popW, popH);
        }

        internal void SelectSuggestion(string suggestion)
        {
            AddToken(suggestion);
            _inputBox.Text = string.Empty;
            _dropdown.Close();
            _inputBox.Focus();
        }

        private void InputBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (_dropdown.Visible)
            {
                if (e.KeyCode == Keys.Down)
                {
                    _popupControl.SelectNext();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
                if (e.KeyCode == Keys.Up)
                {
                    _popupControl.SelectPrevious();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
                {
                    if (_popupControl.SelectedItem != null)
                    {
                        SelectSuggestion(_popupControl.SelectedItem);
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        return;
                    }
                }
                if (e.KeyCode == Keys.Escape)
                {
                    _dropdown.Close();
                    e.Handled = true;
                    return;
                }
            }

            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Oemcomma)
            {
                e.SuppressKeyPress = true;
                string text = _inputBox.Text.Trim().TrimEnd(',');
                if (!string.IsNullOrEmpty(text))
                {
                    AddToken(text);
                    _inputBox.Text = string.Empty;
                }
            }
            else if (e.KeyCode == Keys.Back && string.IsNullOrEmpty(_inputBox.Text) && _tokens.Count > 0)
            {
                RemoveToken(_tokens.Count - 1);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Relayout();
        }

        private void Relayout()
        {
            if (_inputBox == null) return;

            _tokenBounds.Clear();
            _closeBounds.Clear();

            int curX = 8;
            int curY = (Height - _tokenHeight) / 2;

            using (var g = CreateGraphics())
            {
                for (int i = 0; i < _tokens.Count; i++)
                {
                    var size = g.MeasureString(_tokens[i], Font);
                    int tokenW = (int)size.Width + 28; // text + padding + close button
                    var tokenRect = new Rectangle(curX, curY, tokenW, _tokenHeight);
                    var closeRect = new Rectangle(curX + tokenW - 18, curY + (_tokenHeight - 12) / 2, 12, 12);

                    _tokenBounds.Add(tokenRect);
                    _closeBounds.Add(closeRect);

                    curX += tokenW + _tokenSpacing;
                }

                // Position input box
                int inputW = Math.Max(80, Width - curX - 10);
                _inputBox.Location = new Point(curX, (Height - _inputBox.Height) / 2);
                _inputBox.Width = inputW;
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var colors = CurrentPalette;
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

            // Background & Border
            using (var path = CreateRoundedRectanglePath(bounds, 4))
            {
                using (var brush = new SolidBrush(colors.Surface))
                {
                    g.FillPath(brush, path);
                }

                Color borderColor = _inputBox.Focused ? colors.Primary : colors.Border;
                using (var pen = new Pen(borderColor, _inputBox.Focused ? 1.5f : 1.0f))
                {
                    g.DrawPath(pen, path);
                }
            }

            // Draw Tokens
            for (int i = 0; i < _tokens.Count; i++)
            {
                if (i >= _tokenBounds.Count) break;
                var tRect = _tokenBounds[i];
                var cRect = _closeBounds[i];

                using (var tPath = CreateRoundedRectanglePath(tRect, 4))
                {
                    using (var tBrush = new SolidBrush(colors.HeaderBackground))
                    {
                        g.FillPath(tBrush, tPath);
                    }
                    using (var tPen = new Pen(colors.Border, 1f))
                    {
                        g.DrawPath(tPen, tPath);
                    }
                }

                // Draw Token Text
                using (var brush = new SolidBrush(colors.TextPrimary))
                {
                    var textRect = new Rectangle(tRect.X + 6, tRect.Y, tRect.Width - 22, tRect.Height);
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Center,
                        Trimming = StringTrimming.EllipsisCharacter
                    };
                    g.DrawString(_tokens[i], Font, brush, textRect, sf);
                }

                // Draw Close X
                Color xColor = (i == _hoveredCloseIndex) ? colors.Danger : colors.TextSecondary;
                using (var pen = new Pen(xColor, 1.4f))
                {
                    g.DrawLine(pen, cRect.X + 2, cRect.Y + 2, cRect.Right - 2, cRect.Bottom - 2);
                    g.DrawLine(pen, cRect.Right - 2, cRect.Y + 2, cRect.X + 2, cRect.Bottom - 2);
                }
            }

            // Draw Placeholder if empty
            if (_tokens.Count == 0 && string.IsNullOrEmpty(_inputBox.Text) && !_inputBox.Focused)
            {
                using (var brush = new SolidBrush(colors.TextSecondary))
                {
                    var sf = new StringFormat { LineAlignment = StringAlignment.Center };
                    g.DrawString(Placeholder, Font, brush, new Rectangle(12, 0, Width - 24, Height), sf);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int prevIdx = _hoveredCloseIndex;
            _hoveredCloseIndex = -1;

            for (int i = 0; i < _closeBounds.Count; i++)
            {
                if (_closeBounds[i].Contains(e.Location))
                {
                    _hoveredCloseIndex = i;
                    break;
                }
            }

            if (prevIdx != _hoveredCloseIndex)
            {
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                for (int i = 0; i < _closeBounds.Count; i++)
                {
                    if (_closeBounds[i].Contains(e.Location))
                    {
                        RemoveToken(i);
                        return;
                    }
                }
                _inputBox.Focus();
            }
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dropdown.Dispose();
            }
            base.Dispose(disposing);
        }

        private class TokenSuggestionPopupControl : Control
        {
            private readonly TokenEdit _owner;
            private readonly List<string> _suggestions = new List<string>();
            private int _selectedIndex = -1;
            private int _hoveredIndex = -1;
            private const int ItemHeight = 28;

            public string? SelectedItem => (_selectedIndex >= 0 && _selectedIndex < _suggestions.Count)
                ? _suggestions[_selectedIndex]
                : null;

            public int PreferredHeight => Math.Min(220, Math.Max(32, _suggestions.Count * ItemHeight + 4));

            public TokenSuggestionPopupControl(TokenEdit owner)
            {
                _owner = owner;
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw, true);
                DoubleBuffered = true;
                Cursor = Cursors.Hand;
            }

            public void SetSuggestions(List<string> items)
            {
                _suggestions.Clear();
                _suggestions.AddRange(items);
                _selectedIndex = items.Count > 0 ? 0 : -1;
                _hoveredIndex = -1;
                Invalidate();
            }

            public void SelectNext()
            {
                if (_suggestions.Count == 0) return;
                _selectedIndex = (_selectedIndex + 1) % _suggestions.Count;
                Invalidate();
            }

            public void SelectPrevious()
            {
                if (_suggestions.Count == 0) return;
                _selectedIndex = (_selectedIndex - 1 + _suggestions.Count) % _suggestions.Count;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var colors = _owner.CurrentPalette;
                g.Clear(colors.Surface);

                using (var borderPen = new Pen(colors.Border, 1f))
                {
                    g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
                }

                for (int i = 0; i < _suggestions.Count; i++)
                {
                    int itemY = 2 + i * ItemHeight;
                    if (itemY >= Height) break;

                    var itemRect = new Rectangle(3, itemY, Width - 6, ItemHeight);
                    bool isSelected = (i == _selectedIndex);
                    bool isHovered = (i == _hoveredIndex);

                    if (isSelected || isHovered)
                    {
                        Color highlightBg = isSelected
                            ? Color.FromArgb(35, colors.Primary)
                            : Color.FromArgb(18, colors.Primary);

                        using (var hPath = CreateRoundedRectanglePath(itemRect, 4))
                        {
                            using (var hBrush = new SolidBrush(highlightBg))
                            {
                                g.FillPath(hBrush, hPath);
                            }
                            if (isSelected)
                            {
                                using (var hPen = new Pen(Color.FromArgb(90, colors.Primary), 1f))
                                {
                                    g.DrawPath(hPen, hPath);
                                }
                            }
                        }
                    }

                    Color textColor = isSelected ? colors.Primary : colors.TextPrimary;
                    using (var textBrush = new SolidBrush(textColor))
                    {
                        var textRect = new Rectangle(itemRect.X + 8, itemRect.Y, itemRect.Width - 16, itemRect.Height);
                        var sf = new StringFormat
                        {
                            LineAlignment = StringAlignment.Center,
                            Trimming = StringTrimming.EllipsisCharacter
                        };
                        g.DrawString(_suggestions[i], _owner.Font, textBrush, textRect, sf);
                    }
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                int idx = (e.Y - 2) / ItemHeight;
                if (idx >= 0 && idx < _suggestions.Count)
                {
                    if (_hoveredIndex != idx)
                    {
                        _hoveredIndex = idx;
                        Invalidate();
                    }
                }
                else if (_hoveredIndex != -1)
                {
                    _hoveredIndex = -1;
                    Invalidate();
                }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                if (_hoveredIndex != -1)
                {
                    _hoveredIndex = -1;
                    Invalidate();
                }
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button == MouseButtons.Left)
                {
                    int idx = (e.Y - 2) / ItemHeight;
                    if (idx >= 0 && idx < _suggestions.Count)
                    {
                        _selectedIndex = idx;
                        _owner.SelectSuggestion(_suggestions[idx]);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Legacy alias for TokenEdit.
    /// Preserved for 100% backward compatibility.
    /// </summary>
    [Obsolete("ZeroTokenEdit is deprecated. Please use TokenEdit instead.")]
    [ToolboxItem(false)]
    public class ZeroTokenEdit : TokenEdit
    {
    }
}
