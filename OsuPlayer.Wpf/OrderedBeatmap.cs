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

    public class OrderedBeatmapGroup : OrderedModel<BeatmapGroup>
    {
        public OrderedBeatmapGroup()
        {
        }

        public OrderedBeatmapGroup(int i, BeatmapGroup group) : base(i, group)
        {
        }
    }

    public static class OrderedModelExtension
    {
        public static IEnumerable<OrderedBeatmap> AsOrderedBeatmap(this IEnumerable<Beatmap> orderedModel)
        {
            return orderedModel.Select((k, i) => new OrderedBeatmap(i, k));
        }

        public static IEnumerable<OrderedBeatmapGroup> AsOrderedBeatmapGroup(this IEnumerable<BeatmapGroup> orderedModel)
        {
            return orderedModel.Select((k, i) => new OrderedBeatmapGroup(i, k));
        }
    }
}