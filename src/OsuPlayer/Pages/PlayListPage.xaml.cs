﻿using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Audio;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.UiComponents.ContentDialogComponent;
using Milki.OsuPlayer.UserControls;
using Milki.OsuPlayer.Utils;

namespace Milki.OsuPlayer.Pages;

public class PlayListPageVm : VmBase
{
    private PlayList _playList;
    
    public PlayList PlayList
    {
        get => _playList;
        set => this.RaiseAndSetIfChanged(ref _playList, value);
    }
}

/// <summary>
/// PlayListPage.xaml 的交互逻辑
/// </summary>
public partial class PlayListPage : Page
{
    private readonly PlayerService _playerService;
    private readonly PlayListService _playListService;
    private readonly PlayListPageVm _viewModel;

    private List<PlayItem> _playItems;
    private bool _isFirstLoaded;

    public PlayListPage(PlayList playList)
    {
        InitializeComponent();
        _playerService = App.Current.ServiceProvider.GetService<PlayerService>();
        _playListService = App.Current.ServiceProvider.GetService<PlayListService>();
        DataContext = _viewModel = new PlayListPageVm();
        _viewModel.PlayList = playList;
    }

    public async Task UpdateList()
    {
        await using var dbContext = ServiceProviders.GetApplicationDbContext();
        var playListDetail = await dbContext.PlayLists
            .Include(k => k.PlayListRelations)
                .ThenInclude(k => k.PlayItem)
                    .ThenInclude(k => k.PlayItemDetail)
            .Include(k => k.PlayListRelations)
                .ThenInclude(k => k.PlayItem)
                    .ThenInclude(k => k.PlayItemAsset)
            .FirstOrDefaultAsync(k => k.Id == _viewModel.PlayList.Id);
        if (playListDetail == null)
        {
            CardCollectionControl.PlayItems = null;
            return;
        }

        _playItems = playListDetail.PlayListRelations
            .OrderByDescending(k => k.CreateTime)
            .Select(k => k.PlayItem).ToList();

        CardCollectionControl.PlayItems = new ObservableCollection<PlayItem>(_playItems);
        ListCount.Content = _playItems.Count;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_isFirstLoaded)
        {
            await UpdateList();
            _isFirstLoaded = true;
        }

        var item = _playItems?.FirstOrDefault(k => k.StandardizedPath == _playListService.GetCurrentPath());
        if (item != null)
        {
            //MapCardList.SelectedItem = item;
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var keyword = SearchBox.Text.Trim();
        CardCollectionControl.PlayItems = string.IsNullOrWhiteSpace(keyword)
            ? _playItems
            : new ObservableCollection<PlayItem>(_playItems); // todo: search
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
    }

    private async void BtnDelCol_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(I18NUtil.GetString("ui-ensureRemoveCollection"),
            App.CurrentMainWindow?.Title,
            MessageBoxButton.OKCancel,
            MessageBoxImage.Exclamation);
        if (result != MessageBoxResult.OK) return;
        await using var dbContext = ServiceProviders.GetApplicationDbContext();

        dbContext.Remove(_viewModel.PlayList);
        await dbContext.SaveChangesAsync();

        await SharedVm.Default.UpdatePlayListsAsync();
        SharedVm.Default.CheckedNavigationType = NavigationType.Recent;
    }

    private void BtnExportAll_Click(object sender, RoutedEventArgs e)
    {
        ExportPage.QueueBeatmaps(_playItems);
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        App.CurrentMainContentDialog.ShowContent(new EditPlayListControl(_viewModel.PlayList),
            DialogOptionFactory.EditPlayListOptions, (_, _) =>
            {
                var playList = _viewModel.PlayList;
                _viewModel.PlayList = null;
                _viewModel.PlayList = playList;
            });
    }

    private async void BtnPlayAll_Click(object sender, RoutedEventArgs e)
    {
        if (_playItems.Count <= 0) return;
        await FormUtils.ReplacePlayListAndPlayAll(_playItems.Select(k => k.StandardizedPath),
            _playListService, _playerService);
    }
}