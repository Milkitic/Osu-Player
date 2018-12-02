using Milkitic.OsuPlayer.Data;
using Milkitic.WpfApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milkitic.OsuPlayer.ViewModels
{
    public class EditCollectionPageViewModel : ViewModelBase
    {
        private string _name;
        private string _description;
        private string _coverPath;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        public string CoverPath
        {
            get => _coverPath;
            set
            {
                _coverPath = value;
                OnPropertyChanged();
            }
        }
    }
}
