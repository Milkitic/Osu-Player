using Milkitic.OsuPlayer.Data;
using Milkitic.OsuPlayer.Pages;
using Milkitic.WpfApi;
using Milkitic.WpfApi.Commands;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Milkitic.OsuPlayer.ViewModels
{
    public class DiffSelectPageViewModel : ViewModelBase
    {
        private IEnumerable<BeatmapDataModel> _dataModels;

        public IEnumerable<BeatmapDataModel> DataModels
        {
            get => _dataModels;
            set
            {
                _dataModels = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<BeatmapEntry> Entries { get; set; }
        public BeatmapDataModel SelectedMap { get; set; }
        public Action Callback { get; set; }

        public ICommand SelectCommand
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    SelectedMap = DataModels.FirstOrDefault(k => k.Version == (string)obj);
                    Callback?.Invoke();
                });
            }
        }

    }
}
