using System.Threading.Tasks;

namespace OsuPlayer.Media.Audio;

/// <summary>
/// Defines playback control operations for a beatmap session.
/// Replaces the former loose-delegate pattern on <see cref="BeatmapContext"/>
/// with a type-safe, single-injection-point contract.
/// </summary>
public interface IPlaybackController
{
    Task PlayAsync();
    Task PauseAsync();
    Task StopAsync();
    Task RestartAsync();
    Task TogglePlayAsync();
    Task SetTimeAsync(double time, bool play);
}
