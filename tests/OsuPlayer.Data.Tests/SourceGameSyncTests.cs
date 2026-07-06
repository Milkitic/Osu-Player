using Microsoft.EntityFrameworkCore;
using OsuPlayer.Data.Models;
using OsuPlayer.Shared;
using OsuPlayer.Shared.Models;
using Xunit;

namespace OsuPlayer.Data.Tests;

public sealed class SourceGameSyncTests
{
    [Fact]
    public async Task SyncMaps_keeps_osu_and_iidx_entries_isolated()
    {
        using var temp = new TempDatabaseFile();
        var options = CreateOptions(temp.DatabasePath);
        await CreateDatabaseAsync(options);

        var osu = CreateBeatmap(SourceGame.Osu, "shared-folder", "Normal");
        var iidx = CreateBeatmap(SourceGame.Osu, "shared-folder", "Normal");
        iidx.IidxMusicId = 1201;
        iidx.IidxFileIdentifier = 3;

        await using (var db = new OsuPlayerDbContext(options))
        {
            await db.SyncMapsFromOsuDbAsync([osu], addOnly: false);
            await db.SyncMapsFromIidxMusicDataAsync([iidx], addOnly: false);
        }

        await using (var db = new OsuPlayerDbContext(options))
        {
            var maps = await db.GetBeatmapsFromFolderAsync("shared-folder");

            Assert.Equal(2, maps.Count);
            Assert.Contains(maps, k => k.SourceGame == SourceGame.Osu);
            Assert.Contains(maps, k => k.SourceGame == SourceGame.Iidx && k.IidxMusicId == 1201);
        }

        await using (var db = new OsuPlayerDbContext(options))
        {
            await db.SyncMapsFromOsuDbAsync([], addOnly: false);
        }

        await using (var db = new OsuPlayerDbContext(options))
        {
            var maps = await db.GetBeatmapsFromFolderAsync("shared-folder");

            Assert.Single(maps);
            Assert.Equal(SourceGame.Iidx, maps[0].SourceGame);
        }
    }

    [Fact]
    public async Task BeatmapSettings_identity_includes_source_game()
    {
        using var temp = new TempDatabaseFile();
        var options = CreateOptions(temp.DatabasePath);
        await CreateDatabaseAsync(options);

        var osu = CreateBeatmap(SourceGame.Osu, "shared-folder", "Normal");
        var iidx = CreateBeatmap(SourceGame.Iidx, "shared-folder", "Normal");

        await using (var db = new OsuPlayerDbContext(options))
        {
            await db.TryUpdateMapAsync(osu, offset: 12);
            await db.TryUpdateMapAsync(iidx, offset: 34);
        }

        await using (var db = new OsuPlayerDbContext(options))
        {
            var osuSettings = await db.GetMapFromDbAsync(osu);
            var iidxSettings = await db.GetMapFromDbAsync(
                new GameMapIdentity("shared-folder", "Normal", inOwnDb: false, SourceGame.Iidx));

            Assert.NotEqual(osuSettings.Id, iidxSettings.Id);
            Assert.Equal(12, osuSettings.Offset);
            Assert.Equal(34, iidxSettings.Offset);
            Assert.Equal(SourceGame.Osu, osuSettings.SourceGame);
            Assert.Equal(SourceGame.Iidx, iidxSettings.SourceGame);
        }
    }

    private static DbContextOptions<OsuPlayerDbContext> CreateOptions(string databasePath) =>
        new DbContextOptionsBuilder<OsuPlayerDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

    private static async Task CreateDatabaseAsync(DbContextOptions<OsuPlayerDbContext> options)
    {
        await using var db = new OsuPlayerDbContext(options);
        await db.Database.MigrateAsync();
    }

    private static Beatmap CreateBeatmap(SourceGame sourceGame, string folderName, string version) => new()
    {
        Artist = "artist",
        ArtistUnicode = "artist",
        Title = sourceGame == SourceGame.Iidx ? "iidx title" : "osu title",
        TitleUnicode = sourceGame == SourceGame.Iidx ? "iidx title" : "osu title",
        Creator = "creator",
        Version = version,
        AudioFileName = "audio.mp3",
        BeatmapFileName = "map.osu",
        LastModifiedTime = new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc),
        DrainTimeSeconds = 120,
        TotalTime = 120000,
        AudioPreviewTime = 30000,
        GameMode = 0,
        SongSource = "",
        SongTags = "",
        FolderName = folderName,
        InOwnDb = false,
        SourceGame = sourceGame
    };

    private sealed class TempDatabaseFile : IDisposable
    {
        private readonly string _directoryPath = Path.Combine(
            Path.GetTempPath(),
            "OsuPlayerDataTests",
            Guid.NewGuid().ToString("N"));

        public TempDatabaseFile()
        {
            Directory.CreateDirectory(_directoryPath);
            DatabasePath = Path.Combine(_directoryPath, "app.db");
        }

        public string DatabasePath { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_directoryPath, recursive: true);
            }
            catch
            {
                // Best-effort cleanup for SQLite handles on slower machines.
            }
        }
    }
}
