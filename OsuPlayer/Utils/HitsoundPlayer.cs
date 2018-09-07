using Milkitic.OsuLib;
using Milkitic.OsuLib.Enums;
using Milkitic.OsuLib.Model.Raw;
using Milkitic.OsuPlayer.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Milkitic.OsuPlayer.Utils
{
    internal class HitsoundPlayer:IDisposable
    {
        public bool IsWorking => _playingTask != null && !_playingTask.IsCompleted && !_playingTask.IsCanceled;
        private readonly string _defaultDir = Domain.DefaultPath;
        private ConcurrentQueue<HitsoundElement> _hsQueue;
        private readonly List<HitsoundElement> _hitsoundList;

        // Play Control
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private long _playOffset;
        private Task _playingTask;

        private readonly Stopwatch _sw = new Stopwatch();

        public HitsoundPlayer(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            DirectoryInfo dirInfo = fileInfo.Directory;
            if (!fileInfo.Exists)
                throw new FileNotFoundException("文件不存在：" + filePath);
            if (dirInfo == null)
                throw new DirectoryNotFoundException("获取" + fileInfo.Name + "所在目录失败了？");

            List<HitsoundElement> hitsoundList = FillHitsoundList(filePath, dirInfo);

            _hitsoundList = hitsoundList.OrderBy(t => t.Offset).ToList(); // Sorted before enqueue.
            Requeue();

            List<string> allPaths = hitsoundList.Select(t => t.FilePaths).SelectMany(sbx2 => sbx2).Distinct().ToList();
            foreach (var path in allPaths)
                WavePlayer.SaveToCache(path); // Cache each file once before play.
        }

        public void Play()
        {
            while (IsWorking)
                Stop();

            _playingTask = new Task(() =>
            {
                _sw.Restart();
                while (_hsQueue.Count > 0)
                {
                    var current = _playOffset + _sw.ElapsedMilliseconds;
                    // Loop
                    while (_hsQueue.Count != 0 && _hsQueue.First().Offset <= current)
                    {
                        if (!_hsQueue.TryDequeue(out var hs))
                            continue;

                        foreach (var path in hs.FilePaths)
                        {
                            if (_cts.Token.IsCancellationRequested)
                            {
                                _sw.Stop();
                                _playOffset = 0;
                                return;
                            }

                            WavePlayer.PlayFile(path, hs.Volume);
                        }
                    }

                    Thread.Sleep(1);
                }
            });
            _playingTask.Start();
        }

        public void Stop()
        {
            _cts.Cancel();
        }

        public void SetTime(long ms)
        {
            Stop();
            _playOffset = ms;
            Requeue(ms);
        }

        private void Requeue(long skippedMs = 0)
        {
            _hsQueue = new ConcurrentQueue<HitsoundElement>();
            foreach (var i in _hitsoundList)
            {
                if (i.Offset < skippedMs)
                    return;
                _hsQueue.Enqueue(i);
            }
        }

        private List<HitsoundElement> FillHitsoundList(string filePath, DirectoryInfo dirInfo)
        {
            OsuFile file = new OsuFile(filePath);
            List<RawHitObject> hitObjects = file.HitObjects.HitObjectList;
            List<HitsoundElement> hitsoundList = new List<HitsoundElement>();

            var mapFiles = dirInfo.GetFiles("*.wav").Select(p => p.Name).ToArray();

            foreach (var obj in hitObjects)
            {
                if (obj.ObjectType == ObjectType.Slider)
                {
                    foreach (var item in obj.SliderInfo.Edges)
                    {
                        var currentTiming = file.TimingPoints.GetRedLine(item.Offset);
                        var currentLine = file.TimingPoints.GetLine(item.Offset);
                        var element = new HitsoundElement
                        {
                            GameMode = file.General.Mode,
                            Offset = item.Offset,
                            Volume = (obj.SampleVolume != 0 ? obj.SampleVolume : currentLine.Volume) / 100f,
                            Hitsound = item.EdgeHitsound,
                            Sample = item.EdgeSample,
                            Addition = item.EdgeAddition,
                            Track = currentLine.Track,
                            LineSample = currentTiming.SamplesetEnum,
                            CustomFile = obj.FileName,
                        };
                        SetFullPath(dirInfo, mapFiles, element);

                        hitsoundList.Add(element);
                    }
                }
                else
                {
                    var currentTiming = file.TimingPoints.GetRedLine(obj.Offset);
                    var currentLine = file.TimingPoints.GetLine(obj.Offset);
                    var offset = obj.Offset; //todo: spinner & hold

                    var element = new HitsoundElement
                    {
                        GameMode = file.General.Mode,
                        Offset = offset,
                        Volume = (obj.SampleVolume != 0 ? obj.SampleVolume : currentLine.Volume) / 100f,
                        Hitsound = obj.Hitsound,
                        Sample = obj.SampleSet,
                        Addition = obj.AdditionSet,
                        Track = currentLine.Track,
                        LineSample = currentTiming.SamplesetEnum,
                        CustomFile = obj.FileName,
                    };
                    SetFullPath(dirInfo, mapFiles, element);
                    hitsoundList.Add(element);
                }
            }

            return hitsoundList;
        }

        private void SetFullPath(DirectoryInfo dirInfo, string[] mapFiles, HitsoundElement element)
        {
            element.FilePaths = new string[element.FileNames.Length];
            for (var i = 0; i < element.FileNames.Length; i++)
            {
                var name = element.FileNames[i];
                if (!mapFiles.Contains(name) || element.Track == 0 && string.IsNullOrEmpty(element.CustomFile))
                {
                    element.FilePaths[i] = Path.Combine(_defaultDir, element.DefaultFileNames[i]);
                }
                else
                {
                    element.FilePaths[i] = Path.Combine(dirInfo.FullName, name);
                }
            }
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
            _playingTask?.Dispose();
        }
    }
}
