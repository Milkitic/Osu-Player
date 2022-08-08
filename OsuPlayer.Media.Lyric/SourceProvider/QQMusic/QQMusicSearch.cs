using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Milki.OsuPlayer.Media.Lyric.SourceProvider.QQMusic
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

        public override string Title { get => title; }
        public string title { get; set; }
        //public int id { get; set; }
        public string mid { get; set; }
        public override string ID { get => /*id.ToString();*/mid; }

        /// <summary>
        /// 如果没有这个，一些歌的ID会下载到其他歌的歌词，比如 Ooi (Game edit)
        /// 获取歌词的时候的querystring是"songtype={type}"
        /// </summary>
        public string type { get; set; }

        //建议qq音乐那边声明这个玩意的人跟我一起重学英语,谢谢
        /// <summary>
        /// duration
        /// </summary>
        public int interval { get; set; }

        public override string Artist { get => singer?.First().name ?? null; }
        public override int Duration { get => interval * 1000; }

        public override string ToString()
        {
            return $"({ID}){Artist} - {title} ({interval / 60}:{interval % 60})";
        }
    }

    #endregion

    public class QQMusicSearch : SongSearchBase<Song>
    {
        private static readonly string API_URL = @"http://c.y.qq.com/soso/fcgi-bin/client_search_cp?ct=24&qqmusic_ver=1298&new_json=1&remoteplace=txt.yqq.song&t=0&aggr=1&cr=1&catZhida=1&lossless=0&flag_qc=0&p=1&n=20&w={0} {1}&g_tk=5381&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq&needNewCode=0";

        public override List<Song> Search(params string[] args)
        {
            string title = args[0], artist = args[1];
            Uri url = new Uri(string.Format(API_URL, artist, title));

            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.Timeout = Settings.SearchAndDownloadTimeout;

            var response = request.GetResponse();

            string content;

            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                content = reader.ReadToEnd();
            }

            var json = JObject.Parse(content);
            var arr = json["data"]["song"]["list"];

            var songs = (arr.ToObject<List<Song>>());

            return songs;
        }
    }

}
