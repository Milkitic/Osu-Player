using Milky.OsuPlayer.Pages;
using Milky.OsuPlayer.Utils;
using Milky.WpfApi;
using Milky.WpfApi.Collections;
using Milky.WpfApi.Commands;
using osu_database_reader.Components.Beatmaps;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Common.I18N;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Metadata;

namespace Milky.OsuPlayer.ViewModels
{
    public class ExportPageViewModel : ViewModelBase
    {
        public ExportPageViewModel()
        {
            _uiMetadata = InstanceManage.GetInstance<UiMetadata>();
        }

        private string _exportPath;
        private NumberableObservableCollection<BeatmapDataModel> _dataModelList;
        private IEnumerable<BeatmapEntry> _entries;
        private readonly UiMetadata _uiMetadata;

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

                        DbOperate.AddMapExport(dataModel.GetIdentity(), null);
                    }

                    Execute.OnUiThread(InnerUpdate);
                });


            }
        }

        private BeatmapEntry ConvertToEntry(BeatmapDataModel dataModel)
        {
            return _entries.FilterByFolder(dataModel.FolderName)
                .FirstOrDefault(k => k.Version == dataModel.Version);
        }

        private IEnumerable<BeatmapEntry> ConvertToEntries(IEnumerable<BeatmapDataModel> dataModels)
        {
            return dataModels.Select(ConvertToEntry);
        }

        private void InnerUpdate()
        {
            var maps = (List<MapInfo>)DbOperate.GetExportedMaps();
            List<(MapIdentity MapIdentity, string path, string time, string size)> list =
                new List<(MapIdentity, string, string, string)>();
            foreach (var map in maps)
            {
                var fi = new FileInfo(map.ExportFile);
                list.Add(!fi.Exists
                    ? (map.GetIdentity(), map.ExportFile, "已从目录移除", "已从目录移除")
                    : (map.GetIdentity(), map.ExportFile, fi.CreationTime.ToString("g"), Util.CountSize(fi.Length)));
            }

            _entries = maps.ToBeatmapEntries(InstanceManage.GetInstance<OsuDbInst>().Beatmaps);
            var viewModels = _entries.ToDataModels(true).ToList();
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
