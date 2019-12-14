using System.Collections.Generic;
using System.IO;
using Milky.OsuPlayer.Common.Configuration;
using OSharp.Beatmap;

namespace Milky.OsuPlayer.Media.Audio.Music
{
    internal class SampleTrackPlayer : HitsoundPlayer
    {
        protected override string Flag { get; } = "SampleTrack";

        public SampleTrackPlayer(string filePath, OsuFile osuFile) : base(filePath, osuFile)
        {
        }
        protected override void InitVolume()
        {
            Engine.Volume = 1f * AppSettings.Default.Volume.Sample * AppSettings.Default.Volume.Main;
        }

        protected override void Volume_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Engine.Volume = 1f * AppSettings.Default.Volume.Sample * AppSettings.Default.Volume.Main;
        }

        protected override List<SoundElement> FillHitsoundList(OsuFile osuFile, DirectoryInfo dirInfo)
        {
            List<SoundElement> hitsoundList = new List<SoundElement>();
            var sampleList = osuFile.Events.SampleInfo;
            if (sampleList == null)
                return hitsoundList;
            foreach (var sampleData in sampleList)
            {
                var element = new HitsoundElement(
                    mapFolderName: dirInfo.FullName,
                    mapWaveFiles: new HashSet<string>(),
                    gameMode: osuFile.General.Mode,
                    offset: sampleData.Offset,
                    track: -1,
                    lineSample: OSharp.Beatmap.Sections.Timing.TimingSamplesetType.None,
                    hitsound: OSharp.Beatmap.Sections.HitObject.HitsoundType.Normal,
                    sample: OSharp.Beatmap.Sections.HitObject.ObjectSamplesetType.Auto,
                    addition: OSharp.Beatmap.Sections.HitObject.ObjectSamplesetType.Auto,
                    customFile: sampleData.Filename,
                    volume: sampleData.Volume / 100f,
                    balance: 0,
                    forceTrack: 0,
                    fullHitsoundType: null
                );

                hitsoundList.Add(element);
            }

            return hitsoundList;
        }
    }
}