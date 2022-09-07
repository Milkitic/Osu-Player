using Coosu.Beatmap;
using Coosu.Beatmap.Extensions.Playback;

namespace Milki.OsuPlayer.Audio.Internal;

internal static class NightcoreTilingHelper
{
    private const string NC_FINISH = "nightcore-finish";
    private const string NC_KICK = "nightcore-kick";
    private const string NC_CLAP = "nightcore-clap";

    private static readonly Dictionary<int, RhythmGroup> RhythmDeclarations =
        new()
        {
            [3] = new RhythmGroup(6, 4, new[]
            {
                (NC_KICK, 2), (NC_CLAP, 1), (NC_KICK, 2), (NC_CLAP, 1),
            }),
            [4] = new RhythmGroup(8, 4, new[]
            {
                (NC_KICK, 2), (NC_CLAP, 2), (NC_KICK, 2), (NC_CLAP, 2),
            }),
            [5] = new RhythmGroup(5, 8, new[]
            {
                (NC_KICK, 2), (NC_CLAP, 2), (NC_KICK, 1),
            }),
            [6] = new RhythmGroup(6, 8, new[]
            {
                (NC_KICK, 2), (NC_CLAP, 2), (NC_KICK, 2),
            }),
            [7] = new RhythmGroup(7, 8, new[]
            {
                (NC_KICK, 2), (NC_CLAP, 2), (NC_KICK, 2), (NC_CLAP, 1),
            })
        };

    public static List<HitsoundNode> GetHitsoundNodes(OsuFile osuFile, TimeSpan mp3MaxDuration)
    {
        var timingSection = osuFile.TimingPoints;
        var redLines = timingSection.TimingList.Where(k => !k.IsInherit);
        var allTimings = timingSection.GetInterval(0.5);
        var redLineGroups = redLines
            .Select(k =>
                (k, allTimings.FirstOrDefault(o => Math.Abs(o.Key - k.Offset) < 0.001).Value)
            )
            .ToList();

        var maxTime = MathEx.Max(mp3MaxDuration.TotalMilliseconds,
            osuFile.HitObjects.MaxTime,
            timingSection.MaxTime
        );
        var hitsoundList = new List<HitsoundNode>();

        for (int i = 0; i < redLineGroups.Count; i++)
        {
            var (currentLine, interval) = redLineGroups[i];
            var startTime = currentLine.Offset;
            var endTime = i == redLineGroups.Count - 1 ? maxTime : redLineGroups[i + 1].k.Offset;
            var rhythm = currentLine.Rhythm;

            double period; // 一个周期的1/2数量
            double loopCount; // 周期总数
            double currentTime = startTime;

            if (!RhythmDeclarations.ContainsKey(rhythm))
            {
                rhythm = 4;
            }

            var ncRhythm = RhythmDeclarations[rhythm];
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
                        var soundElement = GetHitsoundAndSkip(ref currentTime, 0, NC_FINISH);
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

        return hitsoundList;
    }

    private static HitsoundNode GetHitsoundAndSkip(ref double currentTime, double skipTime,
        string fileName)
    {
        var ele = HitsoundNode.Create(Guid.NewGuid(), (int)currentTime, 1, 0, fileName, true,
            PlayablePriority.Sampling);
        currentTime += skipTime;
        return ele;
    }

    private class RhythmGroup
    {
        public RhythmGroup(int periodCount, int loopCount, (string, int)[] relativeNode)
        {
            PeriodCount = periodCount;
            LoopCount = loopCount;
            RelativeNodes = relativeNode;
        }

        public int LoopCount { get; set; }

        public int PeriodCount { get; set; }

        public (string fileName, int skipRhythm)[] RelativeNodes { get; set; }
    }
}