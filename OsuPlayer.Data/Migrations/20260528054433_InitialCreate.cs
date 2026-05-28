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
                name: "__OsuPlayerDataMigrationHistory",
                columns: table => new
                {
                    migrationId = table.Column<string>(type: "TEXT", nullable: false),
                    sourcePath = table.Column<string>(type: "TEXT", nullable: false),
                    migratedAtUtc = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___OsuPlayerDataMigrationHistory", x => x.migrationId);
                });

            migrationBuilder.CreateTable(
                name: "beatmap",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    artist = table.Column<string>(type: "TEXT", nullable: true),
                    artistU = table.Column<string>(type: "TEXT", nullable: true),
                    title = table.Column<string>(type: "TEXT", nullable: true),
                    titleU = table.Column<string>(type: "TEXT", nullable: true),
                    creator = table.Column<string>(type: "TEXT", nullable: true),
                    version = table.Column<string>(type: "TEXT", nullable: true),
                    audioName = table.Column<string>(type: "TEXT", nullable: true),
                    fileName = table.Column<string>(type: "TEXT", nullable: true),
                    lastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    diffSrStd = table.Column<double>(type: "REAL", nullable: false),
                    diffSrTaiko = table.Column<double>(type: "REAL", nullable: false),
                    diffSrCtb = table.Column<double>(type: "REAL", nullable: false),
                    diffSrMania = table.Column<double>(type: "REAL", nullable: false),
                    drainTime = table.Column<int>(type: "INTEGER", nullable: false),
                    totalTime = table.Column<int>(type: "INTEGER", nullable: false),
                    audioPreview = table.Column<int>(type: "INTEGER", nullable: false),
                    beatmapId = table.Column<int>(type: "INTEGER", nullable: false),
                    beatmapSetId = table.Column<int>(type: "INTEGER", nullable: false),
                    gameMode = table.Column<byte>(type: "INTEGER", nullable: false),
                    source = table.Column<string>(type: "TEXT", nullable: true),
                    tags = table.Column<string>(type: "TEXT", nullable: true),
                    folderName = table.Column<string>(type: "TEXT", nullable: true),
                    own = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_beatmap", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "collection",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    locked = table.Column<int>(type: "INTEGER", nullable: false),
                    index = table.Column<int>(type: "INTEGER", nullable: false),
                    imagePath = table.Column<string>(type: "TEXT", maxLength: 700, nullable: true),
                    description = table.Column<string>(type: "TEXT", maxLength: 700, nullable: true),
                    createTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collection", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "collection_relation",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    collectionId = table.Column<string>(type: "TEXT", nullable: false),
                    mapId = table.Column<string>(type: "TEXT", nullable: false),
                    addTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collection_relation", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "map_info",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    version = table.Column<string>(type: "TEXT", nullable: false),
                    folder = table.Column<string>(type: "TEXT", nullable: false),
                    ownDb = table.Column<bool>(type: "INTEGER", nullable: false),
                    offset = table.Column<int>(type: "INTEGER", nullable: false),
                    lastPlayTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    exportFile = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_map_info", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "map_thumb",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    mapId = table.Column<Guid>(type: "TEXT", nullable: false),
                    thumbPath = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_map_thumb", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sb_info",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    mapId = table.Column<string>(type: "TEXT", nullable: false),
                    thumbPath = table.Column<string>(type: "TEXT", nullable: false),
                    thumbVideoPath = table.Column<string>(type: "TEXT", nullable: false),
                    version = table.Column<string>(type: "TEXT", nullable: false),
                    folder = table.Column<string>(type: "TEXT", nullable: false),
                    own = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sb_info", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_beatmap_identity",
                table: "beatmap",
                columns: new[] { "folderName", "version", "own" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "__OsuPlayerDataMigrationHistory");

            migrationBuilder.DropTable(
                name: "beatmap");

            migrationBuilder.DropTable(
                name: "collection");

            migrationBuilder.DropTable(
                name: "collection_relation");

            migrationBuilder.DropTable(
                name: "map_info");

            migrationBuilder.DropTable(
                name: "map_thumb");

            migrationBuilder.DropTable(
                name: "sb_info");
        }
    }
}
