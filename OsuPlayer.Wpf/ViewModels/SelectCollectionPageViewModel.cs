using Milkitic.OsuPlayer.Data;
using Milkitic.WpfApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milkitic.OsuPlayer.ViewModels
{
    public class SelectCollectionPageViewModel : ViewModelBase
    {
        private List<Collection> _collections;

        public List<Collection> Collections
        {
            get => _collections;
            set
            {
                _collections = value;
                OnPropertyChanged();
            }
        }
    }
}
