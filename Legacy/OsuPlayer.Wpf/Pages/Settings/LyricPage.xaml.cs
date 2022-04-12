﻿using Milky.OsuPlayer.Instances;
using Milky.OsuPlayer.Media.Lyric;
using Milky.OsuPlayer.Presentation;
using Milky.OsuPlayer.Shared;
using Milky.OsuPlayer.Windows;
using System.Windows;
using System.Windows.Controls;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Shared.Dependency;

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
            _mainWindow = WindowEx.GetCurrentFirst<MainWindow>();
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            EnableLyric.IsChecked = AppSettings.Default.Lyric.EnableLyric;
            if (AppSettings.Default.Lyric.LyricSource == LyricSource.Auto)
                SourceAuto.IsChecked = true;
            else if (AppSettings.Default.Lyric.LyricSource == LyricSource.Netease)
                SourceNetease.IsChecked = true;
            else if (AppSettings.Default.Lyric.LyricSource == LyricSource.Kugou)
                SourceKugou.IsChecked = true;
            else if (AppSettings.Default.Lyric.LyricSource == LyricSource.QqMusic)
                SourceQqMusic.IsChecked = true;
            if (AppSettings.Default.Lyric.ProvideType == LyricProvideType.PreferBoth)
                ShowAll.IsChecked = true;
            else if (AppSettings.Default.Lyric.ProvideType == LyricProvideType.Original)
                ShowOrigin.IsChecked = true;
            else if (AppSettings.Default.Lyric.ProvideType == LyricProvideType.PreferTranslated)
                ShowTrans.IsChecked = true;
            StrictMode.IsChecked = AppSettings.Default.Lyric.StrictMode;
            EnableCache.IsChecked = AppSettings.Default.Lyric.EnableCache;
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
                AppSettings.Default.Lyric.LyricSource = LyricSource.Auto;
            else if (SourceNetease.IsChecked.HasValue && SourceNetease.IsChecked.Value)
                AppSettings.Default.Lyric.LyricSource = LyricSource.Netease;
            else if (SourceKugou.IsChecked.HasValue && SourceKugou.IsChecked.Value)
                AppSettings.Default.Lyric.LyricSource = LyricSource.Kugou;
            else if (SourceQqMusic.IsChecked.HasValue && SourceQqMusic.IsChecked.Value)
                AppSettings.Default.Lyric.LyricSource = LyricSource.QqMusic;
            ReloadLyric();
            AppSettings.SaveDefault();
        }

        private void Show_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            if (ShowAll.IsChecked.HasValue && ShowAll.IsChecked.Value)
                AppSettings.Default.Lyric.ProvideType = LyricProvideType.PreferBoth;
            else if (ShowOrigin.IsChecked.HasValue && ShowOrigin.IsChecked.Value)
                AppSettings.Default.Lyric.ProvideType = LyricProvideType.Original;
            else if (ShowTrans.IsChecked.HasValue && ShowTrans.IsChecked.Value)
                AppSettings.Default.Lyric.ProvideType = LyricProvideType.PreferTranslated;
            ReloadLyric();
            AppSettings.SaveDefault();
        }

        private void StrictMode_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            if (StrictMode.IsChecked.HasValue && StrictMode.IsChecked.Value)
                AppSettings.Default.Lyric.StrictMode = true;
            else
                AppSettings.Default.Lyric.StrictMode = false;
            ReloadLyric();
            AppSettings.SaveDefault();
        }

        private void EnableCache_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            if (EnableCache.IsChecked.HasValue && EnableCache.IsChecked.Value)
                AppSettings.Default.Lyric.EnableCache = true;
            else
                AppSettings.Default.Lyric.EnableCache = false;
            ReloadLyric();
            AppSettings.SaveDefault();
        }

        private void ReloadLyric()
        {
            Service.Get<LyricsInst>().ReloadLyricProvider();
            _mainWindow.SetLyricSynchronously();
        }
    }
}
