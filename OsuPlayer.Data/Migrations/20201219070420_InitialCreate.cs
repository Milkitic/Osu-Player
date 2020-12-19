using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Milky.OsuPlayer.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Beatmaps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Artist = table.Column<string>(type: "TEXT", nullable: true),
                    ArtistUnicode = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    TitleUnicode = table.Column<string>(type: "TEXT", nullable: true),
                    Creator = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    BeatmapFileName = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DiffSrNoneStandard = table.Column<double>(type: "REAL", nullable: false),
                    DiffSrNoneTaiko = table.Column<double>(type: "REAL", nullable: false),
                    DiffSrNoneCtB = table.Column<double>(type: "REAL", nullable: false),
                    DiffSrNoneMania = table.Column<double>(type: "REAL", nullable: false),
                    DrainTimeSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTime = table.Column<int>(type: "INTEGER", nullable: false),
                    AudioPreviewTime = table.Column<int>(type: "INTEGER", nullable: false),
                    BeatmapId = table.Column<int>(type: "INTEGER", nullable: false),
                    BeatmapSetId = table.Column<int>(type: "INTEGER", nullable: false),
                    GameMode = table.Column<int>(type: "INTEGER", nullable: false),
                    SongSource = table.Column<string>(type: "TEXT", nullable: true),
                    SongTags = table.Column<string>(type: "TEXT", nullable: true),
                    FolderNameOrPath = table.Column<string>(type: "TEXT", nullable: true),
                    AudioFileName = table.Column<string>(type: "TEXT", nullable: true),
                    InOwnDb = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beatmaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Collections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BeatmapConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MainVolume = table.Column<int>(type: "INTEGER", nullable: true),
                    MusicVolume = table.Column<int>(type: "INTEGER", nullable: true),
                    HitsoundVolume = table.Column<int>(type: "INTEGER", nullable: true),
                    SampleVolume = table.Column<int>(type: "INTEGER", nullable: true),
                    Offset = table.Column<int>(type: "INTEGER", nullable: true),
                    PlaybackRate = table.Column<float>(type: "REAL", nullable: true),
                    PlayUseTempo = table.Column<bool>(type: "INTEGER", nullable: true),
                    LyricOffset = table.Column<int>(type: "INTEGER", nullable: true),
                    ForceLyricId = table.Column<string>(type: "TEXT", nullable: true),
                    BeatmapId = table.Column<string>(type: "TEXT", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeatmapConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BeatmapConfigs_Beatmaps_BeatmapId",
                        column: x => x.BeatmapId,
                        principalTable: "Beatmaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Exports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExportPath = table.Column<string>(type: "TEXT", nullable: true),
                    BeatmapId = table.Column<string>(type: "TEXT", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exports_Beatmaps_BeatmapId",
                        column: x => x.BeatmapId,
                        principalTable: "Beatmaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Playlist",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlayTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    BeatmapId = table.Column<string>(type: "TEXT", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playlist", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Playlist_Beatmaps_BeatmapId",
                        column: x => x.BeatmapId,
                        principalTable: "Beatmaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecentList",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlayTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BeatmapId = table.Column<string>(type: "TEXT", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecentList", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecentList_Beatmaps_BeatmapId",
                        column: x => x.BeatmapId,
                        principalTable: "Beatmaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Storyboards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StoryboardVideoPath = table.Column<string>(type: "TEXT", nullable: true),
                    BeatmapId = table.Column<string>(type: "TEXT", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Storyboards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Storyboards_Beatmaps_BeatmapId",
                        column: x => x.BeatmapId,
                        principalTable: "Beatmaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BeatmapCollection",
                columns: table => new
                {
                    BeatmapsId = table.Column<string>(type: "TEXT", nullable: false),
                    CollectionsId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeatmapCollection", x => new { x.BeatmapsId, x.CollectionsId });
                    table.ForeignKey(
                        name: "FK_BeatmapCollection_Beatmaps_BeatmapsId",
                        column: x => x.BeatmapsId,
                        principalTable: "Beatmaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BeatmapCollection_Collections_CollectionsId",
                        column: x => x.CollectionsId,
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Relations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CollectionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BeatmapId = table.Column<string>(type: "TEXT", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Relations_Beatmaps_BeatmapId",
                        column: x => x.BeatmapId,
                        principalTable: "Beatmaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Relations_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Thumbs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ThumbPath = table.Column<string>(type: "TEXT", nullable: true),
                    VideoPath = table.Column<string>(type: "TEXT", nullable: true),
                    BeatmapStoryboardId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BeatmapId = table.Column<string>(type: "TEXT", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Thumbs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Thumbs_Beatmaps_BeatmapId",
                        column: x => x.BeatmapId,
                        principalTable: "Beatmaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Thumbs_Storyboards_BeatmapStoryboardId",
                        column: x => x.BeatmapStoryboardId,
                        principalTable: "Storyboards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Collections",
                columns: new[] { "Id", "CreateTime", "Description", "ImagePath", "Index", "IsDefault", "Name", "UpdateTime" },
                values: new object[] { new Guid("7e3d1fed-49db-4899-9775-cb4893e547a1"), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, 0, true, "Favorite", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.CreateIndex(
                name: "IX_BeatmapCollection_CollectionsId",
                table: "BeatmapCollection",
                column: "CollectionsId");

            migrationBuilder.CreateIndex(
                name: "IX_BeatmapConfigs_BeatmapId",
                table: "BeatmapConfigs",
                column: "BeatmapId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exports_BeatmapId",
                table: "Exports",
                column: "BeatmapId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Playlist_BeatmapId",
                table: "Playlist",
                column: "BeatmapId");

            migrationBuilder.CreateIndex(
                name: "IX_RecentList_BeatmapId",
                table: "RecentList",
                column: "BeatmapId");

            migrationBuilder.CreateIndex(
                name: "IX_Relations_BeatmapId",
                table: "Relations",
                column: "BeatmapId");

            migrationBuilder.CreateIndex(
                name: "IX_Relations_CollectionId",
                table: "Relations",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Storyboards_BeatmapId",
                table: "Storyboards",
                column: "BeatmapId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Thumbs_BeatmapId",
                table: "Thumbs",
                column: "BeatmapId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Thumbs_BeatmapStoryboardId",
                table: "Thumbs",
                column: "BeatmapStoryboardId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BeatmapCollection");

            migrationBuilder.DropTable(
                name: "BeatmapConfigs");

            migrationBuilder.DropTable(
                name: "Exports");

            migrationBuilder.DropTable(
                name: "Playlist");

            migrationBuilder.DropTable(
                name: "RecentList");

            migrationBuilder.DropTable(
                name: "Relations");

            migrationBuilder.DropTable(
                name: "Thumbs");

            migrationBuilder.DropTable(
                name: "Collections");

            migrationBuilder.DropTable(
                name: "Storyboards");

            migrationBuilder.DropTable(
                name: "Beatmaps");
        }
    }
}
