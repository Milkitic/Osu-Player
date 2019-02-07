using System.Collections.ObjectModel;
using Milky.WpfApi;
using osu_database_reader.Components.Beatmaps;

namespace Milky.OsuPlayer.ViewModels
{
    public class SelectCollectionPageViewModel : ViewModelBase
    {
        private ObservableCollection<CollectionViewModel> _collections;
        private BeatmapEntry _entry;

        public ObservableCollection<CollectionViewModel> Collections
        {
            get => _collections;
            set
            {
                _collections = value;
                OnPropertyChanged();
            }
        }

        public BeatmapEntry Entry
        {
            get => _entry;
            set
            {
                _entry = value;
                OnPropertyChanged();
            }
        }
    }
}
