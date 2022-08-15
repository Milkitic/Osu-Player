using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
        var hitsoundNodes = await osuDir.GetHitsoundNodesAsync(_osuFile);

        await InitializeMusicTrack();
        await InitializeHitsoundTrack(hitsoundNodes);

        var folder = "res://OsuPlayer.Audio/Milki.OsuPlayer.Audio.resources.default";
        _nightcoreTilingProvider = new NightcoreTilingProvider(folder, _osuFile, _musicTrack.Duration);
        await InitializeSampleTrack(hitsoundNodes);

        if (CachedSoundFactory.GetCount(SkinSoundFlag) == 0)
        {
            var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            await Task.Run(() =>
            {
                resources
                    .Where(k => k.EndsWith(".ogg", StringComparison.Ordinal))
                    .AsParallel()
                    .ForAll(resource =>
                    {
                        var path = $"res://OsuPlayer.Audio/{resource}";
                        var result = CachedSoundFactory.GetOrCreateCacheSound(Engine.FileWaveFormat, path,
                            SkinSoundFlag, false).Result;
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
        if (previousStatus is TimerStatus.Start or TimerStatus.Restart &&
            currentStatus is TimerStatus.Stop or TimerStatus.Reset)
        {
            PauseMusic();
        }
        else if (previousStatus is TimerStatus.Stop or TimerStatus.Reset &&
                 currentStatus is TimerStatus.Start or TimerStatus.Restart)
        {
            PlayMusic();
        }
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

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}