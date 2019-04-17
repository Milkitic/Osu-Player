using System.Collections.Generic;
using System.IO;
using System.Linq;
using Milky.OsuPlayer.Common;
using OSharp.Beatmap.Sections.GamePlay;
using OSharp.Beatmap.Sections.HitObject;
using OSharp.Beatmap.Sections.Timing;

namespace Milky.OsuPlayer.Media.Audio
{
    public class HitsoundElement
    {
        private readonly string _mapFolderName;
        private readonly string[] _mapWaveFiles;
        private string[] _fileNamesWithoutTrack;
        private string _extension = ".wav";

        public HitsoundElement(
            string mapFolderName,
            string[] mapWaveFiles,
            GameMode gameMode,
            double offset,
            int track,
            TimingSamplesetType lineSample,
            HitsoundType hitsound,
            ObjectSamplesetType sample,
            ObjectSamplesetType addition,
            string customFile,
            float volume)
        {
            _mapFolderName = mapFolderName;
            _mapWaveFiles = mapWaveFiles;
            GameMode = gameMode;
            Offset = offset;
            Track = track;
            LineSample = lineSample;
            Hitsound = hitsound;
            Sample = sample;
            Addition = addition;
            CustomFile = customFile;
            Volume = volume;

            SetNamesWithoutTrack();
            SetFullPath();
        }

        public GameMode GameMode { get; }
        public double Offset { get; }
        public float Volume { get; }
        public HitsoundType Hitsound { get; }
        public int Track { get; }
        public TimingSamplesetType LineSample { get; }
        public ObjectSamplesetType Sample { get; }
        public ObjectSamplesetType Addition { get; }
        public string CustomFile { get; }

        public string[] FilePaths { get; private set; }

        private void SetFullPath()
        {
            FilePaths = new string[_fileNamesWithoutTrack.Length];
            for (var i = 0; i < _fileNamesWithoutTrack.Length; i++)
            {
                var name = _fileNamesWithoutTrack[i] + (Track > 1 && string.IsNullOrWhiteSpace(CustomFile) ? Track.ToString() : "");
                if (_mapWaveFiles.Contains(name))
                {
                    FilePaths[i] = Path.Combine(_mapFolderName, name + _extension);
                }
                else if (!string.IsNullOrWhiteSpace(CustomFile))
                {
                    FilePaths[i] = Path.Combine(_mapFolderName, name);
                }
                else
                {
                    FilePaths[i] = Path.Combine(Domain.DefaultPath, _fileNamesWithoutTrack[i] + _extension);
                }
            }
        }

        private void SetNamesWithoutTrack()
        {
            var tracks = new List<string>();
            if (!string.IsNullOrEmpty(CustomFile))
            {
                tracks.Add(CustomFile);
                _fileNamesWithoutTrack = tracks.ToArray();
                return;
            }

            string sample = GetFromLineSample();
            AdjustObjectSample(ref sample);
            string addition = GetObjectAddition(sample);

            if (Hitsound == 0)
                tracks.Add($"{sample}-hitnormal");
            else
            {
                if (Hitsound.HasFlag(HitsoundType.Whistle))
                    tracks.Add($"{addition}-hitwhistle");
                if (Hitsound.HasFlag(HitsoundType.Clap))
                    tracks.Add($"{addition}-hitclap");
                if (Hitsound.HasFlag(HitsoundType.Finish))
                    tracks.Add($"{addition}-hitfinish");
                if (Hitsound.HasFlag(HitsoundType.Normal) ||
                    (Hitsound & HitsoundType.Normal) == 0)
                {
                    if (GameMode != GameMode.Mania)
                        tracks.Add($"{sample}-hitnormal");
                }
            }

            _fileNamesWithoutTrack = tracks.ToArray();
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
    }
}
