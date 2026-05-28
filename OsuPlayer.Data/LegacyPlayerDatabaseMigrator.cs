using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace Milky.OsuPlayer.Data
{
    internal static class LegacyPlayerDatabaseMigrator
    {
        private const string LegacyMigrationId = "legacy-player-db";
        private const string MigrationHistoryTable = "__OsuPlayerDataMigrationHistory";
        private const string LegacySchema = "legacy";
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly string[] Tables =
        {
            "beatmap",
            "map_info",
            "collection",
            "collection_relation",
            "map_thumb",
            "sb_info"
        };

        public static void MigrateIfRequired(string appDatabasePath, string legacyDatabasePath)
        {
            if (!File.Exists(legacyDatabasePath))
            {
                return;
            }

            var appFullPath = Path.GetFullPath(appDatabasePath);
            var legacyFullPath = Path.GetFullPath(legacyDatabasePath);
            if (string.Equals(appFullPath, legacyFullPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            using var appConnection = new SqliteConnection(CreateConnectionString(appFullPath));
            appConnection.Open();

            EnsureMigrationHistoryTable(appConnection);
            if (HasDataMigration(appConnection))
            {
                return;
            }

            using var legacyConnection = new SqliteConnection(CreateConnectionString(legacyFullPath));
            legacyConnection.Open();

            if (TableExists(legacyConnection, "main", "__EFMigrationsHistory"))
            {
                Logger.Info("Skip legacy player.db import because it already contains EF migration history.");
                return;
            }

            if (!Tables.Any(table => TableExists(legacyConnection, "main", table)))
            {
                Logger.Info("Skip legacy player.db import because no known legacy tables were found.");
                return;
            }

            AttachLegacyDatabase(appConnection, legacyFullPath);

            try
            {
                using var transaction = appConnection.BeginTransaction();
                var copiedRows = 0;

                foreach (var table in Tables)
                {
                    copiedRows += CopyTable(appConnection, transaction, table);
                }

                MarkDataMigration(appConnection, transaction, legacyFullPath);
                transaction.Commit();

                Logger.Info("Imported {0} rows from legacy player.db into app.db.", copiedRows);
            }
            finally
            {
                DetachLegacyDatabase(appConnection);
            }
        }

        private static string CreateConnectionString(string databasePath)
        {
            return new SqliteConnectionStringBuilder
            {
                DataSource = databasePath
            }.ToString();
        }

        private static void EnsureMigrationHistoryTable(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $@"
CREATE TABLE IF NOT EXISTS {QuoteIdentifier(MigrationHistoryTable)} (
    migrationId TEXT NOT NULL PRIMARY KEY,
    sourcePath TEXT NOT NULL,
    migratedAtUtc TEXT NOT NULL
);";
            command.ExecuteNonQuery();
        }

        private static bool HasDataMigration(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $@"
SELECT 1
FROM {QuoteIdentifier(MigrationHistoryTable)}
WHERE migrationId = $migrationId
LIMIT 1;";
            command.Parameters.AddWithValue("$migrationId", LegacyMigrationId);

            return command.ExecuteScalar() != null;
        }

        private static void MarkDataMigration(SqliteConnection connection, SqliteTransaction transaction,
            string legacyDatabasePath)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
INSERT INTO {QuoteIdentifier(MigrationHistoryTable)} (migrationId, sourcePath, migratedAtUtc)
VALUES ($migrationId, $sourcePath, $migratedAtUtc);";
            command.Parameters.AddWithValue("$migrationId", LegacyMigrationId);
            command.Parameters.AddWithValue("$sourcePath", legacyDatabasePath);
            command.Parameters.AddWithValue("$migratedAtUtc", DateTime.UtcNow.ToString("O"));
            command.ExecuteNonQuery();
        }

        private static void AttachLegacyDatabase(SqliteConnection connection, string legacyDatabasePath)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"ATTACH DATABASE $path AS {QuoteIdentifier(LegacySchema)};";
            command.Parameters.AddWithValue("$path", legacyDatabasePath);
            command.ExecuteNonQuery();
        }

        private static void DetachLegacyDatabase(SqliteConnection connection)
        {
            if (connection.State != ConnectionState.Open)
            {
                return;
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"DETACH DATABASE {QuoteIdentifier(LegacySchema)};";
            command.ExecuteNonQuery();
        }

        private static int CopyTable(SqliteConnection connection, SqliteTransaction transaction, string tableName)
        {
            if (!TableExists(connection, LegacySchema, tableName) || !TableExists(connection, "main", tableName))
            {
                return CopyLegacyTable(connection, transaction, tableName);
            }

            var targetColumns = GetColumnNames(connection, "main", tableName);
            var legacyColumns = GetColumnNames(connection, LegacySchema, tableName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var sharedColumns = targetColumns
                .Where(legacyColumns.Contains)
                .ToArray();

            if (sharedColumns.Length == 0)
            {
                return 0;
            }

            var columnList = string.Join(", ", sharedColumns.Select(QuoteIdentifier));
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
INSERT OR IGNORE INTO main.{QuoteIdentifier(tableName)} ({columnList})
SELECT {columnList}
FROM {QuoteIdentifier(LegacySchema)}.{QuoteIdentifier(tableName)};";

            return command.ExecuteNonQuery();
        }

        private static int CopyLegacyTable(SqliteConnection connection, SqliteTransaction transaction, string tableName)
        {
            if (!TableExists(connection, LegacySchema, tableName))
            {
                return 0;
            }

            var sql = tableName switch
            {
                "beatmap" when TableExists(connection, "main", "beatmaps") => @"
INSERT OR IGNORE INTO main.beatmaps (
    id, artist, artist_unicode, title, title_unicode, creator, difficulty_name, audio_file_name,
    beatmap_file_name, last_modified_at, star_rating_standard, star_rating_taiko, star_rating_catch,
    star_rating_mania, drain_time_seconds, total_time_ms, preview_time_ms, osu_beatmap_id,
    osu_beatmapset_id, game_mode, source, tags, folder_name, is_local)
SELECT id, artist, artistU, title, titleU, creator, version, audioName,
       fileName, lastModified, diffSrStd, diffSrTaiko, diffSrCtb,
       diffSrMania, drainTime, totalTime, audioPreview, beatmapId,
       beatmapSetId, gameMode, source, tags, folderName, own
FROM legacy.beatmap;",
                "map_info" when TableExists(connection, "main", "beatmap_play_settings") => @"
INSERT OR IGNORE INTO main.beatmap_play_settings (
    id, difficulty_name, folder_name, is_local, audio_offset_ms, last_played_at, exported_file_path)
SELECT id, version, folder, ownDb, offset, lastPlayTime, exportFile
FROM legacy.map_info;",
                "collection" when TableExists(connection, "main", "collections") => @"
INSERT OR IGNORE INTO main.collections (
    id, name, is_locked, sort_order, cover_image_path, description, created_at)
SELECT id, name, locked, ""index"", imagePath, description, createTime
FROM legacy.collection;",
                "collection_relation" when TableExists(connection, "main", "collection_beatmaps") => @"
INSERT OR IGNORE INTO main.collection_beatmaps (
    id, collection_id, beatmap_settings_id, added_at)
SELECT id, collectionId, mapId, addTime
FROM legacy.collection_relation
WHERE EXISTS (SELECT 1 FROM main.collections WHERE main.collections.id = legacy.collection_relation.collectionId)
  AND EXISTS (SELECT 1 FROM main.beatmap_play_settings WHERE main.beatmap_play_settings.id = legacy.collection_relation.mapId);",
                "map_thumb" when TableExists(connection, "main", "beatmap_thumbnails") => @"
INSERT OR IGNORE INTO main.beatmap_thumbnails (
    id, beatmap_id, thumbnail_path)
SELECT id, mapId, thumbPath
FROM legacy.map_thumb
WHERE EXISTS (SELECT 1 FROM main.beatmaps WHERE main.beatmaps.id = legacy.map_thumb.mapId);",
                "sb_info" when TableExists(connection, "main", "storyboard_assets") => @"
INSERT OR IGNORE INTO main.storyboard_assets (
    id, beatmap_id, thumbnail_path, preview_video_path, difficulty_name, folder_name, is_local)
SELECT id, mapId, thumbPath, thumbVideoPath, version, folder, own
FROM legacy.sb_info;",
                _ => null
            };

            if (string.IsNullOrWhiteSpace(sql))
            {
                return 0;
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            return command.ExecuteNonQuery();
        }

        private static bool TableExists(SqliteConnection connection, string schemaName, string tableName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $@"
SELECT 1
FROM {QuoteIdentifier(schemaName)}.sqlite_master
WHERE type = 'table' AND name = $tableName
LIMIT 1;";
            command.Parameters.AddWithValue("$tableName", tableName);

            return command.ExecuteScalar() != null;
        }

        private static List<string> GetColumnNames(SqliteConnection connection, string schemaName, string tableName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA {QuoteIdentifier(schemaName)}.table_info({QuoteString(tableName)});";

            using var reader = command.ExecuteReader();
            var columns = new List<string>();
            while (reader.Read())
            {
                columns.Add(reader.GetString(reader.GetOrdinal("name")));
            }

            return columns;
        }

        private static string QuoteIdentifier(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string QuoteString(string value)
        {
            return "'" + value.Replace("'", "''") + "'";
        }
    }
}
