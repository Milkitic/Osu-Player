using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Services;

namespace OsuPlayer.Views.UserControls;

public partial class WelcomeControlVm : ObservableObject
{
    [ObservableProperty]
    private bool _guideSyncing;

    [ObservableProperty]
    private bool _guideSelectedDb;

    [RelayCommand]
    private async Task SelectDbAsync()
    {
        GuideSyncing = true;
        await Task.Delay(1);
        AppNotificationService.Instance.Push("ui-err-osudb-sync");
        GuideSyncing = false;
    }

    [RelayCommand]
    private void Skip()
    {
    }
}

public partial class WelcomeControl : UserControl
{
    public WelcomeControlVm ViewModel { get; }

    public WelcomeControl()
    {
        InitializeComponent();
        ViewModel = (WelcomeControlVm)DataContext!;
    }
}
