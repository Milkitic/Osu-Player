using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Anotar.NLog;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Models;
using Milki.OsuPlayer.UiComponents.PanelComponent;
using Milki.OsuPlayer.ViewModels;
using Milki.OsuPlayer.Wpf;

namespace Milki.OsuPlayer.Pages;

/// <summary>
/// SearchPage.xaml 的交互逻辑
/// </summary>
public partial class SearchPage : Page
{
    private const int MaxListCount = 100;
    private readonly PlayerService _playerService;
    private readonly SearchPageViewModel _viewModel;

    private readonly Stopwatch _querySw = new();
    private static readonly object QueryLock = new();
    private bool _isQuerying;

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
        await PlayListQueryAsync();
    }

    private async void SearchPage_Loaded(object sender, RoutedEventArgs e)
    {
        await PlayListQueryAsync();
    }

    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.SearchText = ((TextBox)sender).Text;
        await PlayListQueryAsync();
    }

    private VirtualizingGalleryWrapPanel _virtualizingGalleryWrapPanel;

    private void Panel_Loaded(object sender, RoutedEventArgs e)
    {
        _virtualizingGalleryWrapPanel = sender as VirtualizingGalleryWrapPanel;
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

    private async Task PlayListQueryAsync(int page = 0)
    {
        var sortMode = BeatmapOrderOptions.Artist;
        _querySw.Restart();

        lock (QueryLock)
        {
            if (_isQuerying)
                return;
            _isQuerying = true;
        }

        try
        {
            await Task.Run(() =>
            {
                while (_querySw.ElapsedMilliseconds < 300)
                    Thread.Sleep(10);
                _querySw.Stop();
            });

            await using var dbContext = ServiceProviders.GetApplicationDbContext();
            var paginationQueryResult = await dbContext
                .SearchPlayItemsAsync(_viewModel.SearchText ?? "",
                    sortMode,
                    page: page,
                    countPerPage: MaxListCount
                );

            _viewModel.PlayItems = new ObservableCollection<PlayGroupQuery>(paginationQueryResult.Results);
            SetPage(paginationQueryResult.TotalCount, page);
        }
        finally
        {
            lock (QueryLock)
            {
                _isQuerying = false;
            }
        }
    }

    private void SetPage(int totalCount, int nowPage)
    {
        var totalPageCount = (int)Math.Ceiling(totalCount / (float)MaxListCount);
        int count, startIndex;
        if (totalPageCount > 10)
        {
            if (nowPage > 5)
            {
                _viewModel.FirstPage = new ListPageViewModel(1);
                if (nowPage >= totalPageCount - 5)
                {
                    startIndex = totalPageCount - 10;
                }
                else
                {
                    startIndex = nowPage - 5;
                }
            }
            else
            {
                startIndex = 0;
            }

            count = 10;
        }
        else
        {
            count = totalPageCount;
            startIndex = 0;
        }

        var pages = new List<ListPageViewModel>(totalPageCount);
        for (int i = startIndex; i < startIndex + count; i++)
        {
            pages.Add(new ListPageViewModel(i + 1));
        }

        _viewModel.Pages = pages;
        ListPageViewModel page = GetPage(nowPage + 1);

        if (page != null)
            page.IsActivated = true;

        _viewModel.CurrentPage = page;
        _virtualizingGalleryWrapPanel?.ClearNotificationCount();

        //DisplayedMaps = SearchedMaps.Skip(nowIndex * MaxListCount).Take(MaxListCount).ToList();
    }

    private ListPageViewModel GetPage(int page)
    {
        return _viewModel.Pages.FirstOrDefault(k => k.Index == page);
    }
}