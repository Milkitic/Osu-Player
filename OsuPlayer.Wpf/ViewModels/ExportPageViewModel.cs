using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Coosu.Beatmap.MetaData;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Pages;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Presentation.ObjectModel;
using Milky.OsuPlayer.Services;
using Milky.OsuPlayer.Shared;
using Milky.OsuPlayer.UiComponents.NotificationComponent;
using Milky.OsuPlayer.Utils;

namespace Milky.OsuPlayer.ViewModels
{
    public class ExportPageViewModel : VmBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private string _exportPath;
        private NumberableObservableCollection<BeatmapDataModel> _dataModelList;
        private IEnumerable<Beatmap> _entries;
        private readonly IPlayerDataService _playerData;

        public ExportPageViewModel()
            : this(AppServices.PlayerData)
        {
        }

        public ExportPageViewModel(IPlayerDataService playerData)
        {
            _playerData = playerData;
        }

        public NumberableObservableCollection<BeatmapDataModel> DataModelList
        {
            get => _dataModelList;
            set
            {
                _dataModelList = value;
                OnPropertyChanged();
            }
        }

        public string ExportPath
        {
            get => _exportPath;
            set
            {
                _exportPath = value;
                OnPropertyChanged();
            }
        }

        public ICommand UpdateList
        {
            get
            {
                return new AsyncRelayCommand<object>(async obj => { await Execute.OnUiThreadAsync(InnerUpdateAsync); });
            }
        }

        public ICommand ItemFolderCommand
        {
            get
            {
                return new RelayCommand<object>(obj =>
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
                });
            }
        }

        public ICommand ItemReExportCommand
        {
            get
            {
                return new AsyncRelayCommand<object>(async obj =>
                {
                    if (obj == null) return;
                    var selected = ((System.Windows.Controls.ListView)obj).SelectedItems;
                    var entries = await ConvertToEntriesAsync(selected.Cast<BeatmapDataModel>());
                    foreach (var entry in entries)
                    {
                        ExportPage.QueueEntry(entry);
                    }

                    await Task.Run(async () =>
                    {
                        while (ExportPage.IsTaskBusy)
                        {
                            Thread.Sleep(10);
                            if (!ExportPage.HasTaskSuccess) continue;
                            await Execute.OnUiThreadAsync(InnerUpdateAsync);
                        }
                    });
                });
            }
        }

        public ICommand ItemDeleteCommand
        {
            get
            {
                return new AsyncRelayCommand<object>(async obj =>
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
                });
            }
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
            List<(MapIdentity MapIdentity, string path, string time, string size)> list =
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
                    Logger.Error(ex, "Error while updating view item: {0}", map.GetIdentity());
                }
            }

            _entries = await _playerData.GetBeatmapsByIdentifiableAsync(maps);
            var viewModels = _entries.ToDataModelList(true).ToList();
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
}