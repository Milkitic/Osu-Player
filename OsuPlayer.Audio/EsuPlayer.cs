using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Anotar.NLog;
using Coosu.Beatmap;
using Coosu.Beatmap.Extensions.Playback;
using JetBrains.Annotations;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using Milki.OsuPlayer.Audio.Mixing;
using NAudio.Wave;

namespace Milki.OsuPlayer.Audio;

public class EsuPlayer : TrackPlayer, INotifyPropertyChanged
{
    public const string SkinSoundFlag = "UserSkin";

    private bool _isMusicTrackAdded;

    private readonly string _folder;
    private readonly string _fileName;
    private readonly LocalOsuFile _osuFile;

    private readonly SoundSeekingTrack _musicTrack;
    private readonly EsuMixingTrack _hitsoundTrack;
    private readonly EsuMixingTrack _sampleTrack;

    private readonly EnhancedVolumeSampleProvider _musicVsp;
    private readonly EnhancedVolumeSampleProvider _hitsoundVsp;
    private readonly EnhancedVolumeSampleProvider _sampleVsp;

    private NightcoreTilingProvider? _nightcoreTilingProvider;
    private PlayModifier _playModifier;
    private List<HitsoundNode>? _hitsoundNodes;
    private int _nextCachingTime;

    public EsuPlayer(LocalOsuFile osuFile, AudioPlaybackEngine engine) : base(engine)
    {
        _osuFile = osuFile;
        _folder = Path.GetDirectoryName(osuFile.OriginalPath)!;
        _fileName = Path.GetFileName(osuFile.OriginalPath)!;

        var waveFormat = engine.WaveFormat;
        _musicTrack = new SoundSeekingTrack(TimerSource, waveFormat);
        _hitsoundTrack = new EsuMixingTrack(TimerSource, waveFormat);
        _sampleTrack = new EsuMixingTrack(TimerSource, waveFormat);

        _musicVsp = new EnhancedVolumeSampleProvider(null!) { Volume = 1f };
        _hitsoundVsp = new EnhancedVolumeSampleProvider(null!) { Volume = 1f };
        _sampleVsp = new EnhancedVolumeSampleProvider(null!) { Volume = 1f };
    }

    public TimeSpan PlayTime => TimerSource.Elapsed;
    public TimeSpan TotalTime => TimeSpan.FromMilliseconds(Duration);

    public float Volume
    {
        get => Engine.Volume;
        set
        {
            if (Equals(value, Engine.Volume)) return;
            Engine.Volume = value;
            OnPropertyChanged();
        }
    }

    public float MusicVolume
    {
        get => _musicVsp.Volume;
        set
        {
            if (Equals(value, _musicVsp.Volume)) return;
            _musicVsp.Volume = value;
            OnPropertyChanged();
        }
    }

    public float HitsoundVolume
    {
        get => _hitsoundVsp.Volume;
        set
        {
            if (Equals(value, _hitsoundVsp.Volume)) return;
            _hitsoundVsp.Volume = value;
            OnPropertyChanged();
        }
    }

    public float HitsoundBalanceRatio
    {
        get => _hitsoundTrack.BalanceRatio;
        set
        {
            if (Equals(value, _hitsoundTrack.BalanceRatio)) return;
            _hitsoundTrack.BalanceRatio = value;
            OnPropertyChanged();
        }
    }

    public float SampleVolume
    {
        get => _sampleVsp.Volume;
        set
        {
            if (Equals(value, _sampleVsp.Volume)) return;
            _sampleVsp.Volume = value;
            OnPropertyChanged();
        }
    }

    public double Offset
    {
        get => _hitsoundTrack.Offset;
        set
        {
            if (Equals(value, _hitsoundTrack.Offset)) return;
            _hitsoundTrack.Offset = value;
            _sampleTrack.Offset = value;
            OnPropertyChanged();
        }
    }

    public PlayModifier PlayModifier
    {
        get => _playModifier;
        set
        {
            if (value == _playModifier) return;
            _playModifier = value;

            switch (value)
            {
                case PlayModifier.None:
                    SetRate(1, false);
                    break;
                case PlayModifier.DoubleTime:
                    SetRate(1.5f, true);
                    break;
                case PlayModifier.NightCore:
                    SetRate(1.5f, false);
                    break;
                case PlayModifier.HalfTime:
                    SetRate(0.75f, true);
                    break;
                case PlayModifier.DayCore:
                    SetRate(0.75f, false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }

            OnPropertyChanged();
        }
    }

    public override async ValueTask InitializeAsync()
    {
        var osuDir = new OsuDirectory(_folder);
        await osuDir.InitializeAsync(_fileName);
        _hitsoundNodes = await osuDir.GetHitsoundNodesAsync(_osuFile);

        AddAudioCacheInBackground(0, 13000, _hitsoundNodes);
        _nextCachingTime = 10000;

        await InitializeMusicTrack();
        await InitializeHitsoundTrack(_hitsoundNodes);

        var folder = "res://OsuPlayer.Audio/Milki.OsuPlayer.Audio.resources.default";
        _nightcoreTilingProvider = new NightcoreTilingProvider(folder, _osuFile, _musicTrack.Duration);
        await InitializeSampleTrack(_hitsoundNodes);

        if (CachedSoundFactory.GetCount(SkinSoundFlag) == 0)
        {
            var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            await Task.Run(() =>
            {
                resources
                    .Where(k => k.EndsWith(".ogg", StringComparison.Ordinal))
                    .AsParallel()
                    .WithDegreeOfParallelism(1)
                    .ForAll(resource =>
                    {
                        var path = $"res://OsuPlayer.Audio/{resource}";
                        var result = CachedSoundFactory.GetOrCreateCacheSound(Engine.FileWaveFormat, path,
                            SkinSoundFlag, false, useWdlResampler: true).Result;
                        Debug.Assert(result != null);
                    });
            });
        }

        _musicVsp.Source = _musicTrack.RootSampleProvider;
        _hitsoundVsp.Source = _hitsoundTrack.RootSampleProvider;
        _sampleVsp.Source = _sampleTrack.RootSampleProvider;

        Engine.RootMixer.AddMixerInput(_musicVsp);
        Engine.RootMixer.AddMixerInput(_hitsoundVsp);
        Engine.RootMixer.AddMixerInput(_sampleVsp);

        Tracks.Add(_musicTrack);
        Tracks.Add(_hitsoundTrack);
        Tracks.Add(_sampleTrack);

        Duration = Tracks.Max(k => k.Duration);
        PlayerStatus = PlayerStatus.Ready;
    }

    private async ValueTask InitializeMusicTrack()
    {
        var audioFile = Path.Combine(_folder, _osuFile.General?.AudioFilename ?? "audio.mp3");

        _musicTrack.Path = audioFile;
        await _musicTrack.InitializeAsync();
    }

    private async ValueTask InitializeHitsoundTrack(List<HitsoundNode> hitsoundNodes)
    {
        _hitsoundTrack.HitsoundNodes = hitsoundNodes.Where(k => k is not PlayableNode
        {
            PlayablePriority: PlayablePriority.Sampling
        }).ToList();
        await _hitsoundTrack.InitializeAsync();
    }

    private async ValueTask InitializeSampleTrack(List<HitsoundNode> hitsoundNodes)
    {
        _sampleTrack.HitsoundNodes = hitsoundNodes.Where(k => k is PlayableNode
        {
            PlayablePriority: PlayablePriority.Sampling
        }).ToList();
        await _sampleTrack.InitializeAsync();
    }

    public override void OnStatusChanged(TimerStatus previousStatus, TimerStatus currentStatus)
    {
        //Console.WriteLine(previousStatus + "->" + currentStatus);
        if (previousStatus is TimerStatus.Start or TimerStatus.Restart or TimerStatus.Skip &&
            currentStatus is TimerStatus.Stop or TimerStatus.Reset)
        {
            PauseMusic();
        }
        else if (previousStatus is TimerStatus.Stop or TimerStatus.Reset or TimerStatus.Skip &&
                 currentStatus is TimerStatus.Start or TimerStatus.Restart)
        {
            PlayMusic();
        }
    }

    public override void OnUpdated(double previous, double current)
    {
        if (current > _nextCachingTime && _hitsoundNodes != null)
        {
            AddAudioCacheInBackground(_nextCachingTime, _nextCachingTime + 13000, _hitsoundNodes);
            _nextCachingTime += 10000;
        }
    }

    protected override async ValueTask SeekCore(TimeSpan time)
    {
        if (_hitsoundNodes != null)
        {
            await AddAudioCacheAsync((int)time.TotalMilliseconds,
                (int)time.TotalMilliseconds + 13000, _hitsoundNodes);
        }

        await base.SeekCore(time);
    }

    private void PlayMusic()
    {
        if (_isMusicTrackAdded) return;
        _isMusicTrackAdded = true;
        Engine.AddMixerInput(_musicVsp);
    }

    private void PauseMusic()
    {
        _isMusicTrackAdded = false;
        Engine.RemoveMixerInput(_musicVsp);
    }

    private async void AddAudioCacheInBackground(int startTime, int endTime, IEnumerable<HitsoundNode> hitsoundNodes)
    {
        await AddAudioCacheAsync(startTime, endTime, hitsoundNodes);
    }

    private async Task AddAudioCacheAsync(int startTime, int endTime, IEnumerable<HitsoundNode> hitsoundNodes)
    {
        if (hitsoundNodes is IList { Count: 0 })
        {
            LogTo.Warn($"No hitsounds in list, stop adding cache.");
            return;
        }

        var hitsoundList = hitsoundNodes;
        var folder = _folder;
        var waveFormat = Engine.WaveFormat;
        await Task.Run(() =>
        {
            hitsoundList
                .Where(k => k.Offset >= startTime && k.Offset < endTime)
                .AsParallel()
                .WithDegreeOfParallelism(1)
                //.WithDegreeOfParallelism(Environment.ProcessorCount == 1 ? 1 : Environment.ProcessorCount / 2)
                .ForAll(playableNode => { AddHitsoundCache(playableNode, folder, waveFormat).Wait(); });
        });
    }

    private async Task AddHitsoundCache(HitsoundNode hitsoundNode,
        string beatmapFolder,
        WaveFormat waveFormat)
    {
        if (hitsoundNode.Filename == null)
        {
            if (hitsoundNode is PlayableNode)
            {
                LogTo.Warn($"Filename is null, add null cache.");
            }

            CacheManager.Instance.AddCachedSound(hitsoundNode, null);
            return;
        }

        string path;
        string? identifier = null;
        if (hitsoundNode.UseUserSkin)
        {
            identifier = "internal";
            path = $"res://OsuPlayer.Audio/Milki.OsuPlayer.Audio.resources.default.{hitsoundNode.Filename}.ogg";
        }
        else
        {
            path = Path.Combine(beatmapFolder, hitsoundNode.Filename);
        }

        var (result, status) = await CachedSoundFactory
            .GetOrCreateCacheSoundStatus(waveFormat, path, identifier, checkFileExist: false);

        if (result == null)
        {
            LogTo.Warn("Caching sound failed: " + (File.Exists(path) ? path : "FileNotFound"));
        }
        else if (status == true)
        {
            LogTo.Info("Cached sound: " + path);
        }

        CacheManager.Instance.AddCachedSound(hitsoundNode, result);
        CacheManager.Instance.AddCachedSound(Path.GetFileNameWithoutExtension(path), result);
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}