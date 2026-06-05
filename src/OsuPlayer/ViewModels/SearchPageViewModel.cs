using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OsuPlayer.Core;
using OsuPlayer.Core.Services;
using OsuPlayer.Data.Models;
using OsuPlayer.Media.Audio;
using OsuPlayer.Presentation.Interaction;
using OsuPlayer.Services;
using OsuPlayer.Shared.Models;
using OsuPlayer.UiComponents.PanelComponent;

namespace OsuPlayer.ViewModels;

public partial class SearchPageViewModel : ObservableObject
{
    private readonly IPlayerDataService _playerData;
    private readonly ObservablePlayController _controller;
    private readonly IBeatmapActionService _beatmapActions;

    private const int MaxListCount = 250;
    private const int QueryDelayMs = 167;

    private CancellationTokenSource _queryCancellation;
    private int _queryVersion;

    public SearchPageViewModel(
        IPlayerDataService playerData,
        ObservablePlayController controller,
        IBeatmapActionService beatmapActions)
    {
        _playerData = playerData;
        _controller = controller;
        _beatmapActions = beatmapActions;
    }

    [ObservableProperty]
    public partial List<BeatmapDataModel> DisplayedMaps { get; private set; } = [];

    [ObservableProperty]
    public partial bool IsMinimalMode { get; set; }

    [ObservableProperty]
    public partial List<ListPageViewModel> Pages { get; private set; } = [];

    [ObservableProperty]
    public partial ListPageViewModel CurrentPage { get; private set; }

    public VirtualizingGalleryWrapPanel GalleryWrapPanel { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; }

    partial void OnSearchTextChanged(string value)
    {
        _ = PlayListQueryAsync(0);
    }

    // Stores the currently displayed page results so existing page actions can reuse them.
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
            GalleryWrapPanel?.ClearNotificationCount();
            DisplayedMaps = SearchedDbMaps.ToDataModelList(true);
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

        int count, startIndex;
        if (totalCount > 10)
        {
            if (nowIndex > 5)
            {
                if (nowIndex >= totalCount - 5)
                {
                    startIndex = totalCount - 10;
                }
                else
                {
                    startIndex = nowIndex - 5;
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
            count = totalCount;
            startIndex = 0;
        }

        var pages = new List<ListPageViewModel>(totalCount);
        for (int i = startIndex; i < startIndex + count; i++)
        {
            pages.Add(new ListPageViewModel(i + 1));
        }

        Pages = pages;
        ListPageViewModel page = GetPage(nowIndex + 1);

        if (page != null)
            page.IsActivated = true;

        CurrentPage = page;
    }

    private ListPageViewModel GetPage(int page)
    {
        return Pages.FirstOrDefault(k => k.Index == page);
    }

    [RelayCommand]
    private async Task SwitchAsync(object obj)
    {
        if (obj is bool b)
        {
            if (CurrentPage == null) return;
            var page = b ? GetPage(CurrentPage.Index + 1) : GetPage(CurrentPage.Index - 1);
            if (page == null) return;
            if (page.IsActivated)
            {
                return;
            }

            await PlayListQueryAsync(page.Index - 1, false);
        }
        else
        {
            var reqPage = (int)obj;
            var page = GetPage(reqPage);
            if (page == null) return;
            if (page.IsActivated)
            {
                return;
            }

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
        var group = beatmaps.GroupBy(k => k.FolderName);
        var newBeatmaps = group
            .Select(k => k.GetHighestDiff())
            .ToList();

        await _controller.SetPlaylistAsync(newBeatmaps, true);
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

[MarkupExtensionReturnType(typeof(ContentControl))]
public class RootObject : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var rootObjectProvider = (IRootObjectProvider)serviceProvider.GetService(typeof(IRootObjectProvider));
        return rootObjectProvider?.RootObject;
    }
}
