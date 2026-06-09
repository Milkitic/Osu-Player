using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OsuPlayer.Core;

namespace OsuPlayer.Avalonia.ViewModels;

public partial class ExportPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<BeatmapDataModel> Exports { get; set; } = new();
}
