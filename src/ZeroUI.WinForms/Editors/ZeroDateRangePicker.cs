using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{
    public enum DateRangePreset
    {
        Custom,
        Today,
        Yesterday,
        Last7Days,
        Last30Days,
        ThisMonth,
        LastMonth,
        YearToDate
    }

    /// <summary>
    /// Enterprise Dual-Date Range Selector (From Date -> To Date) with connected range ribbon,
    /// 1-click quick preset filters, interactive hover range preview, and calendar popup.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultEvent("DateRangeChanged")]
    [DefaultProperty("StartDate")]
    [Description("Enterprise dual-date range selector with 1-click presets and calendar popup")]
    public class ZeroDateRangePicker : Control
    {
        private DateTime _startDate = DateTime.Today.AddDays(-6);
        private DateTime _endDate = DateTime.Today;
        private string _dateFormat = "yyyy-MM-dd";
        private DateRangePreset _preset = DateRangePreset.Last7Days;

        private bool _isHovered = false;
        private bool _isFocused = false;
        private readonly ToolStripDropDown _dropdown;
        private readonly DateRangePopupControl _popupControl;
        private Rectangle _chevronRect;

        public event EventHandler? DateRangeChanged;

        public ZeroDateRangePicker()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);

            Size = new Size(260, 36);
            Font = new Font("Segoe UI", 9.25f);
            BackColor = Color.FromArgb(15, 23, 42); // Obsidian Dark
            Cursor = Cursors.Hand;

            _popupControl = new DateRangePopupControl(this);
            var host = new ToolStripControlHost(_popupControl)
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
            _dropdown.Closed += (s, e) =>
            {
                _isFocused = false;
                Invalidate();
            };

            ZeroTheme.ThemeChanged += (s, e) => Invalidate();
        }

        [Category("Data")]
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                var val = value.Date;
                if (_startDate != val)
                {
                    _startDate = val;
                    if (_endDate < _startDate) _endDate = _startDate;
                    _preset = DateRangePreset.Custom;
                    Invalidate();
                    DateRangeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [Category("Data")]
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                var val = value.Date;
                if (_endDate != val)
                {
                    _endDate = val;
                    if (_startDate > _endDate) _startDate = _endDate;
                    _preset = DateRangePreset.Custom;
                    Invalidate();
                    DateRangeChanged?.Invoke(this, EventArgs.Empty);
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
        [DefaultValue(DateRangePreset.Last7Days)]
        public DateRangePreset Preset
        {
            get => _preset;
            set
            {
                _preset = value;
                ApplyPreset(value);
            }
        }

        public void SetRange(DateTime start, DateTime end)
        {
            _startDate = start < end ? start.Date : end.Date;
            _endDate = end >= start ? end.Date : start.Date;
            _preset = DateRangePreset.Custom;
            Invalidate();
            DateRangeChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ApplyPreset(DateRangePreset preset)
        {
            _preset = preset;
            DateTime today = DateTime.Today;

            switch (preset)
            {
                case DateRangePreset.Today:
                    _startDate = today;
                    _endDate = today;
                    break;
                case DateRangePreset.Yesterday:
                    _startDate = today.AddDays(-1);
                    _endDate = today.AddDays(-1);
                    break;
                case DateRangePreset.Last7Days:
                    _startDate = today.AddDays(-6);
                    _endDate = today;
                    break;
                case DateRangePreset.Last30Days:
                    _startDate = today.AddDays(-29);
                    _endDate = today;
                    break;
                case DateRangePreset.ThisMonth:
                    _startDate = new DateTime(today.Year, today.Month, 1);
                    _endDate = _startDate.AddMonths(1).AddDays(-1);
                    break;
                case DateRangePreset.LastMonth:
                    var firstLastMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                    _startDate = firstLastMonth;
                    _endDate = firstLastMonth.AddMonths(1).AddDays(-1);
                    break;
                case DateRangePreset.YearToDate:
                    _startDate = new DateTime(today.Year, 1, 1);
                    _endDate = today;
                    break;
                case DateRangePreset.Custom:
                default:
                    return;
            }

            Invalidate();
            DateRangeChanged?.Invoke(this, EventArgs.Empty);
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

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!_dropdown.Visible)
            {
                _popupControl.SyncFromPicker(_startDate, _endDate);
                _popupControl.Size = new Size(500, 280);
                _dropdown.Size = new Size(500, 280);
                _isFocused = true;
                Invalidate();
                _dropdown.Show(this, new Point(0, Height + 2), ToolStripDropDownDirection.BelowRight);
            }
            else
            {
                _dropdown.Close();
            }
        }

        internal void ClosePopup()
        {
            _dropdown.Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var palette = ZeroTheme.Colors;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // 1. Box Background & Rounded Border
            using (var path = CreateRoundedRect(rect, 6))
            {
                using var brushBg = new SolidBrush(palette.Surface);
                g.FillPath(brushBg, path);

                Color borderCol = _isFocused ? palette.Primary : (_isHovered ? palette.PrimaryHover : palette.Border);
                using var penBorder = new Pen(borderCol, _isFocused ? 1.5f : 1f);
                g.DrawPath(penBorder, path);
            }

            // 2. Calendar Glyph (📅)
            using (var iconFont = new Font("Segoe UI Emoji", 9.5f))
            using (var brushIcon = new SolidBrush(palette.Primary))
            {
                g.DrawString("📅", iconFont, brushIcon, 8, (Height - 18) / 2);
            }

            // 3. Date Range Text with pill accent: "2026-09-01  →  2026-09-03"
            string sText = _startDate.ToString(_dateFormat);
            string eText = _endDate.ToString(_dateFormat);

            using (var fontText = new Font(Font.FontFamily, 9f, FontStyle.Bold))
            using (var brushText = new SolidBrush(palette.TextPrimary))
            using (var brushArrow = new SolidBrush(palette.Primary))
            {
                g.DrawString(sText, fontText, brushText, 32, (Height - 16) / 2);

                int arrowX = 32 + (int)g.MeasureString(sText, fontText).Width + 4;
                g.DrawString("→", fontText, brushArrow, arrowX, (Height - 16) / 2);

                int endX = arrowX + 16;
                g.DrawString(eText, fontText, brushText, endX, (Height - 16) / 2);
            }

            // 4. Dropdown Chevron (▼ / ▲)
            using (var chevBrush = new SolidBrush(_isFocused ? palette.Primary : palette.TextSecondary))
            {
                PointF center = new PointF(_chevronRect.X + (_chevronRect.Width / 2f), _chevronRect.Y + (_chevronRect.Height / 2f));
                PointF[] pts;
                if (_isFocused)
                {
                    pts = new[]
                    {
                        new PointF(center.X - 3.5f, center.Y + 2f),
                        new PointF(center.X + 3.5f, center.Y + 2f),
                        new PointF(center.X, center.Y - 2.5f)
                    };
                }
                else
                {
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

        private static GraphicsPath CreateRoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Inner calendar & preset popup container with connected range ribbon and hover preview.
        /// </summary>
        private class DateRangePopupControl : Control
        {
            private readonly ZeroDateRangePicker _picker;
            private DateTime _tempStart;
            private DateTime _tempEnd;
            private DateTime _hoverDate;
            private DateTime _viewMonth;
            private int _clickStep = 0; // 0: picking start, 1: picking end

            private Rectangle _prevYearRect;
            private Rectangle _prevMonthRect;
            private Rectangle _nextMonthRect;
            private Rectangle _nextYearRect;

            private Rectangle[] _presetRects = new Rectangle[7];
            private readonly string[] _presetNames = new[]
            {
                "Hôm nay", "Hôm qua", "7 Ngày qua", "30 Ngày qua", "Tháng này", "Tháng trước", "Từ đầu năm"
            };
            private readonly DateRangePreset[] _presetValues = new[]
            {
                DateRangePreset.Today, DateRangePreset.Yesterday, DateRangePreset.Last7Days,
                DateRangePreset.Last30Days, DateRangePreset.ThisMonth, DateRangePreset.LastMonth, DateRangePreset.YearToDate
            };
            private int _hoveredPreset = -1;

            public DateRangePopupControl(ZeroDateRangePicker picker)
            {
                _picker = picker;
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw, true);

                Font = new Font("Segoe UI", 9f);
                BackColor = ZeroTheme.Colors.CardBackground;
            }

            public void SyncFromPicker(DateTime start, DateTime end)
            {
                _tempStart = start;
                _tempEnd = end;
                _hoverDate = end;
                _viewMonth = new DateTime(start.Year, start.Month, 1);
                _clickStep = 0;
                Invalidate();
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);

                int hov = -1;
                for (int i = 0; i < _presetRects.Length; i++)
                {
                    if (_presetRects[i].Contains(e.Location))
                    {
                        hov = i;
                        break;
                    }
                }

                int calLeft = 140;
                int startY = 60;
                int dayW = (Width - calLeft - 20) / 7;
                int dayH = 26;

                if (e.X >= calLeft && e.X < Width - 20 && e.Y >= startY && e.Y < startY + (6 * dayH))
                {
                    int col = (e.X - calLeft) / dayW;
                    int row = (e.Y - startY) / dayH;

                    int firstDayOfWeek = (int)_viewMonth.DayOfWeek;
                    int dayIndex = (row * 7) + col - firstDayOfWeek + 1;
                    int daysInMonth = DateTime.DaysInMonth(_viewMonth.Year, _viewMonth.Month);

                    if (dayIndex >= 1 && dayIndex <= daysInMonth)
                    {
                        var d = new DateTime(_viewMonth.Year, _viewMonth.Month, dayIndex);
                        if (_hoverDate != d)
                        {
                            _hoverDate = d;
                            Invalidate();
                        }
                    }
                }

                if (_hoveredPreset != hov)
                {
                    _hoveredPreset = hov;
                    Invalidate();
                }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                _hoveredPreset = -1;
                Invalidate();
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);

                // Check Presets
                for (int i = 0; i < _presetRects.Length; i++)
                {
                    if (_presetRects[i].Contains(e.Location))
                    {
                        _picker.ApplyPreset(_presetValues[i]);
                        _picker.ClosePopup();
                        return;
                    }
                }

                // Month / Year Navigation
                if (_prevYearRect.Contains(e.Location))
                {
                    _viewMonth = _viewMonth.AddYears(-1);
                    Invalidate();
                    return;
                }
                if (_prevMonthRect.Contains(e.Location))
                {
                    _viewMonth = _viewMonth.AddMonths(-1);
                    Invalidate();
                    return;
                }
                if (_nextMonthRect.Contains(e.Location))
                {
                    _viewMonth = _viewMonth.AddMonths(1);
                    Invalidate();
                    return;
                }
                if (_nextYearRect.Contains(e.Location))
                {
                    _viewMonth = _viewMonth.AddYears(1);
                    Invalidate();
                    return;
                }

                // Apply Button
                var applyRect = new Rectangle(Width - 85, Height - 34, 75, 26);
                if (applyRect.Contains(e.Location))
                {
                    _picker.SetRange(_tempStart, _tempEnd);
                    _picker.ClosePopup();
                    return;
                }

                // Day Grid Click
                int calLeft = 140;
                int startY = 60;
                int dayW = (Width - calLeft - 20) / 7;
                int dayH = 26;

                if (e.X >= calLeft && e.X < Width - 20 && e.Y >= startY && e.Y < startY + (6 * dayH))
                {
                    int col = (e.X - calLeft) / dayW;
                    int row = (e.Y - startY) / dayH;

                    int firstDayOfWeek = (int)_viewMonth.DayOfWeek;
                    int dayIndex = (row * 7) + col - firstDayOfWeek + 1;
                    int daysInMonth = DateTime.DaysInMonth(_viewMonth.Year, _viewMonth.Month);

                    if (dayIndex >= 1 && dayIndex <= daysInMonth)
                    {
                        DateTime clickedDate = new DateTime(_viewMonth.Year, _viewMonth.Month, dayIndex);
                        if (_clickStep == 0)
                        {
                            _tempStart = clickedDate;
                            _tempEnd = clickedDate;
                            _clickStep = 1;
                        }
                        else
                        {
                            if (clickedDate < _tempStart)
                            {
                                _tempEnd = _tempStart;
                                _tempStart = clickedDate;
                            }
                            else
                            {
                                _tempEnd = clickedDate;
                            }
                            _clickStep = 0;
                        }
                        Invalidate();
                    }
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var palette = ZeroTheme.Colors;
                g.Clear(palette.CardBackground);

                using (var penBorder = new Pen(palette.Border, 1f))
                {
                    g.DrawRectangle(penBorder, 0, 0, Width - 1, Height - 1);
                }

                // 1. Left Preset Sidebar (Width = 135)
                int sideW = 135;
                using (var brushSide = new SolidBrush(palette.Surface))
                {
                    g.FillRectangle(brushSide, new Rectangle(0, 0, sideW, Height));
                }
                using (var penDiv = new Pen(palette.Border, 1f))
                {
                    g.DrawLine(penDiv, sideW, 0, sideW, Height);
                }

                using var fontPreset = new Font(Font.FontFamily, 8.5f, FontStyle.Regular);
                for (int i = 0; i < _presetNames.Length; i++)
                {
                    int py = 10 + (i * 34);
                    _presetRects[i] = new Rectangle(8, py, sideW - 16, 28);

                    bool isHov = i == _hoveredPreset;
                    bool isCur = _picker.Preset == _presetValues[i];

                    if (isCur)
                    {
                        using var brushCur = new SolidBrush(Color.FromArgb(40, palette.Primary));
                        using var pathCur = CreateRoundedRect(_presetRects[i], 5);
                        g.FillPath(brushCur, pathCur);

                        // Active left pill accent
                        using var penLeft = new SolidBrush(palette.Primary);
                        g.FillRectangle(penLeft, new Rectangle(_presetRects[i].X, _presetRects[i].Y + 4, 3, _presetRects[i].Height - 8));
                    }
                    else if (isHov)
                    {
                        using var brushHov = new SolidBrush(Color.FromArgb(20, palette.Primary));
                        using var pathHov = CreateRoundedRect(_presetRects[i], 5);
                        g.FillPath(brushHov, pathHov);
                    }

                    Color textColor = isCur ? palette.Primary : (isHov ? palette.TextPrimary : palette.TextSecondary);
                    using var brushText = new SolidBrush(textColor);
                    var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                    g.DrawString(_presetNames[i], fontPreset, brushText, new Rectangle(_presetRects[i].X + 10, _presetRects[i].Y, _presetRects[i].Width - 10, _presetRects[i].Height), sf);
                }

                // 2. Right Calendar Area
                int calLeft = sideW + 14;
                int calW = Width - calLeft - 14;

                // Month Title & Navigation Buttons
                int btnSz = 22;
                _prevYearRect = new Rectangle(calLeft, 10, btnSz, btnSz);
                _prevMonthRect = new Rectangle(calLeft + 24, 10, btnSz, btnSz);
                _nextMonthRect = new Rectangle(Width - 50, 10, btnSz, btnSz);
                _nextYearRect = new Rectangle(Width - 26, 10, btnSz, btnSz);

                // Title: "MMMM yyyy"
                using var fontTitle = new Font(Font.FontFamily, 9.5f, FontStyle.Bold);
                using var brushTitle = new SolidBrush(palette.TextPrimary);
                string monthName = _viewMonth.ToString("MMMM yyyy");
                var titleRect = new Rectangle(calLeft + 48, 10, calW - 96, 22);
                var sfTitle = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(monthName, fontTitle, brushTitle, titleRect, sfTitle);

                // Navigation Glyph Arrows («, ‹, ›, »)
                using (var fontNav = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (var brushNav = new SolidBrush(palette.TextSecondary))
                {
                    var sfNav = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("«", fontNav, brushNav, _prevYearRect, sfNav);
                    g.DrawString("‹", fontNav, brushNav, _prevMonthRect, sfNav);
                    g.DrawString("›", fontNav, brushNav, _nextMonthRect, sfNav);
                    g.DrawString("»", fontNav, brushNav, _nextYearRect, sfNav);
                }

                // Day-of-week headers
                string[] dayHeaders = new[] { "CN", "T2", "T3", "T4", "T5", "T6", "T7" };
                int dayW = calW / 7;
                int dayH = 26;
                int startY = 38;

                using var fontHeader = new Font(Font.FontFamily, 7.75f, FontStyle.Bold);
                for (int c = 0; c < 7; c++)
                {
                    var cellRect = new Rectangle(calLeft + (c * dayW), startY, dayW, 18);
                    Color cColor = (c == 0 || c == 6) ? palette.Warning : palette.TextSecondary;
                    using var brushHeader = new SolidBrush(cColor);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(dayHeaders[c], fontHeader, brushHeader, cellRect, sf);
                }

                // Render Calendar Days with Connected Ribbon
                int firstDayOfWeek = (int)_viewMonth.DayOfWeek;
                int daysInMonth = DateTime.DaysInMonth(_viewMonth.Year, _viewMonth.Month);
                int gridY = startY + 20;

                using var fontDay = new Font(Font.FontFamily, 8.5f, FontStyle.Regular);
                using var fontDayBold = new Font(Font.FontFamily, 8.5f, FontStyle.Bold);

                DateTime rangeStart = _tempStart;
                DateTime rangeEnd = (_clickStep == 1 && _hoverDate >= _tempStart) ? _hoverDate : _tempEnd;

                for (int d = 1; d <= daysInMonth; d++)
                {
                    int cellIdx = firstDayOfWeek + d - 1;
                    int r = cellIdx / 7;
                    int c = cellIdx % 7;

                    DateTime curDate = new DateTime(_viewMonth.Year, _viewMonth.Month, d);
                    var cellRect = new Rectangle(calLeft + (c * dayW), gridY + (r * dayH), dayW, dayH);

                    bool isStart = curDate == rangeStart;
                    bool isEnd = curDate == rangeEnd;
                    bool inRange = curDate > rangeStart && curDate < rangeEnd;

                    // Connected Range Ribbon (Continuous soft highlight)
                    if (inRange)
                    {
                        using var brushRange = new SolidBrush(Color.FromArgb(35, palette.Primary));
                        g.FillRectangle(brushRange, new Rectangle(cellRect.X, cellRect.Y + 2, dayW, dayH - 4));
                    }

                    // Rounded Capsule on Start Date
                    if (isStart)
                    {
                        if (rangeEnd > rangeStart)
                        {
                            using var brushHalf = new SolidBrush(Color.FromArgb(35, palette.Primary));
                            g.FillRectangle(brushHalf, new Rectangle(cellRect.X + (dayW / 2), cellRect.Y + 2, dayW / 2, dayH - 4));
                        }
                        using var brushEndpoint = new SolidBrush(palette.Primary);
                        using var pathEp = CreateRoundedRect(new Rectangle(cellRect.X + 2, cellRect.Y + 2, dayW - 4, dayH - 4), 5);
                        g.FillPath(brushEndpoint, pathEp);
                    }

                    // Rounded Capsule on End Date
                    if (isEnd && !isStart)
                    {
                        using var brushHalf = new SolidBrush(Color.FromArgb(35, palette.Primary));
                        g.FillRectangle(brushHalf, new Rectangle(cellRect.X, cellRect.Y + 2, dayW / 2, dayH - 4));

                        using var brushEndpoint = new SolidBrush(palette.Primary);
                        using var pathEp = CreateRoundedRect(new Rectangle(cellRect.X + 2, cellRect.Y + 2, dayW - 4, dayH - 4), 5);
                        g.FillPath(brushEndpoint, pathEp);
                    }

                    Color dayColor = (isStart || isEnd) ? Color.White : palette.TextPrimary;
                    using var brushDay = new SolidBrush(dayColor);
                    var activeFont = (isStart || isEnd) ? fontDayBold : fontDay;
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(d.ToString(), activeFont, brushDay, cellRect, sf);
                }

                // 3. Bottom Action Bar (Apply button)
                var applyRect = new Rectangle(Width - 85, Height - 32, 75, 24);
                using (var brushApply = new SolidBrush(palette.Primary))
                using (var pathApply = CreateRoundedRect(applyRect, 4))
                {
                    g.FillPath(brushApply, pathApply);
                }
                using (var brushApplyText = new SolidBrush(Color.White))
                using (var fontApply = new Font(Font.FontFamily, 8f, FontStyle.Bold))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("Áp Dụng", fontApply, brushApplyText, applyRect, sf);
                }
            }
        }
    }
}
