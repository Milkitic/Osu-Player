using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Milky.OsuPlayer.Common;

namespace Milky.OsuPlayer.Media.Lyric.SourceProvider
{
    public abstract class SourceProviderBase
    {
        protected readonly bool StrictMode;

        protected SourceProviderBase(bool strictMode)
        {
            StrictMode = strictMode;
        }

        public abstract Lyric ProvideLyric(string artist, string title, int time, bool requestTransLyrics);
    }

    public abstract class SourceProviderBase<TSearchresult, TSearcher, TDownloader, TParser> : SourceProviderBase
        where TDownloader : LyricDownloaderBase, new()
        where TParser : LyricParserBase, new()
        where TSearcher : SongSearchBase<TSearchresult>, new()
        where TSearchresult : SearchSongResultBase, new()
    {
        public int DurationThresholdValue { get; set; } = 1000;
        public TDownloader Downloader { get; } = new TDownloader();
        public TSearcher Seacher { get; } = new TSearcher();
        public TParser Parser { get; } = new TParser();

        public override Lyric ProvideLyric(string artist, string title, int time, bool requestTransLyrics)
        {
            try
            {
                List<TSearchresult> searchResult = Seacher.Search(artist, title);

                var lyrics = PickLyric(artist, title, time, searchResult, requestTransLyrics, out TSearchresult pickedResult);

                if (lyrics != null && SearchSettings.EnableOutputSearchResult)
                {
                    //output lyrics search result
                    var contentObj = new
                    {
                        DateTime = DateTime.Now,
                        ID = pickedResult.ResultId,
                        pickedResult.Artist,
                        pickedResult.Title,
                        pickedResult.Duration,
                        Raw_Title = title,
                        Raw_Artist = artist,
                        Raw_Duration = time
                    };
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(contentObj, Newtonsoft.Json.Formatting.None);
                    if (!Directory.Exists(Domain.LyricCachePath))
                        Directory.CreateDirectory(Domain.LyricCachePath);
                    string filePath = Path.Combine(Domain.LyricCachePath, $"{GetType().Name}.txt");
                    File.AppendAllText(filePath, json + Environment.NewLine, Encoding.UTF8);

                    Console.WriteLine(@"-> 从{0}获取到 {1}", GetType().Name, lyrics.IsTranslatedLyrics ? "翻译歌词" : "原歌词");
                }

                return lyrics;
            }
            catch (Exception e)
            {
                return null;
                //throw new Exception($"{GetType().Name}获取歌词失败。", e);
            }
        }

        public virtual Lyric PickLyric(string artist, string title, int time, List<TSearchresult> searchResult,
            bool requestTransLyrics, out TSearchresult pickedResult)
        {
            pickedResult = null;

            DumpSearchList("-", time, searchResult);
            FuckSearchFilte(artist, title, time, ref searchResult);
            DumpSearchList("+", time, searchResult);

            if (searchResult.Count == 0)
                return null;

            Lyric lyric = null;
            TSearchresult curResult = null;

            foreach (var result in searchResult)
            {
                var content = Downloader.DownloadLyric(result, requestTransLyrics);
                curResult = result;
                if (string.IsNullOrWhiteSpace(content))
                    continue;

                lyric = Parser.Parse(content);
                //过滤没有实质歌词内容的玩意,比如没有时间轴的歌词文本
                if (lyric?.LyricSentencs?.Count == 0)
                    continue;

                break;
            }

            if (lyric == null)
                return null;

            pickedResult = curResult;
            WrapInfo(lyric);
            return lyric;

            #region Wrap Methods

            //封装信息
            void WrapInfo(Lyric l)
            {
                if (l == null)
                    return;

                Info rawInfo = new Info
                {
                    Artist = artist,
                    Title = title
                }, queryInfo = new Info
                {
                    Artist = curResult.Artist,
                    Title = curResult.Title,
                    Id = curResult.ResultId
                };

                l.RawInfo = rawInfo;
                l.QueryInfo = queryInfo;
                l.IsTranslatedLyrics = requestTransLyrics;
            }

            #endregion
        }

        private static void DumpSearchList(string prefix, int time, List<TSearchresult> searchList)
        {
            foreach (var r in searchList)
                Console.WriteLine(@"{0} music_id:{1} artist:{2} title:{3} time{4}({5:F2})", prefix, r.ResultId,
                    r.Artist, r.Title, r.Duration, Math.Abs(r.Duration - time));
        }

        public virtual void FuckSearchFilte(string artist, string title, int time, ref List<TSearchresult> searchResult)
        {
            //删除长度不对的
            searchResult.RemoveAll(r => Math.Abs(r.Duration - time) > DurationThresholdValue);

            string checkStr = $"{title.Trim()}";

            if (StrictMode) //(Setting.StrictMatch)
            {
                //删除标题看起来不匹配的(超过1/3内容不对就出局)，当然开头相同除外
                float threholdLength = checkStr.Length * (1.0f / 3);
                searchResult.RemoveAll((r) =>
                {
                    //XXXX和XXXXX(Full version)这种情况可以跳过
                    if (r.Title.Trim().StartsWith(checkStr))
                        return false;//不用删除，通过

                    var distance = GetEditDistance(r);
                    return distance > threholdLength;
                }
                );
            }

            searchResult.Sort((a, b) => GetEditDistance(a) - GetEditDistance(b));

            int GetEditDistance(SearchSongResultBase s) => StringUtils.GetEditDistance($"{s.Title}", checkStr);
        }

        protected SourceProviderBase(bool strictMode) : base(strictMode)
        {
        }
    }
}
