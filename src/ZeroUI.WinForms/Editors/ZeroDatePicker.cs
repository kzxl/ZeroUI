using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{
    /// <summary>
    /// Modern, anti-aliased single date picker control with 100% custom-drawn calendar popup,
    /// quick preset pills, year/month navigation, and native Obsidian Dark / Clean Light theming.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultProperty("Value")]
    [DefaultEvent("ValueChanged")]
    [Description("Modern date picker with custom-drawn popup calendar and quick-select presets")]
    public class ZeroDatePicker : Control
    {
        private DateTime _selectedDate = DateTime.Today;
        private string _dateFormat = "yyyy-MM-dd";
        private bool _showPresets = true;
        private bool _isHovered = false;
        private bool _isFocused = false;

        private ToolStripDropDown? _popup;
        private ZeroCalendarPopupControl? _calendarControl;
        private Rectangle _chevronRect;

        public event EventHandler? ValueChanged;

        public ZeroDatePicker()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);

            Size = new Size(160, 36);
            BackColor = Color.FromArgb(15, 23, 42); // Obsidian Dark default
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            Cursor = Cursors.Hand;

            ZeroTheme.ThemeChanged += (s, e) => Invalidate();
            ZeroUIConfig.ConfigChanged += (s, e) =>
            {
                Font = ZeroUIConfig.DefaultFont;
                Invalidate();
            };
        }

        [Category("Data")]
        public DateTime Value
        {
            get => _selectedDate;
            set
            {
                var val = value.Date;
                if (_selectedDate != val)
                {
                    _selectedDate = val;
                    Invalidate();
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue("yyyy-MM-dd")]
        public string DateFormat
        {
            get => _dateFormat;
            set
            {
                _dateFormat = value ?? "yyyy-MM-dd";
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool ShowPresets
        {
            get => _showPresets;
            set
            {
                _showPresets = value;
                Invalidate();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _chevronRect = new Rectangle(Width - 24, (Height - 14) / 2, 14, 14);
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
            ShowCalendarPopup();
        }

        private void ShowCalendarPopup()
        {
            if (_popup != null && _popup.Visible)
            {
                _popup.Close();
                return;
            }

            _calendarControl = new ZeroCalendarPopupControl(this, _selectedDate, _showPresets);
            var host = new ToolStripControlHost(_calendarControl)
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AutoSize = false,
                Size = _calendarControl.Size
            };

            _popup = new ToolStripDropDown
            {
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                DropShadowEnabled = true,
                AutoClose = true
            };
            _popup.Items.Add(host);

            _popup.Closed += (s, e) =>
            {
                _isFocused = false;
                Invalidate();
            };

            _isFocused = true;
            Invalidate();
            _popup.Show(this, new Point(0, Height + 2), ToolStripDropDownDirection.BelowRight);
        }

        internal void OnDateSelectedFromPopup(DateTime date)
        {
            Value = date;
            _popup?.Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var palette = ZeroTheme.Colors;

            // 1. Fill parent background to eliminate black corner clipping artifacts
            Color parentBg = ZeroUIConfig.GetParentBackground(this, palette.Background);
            using (var brushParent = new SolidBrush(parentBg))
            {
                g.FillRectangle(brushParent, ClientRectangle);
            }

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int effRadius = ZeroUIConfig.GetEffectiveRadius(6);

            // 2. Box Background & Border
            using (var path = CreateRoundedRectangle(rect, effRadius))
            {
                using var bgBrush = new SolidBrush(palette.Surface);
                g.FillPath(bgBrush, path);

                Color borderColor = _isFocused ? palette.Primary : (_isHovered ? palette.PrimaryHover : palette.Border);
                using var pen = new Pen(borderColor, _isFocused ? 1.5f : 1f);
                g.DrawPath(pen, path);
            }

            // 2. Calendar Glyph Icon (📅)
            using (var iconFont = new Font("Segoe UI Emoji", 9.5f))
            using (var brushIcon = new SolidBrush(palette.Primary))
            {
                g.DrawString("📅", iconFont, brushIcon, 8, (Height - 18) / 2);
            }

            // 3. Formatted Date Text
            string dateStr = _selectedDate.ToString(_dateFormat);
            using (var fontText = new Font(Font.FontFamily, 9.25f, FontStyle.Bold))
            using (var brushText = new SolidBrush(palette.TextPrimary))
            {
                g.DrawString(dateStr, fontText, brushText, 32, (Height - 16) / 2);
            }

            // 4. Dropdown Chevron (▼)
            using (var chevBrush = new SolidBrush(_isFocused ? palette.Primary : palette.TextSecondary))
            {
                PointF center = new PointF(_chevronRect.X + (_chevronRect.Width / 2f), _chevronRect.Y + (_chevronRect.Height / 2f));
                PointF[] pts;
                if (_isFocused)
                {
                    // Up arrow (▲) when popup is open
                    pts = new[]
                    {
                        new PointF(center.X - 3.5f, center.Y + 2f),
                        new PointF(center.X + 3.5f, center.Y + 2f),
                        new PointF(center.X, center.Y - 2.5f)
                    };
                }
                else
                {
                    // Down arrow (▼)
                    pts = new[]
                    {
                        new PointF(center.X - 3.5f, center.Y - 2f),
                        new PointF(center.X + 3.5f, center.Y - 2f),
                        new PointF(center.X, center.Y + 2.5f)
                    };
                }
                g.FillPolygon(chevBrush, pts);
            }
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0 || rect.Width <= 0 || rect.Height <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int diameter = radius * 2;
            Rectangle arc = new Rectangle(rect.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// 100% custom-drawn calendar popup control for ZeroDatePicker.
        /// Features year/month steppers, interactive day grid, quick presets, today highlight, and dark/light theming.
        /// </summary>
        private sealed class ZeroCalendarPopupControl : Control
        {
            private readonly ZeroDatePicker _owner;
            private DateTime _viewMonth;
            private DateTime _selectedDate;
            private readonly bool _showPresets;

            private Rectangle _prevYearRect;
            private Rectangle _prevMonthRect;
            private Rectangle _nextMonthRect;
            private Rectangle _nextYearRect;
            private Rectangle _monthTitleRect;
            private Rectangle _todayLinkRect;

            private Rectangle[] _presetRects = new Rectangle[4];
            private readonly string[] _presetNames = new[] { "Hôm nay", "Hôm qua", "Ngày mai", "+7 Ngày" };
            private int _hoveredPreset = -1;

            private int _hoveredDayIndex = -1; // 0..41
            private DateTime[] _gridDates = new DateTime[42];

            public ZeroCalendarPopupControl(ZeroDatePicker owner, DateTime initialDate, bool showPresets)
            {
                _owner = owner;
                _selectedDate = initialDate.Date;
                _viewMonth = new DateTime(initialDate.Year, initialDate.Month, 1);
                _showPresets = showPresets;

                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.Selectable, true);

                int w = 270;
                int h = _showPresets ? 316 : 280;
                Size = new Size(w, h);
                Font = new Font("Segoe UI", 9f);
                BackColor = ZeroTheme.Colors.CardBackground;

                BuildGridDates();
            }

            private void BuildGridDates()
            {
                int firstDayOfWeek = (int)_viewMonth.DayOfWeek; // 0 = Sunday
                DateTime startDate = _viewMonth.AddDays(-firstDayOfWeek);

                for (int i = 0; i < 42; i++)
                {
                    _gridDates[i] = startDate.AddDays(i);
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                int hovPreset = -1;
                if (_showPresets)
                {
                    for (int i = 0; i < _presetRects.Length; i++)
                    {
                        if (_presetRects[i].Contains(e.Location))
                        {
                            hovPreset = i;
                            break;
                        }
                    }
                }

                int hovDay = -1;
                int gridTop = _showPresets ? 74 : 38;
                int cellW = (Width - 16) / 7;
                int cellH = 28;

                if (e.X >= 8 && e.X < Width - 8 && e.Y >= gridTop && e.Y < gridTop + (6 * cellH))
                {
                    int col = (e.X - 8) / cellW;
                    int row = (e.Y - gridTop) / cellH;
                    if (col >= 0 && col < 7 && row >= 0 && row < 6)
                    {
                        hovDay = (row * 7) + col;
                    }
                }

                bool onNav = _prevYearRect.Contains(e.Location) || _prevMonthRect.Contains(e.Location) ||
                             _nextMonthRect.Contains(e.Location) || _nextYearRect.Contains(e.Location) ||
                             _todayLinkRect.Contains(e.Location);

                Cursor = (hovPreset >= 0 || hovDay >= 0 || onNav) ? Cursors.Hand : Cursors.Default;

                if (_hoveredPreset != hovPreset || _hoveredDayIndex != hovDay)
                {
                    _hoveredPreset = hovPreset;
                    _hoveredDayIndex = hovDay;
                    Invalidate();
                }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                _hoveredPreset = -1;
                _hoveredDayIndex = -1;
                Cursor = Cursors.Default;
                Invalidate();
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);

                // Check Presets
                if (_showPresets)
                {
                    for (int i = 0; i < _presetRects.Length; i++)
                    {
                        if (_presetRects[i].Contains(e.Location))
                        {
                            DateTime target = i switch
                            {
                                0 => DateTime.Today,
                                1 => DateTime.Today.AddDays(-1),
                                2 => DateTime.Today.AddDays(1),
                                _ => DateTime.Today.AddDays(7)
                            };
                            _owner.OnDateSelectedFromPopup(target);
                            return;
                        }
                    }
                }

                // Month / Year Navigation
                if (_prevYearRect.Contains(e.Location))
                {
                    _viewMonth = _viewMonth.AddYears(-1);
                    BuildGridDates();
                    Invalidate();
                    return;
                }
                if (_prevMonthRect.Contains(e.Location))
                {
                    _viewMonth = _viewMonth.AddMonths(-1);
                    BuildGridDates();
                    Invalidate();
                    return;
                }
                if (_nextMonthRect.Contains(e.Location))
                {
                    _viewMonth = _viewMonth.AddMonths(1);
                    BuildGridDates();
                    Invalidate();
                    return;
                }
                if (_nextYearRect.Contains(e.Location))
                {
                    _viewMonth = _viewMonth.AddYears(1);
                    BuildGridDates();
                    Invalidate();
                    return;
                }

                // Today Link Click
                if (_todayLinkRect.Contains(e.Location))
                {
                    _owner.OnDateSelectedFromPopup(DateTime.Today);
                    return;
                }

                // Grid Days Click
                if (_hoveredDayIndex >= 0 && _hoveredDayIndex < 42)
                {
                    _owner.OnDateSelectedFromPopup(_gridDates[_hoveredDayIndex]);
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var palette = ZeroTheme.Colors;
                g.Clear(palette.CardBackground);

                // Border around popup
                using (var penBorder = new Pen(palette.Border, 1f))
                {
                    g.DrawRectangle(penBorder, 0, 0, Width - 1, Height - 1);
                }

                int curY = 6;

                // 1. Quick Presets Bar (Pills)
                if (_showPresets)
                {
                    int pW = (Width - 20) / 4;
                    using var fontPreset = new Font(Font.FontFamily, 8f, FontStyle.Bold);

                    for (int i = 0; i < 4; i++)
                    {
                        _presetRects[i] = new Rectangle(8 + (i * pW) + (i * 2), curY, pW - 2, 24);

                        bool isHov = i == _hoveredPreset;
                        Color pillBg = isHov ? Color.FromArgb(35, palette.Primary) : Color.FromArgb(20, palette.Border);

                        using var brushPill = new SolidBrush(pillBg);
                        using var pathPill = CreateRoundedRectangle(_presetRects[i], 4);
                        g.FillPath(brushPill, pathPill);

                        Color textColor = isHov ? palette.Primary : palette.TextSecondary;
                        using var brushText = new SolidBrush(textColor);
                        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(_presetNames[i], fontPreset, brushText, _presetRects[i], sf);
                    }
                    curY += 28;
                }

                // 2. Month & Year Navigation Header
                int navBtnSz = 22;
                _prevYearRect = new Rectangle(8, curY + 2, navBtnSz, navBtnSz);
                _prevMonthRect = new Rectangle(32, curY + 2, navBtnSz, navBtnSz);
                _nextMonthRect = new Rectangle(Width - 54, curY + 2, navBtnSz, navBtnSz);
                _nextYearRect = new Rectangle(Width - 30, curY + 2, navBtnSz, navBtnSz);
                _monthTitleRect = new Rectangle(56, curY, Width - 112, 26);

                // Draw Header Title: "Tháng MM / yyyy"
                string titleText = _viewMonth.ToString("MMMM yyyy");
                using (var fontTitle = new Font(Font.FontFamily, 9.5f, FontStyle.Bold))
                using (var brushTitle = new SolidBrush(palette.TextPrimary))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(titleText, fontTitle, brushTitle, _monthTitleRect, sf);
                }

                // Draw Arrow Nav Buttons («, ‹, ›, »)
                using (var fontNav = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (var brushNav = new SolidBrush(palette.TextSecondary))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("«", fontNav, brushNav, _prevYearRect, sf);
                    g.DrawString("‹", fontNav, brushNav, _prevMonthRect, sf);
                    g.DrawString("›", fontNav, brushNav, _nextMonthRect, sf);
                    g.DrawString("»", fontNav, brushNav, _nextYearRect, sf);
                }

                curY += 28;

                // 3. Day of Week Column Headers
                string[] dayHeaders = new[] { "CN", "T2", "T3", "T4", "T5", "T6", "T7" };
                int cellW = (Width - 16) / 7;
                using (var fontDOW = new Font(Font.FontFamily, 7.75f, FontStyle.Bold))
                {
                    for (int c = 0; c < 7; c++)
                    {
                        var cellRect = new Rectangle(8 + (c * cellW), curY, cellW, 18);
                        Color cColor = (c == 0 || c == 6) ? palette.Warning : palette.TextSecondary;
                        using var brushDOW = new SolidBrush(cColor);
                        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(dayHeaders[c], fontDOW, brushDOW, cellRect, sf);
                    }
                }

                curY += 20;

                // 4. Days Grid (42 cells: 6 rows x 7 cols)
                int cellH = 26;
                DateTime today = DateTime.Today;

                using var fontDay = new Font(Font.FontFamily, 8.5f, FontStyle.Regular);
                using var fontDayBold = new Font(Font.FontFamily, 8.5f, FontStyle.Bold);

                for (int i = 0; i < 42; i++)
                {
                    int r = i / 7;
                    int c = i % 7;
                    DateTime cellDate = _gridDates[i];
                    var cellRect = new Rectangle(8 + (c * cellW), curY + (r * cellH), cellW, cellH);

                    bool isCurrentMonth = cellDate.Month == _viewMonth.Month;
                    bool isSelected = cellDate == _selectedDate;
                    bool isToday = cellDate == today;
                    bool isHovered = i == _hoveredDayIndex;

                    // Background highlight
                    if (isSelected)
                    {
                        using var brushSel = new SolidBrush(palette.Primary);
                        using var pathSel = CreateRoundedRectangle(new Rectangle(cellRect.X + 2, cellRect.Y + 1, cellW - 4, cellH - 2), 5);
                        g.FillPath(brushSel, pathSel);
                    }
                    else if (isHovered)
                    {
                        using var brushHov = new SolidBrush(Color.FromArgb(25, palette.Primary));
                        using var pathHov = CreateRoundedRectangle(new Rectangle(cellRect.X + 2, cellRect.Y + 1, cellW - 4, cellH - 2), 5);
                        g.FillPath(brushHov, pathHov);
                    }

                    // Today's subtle indicator (underline bar or border)
                    if (isToday && !isSelected)
                    {
                        using var penToday = new Pen(palette.Primary, 1.2f);
                        using var pathToday = CreateRoundedRectangle(new Rectangle(cellRect.X + 2, cellRect.Y + 1, cellW - 4, cellH - 2), 5);
                        g.DrawPath(penToday, pathToday);
                    }

                    // Text color
                    Color textColor;
                    if (isSelected) textColor = Color.White;
                    else if (!isCurrentMonth) textColor = Color.FromArgb(90, palette.TextSecondary);
                    else if (isToday) textColor = palette.Primary;
                    else textColor = palette.TextPrimary;

                    using (var brushDayText = new SolidBrush(textColor))
                    {
                        var activeFont = (isSelected || isToday) ? fontDayBold : fontDay;
                        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(cellDate.Day.ToString(), activeFont, brushDayText, cellRect, sf);
                    }
                }

                curY += (6 * cellH) + 2;

                // 5. Footer Bar (Divider + Today link)
                using (var penDiv = new Pen(Color.FromArgb(15, palette.Border), 1f))
                {
                    g.DrawLine(penDiv, 8, curY, Width - 8, curY);
                }

                _todayLinkRect = new Rectangle(8, curY + 2, Width - 16, 20);
                using (var fontFoot = new Font(Font.FontFamily, 8f, FontStyle.Regular))
                using (var brushFoot = new SolidBrush(palette.Primary))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString($"Hôm nay: {today:yyyy-MM-dd}", fontFoot, brushFoot, _todayLinkRect, sf);
                }
            }
        }
    }
}
