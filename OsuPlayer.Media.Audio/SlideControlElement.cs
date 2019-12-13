using System.IO;
using Milky.OsuPlayer.Common;
using OSharp.Beatmap.Sections.HitObject;
using OSharp.Beatmap.Sections.Timing;

namespace Milky.OsuPlayer.Media.Audio
{
    public class SlideControlElement : ISoundElement
    {
        public SlideControlElement(double offset, float volume, float balance, string[] filePaths, SlideControlMode controlMode,
            bool isAddition)
        {
            Offset = offset;
            Volume = volume;
            Balance = balance;
            FilePaths = filePaths;
            ControlMode = controlMode;
            IsAddition = isAddition;
        }

        public SlideControlElement(int offset,
            float volume,
            float balance,
            int track,
            TimingSamplesetType lineSample,
            ObjectSamplesetType addition,
            int forceTrack,
            SlideControlMode controlMode,
            bool isAddition)
        {
            Offset = offset;
            Volume = volume;
            Balance = balance;
            Track = track;
            LineSample = lineSample;
            Addition = addition;
            ForceTrack = forceTrack;
            ControlMode = controlMode;
            IsAddition = isAddition;
        }

        public double Offset { get; }
        public float Volume { get; }
        public float Balance { get; }
        public string[] FilePaths
        {
            get { return new[] { Path.Combine(Domain.DefaultPath, "soft-sliderslide.wav") }; }
            private set { }
        }

        public int Track { get; set; }

        public TimingSamplesetType LineSample { get; set; }
        public ObjectSamplesetType Addition { get; set; }
        public int ForceTrack { get; set; }

        public bool IsAddition { get; }
        public SlideControlMode ControlMode { get; }
    }

    public enum SlideControlMode
    {
        NewSample, ChangeBalance, Stop
    }
}