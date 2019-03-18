using Milky.OsuPlayer.Common.Data.EF;
using Milky.OsuPlayer.Common.Data.EF.Model;
using osu.Shared.Serialization;
using osu_database_reader.BinaryFiles;
using osu_database_reader.Components.Beatmaps;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Common.Instances
{
    public class OsuDbInst
    {
        public async Task<bool> TryLoadNewDbAsync(string path)
        {
            try
            {
                await LoadNewDbAsync(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task LoadNewDbAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;
            var db = await ReadDbAsync(path);

            BeatmapDbContext.SyncMapsFromOsuDb(db.Beatmaps);
            //BeatmapDb = db;
        }

        private static async Task<OsuDb> ReadDbAsync(string path)
        {
            return await Task.Run(() =>
            {
                var db = new OsuDb();
                using (FileStream fs = new FileStream(path, FileMode.Open))
                {
                    db.ReadFromStream(new SerializationReader(fs));
                }

                return db;
            });
        }
    }
}