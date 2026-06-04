using KeyAsio.Core.OsuAudio.Hitsounds.Playback;
using Milky.OsuPlayer.Media.Audio;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public class OsuPlaybackEventAudioCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_ControlEventsWithoutAudio_ReturnsNull()
    {
        var cache = new OsuPlaybackEventAudioCache(null!);

        Assert.Null(await cache.GetOrCreateAsync(
            PlaybackEvent.CreateLoopStopSignal(100, LoopChannel.Normal)));
        Assert.Null(await cache.GetOrCreateAsync(
            PlaybackEvent.CreateLoopVolumeSignal(100, 0.5f)));
        Assert.Null(await cache.GetOrCreateAsync(
            PlaybackEvent.CreateLoopBalanceSignal(100, 0.5f)));
    }
}
