using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Core;
using OsuPlayer.Core.ObjectModel;
using OsuPlayer.Core.Services;
using OsuPlayer.Data.Models;
using OsuPlayer.Lang;
using OsuPlayer.Localization;
using OsuPlayer.Presentation.Interaction;
using OsuPlayer.Services;
using OsuPlayer.Shared;

namespace OsuPlayer.ViewModels;

public partial class ExportPageViewModel : ObservableObject
{
    private readonly IMapModelConverter _mapModelConverter;
    private readonly IPlayerDataService _playerData;
    private readonly IExportService _exportService;
    private IEnumerable<Beatmap>? _entries;

    public ExportPageViewModel(
        IPlayerDataService playerData,
        IExportService exportService,
        IMapModelConverter mapModelConverter)
    {
        _playerData = playerData;
        _exportService = exportService;
        _mapModelConverter = mapModelConverter;
        _exportService.TaskSuccess += OnExportTaskSuccess;
        ExportPath = AppSettingsOrFallback();
    }

    private static string AppSettingsOrFallback()
    {
        return OsuPlayer.Core.Configuration.AppSettings.Default?.Export.MusicPath ?? AppPaths.Current.MusicPath;
    }

    private void OnExportTaskSuccess(object? sender, EventArgs e)
    {
        _ = UpdateListAsync();
    }

    [ObservableProperty]
    public partial NumberableObservableCollection<BeatmapDataModel>? DataModelList { get; set; }

    [ObservableProperty]
    public partial string ExportPath { get; set; } = string.Empty;

    [RelayCommand]
    public async Task UpdateListAsync()
    {
        await Execute.OnUiThreadAsync(InnerUpdateAsync);
    }

    public Task ReExportAsync(IEnumerable<BeatmapDataModel> dataModels)
    {
        return ItemReExportAsync(dataModels.Cast<object>().ToList());
    }

    public Task DeleteAsync(IEnumerable<BeatmapDataModel> dataModels)
    {
        return ItemDeleteAsync(dataModels.Cast<object>().ToList());
    }

    [RelayCommand]
    private void ItemFolder(object? obj)
    {
        switch (obj)
        {
            case string path when Directory.Exists(path):
                StartProcess(path);
                break;
            case BeatmapDataModel dataModel when !string.IsNullOrWhiteSpace(dataModel.ExportFile):
                StartProcess("Explorer", "/select," + dataModel.ExportFile);
                break;
            default:
                AppNotificationService.Instance.Push(LocalizationService.Instance[SRKeys.Err_DirNotFound]);
                break;
        }
    }

    [RelayCommand]
    private async Task ItemReExportAsync(IList<object>? selectedItems)
    {
        if (selectedItems == null || selectedItems.Count == 0) return;
        var entries = await ConvertToEntriesAsync(selectedItems.OfType<BeatmapDataModel>());
        _exportService.QueueEntries(entries.Where(k => k != null)!);
    }

    [RelayCommand]
    private async Task ItemDeleteAsync(IList<object>? selectedItems)
    {
        if (selectedItems == null || selectedItems.Count == 0) return;
        var dataModels = selectedItems.OfType<BeatmapDataModel>().ToList();
        foreach (var dataModel in dataModels)
        {
            if (File.Exists(dataModel.ExportFile))
            {
                File.Delete(dataModel.ExportFile);
                var dir = new FileInfo(dataModel.ExportFile).Directory;
                if (dir is { Exists: true } && !dir.EnumerateFileSystemInfos().Any())
                {
                    dir.Delete();
                }
            }

            await _playerData.TryAddMapExportAsync(dataModel.GetIdentity(), null);
        }

        await Execute.OnUiThreadAsync(InnerUpdateAsync);
    }

    private async Task<Beatmap?> ConvertToEntryAsync(BeatmapDataModel dataModel)
    {
        return (await _playerData.GetBeatmapsFromFolderAsync(dataModel.FolderName))
            .FirstOrDefault(k => k.Version == dataModel.Version);
    }

    private async Task<IEnumerable<Beatmap?>> ConvertToEntriesAsync(IEnumerable<BeatmapDataModel> dataModels)
    {
        var entries = new List<Beatmap?>();
        foreach (var dataModel in dataModels)
        {
            entries.Add(await ConvertToEntryAsync(dataModel));
        }

        return entries;
    }

    private async Task InnerUpdateAsync()
    {
        var maps = await _playerData.GetExportedMapsAsync();
        var list = new List<(OsuPlayer.Shared.MapIdentity identity, string path, string time, string size)>();
        foreach (var map in maps)
        {
            try
            {
                var fi = new FileInfo(map.ExportFile);
                list.Add(!fi.Exists
                    ? (map.GetIdentity(), map.ExportFile, "已从目录移除", "已从目录移除")
                    : (map.GetIdentity(), map.ExportFile, fi.CreationTime.ToString("g"),
                        SharedUtils.CountSize(fi.Length)));
            }
            catch
            {
                list.Add((map.GetIdentity(), map.ExportFile, DateTime.MinValue.ToString("g"), "0 B"));
            }
        }

        _entries = await _playerData.GetBeatmapsByIdentifiableAsync(maps);
        var viewModels = _mapModelConverter.ToDataModelList(_entries, true).ToList();
        for (var i = 0; i < viewModels.Count; i++)
        {
            var item = list.First(k => k.identity.Equals(viewModels[i].GetIdentity()));
            viewModels[i].ExportFile = item.path;
            viewModels[i].FileSize = item.size;
            viewModels[i].ExportTime = item.time;
        }

        DataModelList = new NumberableObservableCollection<BeatmapDataModel>(viewModels);
        ExportPath = AppSettingsOrFallback();
    }

    private static void StartProcess(string target, string? arguments = null)
    {
        Process.Start(new ProcessStartInfo(target, arguments ?? string.Empty)
        {
            UseShellExecute = true
        });
    }
}
