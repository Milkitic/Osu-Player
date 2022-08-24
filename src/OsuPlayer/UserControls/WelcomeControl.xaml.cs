using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.UiComponents.FrontDialogComponent;
using Milki.OsuPlayer.Utils;
using Milki.OsuPlayer.Wpf.Command;

namespace Milki.OsuPlayer.UserControls;

public class WelcomeControlVm : VmBase
{
    private bool _guideSyncing;
    private bool _guideSelectedDb;

    public bool GuideSyncing
    {
        get => _guideSyncing;
        set => this.RaiseAndSetIfChanged(ref _guideSyncing, value);
    }

    public bool GuideSelectedDb
    {
        get => _guideSelectedDb;
        set => this.RaiseAndSetIfChanged(ref _guideSelectedDb, value);
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
                    MessageBox.Show(App.CurrentMainWindow!,
                        $"{I18NUtil.GetString("err-osudb-sync")}: {path}\r\n{ex.Message}",
                        App.CurrentMainWindow!.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    GuideSyncing = false;
                }

                if (isSuccess)
                {
                    await using var dbContext = ServiceProviders.GetApplicationDbContext();
                    var softwareState = await dbContext.GetSoftwareState();
                    softwareState.ShowWelcome = false;
                    await dbContext.UpdateAndSaveChangesAsync(softwareState, k => k.ShowWelcome);

                    App.CurrentMainContentDialog.RaiseOk();
                }

            });
        }
    }

    public ICommand SkipCommand
    {
        get
        {
            return new DelegateCommand(async arg =>
            {
                App.CurrentMainContentDialog.RaiseCancel();

                await using var dbContext = ServiceProviders.GetApplicationDbContext();
                var softwareState = await dbContext.GetSoftwareState();
                softwareState.ShowWelcome = false;
                await dbContext.UpdateAndSaveChangesAsync(softwareState, k => k.ShowWelcome);
            });
        }
    }
}

/// <summary>
/// WelcomeControl.xaml 的交互逻辑
/// </summary>
public partial class WelcomeControl : UserControl
{
    private readonly WelcomeControlVm _viewModel;

    public WelcomeControl()
    {
        InitializeComponent();
        DataContext = _viewModel = new WelcomeControlVm();
    }
}