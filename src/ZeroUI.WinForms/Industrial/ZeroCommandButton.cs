using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.Rendering;
using ZeroUI.Core.Scada;
using ZeroUI.WinForms.Native;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Industrial
{
    public enum CommandButtonAction
    {
        Start,
        Stop,
        Reset,
        Open,
        Close,
        EmergencyStop,
        Custom
    }

    /// <summary>
    /// Industrial operator command button with safety interlock verification,
    /// two-stage confirmation dialog or press-and-hold progress ring to prevent unintended touch triggers.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Industrial & SCADA")]
    [Description("Industrial command button with interlock protection and safety confirmation")]
    public class ZeroCommandButton : Control, IAnimationFrameListener
    {
        private CommandButtonAction _action = CommandButtonAction.Start;
        private string _commandText = "START MOTOR";
        private bool _isInterlocked = false;
        private string _interlockReason = "";
        private bool _requiresConfirmation = true;
        private float _pressAndHoldSeconds = 1.0f; // 0 = click instant (or prompt dialog)
        private string? _targetTagPath;

        private bool _isHovered;
        private bool _isPressed;
        private float _heldElapsedSeconds = 0f;
        private IDisposable? _clockToken;

        /// <summary>
        /// Fired when safety conditions are satisfied and the operator command is executed.
        /// </summary>
        public event EventHandler? CommandExecuted;

        /// <summary>
        /// Fired when an operator attempts to press an interlocked button.
        /// </summary>
        public event EventHandler<string>? InterlockBlocked;

        [Category("Command Safety")]
        [DefaultValue(CommandButtonAction.Start)]
        public CommandButtonAction Action
        {
            get => _action;
            set { _action = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue("START MOTOR")]
        public string CommandText
        {
            get => _commandText;
            set { _commandText = value ?? ""; Invalidate(); }
        }

        [Category("Command Safety")]
        [DefaultValue(false)]
        public bool IsInterlocked
        {
            get => _isInterlocked;
            set { _isInterlocked = value; Invalidate(); }
        }

        [Category("Command Safety")]
        [DefaultValue("")]
        public string InterlockReason
        {
            get => _interlockReason;
            set => _interlockReason = value ?? "";
        }

        [Category("Command Safety")]
        [DefaultValue(true)]
        public bool RequiresConfirmation
        {
            get => _requiresConfirmation;
            set => _requiresConfirmation = value;
        }

        [Category("Command Safety")]
        [DefaultValue(1.0f)]
        public float PressAndHoldSeconds
        {
            get => _pressAndHoldSeconds;
            set => _pressAndHoldSeconds = Math.Max(0f, value);
        }

        [Category("SCADA Telemetry")]
        public string? TargetTagPath
        {
            get => _targetTagPath;
            set => _targetTagPath = value;
        }

        public ZeroCommandButton()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(160, 48);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            Cursor = Cursors.Hand;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!ZeroDesignHelper.IsInDesignMode(this))
            {
                _clockToken = ZeroAnimationClock.Subscribe(OnAnimationFrameTick);
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);
            _clockToken?.Dispose();
            _clockToken = null;
        }

        public void OnAnimationFrame(double deltaSeconds, long frameCount)
        {
            OnAnimationFrameTick(deltaSeconds, frameCount);
        }

        private void OnAnimationFrameTick(double deltaSeconds, long frameCount)
        {
            if (_isPressed && _pressAndHoldSeconds > 0 && !_isInterlocked)
            {
                _heldElapsedSeconds += (float)deltaSeconds;
                if (_heldElapsedSeconds >= _pressAndHoldSeconds)
                {
                    _isPressed = false;
                    _heldElapsedSeconds = 0f;
                    ExecuteCommand();
                }

                if (IsHandleCreated && Visible)
                {
                    Invalidate();
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            if (_isInterlocked)
            {
                InterlockBlocked?.Invoke(this, string.IsNullOrEmpty(_interlockReason) ? "Interlock condition active" : _interlockReason);
                return;
            }

            _isPressed = true;
            _heldElapsedSeconds = 0f;

            if (_pressAndHoldSeconds <= 0)
            {
                _isPressed = false;
                if (_requiresConfirmation)
                {
                    var res = MessageBox.Show(
                        $"Confirm operation: '{_commandText}'?",
                        "SCADA Safety Interlock",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (res == DialogResult.Yes)
                    {
                        ExecuteCommand();
                    }
                }
                else
                {
                    ExecuteCommand();
                }
            }

            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_isPressed)
            {
                _isPressed = false;
                _heldElapsedSeconds = 0f;
                Invalidate();
            }
        }

        private void ExecuteCommand()
        {
            if (!string.IsNullOrEmpty(_targetTagPath))
            {
                ZeroTagEngine.SetTagValue(_targetTagPath!, true);
            }

            CommandExecuted?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _isHovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _isHovered = false; _isPressed = false; _heldElapsedSeconds = 0f; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool isDark = ZeroTheme.IsDark;
            Color btnBg, btnBorder, textCol;

            if (_isInterlocked)
            {
                btnBg = isDark ? Color.FromArgb(30, 41, 59) : Color.FromArgb(226, 232, 240);
                btnBorder = isDark ? Color.FromArgb(71, 85, 105) : Color.FromArgb(148, 163, 184);
                textCol = isDark ? Color.FromArgb(100, 116, 139) : Color.FromArgb(148, 163, 184);
            }
            else
            {
                switch (_action)
                {
                    case CommandButtonAction.Stop:
                    case CommandButtonAction.EmergencyStop:
                        btnBg = _isPressed ? Color.FromArgb(185, 28, 28) : (_isHovered ? Color.FromArgb(239, 68, 68) : Color.FromArgb(220, 38, 38));
                        btnBorder = Color.FromArgb(248, 113, 113);
                        break;
                    case CommandButtonAction.Reset:
                        btnBg = _isPressed ? Color.FromArgb(180, 83, 9) : (_isHovered ? Color.FromArgb(245, 158, 11) : Color.FromArgb(217, 119, 6));
                        btnBorder = Color.FromArgb(251, 191, 36);
                        break;
                    default: // Start, Open, Close
                        btnBg = _isPressed ? Color.FromArgb(21, 128, 61) : (_isHovered ? Color.FromArgb(34, 197, 94) : Color.FromArgb(22, 163, 74));
                        btnBorder = Color.FromArgb(74, 222, 128);
                        break;
                }
                textCol = Color.White;
            }

            // 1. Button Rounded Background
            var rect = new RectangleF(2f, 2f, Width - 4f, Height - 4f);
            using (var path = CreateRoundedRectanglePath(rect, 6f))
            using (var brush = new SolidBrush(btnBg))
            using (var pen = new Pen(btnBorder, _isHovered && !_isInterlocked ? 2f : 1.2f))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            // 2. Press-and-Hold Progress Fill (if being held)
            if (_isPressed && _pressAndHoldSeconds > 0 && !_isInterlocked)
            {
                float progress = Math.Min(1f, _heldElapsedSeconds / _pressAndHoldSeconds);
                var progressRect = new RectangleF(rect.X, rect.Y, rect.Width * progress, rect.Height);
                using (var progressBrush = new SolidBrush(Color.FromArgb(80, 255, 255, 255)))
                {
                    g.FillRectangle(progressBrush, progressRect);
                }
            }

            // 3. Button Text & Interlock Icon
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var textBrush = new SolidBrush(textCol))
            {
                string displayText = _isInterlocked ? $"🔒 {_commandText} [LOCKED]" : _commandText;
                g.DrawString(displayText, Font, textBrush, rect, sf);
            }
        }

        private static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
