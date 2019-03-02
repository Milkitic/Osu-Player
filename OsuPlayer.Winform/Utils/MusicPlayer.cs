using Milkitic.OsuPlayer.Interface;
using Milkitic.OsuPlayer;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Milkitic.OsuPlayer.Utils
{
    public class MusicPlayer : IPlayer, IDisposable
    {
        public PlayStatusEnum PlayStatus { get; private set; }
        public int Duration => (int)_audioFile.TotalTime.TotalMilliseconds;
        public int PlayTime { get; private set; }

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private WaveOutEvent _device;
        private MyAudioFileReader _audioFile;

        public MusicPlayer(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            if (!fi.Exists)
                throw new FileNotFoundException("找不到音乐文件…", fi.FullName);
            _device = new WaveOutEvent { DesiredLatency = 80 };
            _device.PlaybackStopped += (sender, args) =>
            {
                PlayStatus = PlayStatusEnum.Stopped;
            };

            _audioFile = new MyAudioFileReader(filePath);
            _device.Init(_audioFile);

            PlayStatus = PlayStatusEnum.Ready;

            Task.Run(() =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    if (PlayStatus != PlayStatusEnum.NotInitialized && _audioFile != null)
                    {
                        _audioFile.Volume = Core.Config.Volume.Main * Core.Config.Volume.Music;
                        PlayTime = (int)_audioFile?.CurrentTime.TotalMilliseconds;
                    }
                    Thread.Sleep(10);
                }
            });
        }

        public void Play()
        {
            _device.Play();
            PlayStatus = PlayStatusEnum.Playing;
        }

        public void Pause()
        {
            _device.Pause();
            PlayStatus = PlayStatusEnum.Paused;
        }

        public void Replay()
        {
            SetTime(0);
            Play();
        }

        public void SetTime(int ms, bool play = true)
        {
            var span = new TimeSpan(0, 0, 0, 0, ms);
            _audioFile.CurrentTime = span >= _audioFile.TotalTime ? _audioFile.TotalTime - new TimeSpan(0, 0, 0, 0, 1) : span;
            PlayStatus = PlayStatusEnum.Playing;
            if (!play) Pause();
        }

        public void Stop()
        {
            SetTime(0, false);
            PlayStatus = PlayStatusEnum.Stopped;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _device?.Dispose();
            _device = null;
            _audioFile?.Dispose();
            _audioFile = null;
            _cts?.Dispose();
        }
    }
}
