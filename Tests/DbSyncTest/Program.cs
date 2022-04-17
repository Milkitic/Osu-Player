
using Coosu.Database;
using Microsoft.EntityFrameworkCore;
using OsuPlayer.Data;

await using var appDbContext = new ApplicationDbContext();

await appDbContext.Database.MigrateAsync();
var reader = new OsuDbReader(@"E:\Games\osu!\osu!.db");
var beatmaps = reader.EnumerateDbModels();
var syncer = new BeatmapSyncService(appDbContext);
await syncer.SynchronizeManaged(beatmaps);


await appDbContext.SearchBeatmapAsync("Camellia", BeatmapOrderOptions.ArtistUnicode, 0, 100);
