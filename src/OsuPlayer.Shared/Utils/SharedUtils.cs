namespace Milki.OsuPlayer.Shared.Utils;

public static class SharedUtils
{
    private static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

    public static string SizeSuffix(long value, int decimalPlaces = 1)
    {
        if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }

        int i = 0;
        double dValue = value;
        while (Math.Round(dValue, decimalPlaces) >= 1000)
        {
            dValue /= 1024;
            i++;
        }

        return string.Format("{0:n" + decimalPlaces + "} {1}", dValue, SizeSuffixes[i]);
    }

    public static IEnumerable<FileInfo> EnumerateFiles(string path, params string[] extName)
    {
        var lowerExtName = extName?.Select(k => k.ToLower()).ToArray();
        DirectoryInfo currentDir = new DirectoryInfo(path);
        //IEnumerable<string> subDirs = Directory.EnumerateDirectories(path).AsParallel(); // 目录列表                    
        IEnumerable<FileInfo> files = currentDir.EnumerateFiles().AsParallel();

        foreach (FileInfo f in files) // 显示当前目录所有文件   
        {
            if (lowerExtName == null || lowerExtName.Length == 0 || lowerExtName.Contains(f.Extension.ToLower()))
            {
                yield return f;
            }
        }
    }
}