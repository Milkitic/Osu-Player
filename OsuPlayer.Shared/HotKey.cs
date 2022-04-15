using Milki.Extensions.MouseKeyHook;

namespace OsuPlayer.Shared;

public readonly struct HotKey
{
    public HotKeyType Type { get; init; }
    public HookKeys? Key { get; init; }
    public ModifierKeys? ModifierKeys { get; init; }
}