using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Milky.OsuPlayer.Data;
using Xunit;

namespace OsuPlayer.Data.Tests;

public sealed class LegacyPlayerDatabaseMigratorTests
{
    [Fact]
    public async Task MigrateIfRequired_imports_legacy_dapper_schema_and_marks_history()
    {
        using var temp = new TempDatabaseFiles();
        await CreateAppDatabaseAsync(temp.AppDatabasePath);
        CreateLegacyDapperDatabase(temp.LegacyDatabasePath, includeDuplicateBeatmap: false);

        LegacyPlayerDatabaseMigrator.MigrateIfRequired(temp.AppDatabasePath, temp.LegacyDatabasePath);

        using var connection = OpenConnection(temp.AppDatabasePath);
        Assert.Equal(1, CountRows(connection, "beatmaps"));
        Assert.Equal(1, CountRows(connection, "beatmap_play_settings"));
        Assert.Equal(1, CountRows(connection, "collections"));
        Assert.Equal(1, CountRows(connection, "collection_beatmaps"));
        Assert.Equal(1, CountRows(connection, "beatmap_thumbnails"));
        Assert.Equal(1, CountRows(connection, "storyboard_assets"));
        Assert.Equal(1, CountRows(connection, "__OsuPlayerDataMigrationHistory"));
    }

    [Fact]
    public async Task MigrateIfRequired_imports_legacy_ef_schema_even_when_history_exists()
    {
        using var temp = new TempDatabaseFiles();
        await CreateAppDatabaseAsync(temp.AppDatabasePath);
        await CreateAppDatabaseAsync(temp.LegacyDatabasePath);

        using (var connection = OpenConnection(temp.LegacyDatabasePath))
        {
            Execute(connection, """
                INSERT INTO beatmaps (
                    id, artist, artist_unicode, title, title_unicode, creator, difficulty_name,
                    audio_file_name, beatmap_file_name, last_modified_at, star_rating_standard,
                    star_rating_taiko, star_rating_catch, star_rating_mania, drain_time_seconds,
                    total_time_ms, preview_time_ms, osu_beatmap_id, osu_beatmapset_id, game_mode,
                    source, tags, folder_name, is_local)
                VALUES (
                    '11111111-1111-1111-1111-111111111111', 'artist', 'artistU', 'title', 'titleU',
                    'creator', 'Hard', 'audio.mp3', 'map.osu', '2026-05-28T00:00:00',
                    1.0, 0.0, 0.0, 0.0, 90, 120000, 10000, 1001, 2001, 0,
                    'source', 'tags', 'set-a', 0);
                """);
        }

        LegacyPlayerDatabaseMigrator.MigrateIfRequired(temp.AppDatabasePath, temp.LegacyDatabasePath);

        using var appConnection = OpenConnection(temp.AppDatabasePath);
        Assert.Equal(1, CountRows(appConnection, "beatmaps"));
        Assert.Equal(1, CountRows(appConnection, "__OsuPlayerDataMigrationHistory"));
    }

    [Fact]
    public async Task MigrateIfRequired_ignores_duplicate_legacy_identity_rows_without_failing()
    {
        using var temp = new TempDatabaseFiles();
        await CreateAppDatabaseAsync(temp.AppDatabasePath);
        CreateLegacyDapperDatabase(temp.LegacyDatabasePath, includeDuplicateBeatmap: true);

        LegacyPlayerDatabaseMigrator.MigrateIfRequired(temp.AppDatabasePath, temp.LegacyDatabasePath);

        using var connection = OpenConnection(temp.AppDatabasePath);
        Assert.Equal(1, CountRows(connection, "beatmaps"));
        Assert.Equal(1, CountRows(connection, "__OsuPlayerDataMigrationHistory"));
    }

    private static async Task CreateAppDatabaseAsync(string path)
    {
        var options = new DbContextOptionsBuilder<OsuPlayerDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;

        await using var db = new OsuPlayerDbContext(options);
        await db.Database.MigrateAsync();
    }

    private static void CreateLegacyDapperDatabase(string path, bool includeDuplicateBeatmap)
    {
        using var connection = OpenConnection(path);
        Execute(connection, """
            CREATE TABLE beatmap (
                id TEXT NOT NULL PRIMARY KEY,
                artist TEXT,
                artistU TEXT,
                title TEXT,
                titleU TEXT,
                creator TEXT,
                version TEXT,
                fileName TEXT,
                lastModified TEXT NOT NULL,
                diffSrStd REAL NOT NULL,
                diffSrTaiko REAL NOT NULL,
                diffSrCtb REAL NOT NULL,
                diffSrMania REAL NOT NULL,
                drainTime INTEGER NOT NULL,
                totalTime INTEGER NOT NULL,
                audioPreview INTEGER NOT NULL,
                beatmapId INTEGER NOT NULL,
                beatmapSetId INTEGER NOT NULL,
                gameMode INTEGER NOT NULL,
                source TEXT,
                tags TEXT,
                folderName TEXT,
                audioName TEXT,
                own INTEGER NOT NULL
            );

            CREATE TABLE map_info (
                id TEXT NOT NULL PRIMARY KEY,
                version TEXT NOT NULL,
                folder TEXT NOT NULL,
                ownDb INTEGER NOT NULL,
                offset INTEGER NOT NULL,
                lastPlayTime TEXT,
                exportFile TEXT
            );

            CREATE TABLE collection (
                id TEXT NOT NULL PRIMARY KEY,
                name TEXT NOT NULL,
                locked INTEGER NOT NULL,
                "index" INTEGER NOT NULL,
                imagePath TEXT,
                description TEXT,
                createTime TEXT NOT NULL
            );

            CREATE TABLE collection_relation (
                id TEXT NOT NULL PRIMARY KEY,
                collectionId TEXT NOT NULL,
                mapId TEXT NOT NULL,
                addTime TEXT
            );

            CREATE TABLE map_thumb (
                id TEXT NOT NULL PRIMARY KEY,
                mapId TEXT NOT NULL,
                thumbPath TEXT
            );

            CREATE TABLE sb_info (
                id TEXT NOT NULL PRIMARY KEY,
                mapId TEXT NOT NULL,
                thumbPath TEXT NOT NULL,
                thumbVideoPath TEXT NOT NULL,
                version TEXT NOT NULL,
                folder TEXT NOT NULL,
                own INTEGER NOT NULL
            );

            INSERT INTO beatmap (
                id, artist, artistU, title, titleU, creator, version, audioName, fileName,
                lastModified, diffSrStd, diffSrTaiko, diffSrCtb, diffSrMania, drainTime,
                totalTime, audioPreview, beatmapId, beatmapSetId, gameMode, source, tags,
                folderName, own)
            VALUES (
                '11111111-1111-1111-1111-111111111111', 'artist', 'artistU', 'title', 'titleU',
                'creator', 'Hard', 'audio.mp3', 'map.osu', '2026-05-28T00:00:00',
                1.0, 0.0, 0.0, 0.0, 90, 120000, 10000, 1001, 2001, 0,
                'source', 'tags', 'set-a', 0);

            INSERT INTO map_info (id, version, folder, ownDb, offset, lastPlayTime, exportFile)
            VALUES ('settings-1', 'Hard', 'set-a', 0, 25, '2026-05-28T00:01:00', 'export.mp3');

            INSERT INTO collection (id, name, locked, "index", imagePath, description, createTime)
            VALUES ('collection-1', 'Favorite', 1, 0, NULL, NULL, '2026-05-28T00:02:00');

            INSERT INTO collection_relation (id, collectionId, mapId, addTime)
            VALUES ('relation-1', 'collection-1', 'settings-1', '2026-05-28T00:03:00');

            INSERT INTO map_thumb (id, mapId, thumbPath)
            VALUES ('thumb-1', '11111111-1111-1111-1111-111111111111', 'thumb-a');

            INSERT INTO sb_info (id, mapId, thumbPath, thumbVideoPath, version, folder, own)
            VALUES ('storyboard-1', '11111111-1111-1111-1111-111111111111', 'sb-thumb', 'sb-video', 'Hard', 'set-a', 0);
            """);

        if (!includeDuplicateBeatmap)
        {
            return;
        }

        Execute(connection, """
            INSERT INTO beatmap (
                id, artist, artistU, title, titleU, creator, version, audioName, fileName,
                lastModified, diffSrStd, diffSrTaiko, diffSrCtb, diffSrMania, drainTime,
                totalTime, audioPreview, beatmapId, beatmapSetId, gameMode, source, tags,
                folderName, own)
            VALUES (
                '22222222-2222-2222-2222-222222222222', 'artist2', 'artistU2', 'title2', 'titleU2',
                'creator2', 'Hard', 'audio2.mp3', 'map2.osu', '2026-05-28T00:04:00',
                2.0, 0.0, 0.0, 0.0, 91, 121000, 10001, 1002, 2002, 0,
                'source2', 'tags2', 'set-a', 0);
            """);
    }

    private static SqliteConnection OpenConnection(string path)
    {
        var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        return connection;
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static int CountRows(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\";";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private sealed class TempDatabaseFiles : IDisposable
    {
        private readonly string _directoryPath = Path.Combine(Path.GetTempPath(), "OsuPlayerDataTests", Guid.NewGuid().ToString("N"));

        public TempDatabaseFiles()
        {
            Directory.CreateDirectory(_directoryPath);
            AppDatabasePath = Path.Combine(_directoryPath, "app.db");
            LegacyDatabasePath = Path.Combine(_directoryPath, "player.db");
        }

        public string AppDatabasePath { get; }

        public string LegacyDatabasePath { get; }

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
