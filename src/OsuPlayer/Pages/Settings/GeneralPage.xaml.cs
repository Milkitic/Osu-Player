using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Anotar.NLog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Utils;
using Milki.OsuPlayer.Windows;

namespace Milki.OsuPlayer.Pages.Settings;

/// <summary>
/// GeneralPage.xaml 的交互逻辑
/// </summary>
public partial class GeneralPage : Page
{
    private readonly ConfigWindow _configWindow;
    private readonly BeatmapSyncService _syncService;
    private readonly OsuFileScanningService _osuFileScanningService;

    public GeneralPage()
    {
        _configWindow = App.Current.Windows.OfType<ConfigWindow>().First();
        _syncService = ServiceProviders.Default.GetService<BeatmapSyncService>();
        _osuFileScanningService = ServiceProviders.Default.GetService<OsuFileScanningService>()!;
        InitializeComponent();

        DataContext = _osuFileScanningService.ViewModel;
    }

    private void RunOnStartup_CheckChanged(object sender, RoutedEventArgs e)
    {
        var isRunOnStartup = RunOnStartup.IsChecked == true;

        using (var rKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"))
        {
            if (isRunOnStartup)
            {
                rKey?.SetValue("OsuPlayer", Process.GetCurrentProcess().MainModule?.FileName ?? "");
                AppSettings.Default.GeneralSection.RunOnStartup = true;
            }
            else
            {
                rKey?.DeleteValue("OsuPlayer", false);
                AppSettings.Default.GeneralSection.RunOnStartup = false;
            }
        }

        AppSettings.SaveDefault();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        RunOnStartup.IsChecked = AppSettings.Default.GeneralSection.RunOnStartup;
        TbDbPath.Text = AppSettings.Default.GeneralSection.DbPath;
        TbCustomPath.Text = AppSettings.Default.GeneralSection.CustomSongDir;

        if (AppSettings.Default.GeneralSection.ExitWhenClosed.HasValue)
        {
            if (AppSettings.Default.GeneralSection.ExitWhenClosed.Value)
                RadioExit.IsChecked = true;
            else
                RadioMinimum.IsChecked = true;
        }
        else
        {
            RadioMinimum.IsChecked = true;
            AsDefault.IsChecked = false;
        }
    }

    private void Radio_CheckChanged(object sender, RoutedEventArgs e)
    {
        if (RadioExit.IsChecked.HasValue && RadioExit.IsChecked.Value)
        {
            AppSettings.Default.GeneralSection.ExitWhenClosed = true;
        }
        else if (RadioMinimum.IsChecked.HasValue && RadioMinimum.IsChecked.Value)
        {
            AppSettings.Default.GeneralSection.ExitWhenClosed = false;
        }

        AsDefault.IsChecked = true;
        AppSettings.SaveDefault();
    }

    private async void BrowseDb_Click(object sender, RoutedEventArgs e)
    {
        var result = CommonUtils.BrowseDb(out var path);
        if (!result.HasValue || !result.Value)
        {
            return;
        }

        TbDbPath.Text = path;
        await SyncOsuDbAsync(path);
    }

    private void AsDefault_CheckChanged(object sender, RoutedEventArgs e)
    {
        if (AsDefault.IsChecked.HasValue && !AsDefault.IsChecked.Value)
            AppSettings.Default.GeneralSection.ExitWhenClosed = null;
        else
            Radio_CheckChanged(sender, e);
        AppSettings.SaveDefault();
    }

    private async void BrowseCustom_Click(object sender, RoutedEventArgs e)
    {
        using var openFileDialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true,
            Title = "Select Folder"
        };

        var result = openFileDialog.ShowDialog();
        if (result != CommonFileDialogResult.Ok)
        {
            return;
        }

        var path = openFileDialog.FileName;
        TbCustomPath.Text = path;
        await SyncCustomFolderAsync(path);
    }

    private async void CancelScan_Click(object sender, RoutedEventArgs e)
    {
        await _osuFileScanningService.CancelTaskAsync();
    }

    private async void SyncNow_Click(object sender, RoutedEventArgs e)
    {
        await SyncOsuDbAsync(AppSettings.Default.GeneralSection.DbPath);
    }

    private async void ScanNow_Click(object sender, RoutedEventArgs e)
    {
        await _osuFileScanningService.CancelTaskAsync();
        await SyncCustomFolderAsync(AppSettings.Default.GeneralSection.CustomSongDir);
    }

    private async Task SyncOsuDbAsync(string path)
    {
        try
        {
            await _syncService.SynchronizeManaged(_syncService.EnumeratePlayItemDetailsFormDb(path));

            AppSettings.Default.GeneralSection.DbPath = path;
            AppSettings.SaveDefault();

            await using var dbContext = new ApplicationDbContext();
            var softwareState = await dbContext.GetSoftwareState();

            softwareState.LastSync = DateTime.Now;
            await dbContext.UpdateAndSaveChangesAsync(softwareState, k => k.LastSync);
        }
        catch (Exception ex)
        {
            LogTo.ErrorException($"Error while syncing osu!db: {path}", ex);
            MessageBox.Show(_configWindow, $"{I18NUtil.GetString("err-osudb-sync")}: {path}\r\n{ex.Message}",
                _configWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task SyncCustomFolderAsync(string path)
    {
        try
        {
            await _osuFileScanningService.CancelTaskAsync();
            await _osuFileScanningService.NewScanAndAddAsync(path);

            AppSettings.Default.GeneralSection.CustomSongDir = path;
            AppSettings.SaveDefault();
        }
        catch (Exception ex)
        {
            LogTo.ErrorException($"Error while scanning custom folder: {path}", ex);
            MessageBox.Show(_configWindow, $"{I18NUtil.GetString("err-custom-scan")}: {path}\r\n{ex.Message}",
                _configWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}