using Milkitic.OsuPlayer.Models;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milkitic.OsuPlayer.Interface
{
    public interface IPlayer
    {
        PlayStatusEnum PlayStatus { get; }
        int Duration { get; }
        int PlayTime { get; }

        void Play();
        void Pause();
        void Stop();
        void Replay();
        void SetTime(int ms, bool play = true);
    }
}
