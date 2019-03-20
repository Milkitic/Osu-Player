using System.Collections.Generic;
using System.Linq;
using OSharp.Beatmap.Sections.GamePlay;
using OSharp.Beatmap.Sections.HitObject;
using OSharp.Beatmap.Sections.Timing;

namespace Milky.OsuPlayer.Media.Audio
{
    public class HitsoundElement
    {
        public GameMode GameMode { get; set; }
        public double Offset { get; set; }
        public float Volume { get; set; }
        public HitsoundType Hitsound { get; set; }
        public int Track { get; set; }
        public TimingSamplesetType LineSample { get; set; }
        public ObjectSamplesetType Sample { get; set; }
        public ObjectSamplesetType Addition { get; set; }
        public string CustomFile { get; set; }
        public string[] FilePaths { get; set; }
        public string[] DefaultFileNames
        {
            get
            {
                var tracks = new List<string>();
                if (!string.IsNullOrEmpty(CustomFile))
                {
                    tracks.Add(CustomFile);
                    return tracks.ToArray();
                }

                string sample = GetFromLineSample();
                AdjustObjectSample(ref sample);
                string addition = GetObjectAddition(sample);

                if (Hitsound == 0)
                    tracks.Add($"{sample}-hitnormal.wav");
                else
                {
                    if (Hitsound.HasFlag(HitsoundType.Whistle))
                        tracks.Add($"{addition}-hitwhistle.wav");
                    if (Hitsound.HasFlag(HitsoundType.Clap))
                        tracks.Add($"{addition}-hitclap.wav");
                    if (Hitsound.HasFlag(HitsoundType.Finish))
                        tracks.Add($"{addition}-hitfinish.wav");
                    if (Hitsound.HasFlag(HitsoundType.Normal) ||
                        (Hitsound & HitsoundType.Normal) == 0)
                    {
                        if (GameMode != GameMode.Mania)
                            tracks.Add($"{sample}-hitnormal.wav");
                    }
                }

                return tracks.ToArray();
            }
        }

        private string GetObjectAddition(string sample)
        {
            string addition;
            switch (Addition)
            {
                case ObjectSamplesetType.Soft:
                    addition = "soft";
                    break;
                case ObjectSamplesetType.Drum:
                    addition = "drum";
                    break;
                case ObjectSamplesetType.Normal:
                    addition = "normal";
                    break;
                default:
                case ObjectSamplesetType.Auto:
                    addition = sample;
                    break;
            }

            return addition;
        }

        private void AdjustObjectSample(ref string sample)
        {
            switch (Sample)
            {
                case ObjectSamplesetType.Soft:
                    sample = "soft";
                    break;
                case ObjectSamplesetType.Drum:
                    sample = "drum";
                    break;
                case ObjectSamplesetType.Normal:
                    sample = "normal";
                    break;
            }
        }

        private string GetFromLineSample()
        {
            string sample;
            switch (LineSample)
            {
                case TimingSamplesetType.Soft:
                    sample = "soft";
                    break;
                case TimingSamplesetType.Drum:
                    sample = "drum";
                    break;
                default:
                case TimingSamplesetType.None:
                case TimingSamplesetType.Normal:
                    sample = "normal";
                    break;
            }

            return sample;
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
                    var split = s.Split('.');
                    return split.Length == 1 ? s : $"{split[0]}{track}.{split[1]}";
                }).ToArray();
            }
        }
    }
}
