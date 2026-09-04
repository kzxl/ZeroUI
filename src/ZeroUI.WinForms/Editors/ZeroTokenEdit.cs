using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
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
    public class ZeroTokenEdit : Control
    {
        private readonly List<string> _tokens = new List<string>();
        private readonly List<Rectangle> _tokenBounds = new List<Rectangle>();
        private readonly List<Rectangle> _closeBounds = new List<Rectangle>();

        private readonly TextBox _inputBox;
        private string _placeholder = "Type and press Enter...";
        private int _tokenHeight = 24;
        private int _tokenSpacing = 6;
        private int _hoveredCloseIndex = -1;

        public event EventHandler<TokenEventArgs>? TokenAdded;
        public event EventHandler<TokenEventArgs>? TokenRemoved;
        public event EventHandler? TokensChanged;

        [Category("Appearance")]
        [DefaultValue("Type and press Enter...")]
        public string Placeholder
        {
            get => _placeholder;
            set { _placeholder = value; Invalidate(); }
        }

        [Browsable(false)]
        public IReadOnlyList<string> Tokens => _tokens;

        public ZeroTokenEdit()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(300, 40);
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
            _inputBox.TextChanged += (s, e) => Relayout();
            Controls.Add(_inputBox);

            ZeroTheme.ThemeChanged += (s, e) =>
            {
                _inputBox.BackColor = ZeroTheme.Colors.Surface;
                _inputBox.ForeColor = ZeroTheme.Colors.TextPrimary;
                Invalidate();
            };
        }

        public void AddToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return;
            string clean = token.Trim();
            if (!_tokens.Contains(clean))
            {
                _tokens.Add(clean);
                int idx = _tokens.Count - 1;
                TokenAdded?.Invoke(this, new TokenEventArgs(clean, idx));
                TokensChanged?.Invoke(this, EventArgs.Empty);
                Relayout();
            }
        }

        public void RemoveToken(int index)
        {
            if (index < 0 || index >= _tokens.Count) return;
            string token = _tokens[index];
            _tokens.RemoveAt(index);
            TokenRemoved?.Invoke(this, new TokenEventArgs(token, index));
            TokensChanged?.Invoke(this, EventArgs.Empty);
            Relayout();
        }

        public void ClearTokens()
        {
            if (_tokens.Count > 0)
            {
                _tokens.Clear();
                TokensChanged?.Invoke(this, EventArgs.Empty);
                Relayout();
            }
        }

        private void InputBox_KeyDown(object? sender, KeyEventArgs e)
        {
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

            var colors = ZeroTheme.Colors;
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
                    g.DrawString(_placeholder, Font, brush, new Rectangle(12, 0, Width - 24, Height), sf);
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
    }
}
