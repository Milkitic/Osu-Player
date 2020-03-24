using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OSharp.Beatmap;
using OSharp.Beatmap.Sections.GamePlay;
using OSharp.Beatmap.Sections.HitObject;
using OSharp.Beatmap.Sections.Timing;
using PlayerTest.Player.Channel;

namespace PlayerTest.Player
{
    public class OsuMixPlayer : MultichannelPlayer
    {
        private readonly OsuFile _osuFile;
        private readonly string _sourceFolder;
        private SingleMediaChannel _musicChannel;
        private MultiElementsChannel _hitsoundChannel;
        private MultiElementsChannel _sampleChannel;

        public OsuMixPlayer(OsuFile osuFile, string sourceFolder)
        {
            _osuFile = osuFile;
            _sourceFolder = sourceFolder;
        }

        public override async Task Initialize()
        {
            var mp3Path = Path.Combine(_sourceFolder, _osuFile.General.AudioFilename);
            _musicChannel = new SingleMediaChannel(Engine, mp3Path,
                AppSettings.Default.Play.PlaybackRate,
                AppSettings.Default.Play.PlayUseTempo)
            {
                Description = "Music"
            };

            var hitsoundList = await GetHitsoundsAsync();
            _hitsoundChannel = new MultiElementsChannel(Engine, hitsoundList, _musicChannel)
            {
                Description = "Hitsound"
            };

            var sampleList = GetSamplesAsync();
            _sampleChannel = new MultiElementsChannel(Engine, sampleList, _musicChannel)
            {
                Description = "Sample"
            };

            AddSubchannel(_musicChannel);
            AddSubchannel(_hitsoundChannel);
            AddSubchannel(_sampleChannel);
        }

        private async Task<List<SoundElement>> GetHitsoundsAsync()
        {
            List<RawHitObject> hitObjects = _osuFile.HitObjects.HitObjectList;
            var elements = new List<SoundElement>();
            var dirInfo = new DirectoryInfo(_sourceFolder);
            var waves = new HashSet<string>(dirInfo.EnumerateFiles()
                .Where(k => AudioPlaybackEngine.SupportExtensions.Contains(
                    k.Extension, StringComparer.OrdinalIgnoreCase)
                )
                .Select(p => Path.GetFileNameWithoutExtension(p.Name))
            );

            foreach (var obj in hitObjects)
            {
                if (obj.ObjectType != HitObjectType.Slider)
                {
                    var hitOffset = obj.ObjectType == HitObjectType.Spinner
                        ? obj.HoldEnd // spinner
                        : obj.Offset; // hold & circle
                    var timingPoint = _osuFile.TimingPoints.GetLine(hitOffset);

                    float balance = GetObjectBalance(obj.X);
                    float volume = (obj.SampleVolume != 0 ? obj.SampleVolume : timingPoint.Volume) / 100f;

                    var files = AnalyzeHitsoundFile(obj.Hitsound, obj.SampleSet, obj.AdditionSet,
                        timingPoint, obj, waves);
                    foreach (var file in files)
                    {
                        var element = SoundElement.Create(hitOffset, volume, balance, file);
                        await element.GetCachedSoundAsync();
                        elements.Add(element);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return elements;
        }

        private IEnumerable<string> AnalyzeHitsoundFile(
            HitsoundType itemHitsound, ObjectSamplesetType itemSample, ObjectSamplesetType itemAddition,
            TimingPoint timingPoint, RawHitObject hitObject,
            HashSet<string> waves)
        {
            if (!string.IsNullOrEmpty(hitObject.FileName))
            {
                return new[] { GetFileUntilFind(_sourceFolder, Path.GetFileNameWithoutExtension(hitObject.FileName)) };
            }

            var files = new List<string>();

            // hitnormal
            var sampleStr = itemSample != ObjectSamplesetType.Auto
                ? itemSample.ToHitsoundString()
                : timingPoint.TimingSampleset.ToHitsoundString();

            // hitclap, hitfinish, hitwhistle
            string additionStr = itemAddition.ToHitsoundString();

            if (hitObject.ObjectType == HitObjectType.Slider && hitObject.SliderInfo.EdgeHitsounds == null)
            {
                var hitsounds = GetHitsounds(hitObject.Hitsound, sampleStr, additionStr);
                files.AddRange(hitsounds);
            }
            else
            {
                var hitsounds = GetHitsounds(itemHitsound, sampleStr, additionStr);
                files.AddRange(_osuFile.General.Mode == GameMode.Mania
                    ? hitsounds.Take(1)
                    : hitsounds);
            }

            for (var i = 0; i < files.Count; i++)
            {
                var fileNameWithoutIndex = files[i];

                int baseIndex = hitObject.CustomIndex > 0 ? hitObject.CustomIndex : timingPoint.Track;
                string indexStr = baseIndex > 1 ? baseIndex.ToString() : "";

                var fileNameWithoutExt = fileNameWithoutIndex + indexStr;

                string filePath;
                if (timingPoint.Track == 0)
                    filePath = Path.Combine(Domain.DefaultPath, fileNameWithoutExt + AudioPlaybackEngine.WavExtension);
                else if (waves.Contains(fileNameWithoutExt))
                    filePath = GetFileUntilFind(_sourceFolder, fileNameWithoutExt);
                else
                    filePath = Path.Combine(Domain.DefaultPath, fileNameWithoutExt + AudioPlaybackEngine.WavExtension);

                files[i] = filePath;
            }

            return files;
        }

        private readonly Dictionary<string, string> _pathCache = new Dictionary<string, string>();

        private string GetFileUntilFind(string sourceFolder, string fileNameWithoutExtension)
        {
            var combine = Path.Combine(sourceFolder, fileNameWithoutExtension);
            if (_pathCache.ContainsKey(combine))
            {
                return _pathCache[combine];
            }

            string path = "";
            foreach (var extension in AudioPlaybackEngine.SupportExtensions)
            {
                path = Path.Combine(sourceFolder, fileNameWithoutExtension + extension);

                if (File.Exists(path))
                {
                    _pathCache.Add(combine, path);
                    return path;
                }
            }

            _pathCache.Add(combine, path);
            return path;
        }

        private float GetObjectBalance(float x)
        {
            if (_osuFile.General.Mode == GameMode.Taiko) return 0;

            if (x > 512) x = 512;
            else if (x < 0) x = 0;

            float balance = (x - 256f) / 256f;
            return balance;
        }

        private static IEnumerable<string> GetHitsounds(HitsoundType type,
            string sampleStr, string additionStr)
        {
            if (type.HasFlag(HitsoundType.Whistle))
                yield return $"{additionStr}-hitwhistle";
            if (type.HasFlag(HitsoundType.Clap))
                yield return $"{additionStr}-hitclap";
            if (type.HasFlag(HitsoundType.Finish))
                yield return $"{additionStr}-hitfinish";
            if (type.HasFlag(HitsoundType.Normal) ||
                (type & HitsoundType.Normal) == 0)
            {
                yield return $"{sampleStr}-hitnormal";
            }
        }

        private List<SoundElement> GetSamplesAsync()
        {
            throw new NotImplementedException();
        }
    }

    public static class ToStringExtension
    {
        public static string ToHitsoundString(this TimingSamplesetType type)
        {
            switch (type)
            {
                case TimingSamplesetType.Soft:
                    return "soft";
                case TimingSamplesetType.Drum:
                    return "drum";
                default:
                case TimingSamplesetType.None:
                case TimingSamplesetType.Normal:
                    return "normal";
            }
        }

        public static string ToHitsoundString(this ObjectSamplesetType type)
        {
            switch (type)
            {
                case ObjectSamplesetType.Soft:
                    return "soft";
                case ObjectSamplesetType.Drum:
                    return "drum";
                default:
                case ObjectSamplesetType.Normal:
                    return "normal";
            }
        }
    }
}