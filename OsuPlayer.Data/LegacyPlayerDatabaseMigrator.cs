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

        private static readonly TableMigration[] TableMigrations =
        {
            new("beatmap", "beatmaps"),
            new("map_info", "beatmap_play_settings"),
            new("collection", "collections"),
            new("collection_relation", "collection_beatmaps"),
            new("map_thumb", "beatmap_thumbnails"),
            new("sb_info", "storyboard_assets")
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
                Logger.Info("Legacy player.db contains EF migration history; importing compatible tables.");
            }

            if (!TableMigrations.Any(migration => migration.HasSourceTable(legacyConnection, "main")))
            {
                Logger.Info("Skip legacy player.db import because no known legacy tables were found.");
                return;
            }

            AttachLegacyDatabase(appConnection, legacyFullPath);

            try
            {
                using var transaction = appConnection.BeginTransaction();
                var copiedRows = 0;
                var ignoredRows = 0;

                foreach (var migration in TableMigrations)
                {
                    var result = CopyTable(appConnection, transaction, migration);
                    copiedRows += result.InsertedRows;
                    ignoredRows += result.IgnoredRows;

                    if (result.SourceRows > 0 || result.IgnoredRows > 0)
                    {
                        Logger.Info(
                            "Imported table {0}->{1}: source={2}, inserted={3}, ignored={4}.",
                            result.SourceTable,
                            result.TargetTable,
                            result.SourceRows,
                            result.InsertedRows,
                            result.IgnoredRows);
                    }
                }

                MarkDataMigration(appConnection, transaction, legacyFullPath);
                transaction.Commit();

                Logger.Info(
                    "Imported {0} rows from legacy player.db into app.db. Ignored {1} rows due to conflicts or invalid relations.",
                    copiedRows,
                    ignoredRows);
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

        private static CopyResult CopyTable(SqliteConnection connection, SqliteTransaction transaction,
            TableMigration migration)
        {
            if (TableExists(connection, LegacySchema, migration.LegacyTableName) &&
                TableExists(connection, "main", migration.TargetTableName))
            {
                return CopyLegacyTable(connection, transaction, migration);
            }

            if (!TableExists(connection, LegacySchema, migration.TargetTableName) ||
                !TableExists(connection, "main", migration.TargetTableName))
            {
                return CopyResult.Empty(migration.LegacyTableName, migration.TargetTableName);
            }

            var targetColumns = GetColumnNames(connection, "main", migration.TargetTableName);
            var legacyColumns = GetColumnNames(connection, LegacySchema, migration.TargetTableName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var sharedColumns = targetColumns
                .Where(legacyColumns.Contains)
                .ToArray();

            if (sharedColumns.Length == 0)
            {
                return CopyResult.Empty(migration.TargetTableName, migration.TargetTableName);
            }

            var sourceRows = CountRows(connection, LegacySchema, migration.TargetTableName);
            var columnList = string.Join(", ", sharedColumns.Select(QuoteIdentifier));
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
INSERT OR IGNORE INTO main.{QuoteIdentifier(migration.TargetTableName)} ({columnList})
SELECT {columnList}
FROM {QuoteIdentifier(LegacySchema)}.{QuoteIdentifier(migration.TargetTableName)};";

            var insertedRows = command.ExecuteNonQuery();
            return new CopyResult(migration.TargetTableName, migration.TargetTableName, sourceRows, insertedRows);
        }

        private static CopyResult CopyLegacyTable(SqliteConnection connection, SqliteTransaction transaction,
            TableMigration migration)
        {
            if (!TableExists(connection, LegacySchema, migration.LegacyTableName))
            {
                return CopyResult.Empty(migration.LegacyTableName, migration.TargetTableName);
            }

            var sourceCountSql = migration.LegacyTableName switch
            {
                "collection_relation" => @"
SELECT COUNT(*)
FROM legacy.collection_relation
WHERE EXISTS (SELECT 1 FROM main.collections WHERE main.collections.id = legacy.collection_relation.collectionId)
  AND EXISTS (SELECT 1 FROM main.beatmap_play_settings WHERE main.beatmap_play_settings.id = legacy.collection_relation.mapId);",
                "map_thumb" => @"
SELECT COUNT(*)
FROM legacy.map_thumb
WHERE EXISTS (SELECT 1 FROM main.beatmaps WHERE main.beatmaps.id = legacy.map_thumb.mapId);",
                _ => $"SELECT COUNT(*) FROM {QuoteIdentifier(LegacySchema)}.{QuoteIdentifier(migration.LegacyTableName)};"
            };

            var sql = migration.LegacyTableName switch
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
                return CopyResult.Empty(migration.LegacyTableName, migration.TargetTableName);
            }

            var sourceRows = CountRows(connection, sourceCountSql);
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            var insertedRows = command.ExecuteNonQuery();
            return new CopyResult(migration.LegacyTableName, migration.TargetTableName, sourceRows, insertedRows);
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

        private static int CountRows(SqliteConnection connection, string schemaName, string tableName)
        {
            return CountRows(connection,
                $"SELECT COUNT(*) FROM {QuoteIdentifier(schemaName)}.{QuoteIdentifier(tableName)};");
        }

        private static int CountRows(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private sealed class TableMigration
        {
            public TableMigration(string legacyTableName, string targetTableName)
            {
                LegacyTableName = legacyTableName;
                TargetTableName = targetTableName;
            }

            public string LegacyTableName { get; }

            public string TargetTableName { get; }

            public bool HasSourceTable(SqliteConnection connection, string schemaName)
            {
                return TableExists(connection, schemaName, LegacyTableName) ||
                       TableExists(connection, schemaName, TargetTableName);
            }
        }

        private readonly struct CopyResult
        {
            public CopyResult(string sourceTable, string targetTable, int sourceRows, int insertedRows)
            {
                SourceTable = sourceTable;
                TargetTable = targetTable;
                SourceRows = sourceRows;
                InsertedRows = insertedRows;
            }

            public string SourceTable { get; }

            public string TargetTable { get; }

            public int SourceRows { get; }

            public int InsertedRows { get; }

            public int IgnoredRows => Math.Max(0, SourceRows - InsertedRows);

            public static CopyResult Empty(string sourceTable, string targetTable)
            {
                return new CopyResult(sourceTable, targetTable, 0, 0);
            }
        }
    }
}
