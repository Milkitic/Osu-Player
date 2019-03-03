using System.Collections.Generic;
using System.Windows.Input;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Player;
using Milky.WpfApi;
using Milky.WpfApi.Commands;

namespace Milky.OsuPlayer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private bool _isNavigationCollapsed;
        private bool _isMiniMode;
        private List<Collection> _collection;
        private PlayListMode _playListMode;
        private bool _isLyricWindowShown;
        private bool _isLyricWindowLocked;
        private bool _isPlaying;
        private bool _enableVideo;
        private bool _isSyncing;

        public bool IsNavigationCollapsed
        {
            get => _isNavigationCollapsed;
            set
            {
                _isNavigationCollapsed = value;
                OnPropertyChanged();
            }
        }

        public bool IsMiniMode
        {
            get => _isMiniMode;
            set
            {
                _isMiniMode = value;
                OnPropertyChanged();
            }
        }

        public bool IsLyricWindowShown
        {
            get => _isLyricWindowShown;
            set
            {
                _isLyricWindowShown = value;
                OnPropertyChanged();
            }
        }

        public bool IsLyricWindowLocked
        {
            get => _isLyricWindowLocked;
            set
            {
                _isLyricWindowLocked = value;
                OnPropertyChanged();
            }
        }

        public bool IsSyncing
        {
            get => _isSyncing;
            set
            {
                _isSyncing = value;
                OnPropertyChanged();
            }
        }

        public List<Collection> Collection
        {
            get => _collection;
            set
            {
                _collection = value;
                OnPropertyChanged();
            }
        }

        public PlayListMode PlayListMode
        {
            get => _playListMode;
            set
            {
                _playListMode = value;
                OnPropertyChanged();
            }
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value;
                OnPropertyChanged();
            }
        }

        public bool EnableVideo
        {
            get => _enableVideo;
            set
            {
                _enableVideo = value;
                OnPropertyChanged();
            }
        }

        public ICommand CollapseCommand
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    Execute.OnUiThread(() =>
                    {
                        IsNavigationCollapsed = !IsNavigationCollapsed;
                    });
                });
            }
        }
    }
}
