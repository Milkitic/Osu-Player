namespace Milki.OsuPlayer.Shared.Utils;

public static class PathUtils
{
    public static string GetFullPath(string standardizedPath, string songDir)
    {
        if (standardizedPath.StartsWith("./"))
        {
            return Path.GetFullPath(Path.Combine(songDir, standardizedPath.Substring(2)));
        }

        return Path.GetFullPath(standardizedPath);
    }

    public static string GetFolder(string path)
    {
        var lastSeparator = path.LastIndexOf('/');
        if (lastSeparator != -1) return path[..lastSeparator];

        lastSeparator = path.LastIndexOf('\\');
        if (lastSeparator != -1) return path[..lastSeparator];

        throw new Exception("The path does not have a folder");
    }

    public static string StandardizePath(string path, string? baseFolder)
    {
        if (baseFolder == null) return path.Replace('\\', '/');
        if (path.StartsWith('.')) return path.Replace('\\', '/');

        var truePath = Path.GetRelativePath(baseFolder, path);
        if (truePath.Contains("..", StringComparison.Ordinal))
            return path.Replace('\\', '/'); // Not in base folder
        if (Path.IsPathRooted(truePath))
            return truePath.Replace('\\', '/');

        var separator = Path.DirectorySeparatorChar;
        var finalPath = string.Create(truePath.Length + 2, truePath, (span, s) =>
        {
            span[0] = '.';
            span[1] = '/';
            var folder = s.AsSpan();
            int i = 2;
            foreach (var c in folder)
            {
                span[i] = c == separator ? '/' : c;
                i++;
            }
        });
        return finalPath;
    }
}