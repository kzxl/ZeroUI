using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ZeroUI.WinForms.Buttons
{
    /// <summary>
    /// Lightweight borderless owned floating window designed to host <see cref="ZFloatingActionButton"/>.
    /// Permanently stays on top of all child controls, tabs, and data grids of the parent form
    /// without ever being obscured, losing z-order priority, or stealing focus.
    /// </summary>
    public class ZFloatingActionOverlay : Form
    {
        private readonly Form _parentForm;
        private readonly ZFloatingActionButton _button;
        private readonly int _rightMargin;
        private readonly int _bottomMargin;
        private bool _isDisposed;

        public ZFloatingActionButton Button => _button;

        public ZFloatingActionOverlay(Form parentForm, ZFloatingActionButton button, int rightMargin = 28, int bottomMargin = 28)
        {
            _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            _button = button ?? throw new ArgumentNullException(nameof(button));
            _rightMargin = rightMargin;
            _bottomMargin = bottomMargin;

            // Form properties for pure floating overlay
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = _button.Size;
            this.BackColor = Color.Magenta;
            this.TransparencyKey = Color.Magenta;
            this.Owner = _parentForm;

            // Circular clipping at window level - 100% transparent corners
            UpdateRegion();

            _button.Dock = DockStyle.Fill;
            this.Controls.Add(_button);

            // Synchronize overlay position with parent form
            UpdatePosition();

            _parentForm.LocationChanged += OnParentStateChanged;
            _parentForm.SizeChanged += OnParentStateChanged;
            _parentForm.VisibleChanged += OnParentStateChanged;
            _parentForm.Activated += OnParentActivated;
            _parentForm.FormClosing += OnParentFormClosing;
        }

        #region Win32 Z-Order Control

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOP = IntPtr.Zero;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        /// <summary>
        /// Explicitly elevates this overlay to the absolute top of the window Z-order without stealing focus.
        /// </summary>
        public void BringToTop()
        {
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                SetWindowPos(this.Handle, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
        }

        #endregion

        /// <summary>
        /// Prevents this overlay window from stealing input focus from the parent form.
        /// </summary>
        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(0, 0, Width, Height);
                this.Region = new Region(path);
            }
        }

        private void OnParentStateChanged(object sender, EventArgs e)
        {
            if (_isDisposed || this.IsDisposed) return;
            UpdatePosition();
            BringToTop();
        }

        private void OnParentActivated(object sender, EventArgs e)
        {
            if (_isDisposed || this.IsDisposed) return;
            UpdatePosition();
            BringToTop();
        }

        private void OnParentFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_isDisposed)
            {
                this.Close();
            }
        }

        /// <summary>
        /// Recalculates absolute screen coordinates relative to the parent form's client area and raises to top.
        /// </summary>
        public void UpdatePosition()
        {
            if (_parentForm.WindowState == FormWindowState.Minimized || !_parentForm.Visible)
            {
                if (this.Visible) this.Hide();
                return;
            }

            if (!this.Visible) this.Show();

            var clientPt = new Point(
                Math.Max(10, _parentForm.ClientSize.Width - this.Width - _rightMargin),
                Math.Max(10, _parentForm.ClientSize.Height - this.Height - _bottomMargin)
            );

            this.Location = _parentForm.PointToScreen(clientPt);
            BringToTop();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                _isDisposed = true;
                _parentForm.LocationChanged -= OnParentStateChanged;
                _parentForm.SizeChanged -= OnParentStateChanged;
                _parentForm.VisibleChanged -= OnParentStateChanged;
                _parentForm.Activated -= OnParentActivated;
                _parentForm.FormClosing -= OnParentFormClosing;
            }
            base.Dispose(disposing);
        }
    }
}
