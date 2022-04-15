using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OsuPlayer.Data.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    LastModified = table.Column<long>(type: "INTEGER", nullable: false),
                    DefaultStarRatingStd = table.Column<long>(type: "INTEGER", nullable: false),
                    DefaultStarRatingTaiko = table.Column<long>(type: "INTEGER", nullable: false),
                    DefaultStarRatingCtB = table.Column<long>(type: "INTEGER", nullable: false),
                    DefaultStarRatingMania = table.Column<long>(type: "INTEGER", nullable: false),
                    DrainTime = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalTime = table.Column<long>(type: "INTEGER", nullable: false),
                    AudioPreviewTime = table.Column<long>(type: "INTEGER", nullable: false),
                    BeatmapId = table.Column<int>(type: "INTEGER", nullable: false),
                    BeatmapSetId = table.Column<int>(type: "INTEGER", nullable: false),
                    GameMode = table.Column<byte>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    FolderName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    AudioFileName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayItemDetails", x => x.Id);
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
                    Path = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    IsAutoManaged = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlayItemDetailId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayItems_PlayItemDetails_PlayItemDetailId",
                        column: x => x.PlayItemDetailId,
                        principalTable: "PlayItemDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayItems_Path",
                table: "PlayItems",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayItems_PlayItemDetailId",
                table: "PlayItems",
                column: "PlayItemDetailId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayItems");

            migrationBuilder.DropTable(
                name: "SoftwareStates");

            migrationBuilder.DropTable(
                name: "PlayItemDetails");
        }
    }
}
