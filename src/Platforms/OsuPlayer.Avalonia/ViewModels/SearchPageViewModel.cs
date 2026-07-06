using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OsuPlayer.Core;
using OsuPlayer.Core.Services;
using OsuPlayer.Data.Models;
using OsuPlayer.Playback;
using OsuPlayer.Presentation.Interaction;
using OsuPlayer.Services;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.ViewModels;

public partial class SearchPageViewModel : ObservableObject, INavigationAware
{
    private readonly IPlayerDataService _playerData;
    private readonly ObservablePlayController _controller;
    private readonly IBeatmapActionService _beatmapActions;
    private readonly IMapModelConverter _mapModelConverter;

    private const int MaxListCount = 250;
    private const int QueryDelayMs = 167;

    private CancellationTokenSource? _queryCancellation;
    private int _queryVersion;

    public SearchPageViewModel(
        IPlayerDataService playerData,
        ObservablePlayController controller,
        IBeatmapActionService beatmapActions,
        IMapModelConverter mapModelConverter)
    {
        _playerData = playerData;
        _controller = controller;
        _beatmapActions = beatmapActions;
        _mapModelConverter = mapModelConverter;
    }

    [ObservableProperty]
    public partial List<BeatmapDataModel> DisplayedMaps { get; private set; } = [];

    [ObservableProperty]
    public partial bool IsMinimalMode { get; set; }

    [ObservableProperty]
    public partial List<ListPageViewModel> Pages { get; private set; } = [];

    [ObservableProperty]
    public partial ListPageViewModel? CurrentPage { get; private set; }

    public Action? ClearGalleryNotifications { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    partial void OnSearchTextChanged(string value)
    {
        _ = PlayListQueryAsync(0);
    }

    public void OnNavigatedTo(object? parameter)
    {
        if (parameter is SearchNavigationParameter search)
        {
            SearchText = search.Keyword;
        }
    }

    [ObservableProperty]
    public partial List<Beatmap> SearchedDbMaps { get; private set; } = [];

    public async Task PlayListQueryAsync(int pageIndex = 0, bool debounce = true)
    {
        var normalizedPageIndex = Math.Max(0, pageIndex);
        var requestVersion = Interlocked.Increment(ref _queryVersion);
        var cancellation = BeginQuery();

        try
        {
            if (debounce)
            {
                await Task.Delay(QueryDelayMs, cancellation.Token);
            }

            var result = await _playerData.SearchBeatmapPageAsync(SearchText, BeatmapSortMode.Artist,
                normalizedPageIndex * MaxListCount, MaxListCount);
            if (cancellation.IsCancellationRequested || requestVersion != _queryVersion)
            {
                return;
            }

            SearchedDbMaps = result.Results.ToList();
            ClearGalleryNotifications?.Invoke();
            DisplayedMaps = _mapModelConverter.ToDataModelList(SearchedDbMaps, true);
            SetPage(result.TotalCount, normalizedPageIndex);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            EndQuery(cancellation);
        }
    }

    public Task<List<Beatmap>> GetAllMatchedBeatmapsAsync()
    {
        return _playerData.SearchBeatmapByOptionsAsync(SearchText, BeatmapSortMode.Artist, 0, int.MaxValue);
    }

    private void SetPage(int totalCount, int nowIndex)
    {
        totalCount = (int)Math.Ceiling(totalCount / (float)MaxListCount);
        if (totalCount <= 0)
        {
            Pages = [];
            CurrentPage = null;
            return;
        }

        int count;
        int startIndex;
        if (totalCount > 10)
        {
            if (nowIndex > 5)
            {
                startIndex = nowIndex >= totalCount - 5 ? totalCount - 10 : nowIndex - 5;
            }
            else
            {
                startIndex = 0;
            }

            count = 10;
        }
        else
        {
            count = totalCount;
            startIndex = 0;
        }

        var pages = new List<ListPageViewModel>(count);
        for (var i = startIndex; i < startIndex + count; i++)
        {
            pages.Add(new ListPageViewModel(i + 1));
        }

        Pages = pages;
        var page = GetPage(nowIndex + 1);
        if (page != null)
        {
            page.IsActivated = true;
        }

        CurrentPage = page;
    }

    private ListPageViewModel? GetPage(int page)
    {
        return Pages.FirstOrDefault(k => k.Index == page);
    }

    [RelayCommand]
    private async Task SwitchAsync(object? obj)
    {
        if (obj is bool direction)
        {
            if (CurrentPage == null) return;
            var page = direction ? GetPage(CurrentPage.Index + 1) : GetPage(CurrentPage.Index - 1);
            if (page == null || page.IsActivated) return;
            await PlayListQueryAsync(page.Index - 1, false);
            return;
        }

        if (obj is int reqPage)
        {
            var page = GetPage(reqPage);
            if (page == null || page.IsActivated) return;
            await PlayListQueryAsync(reqPage - 1, false);
        }
    }

    [RelayCommand]
    private void SearchByCondition(string param)
    {
        WeakReferenceMessenger.Default.Send(new SearchRequestedMessage(param));
    }

    [RelayCommand]
    private async Task OpenSourceFolderAsync(BeatmapDataModel beatmap)
    {
        await _beatmapActions.OpenSourceFolderAsync(beatmap, highestDifficulty: true);
    }

    [RelayCommand]
    private async Task OpenScorePageAsync(BeatmapDataModel beatmap)
    {
        await _beatmapActions.OpenScorePageAsync(beatmap, highestDifficulty: true);
    }

    [RelayCommand]
    private async Task SaveCollectionAsync(BeatmapDataModel beatmap)
    {
        await _beatmapActions.SaveToCollectionWithDifficultyPickerAsync(beatmap);
    }

    [RelayCommand]
    private async Task ExportAsync(BeatmapDataModel beatmap)
    {
        await _beatmapActions.ExportAsync(beatmap, highestDifficulty: true);
    }

    [RelayCommand]
    public async Task DirectPlayAsync(BeatmapDataModel beatmap)
    {
        await _beatmapActions.PlayAsync(beatmap, highestDifficulty: true);
    }

    [RelayCommand]
    private async Task PlayAllAsync()
    {
        var beatmaps = await GetAllMatchedBeatmapsAsync();
        if (beatmaps.Count <= 0) return;

        var newBeatmaps = beatmaps
            .GroupBy(k => k.FolderName)
            .Select(k => k.GetHighestDiff())
            .Where(k => k != null)
            .Cast<Beatmap>()
            .ToList();

        await _controller.SetPlaylistAsync(newBeatmaps, true);
    }

    [RelayCommand]
    private async Task QueueAllAsync()
    {
        var beatmaps = await GetAllMatchedBeatmapsAsync();
        if (beatmaps.Count <= 0) return;

        var newBeatmaps = beatmaps
            .GroupBy(k => k.FolderName)
            .Select(k => k.GetHighestDiff())
            .Where(k => k != null)
            .Cast<Beatmap>()
            .ToList();

        await _controller.SetPlaylistAsync(newBeatmaps, true, playInstantly: false, autoLoad: false);
    }

    [RelayCommand]
    private async Task PlayAsync(BeatmapDataModel beatmap)
    {
        await _beatmapActions.PlayWithDifficultyPickerAsync(beatmap);
    }

    private CancellationTokenSource BeginQuery()
    {
        var next = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _queryCancellation, next);
        if (previous != null)
        {
            previous.Cancel();
            previous.Dispose();
        }

        return next;
    }

    private void EndQuery(CancellationTokenSource cancellation)
    {
        if (Interlocked.CompareExchange(ref _queryCancellation, null, cancellation) == cancellation)
        {
            cancellation.Dispose();
        }
    }
}
