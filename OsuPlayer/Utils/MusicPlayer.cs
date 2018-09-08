using Milkitic.OsuPlayer.Interface;
using Milkitic.OsuPlayer.Models;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
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
        public int Duration => throw new NotImplementedException();
        public int PlayTime { get; private set; }

        private readonly MediaEngineEx _mediaEngineEx;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public MusicPlayer(string filePath)
        {
            MediaManager.Startup();
            var mediaEngineFactory = new MediaEngineClassFactory();
            var mediaEngine = new MediaEngine(mediaEngineFactory, null, MediaEngineCreateFlags.AudioOnly);
            mediaEngine.PlaybackEvent += OnPlaybackCallback;
            _mediaEngineEx = mediaEngine.QueryInterface<MediaEngineEx>();
            // Opens the file
            var fileStream = new FileStream(filePath, FileMode.Open);
            var stream = new ByteStream(fileStream);
            var url = new Uri(filePath, UriKind.RelativeOrAbsolute);
            _mediaEngineEx.SetSourceFromByteStream(stream, url.AbsoluteUri);

            Task.Run(() =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    if (PlayStatus == PlayStatusEnum.Ready)
                        PlayTime = (int)TimeSpan.FromSeconds(_mediaEngineEx.CurrentTime).TotalMilliseconds;
                    Thread.Sleep(1);
                }
            });
        }

        public void Play()
        {
            _mediaEngineEx.Play();
        }

        public void Pause()
        {
            _mediaEngineEx.Pause();
            PlayStatus = PlayStatusEnum.Paused;
        }

        public void Replay()
        {
            SetTime(0);
            _mediaEngineEx.Play();
        }

        public void SetTime(int ms, bool play = true)
        {
            _mediaEngineEx.CurrentTime = ms / 1000d;
        }

        public void Stop()
        {
            _mediaEngineEx.Pause();
            SetTime(0);
        }

        private void OnPlaybackCallback(MediaEngineEvent mediaEvent, long param1, int param2)
        {
            switch (mediaEvent)
            {
                case MediaEngineEvent.CanPlay:
                    PlayStatus = PlayStatusEnum.Ready;
                    break;
                case MediaEngineEvent.TimeUpdate:
                    PlayStatus = PlayStatusEnum.Playing;
                    break;
                case MediaEngineEvent.Error:
                    //throw new SharpDX.SharpDXException();
                case MediaEngineEvent.Abort:
                    PlayStatus = PlayStatusEnum.Stopped;
                    SetTime(0);
                    break;
                case MediaEngineEvent.Ended:
                    PlayStatus = PlayStatusEnum.Stopped;
                    SetTime(0);
                    break;
            }
        }

        public void Dispose()
        {
            _mediaEngineEx?.Dispose();
            _cts?.Dispose();
        }
    }
}
