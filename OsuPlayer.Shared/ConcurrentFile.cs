using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using SysIO = System.IO;

namespace Milki.OsuPlayer.Shared
{
    public static class ConcurrentFile
    {
        private static readonly ConcurrentDictionary<string, ReaderWriterLockSlim> LockDic =
            new ConcurrentDictionary<string, ReaderWriterLockSlim>();

        public static int Count => LockDic.Count;

        public static byte[] ReadAllBytes(string path)
        {
            path = GetFormattedPath(path);
            var cacheLock = LockDic.GetOrAdd(path, new ReaderWriterLockSlim());
            cacheLock.EnterReadLock();
            try
            {
                return SysIO.File.ReadAllBytes(path);
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        public static string[] ReadAllLines(string path)
        {
            path = GetFormattedPath(path);
            var cacheLock = LockDic.GetOrAdd(path, new ReaderWriterLockSlim());
            cacheLock.EnterReadLock();
            try
            {
                return SysIO.File.ReadAllLines(path);
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        public static string ReadAllText(string path)
        {
            path = GetFormattedPath(path);
            var cacheLock = LockDic.GetOrAdd(path, new ReaderWriterLockSlim());
            cacheLock.EnterReadLock();
            try
            {
                return SysIO.File.ReadAllText(path);
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        public static void WriteAllBytes(string path, byte[] bytes)
        {
            path = GetFormattedPath(path);
            var cacheLock = LockDic.GetOrAdd(path, new ReaderWriterLockSlim());
            cacheLock.EnterWriteLock();
            try
            {
                SysIO.File.WriteAllBytes(path, bytes);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        public static void WriteAllLines(string path, IEnumerable<string> contents)
        {
            path = GetFormattedPath(path);
            var cacheLock = LockDic.GetOrAdd(path, new ReaderWriterLockSlim());
            cacheLock.EnterWriteLock();
            try
            {
                SysIO.File.WriteAllLines(path, contents);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        public static void WriteAllText(string path, string contents)
        {
            path = GetFormattedPath(path);
            var cacheLock = LockDic.GetOrAdd(path, new ReaderWriterLockSlim());
            cacheLock.EnterWriteLock();
            try
            {
                SysIO.File.WriteAllText(path, contents);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        public static void AppendAllLines(string path, IEnumerable<string> contents)
        {
            path = GetFormattedPath(path);
            var cacheLock = LockDic.GetOrAdd(path, new ReaderWriterLockSlim());
            cacheLock.EnterWriteLock();
            try
            {
                SysIO.File.AppendAllLines(path, contents);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        public static void AppendAllText(string path, string contents)
        {
            path = GetFormattedPath(path);
            var cacheLock = LockDic.GetOrAdd(path, new ReaderWriterLockSlim());
            cacheLock.EnterWriteLock();
            try
            {
                SysIO.File.AppendAllText(path, contents);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        public static void Delete(string path)
        {
            path = GetFormattedPath(path);
            var cacheLock = LockDic.GetOrAdd(path, new ReaderWriterLockSlim());
            cacheLock.EnterWriteLock();
            try
            {
                SysIO.File.Delete(path);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

#if NETCOREAPP && NETCOREAPP2_1
        public static async Task<byte[]> ReadAllBytesAsync(string path)
        {
            path = GetFormattedPath(path);
            var cacheLock = LockDic.GetOrAdd(path, new ReaderWriterLockSlim());
            cacheLock.EnterReadLock();
            try
            {
                return await SysIO.File.ReadAllBytesAsync(path);
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        public static async Task<string[]> ReadAllLinesAsync(string path)
        {
            path = GetFormattedPath(path);
            var cacheLock = LockDic.GetOrAdd(path, new ReaderWriterLockSlim());
            cacheLock.EnterReadLock();
            try
            {
                return await SysIO.File.ReadAllLinesAsync(path);
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        public static async Task<string> ReadAllTextAsync(string path)
        {
            path = GetFormattedPath(path);
            var cacheLock = LockDic.GetOrAdd(path, new ReaderWriterLockSlim());
            cacheLock.EnterReadLock();
            try
            {
                return await SysIO.File.ReadAllTextAsync(path);
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        public static async Task WriteAllBytesAsync(string path, byte[] bytes)
        {
            path = GetFormattedPath(path);
            var cacheLock = LockDic.GetOrAdd(path, new ReaderWriterLockSlim());
            cacheLock.EnterWriteLock();
            try
            {
                await SysIO.File.WriteAllBytesAsync(path, bytes);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        public static async Task WriteAllLinesAsync(string path, IEnumerable<string> contents)
        {
            path = GetFormattedPath(path);
            var cacheLock = LockDic.GetOrAdd(path, new ReaderWriterLockSlim());
            cacheLock.EnterWriteLock();
            try
            {
                await SysIO.File.WriteAllLinesAsync(path, contents);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        public static async Task WriteAllTextAsync(string path, string contents)
        {
            path = GetFormattedPath(path);
            var cacheLock = LockDic.GetOrAdd(path, new ReaderWriterLockSlim());
            cacheLock.EnterWriteLock();
            try
            {
                await SysIO.File.WriteAllTextAsync(path, contents);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        public static async Task AppendAllLinesAsync(string path, IEnumerable<string> contents)
        {
            path = GetFormattedPath(path);
            var cacheLock = LockDic.GetOrAdd(path, new ReaderWriterLockSlim());
            cacheLock.EnterWriteLock();
            try
            {
                await SysIO.File.AppendAllLinesAsync(path, contents);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        public static async Task AppendAllTextAsync(string path, string contents)
        {
            path = GetFormattedPath(path);
            var cacheLock = LockDic.GetOrAdd(path, new ReaderWriterLockSlim());
            cacheLock.EnterWriteLock();
            try
            {
                await SysIO.File.AppendAllTextAsync(path, contents);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }
#endif

        private static string GetFormattedPath(string path) => new SysIO.FileInfo(path).FullName;
    }
}
