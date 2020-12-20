using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Presentation.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace Milky.OsuPlayer
{
    public class OrderedBeatmap : OrderedModel<Beatmap>
    {
        public OrderedBeatmap()
        {
        }

        public OrderedBeatmap(int i, Beatmap beatmap) : base(i, beatmap)
        {
        }
    }

    public static class OrderedModelExtension
    {
        public static IEnumerable<OrderedBeatmap> AsOrderedBeatmap(this IEnumerable<Beatmap> orderedModel)
        {
            return orderedModel.Select((k, i) => new OrderedBeatmap(i, k));
        }
    }
}