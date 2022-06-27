using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Coosu.Database;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Presentation.Interaction;

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

        //public async Task LoadLocalDbAsync()
        //{
        //    await Task.Run(() => Beatmaps = new HashSet<Beatmap>(_beatmapDbOperator.GetAllBeatmaps()));
        //}

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
                var beatmaps = await ReadDbAsync(path);
                await _beatmapDbOperator.SyncMapsFromOsuDbAsync(beatmaps, addOnly);
            }

            lock (_scanningObject)
                ViewModel.IsScanning = false;
        }

        private static async Task<IReadOnlyList<Beatmap>> ReadDbAsync(string path)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var reader = new OsuDbReader(path);
                    return reader.EnumerateBeatmapCustom().ToArray();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Read osu!db failed. This file may be corrupted.", ex);
                }
            });
        }

        //public HashSet<Beatmap> Beatmaps { get; set; }
    }
}