using System.Reflection;
using KeyAsio.Core.Audio;
using KeyAsio.Core.Audio.Caching;
using KeyAsio.Core.Audio.SampleProviders;
using Microsoft.Extensions.Logging;
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

public class ObservablePlayControllerTests
{
    private static readonly ILogger<ObservablePlayController> ControllerLog =
        NullLoggerFactory.Instance.CreateLogger<ObservablePlayController>();
    private static readonly ILogger<PlayerEventBus> BusLog =
        NullLoggerFactory.Instance.CreateLogger<PlayerEventBus>();
    private static readonly ILogger<PlayerSessionService> SessionLog =
        NullLoggerFactory.Instance.CreateLogger<PlayerSessionService>();
    private static readonly ILogger<OsuMixPlayer> PlayerLog =
        NullLoggerFactory.Instance.CreateLogger<OsuMixPlayer>();
    private static readonly ILoggerFactory LogFactory =
        NullLoggerFactory.Instance;

    [Fact]
    public void Player_ReturnsPumpCurrentPlayerAndRaisesChangeNotification()
    {
        var dispatcher = new ImmediateUiThreadDispatcher();
        var bus = new PlayerEventBus(dispatcher, BusLog, new StubNotificationService());
        var playList = new PlayList(new FakePlayerDataStore(), dispatcher);
        var session = CreateSession(bus, playList);
        var controller = new ObservablePlayController(
            new FakePlaybackEngine(),
            bus,
            playList,
            session,
            ControllerLog);
        var player = new OsuMixPlayer(null!, string.Empty, null!, null!, PlayerLog);
        var notified = false;

        controller.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ObservablePlayController.Player))
            {
                notified = true;
            }
        };

        bus.AttachPlayer(player);

        Assert.Same(player, controller.Player);
        Assert.True(notified);
    }

    [Fact]
    public async Task PlayAsync_IgnoresAttachedPlayerUntilReady()
    {
        var dispatcher = new ImmediateUiThreadDispatcher();
        var bus = new PlayerEventBus(dispatcher, BusLog, new StubNotificationService());
        var playList = new PlayList(new FakePlayerDataStore(), dispatcher);
        var session = CreateSession(bus, playList);
        var controller = new ObservablePlayController(
            new FakePlaybackEngine(),
            bus,
            playList,
            session,
            ControllerLog);
        var player = new OsuMixPlayer(null!, string.Empty, null!, null!, PlayerLog);

        bus.AttachPlayer(player);

        await controller.PlayAsync();
    }

    [Fact]
    public void AttachPlayer_ReplaysReadyStatusToFacade()
    {
        var dispatcher = new ImmediateUiThreadDispatcher();
        var bus = new PlayerEventBus(dispatcher, BusLog, new StubNotificationService());
        var playList = new PlayList(new FakePlayerDataStore(), dispatcher);
        var session = CreateSession(bus, playList);
        var controller = new ObservablePlayController(
            new FakePlaybackEngine(),
            bus,
            playList,
            session,
            ControllerLog);
        var player = new OsuMixPlayer(null!, string.Empty, null!, null!, PlayerLog);
        SetPlayStatus(player, PlayStatus.Ready);
        PlayStatus? observed = null;

        controller.PlayStatusChanged += status => observed = status;

        bus.AttachPlayer(player);

        Assert.Equal(PlayStatus.Ready, observed);
        Assert.True(controller.IsPlayerReady);
    }

    private static PlayerSessionService CreateSession(PlayerEventBus bus, PlayList playList)
    {
        var playerData = new FakePlayerDataStore();
        var beatmapLoader = new BeatmapLoader(playerData, NullLogger<BeatmapLoader>.Instance);
        return new PlayerSessionService(
            bus,
            playList,
            beatmapLoader,
            playerData,
            new FakePlaybackEngine(),
            new AudioCacheManager(NullLogger<AudioCacheManager>.Instance),
            SessionLog,
            LogFactory);
    }

    private static PlayerEventBus GetBus(ObservablePlayController controller)
    {
        var field = typeof(ObservablePlayController)
            .GetField("_bus", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<PlayerEventBus>(field.GetValue(controller));
    }

    private static void SetPlayStatus(OsuMixPlayer player, PlayStatus status)
    {
        var field = typeof(OsuMixPlayer)
            .GetField("_playStatus", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(player, status);
    }

    private sealed class ImmediateUiThreadDispatcher : IUiThreadDispatcher
    {
        public void Post(Action action) => action();
        public void Send(Action action) => action();
    }

    private sealed class StubNotificationService : IAppNotificationService
    {
        public void Push(string message) { }
        public void Push(string message, string title) { }
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
        public IMixingSampleProvider EffectMixer => null!;
        public IMixingSampleProvider MusicMixer => null!;
        public IMixingSampleProvider RootMixer => null!;
        public ISampleProvider RootSampleProvider => null!;
        public WaveFormat? WaveFormat => SourceWaveFormat;
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

    private sealed class FakePlayerDataStore : IPlayerDataStore
    {
        public Task<Beatmap> GetBeatmapByIdentifiableAsync(IMapIdentifiable beatmap) => Task.FromResult<Beatmap>(null!);
        public Task<BeatmapSettings> GetMapFromDbAsync(IMapIdentifiable beatmap) => Task.FromResult<BeatmapSettings>(null!);
        public Task<bool> TryRemoveFromRecentAsync(MapIdentity identity) => Task.FromResult(false);
        public Task<bool> TryRemoveMapFromCollectionAsync(IMapIdentifiable identity, Collection collection) => Task.FromResult(false);
        public Task<PaginationQueryResult<Beatmap>> SearchBeatmapPageAsync(string searchText, BeatmapSortMode sortMode, int startIndex, int count) => Task.FromResult(new PaginationQueryResult<Beatmap>([], 0));
        public Task<List<Beatmap>> SearchBeatmapByOptionsAsync(string searchText, BeatmapSortMode sortMode, int startIndex, int count) => Task.FromResult(new List<Beatmap>());
        public Task<List<Beatmap>> GetBeatmapsFromFolderAsync(string folderName) => Task.FromResult(new List<Beatmap>());
        public Task<List<Collection>> GetCollectionsAsync() => Task.FromResult(new List<Collection>());
        public Task<List<Collection>> GetCollectionsByMapAsync(BeatmapSettings beatmapSettings) => Task.FromResult(new List<Collection>());
        public Task<bool> TryAddCollectionAsync(string collectionName, bool isLocked) => Task.FromResult(false);
        public Task<List<Beatmap>> GetBeatmapsByIdentifiableAsync(IEnumerable<IMapIdentifiable> mapIdentities) => Task.FromResult(new List<Beatmap>());
        public Task<bool> TryUpdateCollectionAsync(Collection collection) => Task.FromResult(false);
        public Task<bool> TryUpdateMapAsync(IMapIdentifiable beatmap, int? offset = null) => Task.FromResult(true);
        public Task<Collection> GetCollectionByIdAsync(string id) => Task.FromResult<Collection>(null!);
        public Task<List<BeatmapSettings>> GetMapsFromCollectionAsync(Collection collection) => Task.FromResult(new List<BeatmapSettings>());
        public Task<List<Beatmap>> GetBeatmapsByMapInfoAsync(List<BeatmapSettings> settings, TimeSortMode sortMode) => Task.FromResult(new List<Beatmap>());
        public Task<bool> TryRemoveCollectionAsync(Collection collection) => Task.FromResult(false);
        public Task<bool> TryAddMapExportAsync(IMapIdentifiable mapIdentity, string path) => Task.FromResult(false);
        public Task<List<BeatmapSettings>> GetRecentListAsync() => Task.FromResult(new List<BeatmapSettings>());
        public Task<List<BeatmapSettings>> GetExportedMapsAsync() => Task.FromResult(new List<BeatmapSettings>());
        public Task<bool> TryClearRecentAsync() => Task.FromResult(false);
        public Task<bool> TryAddMapsToCollectionAsync(IList<Beatmap> beatmaps, Collection collection) => Task.FromResult(false);
        public Task<bool> TryRemoveLocalAllAsync() => Task.FromResult(false);
        public Task<bool> TryAddNewMapsAsync(IEnumerable<Beatmap> beatmaps) => Task.FromResult(false);
        public Task SyncMapsFromOsuDbAsync(IEnumerable<Beatmap> beatmaps, bool addOnly) => Task.CompletedTask;
        public Task<(bool found, string thumbPath)> TryGetMapThumbAsync(Guid beatmapDbId) => Task.FromResult((false, string.Empty));
        public Task<bool> TrySetMapThumbAsync(Guid beatmapDbId, string thumbPath) => Task.FromResult(false);
    }
}
