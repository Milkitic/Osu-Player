using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Presentation.Interaction;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Milky.OsuPlayer.ViewModels
{
    public class SelectCollectionPageViewModel : VmBase
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
