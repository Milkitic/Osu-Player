using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Shared.Models;
using OsuPlayer.Utils;

namespace OsuPlayer.ViewModels.Pages.Settings;

public partial class ExportPageViewModel : ObservableObject
{
    public string MusicPath
    {
        get => AppSettings.Default?.Export.MusicPath ?? string.Empty;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.Export.MusicPath == value) return;
            AppSettings.Default.Export.MusicPath = value;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }

    public string BgPath
    {
        get => AppSettings.Default?.Export.BgPath ?? string.Empty;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.Export.BgPath == value) return;
            AppSettings.Default.Export.BgPath = value;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }

    public ExportNamingStyle NamingStyle
    {
        get => AppSettings.Default?.Export.ExportNamingStyle ?? ExportNamingStyle.Title;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.Export.ExportNamingStyle == value) return;
            AppSettings.Default.Export.ExportNamingStyle = value;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }

    public ExportGroupStyle GroupStyle
    {
        get => AppSettings.Default?.Export.ExportGroupStyle ?? ExportGroupStyle.None;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.Export.ExportGroupStyle == value) return;
            AppSettings.Default.Export.ExportGroupStyle = value;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }

    [RelayCommand]
    private async Task BrowseMusicAsync()
    {
        var path = await StoragePickerHelper.PickFolderAsync("Select Folder");
        if (!string.IsNullOrWhiteSpace(path))
        {
            MusicPath = path;
        }
    }

    [RelayCommand]
    private async Task BrowseBgAsync()
    {
        var path = await StoragePickerHelper.PickFolderAsync("Select Folder");
        if (!string.IsNullOrWhiteSpace(path))
        {
            BgPath = path;
        }
    }
}
