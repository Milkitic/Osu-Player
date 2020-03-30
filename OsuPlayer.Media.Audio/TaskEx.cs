using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Media.Audio
{
    internal class TaskEx
    {
        public static bool TaskSleep(int milliseconds, CancellationTokenSource cts)
        {
            return TaskSleep(TimeSpan.FromMilliseconds(milliseconds), cts);
        }

        public static bool TaskSleep(TimeSpan time, CancellationTokenSource cts)
        {
            try
            {
                Task.Delay(time).Wait(cts.Token);
            }
            catch (TaskCanceledException)
            {
                return false;
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            return true;
        }

        public static async Task WhenAllSkipNull(params Task[] playingTask)
        {
            await Task.WhenAll(playingTask.Where(k => !(k is null))).ConfigureAwait(false);
        }

        public static void WaitAllSkipNull(params Task[] playingTask)
        {
            Task.WaitAll(playingTask.Where(k => !(k is null)).ToArray());
        }
    }
}