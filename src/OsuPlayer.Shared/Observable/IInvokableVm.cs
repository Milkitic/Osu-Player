namespace Milki.OsuPlayer.Shared.Observable;

public interface IInvokableVm
{
    internal void RaisePropertyChanged(string propertyName);
    internal void RaisePropertyChanging(string propertyName);
}