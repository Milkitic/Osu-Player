using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer;

public class SharedVm : SingletonVm<SharedVm>
{
    private bool _enableVideo = true;
    private bool _isNavigationCollapsed;
    private List<PlayList> _playLists;
    private NavigationType _checkedNavigationType;
    private bool _isLyricEnabled;
    private bool _isLyricWindowLocked;
    private bool _isMinimalMode;

    public bool EnableVideo
    {
        get => _enableVideo;
        set => this.RaiseAndSetIfChanged(ref _enableVideo, value);
    }

    public bool IsNavigationCollapsed
    {
        get => _isNavigationCollapsed;
        set => this.RaiseAndSetIfChanged(ref _isNavigationCollapsed, value);
    }

    public List<PlayList> PlayLists
    {
        get => _playLists;
        set => this.RaiseAndSetIfChanged(ref _playLists, value);
    }

    public NavigationType CheckedNavigationType
    {
        get => _checkedNavigationType;
        set => this.RaiseAndSetIfChanged(ref _checkedNavigationType, value);
    }

    public bool IsLyricWindowEnabled
    {
        get => _isLyricEnabled;
        set => this.RaiseAndSetIfChanged(ref _isLyricEnabled, value);
    }

    public bool IsLyricWindowLocked
    {
        get => _isLyricWindowLocked;
        set => this.RaiseAndSetIfChanged(ref _isLyricWindowLocked, value);
    }

    public bool IsMinimalMode
    {
        get => _isMinimalMode;
        set => this.RaiseAndSetIfChanged(ref _isMinimalMode, value);
    }

    public AppSettings AppSettings => AppSettings.Default;

    /// <summary>
    /// Update collections in the navigation bar.
    /// </summary>
    public async ValueTask UpdatePlayListsAsync()
    {
        await using var dbContext = ServiceProviders.GetApplicationDbContext();
        var list = await dbContext.GetPlayListsAsync();
        PlayLists = new List<PlayList>(list);
    }
}

public enum NavigationType
{
    Search, Recent, Export
}