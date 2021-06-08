using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Exporter
{
    /// <summary>
    /// IO帮助类
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IOUtil
    {
        /// <summary>
        /// 循环搜索目录下的目录/文件
        /// </summary>
        /// <param name="path">所需搜索的目录</param>
        /// <param name="extName">文件扩展名，格式为".*"，不区分大小写</param>
        /// <returns></returns>
        public static IEnumerable<FileInfo> EnumerateFiles(string path, params string[] extName)
        {
            var currentDir = new DirectoryInfo(path);

            foreach (var fileInfo in InnerEnumerate(currentDir, extName))
            {
                yield return fileInfo;
            }
        }

        /// <summary>
        /// 内部循环，避免不必要的性能浪费
        /// </summary>
        /// <param name="currentDir">所需搜索的目录</param>
        /// <param name="extNames">文件扩展名，格式为".*"，须小写</param>
        /// <returns></returns>
        private static IEnumerable<FileInfo> InnerEnumerate(DirectoryInfo currentDir, string[] extNames)
        {
            // Enumerate以按需搜索，而非一次性返回目录下的文件
            // 可防止子项过多而阻塞线程

            // 目录列表     
            IEnumerable<DirectoryInfo> subDirs = currentDir.EnumerateDirectories()
                .AsParallel(); // 并行处理
            // 文件列表
            IEnumerable<FileInfo> files = currentDir.EnumerateFiles()
                .AsParallel();

            foreach (FileInfo fi in files) // 枚举文件列表
            {
                if (extNames == null ||
                    extNames.Length == 0 ||
                    extNames.Contains(fi.Extension, StringComparer.OrdinalIgnoreCase))
                {
                    yield return fi;
                }
            }

            foreach (DirectoryInfo di in subDirs) // 枚举目录列表
            {
                var subFiles = InnerEnumerate(di, extNames); // 递归枚举子目录
                foreach (var subFile in subFiles)
                {
                    yield return subFile;
                }
            }
        }
    }
}