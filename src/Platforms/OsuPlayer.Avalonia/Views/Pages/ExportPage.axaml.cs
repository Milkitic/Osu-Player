using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using OsuPlayer.Core;
using OsuPlayer.ViewModels;

namespace OsuPlayer.Views.Pages;

public partial class ExportPage : UserControl
{
    public ExportPage()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    public ExportPage(ExportPageViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private async void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is ExportPageViewModel viewModel)
        {
            await viewModel.UpdateListAsync();
        }
    }

    private void OpenItemFolder_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: BeatmapDataModel map } && DataContext is ExportPageViewModel viewModel)
        {
            viewModel.ItemFolderCommand.Execute(map);
        }
    }

    private async void ReExportItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: BeatmapDataModel map } && DataContext is ExportPageViewModel viewModel)
        {
            await viewModel.ReExportAsync(new[] { map });
        }
    }

    private async void DeleteItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: BeatmapDataModel map } && DataContext is ExportPageViewModel viewModel)
        {
            await viewModel.DeleteAsync(new[] { map });
        }
    }
}
