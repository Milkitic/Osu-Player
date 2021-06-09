using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Media.Audio.Player;
using Milky.OsuPlayer.Media.Audio.Wave;
using Milky.OsuPlayer.Shared.Models.NostModels;
using NAudio.Lame;
using NAudio.Wave;
using Nostool.Audio;

namespace Nostool.Composer
{
    [Serializable]
    [XmlRoot("music_list")]
    public class MusicList
    {
        [XmlElement(ElementName = "music_spec")]
        public List<MusicSpec> MusicSpec { get; set; }
    }

    [XmlRoot(ElementName = "music_spec")]
    public class MusicSpec
    {
        private string _startDateStr;

        [XmlElement("basename")]
        public string BaseName { get; set; }

        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("artist")]
        public string Artist { get; set; }

        [XmlElement("title_kana")]
        public string TitleKana { get; set; }

        [XmlElement("artist_kana")]
        public string ArtistKana { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        /// <summary>
        /// -11 ~ 0
        /// </summary>
        [XmlElement("volume_bgm")]
        public int BgmVolume { get; set; }

        /// <summary>
        /// -11 ~ 0
        /// </summary>
        [XmlElement("volume_key")]
        public int KeyVolume { get; set; }

        [XmlElement("start_date")]
        public string StartDateStr
        {
            get => _startDateStr;
            set
            {
                StartDate = DateTime.Parse(value);
                _startDateStr = value;
            }
        }

        [XmlIgnore]
        public DateTime StartDate { get; set; }
    }

    class NostComposer
    {
        private readonly string _musicListPath;
        public string[] MusicFolders { get; }
        public Dictionary<string, MusicSpec> Mapping { get; private set; }

        public NostComposer(string musicListPath, params string[] musicFolders)
        {
            _musicListPath = musicListPath;
            MusicFolders = musicFolders;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var serializer = new XmlSerializer(typeof(MusicList));
            var xmlReader = XmlReader.Create(musicListPath);
            var mlist = serializer.Deserialize(xmlReader) as MusicList;
            Mapping = mlist.MusicSpec.ToDictionary(k => k.BaseName, k => k, StringComparer.OrdinalIgnoreCase);
        }

        public static SemaphoreSlim ss = new SemaphoreSlim(1, 1);
        public async Task<string> ComposeSingleByPath(string path)
        {
            await ss.WaitAsync();
            if (CachedSound.DefaultSounds.Count == 0)
            {
                var files = IOUtil.EnumerateFiles(Domain.DefaultPath, ".wav");
                await CachedSound.CreateDefaultCacheSounds(files.Select(k => k.FullName)).ConfigureAwait(false);
                Console.WriteLine("Created default caches.");
            }

            ss.Release();

            XmlSerializer serializer = new XmlSerializer(typeof(MusicScore));
            StreamReader xmlreader = new StreamReader(path);
            var mScore = serializer.Deserialize(xmlreader) as MusicScore;
            var baseName = new FileInfo(path).Directory.Name;
            if (Mapping.TryGetValue(baseName, out var musicSpec))
            {
                return await Write(path, mScore, musicSpec);
            }
            else
            {
                throw new Exception($"\"{baseName}\" was not found in the mdb");
            }
        }

        public async Task<List<string>> ComposeByMusicList()
        {
            var result = await Task.Run(() =>
            {
                var list = new List<string>();
                var all = Mapping.Keys.ToList();

                int i = 0;
                while (true)
                {
                    var sub = all
                        .Skip(Environment.ProcessorCount * 3 * i)
                        .Take(Environment.ProcessorCount * 3)
                        .ToList();
                    if (sub.Count == 0) break;
                    var paths = sub
                        .AsParallel()
                        .WithDegreeOfParallelism(Environment.ProcessorCount + 1)
                        .Select(k =>
                        {
                            try
                            {
                                return ComposeSingleByBaseName(k).Result;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                return null;
                            }
                        });
                    list.AddRange(paths);
                    CachedSound.ClearCacheSounds();
                    i++;
                }

                return list;
            });
            return result.ToList();
        }

        public async Task<List<string>> ComposeSingleByFolderIndex(int index)
        {
            var result = await Task.Run(() =>
            {
                var list = new List<string>();
                var allDirs = new DirectoryInfo(MusicFolders[index]).GetDirectories();

                int i = 0;
                while (true)
                {
                    var sub = allDirs
                        .Skip(Environment.ProcessorCount * 3 * i)
                        .Take(Environment.ProcessorCount * 3)
                        .ToList();
                    if (sub.Count == 0) break;
                    var paths = sub
                        .AsParallel()
                        .WithDegreeOfParallelism(Environment.ProcessorCount + 1)
                        .Select(k =>
                        {
                            var xml = k.EnumerateFiles($"{k.Name.ToLower()}*.xml").FirstOrDefault();
                            if (xml == null) return null;
                            try
                            {
                                return ComposeSingleByPath(xml.FullName).Result;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                return null;
                            }
                        });
                    list.AddRange(paths);
                    CachedSound.ClearCacheSounds();
                    i++;
                }

                return list;
            });
            return result.ToList();
        }

        public async Task<string> ComposeSingleByBaseName(string basename)
        {
            basename = basename.ToLower();
            foreach (var musicFolder in MusicFolders)
            {
                var fullPath = Path.Combine(musicFolder, basename);

                var di = new DirectoryInfo(fullPath);
                if (!di.Exists) continue;

                var fi = di.EnumerateFiles($"{basename}*.xml").FirstOrDefault();
                if (fi == null) return null;

                return await ComposeSingleByPath(fi.FullName);
            }

            throw new Exception($"Basename \"{basename}\" not exist.");
        }

        private static async Task<string> Write(string path, MusicScore mScore, MusicSpec musicSpec)
        {
            var sw2 = Stopwatch.StartNew();
            var seRatio = (musicSpec.KeyVolume + 12) / 12f;
            var bgmRatio = (musicSpec.BgmVolume + 12f) / 12f;

            using var engine = new AudioPlaybackEngine();
            await using var noteChannel = new NoteChannel(path, seRatio, bgmRatio, mScore, engine);
            await noteChannel.Initialize();

            Console.WriteLine("init: " + sw2.Elapsed);
            sw2.Restart();
            string p = null;
            engine.Updated += (e, t1, t2) =>
            {
                noteChannel.TakeElements((int)t2.TotalMilliseconds).Wait();

                var progress = $"Progress: {t2.TotalMilliseconds / noteChannel.ChannelEndTime.TotalMilliseconds:P0}";
                if (p != progress)
                {
                    Console.WriteLine(progress);
                    p = progress;
                }

                if (t2 > noteChannel.ChannelEndTime)
                {
                    engine.RootMixer.ReadFully = false;
                    noteChannel.Submixer.ReadFully = false;
                }
            };

            var sourceProvider = engine.Root.ToWaveProvider();
            sourceProvider = new WaveFloatTo16Provider(sourceProvider);

            //using var outStream = new MemoryStream();
            var filename = string.IsNullOrWhiteSpace(musicSpec.Artist)
                ? ReplaceInvalidChars($"{musicSpec.Title}.mp3")
                : ReplaceInvalidChars($"{musicSpec.Artist} - {musicSpec.Title}.mp3");
            var filepath = Path.Combine(Directories.ExportFolder, filename);
            await using var outStream = new FileStream(filepath, FileMode.Create, FileAccess.Write);
            await using var writer = new LameMP3FileWriter(outStream, sourceProvider.WaveFormat, 320, new ID3TagData
            {
                Title = musicSpec.Title,
                Artist = musicSpec.Artist,
                Subtitle = musicSpec.Description,
                Year = musicSpec.StartDate.Year.ToString()
            });

            //var buffer = new byte[sourceProvider.WaveFormat.AverageBytesPerSecond * 4];
            var buffer = new byte[128];
            while (true)
            {
                int count = sourceProvider.Read(buffer, 0, buffer.Length);
                if (count != 0)
                    writer.Write(buffer, 0, count);
                else
                    break;
            }

            outStream.Flush();
            Console.WriteLine("write: " + sw2.Elapsed);
            return filepath;
        }

        public static string ReplaceInvalidChars(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }
    }
}
