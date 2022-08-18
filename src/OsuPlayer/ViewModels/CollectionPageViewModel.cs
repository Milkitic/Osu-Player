using System.Collections.ObjectModel;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer.ViewModels;

public class CollectionPageViewModel : VmBase
{
    private ObservableCollection<PlayItem> _playItems;
    private PlayList _playList;

    public ObservableCollection<PlayItem> PlayItems
    {
        get => _playItems;
        set => this.RaiseAndSetIfChanged(ref _playItems, value);
    }

    public PlayList PlayList
    {
        get => _playList;
        set => this.RaiseAndSetIfChanged(ref _playList, value);
    }
}