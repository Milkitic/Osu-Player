using Milky.OsuPlayer.Windows;
using System.Windows;
using System.Windows.Controls;
using Milky.OsuPlayer.Common.Data;

namespace Milky.OsuPlayer.Pages.Settings
{
    /// <summary>
    /// LyricPage.xaml 的交互逻辑
    /// </summary>
    public partial class LyricPage : Page
    {
        private readonly MainWindow _mainWindow;
        private bool _loaded;
        public LyricPage(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            EnableLyric.IsChecked = App.Config.Lyric.EnableLyric;
            if (App.Config.Lyric.LyricSource == LyricSource.Auto)
                SourceAuto.IsChecked = true;
            else if (App.Config.Lyric.LyricSource == LyricSource.Netease)
                SourceNetease.IsChecked = true;
            else if (App.Config.Lyric.LyricSource == LyricSource.Kugou)
                SourceKugou.IsChecked = true;
            else if (App.Config.Lyric.LyricSource == LyricSource.QqMusic)
                SourceQqMusic.IsChecked = true;
            if (App.Config.Lyric.ProvideType == LyricProvideType.PreferBoth)
                ShowAll.IsChecked = true;
            else if (App.Config.Lyric.ProvideType == LyricProvideType.Original)
                ShowOrigin.IsChecked = true;
            else if (App.Config.Lyric.ProvideType == LyricProvideType.PreferTranslated)
                ShowTrans.IsChecked = true;
            StrictMode.IsChecked = App.Config.Lyric.StrictMode;
            EnableCache.IsChecked = App.Config.Lyric.EnableCache;
            _loaded = true;
        }

        private void EnableLyric_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            if (EnableLyric.IsChecked.HasValue && EnableLyric.IsChecked.Value)
            {
                App.Config.Lyric.EnableLyric = true;
                _mainWindow.LyricWindow.Show();
            }
            else
            {
                App.Config.Lyric.EnableLyric = false;
                _mainWindow.LyricWindow.Hide();
            }

            App.SaveConfig();
        }

        private void Source_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            if (SourceAuto.IsChecked.HasValue && SourceAuto.IsChecked.Value)
                App.Config.Lyric.LyricSource = LyricSource.Auto;
            else if (SourceNetease.IsChecked.HasValue && SourceNetease.IsChecked.Value)
                App.Config.Lyric.LyricSource = LyricSource.Netease;
            else if (SourceKugou.IsChecked.HasValue && SourceKugou.IsChecked.Value)
                App.Config.Lyric.LyricSource = LyricSource.Kugou;
            else if (SourceQqMusic.IsChecked.HasValue && SourceQqMusic.IsChecked.Value)
                App.Config.Lyric.LyricSource = LyricSource.QqMusic;
            ReloadLyric();
            App.SaveConfig();
        }

        private void Show_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            if (ShowAll.IsChecked.HasValue && ShowAll.IsChecked.Value)
                App.Config.Lyric.ProvideType = LyricProvideType.PreferBoth;
            else if (ShowOrigin.IsChecked.HasValue && ShowOrigin.IsChecked.Value)
                App.Config.Lyric.ProvideType = LyricProvideType.Original;
            else if (ShowTrans.IsChecked.HasValue && ShowTrans.IsChecked.Value)
                App.Config.Lyric.ProvideType = LyricProvideType.PreferTranslated;
            ReloadLyric();
            App.SaveConfig();
        }

        private void StrictMode_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            if (StrictMode.IsChecked.HasValue && StrictMode.IsChecked.Value)
                App.Config.Lyric.StrictMode = true;
            else
                App.Config.Lyric.StrictMode = false;
            ReloadLyric();
            App.SaveConfig();
        }

        private void EnableCache_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            if (EnableCache.IsChecked.HasValue && EnableCache.IsChecked.Value)
                App.Config.Lyric.EnableCache = true;
            else
                App.Config.Lyric.EnableCache = false;
            ReloadLyric();
            App.SaveConfig();
        }

        private void ReloadLyric()
        {
            App.ReloadLyricProvider();
            _mainWindow.SetLyric();
        }
    }
}
