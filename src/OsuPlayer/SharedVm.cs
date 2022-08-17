using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer;

public class SharedVm : VmBase
{
    private bool _enableVideo = true;
    private bool _isPlaying = false;
    private List<PlayList> _playLists;

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

    public AppSettings AppSettings => AppSettings.Default;

    #region Initialzation

    public static SharedVm Default { get; } = new();

    private SharedVm()
    {
    }

    #endregion
}