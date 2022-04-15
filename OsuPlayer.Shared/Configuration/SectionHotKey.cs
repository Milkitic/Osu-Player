namespace OsuPlayer.Shared.Configuration;

public sealed class SectionHotKey
{
    public bool IsHotKeyEnabled { get; set; } = true;
    public List<HotKey> HotKeys { get; set; } = new();
}