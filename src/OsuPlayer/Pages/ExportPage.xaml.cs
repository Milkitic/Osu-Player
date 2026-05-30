using System.Windows;
using System.Windows.Controls;
using Milky.OsuPlayer.Core.Configuration;
using Milky.OsuPlayer.ViewModels;

namespace Milky.OsuPlayer.Pages;

/// <summary>
/// ExportPage.xaml 的交互逻辑
/// </summary>
public partial class ExportPage : Page
{
    public ExportPageViewModel ViewModel { get; }

    public ExportPage(ExportPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = ViewModel;
        ViewModel.ExportPath = AppSettings.Default.Export.MusicPath;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.UpdateListAsync();
    }
}