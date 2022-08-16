using Milki.OsuPlayer.Presentation.Interaction;

namespace Milki.OsuPlayer.ViewModels
{
    public class EditCollectionPageViewModel : VmBase
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
