using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OSharp.Beatmap.Sections.HitObject;
using PlayerTest.TrackProvider;
using PlayerTest.Wave;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlayerTest.Player.Channel
{
    public class MultiElementsChannel : Subchannel
    {
        private readonly VariableStopwatch _sw = new VariableStopwatch();

        private readonly List<SoundElement> _soundElements;
        private readonly SingleMediaChannel _referenceChannel;
        private ConcurrentQueue<SoundElement> _soundElementsQueue;

        private VolumeSampleProvider _volumeProvider;

        private Task _playingTask;
        private Task _calibrationTask;
        private CancellationTokenSource _cts;
        private readonly object _skipLock = new object();

        private BalanceSampleProvider _sliderSlideBalance;
        private VolumeSampleProvider _sliderSlideVolume;
        private BalanceSampleProvider _sliderAdditionBalance;
        private VolumeSampleProvider _sliderAdditionVolume;
        private MemoryStream _lastSliderStream;

        public bool IsPlayRunning => _playingTask != null &&
                                     !_playingTask.IsCanceled &&
                                     !_playingTask.IsCompleted &&
                                     !_playingTask.IsFaulted;

        public bool IsCalibrationRunning => _calibrationTask != null &&
                                            !_calibrationTask.IsCanceled &&
                                            !_calibrationTask.IsCompleted &&
                                            !_calibrationTask.IsFaulted;

        public override TimeSpan Duration { get; protected set; }

        public override TimeSpan Position
        {
            get => _sw.Elapsed;
            protected set
            {
                _sw.SkipTo(value);
                base.Position = value;
            }
        }

        public int ManualOffset
        {
            get => (int)_sw.ManualOffset.TotalMilliseconds;
            protected set => _sw.ManualOffset = TimeSpan.FromMilliseconds(value);
        }

        public override float PlaybackRate { get; protected set; }
        public override bool UseTempo { get; protected set; }

        public float BalanceFactor { get; set; } = 0.35f;

        public MixingSampleProvider Submixer { get; protected set; }

        public MultiElementsChannel(AudioPlaybackEngine engine, List<SoundElement> soundElements,
            SingleMediaChannel referenceChannel = null) : base(engine)
        {
            _soundElements = soundElements;
            _referenceChannel = referenceChannel;
        }

        public override async Task Initialize()
        {
            _soundElements.Sort(new SoundElementTimingComparer());

            Duration = TimeSpan.FromMilliseconds(_soundElements.Count == 0 ? 0 : _soundElements.Max(k => k.Offset));

            Submixer = new MixingSampleProvider(WaveFormatFactory.IeeeWaveFormat)
            {
                ReadFully = true
            };
            _volumeProvider = new VolumeSampleProvider(Submixer);
            Engine.AddRootSample(_volumeProvider);

            SampleControl.Volume = 1;
            SampleControl.Balance = 0;
            SampleControl.VolumeChanged = f => _volumeProvider.Volume = f;

            await RequeueAsync(TimeSpan.Zero);

            await CachedSound.CreateCacheSounds(_soundElements
                .Where(k => k.FilePath != null)
                .Select(k => k.FilePath));

            await SetPlaybackRate(AppSettings.Default.Play.PlaybackRate, AppSettings.Default.Play.PlayUseTempo);
            PlayStatus = PlayStatus.Ready;
        }

        public override async Task Play()
        {
            await ReadyLoopAsync();

            StartPlayTask();
            StartCalibrationTask();
            PlayStatus = PlayStatus.Playing;
        }

        public override async Task Pause()
        {
            await CancelLoopAsync();
            PlayStatus = PlayStatus.Paused;
        }

        public override async Task Stop()
        {
            await CancelLoopAsync();
            await SkipTo(TimeSpan.Zero);
            PlayStatus = PlayStatus.Paused;
        }

        public override async Task Restart()
        {
            await SkipTo(TimeSpan.Zero);
            await Play();
        }

        public override async Task SkipTo(TimeSpan time)
        {
            await Task.Run(() =>
            {
                lock (_skipLock)
                {
                    var status = PlayStatus;
                    PlayStatus = PlayStatus.Reposition;

                    Submixer.RemoveMixerInput(_sliderSlideBalance);
                    Submixer.RemoveMixerInput(_sliderAdditionBalance);

                    Position = time;
                    RequeueAsync(time).Wait();

                    PlayStatus = status;
                }
            }).ConfigureAwait(false);
        }

        public override async Task SetPlaybackRate(float rate, bool useTempo)
        {
            PlaybackRate = rate;
            UseTempo = useTempo;
            AdjustModOffset();
            await Task.CompletedTask;
        }

        private void AdjustModOffset()
        {
            if (Math.Abs(_sw.Rate - 0.75) < 0.001 && !UseTempo)
                _sw.VariableOffset = TimeSpan.FromMilliseconds(-25);
            else if (Math.Abs(_sw.Rate - 1.5) < 0.001 && UseTempo)
                _sw.VariableOffset = TimeSpan.FromMilliseconds(15);
            else
                _sw.VariableOffset = TimeSpan.Zero;
        }

        private void StartCalibrationTask()
        {
            if (IsCalibrationRunning) return;
            if (_referenceChannel == null) return;

            _calibrationTask = new Task(() =>
            {
                double? refOldTime = null;
                DateTime? lastSyncTime = null;
                const int loopCheckInterval = 100;
                const int forceSyncDelay = 200;

                while (!_cts.IsCancellationRequested)
                {
                    var refNewTime = _referenceChannel.ReferencePosition.TotalMilliseconds;
                    var now = DateTime.Now;
                    if (Equals(refNewTime, refOldTime) && // 若时间相同且没有超过强制同步时间
                        now - lastSyncTime <= TimeSpan.FromMilliseconds(forceSyncDelay) ||
                        _referenceChannel.PlayStatus != PlayStatus.Playing) // 或者参照的播放停止
                    {
                        if (!TaskEx.TaskSleep(loopCheckInterval, _cts)) return;
                        continue;
                    }

                    refOldTime = refNewTime;
                    lastSyncTime = now;
                    var thisTime = Position.TotalMilliseconds;

                    var difference = refNewTime - thisTime;
                    if (Math.Abs(difference) > 5)
                    {
                        //Console.WriteLine($@"music: {App.MusicPlayer.PlayTime}, hs: {PlayTime}, {d}({r})");
                        _sw.CalibrationOffset = TimeSpan.FromMilliseconds(difference); // 计算音效偏移量
                    }

                    if (!TaskEx.TaskSleep(loopCheckInterval, _cts)) return;
                }
            }, TaskCreationOptions.LongRunning);
            _calibrationTask.Start();
        }

        private void StartPlayTask()
        {
            if (IsPlayRunning) return;

            _playingTask = new Task(async () =>
            {
                while (_soundElementsQueue.Count > 0)
                {
                    if (_cts.Token.IsCancellationRequested)
                    {
                        _sw.Stop();
                        break;
                    }

                    base.Position = this.Position;
                    if (_soundElementsQueue.TryPeek(out var soundElement) &&
                        soundElement.Offset <= _sw.ElapsedMilliseconds &&
                        _soundElementsQueue.TryDequeue(out soundElement))
                    {
                        switch (soundElement.ControlType)
                        {
                            case SlideControlType.None:
                                var cachedSound = await soundElement.GetCachedSoundAsync();
                                Submixer.PlaySound(cachedSound, soundElement.Volume,
                                    soundElement.Balance * BalanceFactor);
                                break;
                            case SlideControlType.StartNew:
                                Submixer.RemoveMixerInput(_sliderSlideBalance);
                                Submixer.RemoveMixerInput(_sliderAdditionBalance);
                                cachedSound = await soundElement.GetCachedSoundAsync();
                                _lastSliderStream?.Dispose();

                                var byteArray = new byte[cachedSound.AudioData.Length * sizeof(float)];
                                Buffer.BlockCopy(cachedSound.AudioData, 0, byteArray, 0, byteArray.Length);

                                _lastSliderStream = new MemoryStream(byteArray);
                                var myf = new RawSourceWaveStream(_lastSliderStream, cachedSound.WaveFormat);
                                var loop = new LoopStream(myf);
                                if (soundElement.SlideType.HasFlag(HitsoundType.Slide))
                                {
                                    _sliderSlideVolume = new VolumeSampleProvider(loop.ToSampleProvider())
                                    {
                                        Volume = soundElement.Volume
                                    };
                                    _sliderSlideBalance = new BalanceSampleProvider(_sliderSlideVolume)
                                    {
                                        Balance = soundElement.Balance * BalanceFactor
                                    };
                                    Submixer.AddMixerInput(_sliderSlideBalance);
                                }
                                else if (soundElement.SlideType.HasFlag(HitsoundType.SlideWhistle))
                                {
                                    _sliderAdditionVolume = new VolumeSampleProvider(loop.ToSampleProvider())
                                    {
                                        Volume = soundElement.Volume
                                    };
                                    _sliderAdditionBalance = new BalanceSampleProvider(_sliderAdditionVolume)
                                    {
                                        Balance = soundElement.Balance * BalanceFactor
                                    };
                                    Submixer.AddMixerInput(_sliderAdditionBalance);
                                }

                                break;
                            case SlideControlType.StopRunning:
                                Submixer.RemoveMixerInput(_sliderSlideBalance);
                                Submixer.RemoveMixerInput(_sliderAdditionBalance);
                                break;
                            case SlideControlType.ChangeBalance:
                                if (_sliderAdditionBalance != null)
                                    _sliderAdditionBalance.Balance = soundElement.Balance * BalanceFactor;
                                if (_sliderSlideBalance != null)
                                    _sliderSlideBalance.Balance = soundElement.Balance * BalanceFactor;
                                break;
                            case SlideControlType.ChangeVolume:
                                if (_sliderAdditionVolume != null) _sliderAdditionVolume.Volume = soundElement.Volume;
                                if (_sliderSlideVolume != null) _sliderSlideVolume.Volume = soundElement.Volume;
                                break;
                        }
                    }

                    if (!TaskEx.TaskSleep(1, _cts)) break;
                }

                if (!_cts.Token.IsCancellationRequested)
                {
                    await SkipTo(TimeSpan.Zero);
                    PlayStatus = PlayStatus.Finished;
                }
            }, TaskCreationOptions.LongRunning);
            _playingTask.Start();
        }

        private async Task RequeueAsync(TimeSpan startTime)
        {
            var queue = new ConcurrentQueue<SoundElement>();
            if (_soundElements != null)
            {
                await Task.Run(() =>
                {
                    foreach (var i in _soundElements)
                    {
                        if (i.Offset < startTime.TotalMilliseconds)
                            continue;
                        queue.Enqueue(i);
                    }
                }).ConfigureAwait(false);
            }

            _soundElementsQueue = queue;
        }

        private async Task ReadyLoopAsync()
        {
            _cts = new CancellationTokenSource();
            _sw.Start();
            await Task.CompletedTask;
        }

        private async Task CancelLoopAsync()
        {
            _sw.Stop();
            _cts?.Cancel();
            await TaskEx.WhenAllSkipNull(_playingTask, _calibrationTask);
            Console.WriteLine(@"Task canceled.");
        }
    }
}