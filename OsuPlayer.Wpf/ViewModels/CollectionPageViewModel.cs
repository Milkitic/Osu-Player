using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Data;
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
using Milky.OsuPlayer.Windows;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace Milky.OsuPlayer.ViewModels
{
    public class CollectionPageViewModel : VmBase
    {
        private readonly ObservablePlayController _controller = Service.Get<ObservablePlayController>();

        private ObservableCollection<OrderedBeatmap> _dataList;
        private Collection _collection;

        public ObservableCollection<OrderedBeatmap> DataList
        {
            get => _dataList;
            set
            {
                _dataList = value;
                OnPropertyChanged();
            }
        }

        public Collection Collection
        {
            get => _collection;
            set
            {
                _collection = value;
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
                return new DelegateCommand<OrderedBeatmap>(orderedModel =>
                {
                    var beatmap = orderedModel.Model;
                    var folderName = beatmap.GetFolder(out _, out _);
                    if (!Directory.Exists(folderName))
                    {
                        Notification.Push(@"所选文件不存在，可能没有及时同步。请尝试手动同步osuDB后重试。");
                        return;
                    }

                    ProcessLegacy.StartLegacy(folderName);
                });
            }
        }

        public ICommand OpenScorePageCommand
        {
            get
            {
                return new DelegateCommand<OrderedBeatmap>(orderedModel =>
                {
                    ProcessLegacy.StartLegacy($"https://osu.ppy.sh/s/{orderedModel.Model.BeatmapSetId}");
                });
            }
        }

        public ICommand SaveCollectionCommand
        {
            get
            {
                return new DelegateCommand<OrderedBeatmap>(orderedModel =>
                {
                    FrontDialogOverlay.Default.ShowContent(new SelectCollectionControl(orderedModel),
                        DialogOptionFactory.SelectCollectionOptions);
                });
            }
        }

        public ICommand ExportCommand
        {
            get
            {
                return new DelegateCommand<OrderedBeatmap>(orderedModel =>
                {
                    ExportPage.QueueBeatmap(orderedModel); //todo: export notification
                });
            }
        }

        public ICommand DirectPlayCommand
        {
            get
            {
                return new DelegateCommand<OrderedBeatmap>(async orderedModel =>
                {
                    await _controller.PlayNewAsync(orderedModel);
                });
            }
        }

        public ICommand PlayCommand
        {
            get
            {
                return new DelegateCommand<OrderedBeatmap>(async orderedModel =>
                {
                    await _controller.PlayNewAsync(orderedModel);
                });
            }
        }

        public ICommand RemoveCommand
        {
            get
            {
                return new DelegateCommand<OrderedBeatmap>(async orderedModel =>
                {
                    await using var appDbContext = new ApplicationDbContext();
                    //todo: support multiply
                    await appDbContext.DeleteBeatmapFromCollection(orderedModel, Collection);

                    if (_controller.PlayList.CurrentInfo.Beatmap.Equals(orderedModel) && Collection.IsDefault)
                    {
                        _controller.PlayList.CurrentInfo.BeatmapDetail.Metadata.IsFavorite = false;
                    }

                    DataList.Remove(orderedModel);
                    //await Services.Get<PlayerList>().RefreshPlayListAsync(PlayerList.FreshType.All, PlayListMode.Collection, _entries);
                });
            }
        }
    }
}
