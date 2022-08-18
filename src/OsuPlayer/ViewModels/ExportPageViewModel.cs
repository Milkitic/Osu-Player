using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Anotar.NLog;
using Coosu.Database.DataTypes;
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
    private ObservableCollection<OrderedModel<ExportItem>> _exportList;
    private List<OrderedModel<ExportItem>> _selectedItems;

    public ObservableCollection<OrderedModel<ExportItem>> ExportList
    {
        get => _exportList;
        set
        {
            _exportList = value;
            OnPropertyChanged();
        }
    }

    public List<OrderedModel<ExportItem>> SelectedItems
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
                    case OrderedModel<Beatmap> orderedModel:
                        Process.Start("Explorer", "/select," + orderedModel.Model.ExportItem?.ExportPath);
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
                foreach (var orderedModel in SelectedItems)
                {
                    ExportPage.QueueBeatmap(orderedModel.Model.Beatmap);
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

                foreach (var orderedModel in SelectedItems)
                {
                    var export = orderedModel.Model;
                    if (File.Exists(export.ExportPath))
                    {
                        File.Delete(export.ExportPath);
                        var dir = new FileInfo(export.ExportPath).Directory;
                        if (dir.Exists && dir.GetFiles().Length == 0)
                            dir.Delete();
                    }
                }

                await dbContext.RemoveExports(SelectedItems.Select(k => k.Model));
                await UpdateCollection();
            });
        }
    }

    private async Task UpdateCollection()
    {
        await using var dbContext = new ApplicationDbContext();
        var paginationQueryResult = await dbContext.GetExportList();
        var exports = paginationQueryResult.Collection;
        foreach (var export in exports)
        {
            var map = export.Beatmap;
            try
            {
                var fi = new FileInfo(export.ExportPath);
                if (fi.Exists)
                {
                    export.IsValid = true;
                    export.Size = fi.Length;
                    export.CreationTime = fi.CreationTime;
                }
                else
                {
                    export.IsValid = true;
                }
            }
            catch (Exception ex)
            {
                LogTo.ErrorException($"Error while updating view item: {map}", ex);
            }
        }

        ExportList = new ObservableCollection<OrderedModel<ExportItem>>(exports.AsOrdered());
    }
}