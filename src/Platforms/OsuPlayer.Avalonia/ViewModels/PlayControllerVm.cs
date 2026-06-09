using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Core;

namespace OsuPlayer.ViewModels;

public partial class PlayControllerVm : ObservableObject
{
    [ObservableProperty]
    public partial string CurrentSongTitle { get; set; } = "-";

    [ObservableProperty]
    public partial string CurrentTimeText { get; set; } = "00:00";

    [ObservableProperty]
    public partial string TotalTimeText { get; set; } = "00:00";

    [ObservableProperty]
    public partial double CurrentTimeMs { get; set; }

    [ObservableProperty]
    public partial double TotalTimeMs { get; set; } = 1;

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    public string PlayButtonText => IsPlaying ? "⏸" : "▶";

    public SharedVm Shared => SharedVm.Default;

    partial void OnIsPlayingChanged(bool value)
    {
        OnPropertyChanged(nameof(PlayButtonText));
    }

    [RelayCommand]
    private void Prev()
    {
    }

    [RelayCommand]
    private void Play()
    {
        IsPlaying = !IsPlaying;
    }

    [RelayCommand]
    private void Next()
    {
    }
}
