using System.Collections.ObjectModel;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer.ViewModels;

public class SelectCollectionPageViewModel : VmBase
{
    private IList<PlayItem> _playItems;
    private ObservableCollection<PlayList> _playLists;

    public IList<PlayItem> PlayItems
    {
        get => _playItems;
        set => this.RaiseAndSetIfChanged(ref _playItems, value);
    }

    public ObservableCollection<PlayList> PlayLists
    {
        get => _playLists;
        set => this.RaiseAndSetIfChanged(ref _playLists, value);
    }
}