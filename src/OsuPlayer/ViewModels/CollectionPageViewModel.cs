using System.Collections.ObjectModel;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer.ViewModels;

public class CollectionPageViewModel : VmBase
{
    private ObservableCollection<PlayItem> _dataList;
    private PlayList _playList;

    public ObservableCollection<PlayItem> DataList
    {
        get => _dataList;
        set => this.RaiseAndSetIfChanged(ref _dataList, value);
    }

    public PlayList PlayList
    {
        get => _playList;
        set => this.RaiseAndSetIfChanged(ref _playList, value);
    }
}