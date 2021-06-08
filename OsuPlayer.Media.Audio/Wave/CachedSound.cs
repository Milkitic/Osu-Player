using Milky.OsuPlayer.Media.Audio.Player;
using Milky.OsuPlayer.Shared;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Media.Audio.Wave
{
    public class CachedSound
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public string SourcePath { get; }
        public float[] AudioData { get; private set; }
        public WaveFormat WaveFormat { get; private set; }
        public TimeSpan Duration { get; private set; }
        public static MyAudioFileReader.WaveStreamType WaveStreamType { get; set; } =
            MyAudioFileReader.WaveStreamType.Wav;

        private CachedSound(string filePath)
        {
            SourcePath = filePath;
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
        private static readonly ConcurrentDictionary<string, CachedSound> DefaultDictionary =
            new ConcurrentDictionary<string, CachedSound>();

        public static IReadOnlyDictionary<string, CachedSound> CachedSounds { get; } =
            new ReadOnlyDictionary<string, CachedSound>(CachedDictionary);
        public static IReadOnlyDictionary<string, CachedSound> DefaultSounds { get; } =
            new ReadOnlyDictionary<string, CachedSound>(DefaultDictionary);
        public static bool ContainsHitsound(string path)
        {
            return CachedSounds.ContainsKey(path) || DefaultSounds.ContainsKey(path);
        }

        public static async Task CreateCacheSounds(IEnumerable<string> paths)
        {
            await Task.Run(() =>
            {
                paths.AsParallel()
                    .WithDegreeOfParallelism(Environment.ProcessorCount > 1 ? Environment.ProcessorCount - 1 : 1)
                    .ForAll(k => GetOrCreateCacheSound(k, false).Wait());
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Default skin hitsounds
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static async Task CreateDefaultCacheSounds(IEnumerable<string> paths)
        {
            var tasks = paths.AsParallel().Select(async path =>
            {
                await GetOrCreateCacheSound(path, true).ConfigureAwait(false);
            });

            await Task.WhenAll(tasks);
        }

        public static async Task<CachedSound> GetOrCreateCacheSound(string path)
        {
            if (DefaultDictionary.ContainsKey(path))
                return DefaultDictionary[path];

            //if (!CachedDictionary.ContainsKey(path))
            return await GetOrCreateCacheSound(path, false).ConfigureAwait(false);

            //return CachedDictionary[path];
        }

        public static void ClearCacheSounds()
        {
            CachedDictionary.Clear();
        }

        internal static void ClearDefaultCacheSounds()
        {
            DefaultDictionary.Clear();
        }

        private static async Task<CachedSound> CreateFromFile(string filePath)
        {
            using (var audioFileReader = await WaveFormatFactory.GetResampledAudioFileReader(filePath,
                WaveStreamType).ConfigureAwait(false))
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
                    WaveFormat = audioFileReader.WaveFormat,
                };
                return cachedSound;
            }
        }

        private static async Task<CachedSound> GetOrCreateCacheSound(string path, bool isDefault)
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
                else DefaultDictionary.TryAdd(path, null);
                return null;
            }

            if (!isDefault && CachedDictionary.TryGetValue(path, out var value) ||
                isDefault && DefaultDictionary.TryGetValue(path, out value))
            {
                return value;
            }

            CachedSound cachedSound;
            try
            {
                cachedSound = await CreateFromFile(newPath).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while creating cached sound: {0}", path);
                if (!isDefault) CachedDictionary.TryAdd(path, null);
                else DefaultDictionary.TryAdd(path, null);
                return null;
            }

            // Cache each file once before play.
            var sound = isDefault
                ? DefaultDictionary.GetOrAdd(path, cachedSound)
                : CachedDictionary.GetOrAdd(path, cachedSound);

            Logger.Debug("Total size of cache usage: {0}", SharedUtils.CountSize(
                CachedDictionary.Values.Sum(k => k?.AudioData?.Length * sizeof(float) ?? 0) +
                DefaultDictionary.Values.Sum(k => k?.AudioData?.Length * sizeof(float) ?? 0)));

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
    }
}