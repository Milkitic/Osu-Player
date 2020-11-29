﻿using System.Collections.ObjectModel;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Pages;
using Milky.OsuPlayer.Presentation;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Presentation.ObjectModel;
using Milky.OsuPlayer.Shared.Dependency;
using Milky.OsuPlayer.UiComponents.FrontDialogComponent;
using Milky.OsuPlayer.UiComponents.NotificationComponent;
using Milky.OsuPlayer.UserControls;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.Windows;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace Milky.OsuPlayer.ViewModels
{
    public class CollectionPageViewModel : VmBase
    {
        private readonly ObservablePlayController _controller = Service.Get<ObservablePlayController>();

        private ObservableCollection<Beatmap> _beatmaps;
        private ObservableCollection<Beatmap> _displayedBeatmaps;
        private Collection _collectionInfo;

        public ObservableCollection<Beatmap> Beatmaps
        {
            get => _beatmaps;
            set
            {
                _beatmaps = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Beatmap> DisplayedBeatmaps
        {
            get => _displayedBeatmaps;
            set
            {
                _displayedBeatmaps = value;
                OnPropertyChanged();
            }
        }

        public Collection CollectionInfo
        {
            get => _collectionInfo;
            set
            {
                _collectionInfo = value;
                OnPropertyChanged();
            }
        }

        public ICommand SearchByConditionCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    WindowEx.GetCurrentFirst<MainWindow>()
                        .SwitchSearch
                        .CheckAndAction(page => ((SearchPage)page).Search((string)param));
                });
            }
        }

        public ICommand OpenSourceFolderCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var map = _safeDbOperator.GetBeatmapByIdentifiable(beatmap);
                    if (map == null) return;
                    var folderName = beatmap.GetFolder(out _, out _);
                    if (!Directory.Exists(folderName))
                    {
                        Notification.Push(@"所选文件不存在，可能没有及时同步。请尝试手动同步osuDB后重试。");
                        return;
                    }

                    Process.Start(folderName);
                });
            }
        }

        public ICommand OpenScorePageCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var map = _safeDbOperator.GetBeatmapByIdentifiable(beatmap);
                    if (map == null) return;
                    Process.Start($"https://osu.ppy.sh/s/{map.BeatmapSetId}");
                });
            }
        }

        public ICommand SaveCollectionCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var map = _safeDbOperator.GetBeatmapByIdentifiable(beatmap);
                    if (map == null) return;
                    FrontDialogOverlay.Default.ShowContent(new SelectCollectionControl(map),
                        DialogOptionFactory.SelectCollectionOptions);
                });
            }
        }

        public ICommand ExportCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var map = _safeDbOperator.GetBeatmapByIdentifiable(beatmap);
                    if (map == null) return;
                    ExportPage.QueueEntry(map);
                });
            }
        }

        public ICommand DirectPlayCommand
        {
            get
            {
                return new DelegateCommand(async param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var map = _safeDbOperator.GetBeatmapByIdentifiable(beatmap);
                    if (map == null) return;
                    await _controller.PlayNewAsync(map);
                });
            }
        }

        public ICommand PlayCommand
        {
            get
            {
                return new DelegateCommand(async param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var map = _safeDbOperator.GetBeatmapByIdentifiable(beatmap);
                    if (map == null) return;
                    await _controller.PlayNewAsync(map);
                });
            }
        }

        public ICommand RemoveCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    if (!_safeDbOperator.TryRemoveMapFromCollection(beatmap.GetIdentity(), CollectionInfo))
                        return;
                    if (_controller.PlayList.CurrentInfo.Beatmap.GetIdentity().Equals(beatmap.GetIdentity()) &&
                        CollectionInfo.LockedBool)
                    {
                        _controller.PlayList.CurrentInfo.BeatmapDetail.Metadata.IsFavorite = false;
                    }

                    Beatmaps.Remove(beatmap);
                    DisplayedBeatmaps.Remove(beatmap);
                    //await Services.Get<PlayerList>().RefreshPlayListAsync(PlayerList.FreshType.All, PlayListMode.Collection, _entries);
                });
            }
        }
    }
}
