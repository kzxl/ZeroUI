using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{

    /// <summary>
    /// Modern date picker input control with interactive calendar dropdown and quick-select presets.
    /// </summary>
    public class ZeroDatePicker : Control
    {
        private DateTime _selectedDate = DateTime.Today;
        private string _dateFormat = "yyyy-MM-dd";
        private bool _isHovered = false;
        private bool _isFocused = false;
        private ToolStripDropDown? _popup;
        private MonthCalendarPopupControl? _calendarControl;

        public event EventHandler? ValueChanged;

        public ZeroDatePicker()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);

            Size = new Size(160, 34);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            Cursor = Cursors.Hand;
        }

        [Category("Data")]
        public DateTime Value
        {
            get => _selectedDate;
            set
            {
                if (_selectedDate != value)
                {
                    _selectedDate = value;
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
            set { _dateFormat = value ?? "yyyy-MM-dd"; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var theme = ZeroTheme.Colors;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // 1. Draw Background & Border
            Color borderColor = _isFocused ? theme.Primary : (_isHovered ? theme.PrimaryHover : theme.Border);
            using (var path = CreateRoundedRectangle(rect, 6))
            {
                using var bgBrush = new SolidBrush(theme.Surface);
                g.FillPath(bgBrush, path);

                using var pen = new Pen(borderColor, _isFocused ? 1.5f : 1f);
                g.DrawPath(pen, path);
            }

            // 2. Draw Calendar Glyph (📅)
            Rectangle iconRect = new Rectangle(10, 0, 20, Height);
            TextRenderer.DrawText(g, "📅", new Font("Segoe UI", 10f), iconRect, theme.TextSecondary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            // 3. Draw Formatted Date Text
            string dateStr = _selectedDate.ToString(_dateFormat);
            Rectangle textRect = new Rectangle(34, 0, Width - 42, Height);
            TextRenderer.DrawText(g, dateStr, Font, textRect, theme.TextPrimary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
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

            _calendarControl = new MonthCalendarPopupControl(this, _selectedDate);
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
                DropShadowEnabled = true
            };
            _popup.Items.Add(host);
            _popup.Closed += (s, e) =>
            {
                _isFocused = false;
                Invalidate();
            };

            _isFocused = true;
            Invalidate();
            _popup.Show(this, new Point(0, Height + 2));
        }

        internal void OnDateSelectedFromPopup(DateTime date)
        {
            Value = date;
            _popup?.Close();
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

        private sealed class MonthCalendarPopupControl : Control
        {
            private readonly ZeroDatePicker _owner;
            private DateTime _viewMonth;
            private DateTime _selectedDate;
            private readonly MonthCalendar _monthCalendar;

            public MonthCalendarPopupControl(ZeroDatePicker owner, DateTime initialDate)
            {
                _owner = owner;
                _selectedDate = initialDate;
                _viewMonth = new DateTime(initialDate.Year, initialDate.Month, 1);

                Size = new Size(240, 220);
                BackColor = ZeroTheme.Colors.Surface;

                // Quick presets bar at top
                var topBar = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = Color.Transparent, Padding = new Padding(4, 4, 4, 2) };
                
                var btnToday = CreatePresetButton("Today", () => SelectPreset(DateTime.Today));
                var btnYesterday = CreatePresetButton("Yesterday", () => SelectPreset(DateTime.Today.AddDays(-1)));
                var btnThisWeek = CreatePresetButton("This Week", () => SelectPreset(DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek)));

                topBar.Controls.Add(btnThisWeek);
                topBar.Controls.Add(btnYesterday);
                topBar.Controls.Add(btnToday);

                // MonthCalendar for reliable month navigation
                _monthCalendar = new MonthCalendar
                {
                    Dock = DockStyle.Fill,
                    MaxSelectionCount = 1,
                    SelectionStart = initialDate,
                    SelectionEnd = initialDate,
                    ShowToday = true,
                    ShowTodayCircle = true
                };
                _monthCalendar.DateSelected += (s, e) => _owner.OnDateSelectedFromPopup(e.Start);

                Controls.Add(_monthCalendar);
                Controls.Add(topBar);
            }

            private Button CreatePresetButton(string text, Action onClick)
            {
                var btn = new Button
                {
                    Text = text,
                    Dock = DockStyle.Left,
                    Width = 72,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    BackColor = ZeroTheme.Colors.Hover,
                    ForeColor = ZeroTheme.Colors.TextPrimary,
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) => onClick();
                return btn;
            }

            private void SelectPreset(DateTime dt)
            {
                _owner.OnDateSelectedFromPopup(dt);
            }
        }
    }
}
