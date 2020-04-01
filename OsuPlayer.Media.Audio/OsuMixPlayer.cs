using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Media.Audio.Player;
using Milky.OsuPlayer.Media.Audio.Player.Subchannels;
using Milky.OsuPlayer.Media.Audio.Wave;
using OSharp.Beatmap;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Media.Audio
{
    public class OsuMixPlayer : MultichannelPlayer
    {
        private readonly OsuFile _osuFile;
        private readonly string _sourceFolder;

        public override string Description { get; } = "OsuPlayer";

        public SingleMediaChannel MusicChannel { get; private set; }
        public HitsoundChannel HitsoundChannel { get; private set; }
        public SampleChannel SampleChannel { get; private set; }

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

        private static readonly ConcurrentDictionary<string, string> PathCache =
            new ConcurrentDictionary<string, string>();

        private int _manualOffset;

        public OsuMixPlayer(OsuFile osuFile, string sourceFolder)
        {
            _osuFile = osuFile;
            _sourceFolder = sourceFolder;
        }

        public override async Task Initialize()
        {
            if (CachedSound.DefaultSounds.Count == 0)
            {
                var files = new DirectoryInfo(Domain.DefaultPath).GetFiles("*.wav");
                await CachedSound.CreateDefaultCacheSounds(files.Select(k => k.FullName));
            }

            var mp3Path = Path.Combine(_sourceFolder, _osuFile.General.AudioFilename);
            MusicChannel = new SingleMediaChannel(Engine, mp3Path,
                AppSettings.Default.Play.PlaybackRate,
                AppSettings.Default.Play.PlayUseTempo)
            {
                Description = "Music"
            };
            AddSubchannel(MusicChannel);
            await MusicChannel.Initialize().ConfigureAwait(false);

            HitsoundChannel = new HitsoundChannel(_osuFile, _sourceFolder, Engine);
            AddSubchannel(HitsoundChannel);
            await HitsoundChannel.Initialize().ConfigureAwait(false);

            SampleChannel = new SampleChannel(this, _osuFile, _sourceFolder, Engine);
            AddSubchannel(SampleChannel);
            await SampleChannel.Initialize().ConfigureAwait(false);

            Duration = MathEx.Max(MusicChannel?.ChannelEndTime ?? TimeSpan.Zero,
                HitsoundChannel?.ChannelEndTime ?? TimeSpan.Zero,
                SampleChannel?.ChannelEndTime ?? TimeSpan.Zero);

            foreach (var channel in Subchannels)
            {
                channel.PlayStatusChanged += status => Console.WriteLine($"{channel.Description}: {status}");
            }

            PlayStatus = PlayStatus.Ready;
        }

        public async Task SetPlayMod(PlayModifier modifier)
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

            AppSettings.SaveDefault();
        }

        public static string GetFileUntilFind(string sourceFolder, string fileNameWithoutExtension)
        {
            var combine = Path.Combine(sourceFolder, fileNameWithoutExtension);
            if (PathCache.TryGetValue(combine, out var value))
            {
                return value;
            }

            string path = "";
            foreach (var extension in AudioPlaybackEngine.SupportExtensions)
            {
                path = Path.Combine(sourceFolder, fileNameWithoutExtension + extension);

                if (File.Exists(path))
                {
                    PathCache.TryAdd(combine, path);
                    return path;
                }
            }

            PathCache.TryAdd(combine, path);
            return path;
        }
    }
}