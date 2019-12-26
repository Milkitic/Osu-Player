using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Player;
using OSharp.Beatmap;
using OSharp.Beatmap.Sections.HitObject;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Milky.OsuPlayer.Media.Audio.Music.SampleProviders;
using Milky.OsuPlayer.Media.Audio.Music.WaveProviders;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OSharp.Beatmap.Sections.GamePlay;
using OSharp.Beatmap.Sections.Timing;

namespace Milky.OsuPlayer.Media.Audio.Music
{
    internal class HitsoundPlayer : Player, IDisposable
    {
        protected virtual string Flag { get; } = "Hitsound";

        public override int ProgressRefreshInterval { get; set; }

        private static bool UseSoundTouch => AppSettings.Default.Play.UsePlayerV2;

        public override PlayerStatus PlayerStatus
        {
            get => _playerStatus;
            protected set
            {
                Console.WriteLine(Flag + ": " + value);
                _playerStatus = value;
            }
        }

        public override int Duration { get; protected set; }

        public override int PlayTime
        {
            get => (int)(_sw.ElapsedMilliseconds * _multiplier + _controlOffset); // _multiplier播放速率
            protected set
            {
                _controlOffset = value;
                _sw.Reset(); // sw是秒表
            }
        }

        public int SingleOffset { get; set; }

        public bool IsPlaying => _playingTask != null &&
                                 !_playingTask.IsCanceled &&
                                 !_playingTask.IsCompleted &&
                                 !_playingTask.IsFaulted && PlayerStatus == PlayerStatus.Playing;

        public bool IsRunningDynamicOffset => _offsetTask != null &&
                                              !_offsetTask.IsCanceled &&
                                              !_offsetTask.IsCompleted &&
                                              !_offsetTask.IsFaulted;


        //private LoopStream _slideLoop;
        //private CachedSoundSampleProvider _slideSound;
        //private CachedSoundSampleProvider _slideAddSound;
        private ChannelSampleProvider _currentChannel;
        private VolumeSampleProvider _currentVolume;
        private ChannelSampleProvider _currentChannelAdd;
        private VolumeSampleProvider _currentVolumeAdd;

        private readonly string _defaultDir = Domain.DefaultPath;
        private ConcurrentQueue<SoundElement> _hsQueue;
        private List<SoundElement> _hitsoundList;
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
        protected readonly AudioPlaybackEngine Engine;

        private int _dcOffset;
        private bool? _useTempo;

        public HitsoundPlayer(AudioPlaybackEngine engine, string filePath, OsuFile osuFile)
        {
            Engine = engine;
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

            List<SoundElement> hitsoundList = FillHitsoundList(_osuFile, dirInfo);
            _hitsoundList = hitsoundList.OrderBy(t => t.Offset).ToList(); // Sorted before enqueue.
            Requeue(0);
            var allPaths = hitsoundList.Select(t => t.FilePaths).SelectMany(sbx2 => sbx2).Distinct();
            //_outputDevice = DeviceProvider.CreateOrGetDefaultDevice();
            //Engine = new AudioPlaybackEngine(_outputDevice);
            await Task.Run(() =>
            {
                Engine.CreateCacheSounds(allPaths);
                Engine.CreateCacheSounds(new DirectoryInfo(Domain.DefaultPath).GetFiles().Select(k => k.FullName));
            });
            PlayerStatus = PlayerStatus.Ready;
            SetTempoMode(AppSettings.Default.Play.PlayUseTempo);
            SetPlaybackRate(AppSettings.Default.Play.PlaybackRate, false);
            InitVolume();
            AppSettings.Default.Volume.PropertyChanged += Volume_PropertyChanged;
            //AppSettings.Default.Play.PropertyChanged += Play_PropertyChanged;
            RaisePlayerLoadedEvent(this, new EventArgs());
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

        internal void ResetWithoutNotify(bool finished = false)
        {
            CancelTask(true);
            SetTimePurely(0);
            PlayerStatus = finished ? PlayerStatus.Finished : PlayerStatus.Stopped;
        }

        internal void SetDuration(int musicPlayerDuration)
        {
            var enumerable = _hitsoundList.Select(k =>
            {
                var arr = k.FilePaths.Select(o => (Engine.GetOrCreateCacheSound(o)?.Duration ?? 0) + k.Offset).ToArray();
                return arr.Any() ? arr.Max() : 0;
            }).ToArray();
            var hitsoundDuration = enumerable.Any() ? enumerable.Max() : 0;

            //Duration = (int)Math.Ceiling(_hitsoundList.Count == 0
            //    ? 0
            //    : Math.Max(_hitsoundList.Max(k => k.Offset),
            //        musicPlayerDuration)
            //);
            Duration = (int)Math.Ceiling(_hitsoundList.Count == 0
                ? musicPlayerDuration
                : Math.Max(hitsoundDuration, musicPlayerDuration)
            );
        }

        internal void SetTempoMode(bool useTempo)
        {
            _useTempo = useTempo;
            AdjustModOffset();
        }

        internal void SetPlaybackRate(float rate, bool b)
        {
            _multiplier = rate;
            AdjustModOffset();
        }

        private void AdjustModOffset()
        {
            if (Math.Abs(_multiplier - 0.75) < 0.001 && _useTempo == false)
            {
                _dcOffset = -25;
            }
            else if (Math.Abs(_multiplier - 1.5) < 0.001 && _useTempo == true)
            {
                _dcOffset = 15;
            }
            else
            {
                _dcOffset = 0;
            }
        }

        protected virtual void InitVolume()
        {
            Engine.HitsoundVolume = 1f * AppSettings.Default.Volume.Hitsound * AppSettings.Default.Volume.Main;
        }

        protected virtual void Volume_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Engine.HitsoundVolume = 1f * AppSettings.Default.Volume.Hitsound * AppSettings.Default.Volume.Main;
        }

        //private void Play_PropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    switch (e.PropertyName)
        //    {
        //        case nameof(AppSettings.Play.PlaybackRate):
        //            SetPlaybackRate(AppSettings.Default.Play.PlaybackRate,);
        //            break;
        //    }
        //}

        private void SetTimePurely(int ms)
        {
            PlayTime = ms;
            Requeue(ms);
        }

        private void PlayHitsound()
        {
            var isHitsound = !(this is SampleTrackPlayer);
            _sw.Restart();
            while (_hsQueue.Count > 0 && _mod == PlayMod.None ||
                   ComponentPlayer.Current.MusicPlayer.PlayerStatus != PlayerStatus.Finished)
            {
                if (_cts.Token.IsCancellationRequested)
                {
                    _sw.Stop();
                    return;
                }

                //_sw.Start();

                // Loop
                if (_mod == PlayMod.None)
                {
                    while (_hsQueue.Count != 0 && _hsQueue.First().Offset <= PlayTime)
                    {
                        if (!_hsQueue.TryDequeue(out var hs))
                            continue;

                        if (hs is HitsoundElement he)
                        {
                            foreach (var path in he.FilePaths)
                            {
                                Engine.PlaySound(path, he.Volume * 1f,
                                    he.Balance * AppSettings.Default.Volume.BalanceFactor / 100f, isHitsound);
                                //Task.Run(() =>);
                            }
                        }
                        else if (hs is SlideControlElement sce)
                        {
                            var path = sce.FilePaths[0];

                            switch (sce.ControlMode)
                            {
                                case SlideControlMode.NewSample:
                                    {
                                        Engine.RemoveHitsoundSample(_currentChannel);
                                        Engine.RemoveHitsoundSample(_currentChannelAdd);
                                        //var sound = sce.IsAddition ? NewProviderAndRet(ref _slideAddSound, path) : NewProviderAndRet(ref _slideSound, path);
                                        //var s = new RawSourceWaveStream(
                                        //    sound.SourceSound.AudioData.Select(k => (byte)k).ToArray(), 0,
                                        //    sound.SourceSound.AudioData.Length, sound.WaveFormat);
                                        var myf = new WaveFileReader(path);
                                        var loop = new LoopStream(myf);
                                        if (!sce.IsAddition)
                                        {
                                            _currentVolume = new VolumeSampleProvider(loop.ToSampleProvider());
                                            _currentVolume.Volume = sce.Volume;
                                            _currentChannel = new ChannelSampleProvider(_currentVolume);
                                            _currentChannel.Balance = sce.Balance * AppSettings.Default.Volume.BalanceFactor / 100f;
                                            Engine.AddHitsoundSample(_currentChannel);
                                        }
                                        else
                                        {
                                            _currentVolumeAdd = new VolumeSampleProvider(loop.ToSampleProvider());
                                            _currentVolumeAdd.Volume = sce.Volume;
                                            _currentChannelAdd = new ChannelSampleProvider(_currentVolumeAdd);
                                            _currentChannelAdd.Balance = sce.Balance * AppSettings.Default.Volume.BalanceFactor / 100f;
                                            Engine.AddHitsoundSample(_currentChannelAdd);
                                        }
                                    }
                                    break;
                                case SlideControlMode.ChangeBalance:
                                    if (_currentChannel != null)
                                    {
                                        _currentChannel.Balance = sce.Balance * AppSettings.Default.Volume.BalanceFactor / 100f;
                                    }

                                    if (_currentChannelAdd != null)
                                    {
                                        _currentChannelAdd.Balance = sce.Balance * AppSettings.Default.Volume.BalanceFactor / 100f;
                                    }
                                    break;
                                case SlideControlMode.Volume:
                                    if (_currentChannel != null && !sce.IsAddition)
                                    {
                                        _currentVolume.Volume = sce.Volume;
                                    }

                                    if (_currentChannelAdd != null && sce.IsAddition)
                                    {
                                        _currentVolumeAdd.Volume = sce.Volume;
                                    }
                                    break;
                                case SlideControlMode.Stop:
                                    {
                                        Engine.RemoveHitsoundSample(_currentChannel);
                                        Engine.RemoveHitsoundSample(_currentChannelAdd);
                                        break;
                                    }
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
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
                        int nowMs = ComponentPlayer.Current.MusicPlayer.PlayTime;
                        if (nowMs != preMs) // 音乐play time变动
                        {
                            preMs = nowMs;
                            var d = nowMs - (PlayTime + SingleOffset + _dcOffset); // Single：单曲offset（人工调），PlayTime：音效play time
                            var r = AppSettings.Default.Play.GeneralOffset - d; // General：全局offset
                            if (Math.Abs(r) > 5)
                            {
                                //Console.WriteLine($@"music: {App.MusicPlayer.PlayTime}, hs: {PlayTime}, {d}({r})");
                                _controlOffset -= (int)(r / 2f); // 计算音效偏移量
                            }
                        }

                        Thread.Sleep(10);
                    }
                }, _cts.Token);
            }
        }

        private void Requeue(long startTime)
        {
            _hsQueue = new ConcurrentQueue<SoundElement>();
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

        protected virtual List<SoundElement> FillHitsoundList(OsuFile osuFile, DirectoryInfo dirInfo)
        {
            List<RawHitObject> hitObjects = _osuFile.HitObjects.HitObjectList;
            List<SoundElement> hitsoundList = new List<SoundElement>();

            HashSet<string> mapWaves = new HashSet<string>(dirInfo.EnumerateFiles()
                .Where(k => k.Extension.ToLower() == ".wav" || k.Extension.ToLower() == ".ogg")
                .Select(p => Path.GetFileNameWithoutExtension(p.FullName)));

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
                            balance: balance,
                            forceTrack: 0,
                            fullHitsoundType: obj.SliderInfo.EdgeHitsounds == null ? obj.Hitsound : (HitsoundType?)null
                        );

                        hitsoundList.Add(element);
                    }

                    var ticks = obj.SliderInfo.Ticks;
                    foreach (var sliderTick in ticks)
                    {
                        var currentLine = _osuFile.TimingPoints.GetLine(sliderTick.Offset);
                        float balance = GetObjectBalance(sliderTick.Point.X);

                        var element = new HitsoundElement(
                            mapFolderName: dirInfo.FullName,
                            mapWaveFiles: mapWaves,
                            gameMode: osuFile.General.Mode,
                            offset: sliderTick.Offset,
                            track: currentLine.Track,
                            lineSample: currentLine.TimingSampleset,
                            isTick: true,
                            sample: obj.SampleSet,
                            addition: obj.AdditionSet,
                            volume: (obj.SampleVolume != 0 ? obj.SampleVolume : currentLine.Volume) / 100f * 1.25f,
                            balance: balance,
                            forceTrack: obj.CustomIndex,
                            fullHitsoundType: obj.SliderInfo.EdgeHitsounds == null ? obj.Hitsound : (HitsoundType?)null
                        );

                        hitsoundList.Add(element);
                    }

                    // slide
                    {
                        var start = obj.Offset;
                        var end = obj.SliderInfo.Edges.Last().Offset;

                        var currentLine = _osuFile.TimingPoints.GetLine(start);
                        var volume = (obj.SampleVolume != 0 ? obj.SampleVolume : currentLine.Volume) / 100f;
                        float balance = GetObjectBalance(obj.X);
                        var track = currentLine.Track;
                        var lineSample = currentLine.TimingSampleset;
                        var sample = obj.SampleSet;
                        var addition = obj.AdditionSet;
                        var forceTrack = obj.CustomIndex;

                        hitsoundList.Add(new SlideControlElement(dirInfo.FullName,
                            mapWaves, start, volume, balance, track, lineSample, sample, addition, forceTrack,
                            SlideControlMode.NewSample, false));
                        if (obj.Hitsound.HasFlag(HitsoundType.Whistle))
                        {
                            hitsoundList.Add(new SlideControlElement(dirInfo.FullName,
                                mapWaves, start, volume, balance, track, lineSample, sample, addition, forceTrack,
                                SlideControlMode.NewSample, true));
                        }

                        var timings = _osuFile.TimingPoints.TimingList.Where(k => k.Offset > start && k.Offset < end)
                            .ToList();
                        for (int i = 0; i < timings.Count; i++)
                        {
                            var timing = timings[i];
                            var prevTiming = i == 0 ? currentLine : timings[i - 1];
                            if (timing.Track != prevTiming.Track || timing.TimingSampleset != prevTiming.TimingSampleset)
                            {
                                var volume2 = (obj.SampleVolume != 0 ? obj.SampleVolume : timing.Volume) / 100f;
                                var track2 = timing.Track;
                                var slideControlElement = new SlideControlElement(dirInfo.FullName,
                                    mapWaves, (int)timing.Offset, volume2, balance, track2, timing.TimingSampleset, sample, addition, forceTrack,
                                    SlideControlMode.NewSample, false);
                                if (slideControlElement.FilePaths.First() ==
                                    hitsoundList.OfType<SlideControlElement>().Last(k => !k.IsAddition).FilePaths.First())
                                {
                                    slideControlElement.ControlMode = SlideControlMode.Volume;
                                }

                                hitsoundList.Add(slideControlElement);
                                if (obj.Hitsound.HasFlag(HitsoundType.Whistle))
                                {
                                    var controlElement = new SlideControlElement(dirInfo.FullName,
                                        mapWaves, (int)timing.Offset, volume2, balance, track2, timing.TimingSampleset, sample, addition, forceTrack,
                                        SlideControlMode.NewSample, true);
                                    if (controlElement.FilePaths.First() ==
                                        hitsoundList.OfType<SlideControlElement>().Last(k => k.IsAddition).FilePaths.First())
                                    {
                                        controlElement.ControlMode = SlideControlMode.Volume;
                                    }

                                    hitsoundList.Add(controlElement);
                                }

                                continue;
                            }

                            timings.RemoveAt(i);
                            i--;
                        }

                        hitsoundList.Add(new SlideControlElement(dirInfo.FullName,
                           mapWaves, (int)end, volume, balance, track, lineSample, sample, addition, forceTrack,
                           SlideControlMode.Stop, false));
                    }

                    var trails = obj.SliderInfo.BallTrail;
                    foreach (var trailFrame in trails)
                    {
                        //var currentLine = _osuFile.TimingPoints.GetLine(trailFrame.Offset);
                        float balance = GetObjectBalance(trailFrame.Point.X);

                        var element = new SlideControlElement(
                            dirInfo.FullName,
                            mapWaves, (int)trailFrame.Offset, 0, balance, 0, 0, 0, 0, 0,
                            SlideControlMode.ChangeBalance, true
                        );

                        hitsoundList.Add(element);
                    }
                }
                else
                {
                    //var currentTiming = file.TimingPoints.GetRedLine(obj.Offset);
                    var offset = obj.ObjectType == HitObjectType.Spinner ? obj.HoldEnd : obj.Offset;
                    var currentLine = _osuFile.TimingPoints.GetLine(offset);

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
                        balance: balance,
                        forceTrack: obj.CustomIndex,
                        fullHitsoundType: null
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
                    return 0;
                }

                if (x > 512) x = 512;
                else if (x < 0) x = 0;

                float balance = (x - 256f) / 256f;
                return balance;
            }
        }

        #endregion Load

        public override void Dispose()
        {
            base.Dispose();

            Stop();
            if (_playingTask != null)
                Task.WaitAll(_playingTask);
            _playingTask?.Dispose();
            _cts?.Dispose();
            Engine?.Dispose();
            AppSettings.Default.Volume.PropertyChanged -= Volume_PropertyChanged;
            //AppSettings.Default.Play.PropertyChanged -= Play_PropertyChanged;

            GC.Collect();
        }
    }
}