using System;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Milky.OsuPlayer.Media.Lyric.SourceProvider.QQMusic
{

    public class QqMusicLyricDownloader : LyricDownloaderBase
    {
        private const string NewApiUrl = @"https://c.y.qq.com/lyric/fcgi-bin/fcg_query_lyric_new.fcg?g_tk=753738303&songmid={0}&callback=json&songtype={1}";

        public override string DownloadLyric(SearchSongResultBase song, bool requestTransLyrics = false)
        {
            string songType = (song as Song)?.type ?? "0";

            Uri url = new Uri(string.Format(NewApiUrl, song.ResultId, songType));
            HttpWebRequest request = WebRequest.CreateHttp(url);

            request.Timeout = SearchSettings.SearchDownloadTimeout;
            request.Referer = "https://y.qq.com/portal/player.html";
            request.Headers.Add("Cookie", "skey=@LVJPZmJUX; p");

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

            if (content.StartsWith("json("))
                content = content.Remove(0, 5);
            if (content.EndsWith(")"))
                content = content.Remove(content.Length - 1);

            content = System.Web.HttpUtility.HtmlDecode(content);
            JObject json = JObject.Parse(content);

            int result = json["retcode"].ToObject<int>();
            if (result < 0)
                return null;

            content = json[requestTransLyrics ? "trans" : "lyric"]?.ToString();
            if (string.IsNullOrWhiteSpace(content))
                return null;

            content = EncodingUtils.Base64Decode(content);

            return content;
        }
    }
}
