using OSharp.Beatmap.MetaData;

namespace Milky.OsuPlayer.Common
{
    public struct MapIdentity : IMapIdentifiable
    {
        public MapIdentity(string folderName, string version, bool inOwnDb) : this()
        {
            FolderName = folderName;
            Version = version;
            InOwnDb = inOwnDb;
        }

        public string FolderName { get; }
        public string Version { get; }
        public bool InOwnDb { get; }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is MapIdentity mi))
            {
                return false;
            }

            return mi.FolderName == FolderName && mi.Version == Version;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
