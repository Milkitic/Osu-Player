using Milkitic.OsuPlayer.Data;
using Milkitic.WpfApi;
using Milkitic.WpfApi.Commands;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace Milkitic.OsuPlayer.ViewModels
{
    public class ExportPageViewModel : ViewModelBase
    {
        private string _exportPath;
        private IEnumerable<BeatmapEntry> _entries;
        private ObservableCollection<BeatmapDataModel> _dataModelList;

        public string UiStrExported => "已导出";
        public string UiStrExporting => "正在进行";
        public string UiStrExportPath => "导出目录";
        public string UiStrTitle => "标题";
        public string UiStrArtist => "艺术家";
        public string UiStrFileSize => "大小";
        public string UiStrExportTime => "导出时间";

        public ObservableCollection<BeatmapDataModel> DataModelList
        {
            get => _dataModelList;
            set
            {
                _dataModelList = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<BeatmapEntry> Entries
        {
            get => _entries;
            set
            {
                _entries = value;
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


        public ICommand OpenFolderCommand
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    Execute.OnUiThread(() =>
                    {
                        if (obj == null) return;
                        var viewModel = (BeatmapDataModel)obj;
                        Process.Start("Explorer", "/select," + viewModel.ExportFile);
                    });
                });
            }
        }


        private BeatmapEntry ConvertToEntry(BeatmapDataModel obj)
        {
            return Entries.GetBeatmapsetsByFolder(obj.FolderName).FirstOrDefault(k => k.Version == obj.Version);
        }
    }
}
