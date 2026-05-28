using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Coosu.Database;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Services;

namespace Milky.OsuPlayer.Common.Instances
{
    public class OsuDbInst
    {
        private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly Lock _scanningObject = new Lock();
        private readonly IPlayerDataStore _playerData;

        public OsuDbInst()
            : this(new PlayerDataService())
        {
        }

        public OsuDbInst(IPlayerDataStore playerData)
        {
            _playerData = playerData;
        }

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
                s_logger.Error(ex, "Error while syncing osu db."); // todo: update db file.
                return false;
            }
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
                var beatmaps = await ReadDbAsync(path);
                await _playerData.SyncMapsFromOsuDbAsync(beatmaps, addOnly);
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
                    return reader.EnumerateBeatmapsCustom().ToArray();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Read osu!db failed. This file may be corrupted.", ex);
                }
            });
        }

        //public HashSet<Beatmap> Beatmaps { get; set; }
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
    }
}