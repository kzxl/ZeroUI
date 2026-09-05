using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.Editors;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// Modern anti-aliased Color Picker editor for ZeroUI WinForms.
    /// Provides live swatch preview, hex text input, standard enterprise palette swatches,
    /// and RGB adjustment controls.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultEvent("ColorChanged")]
    [Description("Modern color picker editor with swatch preview, palette, and hex input")]
    public class ColorPickEdit : Control, IZeroEditor
    {
        private Color _selectedColor = Color.FromArgb(79, 70, 229); // Default ZeroUI Primary
        private bool _isHovered = false;
        private bool _isFocused = false;
        private bool _isDroppedDown = false;

        private readonly ToolStripDropDown _dropdown;
        private readonly ColorPickerPopupControl _popupControl;

        public event EventHandler? ColorChanged;
        public event EventHandler? EditValueChanged;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object? EditValue
        {
            get => SelectedColor;
            set
            {
                if (value == null || value == DBNull.Value)
                {
                    SelectedColor = Color.Empty;
                }
                else if (value is Color c)
                {
                    SelectedColor = c;
                }
                else if (value is string s)
                {
                    try
                    {
                        SelectedColor = ColorTranslator.FromHtml(s);
                    }
                    catch
                    {
                        SelectedColor = Color.Empty;
                    }
                }
                else if (value is int argb)
                {
                    SelectedColor = Color.FromArgb(argb);
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool IsModified { get; set; } = false;

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool ReadOnly { get; set; } = false;

        public void Reset()
        {
            SelectedColor = Color.FromArgb(79, 70, 229);
            IsModified = false;
        }

        public void Clear() => Reset();

        [Category("Appearance")]
        public Color SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (_selectedColor != value)
                {
                    _selectedColor = value;
                    IsModified = true;
                    ColorChanged?.Invoke(this, EventArgs.Empty);
                    EditValueChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        public ColorPickEdit()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(160, 36);
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            BackColor = Color.Transparent;

            _popupControl = new ColorPickerPopupControl(this);
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var colors = ZeroTheme.Colors;
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

            // Draw Background & Border
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

            // Draw Color Swatch Box
            int swatchSize = Height - 14;
            int swatchX = 7;
            int swatchY = 7;
            var swatchRect = new Rectangle(swatchX, swatchY, swatchSize, swatchSize);

            using (var sPath = CreateRoundedRectanglePath(swatchRect, 3))
            {
                using (var sBrush = new SolidBrush(_selectedColor))
                {
                    g.FillPath(sBrush, sPath);
                }
                using (var sPen = new Pen(colors.Border, 1f))
                {
                    g.DrawPath(sPen, sPath);
                }
            }

            // Draw Hex String
            string hexText = $"#{_selectedColor.R:X2}{_selectedColor.G:X2}{_selectedColor.B:X2}";
            using (var brush = new SolidBrush(colors.TextPrimary))
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center
                };
                var textRect = new Rectangle(swatchX + swatchSize + 10, 0, Width - swatchSize - 36, Height);
                g.DrawString(hexText, Font, brush, textRect, sf);
            }

            // Draw Dropdown Arrow
            int arrowX = Width - 18;
            int arrowY = Height / 2;
            using (var pen = new Pen(colors.TextSecondary, 1.8f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLine(pen, arrowX, arrowY - 2, arrowX + 4, arrowY + 2);
                g.DrawLine(pen, arrowX + 4, arrowY + 2, arrowX + 8, arrowY - 2);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (ReadOnly || !Enabled) return;

            if (_isDroppedDown)
            {
                _dropdown.Close();
            }
            else
            {
                _isDroppedDown = true;
                _popupControl.SyncColor(_selectedColor);
                _popupControl.Size = new Size(220, 240);
                _dropdown.Size = _popupControl.Size;
                _dropdown.Show(this, new Point(0, Height + 2));
                Invalidate();
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

        // Popup with Palette Swatches and RGB Sliders
        private class ColorPickerPopupControl : Control
        {
            private readonly ColorPickEdit _owner;
            private Color _currentColor;
            private readonly TextBox _hexBox;

            private static readonly Color[] Palette = new[]
            {
                Color.FromArgb(79, 70, 229),   // Indigo (Primary)
                Color.FromArgb(37, 99, 235),   // Blue
                Color.FromArgb(14, 165, 233),  // Sky
                Color.FromArgb(20, 184, 166),  // Teal
                Color.FromArgb(34, 197, 94),   // Green (Success)
                Color.FromArgb(234, 179, 8),   // Yellow (Warning)
                Color.FromArgb(249, 115, 22),  // Orange
                Color.FromArgb(239, 68, 68),   // Red (Danger)
                Color.FromArgb(236, 72, 153),  // Pink
                Color.FromArgb(168, 85, 247),  // Purple
                Color.FromArgb(15, 23, 42),    // Slate Dark
                Color.FromArgb(100, 116, 139), // Slate Muted
                Color.FromArgb(148, 163, 184), // Slate Light
                Color.FromArgb(226, 232, 240), // Slate Border
                Color.FromArgb(255, 255, 255)  // Pure White
            };

            public ColorPickerPopupControl(ColorPickEdit owner)
            {
                _owner = owner;
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw, true);
                DoubleBuffered = true;

                _hexBox = new TextBox
                {
                    Location = new Point(12, 195),
                    Width = 90,
                    Font = new Font("Segoe UI", 9f)
                };
                _hexBox.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        TryApplyHex(_hexBox.Text);
                        e.SuppressKeyPress = true;
                    }
                };
                Controls.Add(_hexBox);
            }

            public void SyncColor(Color color)
            {
                _currentColor = color;
                _hexBox.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                Invalidate();
            }

            private void TryApplyHex(string hex)
            {
                try
                {
                    hex = hex.Trim().TrimStart('#');
                    if (hex.Length == 6)
                    {
                        int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                        int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                        int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                        _currentColor = Color.FromArgb(r, g, b);
                        _owner.SelectedColor = _currentColor;
                        _owner._dropdown.Close();
                    }
                }
                catch { }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var colors = ZeroTheme.Colors;
                g.Clear(colors.Surface);

                using (var pen = new Pen(colors.Border))
                {
                    g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }

                // Draw Swatch Matrix (3 rows x 5 columns)
                int swatchSize = 28;
                int spacing = 8;
                int startX = 12;
                int startY = 12;

                for (int i = 0; i < Palette.Length; i++)
                {
                    int col = i % 5;
                    int row = i / 5;
                    int x = startX + col * (swatchSize + spacing);
                    int y = startY + row * (swatchSize + spacing);

                    var rect = new Rectangle(x, y, swatchSize, swatchSize);
                    using (var path = CreateRoundedRectanglePath(rect, 4))
                    {
                        using (var brush = new SolidBrush(Palette[i]))
                        {
                            g.FillPath(brush, path);
                        }
                        using (var pen = new Pen(colors.Border, 1f))
                        {
                            g.DrawPath(pen, path);
                        }
                    }
                }

                // Draw Color Preview
                int previewX = 120;
                int previewY = 190;
                int previewW = Width - previewX - 12;
                int previewH = 32;
                var pRect = new Rectangle(previewX, previewY, previewW, previewH);
                using (var path = CreateRoundedRectanglePath(pRect, 4))
                {
                    using (var brush = new SolidBrush(_currentColor))
                    {
                        g.FillPath(brush, path);
                    }
                    using (var pen = new Pen(colors.Border, 1f))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                int swatchSize = 28;
                int spacing = 8;
                int startX = 12;
                int startY = 12;

                for (int i = 0; i < Palette.Length; i++)
                {
                    int col = i % 5;
                    int row = i / 5;
                    int x = startX + col * (swatchSize + spacing);
                    int y = startY + row * (swatchSize + spacing);

                    if (new Rectangle(x, y, swatchSize, swatchSize).Contains(e.Location))
                    {
                        _currentColor = Palette[i];
                        _owner.SelectedColor = _currentColor;
                        _owner._dropdown.Close();
                        return;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Legacy alias for ColorPickEdit.
    /// Preserved for 100% backward compatibility.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultEvent("ColorChanged")]
    [Description("Legacy alias for ColorPickEdit")]
    public class ZeroColorPicker : ColorPickEdit
    {
    }
}
