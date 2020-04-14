using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Pages;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Presentation.ObjectModel;
using Milky.OsuPlayer.Shared;
using Milky.OsuPlayer.Utils;
using OSharp.Beatmap.MetaData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Milky.OsuPlayer.UiComponents.NotificationComponent;

namespace Milky.OsuPlayer.ViewModels
{
    public class ExportPageViewModel : VmBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private string _exportPath;
        private NumberableObservableCollection<BeatmapDataModel> _dataModelList;
        private IEnumerable<Beatmap> _entries;
        private static readonly SafeDbOperator SafeDbOperator = new SafeDbOperator();

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
                return new DelegateCommand(obj =>
                {
                    Execute.OnUiThread(InnerUpdate);
                });
            }
        }

        public ICommand ItemFolderCommand
        {
            get
            {
                return new DelegateCommand(obj =>
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
                                Notification.Push(I18NUtil.GetString("err-dirNotFound"), I18NUtil.GetString("text-error"));
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
                return new DelegateCommand(async obj =>
                {
                    if (obj == null) return;
                    var selected = ((System.Windows.Controls.ListView)obj).SelectedItems;
                    var entries = ConvertToEntries(selected.Cast<BeatmapDataModel>());
                    foreach (var entry in entries)
                    {
                        ExportPage.QueueEntry(entry);
                    }

                    await Task.Run(() =>
                    {
                        while (ExportPage.IsTaskBusy)
                        {
                            Thread.Sleep(10);
                            if (!ExportPage.HasTaskSuccess) continue;
                            Execute.OnUiThread(InnerUpdate);
                        }
                    });
                });
            }
        }

        public ICommand ItemDeleteCommand
        {
            get
            {
                return new DelegateCommand(obj =>
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

                        SafeDbOperator.TryAddMapExport(dataModel.GetIdentity(), null);
                    }

                    Execute.OnUiThread(InnerUpdate);
                });
            }
        }

        private Beatmap ConvertToEntry(BeatmapDataModel dataModel)
        {
            return SafeDbOperator.GetBeatmapsFromFolder(dataModel.FolderName)
                .FirstOrDefault(k => k.Version == dataModel.Version);
        }

        private IEnumerable<Beatmap> ConvertToEntries(IEnumerable<BeatmapDataModel> dataModels)
        {
            return dataModels.Select(ConvertToEntry);
        }

        private void InnerUpdate()
        {
            var maps = SafeDbOperator.GetExportedMaps();
            List<(MapIdentity MapIdentity, string path, string time, string size)> list =
                new List<(MapIdentity, string, string, string)>();
            foreach (var map in maps)
            {
                try
                {
                    var fi = new FileInfo(map.ExportFile);
                    list.Add(!fi.Exists
                        ? (map.GetIdentity(), map.ExportFile, "已从目录移除", "已从目录移除")
                        : (map.GetIdentity(), map.ExportFile, fi.CreationTime.ToString("g"), SharedUtils.CountSize(fi.Length)));
                }
                catch (Exception ex)
                {
                    list.Add((map.GetIdentity(), map.ExportFile, new DateTime().ToString("g"), "0 B"));
                    Logger.Error(ex, "Error while updating view item: {0}", map.GetIdentity());
                }
            }

            _entries = SafeDbOperator.GetBeatmapsByIdentifiable(maps);
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
