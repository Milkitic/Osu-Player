using Milkitic.OsuPlayer;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Milkitic.OsuPlayer.Storyboard
{
    public class Timing
    {
        public float Offset => ControlOffset + Watch.ElapsedMilliseconds * PlayBack;
        public long ControlOffset;
        public readonly Stopwatch Watch;
        public float PlayBack { get; set; } = 1f;
        private Task _offsetTask;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public Timing(long controlOffset, Stopwatch watch)
        {
            ControlOffset = controlOffset;
            Watch = watch;
        }

        public void SetTiming(long time, bool start)
        {
            CancelTask();
            ControlOffset = time;
            Watch.Reset();
            if (start) Watch.Start();
            StartTask();
        }

        public void Reset()
        {
            ControlOffset = 0;
            Watch.Stop();
            CancelTask();
            Watch.Reset();
        }

        private void CancelTask()
        {
            _cts.Cancel();
            if (_offsetTask != null) Task.WaitAll(_offsetTask);
        }
        private void StartTask()
        {
            _cts = new CancellationTokenSource();
            DynamicOffset();
        }
        public void Pause()
        {
            Watch.Stop();
            CancelTask();
        }
        public void Start()
        {
            Watch.Start();
            StartTask();
        }

        private void DynamicOffset()
        {
            if (_offsetTask == null || _offsetTask.IsCanceled || _offsetTask.IsCompleted)
                _offsetTask = Task.Run(() =>
                {
                    Thread.Sleep(30);
                    int? preMs = null;
                    while (!_cts.IsCancellationRequested && Core.MusicPlayer.PlayStatus == PlayStatusEnum.Playing)
                    {
                        int playerMs = Core.MusicPlayer.PlayTime;
                        if (playerMs != preMs)
                        {
                            preMs = playerMs;
                            var d = playerMs - Offset;
                            if (Math.Abs(d) > 10)
                            {
                                Console.WriteLine($@"music: {Core.MusicPlayer.PlayTime}, sb: {Offset}, {d} {ControlOffset}");
                                ControlOffset += (int)(d / 2f);
                                Console.WriteLine($@"{ControlOffset}");
                            }
                        }

                        Thread.Sleep(10);
                    }
                }, _cts.Token);
        }
    }
}