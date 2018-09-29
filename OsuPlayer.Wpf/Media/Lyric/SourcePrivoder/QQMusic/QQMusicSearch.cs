using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Milkitic.OsuPlayer.Media.Lyric.SourcePrivoder.Base;
using Newtonsoft.Json.Linq;

namespace Milkitic.OsuPlayer.Media.Lyric.SourcePrivoder.QQMusic
{
    #region JSON

    public struct Singer
    {
        public string name { get; set; }
        public string title { get; set; }
    }

    public class Song : SearchSongResultBase
    {
        public List<Singer> singer { get; set; }
        public string title { get; set; }
        public string mid { get; set; }
        // 如果没有这个，一些歌的ID会下载到其他歌的歌词，比如 Ooi (Game edit)。获取歌词的时候的querystring是"songtype={type}"
        public string type { get; set; }
        public int interval { get; set; }// duration

        public override string Artist => singer?.First().name;
        public override string Title => title;
        public override int Duration => interval * 1000;
        public override string ResultId => mid;


        public override string ToString()
        {
            return $"({ResultId}){Artist} - {title} ({interval / 60}:{interval % 60})";
        }
    }

    #endregion

    public class QqMusicSearch : SongSearchBase<Song>
    {
        private const string ApiUrl = @"http://c.y.qq.com/soso/fcgi-bin/client_search_cp?ct=24&qqmusic_ver=1298&new_json=1&remoteplace=txt.yqq.song&t=0&aggr=1&cr=1&catZhida=1&lossless=0&flag_qc=0&p=1&n=20&w={0} {1}&g_tk=5381&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq&needNewCode=0";

        public override List<Song> Search(params string[] args)
        {
            string title = args[0], artist = args[1];
            Uri url = new Uri(string.Format(ApiUrl, artist, title));

            HttpWebRequest request = WebRequest.CreateHttp(url);
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
            var arr = json["data"]["song"]["list"];

            return arr.ToObject<List<Song>>();
        }
    }

}
