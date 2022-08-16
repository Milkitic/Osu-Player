using System;
using System.Windows.Controls;
using System.Windows.Input;
using Milki.OsuPlayer.Common;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Presentation.Interaction;
using Milki.OsuPlayer.Shared.Dependency;
using Milki.OsuPlayer.UiComponents.FrontDialogComponent;
using Milki.OsuPlayer.UiComponents.NotificationComponent;

namespace Milki.OsuPlayer.UserControls
{
    public class WelcomeControlVm : VmBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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

                    bool isSuccess = false;
                    try
                    {
                        GuideSyncing = true;
                        await Service.Get<OsuDbInst>().SyncOsuDbAsync(path, false);
                        AppSettings.Default.GeneralSection.DbPath = path;
                        AppSettings.SaveDefault();
                        GuideSyncing = false;
                        GuideSelectedDb = true;
                        isSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Error while syncing osu!db: {0}", path);
                        Notification.Push("Error while syncing osu!db: " + path + "\r\n" + ex.Message);
                        GuideSelectedDb = false;
                    }

                    if (isSuccess)
                    {
                        AppSettings.Default.GeneralSection.FirstOpen = false;
                        AppSettings.SaveDefault();
                        FrontDialogOverlay.Default.RaiseOk();
                    }

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
                    AppSettings.Default.GeneralSection.FirstOpen = false;
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