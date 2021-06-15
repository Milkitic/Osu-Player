using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Milki.Extensions.MixPlayer;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.Subchannels;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Shared;
using OSharp.Beatmap;
using OSharp.Beatmap.Sections.GamePlay;
using OSharp.Beatmap.Sections.HitObject;
using OSharp.Beatmap.Sections.Timing;

namespace Milky.OsuPlayer.Media.Audio
{
    public class HitsoundChannel : MultiElementsChannel
    {
        private readonly OsuMixPlayer _player;
        private readonly OsuFile _osuFile;
        private readonly string _sourceFolder;

        public HitsoundChannel(OsuMixPlayer player, OsuFile osuFile, string sourceFolder, AudioPlaybackEngine engine)
            : base(engine)
        {
            _player = player;
            _osuFile = osuFile;
            _sourceFolder = sourceFolder;

            Description = nameof(HitsoundChannel);
        }

        public override async Task<IEnumerable<SoundElement>> GetSoundElements()
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
                hitObjects.AsParallel()
                    .WithDegreeOfParallelism(/*Environment.ProcessorCount > 1 ? Environment.ProcessorCount - 1 :*/ 1)
                    .ForAll(obj => { AddSingleHitObject(obj, waves, elements).Wait(); });
            }).ConfigureAwait(false);

            return new List<SoundElement>(elements);
        }

        private async Task AddSingleHitObject(RawHitObject obj, HashSet<string> waves,
            ConcurrentBag<SoundElement> elements)
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

                    var hs = obj.Hitsound == HitsoundType.Normal ? item.EdgeHitsound : obj.Hitsound | item.EdgeHitsound;
                    var addition = item.EdgeAddition == ObjectSamplesetType.Auto ? obj.AdditionSet : item.EdgeAddition;
                    var sample = item.EdgeSample == ObjectSamplesetType.Auto ? obj.SampleSet : item.EdgeSample;
                    var tuples = AnalyzeHitsoundFiles(hs, sample, addition,
                        timingPoint, obj, waves);
                    foreach (var (filePath, _) in tuples)
                    {
                        var element = SoundElement.Create(itemOffset, volume, balance, filePath);
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

            await Task.CompletedTask;
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
                        _player.GetFileUntilFind(_sourceFolder,
                            Path.GetFileNameWithoutExtension(hitObject.FileName)),
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
                    filePath = _player.GetFileUntilFind(_sourceFolder, fileNameWithoutExt);
                else
                    filePath = Path.Combine(Domain.DefaultPath, fileNameWithoutIndex + AudioPlaybackEngine.WavExtension);

                tuples[i] = (filePath, hitsoundType);
            }

            return tuples;
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