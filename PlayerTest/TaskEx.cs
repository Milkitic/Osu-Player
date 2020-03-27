using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlayerTest
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
            await Task.WhenAll(playingTask.Where(k => !(k is null)));
        }
    }
}