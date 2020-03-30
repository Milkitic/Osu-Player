using System.Collections.Generic;
using System.Threading.Tasks;
using Milky.OsuPlayer.Media.Audio.Player.Subchannels;

namespace Milky.OsuPlayer.Media.Audio.Player
{
    public interface ISoundElementsProvider
    {
        Task<IEnumerable<SoundElement>> GetSoundElements();
    }
}
