using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqKit;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Common.Metadata;
using OSharp.Beatmap.MetaData;
using osu_database_reader.Components.Beatmaps;

namespace Milky.OsuPlayer.Common.Data.EF
{
    public class BeatmapDbOperator
    {
        [ThreadStatic]
        private static BeatmapDbContext _ctx;

        private BeatmapDbContext Ctx
        {
            get
            {
                if (_ctx == null || _ctx.IsDisposed())
                    _ctx = new BeatmapDbContext();
                return _ctx;
            }
        }

        public Beatmap GetBeatmapBy(Func<Beatmap, bool> predicate)
        {
            return Ctx.Beatmaps
                .FirstOrDefault(predicate);
        }

        public List<Beatmap> SearchBeatmapByOptions(string searchText, SortMode sortMode, int startIndex, int count)
        {
            return Ctx.Beatmaps
                .ByKeyword(searchText)
                .OrderAndTake(sortMode, startIndex, count)
                .ToList();
        }

        public Beatmap GetBeatmapByIdentifiable(IMapIdentifiable map)
        {
            var entity = Ctx.Beatmaps.FirstOrDefault(full =>
                             map.FolderName == full.FolderName &&
                         map.Version == full.Version);

            return entity;
        }

        public List<Beatmap> GetBeatmapsByMapInfo(List<MapInfo> reqList, TimeSortMode sortMode)
        {
            var entities = GetBeatmapsByIdentifiable(reqList);

            var newList = reqList.Join(entities,
                mapInfo => mapInfo.GetIdentity(),
                entry => entry.GetIdentity(),
                (mapInfo, entry) => new
                {
                    entry,
                    playTime = mapInfo.LastPlayTime ?? new DateTime(),
                    addTime = mapInfo.AddTime ?? new DateTime()
                });

            return sortMode == TimeSortMode.PlayTime
                ? newList.OrderByDescending(k => k.playTime).Select(k => k.entry).ToList()
                : newList.OrderByDescending(k => k.addTime).Select(k => k.entry).ToList();
        }

        public List<Beatmap> GetBeatmapsFromFolder(string folder)
        {
            return Ctx.Beatmaps.Where(k => k.FolderName == folder).ToList();
        }



        public List<Beatmap> GetBeatmapsByIdentifiable<T>(List<T> reqList)
            where T : IMapIdentifiable
        {
            var folders = reqList.Select(k => k.FolderName).ToList();
            var checkExpr = Ctx.Beatmaps.Where(entity => folders.Contains(entity.FolderName));
            var entities = checkExpr.ToList();

            var filter = entities.Where(full =>
                    reqList.Any(req => req.FolderName.Contains(full.FolderName) &&
                                       req.Version.Contains(full.Version)))
                .ToList();
            return filter;
        }

        public async Task SyncMapsFromHoLLyAsync(IEnumerable<BeatmapEntry> entry, bool addOnly)
        {
            if (addOnly)
            {
                var dbMaps = Ctx.Beatmaps.Where(k => !k.InOwnFolder);
                var newList = entry.Select(Beatmap.ParseFromHolly);
                var except = newList.Except(dbMaps, new Beatmap.Comparer(true));

                Ctx.Beatmaps.AddRange(except);
                await Ctx.SaveChangesAsync();
            }
            else
            {
                var dbMaps = Ctx.Beatmaps.Where(k => !k.InOwnFolder);
                Ctx.Beatmaps.RemoveRange(dbMaps);

                var osuMaps = entry.Select(Beatmap.ParseFromHolly);
                Ctx.Beatmaps.AddRange(osuMaps);

                await Ctx.SaveChangesAsync();
            }
        }

        public async Task AddNewMapAsync(Beatmap beatmap)
        {
            Ctx.Beatmaps.Add(beatmap);
            await Ctx.SaveChangesAsync();
        }

        public async Task RemoveLocalAllAsync()
        {
            var locals = Ctx.Beatmaps.Where(k => k.InOwnFolder);
            Ctx.Beatmaps.RemoveRange(locals);
            await Ctx.SaveChangesAsync();
        }
    }

    public enum TimeSortMode
    {
        PlayTime, AddTime
    }

    public static class BeatmapDbContextExt
    {
        public static IQueryable<Beatmap> ByKeyword(this IQueryable<Beatmap> queryableBeatmaps,
            string keywordStr)
        {
            if (string.IsNullOrWhiteSpace(keywordStr))
            {
                return queryableBeatmaps;
            }

            var keywords = keywordStr.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            var predicate = keywords.Aggregate(PredicateBuilder.New<Beatmap>(), (current, keyword) =>
                current.And(k => k.Title != null && k.Title.ToLower().Contains(keyword) ||
                                k.TitleUnicode != null && k.TitleUnicode.ToLower().Contains(keyword) ||
                                k.Artist != null && k.Artist.ToLower().Contains(keyword) ||
                                k.ArtistUnicode != null && k.ArtistUnicode.ToLower().Contains(keyword) ||
                                k.SongTags != null && k.SongTags.ToLower().Contains(keyword) ||
                                k.SongSource != null && k.SongSource.ToLower().Contains(keyword) ||
                                k.Creator != null && k.Creator.ToLower().Contains(keyword) ||
                                k.Version != null && k.Version.ToLower().Contains(keyword)
                )
            );

            return queryableBeatmaps.Where(predicate);
        }

        public static IQueryable<Beatmap> OrderAndTake(this IQueryable<Beatmap> queryableBeatmaps,
            SortMode sortMode,
            int startIndex,
            int count)
        {
            IQueryable<Beatmap> query;
            switch (sortMode)
            {
                case SortMode.Artist:
                    query = queryableBeatmaps
                        .OrderBy(k => k.ArtistUnicode)
                        .ThenBy(k => k.Artist);
                    break;
                case SortMode.Title:
                    query = queryableBeatmaps
                        .OrderBy(k => k.TitleUnicode)
                        .ThenBy(k => k.Title);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sortMode), sortMode, nameof(sortMode));
            }

            return query.Skip(startIndex).Take(count);
        }
    }
}