
using Coosu.Database;
using Microsoft.EntityFrameworkCore;
using OsuPlayer.Data;
using OsuPlayer.Shared;

await using var appDbContext = new ApplicationDbContext();

//await appDbContext.Database.MigrateAsync();
//var reader = new OsuDbReader(@"E:\Games\osu!\osu!.db");
//var beatmaps = reader.EnumerateDbModels();
//var syncer = new BeatmapSyncService(appDbContext);
//await syncer.SynchronizeManaged(beatmaps);
var appSettings = ConfigurationFactory.GetConfiguration<AppSettings>();


var ok2 = await appDbContext
    .SearchPlayItemsAsync("yf_bmp", BeatmapOrderOptions.Artist, 0, 300);
var first = ok2.Results.FirstOrDefault();
if (first != null)
{
    var allBeatmapsInFolder = await appDbContext.GetPlayItemDetailsByFolderAsync(first.Folder);
    var itemFull = await appDbContext.GetPlayItemByDetail(allBeatmapsInFolder[0], true);
    var playlist = appDbContext.PlayLists
        .Include(k => k.PlayItems)
        .First();
    playlist.PlayItems.Add(itemFull);
    await appDbContext.SaveChangesAsync();
}

//var item = appDbContext.PlayItems.Find(ok.Results.First().Id);
//if (item != null)
//{
//    appDbContext.PlayLists.Include(k => k.PlayItems).First().PlayItems.Add(item);
//}

