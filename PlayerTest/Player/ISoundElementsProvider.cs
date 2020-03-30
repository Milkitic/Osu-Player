using PlayerTest.Player.Subchannels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlayerTest.Player
{
    public interface ISoundElementsProvider
    {
        Task<IEnumerable<SoundElement>> GetSoundElements();
    }
}
