using System.Windows;
using System.Windows.Controls;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Instances;
using Milki.OsuPlayer.Media.Lyric;
using Milki.OsuPlayer.Presentation;
using Milki.OsuPlayer.Shared.Dependency;
using Milki.OsuPlayer.Windows;

namespace Milki.OsuPlayer.Pages.Settings
{
    /// <summary>
    /// LyricPage.xaml 的交互逻辑
    /// </summary>
    public partial class LyricPage : Page
    {
        private readonly MainWindow _mainWindow;
        private bool _loaded;
        public LyricPage()
        {
            _mainWindow = WindowEx.GetCurrentFirst<MainWindow>();
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            EnableLyric.IsChecked = AppSettings.Default.LyricSection.EnableLyric;
            if (AppSettings.Default.LyricSection.LyricSource == LyricSource.Auto)
                SourceAuto.IsChecked = true;
            else if (AppSettings.Default.LyricSection.LyricSource == LyricSource.Netease)
                SourceNetease.IsChecked = true;
            else if (AppSettings.Default.LyricSection.LyricSource == LyricSource.Kugou)
                SourceKugou.IsChecked = true;
            else if (AppSettings.Default.LyricSection.LyricSource == LyricSource.QqMusic)
                SourceQqMusic.IsChecked = true;
            if (AppSettings.Default.LyricSection.ProvideType == LyricProvideType.PreferBoth)
                ShowAll.IsChecked = true;
            else if (AppSettings.Default.LyricSection.ProvideType == LyricProvideType.Original)
                ShowOrigin.IsChecked = true;
            else if (AppSettings.Default.LyricSection.ProvideType == LyricProvideType.PreferTranslated)
                ShowTrans.IsChecked = true;
            StrictMode.IsChecked = AppSettings.Default.LyricSection.StrictMode;
            EnableCache.IsChecked = AppSettings.Default.LyricSection.EnableCache;
            _loaded = true;
        }

        private void EnableLyric_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            if (EnableLyric.IsChecked.HasValue && EnableLyric.IsChecked.Value)
            {
                _mainWindow.LyricWindow.Show();
            }
            else
            {
                _mainWindow.LyricWindow.Hide();
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
                AppSettings.Default.LyricSection.ProvideType = LyricProvideType.PreferBoth;
            else if (ShowOrigin.IsChecked.HasValue && ShowOrigin.IsChecked.Value)
                AppSettings.Default.LyricSection.ProvideType = LyricProvideType.Original;
            else if (ShowTrans.IsChecked.HasValue && ShowTrans.IsChecked.Value)
                AppSettings.Default.LyricSection.ProvideType = LyricProvideType.PreferTranslated;
            ReloadLyric();
            AppSettings.SaveDefault();
        }

        private void StrictMode_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            if (StrictMode.IsChecked.HasValue && StrictMode.IsChecked.Value)
                AppSettings.Default.LyricSection.StrictMode = true;
            else
                AppSettings.Default.LyricSection.StrictMode = false;
            ReloadLyric();
            AppSettings.SaveDefault();
        }

        private void EnableCache_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            if (EnableCache.IsChecked.HasValue && EnableCache.IsChecked.Value)
                AppSettings.Default.LyricSection.EnableCache = true;
            else
                AppSettings.Default.LyricSection.EnableCache = false;
            ReloadLyric();
            AppSettings.SaveDefault();
        }

        private void ReloadLyric()
        {
            Service.Get<LyricsService>().ReloadLyricProvider();
            _mainWindow.SetLyricSynchronously();
        }
    }
}
