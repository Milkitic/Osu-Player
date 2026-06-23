using System.Threading;

namespace OsuPlayer.Data;

internal static class SqliteNativeProvider
{
    private static readonly Lock s_sync = new();
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (Volatile.Read(ref _initialized))
        {
            return;
        }

        lock (s_sync)
        {
            if (_initialized)
            {
                return;
            }

            SQLitePCL.Batteries.Init();
            Volatile.Write(ref _initialized, true);
        }
    }
}
