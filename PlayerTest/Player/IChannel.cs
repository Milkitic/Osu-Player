using System;
using System.Threading.Tasks;

namespace PlayerTest.Player
{
    public interface IChannel
    {
        string Description { get; }
        TimeSpan Duration { get; }
        TimeSpan Position { get; }
        float PlaybackRate { get; }
        bool UseTempo { get; }
        ChannelStatus PlayStatus { get; }

        Task Initialize();
        Task Play();
        Task Pause();
        Task Stop();
        Task Restart();
        Task SetTime(TimeSpan time);
        Task SetPlaybackRate(float rate, bool useTempo);
    }
}