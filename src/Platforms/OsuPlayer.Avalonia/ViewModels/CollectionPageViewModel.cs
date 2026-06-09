using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OsuPlayer.Core;

namespace OsuPlayer.ViewModels;

public partial class CollectionPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<BeatmapDataModel> Beatmaps { get; set; } = new();
}
