using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Milkitic.OsuPlayer.Media.Lyric.SourcePrivoder.Base;
using Newtonsoft.Json.Linq;

namespace Milkitic.OsuPlayer.Media.Lyric.SourcePrivoder.Kugou
{
    public class KugouSearchResultSong : SearchSongResultBase
    {
        public int duration { get; set; }
        public string singername { get; set; }
        public string songname { get; set; }
        public string hash { get; set; } // 获取歌词需要这个玩意,所以拿着个当ID吧

        public override string Artist => singername;
        public override string Title => songname;
        public override int Duration => duration * 1000;
        public override string ResultId => hash;
    }

    public class KugouSearcher : SongSearchBase<KugouSearchResultSong>
    {
        private const string ApiUrl = @"http://mobilecdn.kugou.com/api/v3/search/song?format=json&keyword={1} {0}&page=1&pagesize=20&showtype=1";

        public override List<KugouSearchResultSong> Search(params string[] paramArr)
        {
            string title = paramArr[0], artist = paramArr[1];
            Uri url = new Uri(string.Format(ApiUrl, artist, title));

            //这纸张酷狗有时候response不回来,但用浏览器就可以.先留校观察
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.Method = "GET";
            request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.140 Safari/537.36";
            request.Timeout = Settings.SearchDownloadTimeout;
            var response = request.GetResponse();
            var stream = response.GetResponseStream();

            string content;
            if (stream != null)
            {
                using (var reader = new StreamReader(stream))
                {
                    content = reader.ReadToEnd();
                }
            }
            else throw new NullReferenceException();

            var json = JObject.Parse(content);
            if (!string.IsNullOrWhiteSpace(json["error"].ToString()))
                return new List<KugouSearchResultSong>();

            return json["data"]["info"].ToObject<List<KugouSearchResultSong>>();
        }
    }
}
