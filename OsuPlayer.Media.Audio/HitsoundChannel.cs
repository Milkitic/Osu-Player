using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Coosu.Beatmap;
using Coosu.Beatmap.Sections.GamePlay;
using Coosu.Beatmap.Sections.HitObject;
using Coosu.Beatmap.Sections.Timing;
using Milki.Extensions.MixPlayer;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.Subchannels;
using Milky.OsuPlayer.Common;

namespace Milky.OsuPlayer.Media.Audio
{
    public class HitsoundChannel : MultiElementsChannel
    {
        private readonly HitsoundFileCache _cache;
        private readonly OsuFile _osuFile;
        private readonly string _sourceFolder;

        public HitsoundChannel(LocalOsuFile osuFile, AudioPlaybackEngine engine, HitsoundFileCache cache = null)
            : this(osuFile, Path.GetDirectoryName(osuFile.OriginalPath), engine, cache)
        {
        }

        public HitsoundChannel(OsuFile osuFile, string sourceFolder, AudioPlaybackEngine engine, HitsoundFileCache cache = null)
            : base(engine, new MixSettings { ForceMode = true })
        {
            _cache = cache ?? new HitsoundFileCache();
            _osuFile = osuFile;
            _sourceFolder = sourceFolder;

            Description = nameof(HitsoundChannel);
        }

        public override async Task<IEnumerable<SoundElement>> GetSoundElements()
        {
            if (_osuFile?.HitObjects == null || _osuFile.TimingPoints == null)
            {
                return Array.Empty<SoundElement>();
            }

            _osuFile.HitObjects.ComputeSlidersByCurrentSettings();

            var elements = new ConcurrentBag<SoundElement>();
            var hitObjects = _osuFile.HitObjects.HitObjectList;

            await Task.Run(() =>
            {
                hitObjects.AsParallel()
                    .WithDegreeOfParallelism(1)
                    .ForAll(obj => AddSingleHitObject(obj, elements));

                _osuFile.Events?.Samples?.AsParallel().ForAll(sampleData =>
                {
                    var filePath = ResolveHitsoundPath(Path.GetFileNameWithoutExtension(sampleData.Filename));
                    elements.Add(SoundElement.Create(sampleData.Offset, sampleData.Volume / 100f, 0, filePath));
                });
            }).ConfigureAwait(false);

            return elements.OrderBy(k => k.Offset).ToList();
        }

        private void AddSingleHitObject(RawHitObject hitObject, ConcurrentBag<SoundElement> elements)
        {
            var timingPoints = _osuFile.TimingPoints;
            var ignoreBalance = _osuFile.General?.Mode == GameMode.Taiko;

            if (hitObject.ObjectType != HitObjectType.Slider || hitObject.SliderInfo == null)
            {
                var offset = hitObject.ObjectType == HitObjectType.Spinner ? hitObject.HoldEnd : hitObject.Offset;
                var line = timingPoints.GetLine(offset);
                var balance = ignoreBalance ? 0 : GetObjectBalance(hitObject.X);
                var volume = GetObjectVolume(hitObject, line);

                foreach (var sound in AnalyzeHitsoundFiles(hitObject.Hitsound, hitObject.SampleSet,
                             hitObject.AdditionSet, line, hitObject))
                {
                    elements.Add(SoundElement.Create(offset, volume, balance, sound.FilePath));
                }

                return;
            }

            var useObjectHitsound = hitObject.SliderInfo.EdgeHitsounds == null;
            var edges = hitObject.SliderInfo.GetEdges();
            for (var i = 0; i < edges.Length; i++)
            {
                var sliderEdge = edges[i];
                var offset = sliderEdge.Offset;
                var line = timingPoints.GetLine(offset);
                var balance = ignoreBalance ? 0 : GetObjectBalance(sliderEdge.Point.X);
                var volume = GetObjectVolume(hitObject, line);
                var hitsound = useObjectHitsound ? hitObject.Hitsound : sliderEdge.EdgeHitsound;
                var addition = useObjectHitsound ? hitObject.AdditionSet : sliderEdge.EdgeAddition;
                var sample = useObjectHitsound ? hitObject.SampleSet : sliderEdge.EdgeSample;

                foreach (var sound in AnalyzeHitsoundFiles(hitsound, sample, addition, line, hitObject))
                {
                    elements.Add(SoundElement.Create(offset, volume, balance, sound.FilePath));
                }
            }

            foreach (var sliderTick in hitObject.SliderInfo.GetSliderTicks())
            {
                var line = timingPoints.GetLine(sliderTick.Offset);
                var balance = ignoreBalance ? 0 : GetObjectBalance(sliderTick.Point.X);
                var volume = GetObjectVolume(hitObject, line);
                var sound = AnalyzeHitsoundFiles(HitsoundType.Tick, hitObject.SampleSet,
                    hitObject.AdditionSet, line, hitObject).FirstOrDefault();
                if (sound.FilePath != null)
                {
                    elements.Add(SoundElement.Create(sliderTick.Offset, volume, balance, sound.FilePath));
                }
            }

            AddSliderLoopSignals(hitObject, edges, ignoreBalance, elements);
        }

        private void AddSliderLoopSignals(RawHitObject hitObject, SliderEdge[] edges, bool ignoreBalance,
            ConcurrentBag<SoundElement> elements)
        {
            if (edges.Length == 0)
            {
                return;
            }

            var startOffset = hitObject.Offset;
            var endOffset = edges[edges.Length - 1].Offset;
            var line = _osuFile.TimingPoints.GetLine(startOffset);
            var balance = ignoreBalance ? 0 : GetObjectBalance(hitObject.X);
            var volume = GetObjectVolume(hitObject, line);
            var activeLoopFiles = new Dictionary<int, string>();

            foreach (var loop in AnalyzeSliderLoopFiles(hitObject, line))
            {
                var loopChannel = GetLoopChannel(loop.HitsoundType);
                if (!loopChannel.HasValue)
                {
                    continue;
                }

                activeLoopFiles[loopChannel.Value] = loop.FilePath;
                elements.Add(SoundElement.CreateLoopSignal(startOffset, volume, balance, loop.FilePath, loopChannel.Value));
            }

            var timingChanges = _osuFile.TimingPoints.TimingList
                .Where(k => k.Offset > startOffset + 0.5 && k.Offset < endOffset)
                .ToList();
            for (var i = 0; i < timingChanges.Count; i++)
            {
                var current = timingChanges[i];
                var previous = i == 0 ? line : timingChanges[i - 1];
                if (current.Track == previous.Track && current.TimingSampleset == previous.TimingSampleset)
                {
                    timingChanges.RemoveAt(i);
                    i--;
                    continue;
                }

                volume = GetObjectVolume(hitObject, current);
                foreach (var loop in AnalyzeSliderLoopFiles(hitObject, current))
                {
                    var loopChannel = GetLoopChannel(loop.HitsoundType);
                    if (!loopChannel.HasValue)
                    {
                        continue;
                    }

                    if (loopChannel.Value == (int)SliderLoopChannel.Normal &&
                        activeLoopFiles.TryGetValue(loopChannel.Value, out var currentFile) &&
                        currentFile == loop.FilePath)
                    {
                        elements.Add(SoundElement.CreateLoopVolumeSignal(current.Offset, volume));
                    }
                    else
                    {
                        activeLoopFiles[loopChannel.Value] = loop.FilePath;
                        elements.Add(SoundElement.CreateLoopSignal(current.Offset, volume, balance, loop.FilePath, loopChannel.Value));
                    }
                }
            }

            elements.Add(SoundElement.CreateLoopStopSignal(endOffset, (int)SliderLoopChannel.Normal));
            elements.Add(SoundElement.CreateLoopStopSignal(endOffset, (int)SliderLoopChannel.Whistle));

            foreach (var slide in hitObject.SliderInfo.GetSliderSlides())
            {
                var slideBalance = ignoreBalance ? 0 : GetObjectBalance(slide.Point.X);
                elements.Add(SoundElement.CreateLoopBalanceSignal(slide.Offset, slideBalance));
            }
        }

        private IEnumerable<(string FilePath, HitsoundType HitsoundType)> AnalyzeSliderLoopFiles(
            RawHitObject hitObject, TimingPoint timingPoint)
        {
            return AnalyzeHitsoundFiles((hitObject.Hitsound & HitsoundType.SlideWhistle) | HitsoundType.Slide,
                hitObject.SampleSet, hitObject.AdditionSet, timingPoint, hitObject);
        }

        private IEnumerable<(string FilePath, HitsoundType HitsoundType)> AnalyzeHitsoundFiles(
            HitsoundType itemHitsound, ObjectSamplesetType itemSample,
            ObjectSamplesetType itemAddition, TimingPoint timingPoint, RawHitObject hitObject)
        {
            if (!string.IsNullOrEmpty(hitObject.FileName))
            {
                return new[]
                {
                    (ResolveHitsoundPath(Path.GetFileNameWithoutExtension(hitObject.FileName)), itemHitsound)
                };
            }

            var sample = IsDefinedSampleset(itemSample) && itemSample != ObjectSamplesetType.Auto
                ? itemSample.ToHitsoundString(null)
                : timingPoint.TimingSampleset.ToHitsoundString();
            var addition = IsDefinedSampleset(itemAddition)
                ? itemAddition.ToHitsoundString(sample)
                : timingPoint.TimingSampleset.ToHitsoundString();
            var ignoreBase = _osuFile.General?.Mode == GameMode.Mania &&
                             (hitObject.ObjectType != HitObjectType.Slider || hitObject.SliderInfo?.EdgeHitsounds != null);

            return GetHitsounds(itemHitsound, sample, addition, ignoreBase)
                .Select(k =>
                {
                    var track = hitObject.CustomIndex > 0 ? hitObject.CustomIndex : timingPoint.Track;
                    var trackSuffix = track > 1 ? track.ToString() : string.Empty;
                    var customName = k.FileName + trackSuffix;
                    var filePath = timingPoint.Track != 0 && HasBeatmapHitsound(customName)
                        ? ResolveHitsoundPath(customName)
                        : ResolveDefaultHitsoundPath(k.FileName);
                    return (filePath, k.HitsoundType);
                });
        }

        private bool HasBeatmapHitsound(string fileNameWithoutExtension)
        {
            _cache.GetFileUntilFind(_sourceFolder, fileNameWithoutExtension, out var found);
            return found;
        }

        private string ResolveHitsoundPath(string fileNameWithoutExtension)
        {
            var filePath = _cache.GetFileUntilFind(_sourceFolder, fileNameWithoutExtension, out var found);
            return found ? filePath : ResolveDefaultHitsoundPath(fileNameWithoutExtension);
        }

        private static string ResolveDefaultHitsoundPath(string fileNameWithoutExtension)
        {
            foreach (var extension in new[] { ".wav", ".mp3", ".ogg" })
            {
                var path = Path.Combine(Domain.DefaultPath, fileNameWithoutExtension + extension);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private static bool IsDefinedSampleset(ObjectSamplesetType sampleset)
        {
            return Enum.IsDefined(typeof(ObjectSamplesetType), sampleset);
        }

        private static int? GetLoopChannel(HitsoundType hitsoundType)
        {
            if (hitsoundType.HasFlag(HitsoundType.Slide))
            {
                return (int)SliderLoopChannel.Normal;
            }

            if (hitsoundType.HasFlag(HitsoundType.SlideWhistle))
            {
                return (int)SliderLoopChannel.Whistle;
            }

            return null;
        }

        private static float GetObjectBalance(float x)
        {
            if (x > 512)
            {
                x = 512;
            }
            else if (x < 0)
            {
                x = 0;
            }

            return (x - 256) / 256;
        }

        private static float GetObjectVolume(RawHitObject obj, TimingPoint timingPoint)
        {
            return (obj.SampleVolume != 0 ? obj.SampleVolume : timingPoint.Volume) / 100f;
        }

        private static IEnumerable<(string FileName, HitsoundType HitsoundType)> GetHitsounds(
            HitsoundType type, string sample, string addition, bool ignoreBase = false)
        {
            if (type == HitsoundType.Tick)
            {
                yield return (addition + "-slidertick", type);
                yield break;
            }

            if (type.HasFlag(HitsoundType.Slide))
            {
                yield return (sample + "-sliderslide", HitsoundType.Slide);
            }

            if (type.HasFlag(HitsoundType.SlideWhistle))
            {
                yield return (addition + "-sliderwhistle", HitsoundType.SlideWhistle);
            }

            if (type.HasFlag(HitsoundType.Slide) || type.HasFlag(HitsoundType.SlideWhistle))
            {
                yield break;
            }

            if (type.HasFlag(HitsoundType.Whistle))
            {
                yield return (addition + "-hitwhistle", type);
            }

            if (type.HasFlag(HitsoundType.Clap))
            {
                yield return (addition + "-hitclap", type);
            }

            if (type.HasFlag(HitsoundType.Finish))
            {
                yield return (addition + "-hitfinish", type);
            }

            if ((!ignoreBase || type == 0) && (type.HasFlag(HitsoundType.Normal) || (type & HitsoundType.Normal) == 0))
            {
                yield return (sample + "-hitnormal", type);
            }
        }

        private enum SliderLoopChannel
        {
            Normal,
            Whistle
        }
    }
}