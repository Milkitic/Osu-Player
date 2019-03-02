using System;
using System.IO;
using System.Net;
using Milkitic.OsuPlayer.LyricExtension.SourcePrivoder.Base;
using Newtonsoft.Json.Linq;

namespace Milkitic.OsuPlayer.LyricExtension.SourcePrivoder.Netease
{
    public class NeteaseLyricDownloader : LyricDownloaderBase
    {
        //tv=-1 是翻译版本的歌词
        //lv=1 是源版本歌词
        private const string LyricApiUrl = "https://music.163.com/api/song/lyric?id={0}&{1}";

        public override string DownloadLyric(SearchSongResultBase song, bool requestTransLyrics = false)
        {
            HttpWebRequest request =
                WebRequest.CreateHttp(string.Format(LyricApiUrl, song.ResultId, requestTransLyrics ? "tv=-1" : "lv=1"));
            request.Timeout = Settings.SearchDownloadTimeout;
            var response = request.GetResponse();
            Stream respStream = response.GetResponseStream();

            string content;
            if (respStream != null)
            {
                using (var reader = new StreamReader(respStream))
                {
                    content = reader.ReadToEnd();
                }
            }
            else throw new NullReferenceException();

            JObject json = JObject.Parse(content);
            return json[requestTransLyrics ? "tlyric" : "lrc"]["lyric"].ToString();
        }
    }
}
