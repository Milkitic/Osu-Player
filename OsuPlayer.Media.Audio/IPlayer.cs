using Milky.OsuPlayer.Common.Player;
using System;

namespace Milky.OsuPlayer.Media.Audio
{
    public interface IPlayer
    {
        event EventHandler PlayerLoaded;
        event EventHandler PlayerStarted;
        event EventHandler PlayerStopped;
        event EventHandler PlayerPaused;
        event EventHandler PlayerFinished;
        event EventHandler ProgressChanged;

        int ProgressRefreshInterval { get; set; }

        PlayerStatus PlayerStatus { get; }
        int Duration { get; }
        int PlayTime { get; }

        void Play();
        void Pause();
        void Stop();
        void Replay();
        void SetTime(int ms, bool play = true);
    }
}
