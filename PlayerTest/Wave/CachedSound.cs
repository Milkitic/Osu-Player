using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;

namespace PlayerTest.Wave
{
    internal class CachedSound
    {
        public byte[] RawFileData { get; private set; }
        public byte[] AudioData { get; private set; }
        public WaveFormat WaveFormat { get; private set; }

        public TimeSpan Duration { get; private set; }
        public long Length { get; private set; }

        public static async Task<CachedSound> CreateFromFile(string audioFileName)
        {
            using (var stream = await WaveFormatFactory.Resample(audioFileName))
            using (var audioFileReader = new WaveFileReader(stream))
            {
                var wholeData = new List<byte>((int)(audioFileReader.Length / 4));

                var readBuffer =
                    new byte[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
                int samplesRead;
                while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    wholeData.AddRange(readBuffer.Take(samplesRead));
                }

                return new CachedSound
                {
                    RawFileData = stream.ToArray(),
                    AudioData = wholeData.ToArray(),
                    Duration = audioFileReader.TotalTime,
                    Length = audioFileReader.Length,
                    WaveFormat = audioFileReader.WaveFormat
                };
            }
        }

        private static readonly ConcurrentDictionary<string, CachedSound> CachedDictionary =
            new ConcurrentDictionary<string, CachedSound>();
        private static readonly ConcurrentDictionary<string, CachedSound> InternalDictionary =
            new ConcurrentDictionary<string, CachedSound>();

        private static readonly string[] SupportExtensions = { ".wav", ".mp3", ".ogg" };

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

            if (!CachedDictionary.ContainsKey(path))
                await CreateCacheSound(path, false);

            return CachedDictionary[path];
        }

        private static async Task CreateCacheSound(string path, bool isDefault)
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
                return;
            }

            if (!File.Exists(newPath))
            {
                if (!isDefault) CachedDictionary.TryAdd(path, null);
                else InternalDictionary.TryAdd(path, null);
                return;
            }

            try
            {
                if (!isDefault) CachedDictionary.TryAdd(path, await CreateFromFile(newPath)); // Cache each file once before play.
                else InternalDictionary.TryAdd(path, await CreateFromFile(newPath));
            }
            catch
            {
                if (!isDefault) CachedDictionary.TryAdd(path, null);
                else InternalDictionary.TryAdd(path, null);
            }
        }

        private static string TryGetPath(string path)
        {
            foreach (var ext in SupportExtensions)
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
    }
}