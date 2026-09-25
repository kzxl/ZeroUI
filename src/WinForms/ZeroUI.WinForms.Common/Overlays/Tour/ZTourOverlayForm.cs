using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Overlays
{
    internal sealed class ZTourOverlayForm : Form
    {
        private readonly ZTour _tour;
        private readonly Form _ownerForm;
        private Rectangle _targetRect = Rectangle.Empty;

        // Card Layout
        private Rectangle _cardRect = Rectangle.Empty;
        private Rectangle _btnCloseRect = Rectangle.Empty;
        private Rectangle _btnSkipRect = Rectangle.Empty;
        private Rectangle _btnPrevRect = Rectangle.Empty;
        private Rectangle _btnNextRect = Rectangle.Empty;

        private bool _isNextHover;
        private bool _isPrevHover;
        private bool _isSkipHover;
        private bool _isCloseHover;

        public ZTourOverlayForm(Form owner, ZTour tour)
        {
            _ownerForm = owner ?? throw new ArgumentNullException(nameof(owner));
            _tour = tour ?? throw new ArgumentNullException(nameof(tour));

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = false;
            KeyPreview = true;
            BackColor = Color.FromArgb(10, 15, 26);

            SyncBounds();

            _ownerForm.LocationChanged += (s, e) => { SyncBounds(); Invalidate(); };
            _ownerForm.SizeChanged += (s, e) => { SyncBounds(); Invalidate(); };
        }

        public void DisplayStep(ZTourStep step, int currentIndex, int totalCount)
        {
            _targetRect = ResolveTargetRect(step);
            RecalculateCardLayout(step, currentIndex, totalCount);
            Invalidate();
        }

        private void SyncBounds()
        {
            if (_ownerForm != null && !_ownerForm.IsDisposed)
            {
                var screenPt = _ownerForm.PointToScreen(Point.Empty);
                Location = screenPt;
                Size = _ownerForm.ClientSize;
            }
        }

        private Rectangle ResolveTargetRect(ZTourStep step)
        {
            Control? target = step.Target;
            if (target == null && !string.IsNullOrWhiteSpace(step.TargetName))
            {
                var matches = _ownerForm.Controls.Find(step.TargetName, true);
                if (matches.Length > 0) target = matches[0];
            }

            if (target == null || !target.Visible)
                return Rectangle.Empty;

            var screenPt = target.PointToScreen(Point.Empty);
            var localPt = PointToClient(screenPt);
            var pad = step.TargetPadding;

            return new Rectangle(
                localPt.X - pad.Left,
                localPt.Y - pad.Top,
                target.Width + pad.Left + pad.Right,
                target.Height + pad.Top + pad.Bottom);
        }

        private void RecalculateCardLayout(ZTourStep step, int currentIndex, int totalCount)
        {
            int cardW = 340;
            int cardH = 150;
            int x;
            int y;

            ZTourPlacement placement = step.Placement;
            if (_targetRect.IsEmpty || placement == ZTourPlacement.Center)
            {
                x = (ClientSize.Width - cardW) / 2;
                y = (ClientSize.Height - cardH) / 2;
            }
            else
            {
                if (placement == ZTourPlacement.Auto)
                {
                    if (_targetRect.Bottom + cardH + 16 <= ClientSize.Height)
                        placement = ZTourPlacement.Bottom;
                    else if (_targetRect.Top - cardH - 16 >= 0)
                        placement = ZTourPlacement.Top;
                    else if (_targetRect.Right + cardW + 16 <= ClientSize.Width)
                        placement = ZTourPlacement.Right;
                    else
                        placement = ZTourPlacement.Left;
                }

                switch (placement)
                {
                    case ZTourPlacement.Top:
                        x = _targetRect.Left + (_targetRect.Width - cardW) / 2;
                        y = _targetRect.Top - cardH - 12;
                        break;
                    case ZTourPlacement.Bottom:
                        x = _targetRect.Left + (_targetRect.Width - cardW) / 2;
                        y = _targetRect.Bottom + 12;
                        break;
                    case ZTourPlacement.Left:
                        x = _targetRect.Left - cardW - 12;
                        y = _targetRect.Top + (_targetRect.Height - cardH) / 2;
                        break;
                    case ZTourPlacement.Right:
                        x = _targetRect.Right + 12;
                        y = _targetRect.Top + (_targetRect.Height - cardH) / 2;
                        break;
                    default:
                        x = (ClientSize.Width - cardW) / 2;
                        y = (ClientSize.Height - cardH) / 2;
                        break;
                }
            }

            // Kẹp toạ độ trong màn hình
            x = Math.Max(12, Math.Min(ClientSize.Width - cardW - 12, x));
            y = Math.Max(12, Math.Min(ClientSize.Height - cardH - 12, y));

            _cardRect = new Rectangle(x, y, cardW, cardH);
            _btnCloseRect = new Rectangle(_cardRect.Right - 28, _cardRect.Top + 10, 20, 20);

            int btnY = _cardRect.Bottom - 38;
            _btnNextRect = new Rectangle(_cardRect.Right - 90, btnY, 76, 26);
            _btnPrevRect = new Rectangle(_btnNextRect.Left - 80, btnY, 72, 26);
            _btnSkipRect = new Rectangle(_btnPrevRect.Left - 70, btnY, 60, 26);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var currentStep = _tour.CurrentStep;
            if (currentStep == null) return;

            // 1. Vẽ Backdrop Mask mờ tối
            using (var fullBrush = new SolidBrush(Color.FromArgb(170, 10, 15, 26)))
            {
                if (!_targetRect.IsEmpty && currentStep.Mask)
                {
                    using (var region = new Region(ClientRectangle))
                    using (var holePath = CreateRoundedRectPath(_targetRect, currentStep.CornerRadius))
                    {
                        region.Exclude(holePath);
                        g.FillRegion(fullBrush, region);
                    }

                    // Spotlight Border
                    using (var pen = new Pen(Color.FromArgb(59, 130, 246), 2))
                    using (var holePath = CreateRoundedRectPath(_targetRect, currentStep.CornerRadius))
                    {
                        g.DrawPath(pen, holePath);
                    }
                }
                else
                {
                    g.FillRectangle(fullBrush, ClientRectangle);
                }
            }

            // 2. Vẽ Popover Card
            using (var cardPath = CreateRoundedRectPath(_cardRect, 10))
            using (var cardBgBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (var cardBorderPen = new Pen(Color.FromArgb(51, 65, 85), 1))
            {
                g.FillPath(cardBgBrush, cardPath);
                g.DrawPath(cardBorderPen, cardPath);
            }

            // 3. Header: Step Badge & Title
            string badgeText = $"{_tour.CurrentIndex + 1}/{_tour.Steps.Count}";
            var badgeRect = new Rectangle(_cardRect.Left + 14, _cardRect.Top + 14, 34, 18);
            using (var badgePath = CreateRoundedRectPath(badgeRect, 4))
            using (var badgeBrush = new SolidBrush(Color.FromArgb(59, 130, 246)))
            {
                g.FillPath(badgeBrush, badgePath);
            }
            using (var badgeFont = new Font("Segoe UI", 8f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, badgeText, badgeFont, badgeRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            var titleRect = new Rectangle(badgeRect.Right + 8, _cardRect.Top + 12, _cardRect.Width - 90, 22);
            using (var titleFont = new Font("Segoe UI", 10f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, currentStep.Title, titleFont, titleRect, Color.FromArgb(248, 250, 252), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            // Nút đóng (X)
            using (var closeFont = new Font("Segoe UI", 9f))
            {
                var closeColor = _isCloseHover ? Color.FromArgb(239, 68, 68) : Color.FromArgb(148, 163, 184);
                TextRenderer.DrawText(g, "✕", closeFont, _btnCloseRect, closeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            // 4. Body: Description
            var descRect = new Rectangle(_cardRect.Left + 14, _cardRect.Top + 42, _cardRect.Width - 28, 60);
            using (var descFont = new Font("Segoe UI", 9f))
            {
                TextRenderer.DrawText(g, currentStep.Description, descFont, descRect, Color.FromArgb(148, 163, 184), TextFormatFlags.Left | TextFormatFlags.WordBreak);
            }

            // 5. Footer: Dot Indicators
            int dotX = _cardRect.Left + 16;
            int dotY = _cardRect.Bottom - 26;
            for (int i = 0; i < _tour.Steps.Count; i++)
            {
                bool active = (i == _tour.CurrentIndex);
                int w = active ? 14 : 6;
                using (var dotBrush = new SolidBrush(active ? Color.FromArgb(59, 130, 246) : Color.FromArgb(71, 85, 105)))
                {
                    g.FillEllipse(dotBrush, dotX, dotY, w, 6);
                }
                dotX += w + 4;
            }

            // 6. Action Buttons
            bool isLast = (_tour.CurrentIndex == _tour.Steps.Count - 1);

            // Nút Bỏ qua
            if (!isLast)
            {
                using (var skipFont = new Font("Segoe UI", 8.5f))
                {
                    var skipColor = _isSkipHover ? Color.White : Color.FromArgb(148, 163, 184);
                    TextRenderer.DrawText(g, "Skip", skipFont, _btnSkipRect, skipColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }

            // Previous button
            if (_tour.CurrentIndex > 0)
            {
                using (var btnPath = CreateRoundedRectPath(_btnPrevRect, 4))
                using (var btnBg = new SolidBrush(_isPrevHover ? Color.FromArgb(51, 65, 85) : Color.FromArgb(30, 41, 59)))
                using (var btnPen = new Pen(Color.FromArgb(71, 85, 105), 1))
                {
                    g.FillPath(btnBg, btnPath);
                    g.DrawPath(btnPen, btnPath);
                }
                using (var prevFont = new Font("Segoe UI", 8.5f))
                {
                    TextRenderer.DrawText(g, currentStep.PrevButtonText ?? "Previous", prevFont, _btnPrevRect, Color.FromArgb(241, 245, 249), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }

            // Next / Finish button
            using (var btnPath = CreateRoundedRectPath(_btnNextRect, 4))
            using (var btnBg = new SolidBrush(_isNextHover ? Color.FromArgb(37, 99, 235) : Color.FromArgb(59, 130, 246)))
            {
                g.FillPath(btnBg, btnPath);
            }
            using (var nextFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                string nextText = currentStep.NextButtonText ?? (isLast ? "Finish" : "Next");
                TextRenderer.DrawText(g, nextText, nextFont, _btnNextRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool prevNext = _isNextHover;
            bool prevPrev = _isPrevHover;
            bool prevSkip = _isSkipHover;
            bool prevClose = _isCloseHover;

            _isNextHover = _btnNextRect.Contains(e.Location);
            _isPrevHover = _btnPrevRect.Contains(e.Location);
            _isSkipHover = _btnSkipRect.Contains(e.Location);
            _isCloseHover = _btnCloseRect.Contains(e.Location);

            Cursor = (_isNextHover || _isPrevHover || _isSkipHover || _isCloseHover) ? Cursors.Hand : Cursors.Default;

            if (prevNext != _isNextHover || prevPrev != _isPrevHover || prevSkip != _isSkipHover || prevClose != _isCloseHover)
            {
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (_btnCloseRect.Contains(e.Location))
            {
                _tour.Close(completed: false);
            }
            else if (_btnSkipRect.Contains(e.Location))
            {
                _tour.Close(completed: false);
            }
            else if (_btnPrevRect.Contains(e.Location) && _tour.CurrentIndex > 0)
            {
                _tour.Previous();
            }
            else if (_btnNextRect.Contains(e.Location))
            {
                _tour.Next();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                _tour.Close(completed: false);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Enter)
            {
                _tour.Next();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Left)
            {
                _tour.Previous();
                e.Handled = true;
            }
        }

        private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            if (rect.Width <= 0 || rect.Height <= 0) return path;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
