namespace Milki.OsuPlayer.Shared;

public static class Constants
{
    public static string ApplicationDir { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Milki.OsuPlayer");
    public static string ConfigDir { get; } =
        Path.Combine(ApplicationDir, "configs");

    public const string DateTimeFormat = "g";
}