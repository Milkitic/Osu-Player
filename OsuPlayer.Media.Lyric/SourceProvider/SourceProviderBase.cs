using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Milky.OsuPlayer.Media.Lyric.Models;

namespace Milky.OsuPlayer.Media.Lyric.SourceProvider
{
    public abstract class SourceProviderBase
    {
        public abstract Lyrics ProvideLyric(string artist, string title, int time, bool requestTransLyrics);
    }

    public abstract class SourceProviderBase<TSearchResult, TSearcher, TDownloader, TParser> : SourceProviderBase
        where TDownloader : LyricDownloaderBase, new()
        where TParser : LyricParserBase, new()
        where TSearcher : SongSearchBase<TSearchResult>, new()
        where TSearchResult : SearchSongResultBase, new()
    {
        public int DurationThresholdValue { get; set; } = 1000;

        public TDownloader Downloader { get; } = new TDownloader();
        public TSearcher Seadrcher { get; } = new TSearcher();
        public TParser Parser { get; } = new TParser();

        public override Lyrics ProvideLyric(string artist, string title, int time, bool requestTransLyrics)
        {
            try
            {
                var searchResult = Seadrcher.Search(artist, title);

                var lyrics = PickLyric(artist, title, time, searchResult, requestTransLyrics, out TSearchResult pickedResult);

                if (lyrics != null && Settings.EnableOutputSearchResult)
                {
                    //output lyrics search result
                    var contentObj = new { DateTime = DateTime.Now, pickedResult.ID, pickedResult.Artist, pickedResult.Title, pickedResult.Duration, Raw_Title = title, Raw_Artist = artist, Raw_Duration = time };
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(contentObj, Newtonsoft.Json.Formatting.None);
                    if (!Directory.Exists(@"..\lyric_cache"))
                        Directory.CreateDirectory(@"..\lyric_cache");
                    string filePath = $@"..\lyric_cache\{this.GetType().Name}.txt";
                    File.AppendAllText(filePath, json + Environment.NewLine, Encoding.UTF8);

                    Utils.Output($"-> 从{this.GetType().Name}获取到 {(lyrics.IsTranslatedLyrics ? "翻译歌词" : "原歌词")}", ConsoleColor.Green);
                }

                return lyrics;
            }
            catch (Exception e)
            {
                Utils.Output($"{GetType().Name}获取歌词失败:{e.Message}", ConsoleColor.Red);
                return null;
            }
        }

        public virtual Lyrics PickLyric(string artist, string title, int time, List<TSearchResult> searchResult, bool requestTransLyrics, out TSearchResult pickedResult)
        {
            pickedResult = null;

            DumpSearchList("-", time, searchResult);

            FuckSearchFilte(artist, title, time, ref searchResult);

            DumpSearchList("+", time, searchResult);

            if (searchResult.Count == 0)
                return null;

            Lyrics lyricCont = null;
            TSearchResult curResult = null;

            foreach (var result in searchResult)
            {
                var content = Downloader.DownloadLyric(result, requestTransLyrics);
                curResult = result;

                if (string.IsNullOrWhiteSpace(content))
                    continue;

                lyricCont = Parser.Parse(content);

                //过滤没有实质歌词内容的玩意,比如没有时间轴的歌词文本
                if (lyricCont?.LyricSentencs?.Count == 0)
                    continue;

                Utils.Debug($"* Picked music_id:{result.ID} artist:{result.Artist} title:{result.Title}");
                break;
            }

            if (lyricCont == null)
                return null;

            pickedResult = curResult;

            WrapInfo(lyricCont);

            return lyricCont;

            #region Wrap Methods

            //封装信息
            void WrapInfo(Lyrics l)
            {
                if (l == null)
                    return;

                Info rawInfo = new Info()
                {
                    Artist = artist,
                    Title = title
                }, queryInfo = new Info()
                {
                    Artist = curResult.Artist,
                    Title = curResult.Title,
                    ID = curResult.ID
                };

                l.RawInfo = rawInfo;
                l.QueryInfo = queryInfo;
                l.IsTranslatedLyrics = requestTransLyrics;
            }

            #endregion
        }

        private static void DumpSearchList(string prefix, int time, List<TSearchResult> searchList)
        {
            if (Settings.DebugMode)
                foreach (var r in searchList)
                    Utils.Debug($"{prefix} music_id:{r.ID} artist:{r.Artist} title:{r.Title} time{r.Duration}({Math.Abs(r.Duration - time):F2})");
        }

        public virtual void FuckSearchFilte(string artist, string title, int time, ref List<TSearchResult> searchResult)
        {
            //删除长度不对的
            searchResult.RemoveAll((r) => Math.Abs(r.Duration - time) > DurationThresholdValue);

            string checkStr = $"{title.Trim()}";

            if (Settings.StrictMatch)
            {
                //删除标题看起来不匹配的(超过1/3内容不对就出局)，当然开头相同除外
                float threholdLength = checkStr.Length * (1.0f / 3);
                searchResult.RemoveAll((r) =>
                {
                    //XXXX和XXXXX(Full version)这种情况可以跳过
                    if (r.Title.Trim().StartsWith(checkStr))
                        return false; //不用删除，通过

                    var distance = GetEditDistance(r);
                    return distance > threholdLength;
                }
                );
            }

            //search_result.Sort((a, b) => Math.Abs(a.Duration - time) - Math.Abs(b.Duration - time));
            searchResult.Sort((a, b) => GetEditDistance(a) - GetEditDistance(b));

            int GetEditDistance(SearchSongResultBase s)
            {
                return Utils.EditDistance($"{s.Title}", checkStr);
            }
        }
    }
}
