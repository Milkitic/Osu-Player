using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared;
using Milki.OsuPlayer.Shared.Utils;
using Milki.OsuPlayer.Utils;
using Milki.OsuPlayer.Windows;

namespace Milki.OsuPlayer.Pages.Settings;

/// <summary>
/// AboutPage.xaml 的交互逻辑
/// </summary>
public partial class AboutPage : Page
{
    private readonly MainWindow _mainWindow;
    private readonly ConfigWindow _configWindow;
    private NewVersionWindow _newVersionWindow;

    private readonly UpdateService _updateService;

    public AboutPage()
    {
        _mainWindow = App.Current.Windows.OfType<MainWindow>().First();
        _configWindow = App.Current.Windows.OfType<ConfigWindow>().First();
        _updateService = App.Current.ServiceProvider.GetService<UpdateService>();

        InitializeComponent();
    }

    private void LinkGithub_Click(object sender, RoutedEventArgs e)
    {
        ProcessUtils.StartWithShellExecute("https://github.com/Milkitic/Osu-Player");
    }

    private void LinkFeedback_Click(object sender, RoutedEventArgs e)
    {
        ProcessUtils.StartWithShellExecute("https://github.com/Milkitic/Osu-Player/discussions");
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        CurrentVer.Content = _updateService.GetVersion();
        if (_updateService.NewRelease != null)
        {
            NewVersion.Visibility = Visibility.Visible;
        }

        await GetLastUpdate();
    }

    private async ValueTask GetLastUpdate()
    {
        await using var dbContext = ServiceProviders.GetApplicationDbContext();
        var softwareState = await dbContext.GetSoftwareState();
        if (softwareState.LastUpdateCheck == null)
        {
            LastUpdate.Content = I18NUtil.GetString("ui-sets-content-never");
        }
        else
        {
            LastUpdate.Content = softwareState.LastUpdateCheck.Value.ToString(Constants.DateTimeFormat);
        }
    }

    private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        //todo: action
        CheckUpdate.IsEnabled = false;
        bool? hasNew;
        try
        {
            hasNew = await _updateService.CheckUpdateAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(_configWindow, I18NUtil.GetString("ui-sets-content-errorWhileCheckingUpdate") + Environment.NewLine +
                                           (ex.InnerException?.Message ?? ex.Message),
                _configWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        CheckUpdate.IsEnabled = true;

        await using var dbContext = ServiceProviders.GetApplicationDbContext();
        var softwareState = await dbContext.GetSoftwareState();
        softwareState.LastUpdateCheck = DateTime.Now;
        await GetLastUpdate();
        AppSettings.SaveDefault();
        if (hasNew == true)
        {
            NewVersion.Visibility = Visibility.Visible;
            NewVersion_Click(sender, e);
        }
        else
        {
            MessageBox.Show(_configWindow, I18NUtil.GetString("ui-sets-content-alreadyNewest"), _configWindow.Title,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void NewVersion_Click(object sender, RoutedEventArgs e)
    {
        if (_newVersionWindow is { IsClosed: false })
        {
            _newVersionWindow.Close();
        }

        _newVersionWindow = new NewVersionWindow(_updateService.NewRelease, _mainWindow);
        _newVersionWindow.ShowDialog();
    }

    private void LinkLicense_Click(object sender, RoutedEventArgs e)
    {
        Process.Start("https://github.com/Milkitic/Osu-Player/blob/master/LICENSE");
    }

    private void LinkPrivacy_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("This software will NOT collect any user information.");
    }
}