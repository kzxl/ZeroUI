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
    /// Enterprise Dual-Date Range Selector (From Date -> To Date) with 1-click quick preset filters
    /// and interactive calendar range popup.
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
        private readonly ToolStripDropDown _dropdown;
        private readonly DateRangePopupControl _popupControl;

        public event EventHandler? DateRangeChanged;

        public ZeroDateRangePicker()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);

            Size = new Size(250, 36);
            Font = new Font("Segoe UI", 9.5f);
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
                _popupControl.Size = new Size(480, 270);
                _dropdown.Size = new Size(480, 270);
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

            // 1. Background
            using (var path = CreateRoundedRect(rect, 6))
            {
                using var brushBg = new SolidBrush(palette.Surface);
                g.FillPath(brushBg, path);

                Color borderCol = _isHovered ? palette.Primary : palette.Border;
                using var penBorder = new Pen(borderCol, 1.2f);
                g.DrawPath(penBorder, path);
            }

            // 2. Calendar Glyph (📅)
            using (var iconFont = new Font("Segoe UI Emoji", 10f))
            using (var brushIcon = new SolidBrush(palette.Primary))
            {
                g.DrawString("📅", iconFont, brushIcon, 10, (Height - 19) / 2);
            }

            // 3. Date Range Text: "2026-09-01  →  2026-09-03"
            string text = $"{_startDate.ToString(_dateFormat)}  →  {_endDate.ToString(_dateFormat)}";
            using (var fontText = new Font(Font.FontFamily, 9f, FontStyle.Bold))
            using (var brushText = new SolidBrush(palette.TextPrimary))
            {
                g.DrawString(text, fontText, brushText, 34, (Height - 16) / 2);
            }

            // 4. Dropdown Chevron (▼)
            using (var chevBrush = new SolidBrush(palette.TextSecondary))
            {
                int cx = Width - 16;
                int cy = Height / 2;
                PointF[] pts = new[]
                {
                    new PointF(cx - 3.5f, cy - 2f),
                    new PointF(cx + 3.5f, cy - 2f),
                    new PointF(cx, cy + 2.5f)
                };
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
        /// Inner calendar & preset popup container.
        /// </summary>
        private class DateRangePopupControl : Control
        {
            private readonly ZeroDateRangePicker _picker;
            private DateTime _tempStart;
            private DateTime _tempEnd;
            private DateTime _viewMonth;
            private int _clickStep = 0; // 0: picking start, 1: picking end

            private Rectangle[] _presetRects = new Rectangle[7];
            private readonly string[] _presetNames = new[]
            {
                "Today", "Yesterday", "Last 7 Days", "Last 30 Days", "This Month", "Last Month", "Year-to-Date"
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
                BackColor = Color.FromArgb(30, 41, 59);
            }

            public void SyncFromPicker(DateTime start, DateTime end)
            {
                _tempStart = start;
                _tempEnd = end;
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

                // Check presets
                for (int i = 0; i < _presetRects.Length; i++)
                {
                    if (_presetRects[i].Contains(e.Location))
                    {
                        _picker.ApplyPreset(_presetValues[i]);
                        _picker.ClosePopup();
                        return;
                    }
                }

                // Month navigation
                int calLeft = 140;
                var prevRect = new Rectangle(calLeft + 6, 10, 24, 24);
                var nextRect = new Rectangle(Width - 36, 10, 24, 24);

                if (prevRect.Contains(e.Location))
                {
                    _viewMonth = _viewMonth.AddMonths(-1);
                    Invalidate();
                    return;
                }
                if (nextRect.Contains(e.Location))
                {
                    _viewMonth = _viewMonth.AddMonths(1);
                    Invalidate();
                    return;
                }

                // Apply button
                var applyRect = new Rectangle(Width - 85, Height - 34, 75, 26);
                if (applyRect.Contains(e.Location))
                {
                    _picker.SetRange(_tempStart, _tempEnd);
                    _picker.ClosePopup();
                    return;
                }

                // Day grid click
                int startY = 65;
                int dayW = (Width - calLeft - 20) / 7;
                int dayH = 26;

                if (e.X >= calLeft && e.X < Width - 20 && e.Y >= startY && e.Y < startY + (6 * dayH))
                {
                    int col = (e.X - calLeft) / dayW;
                    int row = (e.Y - startY) / dayH;

                    int firstDayOfWeek = (int)_viewMonth.DayOfWeek; // 0 is Sunday
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

                // 1. Left Preset Sidebar (Width = 130)
                int sideW = 130;
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
                    int py = 12 + (i * 32);
                    _presetRects[i] = new Rectangle(8, py, sideW - 16, 26);

                    bool isHov = i == _hoveredPreset;
                    bool isCur = _picker.Preset == _presetValues[i];

                    if (isCur)
                    {
                        using var brushCur = new SolidBrush(Color.FromArgb(40, palette.Primary));
                        using var pathCur = CreateRoundedRect(_presetRects[i], 4);
                        g.FillPath(brushCur, pathCur);
                    }
                    else if (isHov)
                    {
                        using var brushHov = new SolidBrush(Color.FromArgb(20, palette.Primary));
                        using var pathHov = CreateRoundedRect(_presetRects[i], 4);
                        g.FillPath(brushHov, pathHov);
                    }

                    Color textColor = isCur ? palette.Primary : palette.TextPrimary;
                    using var brushText = new SolidBrush(textColor);
                    var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                    g.DrawString(_presetNames[i], fontPreset, brushText, new Rectangle(_presetRects[i].X + 8, _presetRects[i].Y, _presetRects[i].Width - 8, _presetRects[i].Height), sf);
                }

                // 2. Right Calendar Area
                int calLeft = sideW + 12;
                int calW = Width - calLeft - 12;

                // Month Title & Arrows
                using var fontTitle = new Font(Font.FontFamily, 10f, FontStyle.Bold);
                using var brushTitle = new SolidBrush(palette.TextPrimary);
                string monthName = _viewMonth.ToString("MMMM yyyy");
                g.DrawString(monthName, fontTitle, brushTitle, calLeft + 40, 12);

                // Prev / Next Buttons (◀ / ▶)
                using var brushArrow = new SolidBrush(palette.TextSecondary);
                g.DrawString("◀", fontPreset, brushArrow, calLeft + 10, 13);
                g.DrawString("▶", fontPreset, brushArrow, Width - 30, 13);

                // Day-of-week headers
                string[] dayHeaders = new[] { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" };
                int dayW = calW / 7;
                int dayH = 26;
                int startY = 40;

                using var fontHeader = new Font(Font.FontFamily, 8f, FontStyle.Bold);
                using var brushHeader = new SolidBrush(palette.TextSecondary);
                for (int c = 0; c < 7; c++)
                {
                    var rect = new Rectangle(calLeft + (c * dayW), startY, dayW, 20);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(dayHeaders[c], fontHeader, brushHeader, rect, sf);
                }

                // Render Calendar Days
                int firstDayOfWeek = (int)_viewMonth.DayOfWeek;
                int daysInMonth = DateTime.DaysInMonth(_viewMonth.Year, _viewMonth.Month);
                int gridY = startY + 22;

                using var fontDay = new Font(Font.FontFamily, 8.5f, FontStyle.Regular);

                for (int d = 1; d <= daysInMonth; d++)
                {
                    int cellIdx = firstDayOfWeek + d - 1;
                    int r = cellIdx / 7;
                    int c = cellIdx % 7;

                    DateTime curDate = new DateTime(_viewMonth.Year, _viewMonth.Month, d);
                    var cellRect = new Rectangle(calLeft + (c * dayW), gridY + (r * dayH), dayW, dayH);

                    bool isStart = curDate == _tempStart;
                    bool isEnd = curDate == _tempEnd;
                    bool inRange = curDate > _tempStart && curDate < _tempEnd;

                    if (inRange)
                    {
                        using var brushRange = new SolidBrush(Color.FromArgb(35, palette.Primary));
                        g.FillRectangle(brushRange, cellRect);
                    }

                    if (isStart || isEnd)
                    {
                        using var brushEndpoint = new SolidBrush(palette.Primary);
                        using var pathEp = CreateRoundedRect(new Rectangle(cellRect.X + 2, cellRect.Y + 2, dayW - 4, dayH - 4), 4);
                        g.FillPath(brushEndpoint, pathEp);
                    }

                    Color dayColor = (isStart || isEnd) ? Color.White : palette.TextPrimary;
                    using var brushDay = new SolidBrush(dayColor);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(d.ToString(), fontDay, brushDay, cellRect, sf);
                }

                // 3. Bottom Action Bar (Apply button)
                var applyRect = new Rectangle(Width - 85, Height - 34, 75, 26);
                using (var brushApply = new SolidBrush(palette.Primary))
                using (var pathApply = CreateRoundedRect(applyRect, 4))
                {
                    g.FillPath(brushApply, pathApply);
                }
                using (var brushApplyText = new SolidBrush(Color.White))
                using (var fontApply = new Font(Font.FontFamily, 8.5f, FontStyle.Bold))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("Apply", fontApply, brushApplyText, applyRect, sf);
                }
            }
        }
    }
}
