using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Coosu.Database;
using OsuPlayer.Core.Services;
using OsuPlayer.Data.Models;

using Microsoft.Extensions.Logging;

namespace OsuPlayer.Core.Instances;

public partial class OsuDbInst
{
    private readonly ILogger<OsuDbInst> _logger;
    private readonly Lock _scanningObject = new Lock();
    private readonly IPlayerDataStore _playerData;

    public OsuDbInst(IPlayerDataStore playerData, ILogger<OsuDbInst> logger)
    {
        _playerData = playerData;
        _logger = logger;
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
            _logger.LogError(ex, "Error while syncing osu db."); // todo: update db file.
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
    public partial class ViewModelClass : ObservableObject
    {
        [ObservableProperty]
        public partial bool IsScanning { get; set; }
    }
}
