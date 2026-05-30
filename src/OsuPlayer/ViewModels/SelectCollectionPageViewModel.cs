using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Milky.OsuPlayer.Data.Models;

namespace Milky.OsuPlayer.ViewModels;

public partial class SelectCollectionPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<CollectionViewModel> Collections { get; set; }

    [ObservableProperty]
    public partial IList<Beatmap> Entries { get; set; }
}