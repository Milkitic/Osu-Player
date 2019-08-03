using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Milky.OsuPlayer.Common.Data.EF;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Metadata;
using OSharp.Beatmap;
using OSharp.Beatmap.MetaData;
using OSharp.Beatmap.Sections.GamePlay;
using OSharp.Common;

namespace Milky.OsuPlayer.Common.Data
{
    public static class AppDbOperatorExt
    {
        private static readonly ConcurrentRandom Random = new ConcurrentRandom();

        public static Beatmap GetHighestDiff(this IEnumerable<Beatmap> enumerable)
        {
            var dictionary = enumerable.GroupBy(k => k.GameMode).ToDictionary(k => k.Key, k => k.ToList());
            if (dictionary.ContainsKey(GameMode.Circle))
            {
                return dictionary[GameMode.Circle].Aggregate((i1, i2) => i1.DiffSrNoneStandard > i2.DiffSrNoneStandard ? i1 : i2);
            }
            if (dictionary.ContainsKey(GameMode.Mania))
            {
                return dictionary[GameMode.Mania].Aggregate((i1, i2) => i1.DiffSrNoneMania > i2.DiffSrNoneMania ? i1 : i2);
            }

            if (dictionary.ContainsKey(GameMode.Catch))
            {
                return dictionary[GameMode.Catch].Aggregate((i1, i2) => i1.DiffSrNoneCtB > i2.DiffSrNoneCtB ? i1 : i2);
            }

            if (dictionary.ContainsKey(GameMode.Taiko))
            {
                return dictionary[GameMode.Taiko].Aggregate((i1, i2) => i1.DiffSrNoneTaiko > i2.DiffSrNoneTaiko ? i1 : i2);
            }

            Console.WriteLine(@"Get highest difficulty failed.");
            var randKey = dictionary.Keys.ToList()[Random.Next(dictionary.Keys.Count)];
            return dictionary[randKey][dictionary[randKey].Count];
        }

        public static bool IsDisposed(this DbContext context)
        {
            var result = true;

            var typeDbContext = typeof(DbContext);
            var typeInternalContext = typeDbContext.Assembly.GetType("System.Data.Entity.Internal.InternalContext");

            var fi_InternalContext = typeDbContext.GetField("_internalContext", BindingFlags.NonPublic | BindingFlags.Instance);
            var pi_IsDisposed = typeInternalContext.GetProperty("IsDisposed");

            var ic = fi_InternalContext.GetValue(context);

            if (ic != null)
            {
                result = (bool)pi_IsDisposed.GetValue(ic);
            }

            return result;
        }
    }
}
