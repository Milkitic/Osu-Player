using Milky.OsuPlayer.Common.Data.EF.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;

namespace Milky.OsuPlayer.Common.Data
{
    class DbJsonObject
    {
        public List<Collection> Collections { get; set; }
        public List<CollectionRelation> CollectionRelations { get; set; }
        public List<MapInfo> MapInfos { get; set; }
    }
}
