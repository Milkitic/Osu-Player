using CommunityToolkit.Mvvm.ComponentModel;

namespace Milky.OsuPlayer.ViewModels;

public partial class EditCollectionPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; }

    [ObservableProperty]
    public partial string CoverPath { get; set; }
}