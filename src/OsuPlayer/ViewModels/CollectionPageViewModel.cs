using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Milky.OsuPlayer.Core;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Presentation.ObjectModel;
using Milky.OsuPlayer.Services;

namespace Milky.OsuPlayer.ViewModels;

public partial class CollectionPageViewModel : ObservableObject, INavigationAware
{
    private readonly ObservablePlayController _controller;
    private readonly IPlayerDataService _playerData;
    private readonly IExportService _exportService;
    private readonly IBeatmapActionService _beatmapActions;

    public CollectionPageViewModel(IPlayerDataService playerData, ObservablePlayController controller,
        IExportService exportService,
        IBeatmapActionService beatmapActions)
    {
        _playerData = playerData;
        _controller = controller;
        _exportService = exportService;
        _beatmapActions = beatmapActions;
    }

    public IPlayerDataService PlayerData => _playerData;

    [ObservableProperty]
    public partial NumberableObservableCollection<BeatmapDataModel> Beatmaps { get; set; }

    [ObservableProperty]
    public partial NumberableObservableCollection<BeatmapDataModel> DisplayedBeatmaps { get; set; }

    [ObservableProperty]
    public partial Collection CollectionInfo { get; set; }

    [ObservableProperty]
    public partial bool IsMinimalMode { get; set; }

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value)
    {
        var keyword = value.Trim();
        DisplayedBeatmaps = string.IsNullOrWhiteSpace(keyword)
            ? Beatmaps
            : new NumberableObservableCollection<BeatmapDataModel>(Beatmaps.GetByKeyword(keyword));
    }

    public IEnumerable<Beatmap> Entries { get; private set; }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is string colId)
        {
            _ = UpdateView(colId);
        }
    }

    public async Task UpdateView(string colId)
    {
        var collectionInfo = await _playerData.GetCollectionByIdAsync(colId);
        if (collectionInfo == null) return;
        CollectionInfo = collectionInfo;
        await UpdateListAsync();
    }

    public async Task UpdateListAsync()
    {
        var infos = await _playerData.GetMapsFromCollectionAsync(CollectionInfo);
        Entries = await _playerData.GetBeatmapsByMapInfoAsync(infos, TimeSortMode.AddTime);
        Execute.OnUiThread(() =>
        {
            Beatmaps = new NumberableObservableCollection<BeatmapDataModel>(Entries.ToDataModelList(false));
            DisplayedBeatmaps = Beatmaps;
        });
    }

    [RelayCommand]
    private void SearchByCondition(string param)
    {
        WeakReferenceMessenger.Default.Send(new SearchRequestedMessage(param));
    }

    [RelayCommand]
    private async Task OpenSourceFolderAsync(BeatmapDataModel beatmap)
    {
        await _beatmapActions.OpenSourceFolderAsync(beatmap);
    }

    [RelayCommand]
    private async Task OpenScorePageAsync(BeatmapDataModel beatmap)
    {
        await _beatmapActions.OpenScorePageAsync(beatmap);
    }

    [RelayCommand]
    private async Task SaveCollectionAsync(BeatmapDataModel beatmap)
    {
        await _beatmapActions.SaveToCollectionAsync(beatmap);
    }

    [RelayCommand]
    private async Task ExportAsync(BeatmapDataModel beatmap)
    {
        await _beatmapActions.ExportAsync(beatmap);
    }

    [RelayCommand]
    private void ExportAll()
    {
        if (Entries == null) return;
        _exportService.QueueEntries(Entries);
    }

    [RelayCommand]
    public async Task DirectPlayAsync(BeatmapDataModel beatmap)
    {
        await _beatmapActions.PlayAsync(beatmap);
    }

    [RelayCommand]
    private async Task PlayAsync(BeatmapDataModel beatmap)
    {
        await _beatmapActions.PlayAsync(beatmap);
    }

    [RelayCommand]
    private async Task RemoveAsync(BeatmapDataModel beatmap)
    {
        if (beatmap == null) return;
        if (!await _playerData.TryRemoveMapFromCollectionAsync(beatmap.GetIdentity(), CollectionInfo))
            return;
        if (_controller.PlayList.CurrentInfo?.Beatmap?.GetIdentity().Equals(beatmap.GetIdentity()) == true &&
            CollectionInfo.LockedBool)
        {
            _controller.PlayList.CurrentInfo.BeatmapDetail.Metadata.IsFavorite = false;
        }

        Beatmaps.Remove(beatmap);
        DisplayedBeatmaps.Remove(beatmap);
    }

    [RelayCommand]
    private async Task RemoveSelectedAsync(System.Collections.IList selectedItems)
    {
        if (selectedItems == null || selectedItems.Count == 0) return;
        var itemsToRemove = selectedItems.Cast<BeatmapDataModel>().ToList();
        foreach (var beatmap in itemsToRemove)
        {
            if (!await _playerData.TryRemoveMapFromCollectionAsync(beatmap.GetIdentity(), CollectionInfo))
                continue;
            if (_controller.PlayList.CurrentInfo?.Beatmap?.GetIdentity().Equals(beatmap.GetIdentity()) == true &&
                CollectionInfo.LockedBool)
            {
                if (_controller.PlayList.CurrentInfo.BeatmapDetail?.Metadata != null)
                {
                    _controller.PlayList.CurrentInfo.BeatmapDetail.Metadata.IsFavorite = false;
                }
            }

            Beatmaps.Remove(beatmap);
            DisplayedBeatmaps.Remove(beatmap);
        }
    }

    [RelayCommand]
    private async Task PlayAllAsync()
    {
        if (Entries == null) return;
        var beatmaps = Entries.ToList();
        if (beatmaps.Count <= 0) return;

        await _controller.SetPlaylistAsync(beatmaps, true);
    }

    [RelayCommand]
    public async Task DeleteCollectionAsync()
    {
        if (CollectionInfo == null) return;
        if (!await _playerData.TryRemoveCollectionAsync(CollectionInfo)) return;
        WeakReferenceMessenger.Default.Send(new CollectionDeletedMessage());
    }
}
