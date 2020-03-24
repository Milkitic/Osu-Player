using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;
using PlayerTest.Player;

namespace PlayerTest.Wave
{
    internal class CachedSound
    {
        //public byte[] RawFileData { get; private set; }
        public byte[] AudioData { get; private set; }
        public WaveFormat WaveFormat { get; private set; }

        //public TimeSpan Duration { get; private set; }
        //public long Length { get; private set; }

        public static async Task<CachedSound> CreateFromFile(string audioFileName)
        {
            var type = StreamType.Wav;
            var stream = await WaveFormatFactory.Resample(audioFileName, type);
            var arr = stream.ToArray();
            using (var audioFileReader = new MyAudioFileReader(arr, type))
            {
                var wholeData = new List<byte>((int)(audioFileReader.Length / 4));

                var readBuffer =
                    new byte[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
                int samplesRead;
                while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    wholeData.AddRange(readBuffer.Take(samplesRead));
                }

                var cachedSound = new CachedSound
                {
                    //RawFileData = stream.ToArray(),
                    AudioData = wholeData.ToArray(),
                    //Duration = audioFileReader.TotalTime,
                    //Length = audioFileReader.Length,
                    WaveFormat = audioFileReader.WaveFormat
                };
                return cachedSound;
            }
        }

        private static readonly ConcurrentDictionary<string, CachedSound> CachedDictionary =
            new ConcurrentDictionary<string, CachedSound>();
        private static readonly ConcurrentDictionary<string, CachedSound> InternalDictionary =
            new ConcurrentDictionary<string, CachedSound>();

        private static int _total;
        private static object _totalLock = new object();

        public static async Task CreateCacheSounds(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                await CreateCacheSound(path, false); // Cache each file once before play.
            }
        }

        /// <summary>
        /// Default skin hitsounds
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static async Task CreateDefaultCacheSounds(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                await CreateCacheSound(path, true);
            }
        }

        public static async Task<CachedSound> GetOrCreateCacheSound(string path)
        {
            if (InternalDictionary.ContainsKey(path))
                return InternalDictionary[path];

            //if (!CachedDictionary.ContainsKey(path))
            return await CreateCacheSound(path, false);

            //return CachedDictionary[path];
        }

        private static async Task<CachedSound> CreateCacheSound(string path, bool isDefault)
        {
            string newPath = path;
            if (!File.Exists(newPath))
            {
                newPath = TryGetPath(newPath);
            }

            if (!File.Exists(newPath))
            {
                newPath = TryGetPath(Path.Combine(Path.GetDirectoryName(newPath), Path.GetFileNameWithoutExtension(newPath)));
            }

            if (!isDefault && CachedDictionary.ContainsKey(path) ||
                isDefault && InternalDictionary.ContainsKey(path))
            {
                return isDefault ? InternalDictionary[path] : CachedDictionary[path];
            }

            if (!File.Exists(newPath))
            {
                if (!isDefault) CachedDictionary.TryAdd(path, null);
                else InternalDictionary.TryAdd(path, null);
                return null;
            }

            try
            {
                var cachedSound = await CreateFromFile(newPath);
                if (!isDefault) CachedDictionary.TryAdd(path, cachedSound); // Cache each file once before play.
                else InternalDictionary.TryAdd(path, cachedSound);
                lock (_totalLock)
                {
                    _total += cachedSound.AudioData.Length;
                    Console.WriteLine(CountSize(_total));
                }

                return cachedSound;
            }
            catch
            {
                if (!isDefault) CachedDictionary.TryAdd(path, null);
                else InternalDictionary.TryAdd(path, null);
                return null;
            }
        }

        private static string TryGetPath(string path)
        {
            foreach (var ext in AudioPlaybackEngine.SupportExtensions)
            {
                var autoAudioFile = path + ext;
                if (!File.Exists(autoAudioFile))
                    continue;

                path = autoAudioFile;
                break;
            }

            return path;
        }

        public static void ClearCacheSounds()
        {
            CachedDictionary.Clear();
            lock (_totalLock)
            {
                _total = 0;
            }
        }

        public static string CountSize(long size)
        {
            string strSize = "";
            long factSize = size;
            if (factSize < 1024)
                strSize = $"{factSize:F2} B";
            else if (factSize >= 1024 && factSize < 1048576)
                strSize = (factSize / 1024f).ToString("F2") + " KB";
            else if (factSize >= 1048576 && factSize < 1073741824)
                strSize = (factSize / 1024f / 1024f).ToString("F2") + " MB";
            else if (factSize >= 1073741824)
                strSize = (factSize / 1024f / 1024f / 1024f).ToString("F2") + " GB";
            return strSize;
        }
    }
}