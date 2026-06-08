using System.Windows;
using System.Windows.Controls;
using OsuPlayer.ViewModels;

namespace OsuPlayer.Pages.Settings;

/// <summary>
/// GeneralPage.xaml 的交互逻辑
/// </summary>
public partial class GeneralPage : Page
{
    public GeneralPage(GeneralPageViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}