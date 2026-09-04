using System.Windows.Controls;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Editors
{
    /// <summary>
    /// Modern styled ComboBox adhering to AgentOption WPF UI standards.
    /// Provides dark/light theme aware dropdown popups with high-contrast text and selections.
    /// </summary>
    public class ZeroComboBox : ComboBox
    {
        public ZeroComboBox()
        {
            Style = ZeroWpfStyles.ComboBoxStyle;
            ItemContainerStyle = ZeroWpfStyles.ComboBoxItemStyle;
        }
    }
}
