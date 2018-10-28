using Milkitic.OsuLib.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Milkitic.OsuPlayer.Media
{
    public class HitsoundElement
    {
        public GameModeEnum GameMode { get; set; }
        public double Offset { get; set; }
        public float Volume { get; set; }
        public HitsoundType Hitsound { get; set; }
        public int Track { get; set; }
        public SamplesetEnum LineSample { get; set; }
        public SampleAdditonEnum Sample { get; set; }
        public SampleAdditonEnum Addition { get; set; }
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

                    case SamplesetEnum.Soft:
                        sample = "soft";
                        break;
                    case SamplesetEnum.Drum:
                        sample = "drum";
                        break;
                    default:
                    case SamplesetEnum.None:
                    case SamplesetEnum.Normal:
                        sample = "normal";
                        break;
                }
                switch (Sample)
                {
                    case SampleAdditonEnum.Soft:
                        sample = "soft";
                        break;
                    case SampleAdditonEnum.Drum:
                        sample = "drum";
                        break;
                    case SampleAdditonEnum.Normal:
                        sample = "normal";
                        break;
                }

                switch (Addition)
                {
                    case SampleAdditonEnum.Soft:
                        addition = "soft";
                        break;
                    case SampleAdditonEnum.Drum:
                        addition = "drum";
                        break;
                    case SampleAdditonEnum.Normal:
                        addition = "normal";
                        break;
                    default:
                    case SampleAdditonEnum.Auto:
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
                        if (GameMode != GameModeEnum.Mania) tracks.Add($"{sample}-hitnormal.wav");
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
                    var splitted = s.Split('.');
                    return splitted.Length == 1 ? s : $"{splitted[0]}{track}.{splitted[1]}";
                }).ToArray();
            }
        }
    }
}
