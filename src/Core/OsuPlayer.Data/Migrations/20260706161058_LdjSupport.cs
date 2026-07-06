using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OsuPlayer.Data.Migrations
{
    /// <inheritdoc />
    public partial class LdjSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_beatmaps_identity",
                table: "beatmaps");

            migrationBuilder.DropIndex(
                name: "ux_beatmap_play_settings_identity",
                table: "beatmap_play_settings");

            migrationBuilder.AddColumn<short>(
                name: "iidx_bga_delay",
                table: "beatmaps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "iidx_bgm_volume",
                table: "beatmaps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "iidx_file_identifier",
                table: "beatmaps",
                type: "INTEGER",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<int>(
                name: "iidx_music_id",
                table: "beatmaps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<short>(
                name: "iidx_version",
                table: "beatmaps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "source_game",
                table: "beatmaps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "source_game",
                table: "beatmap_play_settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_beatmaps_iidx_source_music_id",
                table: "beatmaps",
                columns: new[] { "source_game", "iidx_music_id" });

            migrationBuilder.CreateIndex(
                name: "ix_beatmaps_source_game",
                table: "beatmaps",
                column: "source_game");

            migrationBuilder.CreateIndex(
                name: "ux_beatmaps_identity",
                table: "beatmaps",
                columns: new[] { "source_game", "folder_name", "difficulty_name", "is_local" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_beatmap_play_settings_identity",
                table: "beatmap_play_settings",
                columns: new[] { "source_game", "folder_name", "difficulty_name", "is_local" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_beatmaps_iidx_source_music_id",
                table: "beatmaps");

            migrationBuilder.DropIndex(
                name: "ix_beatmaps_source_game",
                table: "beatmaps");

            migrationBuilder.DropIndex(
                name: "ux_beatmaps_identity",
                table: "beatmaps");

            migrationBuilder.DropIndex(
                name: "ux_beatmap_play_settings_identity",
                table: "beatmap_play_settings");

            migrationBuilder.DropColumn(
                name: "iidx_bga_delay",
                table: "beatmaps");

            migrationBuilder.DropColumn(
                name: "iidx_bgm_volume",
                table: "beatmaps");

            migrationBuilder.DropColumn(
                name: "iidx_file_identifier",
                table: "beatmaps");

            migrationBuilder.DropColumn(
                name: "iidx_music_id",
                table: "beatmaps");

            migrationBuilder.DropColumn(
                name: "iidx_version",
                table: "beatmaps");

            migrationBuilder.DropColumn(
                name: "source_game",
                table: "beatmaps");

            migrationBuilder.DropColumn(
                name: "source_game",
                table: "beatmap_play_settings");

            migrationBuilder.CreateIndex(
                name: "ux_beatmaps_identity",
                table: "beatmaps",
                columns: new[] { "folder_name", "difficulty_name", "is_local" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_beatmap_play_settings_identity",
                table: "beatmap_play_settings",
                columns: new[] { "folder_name", "difficulty_name", "is_local" },
                unique: true);
        }
    }
}
