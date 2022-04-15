namespace OsuPlayer.Shared.Configuration;

public class SectionSoftware
{
    public string? Language { get; set; } = "en-us";
    public bool RunOnSystemStartup { get; set; }
    public bool? ExitWhenCloseMainWindow { get; set; }
}