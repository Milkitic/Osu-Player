using Milky.WpfApi;
using Milky.WpfApi.Collections;
using Milky.OsuPlayer.Data.EF.Model;
using Milky.OsuPlayer.Models;

namespace Milky.OsuPlayer.ViewModels
{
    public class CollectionPageViewModel : ViewModelBase
    {
        private NumberableObservableCollection<BeatmapDataModel> _beatmaps;
        private Collection _collectionInfo;

        public NumberableObservableCollection<BeatmapDataModel> Beatmaps
        {
            get => _beatmaps;
            set
            {
                _beatmaps = value;
                OnPropertyChanged();
            }
        }

        public Collection CollectionInfo
        {
            get => _collectionInfo;
            set
            {
                _collectionInfo = value;
                OnPropertyChanged();
            }
        }
    }
}
