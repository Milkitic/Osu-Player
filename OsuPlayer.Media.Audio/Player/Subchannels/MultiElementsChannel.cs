using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Media.Audio.Wave;
using Milky.OsuPlayer.Shared;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OSharp.Beatmap.Sections.HitObject;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Media.Audio.Player.Subchannels
{
    public abstract class MultiElementsChannel : Subchannel, ISoundElementsProvider
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly VariableStopwatch _sw = new VariableStopwatch();

        protected List<SoundElement> SoundElements;
        protected readonly SingleMediaChannel ReferenceChannel;
        private ConcurrentQueue<SoundElement> _soundElementsQueue;

        private VolumeSampleProvider _volumeProvider;

        private Task _playingTask;
        //private Task _calibrationTask;
        private CancellationTokenSource _cts;
        private readonly object _skipLock = new object();

        private BalanceSampleProvider _sliderSlideBalance;
        private VolumeSampleProvider _sliderSlideVolume;
        private BalanceSampleProvider _sliderAdditionBalance;
        private VolumeSampleProvider _sliderAdditionVolume;
        private MemoryStream _lastSliderStream;
        private float _playbackRate;

        public bool IsPlayRunning => _playingTask != null &&
                                     !_playingTask.IsCanceled &&
                                     !_playingTask.IsCompleted &&
                                     !_playingTask.IsFaulted;

        //public bool IsCalibrationRunning => _calibrationTask != null &&
        //                                    !_calibrationTask.IsCanceled &&
        //                                    !_calibrationTask.IsCompleted &&
        //                                    !_calibrationTask.IsFaulted;

        public override TimeSpan Duration { get; protected set; }

        public override TimeSpan Position
        {
            get => _sw.Elapsed;
            //_sw.SkipTo(value);
            protected set => RaisePositionUpdated(value);
        }

        public override TimeSpan ChannelStartTime => TimeSpan.FromMilliseconds(AppSettings.Default.Play.GeneralActualOffset < 0
            ? 0
            : AppSettings.Default.Play.GeneralActualOffset);

        public int ManualOffset
        {
            get => -(int)_sw.ManualOffset.TotalMilliseconds;
            internal set => _sw.ManualOffset = -TimeSpan.FromMilliseconds(value);
        }

        public sealed override float PlaybackRate
        {
            get => _sw.Rate;
            protected set => _sw.Rate = value;
        }

        public sealed override bool UseTempo { get; protected set; }

        public float BalanceFactor { get; set; } = 0.35f;

        public MixingSampleProvider Submixer { get; protected set; }

        public MultiElementsChannel(AudioPlaybackEngine engine,
            SingleMediaChannel referenceChannel = null) : base(engine)
        {
            ReferenceChannel = referenceChannel;
        }

        public override async Task Initialize()
        {
            Submixer = new MixingSampleProvider(WaveFormatFactory.IeeeWaveFormat)
            {
                ReadFully = true
            };
            _volumeProvider = new VolumeSampleProvider(Submixer);
            Engine.AddRootSample(_volumeProvider);

            SampleControl.Volume = 1;
            SampleControl.Balance = 0;
            SampleControl.VolumeChanged = f =>
            {
                if (_volumeProvider != null) _volumeProvider.Volume = f;
            };

            await RequeueAsync(TimeSpan.Zero).ConfigureAwait(false);

            Duration = TimeSpan.FromMilliseconds(SoundElements.Count == 0 ? 0 : SoundElements.Max(k => k.Offset));
            await Task.Run(() =>
            {
                SoundElements
                    .Where(k => k.FilePath == null &&
                                (k.ControlType == SlideControlType.None || k.ControlType == SlideControlType.StartNew))
                    .AsParallel()
                    .WithDegreeOfParallelism(Environment.ProcessorCount > 1 ? Environment.ProcessorCount - 1 : 1)
                    .ForAll(k => CachedSound.CreateCacheSounds(new[] { k.FilePath }).Wait());
            }).ConfigureAwait(false);


            await CachedSound.CreateCacheSounds(SoundElements
                .Where(k => k.FilePath != null)
                .Select(k => k.FilePath));

            await SetPlaybackRate(AppSettings.Default.Play.PlaybackRate, AppSettings.Default.Play.PlayUseTempo)
                .ConfigureAwait(false);
            PlayStatus = PlayStatus.Ready;
        }

        public override async Task Play()
        {
            await ReadyLoopAsync().ConfigureAwait(false);

            StartPlayTask();
            //StartCalibrationTask();
            PlayStatus = PlayStatus.Playing;
        }

        public override async Task Pause()
        {
            await CancelLoopAsync().ConfigureAwait(false);
            PlayStatus = PlayStatus.Paused;
        }

        public override async Task Stop()
        {
            await CancelLoopAsync().ConfigureAwait(false);
            await SkipTo(TimeSpan.Zero).ConfigureAwait(false);
            PlayStatus = PlayStatus.Paused;
        }

        public override async Task Restart()
        {
            await SkipTo(TimeSpan.Zero).ConfigureAwait(false);
            await Play().ConfigureAwait(false);
        }

        public override async Task SkipTo(TimeSpan time)
        {
            Submixer.RemoveMixerInput(_sliderSlideBalance);
            Submixer.RemoveMixerInput(_sliderAdditionBalance);

            await Task.Run(() =>
            {
                lock (_skipLock)
                {
                    var status = PlayStatus;
                    PlayStatus = PlayStatus.Reposition;

                    _sw.SkipTo(time);
                    Logger.Debug("{0} want skip: {1}; actual: {2}", Description, time, Position);
                    RequeueAsync(time).Wait();

                    PlayStatus = status;
                }
            }).ConfigureAwait(false);
        }

        public override async Task Sync(TimeSpan time)
        {
            _sw.SkipTo(time);
            await Task.CompletedTask;
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

        //private void StartCalibrationTask()
        //{
        //    if (IsCalibrationRunning) return;
        //    if (ReferenceChannel == null) return;

        //    _calibrationTask = new Task(() =>
        //    {
        //        double? refOldTime = null;
        //        DateTime lastSyncTime = DateTime.Now;
        //        int loopCheckInterval = 1;
        //        int forceSyncDelay = 200;

        //        while (!_cts.IsCancellationRequested)
        //        {
        //            var refNewTime = ReferenceChannel.ReferencePosition.TotalMilliseconds;
        //            var now = DateTime.Now;
        //            if (Equals(refNewTime, refOldTime) && // 若时间相同且没有超过强制同步时间
        //                now - lastSyncTime <= TimeSpan.FromMilliseconds(forceSyncDelay) ||
        //                ReferenceChannel.PlayStatus != PlayStatus.Playing) // 或者参照的播放停止
        //            {
        //                if (!TaskEx.TaskSleep(loopCheckInterval, _cts)) return;
        //                continue;
        //            }

        //            if (Math.Abs((refNewTime - refOldTime ?? 0) - (now - lastSyncTime).TotalMilliseconds) > 5)
        //            {
        //                refOldTime = refNewTime;
        //                lastSyncTime = now;
        //                continue;
        //            }

        //            Console.WriteLine($@"{refNewTime - refOldTime} vs {(now - lastSyncTime).TotalMilliseconds}");
        //            refOldTime = refNewTime;
        //            lastSyncTime = now;
        //            var thisTime = (Position - _sw.CalibrationOffset).TotalMilliseconds;

        //            var difference = refNewTime - thisTime;
        //            //Console.WriteLine($"old: refNewTime: {refNewTime}; thisTime: {Position.TotalMilliseconds}");
        //            //Console.WriteLine($"old: difference: {difference}");
        //            if (Math.Abs(difference) > 5)
        //            {
        //                //Console.WriteLine($@"music: {App.MusicPlayer.PlayTime}, hs: {PlayTime}, {d}({r})");
        //                _sw.CalibrationOffset = TimeSpan.FromMilliseconds(difference); // 计算音效偏移量
        //                //Console.WriteLine($"fix：refNewTime: {refNewTime}; thisTime: {Position.TotalMilliseconds}");
        //            }

        //            if (!TaskEx.TaskSleep(loopCheckInterval, _cts)) return;
        //        }
        //    }, TaskCreationOptions.LongRunning);
        //    _calibrationTask.Start();
        //}

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

                    Position = _sw.Elapsed;

                    lock (_skipLock)
                    {
                        // wow nothing here
                    }

                    while (_soundElementsQueue.TryPeek(out var soundElement) &&
                        soundElement.Offset <= _sw.ElapsedMilliseconds &&
                        _soundElementsQueue.TryDequeue(out soundElement))
                    {
                        lock (_skipLock)
                        {
                            // wow nothing here
                        }

                        try
                        {
                            switch (soundElement.ControlType)
                            {
                                case SlideControlType.None:
                                    var cachedSound = await soundElement.GetCachedSoundAsync().ConfigureAwait(false);
                                    Submixer.PlaySound(cachedSound, soundElement.Volume,
                                        soundElement.Balance * BalanceFactor);
                                    break;
                                case SlideControlType.StartNew:
                                    Submixer.RemoveMixerInput(_sliderSlideBalance);
                                    Submixer.RemoveMixerInput(_sliderAdditionBalance);
                                    cachedSound = await soundElement.GetCachedSoundAsync().ConfigureAwait(false);
                                    _lastSliderStream?.Dispose();
                                    if (cachedSound is null) continue;
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
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "Error while play target element. Source: {0}; ControlType",
                                soundElement.FilePath, soundElement.ControlType);
                        }
                    }

                    if (!TaskEx.TaskSleep(1, _cts)) break;
                }

                if (!_cts.Token.IsCancellationRequested)
                {
                    PlayStatus = PlayStatus.Finished;
                    await SkipTo(TimeSpan.Zero).ConfigureAwait(false);
                }
            }, TaskCreationOptions.LongRunning);
            _playingTask.Start();
        }

        protected async Task RequeueAsync(TimeSpan startTime)
        {
            var queue = new ConcurrentQueue<SoundElement>();
            if (SoundElements == null)
            {
                SoundElements = new List<SoundElement>(await GetSoundElements().ConfigureAwait(false));
                SoundElements.Sort(new SoundElementTimingComparer());
            }

            await Task.Run(() =>
            {
                foreach (var i in SoundElements)
                {
                    if (i.Offset < startTime.TotalMilliseconds)
                        continue;
                    queue.Enqueue(i);
                }
            }).ConfigureAwait(false);

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
            await TaskEx.WhenAllSkipNull(_playingTask/*, _calibrationTask*/).ConfigureAwait(false);
            Logger.Debug(@"{0} task canceled.", Description);
        }

        public abstract Task<IEnumerable<SoundElement>> GetSoundElements();

        public override async Task DisposeAsync()
        {
            await Stop();
            _cts?.Dispose();

            await base.DisposeAsync();
        }
    }
}