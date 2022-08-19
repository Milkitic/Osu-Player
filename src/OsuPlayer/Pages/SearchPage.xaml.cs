using System.Windows;
using System.Windows.Controls;
using Anotar.NLog;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.UiComponents.PanelComponent;
using Milki.OsuPlayer.ViewModels;
using Milki.OsuPlayer.Wpf;

namespace Milki.OsuPlayer.Pages;

/// <summary>
/// SearchPage.xaml 的交互逻辑
/// </summary>
public partial class SearchPage : Page
{
    private readonly PlayerService _playerService;
    private readonly SearchPageViewModel _viewModel;

    public SearchPage()
    {
        DataContext = _viewModel = new SearchPageViewModel();
        _playerService = App.Current.ServiceProvider.GetService<PlayerService>();
        InitializeComponent();
    }

    public SearchPage Search(string keyword)
    {
        SearchBox.Text = keyword;
        return this;
    }

    private async void SearchPage_Initialized(object sender, EventArgs e)
    {
        await _viewModel.PlayListQueryAsync();
    }

    private async void SearchPage_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.PlayListQueryAsync();
    }

    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.SearchText = ((TextBox)sender).Text;
        await _viewModel.PlayListQueryAsync();
    }

    private VirtualizingGalleryWrapPanel _virtualizingGalleryWrapPanel;

    private void Panel_Loaded(object sender, RoutedEventArgs e)
    {
        _virtualizingGalleryWrapPanel = sender as VirtualizingGalleryWrapPanel;
        _viewModel.GalleryWrapPanel = _virtualizingGalleryWrapPanel;
    }

    private async void BtnPlayAll_Click(object sender, RoutedEventArgs e)
    {
        //List<Beatmap> beatmaps = _viewModel.PlayItems.Select(k => k.Model).ToList();
        //if (beatmaps.Count <= 0) return;
        //var group = beatmaps.GroupBy(k => k.FolderNameOrPath);
        //List<Beatmap> newBeatmaps = group
        //    .Select(sb => sb.GetHighestDiff())
        //    .ToList();

        ////if (map == null) return;
        ////await _mainWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, map.FolderName,
        ////     map.BeatmapFileName));
        //await _playerService.PlayList.SetSongListAsync(newBeatmaps, true);
    }

    private void BtnQueueAll_Click(object sender, RoutedEventArgs e)
    {
    }

    private async void VirtualizingGalleryWrapPanel_OnItemLoaded(object sender, VirtualizingGalleryRoutedEventArgs e)
    {
        var playItem = _viewModel.PlayItems[e.Index].PlayItem;
        try
        {
            var fileName = await CommonUtils.GetThumbByBeatmapDbId(playItem).ConfigureAwait(false);
            Execute.OnUiThread(() => playItem.PlayItemAsset!.FullThumbPath = fileName);
        }
        catch (Exception ex)
        {
            LogTo.ErrorException("Error while loading panel item.", ex);
        }

        LogTo.Debug(() => $"VirtualizingGalleryWrapPanel: {e.Index}");
    }
}