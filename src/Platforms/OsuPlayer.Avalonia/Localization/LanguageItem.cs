namespace OsuPlayer.Localization;

public sealed class LanguageItem
{
    public required string Code { get; init; }
    public required string Name { get; init; }

    public override string ToString() => Name;
}
