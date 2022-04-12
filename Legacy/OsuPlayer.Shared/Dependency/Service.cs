﻿using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Milky.OsuPlayer.Shared.Dependency
{
    public static class Service
    {
        private static readonly ConcurrentDictionary<Type, object> InstanceObjects = new ConcurrentDictionary<Type, object>();

        public static bool TryAddInstance<T>(T instance)
        {
            return InstanceObjects.TryAdd(typeof(T), instance);
        }

        public static void AddOrUpdateInstance<T>(T instance, Func<Type, object, object> updateValueFactory)
        {
            InstanceObjects.AddOrUpdate(typeof(T), instance, updateValueFactory);
        }

        public static T Get<T>()
        {
            return (T)InstanceObjects.FirstOrDefault(k => k.Key == typeof(T)).Value;
        }
    }

}
