using Milkitic.OsuPlayer.Interface;
using Milkitic.OsuPlayer.Models;
using NAudio.Vorbis;
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
        public int Duration
        {
            get
            {
                if (_audioFile != null) return (int)_audioFile.TotalTime.TotalMilliseconds;
                if (_oggFile != null) return (int)_oggFile.TotalTime.TotalMilliseconds;
                throw new Exception();
            }
        }

        public int PlayTime { get; private set; }

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private WaveOutEvent _device;
        private AudioFileReader _audioFile;
        private VorbisWaveReader _oggFile;

        public MusicPlayer(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            if (!fi.Exists)
                throw new FileNotFoundException("Can not locate file.", fi.FullName);
            _device = new WaveOutEvent { DesiredLatency = 100 };
            _device.PlaybackStopped += (sender, args) =>
            {
                PlayStatus = PlayStatusEnum.Stopped;
            };
            try
            {
                _audioFile = new AudioFileReader(filePath);
                _device.Init(_audioFile);
            }
            catch
            {
                _oggFile = new VorbisWaveReader(filePath);
                _device.Init(_oggFile);
            }

            PlayStatus = PlayStatusEnum.Ready;

            Task.Run(() =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    if (PlayStatus != PlayStatusEnum.NotInitialized)
                    {
                        if (_audioFile != null) PlayTime = (int)_audioFile?.CurrentTime.TotalMilliseconds;
                        if (_oggFile != null) PlayTime = (int)_oggFile?.CurrentTime.TotalMilliseconds;
                    }
                    Thread.Sleep(1);
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
            if (_audioFile != null)
                _audioFile.CurrentTime = span >= _audioFile.TotalTime ? _audioFile.TotalTime - new TimeSpan(0, 0, 0, 0, 1) : span;
            else if (_oggFile != null)
                _oggFile.CurrentTime = span > _oggFile.TotalTime ? _oggFile.TotalTime - new TimeSpan(0, 0, 0, 0, 1) : span;
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
            _oggFile?.Dispose();
            _oggFile = null;
            _cts?.Dispose();
        }
    }
}
