using System.Diagnostics;

namespace Milki.OsuPlayer.Utils;

public sealed class InputDelayQueryHelper
{
    private readonly object _queryLock = new();
    private readonly Stopwatch _querySw = new();
    private bool _isQuerying;
    public Func<int, ValueTask> QueryAsync;

    public async ValueTask StartDelayedQuery(int page = 0)
    {
        _querySw.Restart();

        lock (_queryLock)
        {
            if (_isQuerying)
            {
                return;
            }

            _isQuerying = true;
        }

        try
        {
            await Task.Run(() =>
            {
                while (_querySw.ElapsedMilliseconds < 300)
                    Thread.Sleep(10);
                _querySw.Stop();
            });

            await QueryAsync(page);
        }
        finally
        {
            lock (_queryLock)
            {
                _isQuerying = false;
            }
        }
    }
}