namespace OsuPlayer.Shared;

public static class PathUtils
{
    public static string GetFolder(string path)
    {
        var lastSeparator = path.LastIndexOf('/');
        if (lastSeparator != -1) return path[..lastSeparator];

        lastSeparator = path.LastIndexOf('\\');
        if (lastSeparator != -1) return path[..lastSeparator];

        throw new Exception("The path does not have a folder");
    }
}