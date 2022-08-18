using Milki.OsuPlayer.Wpf;

namespace Milki.OsuPlayer.Windows;

/// <summary>
/// ConfigWindow.xaml 的交互逻辑
/// </summary>
public partial class ConfigWindow : WindowEx
{
    public ConfigWindow()
    {
        InitializeComponent();
    }

    private void Window_Shown(object sender, System.EventArgs e)
    {
        SwitchGeneral.IsChecked = true;
    }
}