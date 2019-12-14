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
        static HitsoundPlayer()
        {
            CachedSound.CachePath = Path.Combine(Domain.CachePath, "_temp.sound");
        }

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
                                 !_playingTask.IsFaulted;

        public bool IsRunningDynamicOffset => _offsetTask != null &&
                                              !_offsetTask.IsCanceled &&
                                              !_offsetTask.IsCompleted &&
                                              !_offsetTask.IsFaulted;

        protected AudioPlaybackEngine Engine = new AudioPlaybackEngine();
        private WasapiOut _slideDevice = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 5);
        private WasapiOut _slideAddDevice = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 5);
        private LoopStream _slideLoop;
        private CachedSoundSampleProvider _slideSound;
        private CachedSoundSampleProvider _slideAddSound;
        private ChannelSampleProvider _currentChannel;
        private VolumeSampleProvider _currentVolume;

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

            List<SoundElement> hitsoundList = FillHitsoundList(_osuFile, dirInfo);
            _hitsoundList = hitsoundList.OrderBy(t => t.Offset).Cast<SoundElement>().ToList(); // Sorted before enqueue.
            Requeue(0);
            var allPaths = hitsoundList.Select(t => t.FilePaths).SelectMany(sbx2 => sbx2).Distinct();
            await Task.Run(() =>
            {
                Engine.CreateCacheSounds(allPaths);
                Engine.CreateCacheSounds(new DirectoryInfo(Domain.DefaultPath).GetFiles().Select(k => k.FullName));
            });
            PlayerStatus = PlayerStatus.Ready;
            SetPlayMod(AppSettings.Default.Play.PlayMod, false);
            InitVolume();
            AppSettings.Default.Volume.PropertyChanged += Volume_PropertyChanged;
            RaisePlayerLoadedEvent(this, new EventArgs());
        }

        protected virtual void InitVolume()
        {
            Engine.Volume = 1f * AppSettings.Default.Volume.Hitsound * AppSettings.Default.Volume.Main;
        }

        protected virtual void Volume_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Engine.Volume = 1f * AppSettings.Default.Volume.Hitsound * AppSettings.Default.Volume.Main;
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
            _slideDevice?.Stop();
            _slideDevice?.Dispose();
            _slideAddDevice?.Stop();
            _slideAddDevice?.Dispose();
            AppSettings.Default.Volume.PropertyChanged -= Volume_PropertyChanged;

            GC.Collect();
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
                                    he.Balance * AppSettings.Default.Volume.BalanceFactor / 100f);
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
                                        //var device = sce.IsAddition ? _slideAddDevice : _slideDevice;
                                        var sound = sce.IsAddition ? NewProviderAndRet(ref _slideAddSound, path) : NewProviderAndRet(ref _slideSound, path);
                                        //var s = new RawSourceWaveStream(
                                        //    sound.SourceSound.AudioData.Select(k => (byte)k).ToArray(), 0,
                                        //    sound.SourceSound.AudioData.Length, sound.WaveFormat);
                                        var myf = new WaveFileReader(path);
                                        var loop = new LoopStream(myf);
                                        _currentVolume = new VolumeSampleProvider(loop.ToSampleProvider());
                                        _currentVolume.Volume = sce.Volume;
                                        _currentChannel = new ChannelSampleProvider(_currentVolume);
                                        _currentChannel.Balance = sce.Balance;

                                        //device.Stop();
                                        var device = sce.IsAddition ? NewDeviceAndRet(ref _slideAddDevice) : NewDeviceAndRet(ref _slideDevice);
                                        device.Init(_currentChannel);
                                        device.Play();
                                    }
                                    break;
                                case SlideControlMode.ChangeBalance:
                                    break;
                                case SlideControlMode.Stop:
                                    {
                                        var device = sce.IsAddition ? _slideAddDevice : _slideDevice;
                                        device.Stop();
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

        private WasapiOut NewDeviceAndRet(ref WasapiOut device)
        {
            device?.Stop();
            device?.Dispose();
            return device = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 5);
        }

        private CachedSoundSampleProvider NewProviderAndRet(ref CachedSoundSampleProvider slideAddSound, string path)
        {
            return slideAddSound = new CachedSoundSampleProvider(Engine.GetOrCreateCacheSound(path));
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
                            var d = nowMs - (PlayTime + SingleOffset); // Single：单曲offset（人工调），PlayTime：音效play time
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
                                hitsoundList.Add(new SlideControlElement(dirInfo.FullName,
                                    mapWaves, (int)timing.Offset, volume2, balance, track2, timing.TimingSampleset, sample, addition, forceTrack,
                                    SlideControlMode.NewSample, false));
                                continue;
                            }

                            timings.RemoveAt(i);
                            i--;
                        }

                        hitsoundList.Add(new SlideControlElement(dirInfo.FullName,
                           mapWaves, (int)end, volume, balance, track, lineSample, sample, addition, forceTrack,
                           SlideControlMode.Stop, false));
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