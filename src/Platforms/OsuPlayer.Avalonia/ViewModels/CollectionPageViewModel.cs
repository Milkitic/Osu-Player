using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Core;
using OsuPlayer.Data.Models;
using OsuPlayer.Services;

namespace OsuPlayer.ViewModels;

public partial class CollectionPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<BeatmapDataModel> Beatmaps { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<BeatmapDataModel> DisplayedBeatmaps { get; set; } = new();

    [ObservableProperty]
    public partial Collection? CollectionInfo { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    [RelayCommand]
    private void PlayAll() => AppNotificationService.Instance.Push("Play all (stub)");

    [RelayCommand]
    private void ExportAll() => AppNotificationService.Instance.Push("Export all (stub)");

    [RelayCommand]
    private void EditCollection() => AppNotificationService.Instance.Push("Edit collection (stub)");

    [RelayCommand]
    private void DeleteCollection() => AppNotificationService.Instance.Push("Delete collection (stub)");
}