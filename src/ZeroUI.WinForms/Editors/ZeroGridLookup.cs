using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.Data;
using ZeroUI.Core.Editors;
using ZeroUI.WinForms.DataGrid;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// Enterprise Multi-Column GridLookup dropdown editor for ZeroUI WinForms.
    /// Hosts an embedded virtual DataGrid within a dropdown popup, enabling multi-column search,
    /// pagination, and instant selection for complex enterprise entities (Materials, Customers, Orders).
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultEvent("SelectionChanged")]
    [Description("Enterprise multi-column dropdown lookup with embedded virtual DataGrid and search")]
    public class GridLookupEdit : Control, IZeroEditor
    {
        private readonly ToolStripDropDown _dropdown;
        private readonly Panel _popupContainer;
        private readonly TextBox _searchBox;
        private readonly GridControl _grid;

        private string _placeholder = "Click to search and select...";
        private string _displayMember = "Name";
        private string _valueMember = "Id";
        private string _selectedText = string.Empty;
        private object? _selectedValue = null;
        private object? _selectedItem = null;

        private bool _isHovered = false;
        private bool _isFocused = false;
        private bool _isDroppedDown = false;

        public event EventHandler? SelectionChanged;
        public event EventHandler? DropDownOpened;
        public event EventHandler? DropDownClosed;
        public event EventHandler? EditValueChanged;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object? EditValue
        {
            get => SelectedValue;
            set
            {
                if (!Equals(_selectedValue, value))
                {
                    SelectedValue = value;
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
            _selectedValue = null;
            _selectedItem = null;
            _selectedText = string.Empty;
            IsModified = false;
            Invalidate();
        }

        public void Clear() => Reset();

        [Category("Appearance")]
        [DefaultValue("Click to search and select...")]
        public string Placeholder
        {
            get => _placeholder;
            set { _placeholder = value; Invalidate(); }
        }

        [Category("Data")]
        [DefaultValue("Name")]
        public string DisplayMember
        {
            get => _displayMember;
            set => _displayMember = value;
        }

        [Category("Data")]
        [DefaultValue("Id")]
        public string ValueMember
        {
            get => _valueMember;
            set => _valueMember = value;
        }

        [Browsable(false)]
        public object? SelectedValue
        {
            get => _selectedValue;
            set
            {
                if (!Equals(_selectedValue, value))
                {
                    _selectedValue = value;
                    IsModified = true;
                    Invalidate();
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                    EditValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [Browsable(false)]
        public object? SelectedItem => _selectedItem;

        [Browsable(false)]
        public string SelectedText => _selectedText;

        [Browsable(false)]
        public GridControl Grid => _grid;

        public GridLookupEdit()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(260, 36);
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            BackColor = Color.Transparent;

            // 1. Construct Embedded GridControl first
            _grid = new GridControl
            {
                Dock = DockStyle.Fill
            };

            _popupContainer = new Panel
            {
                Size = new Size(520, 320),
                BackColor = ZeroTheme.Colors.Surface,
                Padding = new Padding(6)
            };

            // Wrap in ToolStripDropDown
            var host = new ToolStripControlHost(_popupContainer)
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
                DropDownClosed?.Invoke(this, EventArgs.Empty);
                Invalidate();
            };

            // Search Header Panel
            var searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(4)
            };

            _searchBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f),
                BackColor = ZeroTheme.Colors.Surface,
                ForeColor = ZeroTheme.Colors.TextPrimary
            };
            _searchBox.TextChanged += (s, e) =>
            {
                string query = _searchBox.Text.Trim();
                if (string.IsNullOrEmpty(query))
                {
                    _grid.ApplyFilter(null);
                    return;
                }

                var src = _grid.DataSource;
                if (src == null) return;

                int colCount = _grid.Columns.Count;
                _grid.ApplyFilter(modelRow =>
                {
                    CellValueBuffer buf = new CellValueBuffer();
                    for (int c = 0; c < colCount; c++)
                    {
                        if (!_grid.Columns[c].IsVisible) continue;
                        buf.Reset();
                        src.GetCellValue(modelRow, c, ref buf);
                        if (buf.Text.IndexOf(query.AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return true;
                        }
                    }
                    return false;
                });
            };
            searchPanel.Controls.Add(_searchBox);
            _popupContainer.Controls.Add(searchPanel);

            _grid.DoubleClick += (s, e) => CommitSelection();
            _grid.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    CommitSelection();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    _dropdown.Close();
                    e.Handled = true;
                }
            };

            _popupContainer.Controls.Add(_grid);
            _grid.BringToFront();

            _dropdown.Opened += (s, e) =>
            {
                _isDroppedDown = true;
                DropDownOpened?.Invoke(this, EventArgs.Empty);
                _searchBox.Focus();
                _searchBox.SelectAll();
                Invalidate();
            };

            ZeroTheme.ThemeChanged += (s, e) =>
            {
                _popupContainer.BackColor = ZeroTheme.Colors.Surface;
                _searchBox.BackColor = ZeroTheme.Colors.Surface;
                _searchBox.ForeColor = ZeroTheme.Colors.TextPrimary;
                Invalidate();
            };
        }

        public void SetDataSource<T>(IList<T> items)
        {
            _grid.SetDataSource(items);
        }

        private void CommitSelection()
        {
            int visualRow = _grid.SelectedVisualRow;
            if (visualRow < 0) return;

            int modelRow = _grid.GetModelRowIndex(visualRow);
            var src = _grid.DataSource;
            if (modelRow < 0 || src == null) return;

            // Extract display value from first visible column or matching DisplayMember
            CellValueBuffer buf = new CellValueBuffer();
            int displayCol = 0;
            for (int i = 0; i < _grid.Columns.Count; i++)
            {
                if (string.Equals(_grid.Columns[i].FieldName, _displayMember, StringComparison.OrdinalIgnoreCase))
                {
                    displayCol = i;
                    break;
                }
            }

            src.GetCellValue(modelRow, displayCol, ref buf);
            _selectedText = buf.Text.ToString();
            _selectedValue = modelRow;
            IsModified = true;

            _dropdown.Close();
            Invalidate();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            EditValueChanged?.Invoke(this, EventArgs.Empty);
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

            // Draw Table/Grid Icon
            int iconX = 10;
            int iconY = (Height - 14) / 2;
            using (var iconPen = new Pen(_isHovered || _isDroppedDown ? colors.Primary : colors.TextSecondary, 1.2f))
            {
                g.DrawRectangle(iconPen, iconX, iconY, 14, 14);
                g.DrawLine(iconPen, iconX, iconY + 5, iconX + 14, iconY + 5);
                g.DrawLine(iconPen, iconX + 7, iconY + 5, iconX + 7, iconY + 14);
            }

            // Draw Dropdown Chevron Arrow
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

            // Draw Selected Text / Placeholder
            string text = !string.IsNullOrEmpty(_selectedText) ? _selectedText : _placeholder;
            bool isPlaceholder = string.IsNullOrEmpty(_selectedText);
            using (var brush = new SolidBrush(isPlaceholder ? colors.TextSecondary : colors.TextPrimary))
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter
                };
                var textRect = new Rectangle(32, 0, Width - 60, Height);
                g.DrawString(text, Font, brush, textRect, sf);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
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
            if (ReadOnly || !Enabled) return;
            if (_dropdown.Visible) return;
            _popupContainer.Size = new Size(Math.Max(Width, 520), 320);
            _dropdown.Size = _popupContainer.Size;
            _dropdown.Show(this, new Point(0, Height + 2));
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

    /// <summary>
    /// Legacy alias for GridLookupEdit.
    /// Preserved for 100% backward compatibility.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultEvent("SelectionChanged")]
    [Description("Legacy alias for GridLookupEdit")]
    public class ZeroGridLookup : GridLookupEdit
    {
    }
}
