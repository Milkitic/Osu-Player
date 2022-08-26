using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Audio;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Models;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.Utils;

namespace Milki.OsuPlayer.Pages;

public class SearchPageVm : VmBase
{
    private string _searchText;

    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    //public ObservableCollection<PlayGroupQuery> PlayGroupQueries { get; set; } = new();
}

/// <summary>
/// SearchPage.xaml 的交互逻辑
/// </summary>
public partial class SearchPage : Page
{
    private readonly PlayerService _playerService;
    private readonly PlayListService _playListService;
    private readonly SearchPageVm _viewModel;
    private readonly InputDelayQueryHelper _inputDelayQueryHelper;
    private BeatmapOrderOptions _sortMode = BeatmapOrderOptions.Artist;
    private bool _isFirstLoaded;

    public SearchPage()
    {
        DataContext = _viewModel = new SearchPageVm();
        _playerService = App.Current.ServiceProvider.GetService<PlayerService>();
        _playListService = App.Current.ServiceProvider.GetService<PlayListService>();

        _inputDelayQueryHelper = new InputDelayQueryHelper { QueryAsync = PlayListQueryAsync };
        InitializeComponent();
    }

    public SearchPage Search(string keyword)
    {
        SearchBox.Text = keyword;
        return this;
    }

    private async ValueTask PlayListQueryAsync(int page = 0)
    {
        await using var dbContext = ServiceProviders.GetApplicationDbContext();
        var paginationQueryResult = await dbContext
            .SearchPlayItemsAsync(_viewModel.SearchText ?? "",
                _sortMode,
                page: page,
                countPerPage: Pagination.ItemsCount
            );
        //sb.PlayItems = _viewModel.PlayGroupQueries;
        //_viewModel.PlayGroupQueries.Clear();
        //foreach (var playGroupQuery in paginationQueryResult.Results)
        //{
        //    _viewModel.PlayGroupQueries.Add(playGroupQuery);
        //}
        sb.PlayItems = paginationQueryResult.Results;
        Pagination.CurrentPageIndex = page < 1 ? 0 : page - 1;
        Pagination.TotalCount = paginationQueryResult.TotalCount;

    }

    private void SearchPage_Initialized(object sender, EventArgs e)
    {
    }

    private async void SearchPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isFirstLoaded) return;
        _isFirstLoaded = true;

        await Task.Delay(100);
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

        var defaultItem = playGroupQuery.CurrentPlayItem;
        await _playerService.InitializeNewAsync(defaultItem.StandardizedPath, true);
    }

    private async void BtnPlayAll_Click(object sender, RoutedEventArgs e)
    {
        await using var dbContext = ServiceProviders.GetApplicationDbContext();
        var paginationQueryResult = await dbContext
            .SearchPlayItemsAsync(_viewModel.SearchText ?? "",
                _sortMode,
                page: 0,
                countPerPage: int.MaxValue
            );
        var allPlayItems = paginationQueryResult.Results.Select(k => k.CurrentPlayItem).ToArray();
        if (allPlayItems.Length == 0) return;
        await dbContext.RecreateCurrentPlayAsync(allPlayItems);
        _playListService.SetPathList(allPlayItems.Select(k => k.StandardizedPath), false);
        await _playerService.InitializeNewAsync(allPlayItems.First().StandardizedPath, true);
    }

    private async void BtnQueueAll_Click(object sender, RoutedEventArgs e)
    {
        await using var dbContext = ServiceProviders.GetApplicationDbContext();
        var paginationQueryResult = await dbContext
            .SearchPlayItemsAsync(_viewModel.SearchText ?? "",
                _sortMode,
                page: 0,
                countPerPage: int.MaxValue
            );
        var allPlayItems = paginationQueryResult.Results.Select(k => k.CurrentPlayItem).ToArray();
        if (allPlayItems.Length == 0) return;
        await dbContext.RecreateCurrentPlayAsync(allPlayItems);
        _playListService.SetPathList(allPlayItems.Select(k => k.StandardizedPath), false);
    }

    private async void Pagination_OnPageSelected(int page)
    {
        await PlayListQueryAsync(page);
    }

    //private async void VirtualizingGalleryWrapPanel_OnItemLoaded(object sender, VirtualizingGalleryRoutedEventArgs e)
    //{
    //    var groupQuery = _viewModel.PlayGroupQueries[e.Index];
    //    var playItem = groupQuery.CurrentPlayItem;
    //    try
    //    {
    //        var fileName = await CommonUtils.GetThumbByBeatmapDbId(playItem).ConfigureAwait(false);
    //        Execute.OnUiThread(() =>
    //        {
    //            playItem.PlayItemAsset!.FullThumbPath = fileName;
    //            groupQuery.ThumbPath = fileName;
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        LogTo.ErrorException("Error while loading panel item.", ex);
    //    }

    //    LogTo.Debug(() => $"VirtualizingGalleryWrapPanel: {e.Index}");
    //}
}