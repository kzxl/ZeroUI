using System;
using System.Windows.Controls;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Editors
{
    /// <summary>
    /// Modern styled ComboBox adhering to AgentOption WPF UI standards.
    /// Provides dark/light theme aware dropdown popups with high-contrast text and selections.
    /// </summary>
    public class ComboBoxEdit : ComboBox
    {
        public ComboBoxEdit()
        {
            Style = ZeroWpfStyles.ComboBoxStyle;
            ItemContainerStyle = ZeroWpfStyles.ComboBoxItemStyle;
        }
    }

    /// <summary>
    /// Backward-compatibility alias for <see cref="ComboBoxEdit"/>.
    /// </summary>
    [Obsolete("ZeroComboBox is deprecated. Use ComboBoxEdit instead.")]
    public class ZeroComboBox : ComboBoxEdit
    {
    }
}
