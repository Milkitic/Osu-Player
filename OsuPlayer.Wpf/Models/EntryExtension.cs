using osu_database_reader.Components.Beatmaps;
using OSharp.Beatmap.MetaData;

namespace Milky.OsuPlayer.Models
{
    public static class EntryExtension
    {
        public static MapIdentity GetIdentity(this BeatmapEntry entry) => entry != null ?
            new MapIdentity(entry.FolderName, entry.Version) : default;
        public static MapIdentity GetIdentity(this IMapIdentifiable entry) =>
            entry != null
                ? new MapIdentity(entry.FolderName, entry.Version)
                : default;
    }
}
