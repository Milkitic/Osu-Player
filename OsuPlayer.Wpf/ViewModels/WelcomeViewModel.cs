using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Utils;
using Milky.WpfApi;
using Milky.WpfApi.Commands;
using System;
using System.Windows;
using System.Windows.Input;

namespace Milky.OsuPlayer.ViewModels
{
    public class WelcomeViewModel : ViewModelBase
    {
        private bool _guideSyncing;
        private bool _guideSelectedDb;
        private bool _showWelcome;

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

        public ICommand Step1Command
        {
            get
            {
                return new DelegateCommand(async arg =>
                {
                    var result = Util.BrowseDb(out var path);
                    if (!result.HasValue || !result.Value)
                    {
                        GuideSelectedDb = false;
                        return;
                    }

                    try
                    {
                        GuideSyncing = true;
                        await InstanceManage.GetInstance<OsuDbInst>().SyncOsuDbAsync(path, false);
                        GuideSyncing = false;
                    }
                    catch (Exception ex)
                    {
                        MsgBox.Show(ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
                        GuideSelectedDb = false;
                    }

                    GuideSelectedDb = true;
                });
            }
        }
        public ICommand SkipCommand
        {
            get
            {
                return new DelegateCommand(arg =>
                {
                    ShowWelcome = false;
                    PlayerConfig.Current.General.FirstOpen = false;
                    PlayerConfig.SaveCurrent();
                });
            }
        }

        public bool ShowWelcome
        {
            get => _showWelcome;
            set
            {
                _showWelcome = value;
                OnPropertyChanged();
            }
        }

    }
}