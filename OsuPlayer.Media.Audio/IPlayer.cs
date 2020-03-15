using System;
using Milky.OsuPlayer.Common.Player;

namespace Milky.OsuPlayer.Media.Audio
{
    public interface IPlayer
    {
        PlayStatus PlayStatus { get; }
        TimeSpan Duration { get; }
        TimeSpan PlayTime { get; }

        void Play();
        void Pause();
        void Stop();
        void Replay();
        void SetTime(TimeSpan time, bool play = true);
    }
}
