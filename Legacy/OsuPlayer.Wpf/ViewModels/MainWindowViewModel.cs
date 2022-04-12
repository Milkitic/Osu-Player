﻿using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Presentation.Interaction;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Milky.OsuPlayer.Common.Configuration;

namespace Milky.OsuPlayer.ViewModels
{
    public class MainWindowViewModel : VmBase
    {
        public static MainWindowViewModel Current { get; private set; }

        public MainWindowViewModel()
        {
            Current = this;
        }

        private bool _isNavigationCollapsed;
        private ObservableCollection<Collection> _collection;
        private bool _isLyricWindowLocked;
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
                if (_isNavigationCollapsed == value) return;
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
                        AppSettings.Default.General.IsNavigationCollapsed = IsNavigationCollapsed;
                        AppSettings.SaveDefault();
                    });
                });
            }
        }
    }
}
