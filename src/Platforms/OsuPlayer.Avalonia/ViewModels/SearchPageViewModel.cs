using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Core;
using OsuPlayer.Services;

namespace OsuPlayer.ViewModels;

public partial class SearchPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    [ObservableProperty]
    public partial ObservableCollection<BeatmapDataModel> DisplayedMaps { get; set; } = new();

    [RelayCommand]
    private void PlayAll() => AppNotificationService.Instance.Push("Play all (stub)");

    [RelayCommand]
    private void Search() => AppNotificationService.Instance.Push("Search (stub)");
}