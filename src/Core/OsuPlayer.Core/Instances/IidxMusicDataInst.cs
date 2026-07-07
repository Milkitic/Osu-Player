using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Coosu.Beatmap.Sections.GamePlay;
using IIDXToolbox.Readers;
using IIDXToolbox.Readers.Structures;
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

    /// <summary>
    /// Reads the IIDX <c>music_data.bin</c> via the BemaniUtils
    /// <see cref="MusicDataReader"/> and returns the raw on-disk entries.
    /// </summary>
    public static async Task<IReadOnlyList<MusicDbEntry32>> ReadMusicDataAsync(string path)
    {
        return await Task.Run(() =>
        {
            using var reader = new MusicDataReader(path);
            reader.ReadToEnd();
            var result = new List<MusicDbEntry32>();
            foreach (ref readonly var entry in reader.EnumerateMusicData())
            {
                result.Add(entry);
            }

            return result;
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
    private static readonly byte[] s_difficultyLevels =
    [
        0, 0, 0, 0, 0,
        0, 0, 0, 0, 0
    ];

    public static IReadOnlyList<Beatmap> CreateBeatmaps(IEnumerable<MusicDbEntry32> entries, DateTime lastModified)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var beatmaps = new List<Beatmap>();
        foreach (var entry in entries)
        {
            var levels = GetDifficultyLevels(in entry);
            var notes = GetNoteCounts(in entry);
            var files = GetFileIdentifiers(in entry);

            for (var i = 0; i < IidxDifficultyLabels.AllLabels.Count; i++)
            {
                if (!HasDifficulty(levels, notes, files, i))
                {
                    continue;
                }

                beatmaps.Add(CreateBeatmap(in entry, levels, notes, files, (IidxDifficulty)i, lastModified));
            }
        }

        return beatmaps;
    }

    private static Beatmap CreateBeatmap(
        in MusicDbEntry32 entry,
        byte[] levels,
        int[] notes,
        byte[] files,
        IidxDifficulty difficulty,
        DateTime lastModified)
    {
        var difficultyIndex = (int)difficulty;
        var label = IidxDifficultyLabels.ShortLabel(difficulty);
        var folderName = $"iidx-{entry.musicId:D5}";
        var fileIdentifier = files[difficultyIndex];
        var title = string.IsNullOrWhiteSpace(entry.TitleRoman) ? entry.Title : entry.TitleRoman;

        return new Beatmap
        {
            Id = Guid.NewGuid(),
            SourceGame = SourceGame.Iidx,
            Artist = entry.Artist ?? string.Empty,
            ArtistUnicode = entry.Artist ?? string.Empty,
            Title = string.IsNullOrWhiteSpace(title) ? (entry.Title ?? string.Empty) : title,
            TitleUnicode = entry.Title ?? string.Empty,
            Creator = "beatmania IIDX",
            Version = label,
            AudioFileName = $"{entry.musicId:D5}_{fileIdentifier:D2}.2dx",
            BeatmapFileName = $"{folderName}-{label}.iidx",
            LastModifiedTime = lastModified,
            DiffSrNoneMania = levels[difficultyIndex],
            DrainTimeSeconds = 0,
            TotalTime = 0,
            AudioPreviewTime = 0,
            BeatmapId = entry.musicId * 100 + difficultyIndex,
            BeatmapSetId = entry.musicId,
            GameMode = GameMode.Mania,
            SongSource = entry.Genre ?? string.Empty,
            SongTags = BuildTags(in entry, label),
            FolderName = folderName,
            InOwnDb = false,
            IidxMusicId = entry.musicId,
            IidxFileIdentifier = fileIdentifier,
            IidxBgmVolume = entry.bgmVolume,
            IidxBgaDelay = entry.BgaDelay,
            IidxVersion = entry.Version
        };
    }

    private static byte[] GetDifficultyLevels(in MusicDbEntry32 entry) =>
    [
        entry.LvSPB, entry.LvSPN, entry.LvSPH, entry.LvSPA, entry.LvSPL,
        entry.LvDPB, entry.LvDPN, entry.LvDPH, entry.LvDPA, entry.LvDPL
    ];

    private static int[] GetNoteCounts(in MusicDbEntry32 entry) =>
    [
        entry.NotesCountSPB, entry.NotesCountSPN, entry.NotesCountSPH,
        entry.NotesCountSPA, entry.NotesCountSPL,
        entry.NotesCountDPB, entry.NotesCountDPN, entry.NotesCountDPH,
        entry.NotesCountDPA, entry.NotesCountDPL
    ];

    private static byte[] GetFileIdentifiers(in MusicDbEntry32 entry) =>
    [
        entry.FileIdentifierSPB, entry.FileIdentifierSPN, entry.FileIdentifierSPH,
        entry.FileIdentifierSPA, entry.FileIdentifierSPL,
        entry.FileIdentifierDPB, entry.FileIdentifierDPN, entry.FileIdentifierDPH,
        entry.FileIdentifierDPA, entry.FileIdentifierDPL
    ];

    private static bool HasDifficulty(byte[] levels, int[] notes, byte[] files, int index)
    {
        return levels.ElementAtOrDefault(index) > 0 ||
               notes.ElementAtOrDefault(index) > 0 ||
               files.ElementAtOrDefault(index) > 0;
    }

    private static string BuildTags(in MusicDbEntry32 entry, string label)
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
