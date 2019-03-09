using Milky.OsuPlayer.Media.Audio;
using OSharp.Beatmap;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Instances
{
    public class PlayersInst
    {
        public ComponentPlayer AudioPlayer { get; private set; }

        public async Task LoadAudioPlayerAsync(string filePath, OsuFile osuFile)
        {
            AudioPlayer = await ComponentPlayer.InitializeAsync(filePath, osuFile);
        }

        public void ClearAudioPlayer()
        {
            AudioPlayer?.Stop();
            AudioPlayer?.Dispose();
            AudioPlayer = null;
        }
    }
}
