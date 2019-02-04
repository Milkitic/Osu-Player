using OSharp.Beatmap.Sections.GamePlay;
using OSharp.Beatmap.Sections.HitObject;
using OSharp.Beatmap.Sections.Timing;
using System.Collections.Generic;
using System.Linq;

namespace Milkitic.OsuPlayer.Media
{
    public class HitsoundElement
    {
        public GameMode GameMode { get; set; }
        public double Offset { get; set; }
        public float Volume { get; set; }
        public HitsoundType Hitsound { get; set; }
        public int Track { get; set; }
        public TimingSampleset LineSample { get; set; }
        public ObjectSampleset Sample { get; set; }
        public ObjectSampleset Addition { get; set; }
        public string CustomFile { get; set; }
        public string[] FilePaths { get; set; }
        public string[] DefaultFileNames
        {
            get
            {
                List<string> tracks = new List<string>();
                if (!string.IsNullOrEmpty(CustomFile))
                {
                    tracks.Add(CustomFile);
                    return tracks.ToArray();
                }

                string sample, addition;
                switch (LineSample)
                {

                    case TimingSampleset.Soft:
                        sample = "soft";
                        break;
                    case TimingSampleset.Drum:
                        sample = "drum";
                        break;
                    default:
                    case TimingSampleset.None:
                    case TimingSampleset.Normal:
                        sample = "normal";
                        break;
                }
                switch (Sample)
                {
                    case ObjectSampleset.Soft:
                        sample = "soft";
                        break;
                    case ObjectSampleset.Drum:
                        sample = "drum";
                        break;
                    case ObjectSampleset.Normal:
                        sample = "normal";
                        break;
                }

                switch (Addition)
                {
                    case ObjectSampleset.Soft:
                        addition = "soft";
                        break;
                    case ObjectSampleset.Drum:
                        addition = "drum";
                        break;
                    case ObjectSampleset.Normal:
                        addition = "normal";
                        break;
                    default:
                    case ObjectSampleset.Auto:
                        addition = sample;
                        break;
                }

                if (Hitsound == 0)
                    tracks.Add($"{sample}-hitnormal.wav");
                else
                {
                    if ((Hitsound & HitsoundType.Whistle) == HitsoundType.Whistle)
                        tracks.Add($"{addition}-hitwhistle.wav");
                    if ((Hitsound & HitsoundType.Clap) == HitsoundType.Clap)
                        tracks.Add($"{addition}-hitclap.wav");
                    if ((Hitsound & HitsoundType.Finish) == HitsoundType.Finish)
                        tracks.Add($"{addition}-hitfinish.wav");
                    if ((Hitsound & HitsoundType.Normal) == HitsoundType.Normal ||
                        (Hitsound & HitsoundType.Normal) == 0)
                    {
                        if (GameMode != GameMode.Mania) tracks.Add($"{sample}-hitnormal.wav");
                    }
                }

                return tracks.ToArray();
            }
        }
        public string[] FileNames
        {
            get
            {
                string track = (Track > 1 ? Track.ToString() : "");
                return DefaultFileNames.ToArray().Select(s =>
                {
                    if (s == CustomFile)
                        return s;
                    var splitted = s.Split('.');
                    return splitted.Length == 1 ? s : $"{splitted[0]}{track}.{splitted[1]}";
                }).ToArray();
            }
        }
    }
}
