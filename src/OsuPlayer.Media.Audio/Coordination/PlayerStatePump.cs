using System;
using KeyAsio.Core.Audio;
using Milky.OsuPlayer.Presentation.Interaction;

namespace Milky.OsuPlayer.Media.Audio.Coordination;

/// <summary>
/// Subscribes to the current <see cref="OsuMixPlayer"/> (when attached) and
/// mirrors its <see cref="OsuMixPlayer.PlayStatusChanged"/>,
/// <see cref="OsuMixPlayer.PositionUpdated"/>, and
/// <see cref="IPlaybackEngine.DeviceError"/> signals to:
/// <list type="bullet">
///   <item>the <see cref="PlayerEventBus"/>, for fan-out to UI subscribers, and</item>
///   <item>the local events used by the facade.</item>
/// </list>
/// Owns no business state; the session service decides what to do with
/// <c>Finished</c> status.
/// </summary>
internal sealed class PlayerStatePump : IDisposable
{
    private readonly PlayerEventBus _bus;
    private readonly Action<Exception> _audioDeviceErrorHandler;
    private readonly IUiThreadDispatcher _dispatcher;
    private readonly NLog.Logger _logger;

    private OsuMixPlayer? _player;

    public PlayerStatePump(
        PlayerEventBus bus,
        Action<Exception> audioDeviceErrorHandler,
        IUiThreadDispatcher dispatcher,
        NLog.Logger logger)
    {
        _bus = bus;
        _audioDeviceErrorHandler = audioDeviceErrorHandler;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public event Action<PlayStatus>? PlayStatusChanged;
    public event Action<TimeSpan>? PositionUpdated;
    public event Action? PlayerChanged;

    public OsuMixPlayer? Player => _player;
    public bool IsPlayerReady => _player != null && _player.PlayStatus != PlayStatus.Unknown;

    public void AttachPlayer(OsuMixPlayer player)
    {
        if (_player != null)
        {
            DetachPlayer();
        }

        _player = player;
        _player.PlayStatusChanged += OnPlayerPlayStatusChanged;
        _player.PositionUpdated += OnPlayerPositionUpdated;
        PlayerChanged?.Invoke();

        if (player.PlayStatus != PlayStatus.Unknown)
        {
            OnPlayerPlayStatusChanged(player.PlayStatus);
        }
    }

    public void DetachPlayer()
    {
        var existing = _player;
        if (existing == null) return;

        _player = null;
        existing.PlayStatusChanged -= OnPlayerPlayStatusChanged;
        existing.PositionUpdated -= OnPlayerPositionUpdated;
        PlayerChanged?.Invoke();
    }

    public void OnPlaybackEngineDeviceError(Exception ex)
    {
        _logger.Error(ex, "Audio device error.");
        _dispatcher.Post(() => _audioDeviceErrorHandler?.Invoke(ex));
    }

    public void Dispose() => DetachPlayer();

    private void OnPlayerPlayStatusChanged(PlayStatus status)
    {
        _dispatcher.Post(() => PlayStatusChanged?.Invoke(status));
        _bus.RaisePlayStatusChanged(status);
    }

    private void OnPlayerPositionUpdated(TimeSpan position)
    {
        _dispatcher.Post(() => PositionUpdated?.Invoke(position));
        _bus.RaisePositionUpdated(position);
    }
}
