using Milky.OsuPlayer.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.EF.Model;
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
