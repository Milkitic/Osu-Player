using Milkitic.OsuPlayer.Data;
using osu_database_reader.Components.Beatmaps;

namespace Milkitic.OsuPlayer
{
    public static class EntryExtension
    {
        public static MapIdentity GetIdentity(this BeatmapEntry entry) => entry != null ?
            new MapIdentity(entry.FolderName, entry.Version) : default;
        public static MapIdentity GetIdentity(this BeatmapViewModel viewModel) =>
            new MapIdentity(viewModel.FolderName, viewModel.Version);
        public static MapIdentity GetIdentity(this MapInfo viewModel) =>
            new MapIdentity(viewModel.FolderName, viewModel.Version);
    }
}
