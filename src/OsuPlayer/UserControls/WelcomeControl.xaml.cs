using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Milky.OsuPlayer.Core;
using Milky.OsuPlayer.Core.Configuration;
using Milky.OsuPlayer.Core.Instances;
using Milky.OsuPlayer.UiComponents.FrontDialogComponent;
using Milky.OsuPlayer.UiComponents.NotificationComponent;

namespace Milky.OsuPlayer.UserControls;

public partial class WelcomeControlVm : ObservableObject
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    [ObservableProperty]
    public partial bool GuideSyncing { get; set; }

    [ObservableProperty]
    public partial bool GuideSelectedDb { get; set; }

    [RelayCommand]
    private async Task SelectDbAsync()
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
            await App.Services.GetRequiredService<OsuDbInst>().SyncOsuDbAsync(path, false);
            AppSettings.Default.General.DbPath = path;
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
            AppSettings.Default.General.FirstOpen = false;
            AppSettings.SaveDefault();
            FrontDialogOverlay.Default.RaiseOk();
        }

        GuideSyncing = false;
    }

    [RelayCommand]
    private void Skip()
    {
        FrontDialogOverlay.Default.RaiseCancel();
        AppSettings.Default.General.FirstOpen = false;
        AppSettings.SaveDefault();
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