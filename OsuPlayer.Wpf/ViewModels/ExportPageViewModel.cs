using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Pages;
using Milky.OsuPlayer.Utils;
using Milky.WpfApi;
using Milky.WpfApi.Collections;
using Milky.WpfApi.Commands;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

namespace Milky.OsuPlayer.ViewModels
{
    public class ExportPageViewModel : ViewModelBase
    {
        private string _exportPath;
        private NumberableObservableCollection<BeatmapDataModel> _dataModelList;
        private IEnumerable<BeatmapEntry> _entries;

        public string UiStrExported => App.UiMetadata.Exported;
        public string UiStrExporting => App.UiMetadata.Exporting;
        public string UiStrExportPath => App.UiMetadata.ExportPath;
        public string UiStrTitle => App.UiMetadata.Title;
        public string UiStrArtist => App.UiMetadata.Artist;
        public string UiStrFileSize => App.UiMetadata.FileSize;
        public string UiStrExportTime => App.UiMetadata.ExportTime;
        public string UiStrDeleteExported => App.UiMetadata.DeleteExported;
        public string UiStrExportAgain => App.UiMetadata.ExportAgain;
        public string UiStrOpenFolder => App.UiMetadata.OpenFileFolder;

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
                            Process.Start(path);
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

                        DbOperator.AddMapExport(dataModel.GetIdentity(), null);
                    }

                    Execute.OnUiThread(InnerUpdate);
                });


            }
        }

        private BeatmapEntry ConvertToEntry(BeatmapDataModel dataModel)
        {
            return _entries.GetBeatmapsetsByFolder(dataModel.FolderName)
                .FirstOrDefault(k => k.Version == dataModel.Version);
        }

        private IEnumerable<BeatmapEntry> ConvertToEntries(IEnumerable<BeatmapDataModel> dataModels)
        {
            return dataModels.Select(ConvertToEntry);
        }

        private void InnerUpdate()
        {
            var maps = (List<MapInfo>)DbOperator.GetExportedMaps();
            List<(MapIdentity MapIdentity, string path, string time, string size)> list =
                new List<(MapIdentity, string, string, string)>();
            foreach (var map in maps)
            {
                var fi = new FileInfo(map.ExportFile);
                list.Add(!fi.Exists
                    ? (map.GetIdentity(), map.ExportFile, "已从目录移除", "已从目录移除")
                    : (map.GetIdentity(), map.ExportFile, fi.CreationTime.ToString("g"), Util.CountSize(fi.Length)));
            }

            _entries = App.Beatmaps.GetMapListFromDb(maps);
            var viewModels = _entries.ToViewModel(false).ToList();
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
