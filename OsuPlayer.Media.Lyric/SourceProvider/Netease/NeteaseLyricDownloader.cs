using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Milki.OsuPlayer.Media.Lyric.SourceProvider.Netease
{
    public class NeteaseLyricDownloader : LyricDownloaderBase
    {
        //tv=-1 是翻译版本的歌词
        //lv=1 是源版本歌词
        private static readonly string LYRIC_API_URL = "https://music.163.com/api/song/lyric?id={0}&{1}";

        public override string DownloadLyric(SearchSongResultBase song, bool requestTransLyrics = false)
        {
            HttpWebRequest request = WebRequest.CreateHttp(string.Format(LYRIC_API_URL, song.ID, requestTransLyrics ? "tv=-1" : "lv=1"));
            request.Timeout = Settings.SearchAndDownloadTimeout;

            var response = request.GetResponse();

            string content;

            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                content = reader.ReadToEnd();
            }

            JObject json = JObject.Parse(content);

            return json[requestTransLyrics ? "tlyric" : "lrc"]["lyric"].ToString();
        }
    }
}
