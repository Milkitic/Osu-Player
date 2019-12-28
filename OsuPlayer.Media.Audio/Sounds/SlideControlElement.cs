using System.Collections.Generic;
using System.IO;
using Milky.OsuPlayer.Common;
using OSharp.Beatmap.Sections.HitObject;
using OSharp.Beatmap.Sections.Timing;

namespace Milky.OsuPlayer.Media.Audio.Sounds
{
    public sealed class SlideControlElement : SoundElement
    {
        private readonly string _mapFolderName;
        private readonly HashSet<string> _mapWaveFiles;

        public SlideControlElement(string mapFolderName,
            HashSet<string> mapWaveFiles,
            int offset,
            float volume,
            float balance,
            int track,
            TimingSamplesetType lineSample,
            ObjectSamplesetType sample,
            ObjectSamplesetType addition,
            int forceTrack,
            SlideControlMode controlMode,
            bool isAddition)
        {
            _mapFolderName = mapFolderName;
            _mapWaveFiles = mapWaveFiles;
            Offset = offset;
            Volume = volume;
            Balance = balance;
            Track = track;
            LineSample = lineSample;
            Sample = sample;
            Addition = addition;
            ForceTrack = forceTrack;
            ControlMode = controlMode;
            IsAddition = isAddition;
            SetNamesWithoutTrack();
            SetFullPath();
        }

        public override double Offset { get; protected set; }
        public override float Volume { get; protected set; }
        public override float Balance { get; protected set; }
        public override string[] FilePaths { get; protected set; }

        public int Track { get; set; }

        public TimingSamplesetType LineSample { get; set; }

        public ObjectSamplesetType Sample { get; set; }
        public ObjectSamplesetType Addition { get; set; }
        public int ForceTrack { get; set; }

        public bool IsAddition { get; }
        public SlideControlMode ControlMode { get; set; }

        private string _fileNameWithoutTrack;

        private void SetNamesWithoutTrack()
        {
            string sample = GetFromLineSample();
            AdjustObjectSample(ref sample);
            if (IsAddition && Addition != ObjectSamplesetType.Auto)
            {
                switch (Addition)
                {
                    case ObjectSamplesetType.Normal:
                        sample = "normal";
                        break;
                    case ObjectSamplesetType.Soft:
                        sample = "soft";
                        break;
                    case ObjectSamplesetType.Drum:
                        sample = "drum";
                        break;
                }
            }
            _fileNameWithoutTrack = $"{sample}-slider{(IsAddition ? "whistle" : "slide")}";
        }

        private void SetFullPath()
        {
            string trackStr;

            if (ForceTrack > 0)
            {
                trackStr = (ForceTrack > 1 ? ForceTrack.ToString() : "");
            }
            else
            {
                trackStr = (Track > 1 ? Track.ToString() : "");
            }
            var name = _fileNameWithoutTrack + trackStr;
            if (Track == 0)
            {
                FilePaths = new[] { Path.Combine(Domain.DefaultPath, _fileNameWithoutTrack + WavExtension) };
            }
            else if (_mapWaveFiles.Contains(name))
            {
                var path = Path.Combine(_mapFolderName, name + WavExtension);
                if (!File.Exists(path))
                {
                    path = Path.Combine(_mapFolderName, name + OggExtension);
                }

                FilePaths = new[] { path };
            }
            //else if (!string.IsNullOrWhiteSpace(CustomFile))
            //{
            //    FilePaths = new[] { Path.Combine(_mapFolderName, name) };
            //}
            else
            {
                FilePaths = new[] { Path.Combine(Domain.DefaultPath, _fileNameWithoutTrack + WavExtension) };
            }

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

    public enum SlideControlMode
    {
        NewSample, ChangeBalance, Stop,
        Volume
    }
}