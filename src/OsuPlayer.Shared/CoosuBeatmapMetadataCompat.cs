using System;
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
    public readonly struct MapIdentity : IMapIdentifiable, IEquatable<MapIdentity>
    {
        private static readonly MapIdentity s_default;

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

        public static ref readonly MapIdentity Default => ref s_default;

        public MapIdentity GetIdentity()
        {
            return this;
        }

        public override bool Equals(object obj)
        {
            if (obj is not MapIdentity mapIdentity)
            {
                return false;
            }

            if (mapIdentity.FolderName == FolderName)
            {
                return mapIdentity.Version == Version;
            }

            return false;
        }

        public bool Equals(MapIdentity other)
        {
            return FolderName == other.FolderName && Version == other.Version && InOwnDb == other.InOwnDb;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FolderName, Version, InOwnDb);
        }

        public override string ToString()
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

        private string DebuggerDisplay() => ToString();
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