using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Milky.OsuPlayer.Core;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;

namespace Milky.OsuPlayer.Pages;

/// <summary>
/// RecentPlayPage.xaml 的交互逻辑
/// </summary>
public partial class RecentPlayPage : Page
{
    private readonly ObservablePlayController _controller;
    private readonly RecentPlayPageViewModel _viewModel;
    private readonly MainWindow _mainWindow;

    public RecentPlayPage(RecentPlayPageViewModel viewModel, ObservablePlayController controller)
    {
        _viewModel = viewModel;
        _controller = controller;

        InitializeComponent();
        _mainWindow = (MainWindow)Application.Current.MainWindow;
        DataContext = _viewModel;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.UpdateListAsync();
        var item = _viewModel.Beatmaps.FirstOrDefault(k =>
            k.GetIdentity().Equals(_controller.PlayList.CurrentInfo?.Beatmap?.GetIdentity()));
        RecentList.SelectedItem = item;
    }

    private async void BtnDelAll_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(_mainWindow, I18NUtil.GetString("ui-ensureRemoveAll"), _mainWindow.Title,
            MessageBoxButton.OKCancel,
            MessageBoxImage.Exclamation);
        if (result == MessageBoxResult.OK)
        {
            await _viewModel.ClearAllRecentAsync();
        }
    }

    private async void RecentListItem_MouseDoubleClick(object sender, RoutedEventArgs e)
    {
        if (RecentList.SelectedItem is BeatmapDataModel map)
        {
            await _viewModel.PlayAsync(map);
        }
    }
}