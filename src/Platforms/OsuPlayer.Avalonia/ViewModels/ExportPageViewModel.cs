using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Core;
using OsuPlayer.Services;

namespace OsuPlayer.ViewModels;

public partial class ExportPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<BeatmapDataModel> DataModelList { get; set; } = new();

    [ObservableProperty]
    public partial string ExportPath { get; set; } = "";

    [RelayCommand]
    private void ItemFolder() => AppNotificationService.Instance.Push("Open folder (stub)");
}