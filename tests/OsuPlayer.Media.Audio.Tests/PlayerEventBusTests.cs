using System;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Media.Audio.Coordination;
using Milky.OsuPlayer.Presentation.Interaction;
using NLog;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public class PlayerEventBusTests
{
    private static readonly Logger NullLogger = LogManager.CreateNullLogger();

    [Fact]
    public void RaisePlayStatusChanged_PostsToDispatcher()
    {
        var dispatcher = new CountingDispatcher();
        var bus = new PlayerEventBus(dispatcher, NullLogger);
        PlayStatus? observed = null;
        bus.PlayStatusChanged += status => observed = status;

        bus.RaisePlayStatusChanged(PlayStatus.Playing);

        Assert.Equal(PlayStatus.Playing, observed);
        Assert.Equal(1, dispatcher.PostCount);
        Assert.Equal(0, dispatcher.SendCount);
    }

    [Fact]
    public void RaiseInterfaceClearRequest_WithNoSubscribers_DoesNotDispatchNull()
    {
        var dispatcher = new CountingDispatcher();
        var bus = new PlayerEventBus(dispatcher, NullLogger);

        bus.RaiseInterfaceClearRequest();

        Assert.Equal(1, dispatcher.SendCount);
    }

    private sealed class CountingDispatcher : IUiThreadDispatcher
    {
        public int PostCount { get; private set; }
        public int SendCount { get; private set; }

        public void Post(Action action)
        {
            PostCount++;
            action();
        }

        public void Send(Action action)
        {
            SendCount++;
            action();
        }
    }
}
