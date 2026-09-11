using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
using ZeroUI.Core.Editors;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// Modern color picker editor for WinForms with inline swatch, hex code, and palette popup.
    /// </summary>
    [DefaultEvent(nameof(ColorChanged))]
    [DefaultProperty(nameof(Color))]
    public class ColorPickerEdit : Control, IZeroEditor
    {
        private Color _color = Color.FromArgb(59, 130, 246);
        private string _hexCode = "#3B82F6";
        private bool _readOnly;
        private bool _isModified;
        private bool _isHovered;
        private ToolStripDropDown? _dropDown;

        [Category("Appearance")]
        public Color Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    _hexCode = $"#{value.R:X2}{value.G:X2}{value.B:X2}";
                    _isModified = true;
                    OnColorChanged();
                    Invalidate();
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue("#3B82F6")]
        public string HexCode
        {
            get => _hexCode;
            set
            {
                if (TryParseHex(value, out Color c))
                {
                    Color = c;
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool ReadOnly
        {
            get => _readOnly;
            set => _readOnly = value;
        }

        [Browsable(false)]
        public bool IsModified
        {
            get => _isModified;
            set => _isModified = value;
        }

        [Browsable(false)]
        public object? EditValue
        {
            get => _color;
            set
            {
                if (value is Color c) Color = c;
                else if (value is string s) HexCode = s;
            }
        }

        public event EventHandler<Color>? ColorChanged;
        public event EventHandler? EditValueChanged;

        public ColorPickerEdit()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            Height = 32;
            Width = 140;
            Cursor = Cursors.Hand;
        }

        public void Reset()
        {
            Color = Color.FromArgb(59, 130, 246);
            _isModified = false;
        }

        public void Clear()
        {
            Color = Color.Transparent;
            _isModified = false;
        }

        private void OnColorChanged()
        {
            ColorChanged?.Invoke(this, _color);
            EditValueChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

            // Background
            using (var bgBrush = new SolidBrush(ZeroTheme.Colors.BgInput))
            using (var path = CreateRoundedRectanglePath(bounds, 5))
            {
                g.FillPath(bgBrush, path);
            }

            // Border
            Color borderColor = _isHovered ? ZeroTheme.Colors.PrimaryAccent : ZeroTheme.Colors.BorderDefault;
            using (var borderPen = new Pen(borderColor, 1f))
            using (var path = CreateRoundedRectanglePath(bounds, 5))
            {
                g.DrawPath(borderPen, path);
            }

            // Swatch Box
            var swatchRect = new Rectangle(6, 6, 20, 20);
            using (var swatchBrush = new SolidBrush(_color))
            using (var swatchPath = CreateRoundedRectanglePath(swatchRect, 3))
            {
                g.FillPath(swatchBrush, swatchPath);
                using var swatchPen = new Pen(ZeroTheme.Colors.BorderDefault, 1f);
                g.DrawPath(swatchPen, swatchPath);
            }

            // Hex Text
            using (var font = new Font("Consolas", 9.5f, FontStyle.Regular))
            using (var brush = new SolidBrush(ZeroTheme.Colors.TextPrimary))
            {
                g.DrawString(_hexCode, font, brush, 32, 8);
            }

            // Chevron
            using (var chevronFont = new Font("Segoe UI", 7f, FontStyle.Regular))
            using (var brush = new SolidBrush(ZeroTheme.Colors.TextSecondary))
            {
                g.DrawString("▼", chevronFont, brush, Width - 18, 10);
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

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (!_readOnly)
            {
                ShowPaletteDropdown();
            }
        }

        private void ShowPaletteDropdown()
        {
            var panel = new Panel
            {
                Width = 200,
                Height = 150,
                BackColor = ZeroTheme.Colors.BgCard,
                Padding = new Padding(8)
            };

            int x = 6;
            int y = 6;
            int swatchSize = 20;
            int spacing = 4;

            foreach (var hex in ColorPalette.StandardPalette)
            {
                if (TryParseHex(hex, out Color swatchColor))
                {
                    var btn = new Button
                    {
                        Location = new Point(x, y),
                        Size = new Size(swatchSize, swatchSize),
                        BackColor = swatchColor,
                        FlatStyle = FlatStyle.Flat,
                        Cursor = Cursors.Hand,
                        Tag = hex
                    };
                    btn.FlatAppearance.BorderSize = 1;
                    btn.FlatAppearance.BorderColor = ZeroTheme.Colors.BorderDefault;
                    btn.Click += (s, e) =>
                    {
                        if (s is Button b && b.Tag is string h)
                        {
                            HexCode = h;
                            _dropDown?.Close();
                        }
                    };
                    panel.Controls.Add(btn);

                    x += swatchSize + spacing;
                    if (x + swatchSize > panel.Width - 8)
                    {
                        x = 6;
                        y += swatchSize + spacing;
                    }
                }
            }

            _dropDown = new ToolStripDropDown { AutoClose = true };
            var host = new ToolStripControlHost(panel) { Margin = Padding.Empty, Padding = Padding.Empty };
            _dropDown.Items.Add(host);
            _dropDown.Show(this, 0, Height);
        }

        public static bool TryParseHex(string? hex, out Color color)
        {
            if (ZeroColor.TryParseHex(hex, out var zc))
            {
                color = Color.FromArgb(zc.A, zc.R, zc.G, zc.B);
                return true;
            }
            color = Color.Transparent;
            return false;
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (rect.Width <= 0 || rect.Height <= 0) return path;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
