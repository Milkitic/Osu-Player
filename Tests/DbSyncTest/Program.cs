
using Coosu.Database;
using Microsoft.EntityFrameworkCore;
using OsuPlayer.Data;

await using var appDbContext = new ApplicationDbContext();

//await appDbContext.Database.MigrateAsync();
//var reader = new OsuDbReader(@"E:\Games\osu!\osu!.db");
//var beatmaps = reader.EnumerateDbModels();
//var syncer = new BeatmapSyncService(appDbContext);
//await syncer.SynchronizeManaged(beatmaps);


var ok2 = await appDbContext
    .SearchBeatmapAutoAsync("yf_bmp", BeatmapOrderOptions.Artist, 0, 300);
//var item = appDbContext.PlayItems.Find(ok.Results.First().Id);
//if (item != null)
//{
//    appDbContext.PlayLists.Include(k => k.PlayItems).First().PlayItems.Add(item);
//}

await appDbContext.SaveChangesAsync();
