using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Player;
using Milky.WpfApi;
using Milky.WpfApi.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Windows;

namespace Milky.OsuPlayer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public static MainWindowViewModel Current { get; private set; }

        public MainWindowViewModel()
        {
            Current = this;
        }

        private bool _isNavigationCollapsed;
        private ObservableCollection<Collection> _collection;
        private bool _isLyricWindowLocked;
        private PlayerViewModel _player;
        private LyricWindowViewModel _lyricWindowViewModel;

        public LyricWindowViewModel LyricWindowViewModel
        {
            get => _lyricWindowViewModel;
            set
            {
                _lyricWindowViewModel = value;
                OnPropertyChanged();
            }
        }

        public bool IsNavigationCollapsed
        {
            get => _isNavigationCollapsed;
            set
            {
                _isNavigationCollapsed = value;
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

        public ObservableCollection<Collection> Collection
        {
            get => _collection;
            set
            {
                _collection = value;
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

        public PlayerViewModel Player
        {
            get => _player;
            set
            {
                _player = value;
                OnPropertyChanged();
            }
        }
    }
}
