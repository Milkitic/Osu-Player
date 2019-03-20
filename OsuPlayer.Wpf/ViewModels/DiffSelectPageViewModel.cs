using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Metadata;
using Milky.WpfApi;
using Milky.WpfApi.Collections;
using Milky.WpfApi.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Milky.OsuPlayer.ViewModels
{
    public class DiffSelectPageViewModel : ViewModelBase
    {
        private NumberableObservableCollection<BeatmapDataModel> _dataModels;

        public NumberableObservableCollection<BeatmapDataModel> DataModels
        {
            get => _dataModels;
            set
            {
                _dataModels = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<Beatmap> Entries { get; set; }
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
