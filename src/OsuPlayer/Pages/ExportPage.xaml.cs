using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.Shared.Utils;
using Milki.OsuPlayer.UiComponents.NotificationComponent;
using Milki.OsuPlayer.Utils;

namespace Milki.OsuPlayer.Pages;

public class ExportPageVm : VmBase
{
    private ObservableCollection<ExportItem> _exportList;
    private string _exportPath;
    private List<ExportItem> _selectedItems;

    public ObservableCollection<ExportItem> ExportList
    {
        get => _exportList;
        set => this.RaiseAndSetIfChanged(ref _exportList, value);
    }

    public string ExportPath
    {
        get => _exportPath;
        set => this.RaiseAndSetIfChanged(ref _exportPath, value);
    }

    public List<ExportItem> SelectedItems
    {
        get => _selectedItems;
        set => this.RaiseAndSetIfChanged(ref _selectedItems, value);
    }
}

/// <summary>
///     ExportPage.xaml 的交互逻辑
/// </summary>
public partial class ExportPage : Page
{
    private readonly ExportService _exportService;
    private readonly ExportPageVm _viewModel;

    public ExportPage()
    {
        _exportService = ServiceProviders.Default.GetService<ExportService>();
        DataContext = _viewModel = new ExportPageVm();
        InitializeComponent();
        _viewModel.ExportPath = AppSettings.Default.ExportSection.DirMusic;
    }

    private void OpenCmdExecuted(object target, ExecutedRoutedEventArgs e)
    {
        string command, targetobj;
        command = ((RoutedCommand)e.Command).Name;
        targetobj = ((FrameworkElement)target).Name;
        MessageBox.Show("The " + command + " command has been invoked on target object " + targetobj);
    }

    private async ValueTask UpdateCollection()
    {
        await using var dbContext = ServiceProviders.GetApplicationDbContext();
        var paginationQueryResult = await dbContext.GetExportListFull();
        var exports = paginationQueryResult.Results;
        _viewModel.ExportList = new ObservableCollection<ExportItem>(exports);
    }

    private void OpenCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await UpdateCollection();
    }

    private void MiOpenSourceFolder_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;

        if (fe.Tag is string path)
        {
            if (Directory.Exists(path))
                ProcessUtils.StartWithShellExecute(path);
            else
                Notification.Push(I18NUtil.GetString("err-dirNotFound"), I18NUtil.GetString("text-error"));
        }
        else if (fe.Tag is ExportItem orderedModel)
        {
            ProcessUtils.StartWithShellExecute("Explorer", "/select," + orderedModel?.ExportPath);
        }
    }

    private async void MiReexport_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedItems == null) return;
        foreach (var exportItem in _viewModel.SelectedItems)
        {
            if (exportItem.PlayItem != null)
            {
                _exportService.QueueBeatmap(exportItem.PlayItem);
            }
            else
            {
                // todo: Not valid...
            }
        }

        while (_exportService.IsTaskBusy) await Task.Delay(10);

        if (_exportService.HasTaskSuccess) await UpdateCollection();
    }

    private async void MiDelete_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedItems == null) return;
        await using var dbContext = ServiceProviders.GetApplicationDbContext();

        foreach (var exportItem in _viewModel.SelectedItems)
        {
            if (!File.Exists(exportItem.ExportPath)) continue;
            File.Delete(exportItem.ExportPath);
            var dir = new FileInfo(exportItem.ExportPath).Directory;
            if (dir is { Exists: true } && !dir.EnumerateFiles().Any()) dir.Delete();
        }

        dbContext.Exports.RemoveRange(_viewModel.SelectedItems);
        await dbContext.SaveChangesAsync();
        await UpdateCollection();
    }
}