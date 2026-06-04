using System;
using System.Reflection;
using System.Threading.Tasks;
using KeyAsio.Core.Audio;
using KeyAsio.Core.Audio.SampleProviders;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Media.Audio.Coordination;
using Milky.OsuPlayer.Presentation.Interaction;
using NAudio.Wave;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public class ObservablePlayControllerTests
{
    [Fact]
    public void Player_ReturnsPumpCurrentPlayerAndRaisesChangeNotification()
    {
        var controller = new ObservablePlayController(
            null!,
            new FakePlaybackEngine(),
            null!,
            null!,
            _ => { },
            new ImmediateUiThreadDispatcher());
        var pump = GetPump(controller);
        var player = new OsuMixPlayer(null!, string.Empty, null!, null!);
        var notified = false;

        controller.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ObservablePlayController.Player))
            {
                notified = true;
            }
        };

        pump.AttachPlayer(player);

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
            null!,
            _ => { },
            new ImmediateUiThreadDispatcher());
        var pump = GetPump(controller);
        var player = new OsuMixPlayer(null!, string.Empty, null!, null!);

        pump.AttachPlayer(player);

        await controller.PlayAsync();
    }

    [Fact]
    public void AttachPlayer_ReplaysReadyStatusToFacade()
    {
        var controller = new ObservablePlayController(
            null!,
            new FakePlaybackEngine(),
            null!,
            null!,
            _ => { },
            new ImmediateUiThreadDispatcher());
        var pump = GetPump(controller);
        var player = new OsuMixPlayer(null!, string.Empty, null!, null!);
        SetPlayStatus(player, PlayStatus.Ready);
        PlayStatus? observed = null;

        controller.PlayStatusChanged += status => observed = status;

        pump.AttachPlayer(player);

        Assert.Equal(PlayStatus.Ready, observed);
        Assert.True(controller.IsPlayerReady);
    }

    private static PlayerStatePump GetPump(ObservablePlayController controller)
    {
        var field = typeof(ObservablePlayController)
            .GetField("_pump", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<PlayerStatePump>(field.GetValue(controller));
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
