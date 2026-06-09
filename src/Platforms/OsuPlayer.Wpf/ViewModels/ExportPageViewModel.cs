using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Core;
using OsuPlayer.Core.Services;
using OsuPlayer.Data.Models;
using OsuPlayer.Presentation.Interaction;
using OsuPlayer.Core.ObjectModel;
using OsuPlayer.Services;
using OsuPlayer.Shared;
using OsuPlayer.UiComponents.NotificationComponent;
using OsuPlayer.Utils;

using Microsoft.Extensions.Logging;

namespace OsuPlayer.ViewModels;

public partial class ExportPageViewModel : ObservableObject
{
    private readonly ILogger<ExportPageViewModel> _logger;
    private readonly IMapModelConverter _mapModelConverter;
    private IEnumerable<Beatmap> _entries;
    private readonly IPlayerDataService _playerData;
    private readonly IExportService _exportService;

    public ExportPageViewModel(IPlayerDataService playerData, IExportService exportService, IMapModelConverter mapModelConverter, ILogger<ExportPageViewModel> logger)
    {
        _playerData = playerData;
        _exportService = exportService;
        _mapModelConverter = mapModelConverter;
        _logger = logger;
        _exportService.TaskSuccess += OnExportTaskSuccess;
    }

    private void OnExportTaskSuccess(object sender, EventArgs e)
    {
        _ = UpdateListAsync();
    }

    [ObservableProperty]
    public partial NumberableObservableCollection<BeatmapDataModel> DataModelList { get; set; }

    [ObservableProperty]
    public partial string ExportPath { get; set; }

    [RelayCommand]
    public async Task UpdateListAsync()
    {
        await Execute.OnUiThreadAsync(InnerUpdateAsync);
    }

    [RelayCommand]
    private void ItemFolder(object obj)
    {
        switch (obj)
        {
            case string path:
                if (Directory.Exists(path))
                {
                    Process.Start(path);
                }
                else
                {
                    Notification.Push(I18NUtil.GetString("err-dirNotFound"),
                        I18NUtil.GetString("text-error"));
                }

                break;
            case BeatmapDataModel dataModel:
                Process.Start("Explorer", "/select," + dataModel.ExportFile);
                break;
            default:
                return;
        }
    }

    [RelayCommand]
    private async Task ItemReExportAsync(object obj)
    {
        if (obj == null) return;
        var selected = ((System.Windows.Controls.ListView)obj).SelectedItems;
        var entries = await ConvertToEntriesAsync(selected.Cast<BeatmapDataModel>());
        _exportService.QueueEntries(entries);
    }

    [RelayCommand]
    private async Task ItemDeleteAsync(object obj)
    {
        if (obj == null) return;
        var selected = ((System.Windows.Controls.ListView)obj).SelectedItems;
        var dataModels = selected.Cast<BeatmapDataModel>();

        foreach (var dataModel in dataModels)
        {
            if (File.Exists(dataModel.ExportFile))
            {
                File.Delete(dataModel.ExportFile);
                var dir = new FileInfo(dataModel.ExportFile).Directory;
                if (dir.Exists && dir.GetFiles().Length == 0)
                    dir.Delete();
            }

            await _playerData.TryAddMapExportAsync(dataModel.GetIdentity(), null);
        }

        await Execute.OnUiThreadAsync(InnerUpdateAsync);
    }

    private async Task<Beatmap> ConvertToEntryAsync(BeatmapDataModel dataModel)
    {
        return (await _playerData.GetBeatmapsFromFolderAsync(dataModel.FolderName))
            .FirstOrDefault(k => k.Version == dataModel.Version);
    }

    private async Task<IEnumerable<Beatmap>> ConvertToEntriesAsync(IEnumerable<BeatmapDataModel> dataModels)
    {
        var entries = new List<Beatmap>();
        foreach (var dataModel in dataModels)
        {
            entries.Add(await ConvertToEntryAsync(dataModel));
        }

        return entries;
    }

    private async Task InnerUpdateAsync()
    {
        var maps = await _playerData.GetExportedMapsAsync();
        List<(global::OsuPlayer.Shared.MapIdentity MapIdentity, string path, string time, string size)> list =
            new List<(MapIdentity, string, string, string)>();
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
            catch (Exception ex)
            {
                list.Add((map.GetIdentity(), map.ExportFile, new DateTime().ToString("g"), "0 B"));
                _logger.LogError(ex, "Error while updating view item: {Identity}", map.GetIdentity());
            }
        }

        _entries = await _playerData.GetBeatmapsByIdentifiableAsync(maps);
        var viewModels = _mapModelConverter.ToDataModelList(_entries, true).ToList();
        for (var i = 0; i < viewModels.Count; i++)
        {
            var sb = list.First(k => k.MapIdentity.Equals(viewModels[i].GetIdentity()));
            viewModels[i].ExportFile = sb.path;
            viewModels[i].FileSize = sb.size;
            viewModels[i].ExportTime = sb.time;
        }

        DataModelList = new NumberableObservableCollection<BeatmapDataModel>(viewModels);
    }
}
