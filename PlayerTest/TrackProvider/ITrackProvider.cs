using System.Collections.Generic;

namespace PlayerTest.TrackProvider
{
    public interface ITrackProvider
    {
        IEnumerable<SoundElement> GetSoundElements();
    }
}
