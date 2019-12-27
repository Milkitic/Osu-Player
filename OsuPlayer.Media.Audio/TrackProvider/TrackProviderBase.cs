using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSharp.Beatmap;

namespace Milky.OsuPlayer.Media.Audio.TrackProvider
{
    public abstract class TrackProviderBase
    {
        protected OsuFile OsuFile { get; private set; }

        public TrackProviderBase(OsuFile osuFile)
        {
            OsuFile = osuFile;
        }

        public abstract void GetSoundElements();
    }
}
