using Milky.WpfApi;

namespace Milky.OsuPlayer.ViewModels
{
    public class WelcomeViewModel : ViewModelBase
    {
        private bool _guideSyncing;
        private bool _guideSelectedDb;

        public bool GuideSyncing
        {
            get => _guideSyncing;
            set
            {
                _guideSyncing = value;
                OnPropertyChanged();
            }
        }

        public bool GuideSelectedDb
        {
            get => _guideSelectedDb;
            set
            {
                _guideSelectedDb = value;
                OnPropertyChanged();
            }
        }
    }
}