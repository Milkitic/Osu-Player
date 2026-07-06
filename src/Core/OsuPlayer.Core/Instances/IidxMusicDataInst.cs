using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Coosu.Beatmap.Sections.GamePlay;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using OsuPlayer.Core.Services;
using OsuPlayer.Data.Models;
using OsuPlayer.Iidx.Abstractions;
using OsuPlayer.Shared;

namespace OsuPlayer.Core.Instances;

public sealed partial class IidxMusicDataInst
{
    private readonly ILogger<IidxMusicDataInst> _logger;
    private readonly Lock _scanningObject = new();
    private readonly IPlayerDataStore _playerData;

    public IidxMusicDataInst(IPlayerDataStore playerData, ILogger<IidxMusicDataInst> logger)
    {
        _playerData = playerData;
        _logger = logger;
    }

    public ViewModelClass ViewModel { get; } = new();

    public async Task<bool> TrySyncMusicDataAsync(string path, bool addOnly)
    {
        try
        {
            await SyncMusicDataAsync(path, addOnly);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while syncing IIDX music_data.bin.");
            return false;
        }
    }

    public async Task SyncMusicDataAsync(string path, bool addOnly)
    {
        lock (_scanningObject)
        {
            if (ViewModel.IsScanning)
            {
                return;
            }

            ViewModel.IsScanning = true;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            var entries = await ReadMusicDataAsync(path);
            var lastModified = File.GetLastWriteTime(path);
            var beatmaps = IidxBeatmapFactory.CreateBeatmaps(entries, lastModified);
            await _playerData.SyncMapsFromIidxMusicDataAsync(beatmaps, addOnly);
        }
        finally
        {
            lock (_scanningObject)
            {
                ViewModel.IsScanning = false;
            }
        }
    }

    public static async Task<IReadOnlyList<IidxMusicEntry>> ReadMusicDataAsync(string path)
    {
        return await Task.Run(() =>
        {
            using var reader = new IidxMusicDataReader(path);
            reader.ReadToEnd();
            return reader.Entries.ToArray();
        });
    }

    public partial class ViewModelClass : ObservableObject
    {
        [ObservableProperty]
        public partial bool IsScanning { get; set; }
    }
}

public static class IidxBeatmapFactory
{
    public static IReadOnlyList<Beatmap> CreateBeatmaps(IEnumerable<IidxMusicEntry> entries, DateTime lastModified)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var beatmaps = new List<Beatmap>();
        foreach (var entry in entries)
        {
            for (var i = 0; i < IidxDifficultyLabels.AllLabels.Count; i++)
            {
                if (!HasDifficulty(entry, i))
                {
                    continue;
                }

                beatmaps.Add(CreateBeatmap(entry, (IidxDifficulty)i, lastModified));
            }
        }

        return beatmaps;
    }

    private static Beatmap CreateBeatmap(IidxMusicEntry entry, IidxDifficulty difficulty, DateTime lastModified)
    {
        var difficultyIndex = (int)difficulty;
        var label = IidxDifficultyLabels.ShortLabel(difficulty);
        var folderName = $"iidx-{entry.MusicId:D5}";
        var fileIdentifier = entry.FileIdentifiers[difficultyIndex];
        var title = string.IsNullOrWhiteSpace(entry.TitleRoman) ? entry.Title : entry.TitleRoman;

        return new Beatmap
        {
            Id = Guid.NewGuid(),
            SourceGame = SourceGame.Iidx,
            Artist = entry.Artist,
            ArtistUnicode = entry.Artist,
            Title = string.IsNullOrWhiteSpace(title) ? entry.Title : title,
            TitleUnicode = entry.Title,
            Creator = "beatmania IIDX",
            Version = label,
            AudioFileName = $"{entry.MusicId:D5}_{fileIdentifier:D2}.2dx",
            BeatmapFileName = $"{folderName}-{label}.iidx",
            LastModifiedTime = lastModified,
            DiffSrNoneMania = entry.DifficultyLevels[difficultyIndex],
            DrainTimeSeconds = 0,
            TotalTime = 0,
            AudioPreviewTime = 0,
            BeatmapId = entry.MusicId * 100 + difficultyIndex,
            BeatmapSetId = entry.MusicId,
            GameMode = GameMode.Mania,
            SongSource = entry.Genre,
            SongTags = BuildTags(entry, label),
            FolderName = folderName,
            InOwnDb = false,
            IidxMusicId = entry.MusicId,
            IidxFileIdentifier = fileIdentifier,
            IidxBgmVolume = entry.BgmVolume,
            IidxBgaDelay = entry.BgaDelay,
            IidxVersion = entry.Version
        };
    }

    private static bool HasDifficulty(IidxMusicEntry entry, int index)
    {
        return entry.DifficultyLevels.ElementAtOrDefault(index) > 0 ||
               entry.NoteCounts.ElementAtOrDefault(index) > 0 ||
               entry.FileIdentifiers.ElementAtOrDefault(index) > 0;
    }

    private static string BuildTags(IidxMusicEntry entry, string label)
    {
        var tags = new List<string> { "iidx", label };
        if (!string.IsNullOrWhiteSpace(entry.TitleRoman))
        {
            tags.Add(entry.TitleRoman);
        }

        if (!string.IsNullOrWhiteSpace(entry.License))
        {
            tags.Add(entry.License);
        }

        return string.Join(' ', tags);
    }
}
