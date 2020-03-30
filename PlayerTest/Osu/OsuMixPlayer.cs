using OSharp.Beatmap;
using PlayerTest.Player;
using PlayerTest.Player.Subchannels;
using PlayerTest.Wave;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PlayerTest.Osu
{
    public class OsuMixPlayer : MultichannelPlayer
    {
        static OsuMixPlayer()
        {
            var files = new DirectoryInfo(Domain.DefaultPath).GetFiles("*.wav");
            CachedSound.CreateCacheSounds(files.Select(k => k.FullName));
        }

        private readonly OsuFile _osuFile;
        private readonly string _sourceFolder;

        public SingleMediaChannel MusicChannel { get; private set; }
        public HitsoundChannel HitsoundChannel { get; private set; }
        public SampleChannel SampleChannel { get; private set; }

        private static readonly ConcurrentDictionary<string, string> PathCache =
            new ConcurrentDictionary<string, string>();

        public OsuMixPlayer(OsuFile osuFile, string sourceFolder)
        {
            _osuFile = osuFile;
            _sourceFolder = sourceFolder;
        }

        public override async Task Initialize()
        {
            var mp3Path = Path.Combine(_sourceFolder, _osuFile.General.AudioFilename);
            MusicChannel = new SingleMediaChannel(Engine, mp3Path,
                AppSettings.Default.Play.PlaybackRate,
                AppSettings.Default.Play.PlayUseTempo)
            {
                Description = "Music"
            };
            AddSubchannel(MusicChannel);
            await MusicChannel.Initialize();

            HitsoundChannel = new HitsoundChannel(_osuFile, _sourceFolder, Engine);
            AddSubchannel(HitsoundChannel);
            await HitsoundChannel.Initialize();

            SampleChannel = new SampleChannel(this, _osuFile, _sourceFolder, Engine);
            AddSubchannel(SampleChannel);
            await SampleChannel.Initialize();

            Duration = MathEx.Max(MusicChannel.ChannelEndTime, HitsoundChannel.ChannelEndTime,
                SampleChannel.ChannelEndTime);

            //_hitsoundChannel.PositionUpdated+= (time) => Console.WriteLine($"{_hitsoundChannel.Description}: {time}");
            foreach (var channel in Subchannels)
            {
                channel.PlayStatusChanged += (status) => Console.WriteLine($"{channel.Description}: {status}");
            }

            PlayStatus = PlayStatus.Ready;
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