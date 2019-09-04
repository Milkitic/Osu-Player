using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Metadata;
using Milky.OsuPlayer.Windows;
using System.Windows;
using System.Windows.Controls;
using Milky.OsuPlayer.Instances;
using Milky.WpfApi;

namespace Milky.OsuPlayer.Pages.Settings
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
            _mainWindow = WindowBase.GetCurrentFirst<MainWindow>();
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            EnableLyric.IsChecked = AppSettings.Current.Lyric.EnableLyric;
            if (AppSettings.Current.Lyric.LyricSource == LyricSource.Auto)
                SourceAuto.IsChecked = true;
            else if (AppSettings.Current.Lyric.LyricSource == LyricSource.Netease)
                SourceNetease.IsChecked = true;
            else if (AppSettings.Current.Lyric.LyricSource == LyricSource.Kugou)
                SourceKugou.IsChecked = true;
            else if (AppSettings.Current.Lyric.LyricSource == LyricSource.QqMusic)
                SourceQqMusic.IsChecked = true;
            if (AppSettings.Current.Lyric.ProvideType == LyricProvideType.PreferBoth)
                ShowAll.IsChecked = true;
            else if (AppSettings.Current.Lyric.ProvideType == LyricProvideType.Original)
                ShowOrigin.IsChecked = true;
            else if (AppSettings.Current.Lyric.ProvideType == LyricProvideType.PreferTranslated)
                ShowTrans.IsChecked = true;
            StrictMode.IsChecked = AppSettings.Current.Lyric.StrictMode;
            EnableCache.IsChecked = AppSettings.Current.Lyric.EnableCache;
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
                AppSettings.Current.Lyric.LyricSource = LyricSource.Auto;
            else if (SourceNetease.IsChecked.HasValue && SourceNetease.IsChecked.Value)
                AppSettings.Current.Lyric.LyricSource = LyricSource.Netease;
            else if (SourceKugou.IsChecked.HasValue && SourceKugou.IsChecked.Value)
                AppSettings.Current.Lyric.LyricSource = LyricSource.Kugou;
            else if (SourceQqMusic.IsChecked.HasValue && SourceQqMusic.IsChecked.Value)
                AppSettings.Current.Lyric.LyricSource = LyricSource.QqMusic;
            ReloadLyric();
            AppSettings.SaveCurrent();
        }

        private void Show_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            if (ShowAll.IsChecked.HasValue && ShowAll.IsChecked.Value)
                AppSettings.Current.Lyric.ProvideType = LyricProvideType.PreferBoth;
            else if (ShowOrigin.IsChecked.HasValue && ShowOrigin.IsChecked.Value)
                AppSettings.Current.Lyric.ProvideType = LyricProvideType.Original;
            else if (ShowTrans.IsChecked.HasValue && ShowTrans.IsChecked.Value)
                AppSettings.Current.Lyric.ProvideType = LyricProvideType.PreferTranslated;
            ReloadLyric();
            AppSettings.SaveCurrent();
        }

        private void StrictMode_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            if (StrictMode.IsChecked.HasValue && StrictMode.IsChecked.Value)
                AppSettings.Current.Lyric.StrictMode = true;
            else
                AppSettings.Current.Lyric.StrictMode = false;
            ReloadLyric();
            AppSettings.SaveCurrent();
        }

        private void EnableCache_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            if (EnableCache.IsChecked.HasValue && EnableCache.IsChecked.Value)
                AppSettings.Current.Lyric.EnableCache = true;
            else
                AppSettings.Current.Lyric.EnableCache = false;
            ReloadLyric();
            AppSettings.SaveCurrent();
        }

        private void ReloadLyric()
        {
            Services.Get<LyricsInst>().ReloadLyricProvider();
            _mainWindow.SetLyricSynchronously();
        }
    }
}
