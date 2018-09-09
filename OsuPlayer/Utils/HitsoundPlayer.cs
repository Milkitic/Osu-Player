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
        private Task _playingTask, _offsetTask;

        private readonly Stopwatch _sw = new Stopwatch();
        public OsuFile Osufile { get; private set; }

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

            FileInfo musicInfo = new FileInfo(Path.Combine(dirInfo.FullName, Osufile.General.AudioFilename));
            _musicPlayer = new MusicPlayer(musicInfo.FullName);
            Duration = (int)Math.Ceiling(Math.Max(_hitsoundList.Max(k => k.Offset), _musicPlayer.Duration));
        }

        public void Play()
        {
            StartTask();
            _musicPlayer.Play();
            DynamicOffset();
            _playingTask = new Task(PlayHitsound);
            _playingTask.Start();
            PlayStatus = PlayStatusEnum.Playing;
        }

        private void PlayHitsound()
        {
            _sw.Restart();
            while (_hsQueue.Count > 0 || _musicPlayer.PlayStatus != PlayStatusEnum.Stopped)
            {
                if (_cts.Token.IsCancellationRequested)
                {
                    _sw.Stop();
                    return;
                }

                // Loop
                while (_hsQueue.Count != 0 && _hsQueue.First().Offset <= PlayTime)
                {
                    if (!_hsQueue.TryDequeue(out var hs)) continue;

                    foreach (var path in hs.FilePaths) Task.Run(() => WavePlayer.PlayFile(path, hs.Volume));
                }

                Thread.Sleep(1);
            }

            InnerStop(true);
        }

        public void Replay()
        {
            Stop();
            Play();
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
            InnerStop();
        }

        public void SetTime(int ms, bool play = true)
        {
            Pause();
            PlayTime = ms;
            Requeue(ms);
            if (play)
            {
                _musicPlayer.SetTime(ms);
                Play();
            }
            else
                _musicPlayer.SetTime(ms, false);

        }

        private void DynamicOffset()
        {
            if (_offsetTask == null || _offsetTask.IsCanceled || _offsetTask.IsCompleted)
                _offsetTask = Task.Run(() =>
                {
                    Thread.Sleep(10);
                    int? preMs = null;
                    while (!_cts.IsCancellationRequested && _musicPlayer.PlayStatus == PlayStatusEnum.Playing)
                    {
                        int playerMs = _musicPlayer.PlayTime;
                        if (playerMs != preMs)
                        {
                            preMs = playerMs;
                            var d = playerMs - (_sw.ElapsedMilliseconds + _controlOffset);
                            var r = Core.Config.Offset - d;
                            if (Math.Abs(r) > 5)
                            {
                                Console.WriteLine($@"music: {_musicPlayer.PlayTime}, hs: {PlayTime}, {d}({r})");
                                _controlOffset -= (int)(r / 2f);
                            }
                        }

                        Thread.Sleep(10);
                    }
                }, _cts.Token);
        }

        private void InnerStop(bool innerCall = false)
        {
            CancelTask(innerCall);
            PlayTime = 0;
            PlayStatus = PlayStatusEnum.Stopped;
            Requeue();
            _musicPlayer.Stop();
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

        private void CancelTask(bool innerCall = false)
        {
            _cts.Cancel();
            if (!innerCall) Task.WaitAll(_playingTask);
            Task.WaitAll(_offsetTask);
            Console.WriteLine(@"Task canceled.");
        }

        public void Dispose()
        {
            Stop();
            Task.WaitAll(_playingTask);
            _playingTask?.Dispose();
            _musicPlayer?.Dispose();
            _cts?.Dispose();
            WavePlayer.ClearCache();
            GC.Collect();
        }

        #region Load

        private List<HitsoundElement> FillHitsoundList(string filePath, DirectoryInfo dirInfo)
        {
            try
            {
                Osufile = new OsuFile(filePath);
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (FormatException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new Exception("载入时发生未知错误。", e);
            }
            List<RawHitObject> hitObjects = Osufile.HitObjects.HitObjectList;
            List<HitsoundElement> hitsoundList = new List<HitsoundElement>();

            var mapFiles = dirInfo.GetFiles("*.wav").Select(p => p.Name).ToArray();

            foreach (var obj in hitObjects)
            {
                if (obj.ObjectType == ObjectType.Slider)
                {
                    foreach (var item in obj.SliderInfo.Edges)
                    {
                        //var currentTiming = file.TimingPoints.GetRedLine(item.Offset);
                        var currentLine = Osufile.TimingPoints.GetLine(item.Offset);
                        var element = new HitsoundElement
                        {
                            GameMode = Osufile.General.Mode,
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
                    var currentLine = Osufile.TimingPoints.GetLine(obj.Offset);
                    var offset = obj.Offset; //todo: spinner & hold

                    var element = new HitsoundElement
                    {
                        GameMode = Osufile.General.Mode,
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

        #endregion Load


    }
}
