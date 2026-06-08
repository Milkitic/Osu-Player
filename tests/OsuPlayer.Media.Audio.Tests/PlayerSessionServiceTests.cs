using KeyAsio.Core.Audio;
using KeyAsio.Core.Audio.Caching;
using KeyAsio.Core.Audio.SampleProviders;
using Microsoft.Extensions.Logging.Abstractions;
using NAudio.Wave;
using OsuPlayer.Core.Services;
using OsuPlayer.Data;
using OsuPlayer.Data.Models;
using OsuPlayer.Media.Audio.Coordination;
using OsuPlayer.Media.Audio.Playlist;
using OsuPlayer.Shared;
using OsuPlayer.Shared.Models;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public class PlayerSessionServiceTests
{
    [Fact]
    public async Task DisposeAsync_WaitsForInFlightOperation()
    {
        var playerData = new BlockingPlayerDataStore();
        await using var service = CreateService(playerData);

        var playTask = service.PlayNewFromBeatmapAsync(CreateBeatmap("map-a"), playInstantly: true);
        await playerData.GetMapRequested.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var disposeTask = service.DisposeAsync().AsTask();
        var completed = await Task.WhenAny(disposeTask, Task.Delay(100));

        Assert.NotSame(disposeTask, completed);

        playerData.ReleaseMapLookup();
        await playTask.WaitAsync(TimeSpan.FromSeconds(2));
        await disposeTask.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task PlayNewFromBeatmapAsync_AfterDispose_DoesNotStartOperation()
    {
        var playerData = new BlockingPlayerDataStore();
        await using var service = CreateService(playerData);

        await service.DisposeAsync();
        await service.PlayNewFromBeatmapAsync(CreateBeatmap("map-a"), playInstantly: true);

        Assert.Equal(0, playerData.GetMapCalls);
    }

    private static PlayerSessionService CreateService(BlockingPlayerDataStore playerData)
    {
        var dispatcher = new ImmediateUiThreadDispatcher();
        var logger = NullLogger<PlayerEventBus>.Instance;
        var LoggerFactory = NullLoggerFactory.Instance;
        var bus = new PlayerEventBus(dispatcher, logger, new StubNotificationService());
        var playList = new PlayList(playerData, dispatcher);
        var beatmapLoader = new BeatmapLoader(playerData, NullLogger<BeatmapLoader>.Instance);

        return new PlayerSessionService(
            bus,
            playList,
            beatmapLoader,
            playerData,
            new FakePlaybackEngine(),
            new AudioCacheManager(NullLogger<AudioCacheManager>.Instance),
            NullLogger<PlayerSessionService>.Instance,
            LoggerFactory);
    }

    private static Beatmap CreateBeatmap(string folderName)
    {
        return new Beatmap
        {
            FolderName = folderName,
            Version = "Normal",
            BeatmapFileName = "map.osu",
            AudioFileName = "audio.mp3",
            Title = "Title",
            Artist = "Artist",
        };
    }

    private sealed class ImmediateUiThreadDispatcher : IUiThreadDispatcher
    {
        public void Post(Action action) => action();
        public void Send(Action action) => action();
    }

    private sealed class BlockingPlayerDataStore : IPlayerDataStore
    {
        private readonly TaskCompletionSource<BeatmapSettings> _mapLookup =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _getMapCalls;

        public TaskCompletionSource<object?> GetMapRequested { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int GetMapCalls => Volatile.Read(ref _getMapCalls);

        public void ReleaseMapLookup()
        {
            _mapLookup.TrySetResult(new BeatmapSettings
            {
                FolderName = "map-a",
                Version = "Normal",
            });
        }

        public Task<BeatmapSettings> GetMapFromDbAsync(IMapIdentifiable beatmap)
        {
            Interlocked.Increment(ref _getMapCalls);
            GetMapRequested.TrySetResult(null);
            return _mapLookup.Task;
        }

        public Task<Beatmap> GetBeatmapByIdentifiableAsync(IMapIdentifiable beatmap)
            => Task.FromResult<Beatmap>(null!);

        public Task<bool> TryRemoveFromRecentAsync(MapIdentity identity) => Task.FromResult(false);

        public Task<bool> TryRemoveMapFromCollectionAsync(IMapIdentifiable identity, Collection collection)
            => Task.FromResult(false);

        public Task<PaginationQueryResult<Beatmap>> SearchBeatmapPageAsync(string searchText, BeatmapSortMode sortMode,
            int startIndex, int count)
            => Task.FromResult(new PaginationQueryResult<Beatmap>([], 0));

        public Task<List<Beatmap>> SearchBeatmapByOptionsAsync(string searchText, BeatmapSortMode sortMode,
            int startIndex, int count)
            => Task.FromResult(new List<Beatmap>());

        public Task<List<Beatmap>> GetBeatmapsFromFolderAsync(string folderName)
            => Task.FromResult(new List<Beatmap>());

        public Task<List<Collection>> GetCollectionsAsync() => Task.FromResult(new List<Collection>());

        public Task<List<Collection>> GetCollectionsByMapAsync(BeatmapSettings beatmapSettings)
            => Task.FromResult(new List<Collection>());

        public Task<bool> TryAddCollectionAsync(string collectionName, bool isLocked) => Task.FromResult(false);

        public Task<List<Beatmap>> GetBeatmapsByIdentifiableAsync(IEnumerable<IMapIdentifiable> mapIdentities)
            => Task.FromResult(new List<Beatmap>());

        public Task<bool> TryUpdateCollectionAsync(Collection collection) => Task.FromResult(false);

        public Task<bool> TryUpdateMapAsync(IMapIdentifiable beatmap, int? offset = null) => Task.FromResult(true);

        public Task<Collection> GetCollectionByIdAsync(string id) => Task.FromResult<Collection>(null!);

        public Task<List<BeatmapSettings>> GetMapsFromCollectionAsync(Collection collection)
            => Task.FromResult(new List<BeatmapSettings>());

        public Task<List<Beatmap>> GetBeatmapsByMapInfoAsync(List<BeatmapSettings> settings, TimeSortMode sortMode)
            => Task.FromResult(new List<Beatmap>());

        public Task<bool> TryRemoveCollectionAsync(Collection collection) => Task.FromResult(false);

        public Task<bool> TryAddMapExportAsync(IMapIdentifiable mapIdentity, string path) => Task.FromResult(false);

        public Task<List<BeatmapSettings>> GetRecentListAsync() => Task.FromResult(new List<BeatmapSettings>());

        public Task<List<BeatmapSettings>> GetExportedMapsAsync() => Task.FromResult(new List<BeatmapSettings>());

        public Task<bool> TryClearRecentAsync() => Task.FromResult(false);

        public Task<bool> TryAddMapsToCollectionAsync(IList<Beatmap> beatmaps, Collection collection)
            => Task.FromResult(false);

        public Task<bool> TryRemoveLocalAllAsync() => Task.FromResult(false);

        public Task<bool> TryAddNewMapsAsync(IEnumerable<Beatmap> beatmaps) => Task.FromResult(false);

        public Task SyncMapsFromOsuDbAsync(IEnumerable<Beatmap> beatmaps, bool addOnly) => Task.CompletedTask;

        public Task<(bool found, string thumbPath)> TryGetMapThumbAsync(Guid beatmapDbId)
            => Task.FromResult((false, string.Empty));

        public Task<bool> TrySetMapThumbAsync(Guid beatmapDbId, string thumbPath) => Task.FromResult(false);
    }

    private sealed class FakePlaybackEngine : IPlaybackEngine
    {
        public event Action<DeviceDescription>? DeviceStarted { add { } remove { } }
        public event Action? DeviceStopped { add { } remove { } }
        public event Action<Exception>? DeviceError { add { } remove { } }

        public IWavePlayer? CurrentDevice => null;
        public DeviceDescription? CurrentDeviceDescription => null;
        public WaveFormat EngineWaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        public WaveFormat SourceWaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        public WaveFormat? WaveFormat => SourceWaveFormat;
        public IMixingSampleProvider EffectMixer => null!;
        public IMixingSampleProvider MusicMixer => null!;
        public IMixingSampleProvider RootMixer => null!;
        public ISampleProvider RootSampleProvider => null!;
        public LimiterType LimiterType { get; set; }
        public float MainVolume { get; set; }
        public float EffectVolume { get; set; }
        public float MusicVolume { get; set; }

        public void AddInput(ISampleProvider input) { }
        public void RemoveInput(ISampleProvider input) { }
        public void StartDevice(DeviceDescription? deviceDescription, WaveFormat? waveFormat = null) { }
        public void StopDevice() { }
        public void Dispose() { }
    }

    private sealed class StubNotificationService : IAppNotificationService
    {
        public void Push(string message) { }
        public void Push(string message, string title) { }
    }
}
