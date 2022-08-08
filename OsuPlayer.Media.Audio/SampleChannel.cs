using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Coosu.Beatmap;
using Milki.Extensions.MixPlayer;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.Subchannels;

namespace Milky.OsuPlayer.Media.Audio
{
    public class SampleChannel : MultiElementsChannel
    {
        private readonly FileCache _cache;
        private readonly OsuFile _osuFile;
        private readonly string _sourceFolder;
        private NightcoreTilingProvider _nightcore;

        public SampleChannel(LocalOsuFile osuFile, AudioPlaybackEngine engine,
            ICollection<Subchannel> referencedChannels, FileCache cache = null)
            : this(osuFile, Path.GetDirectoryName(osuFile.OriginalPath), engine, referencedChannels, cache)
        {
        }

        public SampleChannel(OsuFile osuFile, string sourceFolder, AudioPlaybackEngine engine,
            ICollection<Subchannel> referencedChannels, FileCache cache = null)
            : base(engine, new MixSettings(), referencedChannels)
        {
            _cache = cache;
            _osuFile = osuFile;
            _sourceFolder = sourceFolder;

            Description = nameof(SampleChannel);
        }

        public override async Task<IEnumerable<SoundElement>> GetSoundElements()
        {
            var elements = new ConcurrentBag<SoundElement>();
            var samples = _osuFile.Events?.Samples;
            if (samples == null)
                return new List<SoundElement>(elements);

            await Task.Run(() =>
            {
                samples.AsParallel()
                    .WithDegreeOfParallelism(Environment.ProcessorCount > 1 ? Environment.ProcessorCount - 1 : 1)
                    .ForAll(sample =>
                    {
                        var element = SoundElement.Create(sample.Offset, sample.Volume / 100f, 0,
                            _cache.GetFileUntilFind(_sourceFolder, Path.GetFileNameWithoutExtension(sample.Filename))
                        );
                        elements.Add(element);
                    });
            }).ConfigureAwait(false);

            var elementList = new List<SoundElement>(elements);

            if (PlaybackRate.Equals(1.5f) && !KeepTune)
            {
                var duration1 = MathEx.Max(ReferencedChannels.Select(k => k.ChannelEndTime));
                var duration = MathEx.Max(duration1,
                    TimeSpan.FromMilliseconds(samples.Count == 0 ? 0 : samples.Max(k => k.Offset))
                );
                _nightcore = new NightcoreTilingProvider(_osuFile, duration);
                elementList.AddRange(await _nightcore.GetSoundElements().ConfigureAwait(false));
            }

            return elementList;
        }

        public override async Task SetPlaybackRate(float rate, bool useTempo)
        {
            var oldRate = PlaybackRate;
            var oldTempo = KeepTune;
            await base.SetPlaybackRate(rate, useTempo).ConfigureAwait(false);
            if (oldTempo != KeepTune)
            {
                SoundElements = null;
                await RequeueAsync(Position).ConfigureAwait(false);
            }
        }
    }
}