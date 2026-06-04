using System;
using System.Collections.Generic;
using Milky.OsuPlayer.Media.Audio.Infrastructure;
using Milky.OsuPlayer.Presentation.Interaction;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public class RaiseOnUiExtensionsTests
{
    [Fact]
    public void RaiseOnUi_PostsToDispatcher()
    {
        var dispatcher = new FakeUiThreadDispatcher();
        var invoked = 0;

        dispatcher.RaiseOnUi(() => invoked++);

        // The fake runs Post() synchronously so the side-effect is visible.
        Assert.Equal(1, invoked);
        Assert.Equal(1, dispatcher.PostCount);
    }

    [Fact]
    public void RaiseOnUi_NullDispatcher_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IUiThreadDispatcher)null!).RaiseOnUi(() => { }));
    }

    [Fact]
    public void RaiseOnUi_NullAction_Throws()
    {
        var dispatcher = new FakeUiThreadDispatcher();
        Assert.Throws<ArgumentNullException>(() =>
            dispatcher.RaiseOnUi(null!));
    }

    [Fact]
    public void RaiseOnUiSync_SendsToDispatcher()
    {
        var dispatcher = new FakeUiThreadDispatcher();
        var ran = false;
        dispatcher.RaiseOnUiSync(() => ran = true);

        Assert.True(ran);
        Assert.Equal(1, dispatcher.SendCount);
    }

    private sealed class FakeUiThreadDispatcher : IUiThreadDispatcher
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
