using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Pages;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.UiComponents.NotificationComponent;
using Milki.OsuPlayer.Utils;
using Milki.OsuPlayer.Wpf.Command;

namespace Milki.OsuPlayer.ViewModels;

public class ExportPageViewModel : VmBase
{
    private string _exportPath;
    private ObservableCollection<ExportItem> _exportList;
    private List<ExportItem> _selectedItems;

    public ObservableCollection<ExportItem> ExportList
    {
        get => _exportList;
        set
        {
            _exportList = value;
            OnPropertyChanged();
        }
    }

    public List<ExportItem> SelectedItems
    {
        get => _selectedItems;
        set
        {
            if (Equals(value, _selectedItems)) return;
            _selectedItems = value;
            OnPropertyChanged();
        }
    }

    public string ExportPath
    {
        get => _exportPath;
        set
        {
            _exportPath = value;
            OnPropertyChanged();
        }
    }

    public ICommand UpdateList
    {
        get
        {
            return new DelegateCommand(async obj =>
            {
                await UpdateCollection();
            });
        }
    }

    public ICommand ItemFolderCommand
    {
        get
        {
            return new DelegateCommand(obj =>
            {
                switch (obj)
                {
                    case string path:
                        if (Directory.Exists(path))
                        {
                            Process.Start(path);
                        }
                        else
                        {
                            Notification.Push(I18NUtil.GetString("err-dirNotFound"), I18NUtil.GetString("text-error"));
                        }
                        break;
                    case ExportItem orderedModel:
                        Process.Start("Explorer", "/select," + orderedModel?.ExportPath);
                        // todo: include
                        break;
                    default:
                        return;
                }
            });
        }
    }

    public ICommand ItemReExportCommand
    {
        get
        {
            return new DelegateCommand(async obj =>
            {
                if (SelectedItems == null) return;
                foreach (var exportItem in SelectedItems)
                {
                    if (exportItem.PlayItem != null)
                    {
                        ExportPage.QueueBeatmap(exportItem.PlayItem);
                    }
                    else
                    {
                        // todo: Not valid...
                    }
                }

                while (ExportPage.IsTaskBusy)
                {
                    await Task.Delay(10);
                }

                if (ExportPage.HasTaskSuccess)
                    await UpdateCollection();
            });
        }
    }

    public ICommand ItemDeleteCommand
    {
        get
        {
            return new DelegateCommand(async obj =>
            {
                if (SelectedItems == null) return;
                await using var dbContext = new ApplicationDbContext();

                foreach (var exportItem in SelectedItems)
                {
                    var export = exportItem;
                    if (File.Exists(export.ExportPath))
                    {
                        File.Delete(export.ExportPath);
                        var dir = new FileInfo(export.ExportPath).Directory;
                        if (dir.Exists && dir.GetFiles().Length == 0)
                            dir.Delete();
                    }
                }

                dbContext.Exports.RemoveRange(SelectedItems);
                await dbContext.SaveChangesAsync();
                await UpdateCollection();
            });
        }
    }

    private async Task UpdateCollection()
    {
        await using var dbContext = new ApplicationDbContext();
        var paginationQueryResult = await dbContext.GetExportListFull();
        var exports = paginationQueryResult.Results;
        //foreach (var export in exports)
        //{
        //    var map = export.PlayItem;
        //    try
        //    {
        //        var fi = new FileInfo(export.ExportPath);
        //        if (fi.Exists)
        //        {
        //            export.IsValid = true;
        //            export.IsItemLost = fi.Length;
        //            export.CreationTime = fi.CreationTime;
        //        }
        //        else
        //        {
        //            export.IsValid = true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogTo.ErrorException($"Error while updating view item: {map}", ex);
        //    }
        //}

        ExportList = new ObservableCollection<ExportItem>(exports);
    }
}