using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Milky.OsuPlayer.Common.Data.EF;
using Milky.OsuPlayer.Common.Data.EF.Model;
using osu_database_reader.Components.Beatmaps;

namespace Milky.OsuPlayer.Common.Data
{
    public class BeatmapDbOperator : IDisposable
    {
        private readonly BeatmapDbContext _beatmapDbContext;

        public BeatmapDbOperator()
        {
            _beatmapDbContext = new BeatmapDbContext();
        }

        public static async Task SyncMapsFromHoLLyAsync(IEnumerable<BeatmapEntry> entry, bool addOnly)
        {
            using (var context = new BeatmapDbContext())
            {
                if (addOnly)
                {
                    var dbMaps = context.Beatmaps.Where(k => !k.InOwnFolder);
                    var newList = entry.Select(Beatmap.ParseFromHolly);
                    var except = newList.Except(dbMaps, new Beatmap.Comparer(true));

                    context.Beatmaps.AddRange(except);
                    await context.SaveChangesAsync();
                }
                else
                {
                    var dbMaps = context.Beatmaps.Where(k => !k.InOwnFolder);
                    context.Beatmaps.RemoveRange(dbMaps);

                    var osuMaps = entry.Select(Beatmap.ParseFromHolly);
                    context.Beatmaps.AddRange(osuMaps);

                    await context.SaveChangesAsync();
                }
            }
        }

        public async Task AddNewMapAsync(Beatmap beatmap)
        {
            _beatmapDbContext.Beatmaps.Add(beatmap);
            await _beatmapDbContext.SaveChangesAsync();
        }

        public void Dispose()
        {
            _beatmapDbContext?.Dispose();
        }

        public static async Task RemoveLocalAllAsync()
        {
            using (var context = new BeatmapDbContext())
            {
                var locals = context.Beatmaps.Where(k => k.InOwnFolder);
                context.Beatmaps.RemoveRange(locals);
                await context.SaveChangesAsync();
            }
        }
    }
}
