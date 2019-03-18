using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.WpfApi;
using System.Collections.ObjectModel;

namespace Milky.OsuPlayer.ViewModels
{
    public class SelectCollectionPageViewModel : ViewModelBase
    {
        private ObservableCollection<CollectionViewModel> _collections;
        private Beatmap _entry;

        public ObservableCollection<CollectionViewModel> Collections
        {
            get => _collections;
            set
            {
                _collections = value;
                OnPropertyChanged();
            }
        }

        public Beatmap Entry
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
