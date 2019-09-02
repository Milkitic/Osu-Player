using System.Collections.Generic;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.WpfApi;
using System.Collections.ObjectModel;

namespace Milky.OsuPlayer.ViewModels
{
    public class SelectCollectionPageViewModel : ViewModelBase
    {
        private ObservableCollection<CollectionViewModel> _collections;
        private IList<Beatmap> _entries;

        public ObservableCollection<CollectionViewModel> Collections
        {
            get => _collections;
            set
            {
                _collections = value;
                OnPropertyChanged();
            }
        }

        public IList<Beatmap> Entries
        {
            get => _entries;
            set
            {
                _entries = value;
                OnPropertyChanged();
            }
        }
    }
}
