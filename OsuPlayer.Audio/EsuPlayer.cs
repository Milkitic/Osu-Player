using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Coosu.Beatmap;
using Coosu.Beatmap.Extensions.Playback;
using JetBrains.Annotations;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using Milki.OsuPlayer.Audio.Mixing;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Milki.OsuPlayer.Audio;

public class EsuPlayer : TrackPlayer, INotifyPropertyChanged
{
    public const string SkinSoundFlag = "UserSkin";

    private readonly HashSet<ISampleProvider?> _trackHashSet;

    private readonly string _folder;
    private readonly string _fileName;
    private readonly LocalOsuFile _osuFile;

    private readonly SoundSeekingTrack _musicTrack;
    private readonly EsuMixingTrack _hitsoundTrack;
    private readonly EsuMixingTrack _sampleTrack;

    private readonly MixingSampleProvider _musicMixer;
    private readonly MixingSampleProvider _hitsoundMixer;
    private readonly MixingSampleProvider _sampleMixer;
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
        _trackHashSet = new HashSet<ISampleProvider?>();

        _musicMixer = new MixingSampleProvider(waveFormat) { ReadFully = true };
        _musicVsp = new EnhancedVolumeSampleProvider(_musicMixer) { Volume = 1f };
        _hitsoundMixer = new MixingSampleProvider(waveFormat) { ReadFully = true };
        _hitsoundVsp = new EnhancedVolumeSampleProvider(_hitsoundMixer) { Volume = 1f };
        _sampleMixer = new MixingSampleProvider(waveFormat) { ReadFully = true };
        _sampleVsp = new EnhancedVolumeSampleProvider(_sampleMixer) { Volume = 1f };

        Engine.RootMixer.AddMixerInput(_musicVsp);
        Engine.RootMixer.AddMixerInput(_hitsoundVsp);
        Engine.RootMixer.AddMixerInput(_sampleVsp);

        Tracks.Add(_musicTrack);
        Tracks.Add(_hitsoundTrack);
        Tracks.Add(_sampleTrack);
    }

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
        _nightcoreTilingProvider =
            new NightcoreTilingProvider(null, _osuFile, _musicTrack.Duration);
        await InitializeSampleTrack(hitsoundNodes);

        if (CachedSoundFactory.GetCount(SkinSoundFlag) == 0)
        {
            foreach (var file in new DirectoryInfo(null).EnumerateFiles("*.ogg"))
            {
                await CachedSoundFactory.GetOrCreateCacheSound(Engine.FileWaveFormat, file.FullName);
            }
        }

        Duration = Tracks.Max(k => k.Duration);
    }

    private async ValueTask InitializeMusicTrack()
    {
        var audioFile = Path.Combine(_folder, _osuFile.General?.AudioFilename ?? "audio.mp3");

        _musicTrack.Path = audioFile;
        await _musicTrack.InitializeAsync();
    }

    private async ValueTask InitializeHitsoundTrack(List<HitsoundNode> hitsoundNodes)
    {
        _hitsoundTrack.HitsoundNodes = hitsoundNodes.Where(k => k is not PlayableNode { PlayablePriority: PlayablePriority.Sampling }).ToList();
        await _hitsoundTrack.InitializeAsync();
    }

    private async ValueTask InitializeSampleTrack(List<HitsoundNode> hitsoundNodes)
    {
        _sampleTrack.HitsoundNodes = hitsoundNodes.Where(k => k is PlayableNode { PlayablePriority: PlayablePriority.Sampling }).ToList();
        await _sampleTrack.InitializeAsync();
    }

    public override void OnStatusChanged(TimerStatus previousStatus, TimerStatus currentStatus)
    {
        if (previousStatus is TimerStatus.Start or TimerStatus.Restart &&
            currentStatus is TimerStatus.Stop or TimerStatus.Reset)
        {
            foreach (var track in Tracks.Where(k => k is SoundSeekingTrack))
            {
                RemoveFromMusicMixer(track.RootSampleProvider);
            }
        }
        else if (previousStatus is TimerStatus.Stop or TimerStatus.Reset &&
                 currentStatus is TimerStatus.Start or TimerStatus.Restart)
        {
            foreach (var track in Tracks.Where(k => k is SoundSeekingTrack))
            {
                AddToMusicMixer(track.RootSampleProvider);
            }
        }
    }

    private void AddToMusicMixer(ISampleProvider? sampleProvider)
    {
        if (sampleProvider == null) return;
        if (_trackHashSet.Contains(sampleProvider)) return;

        _trackHashSet.Add(sampleProvider);
        _musicMixer.AddMixerInput(sampleProvider);
    }

    private void RemoveFromMusicMixer(ISampleProvider? sampleProvider)
    {
        if (sampleProvider == null) return;

        _trackHashSet.Remove(sampleProvider);
        _musicMixer.RemoveMixerInput(sampleProvider);
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