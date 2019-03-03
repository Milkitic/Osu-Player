using Milky.OsuPlayer.Common.Player;
using System.Collections.Concurrent;
using System.Linq;

namespace Milky.OsuPlayer.Common
{
    public static class InstanceManage
    {
         private static readonly ConcurrentBag<object> InstanceObjects = new ConcurrentBag<object>();

        public static void AddInstance<T>(T instance)
        {
            InstanceObjects.Add(instance);
        }

        public static T GetInstance<T>()
        {
            return (T)InstanceObjects.FirstOrDefault(k => k.GetType() == typeof(T));
        }
    }

}
