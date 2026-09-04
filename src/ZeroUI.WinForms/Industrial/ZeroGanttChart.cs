using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.Data;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Industrial
{
    /// <summary>
    /// Modern Enterprise Industrial Gantt Timeline Schedule control for ZeroUI WinForms.
    /// Visualizes production orders, machine maintenance schedules, milestones, and task dependencies
    /// with zero-flicker double-buffered GDI+ vector graphics.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Industrial & SCADA")]
    [Description("Industrial Gantt chart for production scheduling, machine timelines, and maintenance")]
    public class ZeroGanttChart : Control
    {
        private readonly List<GanttTaskItem> _tasks = new List<GanttTaskItem>();
        private DateTime _projectStart = DateTime.Today.AddDays(-2);
        private DateTime _projectEnd = DateTime.Today.AddDays(28);

        private int _headerHeight = 44;
        private int _rowHeight = 32;
        private int _taskListWidth = 240;
        private float _pixelsPerDay = 36f;

        private int _scrollX = 0;
        private int _scrollY = 0;
        private int _selectedTaskIndex = -1;

        public event EventHandler<GanttTaskItem>? TaskSelected;

        [Category("Appearance")]
        [DefaultValue(240)]
        public int TaskListWidth
        {
            get => _taskListWidth;
            set { _taskListWidth = Math.Max(120, value); Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(36f)]
        public float PixelsPerDay
        {
            get => _pixelsPerDay;
            set { _pixelsPerDay = Math.Max(10f, Math.Min(120f, value)); Invalidate(); }
        }

        [Category("Data")]
        public DateTime ProjectStart
        {
            get => _projectStart;
            set { _projectStart = value; Invalidate(); }
        }

        [Category("Data")]
        public DateTime ProjectEnd
        {
            get => _projectEnd;
            set { _projectEnd = value; Invalidate(); }
        }

        [Browsable(false)]
        public List<GanttTaskItem> Tasks => _tasks;

        public ZeroGanttChart()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable |
                ControlStyles.SupportsTransparentBackColor, true);
            DoubleBuffered = true;
            BackColor = ZeroTheme.Colors.Background;
            Size = new Size(720, 380);

            ZeroTheme.ThemeChanged += (s, e) => Invalidate();
        }

        public void AddTask(GanttTaskItem task)
        {
            if (task == null) return;
            _tasks.Add(task);
            Invalidate();
        }

        public void Clear()
        {
            _tasks.Clear();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var colors = ZeroTheme.Colors;
            g.Clear(colors.Background);

            int width = Width;
            int height = Height;
            if (width <= 0 || height <= 0) return;

            // 1. Draw Task Rows
            int totalDays = (int)Math.Max(1, (_projectEnd - _projectStart).TotalDays);
            float timelineW = totalDays * _pixelsPerDay;

            int startRow = Math.Max(0, _scrollY / _rowHeight);
            int visibleRowCount = ((height - _headerHeight) / _rowHeight) + 2;
            int endRow = Math.Min(_tasks.Count, startRow + visibleRowCount);

            // Left Task List Background
            using (var taskBgBrush = new SolidBrush(colors.Surface))
            {
                g.FillRectangle(taskBgBrush, 0, 0, _taskListWidth, height);
            }

            // Draw Rows
            for (int r = startRow; r < endRow; r++)
            {
                var task = _tasks[r];
                int rowY = _headerHeight + r * _rowHeight - _scrollY;

                // Alternate row background in timeline
                if (r % 2 == 1)
                {
                    using (var altBrush = new SolidBrush(colors.CardBackground))
                    {
                        g.FillRectangle(altBrush, _taskListWidth, rowY, width - _taskListWidth, _rowHeight);
                    }
                }

                // Row bottom border
                using (var linePen = new Pen(colors.Border))
                {
                    g.DrawLine(linePen, 0, rowY + _rowHeight - 1, width, rowY + _rowHeight - 1);
                }

                // Draw Left Task Name & Progress
                using (var textBrush = new SolidBrush(colors.TextPrimary))
                using (var mutedBrush = new SolidBrush(colors.TextSecondary))
                using (var nameFont = new Font("Segoe UI", 9f, FontStyle.Regular))
                using (var subFont = new Font("Segoe UI", 8f))
                {
                    var sf = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
                    var nameRect = new Rectangle(12 + task.Level * 14, rowY, _taskListWidth - 70 - task.Level * 14, _rowHeight);
                    g.DrawString(task.Name, nameFont, textBrush, nameRect, sf);

                    // Duration / Progress text
                    string progText = $"{task.Progress * 100:0}%";
                    g.DrawString(progText, subFont, mutedBrush, _taskListWidth - 50, rowY + (_rowHeight - 14) / 2);
                }

                // Draw Timeline Task Bar
                float taskStartX = _taskListWidth + (float)(task.StartDate - _projectStart).TotalDays * _pixelsPerDay - _scrollX;
                float taskEndX = _taskListWidth + (float)(task.EndDate - _projectStart).TotalDays * _pixelsPerDay - _scrollX;
                float barW = Math.Max(6, taskEndX - taskStartX);

                if (taskStartX + barW > _taskListWidth && taskStartX < width)
                {
                    float barY = rowY + 6;
                    float barH = _rowHeight - 12;

                    if (task.IsMilestone)
                    {
                        // Draw Diamond Milestone
                        float midX = taskStartX;
                        float midY = barY + barH / 2;
                        PointF[] diamond = new[]
                        {
                            new PointF(midX, midY - barH / 2),
                            new PointF(midX + barH / 2, midY),
                            new PointF(midX, midY + barH / 2),
                            new PointF(midX - barH / 2, midY)
                        };
                        using (var mBrush = new SolidBrush(colors.Warning))
                        {
                            g.FillPolygon(mBrush, diamond);
                        }
                    }
                    else
                    {
                        // Draw Task Bar
                        var barRect = new RectangleF(taskStartX, barY, barW, barH);
                        Color barColor = colors.Primary;
                        using (var barPath = CreateRoundedRectanglePath(Rectangle.Round(barRect), 4))
                        {
                            using (var barBrush = new SolidBrush(Color.FromArgb(180, barColor)))
                            {
                                g.FillPath(barBrush, barPath);
                            }

                            // Progress fill
                            if (task.Progress > 0)
                            {
                                float progW = barW * Math.Min(1.0f, task.Progress);
                                var progRect = new RectangleF(taskStartX, barY, progW, barH);
                                using (var progPath = CreateRoundedRectanglePath(Rectangle.Round(progRect), 4))
                                using (var progBrush = new SolidBrush(barColor))
                                {
                                    g.FillPath(progBrush, progPath);
                                }
                            }

                            using (var borderPen = new Pen(barColor, 1.2f))
                            {
                                g.DrawPath(borderPen, barPath);
                            }
                        }

                        // Resource label next to bar
                        if (!string.IsNullOrEmpty(task.AssignedResource))
                        {
                            using (var resFont = new Font("Segoe UI", 8f))
                            using (var resBrush = new SolidBrush(colors.TextSecondary))
                            {
                                g.DrawString(task.AssignedResource, resFont, resBrush, taskStartX + barW + 6, barY + 2);
                            }
                        }
                    }
                }
            }

            // 2. Draw Today Line
            float todayX = _taskListWidth + (float)(DateTime.Today - _projectStart).TotalDays * _pixelsPerDay - _scrollX;
            if (todayX >= _taskListWidth && todayX <= width)
            {
                using (var todayPen = new Pen(colors.Danger, 1.5f) { DashStyle = DashStyle.Dash })
                {
                    g.DrawLine(todayPen, todayX, _headerHeight, todayX, height);
                }
            }

            // 3. Draw Header
            using (var headerBrush = new SolidBrush(colors.HeaderBackground))
            {
                g.FillRectangle(headerBrush, 0, 0, width, _headerHeight);
            }
            using (var pen = new Pen(colors.Border))
            {
                g.DrawLine(pen, 0, _headerHeight - 1, width, _headerHeight - 1);
                g.DrawLine(pen, _taskListWidth - 1, 0, _taskListWidth - 1, height);
            }

            // Task List Header Caption
            using (var titleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var textBrush = new SolidBrush(colors.TextPrimary))
            {
                g.DrawString("TASK / OPERATION", titleFont, textBrush, 12, 14);
            }

            // Timeline Day Columns in Header
            using (var font = new Font("Segoe UI", 8.5f))
            using (var dayBrush = new SolidBrush(colors.TextSecondary))
            using (var tickPen = new Pen(colors.Border))
            {
                for (int d = 0; d < totalDays; d++)
                {
                    DateTime date = _projectStart.AddDays(d);
                    float dayX = _taskListWidth + d * _pixelsPerDay - _scrollX;
                    if (dayX + _pixelsPerDay < _taskListWidth || dayX > width) continue;

                    g.DrawLine(tickPen, dayX, 22, dayX, _headerHeight);
                    string dayLabel = date.Day.ToString();
                    g.DrawString(dayLabel, font, dayBrush, dayX + 4, 24);

                    if (date.Day == 1 || d == 0)
                    {
                        using (var monthFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                        {
                            g.DrawString(date.ToString("MMM yyyy"), monthFont, dayBrush, dayX + 4, 4);
                        }
                    }
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Y > _headerHeight)
            {
                int row = (e.Y - _headerHeight + _scrollY) / _rowHeight;
                if (row >= 0 && row < _tasks.Count)
                {
                    _selectedTaskIndex = row;
                    TaskSelected?.Invoke(this, _tasks[row]);
                    Invalidate();
                }
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
