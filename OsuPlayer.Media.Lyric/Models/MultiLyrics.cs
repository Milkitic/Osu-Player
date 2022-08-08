using System;
using System.Collections.Generic;
using System.Linq;

namespace Milki.OsuPlayer.Media.Lyric.Models
{
    public class MultiLyrics : Lyrics
    {
        private readonly IEnumerable<Lyrics> lyricses;

        public MultiLyrics(params Lyrics[] lyricses)
        {
            this.lyricses = lyricses.OfType<Lyrics>();
        }

        public override (Sentence, int) GetCurrentSentence(int time)
        {
            int min_index = int.MaxValue;
            Sentence sentence = Sentence.Empty;

            foreach (var lyrics in lyricses)
            {
                var result = lyrics.GetCurrentSentence(time);

                if (result.Item1.Equals(Sentence.Empty))
                    continue;

                sentence = sentence + result.Item1;
                min_index = Math.Min(min_index, result.Item2);
            }

            //trim newline in sentence content
            sentence.Content = sentence.Content.Trim('\n', '\r');

            return (sentence, min_index);
        }
    }
}
