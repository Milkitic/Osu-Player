using System;

namespace OsuPlayer.Shared;

/// <summary>
/// Abstracts dispatching work to the UI thread without coupling domain services to WPF.
/// </summary>
public interface IUiThreadDispatcher
{
    void Send(Action action);

    void Post(Action action);
}
