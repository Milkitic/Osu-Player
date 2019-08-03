using Milky.OsuPlayer.Common.Data.EF;
using Milky.OsuPlayer.Common.Data.EF.Model;
using osu.Shared.Serialization;
using osu_database_reader.BinaryFiles;
using osu_database_reader.Components.Beatmaps;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Milky.OsuPlayer.Common.Data;
using Milky.WpfApi;

namespace Milky.OsuPlayer.Common.Instances
{
    public class OsuDbInst
    {
        private readonly object _scanningObject = new object();
        private BeatmapDbOperator _beatmapDbOperator = new BeatmapDbOperator();

        public class ViewModelClass : ViewModelBase
        {
            private bool _isScanning;

            public bool IsScanning
            {
                get => _isScanning;
                set
                {
                    _isScanning = value;
                    OnPropertyChanged();
                }
            }
        }

        public ViewModelClass ViewModel { get; set; } = new ViewModelClass();

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
        public async Task LoadLocalDbAsync()
        {
            Beatmaps = new HashSet<Beatmap>(await BeatmapDatabaseQuery.GetWholeListFromDbAsync());
        }

        public async Task SyncOsuDbAsync(string path, bool addOnly)
        {
            lock (_scanningObject)
            {
                if (ViewModel.IsScanning)
                    return;

                ViewModel.IsScanning = true;
            }

            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                var db = await ReadDbAsync(path);
                await _beatmapDbOperator.SyncMapsFromHoLLyAsync(db.Beatmaps, addOnly);
            }

            lock (_scanningObject)
                ViewModel.IsScanning = false;
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