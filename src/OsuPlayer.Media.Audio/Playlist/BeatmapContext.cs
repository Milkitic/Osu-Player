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
        private IPlaybackController _playbackController = NoOpController;

        public BeatmapContext() : this(new Beatmap()) { }

        private BeatmapContext(Beatmap beatmap)
        {
            Beatmap = beatmap;
            BeatmapDetail = new BeatmapDetail(beatmap);
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

        public IPlaybackController PlaybackController
        {
            get => _playbackController;
            internal set => _playbackController = value ?? NoOpController;
        }

        public static bool operator ==(BeatmapContext? bc1, BeatmapContext? bc2)
        {
            return Equals(bc1, bc2);
        }

        public static bool operator !=(BeatmapContext? bc1, BeatmapContext? bc2)
        {
            return !(bc1 == bc2);
        }

        public override bool Equals(object? obj)
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
