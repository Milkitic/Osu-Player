using System.Collections.Generic;
using System.IO;
using Milky.OsuPlayer.Common;
using OSharp.Beatmap.Sections.GamePlay;
using OSharp.Beatmap.Sections.HitObject;
using OSharp.Beatmap.Sections.Timing;

namespace Milky.OsuPlayer.Media.Audio.Core
{
    public sealed class HitsoundElement : SoundElement
    {
        private readonly string _mapFolderName;
        private readonly HashSet<string> _mapWaveFiles;
        private readonly int _forceTrack;
        private readonly HitsoundType? _fullHitsoundType;
        private string[] _fileNamesWithoutTrack;

        public HitsoundElement(string mapFolderName,
            HashSet<string> mapWaveFiles,
            GameMode gameMode,
            double offset,
            int track,
            TimingSamplesetType lineSample,
            HitsoundType hitsound,
            ObjectSamplesetType sample,
            ObjectSamplesetType addition,
            string customFile,
            float volume,
            float balance,
            int forceTrack,
            HitsoundType? fullHitsoundType)
        {
            _mapFolderName = mapFolderName;
            _mapWaveFiles = mapWaveFiles;
            _forceTrack = forceTrack;
            _fullHitsoundType = fullHitsoundType;
            GameMode = gameMode;
            Offset = offset;
            Track = track;
            LineSample = lineSample;
            Hitsound = hitsound;
            Sample = sample;
            Addition = addition;
            CustomFile = customFile;
            Volume = volume;
            Balance = balance;
            SetNamesWithoutTrack();
            SetFullPath();
        }
        public HitsoundElement(string mapFolderName,
            HashSet<string> mapWaveFiles,
            GameMode gameMode,
            double offset,
            int track,
            TimingSamplesetType lineSample,
            bool isTick,
            ObjectSamplesetType sample,
            ObjectSamplesetType addition,
            float volume,
            float balance,
            int forceTrack,
            HitsoundType? fullHitsoundType)
        {
            _mapFolderName = mapFolderName;
            _mapWaveFiles = mapWaveFiles;
            _forceTrack = forceTrack;
            _fullHitsoundType = fullHitsoundType;
            GameMode = gameMode;
            Offset = offset;
            Track = track;
            LineSample = lineSample;
            IsTick = isTick;
            Sample = sample;
            Addition = addition;
            Volume = volume;
            Balance = balance;
            SetNamesWithoutTrack();
            SetFullPath();
        }

        public bool IsTick { get; set; }

        public GameMode GameMode { get; }
        public HitsoundType Hitsound { get; }
        public int Track { get; }
        public TimingSamplesetType LineSample { get; }
        public ObjectSamplesetType Sample { get; }
        public ObjectSamplesetType Addition { get; }
        public string CustomFile { get; }

        public override double Offset { get; protected set; }
        public override float Volume { get; protected set; }
        public override float Balance { get; protected set; }
        public override string[] FilePaths { get; protected set; }

        private void SetFullPath()
        {
            FilePaths = new string[_fileNamesWithoutTrack.Length];
            for (var i = 0; i < _fileNamesWithoutTrack.Length; i++)
            {
                string trackStr;
                if (!string.IsNullOrWhiteSpace(CustomFile))
                {

                }
                if (_forceTrack > 0)
                {
                    trackStr = (_forceTrack > 1 ? _forceTrack.ToString() : "");
                }
                else
                {
                    trackStr = (Track > 1 ? Track.ToString() : "");
                }
                var name = _fileNamesWithoutTrack[i] + trackStr;
                if (Track == 0 && string.IsNullOrWhiteSpace(CustomFile))
                {
                    FilePaths = new[] { Path.Combine(Domain.DefaultPath, _fileNamesWithoutTrack[i] + WavExtension) };
                }
                else if (_mapWaveFiles.Contains(name))
                {
                    var path = Path.Combine(_mapFolderName, name + WavExtension);
                    if (!File.Exists(path))
                    {
                        path = Path.Combine(_mapFolderName, name + OggExtension);
                    }

                    FilePaths[i] = path;
                }
                else if (!string.IsNullOrWhiteSpace(CustomFile))
                {
                    FilePaths[i] = Path.Combine(_mapFolderName, name);
                }
                else
                {
                    FilePaths[i] = Path.Combine(Domain.DefaultPath, _fileNamesWithoutTrack[i] + WavExtension);
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
            string addition;

            addition = GetObjectAddition(sample);
            if (IsTick)
            {
                tracks.Add($"{sample}-slidertick");
            }
            else if (_fullHitsoundType == null)
            {
                if (Hitsound == 0)
                    tracks.Add($"{sample}-hitnormal");
                else
                {
                    AddToTrack(Hitsound);
                }
            }
            else
            {
                AddToTrack(_fullHitsoundType.Value);
            }

            _fileNamesWithoutTrack = tracks.ToArray();

            void AddToTrack(HitsoundType type)
            {
                if (type.HasFlag(HitsoundType.Whistle))
                    tracks.Add($"{addition}-hitwhistle");
                if (type.HasFlag(HitsoundType.Clap))
                    tracks.Add($"{addition}-hitclap");
                if (type.HasFlag(HitsoundType.Finish))
                    tracks.Add($"{addition}-hitfinish");
                if (type.HasFlag(HitsoundType.Normal) ||
                    (type & HitsoundType.Normal) == 0)
                {
                    if (GameMode != GameMode.Mania)
                    {
                        tracks.Add($"{sample}-hitnormal");
                    }
                }
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
    }
}
