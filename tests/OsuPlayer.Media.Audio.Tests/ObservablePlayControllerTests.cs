using System.Reflection;
using KeyAsio.Core.Audio;
using KeyAsio.Core.Audio.SampleProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NAudio.Wave;
using OsuPlayer.Media.Audio.Coordination;
using OsuPlayer.Presentation.Interaction;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public class ObservablePlayControllerTests
{
    private static readonly ILogger<ObservablePlayController> ControllerLog =
        NullLoggerFactory.Instance.CreateLogger<ObservablePlayController>();
    private static readonly ILogger<PlayerEventBus> BusLog =
        NullLoggerFactory.Instance.CreateLogger<PlayerEventBus>();
    private static readonly ILogger<PlayerSessionService> SessionLog =
        NullLoggerFactory.Instance.CreateLogger<PlayerSessionService>();
    private static readonly ILogger<OsuMixPlayer> PlayerLog =
        NullLoggerFactory.Instance.CreateLogger<OsuMixPlayer>();
    private static readonly ILoggerFactory LogFactory =
        NullLoggerFactory.Instance;

    [Fact]
    public void Player_ReturnsPumpCurrentPlayerAndRaisesChangeNotification()
    {
        var controller = new ObservablePlayController(
            null!,
            new FakePlaybackEngine(),
            null!,
            _ => { },
            new ImmediateUiThreadDispatcher(),
            null!,
            ControllerLog,
            BusLog,
            SessionLog,
            LogFactory);
        var bus = GetBus(controller);
        var player = new OsuMixPlayer(null!, string.Empty, null!, null!, PlayerLog);
        var notified = false;

        controller.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ObservablePlayController.Player))
            {
                notified = true;
            }
        };

        bus.AttachPlayer(player);

        Assert.Same(player, controller.Player);
        Assert.True(notified);
    }

    [Fact]
    public async Task PlayAsync_IgnoresAttachedPlayerUntilReady()
    {
        var controller = new ObservablePlayController(
            null!,
            new FakePlaybackEngine(),
            null!,
            _ => { },
            new ImmediateUiThreadDispatcher(),
            null!,
            ControllerLog,
            BusLog,
            SessionLog,
            LogFactory);
        var bus = GetBus(controller);
        var player = new OsuMixPlayer(null!, string.Empty, null!, null!, PlayerLog);

        bus.AttachPlayer(player);

        await controller.PlayAsync();
    }

    [Fact]
    public void AttachPlayer_ReplaysReadyStatusToFacade()
    {
        var controller = new ObservablePlayController(
            null!,
            new FakePlaybackEngine(),
            null!,
            _ => { },
            new ImmediateUiThreadDispatcher(),
            null!,
            ControllerLog,
            BusLog,
            SessionLog,
            LogFactory);
        var bus = GetBus(controller);
        var player = new OsuMixPlayer(null!, string.Empty, null!, null!, PlayerLog);
        SetPlayStatus(player, PlayStatus.Ready);
        PlayStatus? observed = null;

        controller.PlayStatusChanged += status => observed = status;

        bus.AttachPlayer(player);

        Assert.Equal(PlayStatus.Ready, observed);
        Assert.True(controller.IsPlayerReady);
    }

    private static PlayerEventBus GetBus(ObservablePlayController controller)
    {
        var field = typeof(ObservablePlayController)
            .GetField("_bus", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<PlayerEventBus>(field.GetValue(controller));
    }

    private static void SetPlayStatus(OsuMixPlayer player, PlayStatus status)
    {
        var field = typeof(OsuMixPlayer)
            .GetField("_playStatus", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(player, status);
    }

    private sealed class ImmediateUiThreadDispatcher : IUiThreadDispatcher
    {
        public void Post(Action action) => action();
        public void Send(Action action) => action();
    }

    private sealed class FakePlaybackEngine : IPlaybackEngine
    {
        public event Action<DeviceDescription>? DeviceStarted { add { } remove { } }
        public event Action? DeviceStopped { add { } remove { } }
        public event Action<Exception>? DeviceError { add { } remove { } }

        public IWavePlayer? CurrentDevice => null;
        public DeviceDescription? CurrentDeviceDescription => null;
        public WaveFormat EngineWaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        public WaveFormat SourceWaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        public IMixingSampleProvider EffectMixer => null!;
        public IMixingSampleProvider MusicMixer => null!;
        public IMixingSampleProvider RootMixer => null!;
        public ISampleProvider RootSampleProvider => null!;
        public WaveFormat? WaveFormat => SourceWaveFormat;
        public LimiterType LimiterType { get; set; }
        public float MainVolume { get; set; }
        public float EffectVolume { get; set; }
        public float MusicVolume { get; set; }

        public void AddInput(ISampleProvider input) { }
        public void RemoveInput(ISampleProvider input) { }
        public void StartDevice(DeviceDescription? deviceDescription, WaveFormat? waveFormat = null) { }
        public void StopDevice() { }
        public void Dispose() { }
    }
}
