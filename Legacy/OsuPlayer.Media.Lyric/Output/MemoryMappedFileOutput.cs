using System.IO;
using System.IO.MemoryMappedFiles;

namespace Milky.OsuPlayer.Media.Lyric.Output
{
    internal class MemoryMappedFileOutput : OutputBase
    {
        public override string FilePath
        {
            get => base.FilePath;
            set
            {
                lock (mtx)
                {
                    base.FilePath = value;
                    mmf_handler?.Dispose();
                    Initialize(value);
                }
            }
        }

        private object mtx = new object();

        public const string MMF_FORMAT = @"mmf://";
        public const int MMF_CAPACITY = 4096;
        public readonly static byte[] clear_buffer = new byte[MMF_CAPACITY];

        private MemoryMappedFile mmf_handler;

        public MemoryMappedFileOutput(string path) : base(path)
        {
            Initialize(path);
        }

        private void Initialize(string path)
        {
            string real_mmf_path = path.StartsWith(MMF_FORMAT) ? path.Remove(0, MMF_FORMAT.Length) : path;
            mmf_handler = MemoryMappedFile.CreateOrOpen(real_mmf_path, MMF_CAPACITY, MemoryMappedFileAccess.ReadWrite);
        }

        public override void Output(string content)
        {
            lock (mtx)
            {
                using (StreamWriter stream = new StreamWriter(mmf_handler.CreateViewStream()))
                {
                    stream.Write(content);
                    stream.Write('\0');
                }
            }
        }
    }
}