using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
            AddSubchannel(_musicChannel);
            await _musicChannel.Initialize();

            var hitsoundList = await GetHitsoundsAsync();
            _hitsoundChannel = new MultiElementsChannel(Engine, hitsoundList, _musicChannel)
            {
                Description = "Hitsound"
            };
            AddSubchannel(_hitsoundChannel);
            await _hitsoundChannel.Initialize();

            var sampleList = await GetSamplesAsync();
            _sampleChannel = new MultiElementsChannel(Engine, sampleList, _musicChannel)
            {
                Description = "Sample"
            };

            AddSubchannel(_sampleChannel);
            await _sampleChannel.Initialize();
        }

        private async Task<List<SoundElement>> GetSamplesAsync()
        {
            var elements = new List<SoundElement>();
            var samples = _osuFile.Events.SampleInfo;
            if (samples == null)
                return elements;

            await Task.Run(() =>
            {
                //foreach (var sample in samples)
                //{
                //    var element = SoundElement.Create(sample.Offset, sample.Volume / 100f, 0,
                //        GetFileUntilFind(_sourceFolder, Path.GetFileNameWithoutExtension(sample.Filename))
                //    );
                //    element.GetCachedSoundAsync().Wait();
                //    elements.Add(element);
                //}

                samples.AsParallel()
                    .WithDegreeOfParallelism(Environment.ProcessorCount > 1 ? Environment.ProcessorCount - 1 : 1)
                    .ForAll(sample =>
                    {
                        var element = SoundElement.Create(sample.Offset, sample.Volume / 100f, 0,
                            GetFileUntilFind(_sourceFolder, Path.GetFileNameWithoutExtension(sample.Filename))
                        );
                        element.GetCachedSoundAsync().Wait();
                        elements.Add(element);
                    });
            });

            return elements;
        }

        private async Task<List<SoundElement>> GetHitsoundsAsync()
        {
            List<RawHitObject> hitObjects = _osuFile.HitObjects.HitObjectList;
            var elements = new ConcurrentBag<SoundElement>();

            var dirInfo = new DirectoryInfo(_sourceFolder);
            var waves = new HashSet<string>(dirInfo.EnumerateFiles()
                .Where(k => AudioPlaybackEngine.SupportExtensions.Contains(
                    k.Extension, StringComparer.OrdinalIgnoreCase)
                )
                .Select(p => Path.GetFileNameWithoutExtension(p.Name))
            );

            await Task.Run(() =>
            {
                //foreach (var obj in hitObjects)
                //{
                //    AddSingleHitObject(obj, waves, elements).Wait();
                //}
                hitObjects.AsParallel()
                    .WithDegreeOfParallelism(Environment.ProcessorCount > 1 ? Environment.ProcessorCount - 1 : 1)
                    .ForAll(obj => { AddSingleHitObject(obj, waves, elements).Wait(); });
            });

            return new List<SoundElement>(elements);
        }

        private async Task AddSingleHitObject(RawHitObject obj, HashSet<string> waves, ConcurrentBag<SoundElement> elements)
        {
            if (obj.ObjectType != HitObjectType.Slider)
            {
                var itemOffset = obj.ObjectType == HitObjectType.Spinner
                    ? obj.HoldEnd // spinner
                    : obj.Offset; // hold & circle
                var timingPoint = _osuFile.TimingPoints.GetLine(itemOffset);

                float balance = GetObjectBalance(obj.X);
                float volume = GetObjectVolume(obj, timingPoint);

                var tuples = AnalyzeHitsoundFiles(obj.Hitsound, obj.SampleSet, obj.AdditionSet,
                    timingPoint, obj, waves);
                foreach (var (filePath, _) in tuples)
                {
                    var element = SoundElement.Create(itemOffset, volume, balance, filePath);
                    await element.GetCachedSoundAsync();
                    elements.Add(element);
                }
            }
            else // sliders
            {
                // edges
                foreach (var item in obj.SliderInfo.Edges)
                {
                    var itemOffset = item.Offset;
                    var timingPoint = _osuFile.TimingPoints.GetLine(itemOffset);

                    float balance = GetObjectBalance(item.Point.X);
                    float volume = GetObjectVolume(obj, timingPoint);

                    var tuples = AnalyzeHitsoundFiles(item.EdgeHitsound, item.EdgeSample, item.EdgeAddition,
                        timingPoint, obj, waves);
                    foreach (var (filePath, _) in tuples)
                    {
                        var element = SoundElement.Create(itemOffset, volume, balance, filePath);
                        await element.GetCachedSoundAsync();
                        elements.Add(element);
                    }
                }

                // ticks
                var ticks = obj.SliderInfo.Ticks;
                foreach (var sliderTick in ticks)
                {
                    var itemOffset = sliderTick.Offset;
                    var timingPoint = _osuFile.TimingPoints.GetLine(itemOffset);

                    float balance = GetObjectBalance(sliderTick.Point.X);
                    float volume = GetObjectVolume(obj, timingPoint) * 1.25f; // ticks x1.25

                    var (filePath, _) = AnalyzeHitsoundFiles(HitsoundType.Tick, obj.SampleSet, obj.AdditionSet,
                        timingPoint, obj, waves).First();

                    var element = SoundElement.Create(itemOffset, volume, balance, filePath);
                    await element.GetCachedSoundAsync();
                    elements.Add(element);
                }

                // sliding
                {
                    var slideElements = new List<SoundElement>();

                    var startOffset = obj.Offset;
                    var endOffset = obj.SliderInfo.Edges[obj.SliderInfo.Edges.Length - 1].Offset;
                    var timingPoint = _osuFile.TimingPoints.GetLine(startOffset);

                    float balance = GetObjectBalance(obj.X);
                    float volume = GetObjectVolume(obj, timingPoint);

                    // start sliding
                    var tuples = AnalyzeHitsoundFiles(
                        obj.Hitsound & HitsoundType.SlideWhistle | HitsoundType.Slide,
                        obj.SampleSet, obj.AdditionSet,
                        timingPoint, obj, waves);
                    foreach (var (filePath, hitsoundType) in tuples)
                    {
                        var element = SoundElement.CreateSlideSignal(startOffset, volume, balance, filePath,
                            hitsoundType);
                        await element.GetCachedSoundAsync();
                        slideElements.Add(element);
                    }

                    // change sample (will optimize if only adjust volume) by inherit timing point
                    var timingsOnSlider = _osuFile.TimingPoints.TimingList
                        .Where(k => k.Offset > startOffset && k.Offset < endOffset)
                        .ToList();

                    for (var i = 0; i < timingsOnSlider.Count; i++)
                    {
                        var timing = timingsOnSlider[i];
                        var prevTiming = i == 0 ? timingPoint : timingsOnSlider[i - 1];
                        if (timing.Track != prevTiming.Track ||
                            timing.TimingSampleset != prevTiming.TimingSampleset)
                        {
                            volume = GetObjectVolume(obj, timing);
                            tuples = AnalyzeHitsoundFiles(
                                obj.Hitsound & HitsoundType.SlideWhistle | HitsoundType.Slide,
                                obj.SampleSet, obj.AdditionSet,
                                timing, obj, waves);
                            foreach (var (filePath, hitsoundType) in tuples)
                            {
                                SoundElement element;
                                if (hitsoundType.HasFlag(HitsoundType.Slide) &&
                                    slideElements
                                        .Last(k => k.SlideType.HasFlag(HitsoundType.Slide))
                                        .FilePath == filePath)
                                {
                                    // optimize by only change volume
                                    element = SoundElement.CreateVolumeSignal(timing.Offset, volume);
                                }
                                else if (hitsoundType.HasFlag(HitsoundType.Slide) &&
                                         slideElements
                                             .Last(k => k.SlideType.HasFlag(HitsoundType.Slide))
                                             .FilePath == filePath)
                                {
                                    // optimize by only change volume
                                    element = SoundElement.CreateVolumeSignal(timing.Offset, volume);
                                }
                                else
                                {
                                    // new sample
                                    element = SoundElement.CreateSlideSignal(timing.Offset, volume, balance,
                                        filePath, hitsoundType);
                                }

                                await element.GetCachedSoundAsync();
                                slideElements.Add(element);
                            }

                            continue;
                        }

                        // optimize useless timing point
                        timingsOnSlider.RemoveAt(i);
                        i--;
                    }

                    // end slide
                    var stopElement = SoundElement.CreateStopSignal(endOffset);
                    slideElements.Add(stopElement);
                    foreach (var slideElement in slideElements)
                    {
                        elements.Add(slideElement);
                    }
                }

                // change balance while sliding (not supported in original game)
                var trails = obj.SliderInfo.BallTrail;
                var all = trails
                    .Select(k => new
                    {
                        offset = k.Offset,
                        balance = GetObjectBalance(k.Point.X)
                    })
                    .Select(k => SoundElement.CreateBalanceSignal(k.offset, k.balance));
                foreach (var balanceElement in all)
                {
                    elements.Add(balanceElement);
                }
            }
        }

        private IEnumerable<(string, HitsoundType)> AnalyzeHitsoundFiles(
            HitsoundType itemHitsound, ObjectSamplesetType itemSample, ObjectSamplesetType itemAddition,
            TimingPoint timingPoint, RawHitObject hitObject,
            HashSet<string> waves)
        {
            if (!string.IsNullOrEmpty(hitObject.FileName))
            {
                return new[]
                {
                    ValueTuple.Create(
                        GetFileUntilFind(_sourceFolder, Path.GetFileNameWithoutExtension(hitObject.FileName)),
                        itemHitsound
                    )
                };
            }

            var tuples = new List<(string, HitsoundType)>();

            // hitnormal, sliderslide
            var sampleStr = itemSample != ObjectSamplesetType.Auto
                ? itemSample.ToHitsoundString(null)
                : timingPoint.TimingSampleset.ToHitsoundString();

            // hitclap, hitfinish, hitwhistle, slidertick, sliderwhistle
            string additionStr = itemAddition.ToHitsoundString(sampleStr);

            if (hitObject.ObjectType == HitObjectType.Slider && hitObject.SliderInfo.EdgeHitsounds == null)
            {
                var hitsounds = GetHitsounds(itemHitsound, sampleStr, additionStr);
                tuples.AddRange(hitsounds);
            }
            else
            {
                var hitsounds = GetHitsounds(itemHitsound, sampleStr, additionStr);
                tuples.AddRange(_osuFile.General.Mode == GameMode.Mania
                    ? hitsounds.Take(1)
                    : hitsounds);
            }

            for (var i = 0; i < tuples.Count; i++)
            {
                var fileNameWithoutIndex = tuples[i].Item1;
                var hitsoundType = tuples[i].Item2;

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

                tuples[i] = (filePath, hitsoundType);
            }

            return tuples;
        }

        private readonly ConcurrentDictionary<string, string> _pathCache = new ConcurrentDictionary<string, string>();

        private string GetFileUntilFind(string sourceFolder, string fileNameWithoutExtension)
        {
            var combine = Path.Combine(sourceFolder, fileNameWithoutExtension);
            if (_pathCache.TryGetValue(combine, out var value))
            {
                return value;
            }

            string path = "";
            foreach (var extension in AudioPlaybackEngine.SupportExtensions)
            {
                path = Path.Combine(sourceFolder, fileNameWithoutExtension + extension);

                if (File.Exists(path))
                {
                    _pathCache.TryAdd(combine, path);
                    return path;
                }
            }

            _pathCache.TryAdd(combine, path);
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

        private static float GetObjectVolume(RawHitObject obj, TimingPoint timingPoint)
        {
            return (obj.SampleVolume != 0 ? obj.SampleVolume : timingPoint.Volume) / 100f;
        }

        private static IEnumerable<(string, HitsoundType)> GetHitsounds(HitsoundType type,
            string sampleStr, string additionStr)
        {
            if (type == HitsoundType.Tick)
            {
                yield return ($"{additionStr}-slidertick", type);
                yield break;
            }

            if (type.HasFlag(HitsoundType.Slide))
                yield return ($"{sampleStr}-sliderslide", type);
            if (type.HasFlag(HitsoundType.SlideWhistle))
                yield return ($"{additionStr}-sliderwhistle", type);

            if (type.HasFlag(HitsoundType.Slide) || type.HasFlag(HitsoundType.SlideWhistle))
                yield break;

            if (type.HasFlag(HitsoundType.Whistle))
                yield return ($"{additionStr}-hitwhistle", type);
            if (type.HasFlag(HitsoundType.Clap))
                yield return ($"{additionStr}-hitclap", type);
            if (type.HasFlag(HitsoundType.Finish))
                yield return ($"{additionStr}-hitfinish", type);
            if (type.HasFlag(HitsoundType.Normal) ||
                (type & HitsoundType.Normal) == 0)
                yield return ($"{sampleStr}-hitnormal", type);
        }
    }
}