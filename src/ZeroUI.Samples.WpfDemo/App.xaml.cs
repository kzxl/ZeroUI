using System.Windows;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Samples.WpfDemo
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ZeroThemeEngine.Initialize(this);
        }
    }
}
