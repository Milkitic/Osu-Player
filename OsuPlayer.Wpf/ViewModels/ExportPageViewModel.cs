using System;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.I18N;
using Milky.OsuPlayer.Common.Metadata;
using Milky.OsuPlayer.Pages;
using Milky.OsuPlayer.Utils;
using Milky.WpfApi;
using Milky.WpfApi.Collections;
using Milky.WpfApi.Commands;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Milky.OsuPlayer.ViewModels
{
    public class ExportPageViewModel : ViewModelBase
    {
        public ExportPageViewModel()
        {
            _uiMetadata = Services.Get<UiMetadata>();
        }

        private string _exportPath;
        private NumberableObservableCollection<BeatmapDataModel> _dataModelList;
        private IEnumerable<Beatmap> _entries;
        private readonly UiMetadata _uiMetadata;
        private BeatmapDbOperator _beatmapDbOperator = new BeatmapDbOperator();
        private AppDbOperator _appDbOperator = new AppDbOperator();

        public string UiStrExported => _uiMetadata.Exported;
        public string UiStrExporting => _uiMetadata.Exporting;
        public string UiStrExportPath => _uiMetadata.ExportPath;
        public string UiStrTitle => _uiMetadata.Title;
        public string UiStrArtist => _uiMetadata.Artist;
        public string UiStrFileSize => _uiMetadata.FileSize;
        public string UiStrExportTime => _uiMetadata.ExportTime;
        public string UiStrDeleteExported => _uiMetadata.DeleteExported;
        public string UiStrExportAgain => _uiMetadata.ExportAgain;
        public string UiStrOpenFolder => _uiMetadata.OpenFileFolder;

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

                        _appDbOperator.AddMapExport(dataModel.GetIdentity(), null);
                    }

                    Execute.OnUiThread(InnerUpdate);
                });


            }
        }

        private Beatmap ConvertToEntry(BeatmapDataModel dataModel)
        {
            return _beatmapDbOperator.GetBeatmapsFromFolder(dataModel.FolderName)
                .FirstOrDefault(k => k.Version == dataModel.Version);
        }

        private IEnumerable<Beatmap> ConvertToEntries(IEnumerable<BeatmapDataModel> dataModels)
        {
            return dataModels.Select(ConvertToEntry);
        }

        private void InnerUpdate()
        {
            var maps = _appDbOperator.GetExportedMaps();
            List<(MapIdentity MapIdentity, string path, string time, string size)> list =
                new List<(MapIdentity, string, string, string)>();
            foreach (var map in maps)
            {
                try
                {
                    var fi = new FileInfo(map.ExportFile);
                    list.Add(!fi.Exists
                        ? (map.GetIdentity(), map.ExportFile, "已从目录移除", "已从目录移除")
                        : (map.GetIdentity(), map.ExportFile, fi.CreationTime.ToString("g"), Util.CountSize(fi.Length)));

                }
                catch (Exception e)
                {
                    list.Add((map.GetIdentity(), map.ExportFile, new DateTime().ToString("g"), "0 B"));
                    Console.WriteLine(e);
                }
            }

            _entries = _beatmapDbOperator.GetBeatmapsByIdentifiable(maps);
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
