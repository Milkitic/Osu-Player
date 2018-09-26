using System.Collections.Generic;
using System.Linq;

namespace Milkitic.OsuPlayer.Wpf.LyricExtension.Model
{
    public class Lyric
    {
        public static Lyric Empty { get; private set; } = new Lyric(new List<Sentence>());

        public List<Sentence> LyricSentencs { get; set; }

        public bool IsTranslatedLyrics { get; set; }

        /// <summary>
        /// 指要搜寻的内容的歌曲信息
        /// </summary>
        public Info RawInfo { get; set; }

        /// <summary>
        /// 指要搜寻的内容的歌曲信息
        /// </summary>
        public Info QueryInfo { get; set; }

        public Lyric() : this(new List<Sentence>()) { }

        public Lyric(IEnumerable<Sentence> sentences, bool isTransLyrics = false)
        {
            LyricSentencs = new List<Sentence>(sentences);
            IsTranslatedLyrics = isTransLyrics;
        }

        public (Sentence, int) GetCurrentSentence(int time)
        {
            var index = LyricSentencs.FindLastIndex((s) => s.StartTime <= time);

            return (index < 0 ? Sentence.Empty : LyricSentencs[index], index);
        }

        public static Lyric operator +(Lyric a, Lyric b)
        {
            if (a == null)
                return b;
            if (b == null)
                return a;

            if (a.IsTranslatedLyrics == b.IsTranslatedLyrics)
            {
                return a;
            }

            Dictionary<int, Sentence> combimeDic = new Dictionary<int, Sentence>();

            foreach (var lyrics in a.LyricSentencs)
            {
                if (combimeDic.ContainsKey(lyrics.StartTime))
                {
                    var exsit = combimeDic[lyrics.StartTime];

                    combimeDic[lyrics.StartTime] = exsit + lyrics;
                }
                else
                {
                    combimeDic[lyrics.StartTime] = lyrics;
                }
            }

            foreach (var lyrics in b.LyricSentencs)
            {
                if (combimeDic.ContainsKey(lyrics.StartTime))
                {
                    var exsit = combimeDic[lyrics.StartTime];

                    combimeDic[lyrics.StartTime] = exsit + lyrics;
                }
                else
                {
                    combimeDic[lyrics.StartTime] = lyrics;
                }
            }

            var sentences = combimeDic.Values.ToList();
            sentences.Sort();
            return new Lyric(sentences);
        }
    }
}
