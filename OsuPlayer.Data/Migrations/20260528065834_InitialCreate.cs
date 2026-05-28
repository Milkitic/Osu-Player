using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Milky.OsuPlayer.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "beatmap_play_settings",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    difficulty_name = table.Column<string>(type: "TEXT", nullable: false),
                    folder_name = table.Column<string>(type: "TEXT", nullable: false),
                    is_local = table.Column<bool>(type: "INTEGER", nullable: false),
                    audio_offset_ms = table.Column<int>(type: "INTEGER", nullable: false),
                    last_played_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    exported_file_path = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_beatmap_play_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "beatmap_thumbnails",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    beatmap_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    thumbnail_path = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_beatmap_thumbnails", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "beatmaps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    artist = table.Column<string>(type: "TEXT", nullable: true),
                    artist_unicode = table.Column<string>(type: "TEXT", nullable: true),
                    title = table.Column<string>(type: "TEXT", nullable: true),
                    title_unicode = table.Column<string>(type: "TEXT", nullable: true),
                    creator = table.Column<string>(type: "TEXT", nullable: true),
                    difficulty_name = table.Column<string>(type: "TEXT", nullable: true),
                    audio_file_name = table.Column<string>(type: "TEXT", nullable: true),
                    beatmap_file_name = table.Column<string>(type: "TEXT", nullable: true),
                    last_modified_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    star_rating_standard = table.Column<double>(type: "REAL", nullable: false),
                    star_rating_taiko = table.Column<double>(type: "REAL", nullable: false),
                    star_rating_catch = table.Column<double>(type: "REAL", nullable: false),
                    star_rating_mania = table.Column<double>(type: "REAL", nullable: false),
                    drain_time_seconds = table.Column<int>(type: "INTEGER", nullable: false),
                    total_time_ms = table.Column<int>(type: "INTEGER", nullable: false),
                    preview_time_ms = table.Column<int>(type: "INTEGER", nullable: false),
                    osu_beatmap_id = table.Column<int>(type: "INTEGER", nullable: false),
                    osu_beatmapset_id = table.Column<int>(type: "INTEGER", nullable: false),
                    game_mode = table.Column<byte>(type: "INTEGER", nullable: false),
                    source = table.Column<string>(type: "TEXT", nullable: true),
                    tags = table.Column<string>(type: "TEXT", nullable: true),
                    folder_name = table.Column<string>(type: "TEXT", nullable: true),
                    is_local = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_beatmaps", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "collections",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    is_locked = table.Column<int>(type: "INTEGER", nullable: false),
                    sort_order = table.Column<int>(type: "INTEGER", nullable: false),
                    cover_image_path = table.Column<string>(type: "TEXT", maxLength: 700, nullable: true),
                    description = table.Column<string>(type: "TEXT", maxLength: 700, nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "storyboard_assets",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    beatmap_id = table.Column<string>(type: "TEXT", nullable: false),
                    thumbnail_path = table.Column<string>(type: "TEXT", nullable: false),
                    preview_video_path = table.Column<string>(type: "TEXT", nullable: false),
                    difficulty_name = table.Column<string>(type: "TEXT", nullable: false),
                    folder_name = table.Column<string>(type: "TEXT", nullable: false),
                    is_local = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storyboard_assets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "collection_beatmaps",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    collection_id = table.Column<string>(type: "TEXT", nullable: false),
                    beatmap_settings_id = table.Column<string>(type: "TEXT", nullable: false),
                    added_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collection_beatmaps", x => x.id);
                    table.ForeignKey(
                        name: "FK_collection_beatmaps_beatmap_play_settings_beatmap_settings_id",
                        column: x => x.beatmap_settings_id,
                        principalTable: "beatmap_play_settings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_collection_beatmaps_collections_collection_id",
                        column: x => x.collection_id,
                        principalTable: "collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_beatmap_play_settings_exported_file_path",
                table: "beatmap_play_settings",
                column: "exported_file_path");

            migrationBuilder.CreateIndex(
                name: "ix_beatmap_play_settings_last_played_at",
                table: "beatmap_play_settings",
                column: "last_played_at");

            migrationBuilder.CreateIndex(
                name: "ux_beatmap_play_settings_identity",
                table: "beatmap_play_settings",
                columns: new[] { "folder_name", "difficulty_name", "is_local" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_beatmap_thumbnails_beatmap_id",
                table: "beatmap_thumbnails",
                column: "beatmap_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_beatmaps_folder_name",
                table: "beatmaps",
                column: "folder_name");

            migrationBuilder.CreateIndex(
                name: "ix_beatmaps_osu_beatmapset_id",
                table: "beatmaps",
                column: "osu_beatmapset_id");

            migrationBuilder.CreateIndex(
                name: "ux_beatmaps_identity",
                table: "beatmaps",
                columns: new[] { "folder_name", "difficulty_name", "is_local" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_collection_beatmaps_beatmap_settings_id",
                table: "collection_beatmaps",
                column: "beatmap_settings_id");

            migrationBuilder.CreateIndex(
                name: "ux_collection_beatmaps_collection_map",
                table: "collection_beatmaps",
                columns: new[] { "collection_id", "beatmap_settings_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_collections_name",
                table: "collections",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_collections_sort_order",
                table: "collections",
                column: "sort_order");

            migrationBuilder.CreateIndex(
                name: "ux_storyboard_assets_beatmap_id",
                table: "storyboard_assets",
                column: "beatmap_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "beatmap_thumbnails");

            migrationBuilder.DropTable(
                name: "beatmaps");

            migrationBuilder.DropTable(
                name: "collection_beatmaps");

            migrationBuilder.DropTable(
                name: "storyboard_assets");

            migrationBuilder.DropTable(
                name: "beatmap_play_settings");

            migrationBuilder.DropTable(
                name: "collections");
        }
    }
}
