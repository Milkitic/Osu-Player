using System.Diagnostics;
using System.IO;

namespace Coosu.Beatmap.MetaData
{
    public interface IMapIdentifiable
    {
        string FolderName { get; }
        string Version { get; }
        bool InOwnDb { get; }

        MapIdentity GetIdentity();
    }

    [DebuggerDisplay("{DebuggerDisplay()}")]
    public readonly struct MapIdentity : IMapIdentifiable
    {
        private static readonly MapIdentity _default;

        public MapIdentity(string folderName, string version, bool inOwnDb)
        {
            this = default;
            FolderName = folderName;
            Version = version;
            InOwnDb = inOwnDb;
        }

        public string FolderName { get; }
        public string Version { get; }
        public bool InOwnDb { get; }

        public static ref readonly MapIdentity Default => ref _default;

        public MapIdentity GetIdentity()
        {
            return this;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is MapIdentity mapIdentity))
            {
                return false;
            }

            if (mapIdentity.FolderName == FolderName)
            {
                return mapIdentity.Version == Version;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private string DebuggerDisplay()
        {
            if (this.IsMapTemporary())
            {
                return "temp: \"" + FolderName + "\"";
            }

            if (InOwnDb)
            {
                return "own: [\"" + FolderName + "\",\"" + Version + "\"]";
            }

            return "osu: [\"" + FolderName + "\",\"" + Version + "\"]";
        }
    }

    public static class MapIdentifiableExtension
    {
        public static bool IsMapTemporary(this IMapIdentifiable map)
        {
            return Path.IsPathRooted(map.FolderName);
        }

        public static bool IsMapTemporary(this MapIdentity? map)
        {
            return map.HasValue && Path.IsPathRooted(map.Value.FolderName);
        }
    }
}
