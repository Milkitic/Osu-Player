namespace Milki.OsuPlayer.Shared.Utils;

public static class SharedUtils
{
    public static string CountSize(long size)
    {
        string strSize = "";
        long factSize = size;
        if (factSize < 1024)
            strSize = $"{factSize:F2} B";
        else if (factSize >= 1024 && factSize < 1048576)
            strSize = (factSize / 1024f).ToString("F2") + " KB";
        else if (factSize >= 1048576 && factSize < 1073741824)
            strSize = (factSize / 1024f / 1024f).ToString("F2") + " MB";
        else if (factSize >= 1073741824)
            strSize = (factSize / 1024f / 1024f / 1024f).ToString("F2") + " GB";
        return strSize;
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