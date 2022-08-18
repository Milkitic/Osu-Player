using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Anotar.NLog;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.UiComponents.FrontDialogComponent;
using Milki.OsuPlayer.UiComponents.NotificationComponent;
using Milki.OsuPlayer.Utils;
using Milki.OsuPlayer.Wpf.Command;

namespace Milki.OsuPlayer.UserControls;

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
                var syncService = ServiceProviders.Default.GetService<BeatmapSyncService>()!;
                var result = CommonUtils.BrowseDb(out var path);
                if (!result.HasValue || !result.Value)
                {
                    GuideSelectedDb = false;
                    return;
                }

                bool isSuccess = false;
                GuideSyncing = true;
                try
                {
                    await syncService.SyncOsuDbAsync(path);
                    GuideSelectedDb = true;
                    isSuccess = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(App.Current.MainWindow!,
                        $"{I18NUtil.GetString("err-osudb-sync")}: {path}\r\n{ex.Message}",
                        App.Current.MainWindow!.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    GuideSyncing = false;
                }

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
                    LogTo.ErrorException($"Error while syncing osu!db: {path}", ex);
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