using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Media.Audio.Player;
using Milky.OsuPlayer.Media.Audio.Player.Subchannels;
using Milky.OsuPlayer.Media.Audio.Playlist;
using Milky.OsuPlayer.Media.Audio.Wave;
using OSharp.Beatmap;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Milki.Extensions.MixPlayer;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using Milki.Extensions.MixPlayer.Subchannels;
using NAudio.Wave;

namespace Milky.OsuPlayer.Media.Audio
{
    public class OsuMixPlayer : MultichannelPlayer
    {
        public static OsuMixPlayer Current { get; private set; }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly ConcurrentDictionary<string, string> _pathCache =
            new ConcurrentDictionary<string, string>();

        private OsuFile _osuFile;
        private string _sourceFolder;
        private int _manualOffset;

        public override string Description { get; } = "OsuPlayer";

        public SingleMediaChannel MusicChannel { get; private set; }
        public HitsoundChannel HitsoundChannel { get; private set; }
        public SampleChannel SampleChannel { get; private set; }
        public IWavePlayer Device => Engine.OutputDevice;
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

        public OsuMixPlayer(OsuFile osuFile, string sourceFolder)
        {
            _osuFile = osuFile;
            _sourceFolder = sourceFolder;
            Current = this;
        }

        public override async Task Initialize()
        {
            try
            {
                if (CachedSound.DefaultSounds.Count == 0)
                {
                    var files = new DirectoryInfo(Domain.DefaultPath).GetFiles("*.wav");
                    await CachedSound.CreateDefaultCacheSounds(files.Select(k => k.FullName)).ConfigureAwait(false);
                }

                await InnerLoad().ConfigureAwait(false);
                await base.Initialize().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while Initializing players.");
                throw;
            }
        }

        public async Task Reload(OsuFile osuFile, string sourceFolder)
        {
            await DisposeSubChannelsAsync();
            _osuFile = osuFile;
            _sourceFolder = sourceFolder;

            await InnerLoad().ConfigureAwait(false);
            await base.Initialize().ConfigureAwait(false);
        }

        private async Task InnerLoad()
        {
            var mp3Path = Path.Combine(_sourceFolder, _osuFile.General.AudioFilename);
            MusicChannel = new SingleMediaChannel(Engine, mp3Path,
                AppSettings.Default?.Play?.PlaybackRate ?? 1,
                AppSettings.Default?.Play?.PlayUseTempo ?? true)
            {
                Description = "Music",
                IsReferenced = true
            };

            AddSubchannel(MusicChannel);
            await MusicChannel.Initialize().ConfigureAwait(false);

            HitsoundChannel = new HitsoundChannel(this, _osuFile, _sourceFolder, Engine);
            AddSubchannel(HitsoundChannel);
            await HitsoundChannel.Initialize().ConfigureAwait(false);

            SampleChannel = new SampleChannel(this, _osuFile, _sourceFolder, Engine);
            AddSubchannel(SampleChannel);
            await SampleChannel.Initialize().ConfigureAwait(false);
            //await CachedSound.CreateCacheSounds(HitsoundChannel.SoundElementCollection
            //    .Where(k => k.FilePath != null)
            //    .Select(k => k.FilePath)
            //    .Concat(SampleChannel.SoundElementCollection
            //        .Where(k => k.FilePath != null)
            //        .Select(k => k.FilePath))
            //    .Concat(new[] { mp3Path })
            //).ConfigureAwait(false);
            foreach (var channel in Subchannels)
            {
                channel.PlayStatusChanged += status => Logger.Debug($"{channel.Description}: {status}");
            }

            InitVolume();

            await CachedSound.GetOrCreateCacheSound(mp3Path);
            await BufferSoundElementsAsync();
        }

        private void InitVolume()
        {
            MusicChannel.Volume = AppSettings.Default?.Volume?.Music ?? 1;
            HitsoundChannel.Volume = AppSettings.Default?.Volume?.Hitsound ?? 1;
            HitsoundChannel.BalanceFactor = AppSettings.Default?.Volume?.BalanceFactor / 100 ?? 0;
            SampleChannel.Volume = AppSettings.Default?.Volume?.Sample ?? 1;
            Volume = AppSettings.Default?.Volume?.Main ?? 1;
            if (AppSettings.Default?.Volume != null)
                AppSettings.Default.Volume.PropertyChanged += Volume_PropertyChanged;
        }

        private void Volume_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AppSettings.Default.Volume.Music):
                    MusicChannel.Volume = AppSettings.Default.Volume.Music;
                    break;
                case nameof(AppSettings.Default.Volume.Hitsound):
                    HitsoundChannel.Volume = AppSettings.Default.Volume.Hitsound;
                    break;
                case nameof(AppSettings.Default.Volume.BalanceFactor):
                    HitsoundChannel.BalanceFactor = AppSettings.Default.Volume.BalanceFactor / 100;
                    break;
                case nameof(AppSettings.Default.Volume.Sample):
                    SampleChannel.Volume = AppSettings.Default.Volume.Sample;
                    break;
                case nameof(AppSettings.Default.Volume.Main):
                    Volume = AppSettings.Default.Volume.Main;
                    break;
            }
        }

        public async Task SetPlayMod(PlayModifier modifier)
        {
            switch (modifier)
            {
                case PlayModifier.None:
                    await SetPlaybackRate(1, false).ConfigureAwait(false);
                    break;
                case PlayModifier.DoubleTime:
                    await SetPlaybackRate(1.5f, true).ConfigureAwait(false);
                    break;
                case PlayModifier.NightCore:
                    await SetPlaybackRate(1.5f, false).ConfigureAwait(false);
                    break;
                case PlayModifier.HalfTime:
                    await SetPlaybackRate(0.75f, true).ConfigureAwait(false);
                    break;
                case PlayModifier.DayCore:
                    await SetPlaybackRate(0.75f, false).ConfigureAwait(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(modifier), modifier, null);
            }

            AppSettings.SaveDefault();
        }

        public string GetFileUntilFind(string sourceFolder, string fileNameWithoutExtension)
        {
            var combine = Path.Combine(sourceFolder, fileNameWithoutExtension);
            if (_pathCache.TryGetValue(combine, out var value))
            {
                return value;
            }

            string path = "";
            foreach (var extension in AudioPlaybackEngine.SupportExtensions)
            {
                path = Path.Combine(sourceFolder, fileNameWithoutExtension + extension);

                if (File.Exists(path))
                {
                    _pathCache.TryAdd(combine, path);
                    return path;
                }
            }

            _pathCache.TryAdd(combine, path);
            return path;
        }

        public override ValueTask DisposeAsync()
        {
            if (AppSettings.Default?.Volume != null)
                AppSettings.Default.Volume.PropertyChanged -= Volume_PropertyChanged;
            return base.DisposeAsync();
        }
    }
}