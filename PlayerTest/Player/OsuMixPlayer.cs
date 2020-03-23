using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OSharp.Beatmap;
using OSharp.Beatmap.Sections.HitObject;
using PlayerTest.Player.Channel;

namespace PlayerTest.Player
{
    public class OsuMixPlayer : MultichannelPlayer
    {
        private readonly OsuFile _osuFile;
        private readonly string _sourceFolder;
        private SingleMediaChannel _musicChannel;
        private MultiElementsChannel _hitsoundChannel;
        private MultiElementsChannel _sampleChannel;

        public OsuMixPlayer(OsuFile osuFile, string sourceFolder)
        {
            _osuFile = osuFile;
            _sourceFolder = sourceFolder;
        }

        public override async Task Initialize()
        {
            var mp3Path = Path.Combine(_sourceFolder, _osuFile.General.AudioFilename);
            _musicChannel = new SingleMediaChannel(Engine, mp3Path,
                AppSettings.Default.Play.PlaybackRate,
                AppSettings.Default.Play.PlayUseTempo)
            {
                Description = "Music"
            };

            var hitsoundList = GetHitsoundsAsync();
            _hitsoundChannel = new MultiElementsChannel(Engine, hitsoundList, _musicChannel)
            {
                Description = "Hitsound"
            };


            var sampleList = GetSamplesAsync();
            _sampleChannel = new MultiElementsChannel(Engine, sampleList, _musicChannel)
            {
                Description = "Sample"
            };

            AddSubchannel(_musicChannel);
            AddSubchannel(_hitsoundChannel);
            AddSubchannel(_sampleChannel);
        }

        private List<SoundElement> GetHitsoundsAsync()
        {
            List<RawHitObject> hitObjects = _osuFile.HitObjects.HitObjectList;
            var elements = new List<SoundElement>();
            var dirInfo = new DirectoryInfo(_sourceFolder);
            var waves = new HashSet<string>(dirInfo.EnumerateFiles()
                .Where(k => AudioPlaybackEngine.SupportExtensions.Contains(
                    k.Extension, StringComparer.OrdinalIgnoreCase)
                )
                .Select(p => Path.GetFileNameWithoutExtension(p.FullName))
            );


        }

        private List<SoundElement> GetSamplesAsync()
        {
            throw new NotImplementedException();
        }
    }
}