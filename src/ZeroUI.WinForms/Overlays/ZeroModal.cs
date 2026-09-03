using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Editors;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Overlays
{

    /// <summary>
    /// Modern enterprise modal dialog with rounded container, backdrop dimming, and action buttons.
    /// </summary>
    public class ZeroModal : Form
    {
        private readonly string _title;
        private readonly Control _contentControl;
        private readonly Action? _onOk;
        private readonly Action? _onCancel;
        private readonly string _okText;
        private readonly string _cancelText;

        private Rectangle _cardRect;
        private Rectangle _closeRect;
        private bool _isCloseHovered = false;
        private readonly ZeroButton _btnOk;
        private readonly ZeroButton _btnCancel;

        public ZeroModal(
            string title,
            Control contentControl,
            Action? onOk = null,
            Action? onCancel = null,
            string okText = "OK",
            string cancelText = "Cancel",
            int cardWidth = 520,
            int cardHeight = 360)
        {
            _title = title;
            _contentControl = contentControl;
            _onOk = onOk;
            _onCancel = onCancel;
            _okText = okText;
            _cancelText = cancelText;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            KeyPreview = true;
            Size = new Size(cardWidth + 80, cardHeight + 80);
            BackColor = Color.FromArgb(15, 23, 42); // Overlay backdrop dark
            Opacity = 0.98;

            _cardRect = new Rectangle(40, 40, cardWidth, cardHeight);

            // Setup isolated body container inside card
            var bodyPanel = new Panel
            {
                Location = new Point(_cardRect.Left + 20, _cardRect.Top + 55),
                Size = new Size(_cardRect.Width - 40, _cardRect.Height - 115),
                BackColor = Color.Transparent
            };
            _contentControl.Dock = DockStyle.Fill;
            bodyPanel.Controls.Add(_contentControl);
            Controls.Add(bodyPanel);

            // OK Button
            _btnOk = new ZeroButton
            {
                Text = _okText,
                ButtonStyle = ZeroButtonStyle.Primary,
                Size = new Size(95, 34),
                Location = new Point(_cardRect.Right - 115, _cardRect.Bottom - 48)
            };
            _btnOk.Click += (s, e) =>
            {
                DialogResult = DialogResult.OK;
                _onOk?.Invoke();
                Close();
            };
            Controls.Add(_btnOk);

            // Cancel Button
            _btnCancel = new ZeroButton
            {
                Text = _cancelText,
                ButtonStyle = ZeroButtonStyle.Secondary,
                Size = new Size(95, 34),
                Location = new Point(_cardRect.Right - 220, _cardRect.Bottom - 48)
            };
            _btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                _onCancel?.Invoke();
                Close();
            };
            Controls.Add(_btnCancel);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                _onCancel?.Invoke();
                Close();
            }
        }


        public static DialogResult Show(
            IWin32Window parent,
            string title,
            Control content,
            Action? onOk = null,
            Action? onCancel = null,
            string okText = "OK",
            string cancelText = "Cancel",
            int width = 520,
            int height = 360)
        {
            using var modal = new ZeroModal(title, content, onOk, onCancel, okText, cancelText, width, height);
            return modal.ShowDialog(parent);
        }

        public static DialogResult Confirm(
            IWin32Window parent,
            string title,
            string message,
            Action onConfirm,
            Action? onCancel = null,
            string confirmText = "Confirm",
            string cancelText = "Cancel")
        {
            var lbl = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 10f),
                ForeColor = ZeroTheme.Colors.TextPrimary,
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            return Show(parent, title, lbl, onConfirm, onCancel, confirmText, cancelText, 440, 220);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var theme = ZeroTheme.Colors;

            // 1. Draw Centered Card
            using (var path = CreateRoundedRectangle(_cardRect, 10))
            {
                using var cardBrush = new SolidBrush(theme.Surface);
                g.FillPath(cardBrush, path);

                using var borderPen = new Pen(theme.Border, 1.2f);
                g.DrawPath(borderPen, path);
            }

            // 2. Draw Header (Title & Divider)
            int headerY = _cardRect.Top + 18;
            using var titleFont = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            Rectangle titleRect = new Rectangle(_cardRect.Left + 20, headerY, _cardRect.Width - 60, 24);
            TextRenderer.DrawText(g, _title, titleFont, titleRect, theme.TextPrimary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            using (var divPen = new Pen(theme.Border, 1f))
            {
                g.DrawLine(divPen, _cardRect.Left, _cardRect.Top + 50, _cardRect.Right, _cardRect.Top + 50);
                g.DrawLine(divPen, _cardRect.Left, _cardRect.Bottom - 60, _cardRect.Right, _cardRect.Bottom - 60);
            }

            // 3. Draw Close (✕) Button
            _closeRect = new Rectangle(_cardRect.Right - 38, _cardRect.Top + 16, 24, 24);
            if (_isCloseHovered)
            {
                using var bPath = CreateRoundedRectangle(_closeRect, 4);
                using var bBrush = new SolidBrush(theme.Hover);
                g.FillPath(bBrush, bPath);
            }

            TextRenderer.DrawText(
                g,
                "✕",
                new Font("Segoe UI", 9.5f, FontStyle.Bold),
                _closeRect,
                _isCloseHovered ? theme.TextPrimary : theme.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_closeRect.IsEmpty && _closeRect.Contains(e.Location))
            {
                if (!_isCloseHovered)
                {
                    _isCloseHovered = true;
                    Cursor = Cursors.Hand;
                    Invalidate(_closeRect);
                }
            }
            else if (_isCloseHovered)
            {
                _isCloseHovered = false;
                Cursor = Cursors.Default;
                Invalidate(_closeRect);
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button == MouseButtons.Left)
            {
                if ((!_closeRect.IsEmpty && _closeRect.Contains(e.Location)) || !_cardRect.Contains(e.Location))
                {
                    DialogResult = DialogResult.Cancel;
                    _onCancel?.Invoke();
                    Close();
                }
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
    }
}
