using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OsuPlayer.Data.Models;

namespace OsuPlayer.ViewModels;

public partial class SelectCollectionPageViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Collection> _collections = new();

    [ObservableProperty]
    private IList<Beatmap> _entries = new List<Beatmap>();
}
