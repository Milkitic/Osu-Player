using Milki.OsuPlayer.Audio;
using Milki.OsuPlayer.Services;

namespace Milki.OsuPlayer.Utils
{
    public static class FormUtils
    {
        public static async ValueTask ReplacePlayListAndPlayAll(IEnumerable<string> standardizedPaths,
            PlayListService playListService, PlayerService playerServices)
        {
            playListService.SetPathList(standardizedPaths, false);
            await playerServices.PlayNextAsync();
        }
    }
}
