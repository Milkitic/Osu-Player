using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Models;
using Milki.OsuPlayer.Windows;

namespace Milki.OsuPlayer.Pages.Settings;

/// <summary>
/// LyricPage.xaml 的交互逻辑
/// </summary>
public partial class LyricPage : Page
{
    private readonly MainWindow _mainWindow;
    private bool _loaded;
    public LyricPage()
    {
        _mainWindow = (MainWindow)App.CurrentMainWindow;
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        EnableLyric.IsChecked = AppSettings.Default.LyricSection.IsDesktopLyricEnabled;
        if (AppSettings.Default.LyricSection.LyricSource == LyricSource.Auto)
            SourceAuto.IsChecked = true;
        else if (AppSettings.Default.LyricSection.LyricSource == LyricSource.Netease)
            SourceNetease.IsChecked = true;
        else if (AppSettings.Default.LyricSection.LyricSource == LyricSource.Kugou)
            SourceKugou.IsChecked = true;
        else if (AppSettings.Default.LyricSection.LyricSource == LyricSource.QqMusic)
            SourceQqMusic.IsChecked = true;
        if (AppSettings.Default.LyricSection.LyricProvideType == LyricProvideType.PreferBoth)
            ShowAll.IsChecked = true;
        else if (AppSettings.Default.LyricSection.LyricProvideType == LyricProvideType.Original)
            ShowOrigin.IsChecked = true;
        else if (AppSettings.Default.LyricSection.LyricProvideType == LyricProvideType.PreferTranslated)
            ShowTrans.IsChecked = true;
        StrictMode.IsChecked = AppSettings.Default.LyricSection.IsStrictModeEnabled;
        EnableCache.IsChecked = AppSettings.Default.LyricSection.IsCacheEnabled;
        _loaded = true;
    }

    private void EnableLyric_CheckChanged(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        if (EnableLyric.IsChecked.HasValue && EnableLyric.IsChecked.Value)
        {
            SharedVm.Default.IsLyricWindowEnabled = true;
        }
        else
        {
            SharedVm.Default.IsLyricWindowEnabled = false;
        }
    }

    private void Source_CheckChanged(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        if (SourceAuto.IsChecked.HasValue && SourceAuto.IsChecked.Value)
            AppSettings.Default.LyricSection.LyricSource = LyricSource.Auto;
        else if (SourceNetease.IsChecked.HasValue && SourceNetease.IsChecked.Value)
            AppSettings.Default.LyricSection.LyricSource = LyricSource.Netease;
        else if (SourceKugou.IsChecked.HasValue && SourceKugou.IsChecked.Value)
            AppSettings.Default.LyricSection.LyricSource = LyricSource.Kugou;
        else if (SourceQqMusic.IsChecked.HasValue && SourceQqMusic.IsChecked.Value)
            AppSettings.Default.LyricSection.LyricSource = LyricSource.QqMusic;
        ReloadLyric();
        AppSettings.SaveDefault();
    }

    private void Show_CheckChanged(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        if (ShowAll.IsChecked.HasValue && ShowAll.IsChecked.Value)
            AppSettings.Default.LyricSection.LyricProvideType = LyricProvideType.PreferBoth;
        else if (ShowOrigin.IsChecked.HasValue && ShowOrigin.IsChecked.Value)
            AppSettings.Default.LyricSection.LyricProvideType = LyricProvideType.Original;
        else if (ShowTrans.IsChecked.HasValue && ShowTrans.IsChecked.Value)
            AppSettings.Default.LyricSection.LyricProvideType = LyricProvideType.PreferTranslated;
        ReloadLyric();
        AppSettings.SaveDefault();
    }

    private void StrictMode_CheckChanged(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        if (StrictMode.IsChecked.HasValue && StrictMode.IsChecked.Value)
            AppSettings.Default.LyricSection.IsStrictModeEnabled = true;
        else
            AppSettings.Default.LyricSection.IsStrictModeEnabled = false;
        ReloadLyric();
        AppSettings.SaveDefault();
    }

    private void EnableCache_CheckChanged(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        if (EnableCache.IsChecked.HasValue && EnableCache.IsChecked.Value)
            AppSettings.Default.LyricSection.IsCacheEnabled = true;
        else
            AppSettings.Default.LyricSection.IsCacheEnabled = false;
        ReloadLyric();
        AppSettings.SaveDefault();
    }

    private void ReloadLyric()
    {
        var lyricService = ServiceProviders.Default.GetService<LyricsService>()!;
        lyricService.SetLyricSynchronously(null);
    }
}