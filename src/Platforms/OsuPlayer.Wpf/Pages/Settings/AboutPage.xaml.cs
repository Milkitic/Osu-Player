using System.Windows;
using System.Windows.Controls;
using OsuPlayer.ViewModels;

namespace OsuPlayer.Pages.Settings;

/// <summary>
/// AboutPage.xaml 的交互逻辑
/// </summary>
public partial class AboutPage : Page
{
    public AboutPage(AboutPageViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
