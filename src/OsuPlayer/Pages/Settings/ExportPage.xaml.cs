using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Shared.Models;

namespace Milki.OsuPlayer.Pages.Settings;

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
        LblMp3Path.Text = AppSettings.Default.ExportSection.DirMusic;
        LblBgPath.Text = AppSettings.Default.ExportSection.DirBackground;
        if (AppSettings.Default.ExportSection.ExportNamingStyle == ExportNamingStyle.Title)
            RadioT.IsChecked = true;
        else if (AppSettings.Default.ExportSection.ExportNamingStyle == ExportNamingStyle.ArtistTitle)
            RadioAt.IsChecked = true;
        else if (AppSettings.Default.ExportSection.ExportNamingStyle == ExportNamingStyle.TitleArtist)
            RadioTa.IsChecked = true;
        if (AppSettings.Default.ExportSection.ExportGroupStyle == ExportGroupStyle.None)
            SortNone.IsChecked = true;
        else if (AppSettings.Default.ExportSection.ExportGroupStyle == ExportGroupStyle.Artist)
            SortArtist.IsChecked = true;
        else if (AppSettings.Default.ExportSection.ExportGroupStyle == ExportGroupStyle.Mapper)
            SortMapper.IsChecked = true;
        else if (AppSettings.Default.ExportSection.ExportGroupStyle == ExportGroupStyle.Source)
            SortSource.IsChecked = true;
    }

    private void BtnMp3Path_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true,
            Title = "选择音乐导出目录",
        };
        var result = dialog.ShowDialog();
        if (result != CommonFileDialogResult.Ok) return;
        AppSettings.Default.ExportSection.DirMusic = dialog.FileName;
        LblMp3Path.Text = AppSettings.Default.ExportSection.DirMusic;
        AppSettings.SaveDefault();
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
            AppSettings.Default.ExportSection.DirBackground = dialog.FileName;
            LblBgPath.Text = AppSettings.Default.ExportSection.DirBackground;
            AppSettings.SaveDefault();
        }
    }

    private void Naming_CheckChanged(object sender, RoutedEventArgs e)
    {
        var exportSection = AppSettings.Default.ExportSection;
        if (RadioT.IsChecked.HasValue && RadioT.IsChecked.Value)
            exportSection.ExportNamingStyle = ExportNamingStyle.Title;
        else if (RadioAt.IsChecked.HasValue && RadioAt.IsChecked.Value)
            exportSection.ExportNamingStyle = ExportNamingStyle.ArtistTitle;
        else if (RadioTa.IsChecked.HasValue && RadioTa.IsChecked.Value)
            exportSection.ExportNamingStyle = ExportNamingStyle.TitleArtist;
        AppSettings.SaveDefault();
    }

    private void Sort_CheckChanged(object sender, RoutedEventArgs e)
    {
        var exportSection = AppSettings.Default.ExportSection;
        if (SortNone.IsChecked.HasValue && SortNone.IsChecked.Value)
            exportSection.ExportGroupStyle = ExportGroupStyle.None;
        else if (SortArtist.IsChecked.HasValue && SortArtist.IsChecked.Value)
            exportSection.ExportGroupStyle = ExportGroupStyle.Artist;
        else if (SortMapper.IsChecked.HasValue && SortMapper.IsChecked.Value)
            exportSection.ExportGroupStyle = ExportGroupStyle.Mapper;
        else if (SortSource.IsChecked.HasValue && SortSource.IsChecked.Value)
            exportSection.ExportGroupStyle = ExportGroupStyle.Source;
        AppSettings.SaveDefault();
    }
}