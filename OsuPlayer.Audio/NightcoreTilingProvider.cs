using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Coosu.Beatmap;
using Coosu.Beatmap.Sections.Timing;
using Milki.Extensions.MixPlayer;

namespace Milki.OsuPlayer.Audio;

internal class NightcoreTilingProvider : ISoundElementsProvider
{
    private readonly struct RhythmGroup
    {
        public RhythmGroup(int periodCount, int loopCount, (string, int)[] relativeNode)
        {
            PeriodCount = periodCount;
            LoopCount = loopCount;
            RelativeNodes = relativeNode;
        }

        public readonly int PeriodCount;
        public readonly int LoopCount;
        public readonly (string fileName, int skipRhythm)[] RelativeNodes;
    }

    private readonly OsuFile _osuFile;
    private readonly TimeSpan _maxDuration;

    private readonly string? _ncFinish;
    private readonly string? _ncKick;
    private readonly string? _ncClap;
    private readonly Dictionary<int, RhythmGroup>? _rhythmDeclarations;

    public NightcoreTilingProvider(string defaultFolder, OsuFile osuFile, TimeSpan maxDuration)
    {
        _osuFile = osuFile;
        _maxDuration = maxDuration;

        _ncFinish ??= Path.Combine(defaultFolder, "nightcore-finish.wav");
        _ncKick ??= Path.Combine(defaultFolder, "nightcore-kick.wav");
        _ncClap ??= Path.Combine(defaultFolder, "nightcore-clap.wav");
        _rhythmDeclarations ??= new Dictionary<int, RhythmGroup>
        {
            [3] = new(6, 4, new[]
            {
                (_ncKick, 2), (_ncClap, 1), (_ncKick, 2), (_ncClap, 1),
            }),
            [4] = new(8, 4, new[]
            {
                (_ncKick, 2), (_ncClap, 2), (_ncKick, 2), (_ncClap, 2),
            }),
            [5] = new(5, 8, new[]
            {
                (_ncKick, 2), (_ncClap, 2), (_ncKick, 1),
            }),
            [6] = new(6, 8, new[]
            {
                (_ncKick, 2), (_ncClap, 2), (_ncKick, 2),
            }),
            [7] = new(7, 8, new[]
            {
                (_ncKick, 2), (_ncClap, 2), (_ncKick, 2), (_ncClap, 1),
            })
        };
    }

    public Task<IEnumerable<SoundElement>> GetSoundElements()
    {
        var timingSection = _osuFile.TimingPoints;
        var redLines = timingSection?.TimingList.Where(k => !k.IsInherit)
                       ?? Array.Empty<TimingPoint>();
        var allTimings = timingSection?.GetInterval(0.5)
                         ?? new Dictionary<double, double>();
        var redLineGroups = redLines
            .Select(k =>
                (k, allTimings.FirstOrDefault(o => Math.Abs(o.Key - k.Offset) < 0.001).Value)
            )
            .ToList();

        var maxTime = MathEx.Max(_maxDuration.TotalMilliseconds,
            _osuFile.HitObjects?.MaxTime ?? 0,
            timingSection?.MaxTime ?? 0
        );
        var hitsoundList = new List<SoundElement>();

        for (int i = 0; i < redLineGroups.Count; i++)
        {
            var (currentLine, interval) = redLineGroups[i];
            var startTime = currentLine.Offset;
            var endTime = i == redLineGroups.Count - 1 ? maxTime : redLineGroups[i + 1].k.Offset;
            var rhythm = currentLine.Rhythm;

            double period; // 一个周期的1/2数量
            double loopCount; // 周期总数
            double currentTime = startTime;

            if (!_rhythmDeclarations!.TryGetValue(rhythm, out var ncRhythm))
            {
                rhythm = 4;
                ncRhythm = _rhythmDeclarations[rhythm];
            }

            period = ncRhythm.PeriodCount * interval;
            loopCount = ncRhythm.LoopCount;
            var exit = false;
            while (!exit)
            {
                for (int j = 0; j < loopCount; j++)
                {
                    if (exit) break;
                    if (j == 0)
                    {
                        var soundElement = GetHitsoundAndSkip(ref currentTime, 0, _ncFinish!);
                        hitsoundList.Add(soundElement);
                    }

                    foreach (var (fileName, skipRhythm) in ncRhythm.RelativeNodes)
                    {
                        var soundElement = GetHitsoundAndSkip(ref currentTime, interval * skipRhythm, fileName);
                        if (!(soundElement.Offset >= endTime))
                        {
                            hitsoundList.Add(soundElement);
                        }
                        else
                        {
                            if (soundElement.Offset.Equals(endTime)) hitsoundList.Add(soundElement);
                            exit = true;
                            break;
                        }
                    }
                }
            }
        }

        return Task.FromResult<IEnumerable<SoundElement>>(hitsoundList);
    }

    private static SoundElement GetHitsoundAndSkip(ref double currentTime, double skipTime,
        string fileName)
    {
        var ele = SoundElement.Create(currentTime, 1, 0, fileName);
        currentTime += skipTime;
        return ele;
    }
}

public static class MathEx
{
    public static T Max<T>(params T[] values) where T : struct, IComparable
    {
        return Max(values.AsEnumerable());
    }

    public static T Max<T>(IEnumerable<T> values) where T : struct, IComparable
    {
        var def = default(T);

        foreach (var value in values)
        {
            if (def.CompareTo(value) < 0) def = value;
        }

        return def;
    }
}