using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Milky.OsuPlayer.Data.Models;
using OSharp.Beatmap.MetaData;

namespace Milky.OsuPlayer.Data
{
    public class ApplicationDbContext : DbContext
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public DbSet<Beatmap> Beatmaps { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<CollectionRelation> Relations { get; set; }
        public DbSet<BeatmapSettings> BeatmapSettings { get; set; }
        public DbSet<BeatmapThumb> BeatmapThumbs { get; set; }
        public DbSet<BeatmapStoryboard> BeatmapStoryboards { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("data source=player.db", options => { });
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public async Task<BeatmapSettings> GetMapSetting(IMapIdentifiable id)
        {
            try
            {
                if (id.IsMapTemporary())
                {
                    Logger.Debug("需确认加入自定义目录后才可继续");
                    return null;
                }

                var map = await BeatmapSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(k => k.Version == id.Version &&
                                              k.FolderName == id.FolderNameOrPath &&
                                              k.InOwnDb == id.InOwnDb);

                if (map != null) return map;

                var guid = Guid.NewGuid();
                BeatmapSettings.Add(new BeatmapSettings
                {
                    Id = guid,
                    Version = id.Version,
                    FolderName = id.FolderNameOrPath,
                    InOwnDb = id.InOwnDb,
                });
                await SaveChangesAsync();

                return await BeatmapSettings.FindAsync(guid);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        public async Task<List<BeatmapSettings>> GetRecentList()
        {
         var list=   BeatmapSettings.Where(k=>k.)
            return ThreadedProvider.Query<BeatmapSettings>(TABLE_MAP,
                    ("lastPlayTime", null, "!="),
                    orderColumn: "lastPlayTime")
                .ToList();
        }
    }
}
