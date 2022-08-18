using System.Collections.ObjectModel;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer;

public class SharedVm : SingletonVm<SharedVm>
{
    private bool _enableVideo = true;
    private bool _isPlaying = false;
    private List<PlayList> _playLists;
    private NavigationType _checkedNavigationType;

    public bool EnableVideo
    {
        get => _enableVideo;
        set => this.RaiseAndSetIfChanged(ref _enableVideo, value);
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set => this.RaiseAndSetIfChanged(ref _isPlaying, value);
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

    public AppSettings AppSettings => AppSettings.Default;

    /// <summary>
    /// Update collections in the navigation bar.
    /// </summary>
    public async ValueTask UpdatePlayLists()
    {
        await using var dbContext = new ApplicationDbContext();
        var list = await dbContext.GetPlayListsAsync();
        PlayLists = new List<PlayList>(list);
    }
}

public enum NavigationType
{
    Search, Recent, Export
}