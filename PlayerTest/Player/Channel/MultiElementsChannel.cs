using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PlayerTest.Wave;

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
        private readonly object _timeLock = new object();
        private int _variableSpeedCompensationOffset;

        private BalanceSampleProvider _sliderSlideBalance;
        private VolumeSampleProvider _sliderSlideVolume;
        private BalanceSampleProvider _sliderAdditionBalance;
        private VolumeSampleProvider _sliderAdditionVolume;

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
            protected set => _sw.SkipTo(value);
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

            Duration = TimeSpan.FromMilliseconds(_soundElements.Max(k => k.Offset));

            Submixer = new MixingSampleProvider(WaveFormatFactory.WaveFormat);
            _volumeProvider = new VolumeSampleProvider(Submixer);
            Engine.AddRootSample(_volumeProvider);

            SampleControl.Volume = 1;
            SampleControl.Balance = 0;
            SampleControl.VolumeChanged = f => _volumeProvider.Volume = f;

            await RequeueAsync(TimeSpan.Zero);

            await CachedSound.CreateCacheSounds(_soundElements.Select(k => k.FilePath));

            await SetPlaybackRate(AppSettings.Default.Play.PlaybackRate, AppSettings.Default.Play.PlayUseTempo);
            PlayStatus = ChannelStatus.Ready;
        }

        public override async Task Play()
        {
            await InnerPlayAsync();

            StartPlayTask();
            StartCalibrationTask();
            PlayStatus = ChannelStatus.Playing;
        }

        private void StartCalibrationTask()
        {
            if (IsCalibrationRunning) return;
            if (_referenceChannel == null) return;

            _calibrationTask = new Task(() =>
            {

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

                    if (_soundElementsQueue.TryPeek(out var soundElement) &&
                        soundElement.Offset <= _sw.ElapsedMilliseconds &&
                        _soundElementsQueue.TryDequeue(out soundElement))
                    {
                        switch (soundElement.ControlType)
                        {
                            case SlideControlType.None:
                                var cachedSound = await soundElement.GetCachedSoundAsync();
                                Submixer.RemoveMixerInput(_sliderSlideBalance);
                                Submixer.RemoveMixerInput(_sliderAdditionBalance);
                                Submixer.PlaySound(cachedSound, soundElement.Volume,
                                    soundElement.Balance * BalanceFactor);
                                break;
                            case SlideControlType.StartNew:
                                cachedSound = await soundElement.GetCachedSoundAsync();
                                using (var mem = new MemoryStream(cachedSound.RawFileData))
                                {
                                    var myf = new WaveFileReader(mem);
                                    var loop = new LoopStream(myf);
                                    switch (soundElement.SlideType)
                                    {
                                        case SlideType.Slide:
                                            _sliderSlideVolume = new VolumeSampleProvider(loop.ToSampleProvider())
                                            {
                                                Volume = soundElement.Volume
                                            };
                                            _sliderSlideBalance = new BalanceSampleProvider(_sliderSlideVolume)
                                            {
                                                Balance = soundElement.Balance * BalanceFactor
                                            };
                                            Submixer.AddMixerInput(_sliderSlideBalance);
                                            break;
                                        case SlideType.Addition:
                                            _sliderAdditionVolume = new VolumeSampleProvider(loop.ToSampleProvider())
                                            {
                                                Volume = soundElement.Volume
                                            };
                                            _sliderAdditionBalance = new BalanceSampleProvider(_sliderAdditionVolume)
                                            {
                                                Balance = soundElement.Balance * BalanceFactor
                                            };
                                            Submixer.AddMixerInput(_sliderAdditionBalance);
                                            break;
                                    }
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

                    Thread.Sleep(1);
                }

                if (!_cts.Token.IsCancellationRequested)
                {
                    await SetTime(TimeSpan.Zero);
                }

            }, TaskCreationOptions.LongRunning);
            _playingTask.Start();
        }

        public override async Task Pause()
        {
            await InnerPauseAsync();
            throw new NotImplementedException();
        }

        public override async Task Stop()
        {
            throw new NotImplementedException();
        }

        public override async Task Restart()
        {
            throw new NotImplementedException();
        }

        public override async Task SetTime(TimeSpan time)
        {
            Submixer.RemoveMixerInput(_sliderSlideBalance);
            Submixer.RemoveMixerInput(_sliderAdditionBalance);

            await Task.Run(() =>
            {
                lock (_timeLock)
                {
                    Position = time;
                    RequeueAsync(time).Wait();
                }
            }).ConfigureAwait(false);
        }

        public override async Task SetPlaybackRate(float rate, bool useTempo)
        {
            PlaybackRate = rate;
            UseTempo = useTempo;
            AdjustModOffset();
        }

        private void AdjustModOffset()
        {
            if (Math.Abs(_sw.Rate - 0.75) < 0.001 && !UseTempo)
            {
                _variableSpeedCompensationOffset = -25;
            }
            else if (Math.Abs(_sw.Rate - 1.5) < 0.001 && UseTempo)
            {
                _variableSpeedCompensationOffset = 15;
            }
            else
            {
                _variableSpeedCompensationOffset = 0;
            }
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

        private async Task InnerPlayAsync()
        {
            _cts = new CancellationTokenSource();
            _sw.Start();
            await Task.CompletedTask;
        }

        private async Task InnerPauseAsync()
        {
            _sw.Stop();
            _cts?.Cancel();
            if (_playingTask != null)
                await Task.WhenAll(_playingTask);
            if (_calibrationTask != null)
                await Task.WhenAll(_calibrationTask);
            Console.WriteLine(@"Task canceled.");
        }
    }

    public class SoundElementTimingComparer : IComparer<SoundElement>
    {
        public int Compare(SoundElement x, SoundElement y)
        {
            return x.Offset.CompareTo(y.Offset);
        }
    }

    public enum SlideControlType
    {
        None, StartNew, StopRunning, ChangeBalance, ChangeVolume
    }
}