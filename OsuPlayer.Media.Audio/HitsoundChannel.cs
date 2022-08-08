using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Coosu.Beatmap;
using Coosu.Beatmap.Sections.GamePlay;
using Coosu.Beatmap.Sections.HitObject;
using Coosu.Beatmap.Sections.Timing;
using Milki.Extensions.MixPlayer;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.Subchannels;
using Milky.OsuPlayer.Common;

namespace Milky.OsuPlayer.Media.Audio
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