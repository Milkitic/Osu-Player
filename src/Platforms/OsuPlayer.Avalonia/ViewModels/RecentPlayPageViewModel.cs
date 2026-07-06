using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OsuPlayer.Core;
using OsuPlayer.Core.ObjectModel;
using OsuPlayer.Core.Services;
using OsuPlayer.Data;
using OsuPlayer.Playback;
using OsuPlayer.Presentation.Interaction;
using OsuPlayer.Services;

namespace OsuPlayer.ViewModels;

public partial class RecentPlayPageViewModel : ObservableObject
{
    private readonly IPlayerDataService _playerData;
    private readonly ObservablePlayController _controller;
    private readonly IBeatmapActionService _beatmapActions;
    private readonly IMapModelConverter _mapModelConverter;

    public RecentPlayPageViewModel(
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
    public partial NumberableObservableCollection<BeatmapDataModel>? Beatmaps { get; set; }

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
    public async Task DirectPlayAsync(BeatmapDataModel beatmap)
    {
        await _beatmapActions.PlayAsync(beatmap);
    }

    [RelayCommand]
    public async Task PlayAsync(BeatmapDataModel beatmap)
    {
        await _beatmapActions.PlayAsync(beatmap);
    }

    [RelayCommand]
    private async Task RemoveAsync(BeatmapDataModel beatmap)
    {
        if (await _playerData.TryRemoveFromRecentAsync(beatmap))
        {
            Beatmaps?.Remove(beatmap);
        }
    }

    [RelayCommand]
    private async Task PlayAllAsync()
    {
        var recentList = await _playerData.GetRecentListAsync();
        var recentBeatmaps = await _playerData.GetBeatmapsByMapInfoAsync(recentList, TimeSortMode.PlayTime);
        if (recentBeatmaps == null || !recentBeatmaps.Any()) return;

        await _controller.SetPlaylistAsync(recentBeatmaps, true);
    }

    [RelayCommand]
    public async Task ClearAllRecentAsync()
    {
        if (await _playerData.TryClearRecentAsync())
        {
            Beatmaps?.Clear();
        }
    }

    public async Task UpdateListAsync()
    {
        var recentList = await _playerData.GetRecentListAsync();
        var recentBeatmaps = await _playerData.GetBeatmapsByMapInfoAsync(recentList, TimeSortMode.PlayTime);
        Beatmaps = new NumberableObservableCollection<BeatmapDataModel>(await _mapModelConverter.ToDataModelListAsync(recentBeatmaps));
    }
}
