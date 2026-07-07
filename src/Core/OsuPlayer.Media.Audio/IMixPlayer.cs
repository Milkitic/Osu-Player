using System;
using System.Threading.Tasks;
using NAudio.Wave;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Media.Audio;

/// <summary>
/// Common playback surface implemented by both <see cref="OsuMixPlayer"/> and
/// <see cref="IidxMixPlayer"/>. <see cref="Playback.PlayerEventBus"/> and
/// <see cref="Playback.ObservablePlayController"/> hold an <c>IMixPlayer?</c> so the
/// UI layer can drive either engine through a single type.
/// </summary>
public interface IMixPlayer : IPlaybackController, IAsyncDisposable
{
    event Action<PlayStatus>? PlayStatusChanged;
    event Action<TimeSpan>? PositionUpdated;

    IWavePlayer? Device { get; }
    TimeSpan Duration { get; }
    TimeSpan Position { get; }
    float PlaybackRate { get; }
    bool KeepTune { get; }
    float PreservePitchRateCompensationMilliseconds { get; set; }
    PlayStatus PlayStatus { get; }
    float Volume { get; set; }
    bool IsLooping { get; set; }
    int ManualOffset { get; set; }
    int GeneralOffset { get; set; }

    Task Initialize();
    Task PlayAsync();
    Task PauseAsync();
    Task StopAsync();
    Task RestartAsync();
    Task TogglePlayAsync();
    Task SetTimeAsync(double time, bool play);
    Task SkipToAsync(TimeSpan time);
    Task SetPlaybackRate(float rate, bool keepTune);
    Task SetPlayMod(PlayModifier modifier);
}