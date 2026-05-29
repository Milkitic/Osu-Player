using CommunityToolkit.Mvvm.ComponentModel;

namespace Milky.OsuPlayer.ViewModels;

public partial class ListPageViewModel : ObservableObject
{
    public ListPageViewModel(int index)
    {
        Index = index;
    }

    [ObservableProperty]
    public partial int Index { get; set; }

    [ObservableProperty]
    public partial bool IsActivated { get; set; }
}