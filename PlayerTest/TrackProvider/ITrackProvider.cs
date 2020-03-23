using System.Collections.Generic;
using PlayerTest.Player.Channel;

namespace PlayerTest.TrackProvider
{
    public interface ITrackProvider
    {

        IEnumerable<SoundElement> GetSoundElements();
    }
}
