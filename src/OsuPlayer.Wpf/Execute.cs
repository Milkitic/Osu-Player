using System.Windows;

namespace Milki.OsuPlayer.Wpf;

public static class Execute
{
    public static void OnUiThread(this Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (Application.Current?.Dispatcher != null)
        {
            Application.Current.Dispatcher.Invoke(action);
        }
        else
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine("UiContext execute error: " + ex.Message);
            }
        }
    }

    public static async Task ToUiThreadAsync(Func<Task> asyncAction)
    {
        if (asyncAction == null) throw new ArgumentNullException(nameof(asyncAction));
        if (Application.Current?.Dispatcher != null)
        {
            await Application.Current.Dispatcher.InvokeAsync(asyncAction);
        }
        else
        {
            try
            {
                await asyncAction.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine("UiContext execute error: " + ex.Message);
            }
        }
    }

    public static async Task ToUiThreadAsync(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (Application.Current?.Dispatcher != null)
        {
            await Application.Current.Dispatcher.InvokeAsync(action);
        }
        else
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine("UiContext execute error: " + ex.Message);
            }
        }
    }
}