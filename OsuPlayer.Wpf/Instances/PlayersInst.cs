using Milky.OsuPlayer.Media.Audio;
using OSharp.Beatmap;
using System.Linq;
using System.Text;

namespace Milky.OsuPlayer.Instances
{
    public class PlayersInst
    {
        public ComponentPlayer AudioPlayer { get; private set; }

        public void SetAudioPlayer(string filePath, OsuFile osuFile)
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
