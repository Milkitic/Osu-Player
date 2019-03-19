using Milky.OsuPlayer.Common.Data.EF;
using Milky.OsuPlayer.Common.Data.EF.Model;
using osu.Shared.Serialization;
using osu_database_reader.BinaryFiles;
using osu_database_reader.Components.Beatmaps;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Milky.OsuPlayer.Common.Data;

namespace Milky.OsuPlayer.Common.Instances
{
    public class OsuDbInst
    {

        public async Task<bool> TrySyncOsuDbAsync(string path, bool addOnly)
        {
            try
            {
                await SyncOsuDbAsync(path, addOnly);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task SyncOsuDbAsync(string path, bool addOnly)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                var db = await ReadDbAsync(path);

                await BeatmapDbContext.SyncMapsFromHoLLyAsync(db.Beatmaps, addOnly);
            }

            Beatmaps = new HashSet<Beatmap>(BeatmapDatabaseQuery.GetWholeListFromDb());
        }

        private static async Task<OsuDb> ReadDbAsync(string path)
        {
            return await Task.Run(() =>
            {
                var db = new OsuDb();
                using (var fs = new FileStream(path, FileMode.Open))
                {
                    db.ReadFromStream(new SerializationReader(fs));
                }

                return db;
            });
        }

        public HashSet<Beatmap> Beatmaps { get; set; }
    }
}