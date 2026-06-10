using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using OsuPlayer.Core;
using OsuPlayer.ViewModels;

namespace OsuPlayer.Views.Pages;

public partial class RecentPlayPage : UserControl
{
    public RecentPlayPage()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    public RecentPlayPage(RecentPlayPageViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private async void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is RecentPlayPageViewModel viewModel)
        {
            await viewModel.UpdateListAsync();
        }
    }

    private async void RecentList_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (RecentList.SelectedItem is BeatmapDataModel map && DataContext is RecentPlayPageViewModel viewModel)
        {
            await viewModel.DirectPlayAsync(map);
        }
    }
}
