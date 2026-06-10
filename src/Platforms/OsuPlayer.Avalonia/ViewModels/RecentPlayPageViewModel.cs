using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Core;
using OsuPlayer.Services;

namespace OsuPlayer.ViewModels;

public partial class RecentPlayPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<BeatmapDataModel> Beatmaps { get; set; } = new();

    [RelayCommand]
    private void PlayAll() => AppNotificationService.Instance.Push("Play all (stub)");

    [RelayCommand]
    private void ClearAllRecent() => AppNotificationService.Instance.Push("Clear all recent (stub)");
}