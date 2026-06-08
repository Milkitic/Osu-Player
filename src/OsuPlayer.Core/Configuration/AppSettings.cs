using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OsuPlayer.Shared;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Core.Configuration;

public class AppSettings : IUserPreferences, IDisposable
{
    //private ThreadLocal<FileStream> FileStream { get; } = new ThreadLocal<FileStream>(() =>
    //    File.Open(Domain.ConfigFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite), true);

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public AppSettings()
    {
        if (Default != null)
        {
            return;
        }

        Default = this;

        Volume.PropertyChanged += (s, e) => {
            if (e.PropertyName == nameof(VolumeSection.Main)) OnPropertyChanged(nameof(VolumeMain));
            else if (e.PropertyName == nameof(VolumeSection.Music)) OnPropertyChanged(nameof(VolumeMusic));
            else if (e.PropertyName == nameof(VolumeSection.Hitsound)) OnPropertyChanged(nameof(VolumeHitsound));
            else if (e.PropertyName == nameof(VolumeSection.Sample)) OnPropertyChanged(nameof(VolumeSample));
            else if (e.PropertyName == nameof(VolumeSection.BalanceFactor)) OnPropertyChanged(nameof(VolumeBalanceFactor));
        };

        Play.PropertyChanged += (s, e) => {
            if (e.PropertyName == nameof(PlaySection.PlaybackRate)) OnPropertyChanged(nameof(PlaybackRate));
            else if (e.PropertyName == nameof(PlaySection.PlayUseTempo)) OnPropertyChanged(nameof(PlayUseTempo));
            else if (e.PropertyName == nameof(PlaySection.DeviceDescription)) OnPropertyChanged(nameof(PlayDeviceDescription));
            else if (e.PropertyName == nameof(PlaySection.PlayListMode)) OnPropertyChanged(nameof(PlayListMode));
        };
    }

    public VolumeSection Volume { get; set; } = new VolumeSection();
    public GeneralSection General { get; set; } = new GeneralSection();
    public InterfaceSection Interface { get; set; } = new InterfaceSection();
    public PlaySection Play { get; set; } = new PlaySection();
    [JsonPropertyName("hot_keys")]
    public List<HotKey> HotKeys { get; set; } = new List<HotKey>();
    public LyricSection Lyric { get; set; } = new LyricSection();
    public ExportSection Export { get; set; } = new ExportSection();
    public HashSet<MapIdentity> CurrentList { get; set; } = new HashSet<MapIdentity>();
    public MapIdentity? CurrentMap { get; set; }
    public DateTime? LastUpdateCheck { get; set; } = null;
    public string IgnoredVer { get; set; } = null;


    public DateTime LastTimeScanOsuDb { get; set; }

    [JsonIgnore]
    public float VolumeMain
    {
        get => Volume.Main;
        set { if (Volume.Main != value) { Volume.Main = value; OnPropertyChanged(); } }
    }
    [JsonIgnore]
    public float VolumeMusic
    {
        get => Volume.Music;
        set { if (Volume.Music != value) { Volume.Music = value; OnPropertyChanged(); } }
    }
    [JsonIgnore]
    public float VolumeHitsound
    {
        get => Volume.Hitsound;
        set { if (Volume.Hitsound != value) { Volume.Hitsound = value; OnPropertyChanged(); } }
    }
    [JsonIgnore]
    public float VolumeSample
    {
        get => Volume.Sample;
        set { if (Volume.Sample != value) { Volume.Sample = value; OnPropertyChanged(); } }
    }
    [JsonIgnore]
    public float VolumeBalanceFactor
    {
        get => Volume.BalanceFactor;
        set { if (Volume.BalanceFactor != value) { Volume.BalanceFactor = value; OnPropertyChanged(); } }
    }

    [JsonIgnore]
    public float PlaybackRate
    {
        get => Play.PlaybackRate;
        set { if (Play.PlaybackRate != value) { Play.PlaybackRate = value; OnPropertyChanged(); } }
    }
    [JsonIgnore]
    public bool PlayUseTempo
    {
        get => Play.PlayUseTempo;
        set { if (Play.PlayUseTempo != value) { Play.PlayUseTempo = value; OnPropertyChanged(); } }
    }
    [JsonIgnore]
    public AudioDeviceDescription PlayDeviceDescription
    {
        get => Play.DeviceDescription;
        set { if (!Equals(Play.DeviceDescription, value)) { Play.DeviceDescription = value; OnPropertyChanged(); } }
    }
    [JsonIgnore]
    public int PlayGeneralActualOffset
    {
        get => Play.GeneralActualOffset;
        set { /* Read-only property in PlaySection */ }
    }
    [JsonIgnore]
    public PlaylistMode PlayListMode
    {
        get => Play.PlayListMode;
        set { if (Play.PlayListMode != value) { Play.PlayListMode = value; OnPropertyChanged(); } }
    }

    public void Save()
    {
        lock (FileSaveLock)
        {
            //FileStream.Value.SetLength(0);
            var content = JsonSerializer.Serialize(this, JsonOptions);
            //byte[] buffer = Encoding.GetBytes(content);
            //FileStream.Value.Write(buffer, 0, buffer.Length);
            File.WriteAllText(Domain.ConfigFile, content);
        }
    }

    public void Dispose()
    {
        //foreach (var fs in FileStream.Values) fs?.Dispose();
        //FileStream?.Dispose();
    }

    private static readonly Encoding Encoding = Encoding.UTF8;
    private static readonly object FileSaveLock = new object();
    public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    public static AppSettings Default { get; private set; }

    public static void SaveDefault()
    {
        Default?.Save();
    }

    public static void Load(AppSettings config)
    {
        Default = config ?? new AppSettings();
        //Default.FileStream = File.Open(Domain.ConfigFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
    }

    private static void LoadNew()
    {
        File.WriteAllText(Domain.ConfigFile, "");
        Load(new AppSettings());
    }

    public static void CreateNewConfig()
    {
        LoadNew();
        SaveDefault();
    }
}
