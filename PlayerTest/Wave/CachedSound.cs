using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;
using PlayerTest.Player;

namespace PlayerTest.Wave
{
    internal class CachedSound
    {
        public string SourcePath { get; }
        public float[] AudioData { get; private set; }
        public WaveFormat WaveFormat { get; private set; }
        public TimeSpan Duration { get; private set; }

        private CachedSound(string filePath)
        {
            SourcePath = filePath;
        }

        private static async Task<CachedSound> CreateFromFile(string filePath)
        {
            var type = StreamType.Wav;
            var stream = await WaveFormatFactory.Resample(filePath, type);
            using (var audioFileReader = new MyAudioFileReader(stream, type))
            {
                var wholeData = new List<float>((int)(audioFileReader.Length / 4));

                var readBuffer =
                    new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
                int samplesRead;
                while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    wholeData.AddRange(readBuffer.Take(samplesRead));
                }

                var cachedSound = new CachedSound(filePath)
                {
                    AudioData = wholeData.ToArray(),
                    Duration = audioFileReader.TotalTime,
                    WaveFormat = audioFileReader.WaveFormat
                };
                return cachedSound;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is CachedSound other)
                return Equals(other);
            return ReferenceEquals(this, obj);
        }

        protected bool Equals(CachedSound other)
        {
            return SourcePath == other.SourcePath;
        }

        public override int GetHashCode()
        {
            return (SourcePath != null ? SourcePath.GetHashCode() : 0);
        }


        private static readonly ConcurrentDictionary<string, CachedSound> CachedDictionary =
            new ConcurrentDictionary<string, CachedSound>();
        private static readonly ConcurrentDictionary<string, CachedSound> InternalDictionary =
            new ConcurrentDictionary<string, CachedSound>();

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

            if (!File.Exists(newPath))
            {
                if (!isDefault) CachedDictionary.TryAdd(path, null);
                else InternalDictionary.TryAdd(path, null);
                return null;
            }

            if (!isDefault && CachedDictionary.TryGetValue(path, out var value) ||
                isDefault && InternalDictionary.TryGetValue(path, out value))
            {
                return value;
            }

            CachedSound cachedSound;
            try
            {
                cachedSound = await CreateFromFile(newPath);
            }
            catch
            {
                if (!isDefault) CachedDictionary.TryAdd(path, null);
                else InternalDictionary.TryAdd(path, null);
                return null;
            }

            // Cache each file once before play.
            var sound = isDefault
                ? InternalDictionary.GetOrAdd(path, cachedSound)
                : CachedDictionary.GetOrAdd(path, cachedSound);

            //Console.WriteLine(CountSize(CachedDictionary.Values.Sum(k => k.AudioData.Length)));

            return sound;
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