using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Coosu.Beatmap;
using Coosu.Beatmap.Extensions;
using Coosu.Beatmap.Extensions.Playback;
using Milki.Extensions.MixPlayer;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.Subchannels;

namespace Milky.OsuPlayer.Media.Audio
{
    public class HitsoundChannel : MultiElementsChannel
    {
        private readonly HitsoundFileCache _cache;
        private readonly OsuFile _osuFile;
        private readonly string _sourceFolder;

        public HitsoundChannel(LocalOsuFile osuFile, AudioPlaybackEngine engine, HitsoundFileCache cache = null)
            : this(osuFile, Path.GetDirectoryName(osuFile.OriginalPath), engine, cache)
        {
        }

        public HitsoundChannel(OsuFile osuFile, string sourceFolder, AudioPlaybackEngine engine, HitsoundFileCache cache = null)
            : base(engine, new MixSettings { ForceMode = true })
        {
            _cache = cache ?? new HitsoundFileCache();

            _osuFile = osuFile;
            _sourceFolder = sourceFolder;

            Description = nameof(HitsoundChannel);
        }

        public override async Task<IEnumerable<SoundElement>> GetSoundElements()
        {
            var directory = new OsuDirectory(_sourceFolder);
            await directory.InitializeAsync("none");

            var hitsoundNodes = await directory.GetHitsoundNodesAsync(_osuFile);
            return hitsoundNodes.Select(hitsoundNode =>
            {
                if (hitsoundNode is PlayableNode playableNode)
                {
                    return SoundElement.Create(playableNode.Offset, playableNode.Volume, playableNode.Balance,
                        playableNode.Filename);
                }

                if (hitsoundNode is ControlNode controlNode)
                {
                    switch (controlNode.ControlType)
                    {
                        case ControlType.StartSliding:
                            return SoundElement.CreateLoopSignal(controlNode.Offset, controlNode.Volume,
                                controlNode.Balance, controlNode.Filename, (int)controlNode.SlideChannel);
                        case ControlType.StopSliding:
                            return SoundElement.CreateLoopStopSignal(controlNode.Offset, (int)controlNode.SlideChannel);
                        case ControlType.ChangeBalance:
                            return SoundElement.CreateLoopBalanceSignal(controlNode.Offset, controlNode.Balance);
                        case ControlType.ChangeVolume:
                            return SoundElement.CreateLoopVolumeSignal(controlNode.Offset, controlNode.Volume);
                        case ControlType.None:
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                throw new ArgumentOutOfRangeException();
            });
        }
    }
}