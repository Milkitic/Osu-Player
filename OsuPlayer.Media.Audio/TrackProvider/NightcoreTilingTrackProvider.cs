using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Milky.OsuPlayer.Media.Audio.Sounds;
using OSharp.Beatmap;
using OSharp.Beatmap.Sections.Timing;

namespace Milky.OsuPlayer.Media.Audio.TrackProvider
{
    class NightcoreTilingTrackProvider : TrackProviderBase
    {
        public NightcoreTilingTrackProvider(OsuFile osuFile) : base(osuFile)
        {
        }

        public override void GetSoundElements()
        {
            var timingSection = OsuFile.TimingPoints;
            var redLines = timingSection.TimingList.Where(k => !k.Inherit);
            var allTimings = timingSection.GetInterval(0.5);
            var keyValuePairs = redLines
                .Select(k =>
                    (k, allTimings.FirstOrDefault(o => Math.Abs(o.Key - k.Offset) < 0.001).Value)
                )
                .ToList();
            var list = new List<SpecificFileSoundElement>();
            for (int i = 0; i < keyValuePairs.Count; i++)
            {
                var (currentLine, interval) = keyValuePairs[i];
                var startTime = currentLine.Offset;
                var endTime = i == keyValuePairs.Count - 1 ? timingSection.MaxTime : keyValuePairs[2].k.Offset;
                var rhythm = currentLine.Rhythm;

                double period;
                double loopCount;
                double currentTime = startTime;

                switch (rhythm)
                {
                    case 3:
                        period = 6 * interval;
                        loopCount = 4;
                        for (int j = 0; j < loopCount; j++)
                        {
                            if (j == 0)
                                list.Add(new SpecificFileSoundElement(1, 0, "nightcore-finish.wav", currentTime));
                            list.Add(new SpecificFileSoundElement(1, 0, "nightcore-kick.wav", currentTime));

                            currentTime += interval * 2;
                            list.Add(new SpecificFileSoundElement(1, 0, "nightcore-clap.wav", currentTime));

                            currentTime += interval;
                            list.Add(new SpecificFileSoundElement(1, 0, "nightcore-kick.wav", currentTime));

                            currentTime += interval * 2;
                            list.Add(new SpecificFileSoundElement(1, 0, "nightcore-clap.wav", currentTime));
                        }

                        break;
                    case 4:
                        period = 8 * interval;
                        loopCount = 4;
                        break;
                    case 5:
                        period = 5 * interval;
                        loopCount = 8;
                        break;
                    case 6:
                        period = 6 * interval;
                        loopCount = 8;
                        break;
                    case 7:
                        period = 7 * interval;
                        loopCount = 8;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
