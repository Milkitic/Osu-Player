using System.Linq;
using System.Text;
using Milky.OsuPlayer.Media.Audio;
using OSharp.Beatmap;

namespace Milky.OsuPlayer.Instances
{
    public class PlayersInst
    {
        public ComponentPlayer AudioPlayer { get; private set; }

        public void LoadAudioPlayer(string filePath, OsuFile osuFile)
        {
            AudioPlayer = new ComponentPlayer(filePath, osuFile);
        }

        public void ClearAudioPlayer()
        {
            AudioPlayer?.Stop();
            AudioPlayer?.Dispose();
            AudioPlayer = null;
        }
    }
}
