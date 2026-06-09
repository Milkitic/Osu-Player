using CommunityToolkit.Mvvm.ComponentModel;

namespace OsuPlayer.Core.Scanning;

public partial class FileScannerViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool IsScanning { get; internal set; }

    [ObservableProperty]
    public partial bool IsCanceling { get; set; }
}