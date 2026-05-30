using System;
using System.IO;

namespace Milky.OsuPlayer.Media.Lyric.Output
{
    public class DiskFileOutput : OutputBase
    {
        public DiskFileOutput(string path) : base(path)
        {
            if (!System.IO.Path.IsPathRooted(FilePath))
            {
                FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FilePath);
                FilePath = Path.GetFullPath(FilePath);
            }

            if (!Directory.Exists(System.IO.Path.GetDirectoryName(FilePath)))
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath));
        }

        public override void Output(string content)
        {
            File.WriteAllText(FilePath, content);
        }
    }
}