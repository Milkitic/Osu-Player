using System.Collections.Generic;
using System.Linq;
using Milky.OsuPlayer.Media.Lyric.Models;

namespace Milky.OsuPlayer.Media.Lyric.SourceProvider.QQMusic
{
    [SourceProviderName("qqmusic", "DarkProjector")]
    public class QQMusicSourceProvider : SourceProviderBase<Song, QQMusicSearch, QQMusicLyricDownloader, QQMusicLyricParser>
    {
        public override Lyrics PickLyric(string artist, string title, int time, List<Song> searchResult, bool requestTransLyrics, out Song picked_result)
        {
            picked_result = null;

            Lyrics result = base.PickLyric(artist, title, time, searchResult, requestTransLyrics, out Song tempPickedResult);

            if (result != null)
            {
                switch (result.LyricSentencs.Count)
                {
                    case 0:
                        Utils.Debug($"{picked_result?.ID}:无任何歌词在里面,rej");
                        return null;

                    case 1:
                        var firstSentence = result.LyricSentencs.First();
                        if (firstSentence.StartTime <= 0 && firstSentence.Content.Contains("纯音乐") && firstSentence.Content.Contains("没有填词"))
                        {
                            Utils.Debug($"{picked_result?.ID}:纯音乐? : " + firstSentence);
                            return null;
                        }
                        break;

                    default:
                        break;
                }
            }

            picked_result = tempPickedResult;
            return result;
        }
    }
}
