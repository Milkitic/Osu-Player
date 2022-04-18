using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OsuPlayer.Data.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CurrentPlaying",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    LastPlay = table.Column<long>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Artist = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Creator = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PlayItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    PlayItemPath = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentPlaying", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExportList",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    ExportPath = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    ExportTime = table.Column<long>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Artist = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Creator = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PlayItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    PlayItemPath = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportList", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayItemAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ThumbPath = table.Column<string>(type: "TEXT", nullable: true),
                    VideoPath = table.Column<string>(type: "TEXT", nullable: true),
                    StoryboardVideoPath = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayItemAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayItemConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Offset = table.Column<int>(type: "INTEGER", nullable: false),
                    LyricOffset = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayItemConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayItemDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Artist = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ArtistUnicode = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    TitleUnicode = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Creator = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    BeatmapFileName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DefaultStarRatingStd = table.Column<long>(type: "INTEGER", nullable: false),
                    DefaultStarRatingTaiko = table.Column<long>(type: "INTEGER", nullable: false),
                    DefaultStarRatingCtB = table.Column<long>(type: "INTEGER", nullable: false),
                    DefaultStarRatingMania = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalTime = table.Column<long>(type: "INTEGER", nullable: false),
                    BeatmapId = table.Column<int>(type: "INTEGER", nullable: false),
                    BeatmapSetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    FolderName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    AudioFileName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UpdateTime = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayItemDetails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreateTime = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdateTime = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SoftwareStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShowWelcome = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowFullNavigation = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowMinimalWindow = table.Column<bool>(type: "INTEGER", nullable: false),
                    MinimalWindowPosition = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    MinimalWindowWorkingArea = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    LastUpdateCheck = table.Column<long>(type: "INTEGER", nullable: true),
                    LastSync = table.Column<long>(type: "INTEGER", nullable: true),
                    IgnoredVersion = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Folder = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    IsAutoManaged = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlayItemDetailId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayItemConfigId = table.Column<int>(type: "INTEGER", nullable: true),
                    PlayItemAssetId = table.Column<int>(type: "INTEGER", nullable: true),
                    LastPlay = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayItems_PlayItemAssets_PlayItemAssetId",
                        column: x => x.PlayItemAssetId,
                        principalTable: "PlayItemAssets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlayItems_PlayItemConfigs_PlayItemConfigId",
                        column: x => x.PlayItemConfigId,
                        principalTable: "PlayItemConfigs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlayItems_PlayItemDetails_PlayItemDetailId",
                        column: x => x.PlayItemDetailId,
                        principalTable: "PlayItemDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayListRelations",
                columns: table => new
                {
                    PlayListId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayItemId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayListRelations", x => new { x.PlayItemId, x.PlayListId });
                    table.ForeignKey(
                        name: "FK_PlayListRelations_PlayItems_PlayItemId",
                        column: x => x.PlayItemId,
                        principalTable: "PlayItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayListRelations_PlayLists_PlayListId",
                        column: x => x.PlayListId,
                        principalTable: "PlayLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "PlayLists",
                columns: new[] { "Id", "CreateTime", "Description", "ImagePath", "Index", "IsDefault", "Name", "UpdateTime" },
                values: new object[] { 1, 0L, null, null, 0, true, "Favorite", 0L });

            migrationBuilder.CreateIndex(
                name: "IX_CurrentPlaying_Index",
                table: "CurrentPlaying",
                column: "Index");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentPlaying_LastPlay",
                table: "CurrentPlaying",
                column: "LastPlay");

            migrationBuilder.CreateIndex(
                name: "IX_PlayItemDetails_Artist",
                table: "PlayItemDetails",
                column: "Artist");

            migrationBuilder.CreateIndex(
                name: "IX_PlayItemDetails_Artist_ArtistUnicode_Title_TitleUnicode_Creator_Source_Tags",
                table: "PlayItemDetails",
                columns: new[] { "Artist", "ArtistUnicode", "Title", "TitleUnicode", "Creator", "Source", "Tags" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayItemDetails_ArtistUnicode",
                table: "PlayItemDetails",
                column: "ArtistUnicode");

            migrationBuilder.CreateIndex(
                name: "IX_PlayItemDetails_Creator",
                table: "PlayItemDetails",
                column: "Creator");

            migrationBuilder.CreateIndex(
                name: "IX_PlayItemDetails_Source",
                table: "PlayItemDetails",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_PlayItemDetails_Tags",
                table: "PlayItemDetails",
                column: "Tags");

            migrationBuilder.CreateIndex(
                name: "IX_PlayItemDetails_Title",
                table: "PlayItemDetails",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_PlayItemDetails_TitleUnicode",
                table: "PlayItemDetails",
                column: "TitleUnicode");

            migrationBuilder.CreateIndex(
                name: "IX_PlayItems_Folder",
                table: "PlayItems",
                column: "Folder");

            migrationBuilder.CreateIndex(
                name: "IX_PlayItems_Path",
                table: "PlayItems",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayItems_PlayItemAssetId",
                table: "PlayItems",
                column: "PlayItemAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayItems_PlayItemConfigId",
                table: "PlayItems",
                column: "PlayItemConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayItems_PlayItemDetailId",
                table: "PlayItems",
                column: "PlayItemDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayListRelations_PlayListId",
                table: "PlayListRelations",
                column: "PlayListId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayLists_Index",
                table: "PlayLists",
                column: "Index");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurrentPlaying");

            migrationBuilder.DropTable(
                name: "ExportList");

            migrationBuilder.DropTable(
                name: "PlayListRelations");

            migrationBuilder.DropTable(
                name: "SoftwareStates");

            migrationBuilder.DropTable(
                name: "PlayItems");

            migrationBuilder.DropTable(
                name: "PlayLists");

            migrationBuilder.DropTable(
                name: "PlayItemAssets");

            migrationBuilder.DropTable(
                name: "PlayItemConfigs");

            migrationBuilder.DropTable(
                name: "PlayItemDetails");
        }
    }
}
