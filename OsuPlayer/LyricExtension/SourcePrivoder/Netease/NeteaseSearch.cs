using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Milkitic.OsuPlayer.LyricExtension.SourcePrivoder.Base;
using Newtonsoft.Json.Linq;

namespace Milkitic.OsuPlayer.LyricExtension.SourcePrivoder.Netease
{
    public class NeteaseSearch : SongSearchBase<NeteaseSearch.Song>
    {
        #region Search Result
        public class Artist
        {
            public List<string> alias { get; set; }
            public string picUrl { get; set; }
            public int id { get; set; }
            public string name { get; set; }
        }

        public class Album
        {
            public int status { get; set; }
            public int copyrightId { get; set; }
            public string name { get; set; }
            public Artist artist { get; set; }
            public ulong publishTime { get; set; }
            public int id { get; set; }
            public int size { get; set; }
        }

        public class Song : SearchSongResultBase
        {
            public Album album { get; set; }
            public int status { get; set; }
            public int copyrightId { get; set; }
            public string name { get; set; }
            public int mvid { get; set; }
            public List<string> Alias { get; set; }
            public List<Artist> artists { get; set; }
            public int duration { get; set; }
            public int id { get; set; }

            public override string Artist => artists?.First().name;
            public override string Title => name;
            public override int Duration => duration;
            public override string ResultId => id.ToString();
        }

        #endregion

        private const string ApiUrl = "http://music.163.com/api/search/get/";
        private const int SearchLimit = 5;

        public override List<Song> Search(params string[] paramArr)
        {
            string title = paramArr[0], artist = paramArr[1];
            Uri url = new Uri($"{ApiUrl}?s={artist} {title}&limit={SearchLimit}&type=1&offset=0");

            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.Method = "POST";
            request.Timeout = Settings.SearchDownloadTimeout;
            request.Referer = "http://music.163.com";
            request.Headers["appver"] = "2.0.2";
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

            JObject json = JObject.Parse(content);
            var count = json["result"]["songCount"]?.ToObject<int>();
            return count == 0
                ? new List<Song>()
                : json["result"]["songs"].ToObject<List<Song>>();
        }
    }
}
