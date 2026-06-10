using CommunityToolkit.Mvvm.ComponentModel;

namespace OsuPlayer.ViewModels;

public partial class EditCollectionPageViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string? _coverPath;
}
