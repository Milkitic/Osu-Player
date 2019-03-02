using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Milky.OsuPlayer.Common.Data;

namespace Milky.OsuPlayer.Pages.Settings
{
    /// <summary>
    /// ExportPage.xaml 的交互逻辑
    /// </summary>
    public partial class ExportPage : Page
    {
        public ExportPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LblMp3Path.Text = App.Config.Export.MusicPath;
            LblBgPath.Text = App.Config.Export.BgPath;
            if (App.Config.Export.NamingStyle == NamingStyle.Title)
                RadioT.IsChecked = true;
            else if (App.Config.Export.NamingStyle == NamingStyle.ArtistTitle)
                RadioAt.IsChecked = true;
            else if (App.Config.Export.NamingStyle == NamingStyle.TitleArtist)
                RadioTa.IsChecked = true;
            if (App.Config.Export.SortStyle == SortStyle.None)
                SortNone.IsChecked = true;
            else if (App.Config.Export.SortStyle == SortStyle.Artist)
                SortArtist.IsChecked = true;
            else if (App.Config.Export.SortStyle == SortStyle.Mapper)
                SortMapper.IsChecked = true;
            else if (App.Config.Export.SortStyle == SortStyle.Source)
                SortSource.IsChecked = true;
        }

        private void BtnMp3Path_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog
            {
                Description = @"选择音乐导出目录",
                ShowNewFolderButton = true,
            };
            var result = dialog.ShowDialog();
            if (result != DialogResult.OK) return;
            App.Config.Export.MusicPath = dialog.SelectedPath;
            LblMp3Path.Text = App.Config.Export.MusicPath;
            App.SaveConfig();
        }

        private void BtnBgPath_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog
            {
                Description = @"选择背景导出目录",
                ShowNewFolderButton = true,
            };
            var result = dialog.ShowDialog();
            if (result != DialogResult.OK) return;
            App.Config.Export.BgPath = dialog.SelectedPath;
            LblBgPath.Text = App.Config.Export.BgPath;
            App.SaveConfig();
        }

        private void Naming_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (RadioT.IsChecked.HasValue && RadioT.IsChecked.Value)
                App.Config.Export.NamingStyle = NamingStyle.Title;
            else if (RadioAt.IsChecked.HasValue && RadioAt.IsChecked.Value)
                App.Config.Export.NamingStyle = NamingStyle.ArtistTitle;
            else if (RadioTa.IsChecked.HasValue && RadioTa.IsChecked.Value)
                App.Config.Export.NamingStyle = NamingStyle.TitleArtist;
            App.SaveConfig();
        }

        private void Sort_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (SortNone.IsChecked.HasValue && SortNone.IsChecked.Value)
                App.Config.Export.SortStyle = SortStyle.None;
            else if (SortArtist.IsChecked.HasValue && SortArtist.IsChecked.Value)
                App.Config.Export.SortStyle = SortStyle.Artist;
            else if (SortMapper.IsChecked.HasValue && SortMapper.IsChecked.Value)
                App.Config.Export.SortStyle = SortStyle.Mapper;
            else if (SortSource.IsChecked.HasValue && SortSource.IsChecked.Value)
                App.Config.Export.SortStyle = SortStyle.Source;
            App.SaveConfig();
        }
    }
}
