using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using OsuPlayer.Devices;

namespace Milky.OsuPlayer.Media.Audio.Player
{
    public abstract class MultichannelPlayer : IChannel
    {
        public event Action<PlayStatus> PlayStatusChanged;
        public event Action<TimeSpan> PositionUpdated;

        public string Description { get; } = nameof(MultichannelPlayer);

        public TimeSpan Duration { get; protected set; }

        public TimeSpan Position
        {
            get => _innerTimelineSw.Elapsed;
            protected set => PositionUpdated?.Invoke(value);
        }

        public float PlaybackRate { get; private set; }
        public bool UseTempo { get; private set; }

        public PlayStatus PlayStatus { get; protected set; }
        public StopMode StopMode { get; set; }

        public float Volume
        {
            get => Engine.RootVolume;
            set => Engine.RootVolume = value;
        }

        private readonly List<Subchannel> _subchannels = new List<Subchannel>();
        protected ReadOnlyCollection<Subchannel> Subchannels => new ReadOnlyCollection<Subchannel>(_subchannels);

        private readonly IWavePlayer _outputDevice;
        protected readonly AudioPlaybackEngine Engine;

        private VariableStopwatch _innerTimelineSw = new VariableStopwatch();
        private CancellationTokenSource _cts;
        private Task _playTask;

        private ConcurrentQueue<Subchannel> _channelsQueue;
        private SortedSet<Subchannel> _runningChannels = new SortedSet<Subchannel>(new ChannelEndTimeComparer());

        public MultichannelPlayer()
        {
            _outputDevice = DeviceProviderExtension.CreateOrGetDefaultDevice();
            //_outputDevice = new AsioOut(0);
            Engine = new AudioPlaybackEngine(_outputDevice);
        }

        public abstract Task Initialize();

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

        public async Task Play()
        {
            if (_playTask?.Status == TaskStatus.Running)
                return;
            _cts = new CancellationTokenSource();
            _innerTimelineSw.Start();

            if (_channelsQueue == null)
                await RequeueChannel();

            _playTask = Task.Run(() =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    Position = _innerTimelineSw.Elapsed;
                    //if (_runningChannels.Count > 0)
                    //    Console.WriteLine(string.Join("; ",
                    //        _runningChannels.Select(k => $"{k.Description}: {k.Position.TotalMilliseconds}")));

                    if (_channelsQueue.Count > 0 &&
                        _channelsQueue.TryPeek(out var channel) &&
                        channel.ChannelStartTime <= _innerTimelineSw.Elapsed &&
                        _channelsQueue.TryDequeue(out channel))
                    {
                        _runningChannels.Add(channel);
                        channel.Play();
                        Console.WriteLine($"[{_innerTimelineSw.Elapsed}] Play: {channel.Description}");

                        if (_channelsQueue.Count == 0)
                            Console.WriteLine($"[{_innerTimelineSw.Elapsed}] All channels are playing.");
                    }

                    if (_runningChannels.Count > 0 &&
                        _runningChannels.First().ChannelEndTime < _innerTimelineSw.Elapsed)
                    {
                        _runningChannels.Remove(_runningChannels.First());
                    }

                    Thread.Sleep(1);
                }
            });

            foreach (var channel in _runningChannels)
            {
                await channel.Play();
            }

            PlayStatus = PlayStatus.Playing;
            await Task.CompletedTask;
        }

        public async Task Pause()
        {
            await CancelTask();
            foreach (var channel in _runningChannels)
            {
                await channel.Pause();
            }

            PlayStatus = PlayStatus.Paused;
        }

        public async Task TogglePlay()
        {
            if (PlayStatus == PlayStatus.Ready ||
                PlayStatus == PlayStatus.Finished ||
                PlayStatus == PlayStatus.Paused)
            {
                await Play();
            }
            else if (PlayStatus == PlayStatus.Playing) await Pause();
        }

        private async Task CancelTask()
        {
            if (_playTask.Status == TaskStatus.Running)
                _cts?.Cancel();
            _cts?.Dispose();
            await TaskEx.WhenAllSkipNull(_playTask);
        }

        public async Task Stop()
        {
            await CancelTask();

            foreach (var channel in _runningChannels)
            {
                await channel.Stop();
            }

            Position = TimeSpan.Zero;
            PlayStatus = PlayStatus.Paused;
        }

        public async Task Restart()
        {
            await Stop();
            await Play();
        }

        public async Task SkipTo(TimeSpan time)
        {
            Position = time;
            await RequeueChannel();
            foreach (var channel in _runningChannels)
            {
                await channel.SkipTo(time - channel.ChannelStartTime);
                if (PlayStatus == PlayStatus.Playing)
                {
                    await channel.Play();
                }
            }

            _innerTimelineSw.SkipTo(time);
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
                    await subchannel.Pause();
                }
            }
        }

        public async Task SetPlaybackRate(float rate, bool useTempo)
        {
            foreach (var channel in _subchannels)
            {
                await channel.SetPlaybackRate(rate, useTempo);
            }

            PlaybackRate = rate;
            UseTempo = useTempo;
        }

        public virtual void Dispose()
        {
            foreach (var subchannel in _subchannels) subchannel.Dispose();

            _outputDevice?.Dispose();
            Engine?.Dispose();
            _cts.Cancel();
            TaskEx.WaitAllSkipNull(_playTask);
            _cts?.Dispose();
            _playTask?.Dispose();
        }
    }

    internal class ChannelEndTimeComparer : IComparer<Subchannel>
    {
        public int Compare(Subchannel x, Subchannel y)
        {
            if (x is null && y is null)
                return 0;
            if (y is null)
                return 1;
            if (x is null)
                return -1;

            return (x.ChannelEndTime).CompareTo(y.ChannelEndTime);
        }
    }
}