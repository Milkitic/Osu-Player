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

namespace Milki.OsuPlayer.Audio;

public sealed class SampleChannel : MultiElementsChannel
{
    private readonly FileCache _cache;
    private readonly OsuFile _osuFile;
    private readonly string _sourceFolder;
    private readonly string _defaultFolder;

    private NightcoreTilingProvider? _nightcoreTilingProvider;

    public SampleChannel(string defaultFolder, LocalOsuFile osuFile, AudioPlaybackEngine engine,
        ICollection<Subchannel> referencedChannels, FileCache? cache = null)
        : this(defaultFolder, osuFile, Path.GetDirectoryName(osuFile.OriginalPath)!, engine, referencedChannels, cache)
    {
    }

    public SampleChannel(string defaultFolder, OsuFile osuFile, string sourceFolder,
        AudioPlaybackEngine engine,
        ICollection<Subchannel> referencedChannels, FileCache? cache = null)
        : base(engine, new MixSettings(), referencedChannels)
    {
        _cache = cache ?? new FileCache();
        _defaultFolder = defaultFolder;
        _osuFile = osuFile;
        _sourceFolder = sourceFolder;

        Description = nameof(SampleChannel);
    }

    public override async Task<IEnumerable<SoundElement>> GetSoundElements()
    {
        var elements = new ConcurrentBag<SoundElement>();
        var samples = _osuFile.Events?.Samples;
        if (samples == null)
        {
            return Array.Empty<SoundElement>();
        }

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
        });

        var elementList = new List<SoundElement>(elements);

        if (PlaybackRate.Equals(1.5f) && !KeepTune)
        {
            var duration1 = MathEx.Max(ReferencedChannels.Select(k => k.ChannelEndTime));
            var duration = MathEx.Max(duration1,
                TimeSpan.FromMilliseconds(samples.Count == 0 ? 0 : samples.Max(k => k.Offset))
            );
            _nightcoreTilingProvider = new NightcoreTilingProvider(_defaultFolder, _osuFile, duration.TotalMilliseconds);
            elementList.AddRange(await _nightcoreTilingProvider.GetSoundElements());
        }

        return elementList;
    }

    public override async Task SetPlaybackRate(float rate, bool keepTune)
    {
        var oldRate = PlaybackRate;
        var oldKeepTune = KeepTune;
        await base.SetPlaybackRate(rate, keepTune);
        if (oldKeepTune != KeepTune)
        {
            SoundElements = null;
            await RequeueAsync(Position);
        }
    }
}