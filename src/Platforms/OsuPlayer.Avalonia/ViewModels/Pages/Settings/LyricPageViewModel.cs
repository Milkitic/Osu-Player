using CommunityToolkit.Mvvm.ComponentModel;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Instances;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.ViewModels.Pages.Settings;

public partial class LyricPageViewModel : ObservableObject
{
    private readonly LyricsInst _lyricsInst;

    public LyricPageViewModel(LyricsInst lyricsInst)
    {
        _lyricsInst = lyricsInst;
    }

    public bool EnableLyric
    {
        get => AppSettings.Default?.Lyric.EnableLyric == true;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.Lyric.EnableLyric == value) return;
            AppSettings.Default.Lyric.EnableLyric = value;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }

    public LyricSource LyricSource
    {
        get => AppSettings.Default?.Lyric.LyricSource ?? LyricSource.Auto;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.Lyric.LyricSource == value) return;
            AppSettings.Default.Lyric.LyricSource = value;
            OnPropertyChanged();
            ReloadLyric();
        }
    }

    public LyricProvideType ProvideType
    {
        get => AppSettings.Default?.Lyric.ProvideType ?? LyricProvideType.Original;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.Lyric.ProvideType == value) return;
            AppSettings.Default.Lyric.ProvideType = value;
            OnPropertyChanged();
            ReloadLyric();
        }
    }

    public bool StrictMode
    {
        get => AppSettings.Default?.Lyric.StrictMode == true;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.Lyric.StrictMode == value) return;
            AppSettings.Default.Lyric.StrictMode = value;
            OnPropertyChanged();
            ReloadLyric();
        }
    }

    public bool EnableCache
    {
        get => AppSettings.Default?.Lyric.EnableCache == true;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.Lyric.EnableCache == value) return;
            AppSettings.Default.Lyric.EnableCache = value;
            OnPropertyChanged();
            ReloadLyric();
        }
    }

    private void ReloadLyric()
    {
        _lyricsInst.ReloadLyricProvider(StrictMode);
        AppSettings.SaveDefault();
    }
}
