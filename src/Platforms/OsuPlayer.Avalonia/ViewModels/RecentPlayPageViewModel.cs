using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Core;

namespace OsuPlayer.ViewModels;

public partial class RecentPlayPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<BeatmapDataModel> Beatmaps { get; set; } = new();

    [RelayCommand]
    private void PlayAll()
    {
        // Avalonia 端 stub
    }
}
