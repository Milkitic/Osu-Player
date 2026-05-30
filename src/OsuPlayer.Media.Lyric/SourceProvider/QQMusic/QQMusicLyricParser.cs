using Milky.OsuPlayer.Media.Lyric.Models;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Milky.OsuPlayer.Media.Lyric.SourceProvider.QQMusic
{
    public class QQMusicLyricParser : LyricParserBase
    {
        private readonly Regex _lyricRegex = new Regex(@"\[(\d{2}\d*)\:(\d{2})\.(\d*)?\](.*)");

        public override Lyrics Parse(string content)
        {
            List<Sentence> sentenceList = new List<Sentence>();

            StreamReader reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(content)));

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                var match = _lyricRegex.Match(line);
                if (!match.Success)
                {
                    continue;
                }
                int min = int.Parse(match.Groups[1].Value),
                    sec = int.Parse(match.Groups[2].Value),
                    msec = int.Parse(match.Groups[3].Value);

                string cont = match.Groups[4].Value.Trim();

                int time = min * 60 * 1000 + sec * 1000 + msec;

                sentenceList.Add(new Sentence(cont, time));
            }

            reader.Close();

            sentenceList.Sort();

            return new Lyrics(sentenceList);
        }
    }
}
