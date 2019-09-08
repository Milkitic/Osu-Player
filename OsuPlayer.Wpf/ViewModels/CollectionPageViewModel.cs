using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Common.Metadata;
using Milky.WpfApi;
using Milky.WpfApi.Collections;
using Milky.OsuPlayer.Models;

namespace Milky.OsuPlayer.ViewModels
{
    public class CollectionPageViewModel : ViewModelBase
    {
        private NumberableObservableCollection<BeatmapDataModel> _beatmaps;
        private NumberableObservableCollection<BeatmapDataModel> _displayedBeatmaps;
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
        public NumberableObservableCollection<BeatmapDataModel> DisplayedBeatmaps
        {
            get => _displayedBeatmaps;
            set
            {
                _displayedBeatmaps = value;
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
