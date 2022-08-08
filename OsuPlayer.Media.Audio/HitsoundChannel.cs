using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Coosu.Beatmap;
using Milki.Extensions.MixPlayer;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.Subchannels;

namespace Milki.OsuPlayer.Media.Audio
{
    public class HitsoundChannel : MultiElementsChannel
    {
        private readonly FileCache _cache;
        private readonly OsuFile _osuFile;
        private readonly string _sourceFolder;

        public HitsoundChannel(LocalOsuFile osuFile, AudioPlaybackEngine engine, FileCache cache = null)
            : this(osuFile, Path.GetDirectoryName(osuFile.OriginalPath), engine, cache)
        {
        }

        public HitsoundChannel(OsuFile osuFile, string sourceFolder, AudioPlaybackEngine engine, FileCache cache = null)
            : base(engine, new MixSettings { ForceMode = true })
        {
            _cache = cache ?? new FileCache();

            _osuFile = osuFile;
            _sourceFolder = sourceFolder;

            Description = nameof(HitsoundChannel);
        }

        public override async Task<IEnumerable<SoundElement>> GetSoundElements()
        {
            var osuDir = new OsuDirectory(_sourceFolder);
            var hitsoundList = await osuDir.GetHitsoundNodesAsync(_osuFile);

            return Array.Empty<SoundElement>();
        }
    }
}