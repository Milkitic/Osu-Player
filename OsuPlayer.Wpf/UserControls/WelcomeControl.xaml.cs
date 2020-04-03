using System;
using System.Windows.Controls;
using System.Windows.Input;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Shared.Dependency;
using Milky.OsuPlayer.UiComponents.FrontDialogComponent;
using Milky.OsuPlayer.UiComponents.NotificationComponent;

namespace Milky.OsuPlayer.UserControls
{
    public class WelcomeControlVm : VmBase
    {
        private bool _guideSyncing;
        private bool _guideSelectedDb;
        private bool _showWelcome;

        public bool GuideSyncing
        {
            get => _guideSyncing;
            set
            {
                if (_guideSyncing == value) return;
                _guideSyncing = value;
                OnPropertyChanged();
            }
        }

        public bool GuideSelectedDb
        {
            get => _guideSelectedDb;
            set
            {
                if (_guideSelectedDb == value) return;
                _guideSelectedDb = value;
                OnPropertyChanged();
            }
        }

        public ICommand SelectDbCommand
        {
            get
            {
                return new DelegateCommand(async arg =>
                {
                    var result = CommonUtils.BrowseDb(out var path);
                    if (!result.HasValue || !result.Value)
                    {
                        GuideSelectedDb = false;
                        return;
                    }

                    try
                    {
                        GuideSyncing = true;
                        await Service.Get<OsuDbInst>().SyncOsuDbAsync(path, false);
                        AppSettings.Default.General.DbPath = path;
                        AppSettings.SaveDefault();
                        GuideSyncing = false;
                        GuideSelectedDb = true;
                    }
                    catch (Exception ex)
                    {
                        Notification.Push(ex.Message);
                        GuideSelectedDb = false;
                    }

                    SkipCommand.Execute(null);
                    GuideSyncing = false;
                });
            }
        }

        public ICommand SkipCommand
        {
            get
            {
                return new DelegateCommand(arg =>
                {
                    //ShowWelcome = false;
                    FrontDialogOverlay.Default.RaiseCancel();
                    AppSettings.Default.General.FirstOpen = false;
                    AppSettings.SaveDefault();
                });
            }
        }
    }

    /// <summary>
    /// WelcomeControl.xaml 的交互逻辑
    /// </summary>
    public partial class WelcomeControl : UserControl
    {
        public WelcomeControlVm ViewModel { get; }

        public WelcomeControl()
        {
            InitializeComponent();
            ViewModel = (WelcomeControlVm)WelcomeArea.DataContext;
        }
    }
}