using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ZeroUI.Core.Notification;

namespace ZeroUI.WinForms.Overlays
{
    /// <summary>
    /// Bridges ZeroUI notifications to the native Windows OS Notification subsystem
    /// (Windows 10/11 Action Center and System Tray Notifications).
    /// Enables external alerts when the application is minimized, hidden, or running in background.
    /// </summary>
    public static class WindowsNotificationBridge
    {
        private static readonly object _syncLock = new object();
        private static NotifyIcon? _notifyIcon;
        private static Action? _pendingClickAction;
        private static Form? _registeredMainForm;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        /// <summary>
        /// Registers the primary application form for auto-restoration and focus when a notification is clicked.
        /// </summary>
        public static void RegisterMainForm(Form form)
        {
            _registeredMainForm = form;
        }

        private static void EnsureNotifyIconInitialized()
        {
            if (_notifyIcon != null) return;

            lock (_syncLock)
            {
                if (_notifyIcon != null) return;

                _notifyIcon = new NotifyIcon
                {
                    Icon = _registeredMainForm?.Icon ?? SystemIcons.Application,
                    Text = _registeredMainForm?.Text ?? "ZeroUI Enterprise Suite",
                    Visible = true
                };

                _notifyIcon.BalloonTipClicked += OnBalloonTipClicked;
                Application.ApplicationExit += (s, e) => Shutdown();
            }
        }

        private static void OnBalloonTipClicked(object? sender, EventArgs e)
        {
            try
            {
                // 1. Restore and bring registered form to foreground
                if (_registeredMainForm != null && !_registeredMainForm.IsDisposed)
                {
                    if (_registeredMainForm.InvokeRequired)
                    {
                        _registeredMainForm.BeginInvoke(new Action(() => RestoreForm(_registeredMainForm)));
                    }
                    else
                    {
                        RestoreForm(_registeredMainForm);
                    }
                }

                // 2. Invoke callback
                var action = _pendingClickAction;
                _pendingClickAction = null;
                action?.Invoke();
            }
            catch
            {
                // Non-critical notification activation
            }
        }

        private static void RestoreForm(Form form)
        {
            if (form.WindowState == FormWindowState.Minimized)
            {
                ShowWindow(form.Handle, SW_RESTORE);
            }
            form.Activate();
            form.BringToFront();
            SetForegroundWindow(form.Handle);
        }

        /// <summary>
        /// Dispatches a notification to the native Windows OS Action Center / System Tray.
        /// </summary>
        /// <param name="title">Notification title caption.</param>
        /// <param name="message">Notification body text.</param>
        /// <param name="type">Severity/Icon type.</param>
        /// <param name="timeoutMs">Display duration in milliseconds.</param>
        /// <param name="onClick">Action invoked when the operator clicks the notification banner.</param>
        public static void ShowNotification(
            string title,
            string message,
            ZeroToastType type = ZeroToastType.Info,
            int timeoutMs = 4000,
            Action? onClick = null)
        {
            try
            {
                EnsureNotifyIconInitialized();
                if (_notifyIcon == null) return;

                _pendingClickAction = onClick;

                ToolTipIcon tipIcon = type switch
                {
                    ZeroToastType.Success => ToolTipIcon.Info,
                    ZeroToastType.Info => ToolTipIcon.Info,
                    ZeroToastType.Warning => ToolTipIcon.Warning,
                    ZeroToastType.Error => ToolTipIcon.Error,
                    ZeroToastType.Alarm => ToolTipIcon.Error,
                    _ => ToolTipIcon.Info
                };

                string displayTitle = string.IsNullOrEmpty(title) ? "ZeroUI Notification" : title;
                _notifyIcon.ShowBalloonTip(timeoutMs, displayTitle, message, tipIcon);
            }
            catch
            {
                // Fallback graceful degradation
            }
        }

        /// <summary>
        /// Releases system tray resources and cleans up OS notification hooks.
        /// </summary>
        public static void Shutdown()
        {
            lock (_syncLock)
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                }
            }
        }
    }
}
