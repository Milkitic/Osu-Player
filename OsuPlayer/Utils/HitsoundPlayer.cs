using Milkitic.OsuLib;
using Milkitic.OsuLib.Enums;
using Milkitic.OsuLib.Model.Raw;
using Milkitic.OsuPlayer.Interface;
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
    public class HitsoundPlayer : IPlayer, IDisposable
    {
        public PlayStatusEnum PlayStatus { get; private set; }
        public int Duration { get; }
        public int PlayTime
        {
            get => (int)_sw.ElapsedMilliseconds + _controlOffset;
            private set
            {
                _controlOffset = value;
                _sw.Reset();
            }
        }

        private readonly MusicPlayer _musicPlayer;
        private readonly string _defaultDir = Domain.DefaultPath;
        private ConcurrentQueue<HitsoundElement> _hsQueue;
        private readonly List<HitsoundElement> _hitsoundList;

        // Play Control
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private int _controlOffset;
        private Task _playingTask;

        private readonly Stopwatch _sw = new Stopwatch();
        private OsuFile _osufile;

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

            _musicPlayer = new MusicPlayer(Path.Combine(dirInfo.FullName, _osufile.General.AudioFilename));
            Duration = (int)Math.Ceiling(_hitsoundList.Max(k => k.Offset));
        }

        public void Play()
        {
            StartTask();
            _playingTask = new Task(() =>
            {
                _sw.Restart();
                while (_hsQueue.Count > 0)
                {
                    if (_cts.Token.IsCancellationRequested)
                    {
                        _sw.Stop();
                        return;
                    }

                    // Loop
                    while (_hsQueue.Count != 0 && _hsQueue.First().Offset <= PlayTime)
                    {
                        if (!_hsQueue.TryDequeue(out var hs))
                            continue;

                        foreach (var path in hs.FilePaths)
                            Task.Run(() => WavePlayer.PlayFile(path, hs.Volume));
                    }

                    Thread.Sleep(1);
                }

                PlayTime = 0;
                Requeue();
                PlayStatus = PlayStatusEnum.Stopped;
            });
            _playingTask.Start();
            PlayStatus = PlayStatusEnum.Playing;
        }

        public void Replay()
        {
            Stop();
            Play();
            _musicPlayer.Replay();
        }

        public void Pause()
        {
            CancelTask();
            PlayTime = PlayTime;
            PlayStatus = PlayStatusEnum.Paused;
            _musicPlayer.Pause();
        }

        public void Stop()
        {
            CancelTask();
            PlayTime = 0;
            PlayStatus = PlayStatusEnum.Stopped;
            Requeue();
            _musicPlayer.Stop();
        }

        public void SetTime(int ms, bool play = true)
        {
            CancelTask();
            PlayTime = ms;
            Requeue(ms);
            _musicPlayer.SetTime(ms);
            _musicPlayer.Pause();
            if (play)
            {
                Play();
                _musicPlayer.Play();
            }
        }

        private List<HitsoundElement> FillHitsoundList(string filePath, DirectoryInfo dirInfo)
        {
            _osufile = new OsuFile(filePath);
            List<RawHitObject> hitObjects = _osufile.HitObjects.HitObjectList;
            List<HitsoundElement> hitsoundList = new List<HitsoundElement>();

            var mapFiles = dirInfo.GetFiles("*.wav").Select(p => p.Name).ToArray();

            foreach (var obj in hitObjects)
            {
                if (obj.ObjectType == ObjectType.Slider)
                {
                    foreach (var item in obj.SliderInfo.Edges)
                    {
                        //var currentTiming = file.TimingPoints.GetRedLine(item.Offset);
                        var currentLine = _osufile.TimingPoints.GetLine(item.Offset);
                        var element = new HitsoundElement
                        {
                            GameMode = _osufile.General.Mode,
                            Offset = item.Offset,
                            Volume = (obj.SampleVolume != 0 ? obj.SampleVolume : currentLine.Volume) / 100f,
                            Hitsound = item.EdgeHitsound,
                            Sample = item.EdgeSample,
                            Addition = item.EdgeAddition,
                            Track = currentLine.Track,
                            LineSample = currentLine.SamplesetEnum,
                            CustomFile = obj.FileName,
                        };
                        SetFullPath(dirInfo, mapFiles, element);

                        hitsoundList.Add(element);
                    }
                }
                else
                {
                    //var currentTiming = file.TimingPoints.GetRedLine(obj.Offset);
                    var currentLine = _osufile.TimingPoints.GetLine(obj.Offset);
                    var offset = obj.Offset; //todo: spinner & hold

                    var element = new HitsoundElement
                    {
                        GameMode = _osufile.General.Mode,
                        Offset = offset,
                        Volume = (obj.SampleVolume != 0 ? obj.SampleVolume : currentLine.Volume) / 100f,
                        Hitsound = obj.Hitsound,
                        Sample = obj.SampleSet,
                        Addition = obj.AdditionSet,
                        Track = currentLine.Track,
                        LineSample = currentLine.SamplesetEnum,
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
            var files = element.FileNames;
            element.FilePaths = new string[files.Length];
            for (var i = 0; i < files.Length; i++)
            {
                var name = files[i];
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

        private void Requeue(long skippedMs = 0)
        {
            _hsQueue = new ConcurrentQueue<HitsoundElement>();
            foreach (var i in _hitsoundList)
            {
                if (i.Offset < skippedMs)
                    continue;
                _hsQueue.Enqueue(i);
            }
        }

        private void StartTask()
        {
            _cts = new CancellationTokenSource();
        }

        private void CancelTask()
        {
            _cts.Cancel();
            Task.WaitAny(_playingTask);
            Console.WriteLine(@"stopped");
        }

        public void Dispose()
        {
            Stop();
            Task.WaitAll(_playingTask);
            _playingTask?.Dispose();
            _cts?.Dispose();
            WavePlayer.ClearCache();
            GC.Collect();//why
        }


    }
}
