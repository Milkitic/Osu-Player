using System;
using System.IO;
using System.Net;
using Milkitic.OsuPlayer.Media.Lyric.SourcePrivoder.Base;
using Newtonsoft.Json.Linq;

namespace Milkitic.OsuPlayer.Media.Lyric.SourcePrivoder.Kugou
{
    public class KugouLyricDownloader : LyricDownloaderBase
    {
        private const string ApiUrl = @"http://www.kugou.com/yy/index.php?r=play/getdata&hash={0}";

        public override string DownloadLyric(SearchSongResultBase song, bool requestTransLyrics = false)
        {
            //没支持翻译歌词的
            if (requestTransLyrics)
                return string.Empty;

            Uri url = new Uri(string.Format(ApiUrl, song.ResultId));
            HttpWebRequest request = WebRequest.CreateHttp(url);
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

            JObject obj = JObject.Parse(content);
            if ((int)obj["err_code"] != 0)
                return null;
            var rawLyric = obj["data"]["lyrics"].ToString();
            return rawLyric.Replace("\r\n", "\n");
        }
    }
}
