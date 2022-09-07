namespace Milki.OsuPlayer.Shared.Observable;

public abstract class SingletonVm<T> : VmBase
    where T : SingletonVm<T>, new()
{
    protected SingletonVm()
    {
    }

    public static T Default { get; } = new();
}