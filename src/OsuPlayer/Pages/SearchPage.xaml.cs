using System.Windows;
using System.Windows.Controls;
using Anotar.NLog;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Audio;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Models;
using Milki.OsuPlayer.UiComponents.PanelComponent;
using Milki.OsuPlayer.Utils;
using Milki.OsuPlayer.ViewModels;
using Milki.OsuPlayer.Wpf;

namespace Milki.OsuPlayer.Pages;

/// <summary>
/// SearchPage.xaml 的交互逻辑
/// </summary>
public partial class SearchPage : Page
{
    private readonly PlayerService _playerService;
    private readonly PlayListService _playListService;
    private readonly SearchPageViewModel _viewModel;
    private readonly InputDelayQueryHelper _inputDelayQueryHelper;

    public SearchPage()
    {
        DataContext = _viewModel = new SearchPageViewModel();
        _playerService = App.Current.ServiceProvider.GetService<PlayerService>();
        _playListService = App.Current.ServiceProvider.GetService<PlayListService>();

        _inputDelayQueryHelper = new InputDelayQueryHelper() { QueryAsync = PlayListQueryAsync };
        InitializeComponent();
    }

    public SearchPage Search(string keyword)
    {
        SearchBox.Text = keyword;
        return this;
    }

    private async ValueTask PlayListQueryAsync(int page = 0)
    {
        var sortMode = BeatmapOrderOptions.Artist;
        await using var dbContext = ServiceProviders.GetApplicationDbContext();
        var paginationQueryResult = await dbContext
            .SearchPlayItemsAsync(_viewModel.SearchText ?? "",
                sortMode,
                page: page,
                countPerPage: Pagination.ItemsCount
            );
        _viewModel.PlayItems.Clear();
        foreach (var playGroupQuery in paginationQueryResult.Results)
        {
            _viewModel.PlayItems.Add(playGroupQuery);
        }

        Pagination.CurrentPageIndex = page < 1 ? 0 : page - 1;
        Pagination.TotalCount = paginationQueryResult.TotalCount;

    }

    private async void SearchPage_Initialized(object sender, EventArgs e)
    {
        await PlayListQueryAsync();
    }

    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.SearchText = SearchBox.Text;
        await _inputDelayQueryHelper.StartDelayedQuery();
    }

    private async void BtnPlayDefault_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: PlayGroupQuery playGroupQuery }) return;

        var defaultItem = playGroupQuery.PlayItem;
        await _playerService.InitializeNewAsync(defaultItem.StandardizedPath, true);
    }

    private void BtnPlayAll_Click(object sender, RoutedEventArgs e)
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

    private async void Pagination_OnPageSelected(int page)
    {
        await PlayListQueryAsync(page);
    }

    private async void VirtualizingGalleryWrapPanel_OnItemLoaded(object sender, VirtualizingGalleryRoutedEventArgs e)
    {
        var groupQuery = _viewModel.PlayItems[e.Index];
        var playItem = groupQuery.PlayItem;
        try
        {
            var fileName = await CommonUtils.GetThumbByBeatmapDbId(playItem).ConfigureAwait(false);
            Execute.OnUiThread(() =>
            {
                playItem.PlayItemAsset!.FullThumbPath = fileName;
                groupQuery.ThumbPath = fileName;
            });
        }
        catch (Exception ex)
        {
            LogTo.ErrorException("Error while loading panel item.", ex);
        }

        LogTo.Debug(() => $"VirtualizingGalleryWrapPanel: {e.Index}");
    }
}