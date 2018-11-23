using Milkitic.WpfApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Milkitic.OsuPlayer.Data;
using Milkitic.OsuPlayer.Windows;
using Milkitic.WpfApi.Commands;

namespace Milkitic.OsuPlayer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private bool _isNavigationCollapsed;
        private List<Collection> _collection;
        private PlayListMode _playListMode;

        public bool IsNavigationCollapsed
        {
            get => _isNavigationCollapsed;
            set
            {
                _isNavigationCollapsed = value;
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
