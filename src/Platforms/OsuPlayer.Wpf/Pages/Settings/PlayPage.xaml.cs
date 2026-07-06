using System.Windows;
using System.Windows.Controls;
using OsuPlayer.ViewModels;

namespace OsuPlayer.Pages.Settings;

/// <summary>
/// PlayPage.xaml 的交互逻辑
/// </summary>
public partial class PlayPage : Page
{
    public PlayPage(PlayPageViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}