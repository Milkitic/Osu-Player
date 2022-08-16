using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Milki.OsuPlayer.Wpf;

public class ApplicationTerminal
{
    public event Action RestartRequested;
    private readonly Application _app;

    private CancellationTokenSource _cts;
    private DispatcherTimer _dispatcherTimer;
    private Thread _checkThread;
    private int _count = 0;

    public ApplicationTerminal(Application app)
    {
        _app = app;
        app.Exit += App_Exit;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _dispatcherTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, (s, args) =>
            {
                if (_count > 10000)
                    _count = 0;
                else
                    _count++;
            },
            _app.Dispatcher);
        _checkThread = new Thread(() =>
        {
            int preCount = 0;
            while (!_cts.IsCancellationRequested)
            {
                Thread.Sleep(20000);

                if (preCount == _count)
                {
                    const string content = "程序卡死超过20秒，准备重启";
                    File.AppendAllText($"logs\\CriticalError-{DateTime.Now:yy-MM-dd_HH-mm-ss}.log", $"[{DateTime.Now}] {content}");

                    RestartRequested?.Invoke();
                    return;
                }
                else
                {
                    preCount = _count;
                }
            }
        });
        _dispatcherTimer.Start();
        _checkThread.Start();
    }

    private void App_Exit(object sender, ExitEventArgs e)
    {
        _cts?.Cancel();
        try
        {
            _checkThread.Abort();
        }
        catch (Exception)
        {
        }

        _dispatcherTimer?.Stop();
    }
}