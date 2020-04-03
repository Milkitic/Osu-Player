using Milky.OsuPlayer.Media.Audio.Player.Subchannels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Media.Audio.Player
{
    public interface ISoundElementsProvider
    {
        Task<IEnumerable<SoundElement>> GetSoundElements();
    }
}
