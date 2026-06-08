using OsuPlayer.Core.Services;
using OsuPlayer.Data;
using OsuPlayer.Data.Models;
using OsuPlayer.Playback.Playlist;
using OsuPlayer.Shared;
using OsuPlayer.Shared.Models;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public class PlayListTests
{
    [Fact]
    public async Task ReplaceAsync_StartAnew_SelectsFirstEntry()
    {
        var playList = CreatePlayList();
        var first = CreateBeatmap("a");
        var second = CreateBeatmap("b");

        await playList.ReplaceAsync([first, second], startAnew: true);

        Assert.Same(first, playList.CurrentInfo?.Beatmap);
        Assert.Equal(0, playList.IndexPointer);
    }

    [Fact]
    public async Task ReplaceAsync_WithoutStartAnew_KeepsCurrentEntryWhenStillPresent()
    {
        var playList = CreatePlayList();
        var first = CreateBeatmap("a");
        var second = CreateBeatmap("b");
        var third = CreateBeatmap("c");

        await playList.ReplaceAsync([first, second], startAnew: true);
        await playList.MoveNextAsync(wrap: true);
        await playList.ReplaceAsync([third, second], startAnew: false);

        Assert.Same(second, playList.CurrentInfo?.Beatmap);
        Assert.Equal(1, playList.IndexPointer);
    }

    [Fact]
    public async Task MoveNextAsync_WrapsWhenRequested()
    {
        var playList = CreatePlayList();
        var first = CreateBeatmap("a");
        var second = CreateBeatmap("b");

        await playList.ReplaceAsync([first, second], startAnew: true);
        await playList.MoveNextAsync(wrap: true);
        await playList.MoveNextAsync(wrap: true);

        Assert.Same(first, playList.CurrentInfo?.Beatmap);
        Assert.Equal(0, playList.IndexPointer);
    }

    [Fact]
    public async Task RemoveAsync_CurrentEntry_SelectsNearestRemainingEntry()
    {
        var playList = CreatePlayList();
        var first = CreateBeatmap("a");
        var second = CreateBeatmap("b");
        var third = CreateBeatmap("c");

        await playList.ReplaceAsync([first, second, third], startAnew: true);
        await playList.MoveNextAsync(wrap: true);
        await playList.RemoveAsync([second]);

        Assert.Same(third, playList.CurrentInfo?.Beatmap);
        Assert.Equal(1, playList.IndexPointer);
    }

    private static PlayList CreatePlayList()
        => new(new FakePlayerDataStore(), new ImmediateUiThreadDispatcher());

    private static Beatmap CreateBeatmap(string folderName)
        => new()
        {
            FolderName = folderName,
            Version = "Normal",
            BeatmapFileName = $"{folderName}.osu",
            AudioFileName = $"{folderName}.mp3",
        };

    private sealed class ImmediateUiThreadDispatcher : IUiThreadDispatcher
    {
        public void Post(System.Action action) => action();
        public void Send(System.Action action) => action();
    }

    private sealed class FakePlayerDataStore : IPlayerDataStore
    {
        public Task<BeatmapSettings> GetMapFromDbAsync(IMapIdentifiable beatmap)
            => Task.FromResult(new BeatmapSettings
            {
                FolderName = beatmap.FolderName,
                Version = beatmap.Version,
            });

        public Task<Beatmap> GetBeatmapByIdentifiableAsync(IMapIdentifiable beatmap)
            => Task.FromResult<Beatmap>(null!);

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
        public Task<(bool found, string thumbPath)> TryGetMapThumbAsync(System.Guid beatmapDbId) => Task.FromResult((false, string.Empty));
        public Task<bool> TrySetMapThumbAsync(System.Guid beatmapDbId, string thumbPath) => Task.FromResult(false);
    }
}
