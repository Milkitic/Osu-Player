using System;
using System.Threading.Tasks;

namespace PlayerTest.Player.Channel
{
    public interface IChannel
    {
        event Action<PlayStatus> PlayStatusChanged;
        event Action<TimeSpan> PositionUpdated;

        string Description { get; }
        TimeSpan Duration { get; }
        TimeSpan Position { get; }
        float PlaybackRate { get; }
        bool UseTempo { get; }
        PlayStatus PlayStatus { get; }

        Task Initialize();
        Task Play();
        Task Pause();
        Task Stop();
        Task Restart();
        Task SkipTo(TimeSpan time);
        Task SetPlaybackRate(float rate, bool useTempo);
    }
}