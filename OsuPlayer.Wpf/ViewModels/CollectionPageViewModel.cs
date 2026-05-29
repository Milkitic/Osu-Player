using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Presentation.ObjectModel;
using Milky.OsuPlayer.Services;
using Milky.OsuPlayer.UiComponents.FrontDialogComponent;
using Milky.OsuPlayer.UiComponents.NotificationComponent;
using Milky.OsuPlayer.UserControls;

namespace Milky.OsuPlayer.ViewModels;

public partial class CollectionPageViewModel : ObservableObject, INavigationAware
{
    private readonly ObservablePlayController _controller;
    private readonly IPlayerDataService _playerData;
    private readonly IExportService _exportService;

    public CollectionPageViewModel(IPlayerDataService playerData, ObservablePlayController controller, IExportService exportService)
    {
        _playerData = playerData;
        _controller = controller;
        _exportService = exportService;
    }

    [ObservableProperty]
    public partial NumberableObservableCollection<BeatmapDataModel> Beatmaps { get; set; }

    [ObservableProperty]
    public partial NumberableObservableCollection<BeatmapDataModel> DisplayedBeatmaps { get; set; }

    [ObservableProperty]
    public partial Collection CollectionInfo { get; set; }

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
        if (beatmap == null) return;
        var map = await _playerData.GetBeatmapByIdentifiableAsync(beatmap);
        if (map == null) return;
        var folderName = beatmap.GetFolder(out _, out _);
        if (!Directory.Exists(folderName))
        {
            Notification.Push(@"所选文件不存在，可能没有及时同步。请尝试手动同步osuDB后重试。");
            return;
        }

        Process.Start(folderName);
    }

    [RelayCommand]
    private async Task OpenScorePageAsync(BeatmapDataModel beatmap)
    {
        if (beatmap == null) return;
        var map = await _playerData.GetBeatmapByIdentifiableAsync(beatmap);
        if (map == null) return;
        Process.Start($"https://osu.ppy.sh/s/{map.BeatmapSetId}");
    }

    [RelayCommand]
    private async Task SaveCollectionAsync(BeatmapDataModel beatmap)
    {
        if (beatmap == null) return;
        var map = await _playerData.GetBeatmapByIdentifiableAsync(beatmap);
        if (map == null) return;
        FrontDialogOverlay.Default.ShowContent(new SelectCollectionControl(map),
            DialogOptionFactory.SelectCollectionOptions);
    }

    [RelayCommand]
    private async Task ExportAsync(BeatmapDataModel beatmap)
    {
        if (beatmap == null) return;
        var map = await _playerData.GetBeatmapByIdentifiableAsync(beatmap);
        if (map == null) return;
        _exportService.QueueEntry(map);
    }

    [RelayCommand]
    private void ExportAll()
    {
        if (Entries == null) return;
        _exportService.QueueEntries(Entries);
    }

    [RelayCommand]
    private async Task DirectPlayAsync(BeatmapDataModel beatmap)
    {
        if (beatmap == null) return;
        var map = await _playerData.GetBeatmapByIdentifiableAsync(beatmap);
        if (map == null) return;
        await _controller.PlayNewAsync(map);
    }

    [RelayCommand]
    private async Task PlayAsync(BeatmapDataModel beatmap)
    {
        if (beatmap == null) return;
        var map = await _playerData.GetBeatmapByIdentifiableAsync(beatmap);
        if (map == null) return;
        await _controller.PlayNewAsync(map);
    }

    [RelayCommand]
    private async Task RemoveAsync(BeatmapDataModel beatmap)
    {
        if (beatmap == null) return;
        if (!await _playerData.TryRemoveMapFromCollectionAsync(beatmap.GetIdentity(), CollectionInfo))
            return;
        if (_controller.PlayList.CurrentInfo.Beatmap.GetIdentity().Equals(beatmap.GetIdentity()) &&
            CollectionInfo.LockedBool)
        {
            _controller.PlayList.CurrentInfo.BeatmapDetail.Metadata.IsFavorite = false;
        }

        Beatmaps.Remove(beatmap);
        DisplayedBeatmaps.Remove(beatmap);
    }
}