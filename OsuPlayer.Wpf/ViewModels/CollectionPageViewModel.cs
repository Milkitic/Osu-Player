using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Milki.OsuPlayer.Common;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Media.Audio;
using Milki.OsuPlayer.Pages;
using Milki.OsuPlayer.Presentation;
using Milki.OsuPlayer.Presentation.Interaction;
using Milki.OsuPlayer.Presentation.ObjectModel;
using Milki.OsuPlayer.Shared.Dependency;
using Milki.OsuPlayer.UiComponents.FrontDialogComponent;
using Milki.OsuPlayer.UiComponents.NotificationComponent;
using Milki.OsuPlayer.UserControls;
using Milki.OsuPlayer.Windows;

namespace Milki.OsuPlayer.ViewModels
{
    public class CollectionPageViewModel : VmBase
    {
        private readonly ObservablePlayController _controller = Service.Get<ObservablePlayController>();

        private ObservableCollection<OrderedModel<Beatmap>> _dataList;
        private Collection _collection;

        public ObservableCollection<OrderedModel<Beatmap>> DataList
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
                return new DelegateCommand<OrderedModel<Beatmap>>(orderedModel =>
                {
                    var beatmap = orderedModel.Model;
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
                return new DelegateCommand<OrderedModel<Beatmap>>(orderedModel =>
                {
                    Process.Start($"https://osu.ppy.sh/s/{orderedModel.Model.BeatmapSetId}");
                });
            }
        }

        public ICommand SaveCollectionCommand
        {
            get
            {
                return new DelegateCommand<OrderedModel<Beatmap>>(orderedModel =>
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
                return new DelegateCommand<OrderedModel<Beatmap>>(orderedModel =>
                {
                    ExportPage.QueueBeatmap(orderedModel); //todo: export notification
                });
            }
        }

        public ICommand DirectPlayCommand
        {
            get
            {
                return new DelegateCommand<OrderedModel<Beatmap>>(async orderedModel =>
                {
                    await _controller.PlayNewAsync(orderedModel);
                });
            }
        }

        public ICommand PlayCommand
        {
            get
            {
                return new DelegateCommand<OrderedModel<Beatmap>>(async orderedModel =>
                {
                    await _controller.PlayNewAsync(orderedModel);
                });
            }
        }

        public ICommand RemoveCommand
        {
            get
            {
                return new DelegateCommand<OrderedModel<Beatmap>>(async orderedModel =>
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
