using System;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Presentation.Interaction;
using osu.Shared.Serialization;
using osu_database_reader.BinaryFiles;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Milky.OsuPlayer.Data.Models;

namespace Milky.OsuPlayer.Common.Instances
{
    public class OsuDbInst
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public class ViewModelClass : VmBase
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

        private readonly object _scanningObject = new object();
        private AppDbOperator _beatmapDbOperator = new AppDbOperator();

        public ViewModelClass ViewModel { get; set; } = new ViewModelClass();

        public async Task<bool> TrySyncOsuDbAsync(string path, bool addOnly)
        {
            try
            {
                await SyncOsuDbAsync(path, addOnly);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while syncing osu db."); // todo: update db file.
                return false;
            }
        }

        public async Task LoadLocalDbAsync()
        {
            await Task.Run(() => Beatmaps = new HashSet<Beatmap>(_beatmapDbOperator.GetAllBeatmaps()));
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