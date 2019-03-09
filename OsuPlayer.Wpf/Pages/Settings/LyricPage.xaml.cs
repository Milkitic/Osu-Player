using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Metadata;
using Milky.OsuPlayer.Windows;
using System.Windows;
using System.Windows.Controls;
using Milky.OsuPlayer.Instances;

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
            EnableLyric.IsChecked = PlayerConfig.Current.Lyric.EnableLyric;
            if (PlayerConfig.Current.Lyric.LyricSource == LyricSource.Auto)
                SourceAuto.IsChecked = true;
            else if (PlayerConfig.Current.Lyric.LyricSource == LyricSource.Netease)
                SourceNetease.IsChecked = true;
            else if (PlayerConfig.Current.Lyric.LyricSource == LyricSource.Kugou)
                SourceKugou.IsChecked = true;
            else if (PlayerConfig.Current.Lyric.LyricSource == LyricSource.QqMusic)
                SourceQqMusic.IsChecked = true;
            if (PlayerConfig.Current.Lyric.ProvideType == LyricProvideType.PreferBoth)
                ShowAll.IsChecked = true;
            else if (PlayerConfig.Current.Lyric.ProvideType == LyricProvideType.Original)
                ShowOrigin.IsChecked = true;
            else if (PlayerConfig.Current.Lyric.ProvideType == LyricProvideType.PreferTranslated)
                ShowTrans.IsChecked = true;
            StrictMode.IsChecked = PlayerConfig.Current.Lyric.StrictMode;
            EnableCache.IsChecked = PlayerConfig.Current.Lyric.EnableCache;
            _loaded = true;
        }

        private void EnableLyric_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            if (EnableLyric.IsChecked.HasValue && EnableLyric.IsChecked.Value)
            {
                PlayerConfig.Current.Lyric.EnableLyric = true;
                _mainWindow.LyricWindow.Show();
            }
            else
            {
                PlayerConfig.Current.Lyric.EnableLyric = false;
                _mainWindow.LyricWindow.Hide();
            }

            PlayerConfig.SaveCurrent();
        }

        private void Source_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            if (SourceAuto.IsChecked.HasValue && SourceAuto.IsChecked.Value)
                PlayerConfig.Current.Lyric.LyricSource = LyricSource.Auto;
            else if (SourceNetease.IsChecked.HasValue && SourceNetease.IsChecked.Value)
                PlayerConfig.Current.Lyric.LyricSource = LyricSource.Netease;
            else if (SourceKugou.IsChecked.HasValue && SourceKugou.IsChecked.Value)
                PlayerConfig.Current.Lyric.LyricSource = LyricSource.Kugou;
            else if (SourceQqMusic.IsChecked.HasValue && SourceQqMusic.IsChecked.Value)
                PlayerConfig.Current.Lyric.LyricSource = LyricSource.QqMusic;
            ReloadLyric();
            PlayerConfig.SaveCurrent();
        }

        private void Show_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            if (ShowAll.IsChecked.HasValue && ShowAll.IsChecked.Value)
                PlayerConfig.Current.Lyric.ProvideType = LyricProvideType.PreferBoth;
            else if (ShowOrigin.IsChecked.HasValue && ShowOrigin.IsChecked.Value)
                PlayerConfig.Current.Lyric.ProvideType = LyricProvideType.Original;
            else if (ShowTrans.IsChecked.HasValue && ShowTrans.IsChecked.Value)
                PlayerConfig.Current.Lyric.ProvideType = LyricProvideType.PreferTranslated;
            ReloadLyric();
            PlayerConfig.SaveCurrent();
        }

        private void StrictMode_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            if (StrictMode.IsChecked.HasValue && StrictMode.IsChecked.Value)
                PlayerConfig.Current.Lyric.StrictMode = true;
            else
                PlayerConfig.Current.Lyric.StrictMode = false;
            ReloadLyric();
            PlayerConfig.SaveCurrent();
        }

        private void EnableCache_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            if (EnableCache.IsChecked.HasValue && EnableCache.IsChecked.Value)
                PlayerConfig.Current.Lyric.EnableCache = true;
            else
                PlayerConfig.Current.Lyric.EnableCache = false;
            ReloadLyric();
            PlayerConfig.SaveCurrent();
        }

        private void ReloadLyric()
        {
            InstanceManage.GetInstance<LyricsInst>().ReloadLyricProvider();
            _mainWindow.SetLyric();
        }
    }
}
