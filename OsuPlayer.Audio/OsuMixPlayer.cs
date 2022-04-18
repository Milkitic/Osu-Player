using Anotar.NLog;
using Coosu.Beatmap;
using Milki.Extensions.MixPlayer;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using Milki.Extensions.MixPlayer.Subchannels;
using NAudio.Wave;

namespace OsuPlayer.Audio;

public sealed class OsuMixPlayer : MultichannelPlayer
{
    private readonly FileCache _fileCache = new();
    private readonly PlayerOptions _options;
    private readonly OsuFile _osuFile;
    private readonly string _sourceFolder;

    private int _manualOffset;

    public OsuMixPlayer(PlayerOptions options, LocalOsuFile osuFile)
        : base(options.DeviceDescription)
    {
        _options = options;
        _osuFile = osuFile;
        _sourceFolder = Path.GetDirectoryName(osuFile.OriginPath)!;
    }

    public OsuMixPlayer(PlayerOptions options, OsuFile osuFile, string sourceFolder)
        : base(options.DeviceDescription)
    {
        _options = options;
        _osuFile = osuFile;
        _sourceFolder = sourceFolder;
    }

    public override string Description => nameof(OsuMixPlayer);

    public SingleMediaChannel? MusicChannel { get; private set; }
    public HitsoundChannel? HitsoundChannel { get; private set; }
    public SampleChannel? SampleChannel { get; private set; }
    public IWavePlayer? Device => Engine.OutputDevice;

    public int ManualOffset
    {
        get => _manualOffset;
        set
        {
            if (HitsoundChannel != null) HitsoundChannel.ManualOffset = value;
            if (SampleChannel != null) SampleChannel.ManualOffset = value;
            _manualOffset = value;
        }
    }

    public override async Task Initialize()
    {
        try
        {
            if (CachedSoundFactory.GetCount("special") == 0)
            {
                foreach (var file in new DirectoryInfo(_options.DefaultFolder).EnumerateFiles("*.wav"))
                {
                    await CachedSoundFactory.GetOrCreateCacheSound(Engine.FileWaveFormat, file.FullName);
                }
            }

            var mp3Path = Path.Combine(_sourceFolder, _osuFile?.General.AudioFilename ?? ".");
            MusicChannel = new SingleMediaChannel(Engine, mp3Path, _options.InitialPlaybackRate, _options.InitialKeepTune)
            {
                Description = "Music",
                IsReferenced = true
            };

            AddSubchannel(MusicChannel);
            await MusicChannel.Initialize();

            HitsoundChannel = new HitsoundChannel(_options.DefaultFolder, _osuFile, _sourceFolder, Engine, _fileCache);
            AddSubchannel(HitsoundChannel);
            await HitsoundChannel.Initialize();

            SampleChannel = new SampleChannel(_options.DefaultFolder, _osuFile, _sourceFolder, Engine, new Subchannel[]
            {
                MusicChannel, HitsoundChannel
            }, _fileCache);
            AddSubchannel(SampleChannel);
            await SampleChannel.Initialize();
            //await CachedSound.CreateCacheSounds(HitsoundChannel.SoundElementCollection
            //    .Where(k => k.FilePath != null)
            //    .Select(k => k.FilePath)
            //    .Concat(SampleChannel.SoundElementCollection
            //        .Where(k => k.FilePath != null)
            //        .Select(k => k.FilePath))
            //    .Concat(new[] { mp3Path })
            //);
            foreach (var channel in Subchannels)
            {
                channel.PlayStatusChanged += status =>
                {
                    LogTo.Debug(() => $"{channel.Description} PlayStatus changed to {status}.");
                };
            }

            SetMainVolume(_options.InitialMainVolume);
            SetMusicVolume(_options.InitialMusicVolume);
            SetHitsoundVolume(_options.InitialHitsoundVolume);
            SetSampleVolume(_options.InitialSampleVolume);
            SetHitsoundBalance(_options.InitialHitsoundBalance);
            ManualOffset = _options.InitialOffset;

            await CachedSoundFactory.GetOrCreateCacheSound(Engine.FileWaveFormat, mp3Path);
            await BufferSoundElementsAsync();

            await base.Initialize();
        }
        catch (Exception ex)
        {
            LogTo.ErrorException("Error while Initializing players.", ex);
            throw;
        }
    }

    public void SetMainVolume(float volume)
    {
        Volume = volume;
    }

    public void SetMusicVolume(float volume)
    {
        if (MusicChannel != null) MusicChannel.Volume = volume;
    }

    public void SetHitsoundVolume(float volume)
    {
        if (HitsoundChannel != null) HitsoundChannel.Volume = volume;
    }

    public void SetSampleVolume(float volume)
    {
        if (SampleChannel != null) SampleChannel.Volume = volume;
    }

    public void SetHitsoundBalance(float balance)
    {
        if (HitsoundChannel != null) HitsoundChannel.BalanceFactor = balance;
    }

    public async Task SetPlayMods(PlayModifier modifier)
    {
        switch (modifier)
        {
            case PlayModifier.None:
                await SetPlaybackRate(1, false);
                break;
            case PlayModifier.DoubleTime:
                await SetPlaybackRate(1.5f, true);
                break;
            case PlayModifier.NightCore:
                await SetPlaybackRate(1.5f, false);
                break;
            case PlayModifier.HalfTime:
                await SetPlaybackRate(0.75f, true);
                break;
            case PlayModifier.DayCore:
                await SetPlaybackRate(0.75f, false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(modifier), modifier, null);
        }
    }
}