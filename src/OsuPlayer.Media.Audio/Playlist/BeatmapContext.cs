using System;
using System.Threading.Tasks;
using Coosu.Beatmap;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Services;

namespace Milky.OsuPlayer.Media.Audio.Playlist
{
    public class BeatmapContext
    {
        private static readonly IPlaybackController NoOpController = new NoOpPlaybackController();

        public BeatmapContext()
        {
            Beatmap = new Beatmap();
            BeatmapDetail = new BeatmapDetail(Beatmap);
            PlaybackController = NoOpController;
        }

        private BeatmapContext(Beatmap beatmap)
        {
            Beatmap = beatmap;
            BeatmapDetail = new BeatmapDetail(beatmap);
            PlaybackController = NoOpController;
        }

        public static async Task<BeatmapContext> CreateAsync(Beatmap beatmap, IPlayerDataStore playerData)
        {
            return new BeatmapContext(beatmap)
            {
                BeatmapSettings = await playerData.GetMapFromDbAsync(beatmap.GetIdentity()),
            };
        }

        public bool FullLoaded { get; set; } = false;
        public Beatmap Beatmap { get; }
        public BeatmapSettings BeatmapSettings { get; private set; }
        public BeatmapDetail BeatmapDetail { get; }
        public LocalOsuFile OsuFile { get; set; }
        public bool PlayInstantly { get; set; }

        /// <summary>
        /// Provides playback control operations for this beatmap context.
        /// Defaults to a no-op controller; set by <see cref="ObservablePlayController.InitializeContextHandle"/>
        /// once the player is ready.
        /// </summary>
        public IPlaybackController PlaybackController { get; set; }

        public static bool operator ==(BeatmapContext bc1, BeatmapContext bc2)
        {
            return Equals(bc1, bc2);
        }

        public static bool operator !=(BeatmapContext bc1, BeatmapContext bc2)
        {
            return !(bc1 == bc2);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (!(obj is BeatmapContext bc))
                return false;
            return Equals(bc);
        }

        protected bool Equals(BeatmapContext other)
        {
            return Equals(Beatmap, other.Beatmap);
        }

        public override int GetHashCode()
        {
            return Beatmap != null ? Beatmap.GetHashCode() : 0;
        }

        /// <summary>
        /// Null Object implementation that safely discards all playback operations.
        /// Used before the real controller is injected, preventing <c>NullReferenceException</c>.
        /// </summary>
        private sealed class NoOpPlaybackController : IPlaybackController
        {
            public Task PlayAsync() => Task.CompletedTask;
            public Task PauseAsync() => Task.CompletedTask;
            public Task StopAsync() => Task.CompletedTask;
            public Task RestartAsync() => Task.CompletedTask;
            public Task TogglePlayAsync() => Task.CompletedTask;
            public Task SetTimeAsync(double time, bool play) => Task.CompletedTask;
        }
    }
}