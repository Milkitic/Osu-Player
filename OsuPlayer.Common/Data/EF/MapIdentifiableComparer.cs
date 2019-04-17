using System.Collections.Generic;
using OSharp.Beatmap.MetaData;

namespace Milky.OsuPlayer.Common.Data.EF {
    internal class MapIdentifiableComparer : IEqualityComparer<IMapIdentifiable>
    {
        public bool Equals(IMapIdentifiable x, IMapIdentifiable y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            return x.GetIdentity().Equals(y.GetIdentity());
        }

        public int GetHashCode(IMapIdentifiable obj)
        {
            return obj.FolderName.GetHashCode() + obj.FolderName.GetHashCode();
        }
    }
}