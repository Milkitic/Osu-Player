using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Player;
using OSharp.Beatmap;
using OSharp.Beatmap.Sections.HitObject;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OSharp.Beatmap.Sections.GamePlay;

namespace Milky.OsuPlayer.Media.Audio.Music
{
    internal class HitsoundPlayer : Player, IDisposable
    {
        static HitsoundPlayer()
        {
            CachedSound.CachePath = Path.Combine(Domain.CachePath, "_temp.sound");
        }

        public override int ProgressRefreshInterval { get; set; }

        private static bool UseSoundTouch => AppSettings.Current.Play.UsePlayerV2;

        public override PlayerStatus PlayerStatus
        {
            get => _playerStatus;
            protected set
            {
                Console.WriteLine(@"Hitsound: " + value);
                _playerStatus = value;
            }
        }

        public override int Duration { get; protected set; }

        public override int PlayTime
        {
            get => (int)(_sw.ElapsedMilliseconds * _multiplier + _controlOffset);
            protected set
            {
                _controlOffset = value;
                _sw.Reset();
            }
        }

        public int SingleOffset { get; set; }

        public bool IsPlaying => _playingTask != null &&
                                 !_playingTask.IsCanceled &&
                                 !_playingTask.IsCompleted &&
                                 !_playingTask.IsFaulted;

        public bool IsRunningDynamicOffset => _offsetTask != null &&
                                              !_offsetTask.IsCanceled &&
                                              !_offsetTask.IsCompleted &&
                                              !_offsetTask.IsFaulted;

        protected AudioPlaybackEngine Engine = new AudioPlaybackEngine();

        private readonly string _defaultDir = Domain.DefaultPath;
        private ConcurrentQueue<HitsoundElement> _hsQueue;
        private List<HitsoundElement> _hitsoundList;
        private readonly string _filePath;

        // Play Control
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private int _controlOffset;
        private Task _playingTask, _offsetTask;
        private float _multiplier = 1f;
        private readonly Stopwatch _sw = new Stopwatch();
        private PlayerStatus _playerStatus;
        private PlayMod _mod;
        private readonly OsuFile _osuFile;

        public HitsoundPlayer(string filePath, OsuFile osuFile)
        {
            _osuFile = osuFile;
            _filePath = filePath;
        }

        public override async Task InitializeAsync()
        {
            FileInfo fileInfo = new FileInfo(_filePath);
            DirectoryInfo dirInfo = fileInfo.Directory;
            if (!fileInfo.Exists)
                throw new FileNotFoundException("文件不存在：" + _filePath);
            if (dirInfo == null)
                throw new DirectoryNotFoundException("获取" + fileInfo.Name + "所在目录失败了？");

            List<HitsoundElement> hitsoundList = FillHitsoundList(_osuFile, dirInfo);
            _hitsoundList = hitsoundList.OrderBy(t => t.Offset).ToList(); // Sorted before enqueue.
            Requeue(0);
            var allPaths = hitsoundList.Select(t => t.FilePaths).SelectMany(sbx2 => sbx2).Distinct();
            await Task.Run(() =>
            {
                Engine.CreateCacheSounds(allPaths);
            });
            PlayerStatus = PlayerStatus.Ready;
            SetPlayMod(AppSettings.Current.Play.PlayMod, false);
            InitVolume();
            AppSettings.Current.Volume.PropertyChanged += Volume_PropertyChanged;
            RaisePlayerLoadedEvent(this, new EventArgs());
        }

        protected virtual void InitVolume()
        {
            Engine.Volume = 1f * AppSettings.Current.Volume.Hitsound * AppSettings.Current.Volume.Main;
        }

        protected virtual void Volume_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Engine.Volume = 1f * AppSettings.Current.Volume.Hitsound * AppSettings.Current.Volume.Main;
        }


        public void SetPlayMod(PlayMod mod, bool play)
        {
            _mod = mod;
            switch (mod)
            {
                case PlayMod.None:
                    SetTime(PlayTime, play);
                    _multiplier = 1f;
                    break;
                case PlayMod.DoubleTime:
                case PlayMod.NightCore:
                    SetTime(PlayTime, play);
                    _multiplier = 1.5f;
                    break;
                case PlayMod.HalfTime:
                case PlayMod.DayCore:
                    SetTime(PlayTime, play);
                    _multiplier = 0.75f;
                    break;
            }
        }

        public override void Play()
        {
            //if (IsPlaying)
            //    return;
            StartTask();
            //App.MusicPlayer.Play();
            if (_mod == PlayMod.None)
            {
                DynamicOffset();
            }
            _playingTask = new Task(PlayHitsound);
            _playingTask.Start();

            PlayerStatus = PlayerStatus.Playing;
            RaisePlayerStartedEvent(this, new ProgressEventArgs(PlayTime, Duration));
        }

        public override void Pause()
        {
            CancelTask(true);
            PlayTime = PlayTime;

            PlayerStatus = PlayerStatus.Paused;
            RaisePlayerPausedEvent(this, new ProgressEventArgs(PlayTime, Duration));
        }

        public override void Stop()
        {
            ResetWithoutNotify();
            RaisePlayerStoppedEvent(this, new EventArgs());
        }

        internal void ResetWithoutNotify(bool finished = false)
        {
            CancelTask(true);
            SetTimePurely(0);
            PlayerStatus = finished ? PlayerStatus.Finished : PlayerStatus.Stopped;
        }

        public override void Replay()
        {
            Stop();
            Play();
        }

        public override void SetTime(int ms, bool play = true)
        {
            Pause();
            SetTimePurely(ms);
        }

        private void SetTimePurely(int ms)
        {
            PlayTime = ms;
            Requeue(ms);
        }

        public override void Dispose()
        {
            base.Dispose();

            Stop();
            if (_playingTask != null)
                Task.WaitAll(_playingTask);
            _playingTask?.Dispose();
            _cts?.Dispose();
            Engine?.Dispose();

            AppSettings.Current.Volume.PropertyChanged -= Volume_PropertyChanged;

            GC.Collect();
        }

        internal void SetDuration(int musicPlayerDuration)
        {
            Duration = (int)Math.Ceiling(_hitsoundList.Count == 0
                ? 0
                : Math.Max(_hitsoundList.Max(k => k.Offset),
                    musicPlayerDuration)
            );
        }

        private void PlayHitsound()
        {
            _sw.Restart();
            while (_hsQueue.Count > 0 && _mod == PlayMod.None ||
                   ComponentPlayer.Current.MusicPlayer.PlayerStatus != PlayerStatus.Finished)
            {
                if (_cts.Token.IsCancellationRequested)
                {
                    _sw.Stop();
                    return;
                }

                // Loop
                if (_mod == PlayMod.None)
                {
                    while (_hsQueue.Count != 0 && _hsQueue.First().Offset <= PlayTime)
                    {
                        if (!_hsQueue.TryDequeue(out var hs))
                            continue;

                        foreach (var path in hs.FilePaths)
                        {
                            Engine.PlaySound(path, hs.Volume * 1f,
                                hs.Balance * AppSettings.Current.Volume.BalanceFactor / 100f);
                            //Task.Run(() =>);
                        }
                    }
                }

                Thread.Sleep(1);
            }

            SetTimePurely(0);
            CancelTask(false);

            PlayerStatus = PlayerStatus.Finished;
            Task.Run(() => { RaisePlayerFinishedEvent(this, new EventArgs()); });
        }

        private void DynamicOffset()
        {
            if (!IsRunningDynamicOffset)
            {
                _offsetTask = Task.Run(() =>
                {
                    Thread.Sleep(30);
                    int? preMs = null;
                    while (!_cts.IsCancellationRequested && ComponentPlayer.Current.MusicPlayer.PlayerStatus == PlayerStatus.Playing)
                    {
                        int playerMs = ComponentPlayer.Current.MusicPlayer.PlayTime;
                        if (playerMs != preMs)
                        {
                            preMs = playerMs;
                            var d = playerMs - (PlayTime + SingleOffset + (UseSoundTouch ? 480 : 0));
                            var r = AppSettings.Current.Play.GeneralOffset - d;
                            if (Math.Abs(r) > 5)
                            {
                                //Console.WriteLine($@"music: {App.MusicPlayer.PlayTime}, hs: {PlayTime}, {d}({r})");
                                _controlOffset -= (int)(r / 2f);
                            }
                        }

                        Thread.Sleep(10);
                    }
                }, _cts.Token);
            }
        }

        private void Requeue(long startTime)
        {
            _hsQueue = new ConcurrentQueue<HitsoundElement>();
            if (_hitsoundList != null)
                foreach (var i in _hitsoundList)
                {
                    if (i.Offset < startTime)
                        continue;
                    _hsQueue.Enqueue(i);
                }
        }

        private void StartTask()
        {
            _cts = new CancellationTokenSource();
        }

        private void CancelTask(bool waitPlayTask)
        {
            _cts?.Cancel();
            if (waitPlayTask && _playingTask != null)
                Task.WaitAll(_playingTask);
            if (_offsetTask != null)
                Task.WaitAll(_offsetTask);
            Console.WriteLine(@"Task canceled.");
        }

        #region Load

        protected virtual List<HitsoundElement> FillHitsoundList(OsuFile osuFile, DirectoryInfo dirInfo)
        {
            List<RawHitObject> hitObjects = _osuFile.HitObjects.HitObjectList;
            List<HitsoundElement> hitsoundList = new List<HitsoundElement>();

            var mapWaves = dirInfo.EnumerateFiles()
                .Where(k => k.Extension.ToLower() == ".wav" || k.Extension.ToLower() == ".ogg")
                .Select(p => Path.GetFileNameWithoutExtension(p.FullName))
                .ToArray();

            foreach (var obj in hitObjects)
            {
                if (obj.ObjectType == HitObjectType.Slider)
                {
                    foreach (var item in obj.SliderInfo.Edges)
                    {
                        //var currentTiming = file.TimingPoints.GetRedLine(item.Offset);
                        var currentLine = _osuFile.TimingPoints.GetLine(item.Offset);
                        float balance = GetObjectBalance(item.Point.X);

                        var element = new HitsoundElement(
                            mapFolderName: dirInfo.FullName,
                            mapWaveFiles: mapWaves,
                            gameMode: osuFile.General.Mode,
                            offset: item.Offset,
                            track: currentLine.Track,
                            lineSample: currentLine.TimingSampleset,
                            hitsound: item.EdgeHitsound,
                            sample: item.EdgeSample,
                            addition: item.EdgeAddition,
                            customFile: obj.FileName,
                            volume: (obj.SampleVolume != 0 ? obj.SampleVolume : currentLine.Volume) / 100f,
                            balance: balance
                        );

                        hitsoundList.Add(element);
                    }
                }
                else
                {
                    //var currentTiming = file.TimingPoints.GetRedLine(obj.Offset);
                    var currentLine = _osuFile.TimingPoints.GetLine(obj.Offset);
                    var offset = obj.Offset; //todo: Spinner & hold

                    float balance = GetObjectBalance(obj.X);

                    var element = new HitsoundElement(
                        mapFolderName: dirInfo.FullName,
                        mapWaveFiles: mapWaves,
                        gameMode: osuFile.General.Mode,
                        offset: offset,
                        track: currentLine.Track,
                        lineSample: currentLine.TimingSampleset,
                        hitsound: obj.Hitsound,
                        sample: obj.SampleSet,
                        addition: obj.AdditionSet,
                        customFile: obj.FileName,
                        volume: (obj.SampleVolume != 0 ? obj.SampleVolume : currentLine.Volume) / 100f,
                        balance: balance
                    );

                    hitsoundList.Add(element);
                }
            }

            return hitsoundList;

            float GetObjectBalance(float x)
            {
                if (osuFile.General.Mode != GameMode.Circle &&
                    osuFile.General.Mode != GameMode.Catch &&
                    osuFile.General.Mode != GameMode.Mania)
                {
                    return 1;
                }

                if (x > 512) x = 512;
                else if (x < 0) x = 0;

                float balance = (x - 256f) / 256f;
                return balance;
            }
        }

        #endregion Load
    }
}