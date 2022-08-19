using System.Diagnostics;
using System.Windows;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Shared.Models;
using Milki.OsuPlayer.Wpf;

namespace Milki.OsuPlayer.Windows;

/// <summary>
/// NewVersionWindow.xaml 的交互逻辑
/// </summary>
public partial class NewVersionWindow : WindowEx
{
    private readonly GithubRelease _release;
    private readonly MainWindow _mainWindow;

    public NewVersionWindow(GithubRelease release, MainWindow mainWindow)
    {
        _release = release;
        _mainWindow = mainWindow;
        InitializeComponent();
        MainGrid.DataContext = _release;
    }

    private void Update_Click(object sender, RoutedEventArgs e)
    {
        var updateWindow = new UpdateWindow(_release, _mainWindow);
        updateWindow.Show();
        Close();
    }

    private void HtmlUrl_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(_release.HtmlUrl);
    }

    private async void Skip_Click(object sender, RoutedEventArgs e)
    {
        var dbContext = ServiceProviders.GetApplicationDbContext();
        var softwareState = await dbContext.GetSoftwareState();
        softwareState.IgnoredVersion = _release.NewVerString;
        AppSettings.SaveDefault();
        Close();
    }

    private void Later_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}