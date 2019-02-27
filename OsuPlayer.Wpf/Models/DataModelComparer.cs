using System.Collections.Generic;

namespace Milky.OsuPlayer.Models
{
    public class DataModelComparer : IEqualityComparer<BeatmapDataModel>
    {
        private readonly bool _multiVersions;

        public DataModelComparer(bool multiVersions)
        {
            _multiVersions = multiVersions;
        }

        public bool Equals(BeatmapDataModel x, BeatmapDataModel y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            if (x.AutoArtist != y.AutoArtist) return false;
            if (x.AutoTitleSource != y.AutoTitleSource) return false;
            if (x.Creator != y.Creator) return false;
            if (_multiVersions)
            {
                if (x.Version != y.Version) return false;
            }

            return true;
        }

        public int GetHashCode(BeatmapDataModel obj)
        {
            return obj.FolderName.GetHashCode();
        }

        //public int GetHashCode(BeatmapViewModel obj)
        //{
        //    return obj.GetHashCode();
        //}
    }
}