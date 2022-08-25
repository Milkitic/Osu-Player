using System.Collections.ObjectModel;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer.ViewModels;

public class MainWindowViewModel : VmBase
{
    private ObservableCollection<PlayList> _playLists;
    private LyricWindowViewModel _lyricWindowViewModel;

    public ObservableCollection<PlayList> PlayLists
    {
        get => _playLists;
        set => this.RaiseAndSetIfChanged(ref _playLists, value);
    }

    public LyricWindowViewModel LyricWindowViewModel
    {
        get => _lyricWindowViewModel;
        set => this.RaiseAndSetIfChanged(ref _lyricWindowViewModel, value);
    }

    public SharedVm SharedVm => SharedVm.Default;
}