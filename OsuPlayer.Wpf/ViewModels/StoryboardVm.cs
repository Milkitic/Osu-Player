using Milky.OsuPlayer.Data.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Properties;

namespace Milky.OsuPlayer.ViewModels
{
    internal class StoryboardVm : INotifyPropertyChanged
    {
        private bool _isScanned;
        private ObservableCollection<BeatmapDataModel> _beatmapModels;

        public bool IsScanned
        {
            get => _isScanned;
            set
            {
                if (value == _isScanned) return;
                _isScanned = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<BeatmapDataModel> BeatmapModels
        {
            get => _beatmapModels;
            set
            {
                if (Equals(value, _beatmapModels)) return;
                _beatmapModels = value;
                OnPropertyChanged();
            }
        }

        public static StoryboardVm Default
        {
            get
            {
                lock (_defaultLock)
                {
                    return _default ?? (_default = new StoryboardVm());
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static StoryboardVm _default;
        private static object _defaultLock = new object();
        private StoryboardVm()
        {
        }
    }
}
