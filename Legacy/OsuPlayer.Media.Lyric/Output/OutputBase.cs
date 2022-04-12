namespace Milky.OsuPlayer.Media.Lyric.Output
{
    public abstract class OutputBase
    {
        public virtual string FilePath { get; set; }

        public OutputBase(string path)
        {
            this.FilePath = path;
        }

        public abstract void Output(string content);

        public static OutputBase Create(string path)
        {
            if (path.StartsWith(MemoryMappedFileOutput.MMF_FORMAT))
                return new MemoryMappedFileOutput(path);
            return new DiskFileOutput(path);
        }
    }
}