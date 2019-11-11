using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Metadata;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;

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
            LblMp3Path.Text = AppSettings.Default.Export.MusicPath;
            LblBgPath.Text = AppSettings.Default.Export.BgPath;
            if (AppSettings.Default.Export.NamingStyle == NamingStyle.Title)
                RadioT.IsChecked = true;
            else if (AppSettings.Default.Export.NamingStyle == NamingStyle.ArtistTitle)
                RadioAt.IsChecked = true;
            else if (AppSettings.Default.Export.NamingStyle == NamingStyle.TitleArtist)
                RadioTa.IsChecked = true;
            if (AppSettings.Default.Export.SortStyle == SortStyle.None)
                SortNone.IsChecked = true;
            else if (AppSettings.Default.Export.SortStyle == SortStyle.Artist)
                SortArtist.IsChecked = true;
            else if (AppSettings.Default.Export.SortStyle == SortStyle.Mapper)
                SortMapper.IsChecked = true;
            else if (AppSettings.Default.Export.SortStyle == SortStyle.Source)
                SortSource.IsChecked = true;
        }

        private void BtnMp3Path_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "选择音乐导出目录",
            })
            {
                var result = dialog.ShowDialog();
                if (result != CommonFileDialogResult.Ok) return;
                AppSettings.Default.Export.MusicPath = dialog.FileName;
                LblMp3Path.Text = AppSettings.Default.Export.MusicPath;
                AppSettings.SaveDefault();
            }
        }

        private void BtnBgPath_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                Title = "选择背景导出目录"
            })
            {
                var result = dialog.ShowDialog();
                if (result != CommonFileDialogResult.Ok) return;
                AppSettings.Default.Export.BgPath = dialog.FileName;
                LblBgPath.Text = AppSettings.Default.Export.BgPath;
                AppSettings.SaveDefault();
            }
        }

        private void Naming_CheckChanged(object sender, RoutedEventArgs e)
        {
            var exportSection = AppSettings.Default.Export;
            if (RadioT.IsChecked.HasValue && RadioT.IsChecked.Value)
                exportSection.NamingStyle = NamingStyle.Title;
            else if (RadioAt.IsChecked.HasValue && RadioAt.IsChecked.Value)
                exportSection.NamingStyle = NamingStyle.ArtistTitle;
            else if (RadioTa.IsChecked.HasValue && RadioTa.IsChecked.Value)
                exportSection.NamingStyle = NamingStyle.TitleArtist;
            AppSettings.SaveDefault();
        }

        private void Sort_CheckChanged(object sender, RoutedEventArgs e)
        {
            var exportSection = AppSettings.Default.Export;
            if (SortNone.IsChecked.HasValue && SortNone.IsChecked.Value)
                exportSection.SortStyle = SortStyle.None;
            else if (SortArtist.IsChecked.HasValue && SortArtist.IsChecked.Value)
                exportSection.SortStyle = SortStyle.Artist;
            else if (SortMapper.IsChecked.HasValue && SortMapper.IsChecked.Value)
                exportSection.SortStyle = SortStyle.Mapper;
            else if (SortSource.IsChecked.HasValue && SortSource.IsChecked.Value)
                exportSection.SortStyle = SortStyle.Source;
            AppSettings.SaveDefault();
        }
    }
}
