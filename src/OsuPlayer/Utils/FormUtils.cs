using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Milki.OsuPlayer.Audio;
using Milki.OsuPlayer.Data.Models;
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
