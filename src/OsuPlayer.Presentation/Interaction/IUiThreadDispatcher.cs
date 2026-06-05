using System;

namespace OsuPlayer.Presentation.Interaction;

/// <summary>
/// Abstracts UI-thread dispatching, decoupling consumers from WPF's
/// <see cref="System.Windows.Threading.Dispatcher"/> or any specific UI framework.
/// </summary>
public interface IUiThreadDispatcher
{
    /// <summary>
    /// Invokes <paramref name="action"/> synchronously on the UI thread,
    /// blocking the calling thread until completion. If already on the UI
    /// thread, the action runs inline.
    /// </summary>
    void Send(Action action);

    /// <summary>
    /// Posts <paramref name="action"/> to the UI thread asynchronously
    /// and returns immediately. If a UI thread is not available, the
    /// action runs inline on the calling thread.
    /// </summary>
    void Post(Action action);
}