using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OSharp.Beatmap;
using PlayerTest.Player.Channel;

namespace PlayerTest.TrackProvider
{
    class NightcoreTilingTrackProvider : ITrackProvider
    {
        private readonly OsuFile _osuFile;
        private readonly TimeSpan _maxDuration;

        public NightcoreTilingTrackProvider(OsuFile osuFile, TimeSpan maxDuration)
        {
            _osuFile = osuFile;
            _maxDuration = maxDuration;
        }

        private static readonly string NC_FINISH = Path.Combine(Domain.DefaultPath, "nightcore-finish.wav");
        private static readonly string NC_KICK = Path.Combine(Domain.DefaultPath, "nightcore-kick.wav");
        private static readonly string NC_CLAP = Path.Combine(Domain.DefaultPath, "nightcore-clap.wav");

        private struct RhythmGroup
        {
            public RhythmGroup(int periodCount, int loopCount, (string, int)[] relativeNode)
            {
                PeriodCount = periodCount;
                LoopCount = loopCount;
                RelativeNodes = relativeNode;
            }

            public int PeriodCount { get; set; }

            public int LoopCount { get; set; }

            public (string fileName, int skipRhythm)[] RelativeNodes { get; set; }
        }

        private readonly Dictionary<int, RhythmGroup> _rhythmDeclarations =
            new Dictionary<int, RhythmGroup>
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

        public IEnumerable<SoundElement> GetSoundElements()
        {
            var timingSection = _osuFile.TimingPoints;
            var redLines = timingSection.TimingList.Where(k => !k.Inherit);
            var allTimings = timingSection.GetInterval(0.5);
            var redlineGroups = redLines
                .Select(k =>
                    (k, allTimings.FirstOrDefault(o => Math.Abs(o.Key - k.Offset) < 0.001).Value)
                )
                .ToList();

            var maxTime = MathEx.Max(_maxDuration.TotalMilliseconds,
                _osuFile.HitObjects.MaxTime,
                timingSection.MaxTime
            );
            var hitsoundList = new List<SoundElement>();

            for (int i = 0; i < redlineGroups.Count; i++)
            {
                var (currentLine, interval) = redlineGroups[i];
                var startTime = currentLine.Offset;
                var endTime = i == redlineGroups.Count - 1 ? maxTime : redlineGroups[i + 1].k.Offset;
                var rhythm = currentLine.Rhythm;

                double period; // 一个周期的1/2数量
                double loopCount; // 周期总数
                double currentTime = startTime;

                if (!_rhythmDeclarations.ContainsKey(rhythm))
                {
                    rhythm = 4;
                }

                var ncRhythm = _rhythmDeclarations[rhythm];
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
                            var (updatedCurrent, soundElement) =
                                 GetHitsoundAndSkip(currentTime, 0, NC_FINISH);
                            hitsoundList.Add(soundElement);
                            currentTime = updatedCurrent;
                        }

                        foreach (var (fileName, skipRhythm) in ncRhythm.RelativeNodes)
                        {
                            var (updatedCurrent, soundElement) =
                                 GetHitsoundAndSkip(currentTime, interval * skipRhythm, fileName);
                            currentTime = updatedCurrent;
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

        private static (double, SoundElement) GetHitsoundAndSkip(double currentTime, double skipTime,
            string fileName)
        {
            var ele = SoundElement.Create(currentTime, 1, 0, fileName);
            currentTime += skipTime;
            return (currentTime, ele);
        }
    }

    public static class MathEx
    {
        public static T Max<T>(params T[] values) where T : IComparable
        {
            var def = default(T);

            foreach (var value in values)
            {
                if (def == null || def.CompareTo(value) < 0) def = value;
            }

            return def;
        }
    }
}
