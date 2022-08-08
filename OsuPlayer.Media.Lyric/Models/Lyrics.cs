using System.Collections.Generic;
using System.Linq;

namespace Milki.OsuPlayer.Media.Lyric.Models
{
    public class Lyrics
    {
        public static Lyrics Empty { get; private set; } = new Lyrics(new List<Sentence>());

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

        public Lyrics() : this(new List<Sentence>()) { }

        public Lyrics(IEnumerable<Sentence> sentences, bool is_trans_lyrics = false)
        {
            LyricSentencs = new List<Sentence>(sentences);
            IsTranslatedLyrics = is_trans_lyrics;
        }

        public virtual (Sentence, int) GetCurrentSentence(int time)
        {
            var index = LyricSentencs.FindLastIndex((s) => s.StartTime <= time);

            return (index < 0 ? Sentence.Empty : LyricSentencs[index], index);
        }

        public static Lyrics operator +(Lyrics a, Lyrics b)
        {
            if (a == null)
                return b;
            if (b == null)
                return a;

            if (a.IsTranslatedLyrics == b.IsTranslatedLyrics)
            {
                return a;
            }

            Dictionary<int, Sentence> combime_dic = new Dictionary<int, Sentence>();

            foreach (var lyrics in a.LyricSentencs)
            {
                if (combime_dic.ContainsKey(lyrics.StartTime))
                {
                    var exsit = combime_dic[lyrics.StartTime];

                    combime_dic[lyrics.StartTime] = exsit + lyrics;
                }
                else
                {
                    combime_dic[lyrics.StartTime] = lyrics;
                }
            }

            foreach (var lyrics in b.LyricSentencs)
            {
                if (combime_dic.ContainsKey(lyrics.StartTime))
                {
                    var exsit = combime_dic[lyrics.StartTime];

                    combime_dic[lyrics.StartTime] = exsit + lyrics;
                }
                else
                {
                    combime_dic[lyrics.StartTime] = lyrics;
                }
            }

            var sentences = combime_dic.Values.ToList();
            sentences.Sort();
            return new Lyrics(sentences);
        }
    }
}
