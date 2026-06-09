using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OsuPlayer.Avalonia.ViewModels;

public partial class SearchPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    [ObservableProperty]
    public partial ObservableCollection<SearchResult> Results { get; set; } = new();

    [RelayCommand]
    private void Search()
    {
        // Avalonia 端 stub
    }
}

public class SearchResult
{
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
}
