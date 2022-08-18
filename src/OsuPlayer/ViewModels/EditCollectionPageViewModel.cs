using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer.ViewModels;

public class EditCollectionPageViewModel : VmBase
{
    private string _name;
    private string _description;
    private string _coverPath;

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public string Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    public string CoverPath
    {
        get => _coverPath;
        set => this.RaiseAndSetIfChanged(ref _coverPath, value);
    }
}