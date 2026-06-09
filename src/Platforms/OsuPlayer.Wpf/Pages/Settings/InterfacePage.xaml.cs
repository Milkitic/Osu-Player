using System.Windows;
using System.Windows.Controls;
using OsuPlayer.ViewModels;

namespace OsuPlayer.Pages.Settings;

/// <summary>
/// InterfacePage.xaml 的交互逻辑
/// </summary>
public partial class InterfacePage : Page
{
    public InterfacePage(InterfacePageViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}