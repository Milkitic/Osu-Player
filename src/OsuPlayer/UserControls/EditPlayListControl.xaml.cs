using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer.UserControls;

public class EditPlayListControlVm : VmBase
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

/// <summary>
/// EditPlayListControl.xaml 的交互逻辑
/// </summary>
public partial class EditPlayListControl : UserControl
{
    private readonly PlayList _playList;
    private readonly EditPlayListControlVm _viewModel;

    public EditPlayListControl(PlayList playList)
    {
        _playList = playList;
        InitializeComponent();
        DataContext = _viewModel = new EditPlayListControlVm();
        _viewModel.Name = _playList.Name;
        _viewModel.Description = _playList.Description;
        _viewModel.CoverPath = _playList.ImagePath;
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        _playList.Name = _viewModel.Name;
        _playList.Description = _viewModel.Description;
        _playList.ImagePath = _viewModel.CoverPath;

        var appDbContext = ServiceProviders.GetApplicationDbContext();
        await appDbContext.AddOrUpdatePlayListAsync(_playList);
        App.CurrentMainContentDialog.RaiseOk();
    }

    private void BtnChooseImg_Click(object sender, RoutedEventArgs e)
    {
        using var fbd = new CommonOpenFileDialog
        {
            Title = @"请选择一个图片",
            Filters = { new CommonFileDialogFilter("所有支持的图片类型", @"jpg;png;jpeg") }
        };
        var result = fbd.ShowDialog();
        if (result == CommonFileDialogResult.Ok)
        {
            _viewModel.CoverPath = fbd.FileName;
        }
    }
}