using Milky.OsuPlayer.Media.Audio.Player.Subchannels;
using Milky.OsuPlayer.Shared;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Media.Audio.Player
{
    public abstract class MultichannelPlayer : IChannel
    {
        public event Action<PlayStatus> PlayStatusChanged;
        public event Action<TimeSpan> PositionUpdated;

        public virtual string Description { get; } = "Player";

        public TimeSpan Duration { get; protected set; }

        public TimeSpan Position => _innerTimelineSw.Elapsed;

        public float PlaybackRate { get; private set; }
        public bool UseTempo { get; private set; }

        public PlayStatus PlayStatus
        {
            get => _playStatus;
            protected set
            {
                if (value == _playStatus) return;
                _playStatus = value;
                InvokeMethodHelper.OnMainThread(() => PlayStatusChanged?.Invoke(value));
            }
        }

        public StopMode StopMode { get; set; }

        public float Volume
        {
            get => Engine.RootVolume;
            set => Engine.RootVolume = value;
        }

        protected ReadOnlyCollection<Subchannel> Subchannels => new ReadOnlyCollection<Subchannel>(_subchannels);
        protected readonly AudioPlaybackEngine Engine;

        private readonly List<Subchannel> _subchannels = new List<Subchannel>();
        private readonly IWavePlayer _outputDevice;

        private readonly VariableStopwatch _innerTimelineSw = new VariableStopwatch();
        private CancellationTokenSource _cts;
        private Task _playTask;

        private ConcurrentQueue<Subchannel> _channelsQueue;
        private SortedSet<Subchannel> _runningChannels = new SortedSet<Subchannel>(new ChannelEndTimeComparer());
        private PlayStatus _playStatus;

        public MultichannelPlayer()
        {
            _outputDevice = DeviceProviderExtension.CreateOrGetDefaultDevice();
            Engine = new AudioPlaybackEngine(_outputDevice);
        }

        public virtual async Task Initialize()
        {
            Duration = MathEx.Max(Subchannels.Select(k => k?.ChannelEndTime ?? TimeSpan.Zero));
            PlayStatus = PlayStatus.Ready;
            await Task.CompletedTask;
        }

        public async Task Play()
        {
            if (_playTask?.Status == TaskStatus.Running)
                return;
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _innerTimelineSw.Start();

            if (_channelsQueue == null)
                await RequeueChannel().ConfigureAwait(false);

            _playTask = Task.Run(async () =>
            {
                var date = DateTime.Now;
                while (!_cts.IsCancellationRequested)
                {
                    RaisePositionUpdated(_innerTimelineSw.Elapsed);
                    //if (_runningChannels.Count > 0)
                    //    Console.WriteLine(string.Join("; ",
                    //        _runningChannels.Select(k => $"{k.Description}: {k.Position.TotalMilliseconds}")));
                    if (_channelsQueue.Count > 0 &&
                        _channelsQueue.TryPeek(out var channel) &&
                        channel.ChannelStartTime <= _innerTimelineSw.Elapsed &&
                        _channelsQueue.TryDequeue(out channel))
                    {
                        _runningChannels.Add(channel);
                        await channel.Play();
                        Console.WriteLine($"[{_innerTimelineSw.Elapsed}] Play: {channel.Description}");

                        if (_channelsQueue.Count == 0)
                            Console.WriteLine($"[{_innerTimelineSw.Elapsed}] All channels are playing.");
                    }

                    if (Position > Duration)
                    {
                        _innerTimelineSw.Stop();
                        SetTime(Duration);
                        PlayStatus = PlayStatus.Finished;
                        break;
                    }

                    if (_runningChannels.Count > 0 &&
                        _runningChannels.First().ChannelEndTime < _innerTimelineSw.Elapsed)
                    {
                        _runningChannels.Remove(_runningChannels.First());
                    }

                    if (DateTime.Now - date > TimeSpan.FromSeconds(10))
                    {
                        await InnerSync();
                        date = DateTime.Now;
                    }

                    Thread.Sleep(1);
                }
            });

            foreach (var channel in _runningChannels)
            {
                await channel.Play().ConfigureAwait(false);
            }

            PlayStatus = PlayStatus.Playing;
            await Task.CompletedTask;
        }

        public async Task Pause()
        {
            await CancelTask().ConfigureAwait(false);
            foreach (var channel in _runningChannels)
            {
                await channel.Pause().ConfigureAwait(false);
            }

            PlayStatus = PlayStatus.Paused;
        }

        public async Task TogglePlay()
        {
            if (PlayStatus == PlayStatus.Ready ||
                PlayStatus == PlayStatus.Finished ||
                PlayStatus == PlayStatus.Paused)
            {
                await Play().ConfigureAwait(false);
            }
            else if (PlayStatus == PlayStatus.Playing) await Pause().ConfigureAwait(false);
        }

        public async Task Stop()
        {
            await CancelTask().ConfigureAwait(false);

            foreach (var channel in _runningChannels)
            {
                await channel.Stop().ConfigureAwait(false);
            }

            SetTime(TimeSpan.Zero);
            PlayStatus = PlayStatus.Paused;
        }

        public async Task Restart()
        {
            await Stop().ConfigureAwait(false);
            await Play().ConfigureAwait(false);
        }

        public async Task SkipTo(TimeSpan time)
        {
            SetTime(time);
            await RequeueChannel().ConfigureAwait(false);
            foreach (var channel in _runningChannels)
            {
                await channel.SkipTo(time - channel.ChannelStartTime).ConfigureAwait(false);
                if (PlayStatus == PlayStatus.Playing)
                {
                    await channel.Play();
                }
            }
        }

        public async Task SetPlaybackRate(float rate, bool useTempo)
        {
            foreach (var channel in _subchannels)
            {
                await channel.SetPlaybackRate(rate, useTempo).ConfigureAwait(false);
            }

            PlaybackRate = rate;
            UseTempo = useTempo;
        }

        public virtual async Task DisposeAsync()
        {
            await Stop();

            foreach (var subchannel in _subchannels) await subchannel.DisposeAsync();

            _outputDevice?.Dispose();
            Engine?.Dispose();
            _cts?.Dispose();
            _playTask?.Dispose();
        }

        protected void AddSubchannel(Subchannel channel)
        {
            _subchannels.Add(channel);
        }

        protected void RemoveSubchannel(Subchannel channel)
        {
            _subchannels.Remove(channel);
        }

        protected IEnumerable<IChannel> EnumerateSubchannels()
        {
            foreach (var subchannel in _subchannels)
            {
                yield return subchannel;
            }
        }

        private async Task CancelTask()
        {
            if (_playTask is null ||
                _playTask.Status == TaskStatus.Canceled ||
                _playTask.Status == TaskStatus.Faulted)
                return;

            _cts?.Cancel();
            await TaskEx.WhenAllSkipNull(_playTask).ConfigureAwait(false);
        }

        private void SetTime(TimeSpan value)
        {
            _innerTimelineSw.SkipTo(value);
            RaisePositionUpdated(value);
        }

        private async Task InnerSync()
        {
            foreach (var channel in _runningChannels)
            {
                if (channel is SingleMediaChannel) continue;
                await channel.Sync(Position - channel.ChannelStartTime).ConfigureAwait(false);
            }
        }

        private async Task RequeueChannel()
        {
            _channelsQueue = new ConcurrentQueue<Subchannel>(_subchannels
                .Where(k => k.ChannelStartTime > Position)
                .OrderBy(k => k.ChannelStartTime)
            );

            var old = _runningChannels.ToList();
            _runningChannels = new SortedSet<Subchannel>(_subchannels
                .Where(k => k.ChannelStartTime <= Position && k.ChannelEndTime > Position), new ChannelEndTimeComparer());
            foreach (var subchannel in old)
            {
                if (!_runningChannels.Contains(subchannel))
                {
                    await subchannel.Pause().ConfigureAwait(false);
                }
            }
        }

        private void RaisePositionUpdated(TimeSpan value)
        {
            InvokeMethodHelper.OnMainThread(() => PositionUpdated?.Invoke(value));
        }
    }
}