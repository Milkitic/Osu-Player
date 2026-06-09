using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Avalonia.ViewModels.Pages.Settings;

public partial class InterfacePageViewModel : ObservableObject
{
    public List<string> AvailableLanguages { get; } = new() { "English" };

    [ObservableProperty]
    public partial string SelectedLanguage { get; set; } = "English";

    [ObservableProperty]
    public partial bool MinimalMode { get; set; }
}
