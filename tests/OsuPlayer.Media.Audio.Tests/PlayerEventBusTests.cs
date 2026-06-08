using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using OsuPlayer.Media.Audio.Coordination;
using OsuPlayer.Presentation.Interaction;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public class PlayerEventBusTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<PlayerEventBus> NullLog = NullLogger<PlayerEventBus>.Instance;

    [Fact]
    public void AttachPlayer_ReplaysPlayStatusThroughDispatcher()
    {
        var dispatcher = new CountingDispatcher();
        var bus = new PlayerEventBus(dispatcher, NullLog);
        var player = new OsuMixPlayer(null!, string.Empty, null!, null!, NullLogger<OsuMixPlayer>.Instance);
        PlayStatus? observed = null;
        bus.PlayStatusChanged += status => observed = status;

        SetPlayStatus(player, PlayStatus.Playing);
        bus.AttachPlayer(player);

        Assert.Equal(PlayStatus.Playing, observed);
        Assert.Equal(1, dispatcher.PostCount);
        Assert.Equal(0, dispatcher.SendCount);
    }

    [Fact]
    public void RaiseInterfaceClearRequest_WithNoSubscribers_DoesNotDispatchNull()
    {
        var dispatcher = new CountingDispatcher();
        var bus = new PlayerEventBus(dispatcher, NullLog);

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

    private static void SetPlayStatus(OsuMixPlayer player, PlayStatus status)
    {
        var field = typeof(OsuMixPlayer)
            .GetField("_playStatus", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(player, status);
    }
}
