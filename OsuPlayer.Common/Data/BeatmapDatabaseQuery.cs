using Milky.OsuPlayer.Common.Data.EF;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Common.Metadata;
using OSharp.Beatmap;
using OSharp.Common;
using osu.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OSharp.Beatmap.MetaData;
using GameMode = OSharp.Beatmap.Sections.GamePlay.GameMode;

namespace Milky.OsuPlayer.Common.Data
{
    public static class BeatmapDatabaseQuery
    {
        public static async Task<List<Beatmap>> GetWholeListFromDbAsync()
        {
            return await Task.Run(() =>
            {
                using (var context = new BeatmapDbContext())
                {
                    return context.Beatmaps.ToList();
                }
            });
        }
    }
}
