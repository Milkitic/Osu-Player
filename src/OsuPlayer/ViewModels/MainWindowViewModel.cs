using System.Collections.ObjectModel;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer.ViewModels;

public class MainWindowViewModel : VmBase
{
    private bool _isNavigationCollapsed;
    private ObservableCollection<PlayList> _playLists;
    private LyricWindowViewModel _lyricWindowViewModel;

    public bool IsNavigationCollapsed
    {
        get => _isNavigationCollapsed;
        set => this.RaiseAndSetIfChanged(ref _isNavigationCollapsed, value);
    }

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
}