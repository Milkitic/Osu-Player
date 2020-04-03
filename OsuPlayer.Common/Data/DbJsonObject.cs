using System.Collections.Generic;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;

namespace Milky.OsuPlayer.Common.Data
{
    class DbJsonObject
    {
        public List<Collection> Collections { get; set; }
        public List<CollectionRelation> CollectionRelations { get; set; }
        public List<BeatmapSettings> MapInfos { get; set; }
    }
}
