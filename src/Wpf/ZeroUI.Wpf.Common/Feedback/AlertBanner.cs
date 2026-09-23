using System;

namespace ZeroUI.Wpf.Feedback
{
    public enum ZeroAlertSeverity
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Error = 3
    }

    /// <summary>
    /// Modern dismissible alert banner for factory floor stoppage, defect alarms, and system broadcasts.
    /// Provides 100% parity with WinForms AlertBanner control.
    /// </summary>
    public class AlertBanner : InfoBar
    {
        public ZeroAlertSeverity AlertSeverity
        {
            get => (ZeroAlertSeverity)(int)Severity;
            set => Severity = (InfoBarSeverity)(int)value;
        }
    }

    /// <summary>
    /// Legacy alias for <see cref="AlertBanner"/>.
    /// </summary>
    [Obsolete("ZeroAlertBanner is deprecated. Please use AlertBanner instead.")]
    public class ZeroAlertBanner : AlertBanner { }
}
