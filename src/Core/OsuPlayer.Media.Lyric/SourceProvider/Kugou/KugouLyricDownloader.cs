using System;
using System.IO;
using System.Net;
using System.Text.Json.Nodes;

namespace OsuPlayer.Media.Lyric.SourceProvider.Kugou;

public class KugouLyricDownloader : LyricDownloaderBase
{
    public static readonly string API_URL = @"http://www.kugou.com/yy/index.php?r=play/getdata&hash={0}";

    public override string DownloadLyric(SearchSongResultBase song, bool requestTransLyrics = false)
    {
        //没支持翻译歌词的
        if (requestTransLyrics)
            return string.Empty;

        Uri url = new Uri(string.Format(API_URL, song.ID));

        HttpWebRequest request = WebRequest.CreateHttp(url);
        request.Timeout = Settings.SearchAndDownloadTimeout;

        var response = request.GetResponse();

        string content;

        using (var reader = new StreamReader(response.GetResponseStream()))
        {
            content = reader.ReadToEnd();
        }

        var obj = JsonNode.Parse(content);
        if (obj["err_code"].GetValue<int>() != 0)
            return null;
        var rawLyric = obj["data"]["lyrics"].GetValue<string>();
        var lyrics = rawLyric.Replace("\r\n", "\n");

        return lyrics;
    }
}